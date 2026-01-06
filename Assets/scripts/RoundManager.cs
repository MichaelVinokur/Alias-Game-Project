using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;
using Photon.Voice.Unity;//online


public class RoundManager : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public TMP_Text wordText;
    public TMP_Text playerNameText;
    public TMP_Text timerText;
    public Button correctButton;
    public Button penaltyButton;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip correctSFX;
    public AudioClip wrongSFX;
    private Recorder recorder;//online
    private Speaker[] speakers;//online

    private float timer;
    private bool running;

    private int teamIndex;
    private int readerIndex;
    private bool isReader = false;
    
    private float nextTimerSync;

   void Start()
    {
        InitRoundFromRoomProps();
        Debug.Log($"[RM] Start running. activeSelf={gameObject.activeSelf} room={PhotonNetwork.InRoom}");
        var r = Object.FindAnyObjectByType<Recorder>(FindObjectsInactive.Include);
        Debug.Log($"[VOICE] recorder={(r != null)} recEnabled={(r != null && r.RecordingEnabled)} transmit={(r != null && r.TransmitEnabled)}");
    }


    int GetPlayerTeam(Photon.Realtime.Player p)
    {
       
        if (p.CustomProperties.TryGetValue("team", out object value))
            return (int)value;

        return -1;
    }

    void Update()
    {
        
        if (timerText != null &&
            PhotonNetwork.InRoom &&
            PhotonNetwork.CurrentRoom != null &&// a change
            PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("Timer", out var tv))
        {
            timerText.text = tv.ToString();
        }

       
        if (!PhotonNetwork.IsMasterClient) return;
        if (!running) return;

        timer -= Time.deltaTime;
        
        if (Time.time >= nextTimerSync)
        {
            nextTimerSync = Time.time + 0.25f;

            if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
            {
                PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
                {
                    { "Timer", Mathf.CeilToInt(timer) }
                });
            }
        }


        if (timer <= 0f)
            EndRound();
    }



    void PickReader()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var candidates = PhotonNetwork.PlayerList
        .Where(p => GetPlayerTeam(p) == teamIndex)
        .ToList();

        if (candidates.Count == 0)
        {
            Debug.LogError($"[RM] No candidates found for team {teamIndex}");
            return;
        }

        var chosen = candidates[Random.Range(0, candidates.Count)];
        Debug.Log($"[RM] ActiveTeam={teamIndex} candidates=" +
    string.Join(", ", PhotonNetwork.PlayerList.Where(p => GetPlayerTeam(p) == teamIndex)
        .Select(p => $"{p.NickName}(actor={p.ActorNumber},team={GetPlayerTeam(p)})")));

        photonView.RPC("RPC_SyncReader", RpcTarget.All, chosen.ActorNumber);
    }


    [PunRPC]
    void RPC_SyncReader(int readerActorNumber)
    {
        var reader = PhotonNetwork.CurrentRoom.GetPlayer(readerActorNumber);

        if (reader == null)
        {
            Debug.LogError("Reader not found in room!");
            return;
        }

        if (playerNameText)
            playerNameText.text = reader.NickName + " is reading";

        isReader = PhotonNetwork.LocalPlayer.ActorNumber == readerActorNumber;

        UpdateUIVisibility();

        if (isReader)
        {
            string w = GameManager.Instance.GetRandomWord();
            photonView.RPC("RPC_SetWord", RpcTarget.All, w);
        }
}



    void UpdateUIVisibility()
    {
        if (correctButton != null) correctButton.gameObject.SetActive(isReader);
        if (penaltyButton != null) penaltyButton.gameObject.SetActive(isReader);

        if (wordText == null) return;

        if (isReader)
        {
            //wordText.text = "..."; 
        }
        else
        {
            wordText.text = "GUESS!";
        }
    }

    void SetupButtons()
    {
        if (correctButton)
        {
            correctButton.onClick.RemoveAllListeners();
            correctButton.onClick.AddListener(() =>
            {
                if (!isReader) return;
                if (audioSource && correctSFX) audioSource.PlayOneShot(correctSFX);

                // ALWAYS send to Master (even if you are Master)
                photonView.RPC("RPC_RequestScoreDelta", RpcTarget.MasterClient, teamIndex, +1);

                if (GameManager.Instance.CheckWin(teamIndex))
                    photonView.RPC("RPC_GameOver", RpcTarget.All);
                else
                    photonView.RPC("RPC_NewWord", RpcTarget.All);
            });
        }

        if (penaltyButton)
        {
            penaltyButton.onClick.RemoveAllListeners();
            penaltyButton.onClick.AddListener(() =>
            {
                if (!isReader) return;
                if (audioSource && wrongSFX) audioSource.PlayOneShot(wrongSFX);

                // ALWAYS send to Master
                photonView.RPC("RPC_RequestScoreDelta", RpcTarget.MasterClient, teamIndex, -1);

                photonView.RPC("RPC_NewWord", RpcTarget.All);
            });
        }
    }


    [PunRPC]
    void RPC_SetWord(string w)
    {
        Debug.Log($"RPC Received! Word: {w}"); 
        if (wordText != null)
        {
            wordText.text = isReader ? w : "GUESS!";
        }
    }

    [PunRPC]
    void RPC_NewWord()
    {
        // Only the reader generates the new word
        if (isReader)
        {
            string w = GameManager.Instance.GetRandomWord();
            photonView.RPC("RPC_SetWord", RpcTarget.All, w);
        }
    }

    [PunRPC]
    void RPC_GameOver()
    {
        running = false;
        SetMicEnabled(false);
        SetSpeakersEnabled(false);
        if (!PhotonNetwork.IsMasterClient) return;
        if (!PhotonNetwork.IsConnectedAndReady) return;
        if (!PhotonNetwork.InRoom) return;
        if (PhotonNetwork.CurrentRoom == null) return;

        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            { "Phase", "GameOver" }
        });
    }

    void EndRound()
    {
        running = false;
        SetMicEnabled(false);
        SetSpeakersEnabled(false);
        if (!PhotonNetwork.IsMasterClient) return;
        if (!PhotonNetwork.IsConnectedAndReady) return;
        if (!PhotonNetwork.InRoom) return;
        if (PhotonNetwork.CurrentRoom == null) return;

        int next = GameManager.Instance.NextTeamIndex + 1;
        if (next >= GameManager.Instance.teams.Count)
            next = 0;

        GameManager.Instance.NextTeamIndex = next;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            { "Phase", "Scoreboard" },
            { "NextTeamIndex", next },
            { "ActiveTeam", -1 }
        });
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("ActiveTeam"))//online
            ApplyVoiceByActiveTeam((int)changedProps["ActiveTeam"]);

        if (!changedProps.ContainsKey("Phase")) return;

        string phase = (string)changedProps["Phase"];

        if (phase == "Round")
        {
            // re-init round for all clients
            enabled = true;
            InitRoundFromRoomProps();
            return;
        }

        // leaving round
        if (phase != "Round")
            running = false;
    }

    [PunRPC]
    void RPC_RequestScoreDelta(int teamIdx, int delta, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        BumpScoreTick();
        photonView.RPC("RPC_ApplyScoreDelta", RpcTarget.All, teamIdx, delta);
    }

    [PunRPC]
    void RPC_ApplyScoreDelta(int teamIdx, int delta)
    {
        ApplyScoreDelta(teamIdx, delta);
    }

    void ApplyScoreDelta(int teamIdx, int delta)
    {
        if (delta > 0) GameManager.Instance.AddCorrectPoint(teamIdx);
        else GameManager.Instance.AddPenalty(teamIdx);
    }
    void BumpScoreTick()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!PhotonNetwork.InRoom) return;

        int tick = 0;
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        if (props.TryGetValue("ScoreTick", out var t)) tick = (int)t;

        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
        {
            { "ScoreTick", tick + 1 }
        });
    }
    void InitRoundFromRoomProps()
    {
        if (!PhotonNetwork.InRoom || GameManager.Instance == null) return;

        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        if (!props.TryGetValue("Phase", out var phaseObj) || (string)phaseObj != "Round") return;
        if (!props.TryGetValue("ActiveTeam", out var activeObj)) return;

        int activeTeam = (int)activeObj;
        int myTeam = GetPlayerTeam(PhotonNetwork.LocalPlayer);

        teamIndex = activeTeam;
        ApplyVoiceByActiveTeam(activeTeam);//online

        // master runs timer + picks reader once
        if (PhotonNetwork.IsMasterClient)
        {
            timer = AliasGameSettings.RoundTimeSeconds;
            Debug.Log("RoundTimeSeconds=" + AliasGameSettings.RoundTimeSeconds);//debug
            running = true;
            //
            nextTimerSync = 0f;
            //
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
            {
                { "Timer", AliasGameSettings.RoundTimeSeconds }
                //{ "Timer", Mathf.CeilToInt(timer) }
            });
            //
            PickReader();
        }
        if (myTeam == activeTeam)
        {
            //
            if (timerText && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("Timer", out var tv))
            timerText.text = tv.ToString();
            //
            SetupButtons();        // safe now (listeners cleared)
            UpdateUIVisibility();
        }
    }
    //online-addons
    void CacheVoice()
    {
        if (recorder == null)
            recorder = Object.FindAnyObjectByType<Recorder>(FindObjectsInactive.Include);

        if (speakers == null || speakers.Length == 0)
            speakers = Object.FindObjectsByType<Speaker>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );
    }

    void SetMicEnabled(bool enabled)
    {
        CacheVoice();
        if (recorder != null) recorder.TransmitEnabled = enabled;
    }

    void SetSpeakersEnabled(bool enabled)
    {
        CacheVoice();
        if (speakers != null)
            foreach (var s in speakers)
                if (s != null) s.enabled = enabled;
    }

    void ApplyVoiceByActiveTeam(int activeTeam)
    {
        //SetVoiceEnabledForMe(true);
        // If SameRoom mode => always off
        if (GameManager.Instance == null || !GameManager.Instance.VoiceEnabled)
        {
            SetMicEnabled(false);
            SetSpeakersEnabled(false);
            return;
        }

        // Online => everyone can hear
        SetSpeakersEnabled(true);

        // Only active team can talk
        int myTeam = GetPlayerTeam(PhotonNetwork.LocalPlayer);
        bool amActiveTeam = (myTeam == activeTeam);
        SetMicEnabled(amActiveTeam);
    }

}
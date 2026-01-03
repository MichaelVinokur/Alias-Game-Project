using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.Collections.Generic;
using System.Linq;


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

    private float timer;
    private bool running;

    private int teamIndex;
    private int readerIndex;
    private bool isReader = false;
    

   void Start()
    {
        if (!PhotonNetwork.InRoom || GameManager.Instance == null)
            return;

        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        if (!props.TryGetValue("Phase", out var phaseObj) || (string)phaseObj != "Round")
            return;

        if (!props.TryGetValue("ActiveTeam", out var activeObj))
            return;

        int activeTeam = (int)activeObj;
        int myTeam = GetPlayerTeam(PhotonNetwork.LocalPlayer);

        teamIndex = activeTeam;

        // ✅ Master always runs the timer
        if (PhotonNetwork.IsMasterClient)
        {
            timer = AliasGameSettings.RoundTimeSeconds;
            running = true;

            PickReader(); // master selects reader
        }

        // ✅ Only playing team sets up UI/buttons
        if (myTeam == activeTeam)
        {
            SetupButtons();
            UpdateUIVisibility();
        }
    }


    int GetPlayerTeam(Photon.Realtime.Player p)
    {
        if (p.CustomProperties.TryGetValue("team", out object value))
            return (int)value;

        return -1;
    }

    void ApplyRoomSettings()
    {
        if (!PhotonNetwork.InRoom) return;

        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        if (props.TryGetValue("RoundTime", out var rt))
            AliasGameSettings.RoundTimeSeconds = (int)rt;

        if (props.TryGetValue("NumTeams", out var nt))
            AliasGameSettings.NumTeams = (int)nt;

        if (props.TryGetValue("Category", out var cat))
            GameManager.Instance.SetCategory((int)cat);

        if (props.TryGetValue("NextTeamIndex", out var nti))
            GameManager.Instance.NextTeamIndex = (int)nti;
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient) return;  // ✅ ONLY master counts time
        if (!running) return;

        timer -= Time.deltaTime;
        if (timerText != null)
            timerText.text = Mathf.Ceil(timer).ToString();

        if (timer <= 0f)
            EndRound();
    }

    void PickReader()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        var team = GameManager.Instance.teams[teamIndex];
        if (team.players.Count == 0) return;

        int i = Random.Range(0, team.players.Count);

        int readerActorNumber = int.Parse(team.players[i].playerId);
        photonView.RPC("RPC_SyncReader", RpcTarget.All, readerActorNumber);
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
            wordText.text = "..."; 
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
            correctButton.onClick.AddListener(() =>
            {
                if (!isReader) return; // only the reader can change the score
                if(audioSource && correctSFX) audioSource.PlayOneShot(correctSFX);//sound effect
                
                GameManager.Instance.AddCorrectPoint(teamIndex); //++

                if (GameManager.Instance.CheckWin(teamIndex))
                {
                    Debug.Log("Victory! Sending GameOver RPC...");
                    photonView.RPC("RPC_GameOver", RpcTarget.All);
                }
                else
                {
                    photonView.RPC("RPC_NewWord", RpcTarget.All);
                }
            });
        }

        if (penaltyButton)
        {
            penaltyButton.onClick.AddListener(() =>
            {
                if (!isReader) return;
                if(audioSource && wrongSFX) audioSource.PlayOneShot(wrongSFX);

                GameManager.Instance.AddPenalty(teamIndex);
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

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
            {
                { "Phase", "GameOver" }
            });
        }
    }


    void EndRound()
    {
        running = false;

        if (!PhotonNetwork.IsMasterClient)
            return;

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
        if (changedProps.ContainsKey("Phase"))
        {
            string phase = (string)changedProps["Phase"];

            // If round ended, stop the round on ALL phones
            if (phase != "Round")
            {
                running = false;
                gameObject.SetActive(false);
            }
        }
    }

}
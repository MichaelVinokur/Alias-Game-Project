using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Photon.Pun;
using System.Collections.Generic;

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
        // SAFETY CHECK: Ensure GameManager exists
        if (GameManager.Instance == null) return;

        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("Not connected to Photon! Working in offline mode.");
        }

        ApplyRoomSettings();
        if (!PhotonNetwork.InRoom) return;

        teamIndex = GameManager.Instance.NextTeamIndex;
        timer = AliasGameSettings.RoundTimeSeconds;
        running = true;

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("Master Client initialized the round.");
        }

        PickReader();
        SetupButtons();
        UpdateUIVisibility();
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
        if (!running) return;

        timer -= Time.deltaTime;
        if(timerText != null) 
            timerText.text = Mathf.Ceil(timer).ToString();

        // Optional: Simple vibration
        if (timer <= 10f && timer > 9.9f) 
            Handheld.Vibrate();

        if (timer <= 0f)
            EndRound();
    }

    void PickReader()
    {
        if (GameManager.Instance.teams == null || teamIndex >= GameManager.Instance.teams.Count) return;

        var team = GameManager.Instance.teams[teamIndex];
        if (team.players.Count == 0) return;

        if (PhotonNetwork.IsMasterClient)
        {
            int chosenIndex = Random.Range(0, team.players.Count);
            photonView.RPC("RPC_SyncReader", RpcTarget.All, chosenIndex);
        }
    }

    [PunRPC]
    void RPC_SyncReader(int indexFromServer)
    {
        readerIndex = indexFromServer;
        var team = GameManager.Instance.teams[teamIndex];
        
        var readerInfo = team.players[readerIndex];
        
        if (playerNameText) 
            playerNameText.text = $"{readerInfo.nickname} is reading";

        isReader = (PhotonNetwork.LocalPlayer.NickName == readerInfo.nickname);

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

    [PunRPC] // the function can be sent over the network
    void RPC_GameOver()
    {
        running = false;
        Debug.Log("The game has ended! Moving to Scoreboard...");
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("EndGameScene"); // transfers everybody to EndGame. importent only masterclient sends this.
        }
    }

    void EndRound()
    {
        running = false;
        if (!PhotonNetwork.IsMasterClient)
            return;
        //maybe duplicity need to check.
        if (PhotonNetwork.IsMasterClient)
        {
            int next = GameManager.Instance.NextTeamIndex + 1;
            if (next >= GameManager.Instance.teams.Count) next = 0; //modulo on team order

            GameManager.Instance.NextTeamIndex = next;

            PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable
            {
                { "NextTeamIndex", next }
            });

            PhotonNetwork.LoadLevel("ScoreboardScene");
        }
    }
}
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

        // בדיקה: האם אנחנו מחוברים לפוטון?
        if (!PhotonNetwork.IsConnected)
        {
            Debug.LogWarning("Not connected to Photon! Working in offline mode.");
        }

        ApplyRoomSettings();
        if (!PhotonNetwork.InRoom) return;

        teamIndex = GameManager.Instance.NextTeamIndex;
        timer = AliasGameSettings.RoundTimeSeconds;
        running = true;

        // רק ה-Master Client מוודא שהמשחק מתחיל לספור
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
        // 1. Validate Team Data
        if (GameManager.Instance.teams == null || GameManager.Instance.teams.Count == 0)
        {
            if(playerNameText) playerNameText.text = "No Teams Found!";
            return;
        }

        // 2. Validate Index
        if (teamIndex >= GameManager.Instance.teams.Count)
        {
            teamIndex = 0; // Reset if out of bounds
        }

        var team = GameManager.Instance.teams[teamIndex];

        // 3. Validate Players in Team
        if (team.players.Count == 0)
        {
            if(playerNameText) playerNameText.text = "Team has no players!";
            return;
        }

        // 4. Pick Reader
        readerIndex = Random.Range(0, team.players.Count);
        var reader = team.players[readerIndex];

        if(playerNameText) 
            playerNameText.text = $"{reader.nickname} is reading";

        // 5. Determine if Local Player is Reader
        // We use ID if possible, otherwise Nickname
        if (PhotonNetwork.LocalPlayer != null)
        {
            isReader = (PhotonNetwork.LocalPlayer.NickName == reader.nickname);
        }

        // 6. Get Word (Only Reader triggers this)
        if (isReader)
        {
            string w = GameManager.Instance.GetRandomWord();
            // This is the line that crashed before if PhotonView was missing
            photonView.RPC("RPC_SetWord", RpcTarget.All, w);
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

    void UpdateUIVisibility()
    {
        if (wordText == null) return;

        if (isReader)
        {
            wordText.text = "..."; // Placeholder until RPC arrives
        }
        else
        {
            wordText.text = "???"; // Non-readers see this
        }
    }

    [PunRPC]
    void RPC_SetWord(string w)
    {
        Debug.Log($"RPC Received! Word: {w}"); // זה יגיד לנו אם הרשת עובדת
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
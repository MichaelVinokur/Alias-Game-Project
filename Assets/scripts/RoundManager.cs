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

        // סנכרון הקטגוריה לכל השחקנים בחדר
        int selectedIdx = GameManager.Instance.state.selectedCategoryIndex;
        GameManager.Instance.SetCategory(selectedIdx);

        teamIndex = GameManager.Instance.NextTeamIndex;
        timer = GameManager.Instance.RoundSeconds;
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
                if (!isReader) return;
                if(audioSource && correctSFX) audioSource.PlayOneShot(correctSFX);
                
                GameManager.Instance.AddCorrectPoint(teamIndex);
                photonView.RPC("RPC_NewWord", RpcTarget.All);
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

    void EndRound()
    {
        running = false;

        // Move to next team for the NEXT round
        GameManager.Instance.NextTeamIndex++;
        if (GameManager.Instance.NextTeamIndex >= GameManager.Instance.teams.Count)
            GameManager.Instance.NextTeamIndex = 0;

        // Load Scoreboard
        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel("ScoreboardScene");
        }
    }
}
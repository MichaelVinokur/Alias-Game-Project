using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using ExitGames.Client.Photon;
using System.Linq;
using System.Collections;

public class ScoreboardManager : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public Transform teamsListContainer;
    public GameObject teamCardPrefab;

    public TMP_Text nextTeamLabel;
    public Button startRoundButton;

    void Awake()
    {
        Debug.Log("[SBM] Awake");
    }

    void Start()
    {
        Debug.Log($"[SBM] Start activeSelf={gameObject.activeSelf} inRoom={PhotonNetwork.InRoom} isMaster={PhotonNetwork.IsMasterClient}");

        if (startRoundButton)
            startRoundButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        // Ensure teams exist
        if (GameManager.Instance != null && (GameManager.Instance.teams == null || GameManager.Instance.teams.Count == 0))
        {
            GameManager.Instance.InitializeTeams(AliasGameSettings.NumTeams);
        }

        if (GameManager.Instance != null && GameManager.Instance.teams != null && GameManager.Instance.teams.Count > 0)
        {
            if (GameManager.Instance.NextTeamIndex >= GameManager.Instance.teams.Count)
                GameManager.Instance.NextTeamIndex = 0;
        }

        BuildScoreboardUI();
        SetupStartButton();
        UpdateScoreboardTextFromRoom();
    }

    public override void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable changedProps)
    {
        // Print what changed + current full state
        Debug.Log("[SBM] OnRoomPropertiesUpdate changedKeys=" +
                  string.Join(",", changedProps.Keys.Cast<object>().Select(k => k.ToString())));

        // Update UI when relevant keys change
        if (changedProps.ContainsKey("Phase") ||
            changedProps.ContainsKey("ActiveTeam") ||
            changedProps.ContainsKey("NextTeamIndex"))
        {
            // If round ended and we returned to scoreboard, rebuild scores
            if (changedProps.TryGetValue("Phase", out var ph) && (string)ph == "Scoreboard")
                BuildScoreboardUI();

            UpdateScoreboardTextFromRoom();
        }
    }

    void SetupStartButton()
    {
        if (!startRoundButton) return;

        startRoundButton.onClick.RemoveAllListeners();
        startRoundButton.onClick.AddListener(() =>
        {
            if (!PhotonNetwork.IsMasterClient) return;
            if (!PhotonNetwork.InRoom) return;

            int teamToPlay = GameManager.Instance.NextTeamIndex;

            var ht = new ExitGames.Client.Photon.Hashtable
            {
                { "Phase", "Round" },
                { "ActiveTeam", teamToPlay }
            };

            bool ok = PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
        });
    }


    void UpdateScoreboardTextFromRoom()
    {
        if (!PhotonNetwork.InRoom) return;
        if (GameManager.Instance == null || GameManager.Instance.teams == null || GameManager.Instance.teams.Count == 0) return;

        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        string phase = props.TryGetValue("Phase", out var p) ? (string)p : "Scoreboard";
        int activeTeam = props.TryGetValue("ActiveTeam", out var a) ? (int)a : -1;

        if (props.TryGetValue("NextTeamIndex", out var nti))
            GameManager.Instance.NextTeamIndex = (int)nti;

        int next = GameManager.Instance.NextTeamIndex;
        string nextName = GameManager.Instance.teams[next].teamName;

        if (phase == "Round" && activeTeam >= 0 && activeTeam < GameManager.Instance.teams.Count)
        {
            nextTeamLabel.text = "Currently playing: " + GameManager.Instance.teams[activeTeam].teamName;
        }
        else
        {
            nextTeamLabel.text = "Next Team: " + nextName;
        }

        if (PhotonNetwork.IsMasterClient && startRoundButton)
        {
            startRoundButton.transform.GetChild(0).GetComponent<TMP_Text>().text =
                "Start Round: " + nextName;
        }
    }

    void BuildScoreboardUI()
    {
        if (teamsListContainer == null || teamCardPrefab == null)
        {
            Debug.LogWarning("[SBM] Missing teamsListContainer or teamCardPrefab");
            return;
        }

        if (GameManager.Instance == null || GameManager.Instance.teams == null)
        {
            Debug.LogWarning("[SBM] GameManager teams not ready");
            return;
        }

        Debug.Log($"[SBM] BuildScoreboardUI teams={GameManager.Instance.teams.Count} before={teamsListContainer.childCount}");

        foreach (Transform t in teamsListContainer)
            Destroy(t.gameObject);

        foreach (var team in GameManager.Instance.teams)
        {
            GameObject card = Instantiate(teamCardPrefab, teamsListContainer);

            var name = card.transform.Find("TeamName")?.GetComponent<TMP_Text>();
            var score = card.transform.Find("ScoreText")?.GetComponent<TMP_Text>();

            if (name) name.text = team.teamName;
            if (score) score.text = "Score: " + team.score;
        }

        Debug.Log($"[SBM] BuildScoreboardUI after={teamsListContainer.childCount}");
    }

}

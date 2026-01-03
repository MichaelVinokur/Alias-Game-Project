using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using ExitGames.Client.Photon;

public class ScoreboardManager : MonoBehaviourPunCallbacks
{
    public GameObject scoreboardPanel;
    public GameObject roundPanel;

    public Transform teamsListContainer;
    public GameObject teamCardPrefab;

    public TMP_Text nextTeamLabel;
    public Button startRoundButton;

    private void Start()
    {
        if (startRoundButton)
            startRoundButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);

        // SAFETY: ensure teams exist
        if (GameManager.Instance.teams == null || GameManager.Instance.teams.Count == 0)
        {
            GameManager.Instance.InitializeTeams(AliasGameSettings.NumTeams);
        }

        if (GameManager.Instance.NextTeamIndex >= GameManager.Instance.teams.Count)
            GameManager.Instance.NextTeamIndex = 0;

        BuildScoreboardUI();
        SetupStartButton();
        RefreshPanels();
        UpdateStartButtonText();

    }

    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // Always update panels (Scoreboard ↔ Round)
        RefreshPanels();

        // If we returned to scoreboard, rebuild scores & players
        if (propertiesThatChanged.ContainsKey("Phase") &&
            (string)propertiesThatChanged["Phase"] == "Scoreboard")
        {
            BuildScoreboardUI();
             UpdateStartButtonText();
        }

        // If next team changed, update labels & button text
        if (propertiesThatChanged.ContainsKey("NextTeamIndex"))
        {
            int next = (int)propertiesThatChanged["NextTeamIndex"];

            nextTeamLabel.text =
                "Next Team: " + GameManager.Instance.teams[next].teamName;

            if (PhotonNetwork.IsMasterClient && startRoundButton != null)
            {
                startRoundButton.transform.GetChild(0)
                    .GetComponent<TMP_Text>().text =
                    "Start Round: " + GameManager.Instance.teams[next].teamName;
            }
        }
    }


    void RefreshPanels()
    {
        string phase = "Scoreboard";
        int activeTeam = GameManager.Instance.NextTeamIndex;

        if (PhotonNetwork.InRoom)
        {
            var props = PhotonNetwork.CurrentRoom.CustomProperties;
            if (props.TryGetValue("Phase", out var p)) phase = (string)p;
            if (props.TryGetValue("ActiveTeam", out var t)) activeTeam = (int)t;
            if (props.TryGetValue("NextTeamIndex", out var nti))
                GameManager.Instance.NextTeamIndex = (int)nti;
        }

        int myTeam = GetPlayerTeam(PhotonNetwork.LocalPlayer);

        bool iAmPlaying = (phase == "Round" && myTeam == activeTeam);

        // Only the playing team sees the round panel
        scoreboardPanel.SetActive(!iAmPlaying);
        roundPanel.SetActive(iAmPlaying);

        if (nextTeamLabel)
        {
            nextTeamLabel.text =
                phase == "Scoreboard"
                ? "Next Team: " + GameManager.Instance.teams[GameManager.Instance.NextTeamIndex].teamName
                : "Currently playing: " + GameManager.Instance.teams[activeTeam].teamName;
        }
    }


    void UpdateStartButtonText()
    {
        if (!startRoundButton || !PhotonNetwork.IsMasterClient) return;

        startRoundButton.transform.GetChild(0)
            .GetComponent<TMP_Text>().text =
            "Start Round: " +
            GameManager.Instance.teams[GameManager.Instance.NextTeamIndex].teamName;
    }


    void SetupStartButton()
    {
        if (!startRoundButton) return;

        startRoundButton.onClick.RemoveAllListeners();
        startRoundButton.onClick.AddListener(() =>
        {
            if (!PhotonNetwork.IsMasterClient) return;

            int teamToPlay = GameManager.Instance.NextTeamIndex;

            PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable
            {
                { "Phase", "Round" },
                { "ActiveTeam", teamToPlay }
            });
        });
    }

    int GetPlayerTeam(Photon.Realtime.Player p)
    {
        if (p.CustomProperties.TryGetValue("team", out object value))
            return (int)value;
        return -1;
    }

    void BuildScoreboardUI()
    {
        foreach (Transform t in teamsListContainer)
            Destroy(t.gameObject);

        foreach (var team in GameManager.Instance.teams)
        {
            GameObject card = Instantiate(teamCardPrefab, teamsListContainer);
            card.transform.Find("TeamName").GetComponent<TMP_Text>().text = team.teamName;
            card.transform.Find("ScoreText").GetComponent<TMP_Text>().text = "Score: " + team.score;
        }
    }
}

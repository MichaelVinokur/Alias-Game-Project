using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ScoreboardManager : MonoBehaviour
{
    public Transform teamsListContainer;
    public GameObject teamCardPrefab;

    public TMP_Text nextTeamLabel;
    public Button startRoundButton;

    private void Start()
    {
        // 1. SAFETY FIX: If no teams exist, create them
        if (GameManager.Instance.teams == null || GameManager.Instance.teams.Count == 0)
        {
            GameManager.Instance.InitializeTeams(AliasGameSettings.NumTeams);
            
            /// ADD THIS: Add fake players for testing
            // We use 'new PlayerInfo(1, "Name")' because your script requires an ID first.
            foreach(var team in GameManager.Instance.teams) 
            {
                team.players.Add(new PlayerInfo("1", "TestPlayer 1")); 
                team.players.Add(new PlayerInfo("2", "TestPlayer 2"));
            }
        }

        // 2. Clamp index
        if (GameManager.Instance.NextTeamIndex >= GameManager.Instance.teams.Count)
        {
            GameManager.Instance.NextTeamIndex = 0;
        }

        BuildScoreboardUI();
        SetupStartButton();
    }


    private void BuildScoreboardUI()
    {
        // Clear old
        foreach (Transform t in teamsListContainer)
            Destroy(t.gameObject);

        // Create UI cards for each team
        for (int i = 0; i < GameManager.Instance.teams.Count; i++)
        {
            TeamInfo team = GameManager.Instance.teams[i];

            GameObject card = Instantiate(teamCardPrefab, teamsListContainer);

            TMP_Text nameTxt = card.transform.Find("TeamName").GetComponent<TMP_Text>();
            TMP_Text scoreTxt = card.transform.Find("ScoreText").GetComponent<TMP_Text>();
            Transform playersList = card.transform.Find("PlayersList");

            nameTxt.text = team.teamName;
            scoreTxt.text = "Score: " + team.score;

            // Show players
            foreach (var player in team.players)
            {
                
                GameObject pObj = new GameObject("PlayerName", typeof(TextMeshProUGUI));
                TMP_Text pt = pObj.GetComponent<TMP_Text>();
                pt.text = player.nickname;
                pt.fontSize = 28;
                pt.color = Color.white;
                pObj.transform.SetParent(playersList, false);
            }
        }
    }

    private void SetupStartButton()
{
    int nextIndex = GameManager.Instance.NextTeamIndex;

    // SAFETY FIX
    if (nextIndex < 0 || nextIndex >= GameManager.Instance.teams.Count)
        nextIndex = 0;

    nextTeamLabel.text = "Next Team: " + GameManager.Instance.teams[nextIndex].teamName;

    startRoundButton.transform.GetChild(0)
        .GetComponent<TMP_Text>().text = "Start Round: " + GameManager.Instance.teams[nextIndex].teamName;

    startRoundButton.onClick.AddListener(() =>
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("RoundScene");
    });
}

}

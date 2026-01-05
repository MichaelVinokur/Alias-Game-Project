using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Photon.Pun;

public class EndGameScript : MonoBehaviourPunCallbacks
{
    public TMP_Text winnersText;
    public Transform scoreTableContent;
    public GameObject scoreRowPrefab;

    void Start()
    {
        ShowResults();
    }

    void ShowResults()
    {
        if (GameManager.Instance == null) return;

        int winningTeamIndex = -1;
        int highestScore = -1;

        //cleaning the table if already full
        foreach (Transform child in scoreTableContent)
        {
            Destroy(child.gameObject);
        }

        foreach (var team in GameManager.Instance.teams)
        {
            Debug.Log($"{team.teamName}: {team.score} points");
            //saving team number and score for winning team
            if (team.score > highestScore)
            {
                highestScore = team.score; 
                winningTeamIndex = GameManager.Instance.teams.IndexOf(team);
            }
            GameObject row = Instantiate(scoreRowPrefab, scoreTableContent);
            TMP_Text rowText = row.GetComponentInChildren<TMP_Text>();
            if (rowText != null)
            {
                rowText.text = $"{team.teamName}: {team.score} Points";
            }
        }
        if (winningTeamIndex != -1)
        {
            winnersText.text = $"The winners are: {GameManager.Instance.teams[winningTeamIndex].teamName}";
        }
        
    }

    public void OnReturnToMainMenu()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
            return; // wait for callback
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public override void OnLeftRoom()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }
}

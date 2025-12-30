using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class TeamColumnUI : MonoBehaviour
{
    public TMP_Text teamNameText;
    public TMP_Text playersCountText;   // can be null
    public Transform playersList;
    public Button joinButton;
    private int teamIndex;
    private SameRoomLobbyManager lobby;

    public void Init(int index, SameRoomLobbyManager lobbyManager)
    {
        teamIndex = index;
        lobby = lobbyManager;

        if (teamNameText != null)
            teamNameText.text = $"Team {index + 1}";

        joinButton.onClick.RemoveAllListeners();//Clears any old click actions
        joinButton.onClick.AddListener(() => lobby.OnLocalPlayerChooseTeam(teamIndex));//When the button is clicked, call your lobby method and pass this column’s teamIndex
    }

    public void SetCount(int count)
    {
        if (playersCountText != null)
            playersCountText.text = count.ToString();
    }
}

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class LobbyPlayerRow : MonoBehaviour
{
    public TMP_Text playerNameText;
    public Button[] teamButtons;   // size 4, assign in Inspector

    private Player player;         // Photon player this row represents
    private SameRoomLobbyManager lobbyManager;

    public void Init(Player p, SameRoomLobbyManager manager, int numTeams)
    {
        player = p;
        lobbyManager = manager;

        if (playerNameText != null)
            playerNameText.text = p.NickName;

        // Enable only used team columns
        for (int i = 0; i < teamButtons.Length; i++)
        {
            bool active = (i < numTeams);
            if (teamButtons[i] != null)
            {
                teamButtons[i].gameObject.SetActive(active);
                teamButtons[i].interactable = (p == PhotonNetwork.LocalPlayer) && active;
            }
        }

        UpdateTeamVisual();
    }

    public void UpdateTeamVisual()
    {
        // Get this player's selected team from custom properties (if any)
        int team = lobbyManager.GetPlayerTeam(player);

        for (int i = 0; i < teamButtons.Length; i++)
        {
            if (teamButtons[i] == null)
                continue;

            ColorBlock colors = teamButtons[i].colors;

            if (i == team)   // selected team for this player
            {
                colors.normalColor = new Color(0.3f, 0.8f, 0.3f);  // greenish
            }
            else
            {
                colors.normalColor = Color.white;
            }

            teamButtons[i].colors = colors;
        }
    }

    // These four are wired from the Inspector
    public void OnTeam1() { OnChooseTeam(0); }
    public void OnTeam2() { OnChooseTeam(1); }
    public void OnTeam3() { OnChooseTeam(2); }
    public void OnTeam4() { OnChooseTeam(3); }

    private void OnChooseTeam(int teamIndex)
    {
        if (player != PhotonNetwork.LocalPlayer)
            return;  // only local player can change their own team

        lobbyManager.OnLocalPlayerChooseTeam(teamIndex);
    }
}

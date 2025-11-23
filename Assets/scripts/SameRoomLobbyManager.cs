using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ExitGames.Client.Photon;

public class SameRoomLobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI - Lobby")]
    public TMP_Text roomCodeText;

    [Header("UI - Settings Window")]
    public GameObject settingsPanel;
    public TMP_Dropdown roundTimeDropdown;
    public TMP_Dropdown teamsDropdown;
    public TMP_Text settingsSummaryText;

    [Header("UI - Players Table")]
    public Transform playersTableContent;   // Content under PlayersTable/Viewport
    public GameObject playerRowPrefab;      // PlayerRow prefab

    // ===== internal state =====
    private Dictionary<Player, LobbyPlayerRow> playerRows =
        new Dictionary<Player, LobbyPlayerRow>();

    private const string TEAM_KEY = "team";

    void Start()
    {
        // Show room code
        if (PhotonNetwork.InRoom && roomCodeText != null)
        {
            roomCodeText.text = "Code: " + PhotonNetwork.CurrentRoom.Name;
        }

        // Settings panel starts hidden
        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Init dropdowns + summary
        InitDropdownValues();
        UpdateSettingsSummaryLabel();

        // Build players table for current players
        RefreshPlayersTable();
    }

    // ---------- SETTINGS ----------

    void InitDropdownValues()
    {
        if (roundTimeDropdown != null)
        {
            for (int i = 0; i < roundTimeDropdown.options.Count; i++)
            {
                if (int.TryParse(roundTimeDropdown.options[i].text, out int value) &&
                    value == AliasGameSettings.RoundTimeSeconds)
                {
                    roundTimeDropdown.value = i;
                    break;
                }
            }
        }

        if (teamsDropdown != null)
        {
            for (int i = 0; i < teamsDropdown.options.Count; i++)
            {
                if (int.TryParse(teamsDropdown.options[i].text, out int value) &&
                    value == AliasGameSettings.NumTeams)
                {
                    teamsDropdown.value = i;
                    break;
                }
            }
        }
    }

    void UpdateSettingsSummaryLabel()
    {
        if (settingsSummaryText == null)
            return;

        settingsSummaryText.text =
            $"Round: {AliasGameSettings.RoundTimeSeconds}s   •   Teams: {AliasGameSettings.NumTeams}";
    }

    public void OnSettingsButton()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(true);
    }

    public void OnCloseSettingsButton()
    {
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    public void OnApplySettingsButton()
    {
        int newRoundTime = AliasGameSettings.RoundTimeSeconds;
        int newNumTeams = AliasGameSettings.NumTeams;

        if (roundTimeDropdown != null)
        {
            string optionText = roundTimeDropdown.options[roundTimeDropdown.value].text;
            int.TryParse(optionText, out newRoundTime);
        }

        if (teamsDropdown != null)
        {
            string optionText = teamsDropdown.options[teamsDropdown.value].text;
            int.TryParse(optionText, out newNumTeams);
        }

        AliasGameSettings.RoundTimeSeconds = newRoundTime;
        AliasGameSettings.NumTeams = newNumTeams;

        Debug.Log($"Settings applied: {newRoundTime}s round, {newNumTeams} teams");

        UpdateSettingsSummaryLabel();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);

        // Rebuild table to match new team count (show/hide columns)
        RefreshPlayersTable();
    }

    // ---------- PLAYERS TABLE ----------

    void ClearPlayersTable()
    {
        if (playersTableContent == null)
            return;

        foreach (Transform child in playersTableContent)
        {
            Destroy(child.gameObject);
        }

        playerRows.Clear();
    }

    void RefreshPlayersTable()
    {
        if (playersTableContent == null || playerRowPrefab == null)
            return;

        ClearPlayersTable();

        int numTeams = AliasGameSettings.NumTeams;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            GameObject rowObj = Object.Instantiate(playerRowPrefab, playersTableContent);
            LobbyPlayerRow row = rowObj.GetComponent<LobbyPlayerRow>();
            row.Init(p, this, numTeams);
            playerRows[p] = row;
        }
    }

    // Called by LobbyPlayerRow to know which team to highlight
    public int GetPlayerTeam(Player p)
    {
        if (p.CustomProperties.TryGetValue(TEAM_KEY, out object value))
        {
            if (value is int teamIndex)
                return teamIndex;
        }
        return -1; // no team
    }

    // Called by LobbyPlayerRow when *local* player clicks a team button
    public void OnLocalPlayerChooseTeam(int teamIndex)
    {
        Hashtable props = new Hashtable();
        props[TEAM_KEY] = teamIndex;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    // ---------- PHOTON CALLBACKS ----------

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RefreshPlayersTable();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RefreshPlayersTable();
    }

    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // When someone's team changes, update their row
        if (playerRows.TryGetValue(targetPlayer, out LobbyPlayerRow row))
        {
            row.UpdateTeamVisual();
        }
    }
}

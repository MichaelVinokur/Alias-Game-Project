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
    
    // Category Dropdown
    public TMP_Dropdown categoryDropdown;

    [Header("UI - Players Table")]
    public Transform playersTableContent;
    public GameObject playerRowPrefab;

    [Header("UI - Start Game Button")]
    public GameObject startGameButton;   // assign in inspector

    private Dictionary<Player, LobbyPlayerRow> playerRows =
        new Dictionary<Player, LobbyPlayerRow>();

    private const string TEAM_KEY = "team";

    // STATIC LOCK: This protects against duplicate scripts or double-clicks
    private static bool isGameStarting = false;

    void Start()
    {
        // RESET LOCK: Ensure we can start a new game when we enter the lobby
        isGameStarting = false;

        if (roomCodeText != null && PhotonNetwork.CurrentRoom != null)
        {
            // This gets the real Room Name (like "829202") from Photon
            roomCodeText.text = "Code: " + PhotonNetwork.CurrentRoom.Name;
        }
        
        // 1. Fill dropdown with category names
        if (categoryDropdown != null && GameManager.Instance != null)
        {
            categoryDropdown.ClearOptions();

            List<string> names = new List<string>();
            if (GameManager.Instance.categories != null)
            {
                foreach (var c in GameManager.Instance.categories)
                    names.Add(c.categoryName);
            }

            categoryDropdown.AddOptions(names);

            // 2. Set saved selection
            if (GameManager.Instance.state != null)
                categoryDropdown.value = GameManager.Instance.state.selectedCategoryIndex;

            // 3. Listen for changes
            categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);
        }

        InitDropdownValues();
        UpdateSettingsSummaryLabel();
        RefreshPlayersTable();
        
        // Setup Start Button - Only adds listener if not already set in Inspector
        if(startGameButton != null)
        {
             // Remove all previous listeners to be safe (prevents double clicks)
             startGameButton.GetComponent<UnityEngine.UI.Button>().onClick.RemoveAllListeners();
             startGameButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(StartGame);
        }
    }

    void OnCategoryChanged(int index)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.state.selectedCategoryIndex = index;
            GameManager.Instance.SetCategory(index);
        }
    }


    // ---------- SETTINGS ----------

    void InitDropdownValues()
    {
        if (roundTimeDropdown != null)
        {
            for (int i = 0; i < roundTimeDropdown.options.Count; i++)
            {
                if (int.TryParse(roundTimeDropdown.options[i].text, out int v) &&
                    v == AliasGameSettings.RoundTimeSeconds)
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
                if (int.TryParse(teamsDropdown.options[i].text, out int v) &&
                    v == AliasGameSettings.NumTeams)
                {
                    teamsDropdown.value = i;
                    break;
                }
            }
        }
    }

    // Show Category Name
    void UpdateSettingsSummaryLabel()
    {
        if (settingsSummaryText != null)
        {
            string categoryName = "None";
            if (GameManager.Instance != null && GameManager.Instance.currentCategory != null)
            {
                categoryName = GameManager.Instance.currentCategory.categoryName;
            }

            settingsSummaryText.text =
                $"Round: {AliasGameSettings.RoundTimeSeconds}s   •   Teams: {AliasGameSettings.NumTeams}\nCategory: {categoryName}";
        }
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

    // Save Settings
    public void OnApplySettingsButton()
    {
        int newRoundTime = AliasGameSettings.RoundTimeSeconds;
        int newNumTeams = AliasGameSettings.NumTeams;

        if (roundTimeDropdown != null)
            int.TryParse(roundTimeDropdown.options[roundTimeDropdown.value].text, out newRoundTime);

        if (teamsDropdown != null)
            int.TryParse(teamsDropdown.options[teamsDropdown.value].text, out newNumTeams);

        if (categoryDropdown != null && GameManager.Instance != null)
        {
            int selectedIndex = categoryDropdown.value;
            GameManager.Instance.SetCategory(selectedIndex);
        }

        AliasGameSettings.RoundTimeSeconds = newRoundTime;
        AliasGameSettings.NumTeams = newNumTeams;

        Debug.Log($"Settings applied! Time: {newRoundTime}, Teams: {newNumTeams}, Category Index: {categoryDropdown?.value}");

        UpdateSettingsSummaryLabel();
        RefreshPlayersTable();

        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }

    // ---------- PLAYERS TABLE ----------

    void ClearPlayersTable()
    {
        foreach (Transform t in playersTableContent)
            Destroy(t.gameObject);

        playerRows.Clear();
    }

    void RefreshPlayersTable()
    {
        ClearPlayersTable();

        int numTeams = AliasGameSettings.NumTeams;

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            GameObject rowObj = Instantiate(playerRowPrefab, playersTableContent);
            LobbyPlayerRow row = rowObj.GetComponent<LobbyPlayerRow>();
            row.Init(p, this, numTeams);
            playerRows[p] = row;
        }
    }

    public int GetPlayerTeam(Player p)
    {
        if (p.CustomProperties.TryGetValue(TEAM_KEY, out object value))
        {
            if (value is int idx)
                return idx;
        }
        return -1;
    }

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
        if (playerRows.TryGetValue(targetPlayer, out LobbyPlayerRow row))
            row.UpdateTeamVisual();
    }

    // ---------- START GAME (STATIC LOCK VERSION) ----------

    private void StartGame()
    {
        // 1. Safety Checks
        if (!PhotonNetwork.IsMasterClient) return;
        
        // GLOBAL LOCK: If ANY script has started the game, stop.
        if (isGameStarting) 
        {
             Debug.LogWarning("StartGame blocked: Game is already starting.");
             return;
        }

        // 2. Lock it immediately
        isGameStarting = true;
        Debug.Log("Starting Game...");

        if (startGameButton != null) 
            startGameButton.GetComponent<UnityEngine.UI.Button>().interactable = false;

        // 3. Create GameManager teams
        GameManager.Instance.InitializeTeams(AliasGameSettings.NumTeams);
        
        // 4. Assign players to teams
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            int teamIndex = GetPlayerTeam(p);
            if (teamIndex < 0) teamIndex = 0; 

            if (teamIndex < GameManager.Instance.teams.Count)
            {
                TeamInfo t = GameManager.Instance.teams[teamIndex];
                PlayerInfo info = new PlayerInfo(p.UserId, p.NickName);
                t.players.Add(info);
            }
        }
        
        // 5. Reset round state
        GameManager.Instance.state.currentTeamIndex = 0;
        GameManager.Instance.state.nextTeamIndex = 0;

        // 6. Load scoreboard (ONCE)
        PhotonNetwork.LoadLevel("ScoreboardScene");
    }
}
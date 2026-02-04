using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using ExitGames.Client.Photon;
using Photon.Pun.UtilityScripts;

public class SameRoomLobbyManager : MonoBehaviourPunCallbacks
{
    [Header("UI - Lobby")]
    public TMP_Text roomCodeText;

    [Header("UI - Settings Window")]
    public GameObject settingsButton;
    public GameObject settingsPanel;
    public TMP_Dropdown roundTimeDropdown;
    public TMP_Dropdown teamsDropdown;
    public TMP_Dropdown scoreDropdown;
    public TMP_Text settingsSummaryText;
    
    public TMP_Dropdown categoryDropdown;
    public TMP_Dropdown difficultyDropdown;

    [Header("UI - Players Table")]
    public Transform teamTable;          
    public GameObject teamColumnPrefab;  
    public GameObject playerNamePrefab;
    private List<TeamColumnUI> teamColumns = new List<TeamColumnUI>();
    private const string TEAM_KEY = "team";

    [Header("UI - Start Game Button")]
    public GameObject startGameButton;   
    private static bool isGameStarting = false;

    void Start()
    {
        isGameStarting = false;
        
        // 1. בדיקה האם נכנסנו מאוחר והמשחק כבר רץ
        CheckIfGameAlreadyStarted();

        // 2. הגדרת זמן חסד לניתוק
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.PlayerTtl = 30000; // 30 seconds
        }

        if (GameManager.Instance != null)
            GameManager.Instance.ForceDisableVoiceEverywhere();

        if (roomCodeText != null && PhotonNetwork.CurrentRoom != null)
            roomCodeText.text = "Code: " + PhotonNetwork.CurrentRoom.Name;
        
        // אתחול דרופדאונים (קטגוריה וקושי)
        InitCategoryAndDifficulty();

        // שחזור הגדרות מהחדר
        SyncSettingsFromRoom();

        UpdateSettingsSummaryLabel();
        RefreshPlayersTable();
        
        if(startGameButton != null)
        {
             startGameButton.GetComponent<UnityEngine.UI.Button>().onClick.RemoveAllListeners();
             startGameButton.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(StartGame);
        }

        if (PhotonNetwork.IsMasterClient)
        {
            // 1. עדכון ה-GameManager המקומי מיד לפי מה שכתוב ב-Dropdown
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetCategory(categoryDropdown.value);
                GameManager.Instance.SetDifficulty(difficultyDropdown.value);
            }

            // 2. בדיקה האם החדר "ריק" מהגדרות. אם כן - נדחוף את ברירת המחדל לענן
            var props = PhotonNetwork.CurrentRoom.CustomProperties;
            if (!props.ContainsKey("Category") || !props.ContainsKey("Difficulty"))
            {
                Hashtable initialProps = new Hashtable
                {
                    { "RoundTime", AliasGameSettings.RoundTimeSeconds },
                    { "NumTeams", AliasGameSettings.NumTeams },
                    { "Score", AliasGameSettings.Score },
                    { "Category", categoryDropdown.value },      // לוקח את הערך הראשון (0)
                    { "Difficulty", difficultyDropdown.value }   // לוקח את הערך הראשון (Easy)
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(initialProps);
            }
        }

        UpdateHostUI();
    }

    // פונקציה חדשה וחשובה!
    void CheckIfGameAlreadyStarted()
    {
        if (PhotonNetwork.CurrentRoom != null && PhotonNetwork.CurrentRoom.CustomProperties.ContainsKey("GameStarted"))
        {
            bool started = (bool)PhotonNetwork.CurrentRoom.CustomProperties["GameStarted"];
            if (started)
            {
                Debug.Log("Game already started! Joining mid-game...");
                // מעבר מיידי למשחק
                PhotonNetwork.LoadLevel("GameScene");
            }
        }
    }

    void InitCategoryAndDifficulty()
    {
        if (categoryDropdown != null && GameManager.Instance != null)
        {
            categoryDropdown.ClearOptions();
            List<string> names = new List<string>();
            if (GameManager.Instance.categories != null)
            {
                foreach (var c in GameManager.Instance.categories) names.Add(c.categoryName);
            }
            categoryDropdown.AddOptions(names);
            categoryDropdown.value = GameManager.Instance.state.selectedCategoryIndex;
            categoryDropdown.onValueChanged.AddListener(OnCategoryChanged);
        }

        if (difficultyDropdown != null && GameManager.Instance != null)
        {
            difficultyDropdown.ClearOptions();
            difficultyDropdown.AddOptions(new List<string> { "Easy", "Medium", "Hard" });
            difficultyDropdown.value = (int)GameManager.Instance.selectedDifficulty;
            difficultyDropdown.onValueChanged.AddListener(idx => {
                if(PhotonNetwork.IsMasterClient) GameManager.Instance.SetDifficulty(idx);
            });
        }
    }

    void SyncSettingsFromRoom()
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        if (props.ContainsKey("RoundTime")) AliasGameSettings.RoundTimeSeconds = (int)props["RoundTime"];
        if (props.ContainsKey("NumTeams")) AliasGameSettings.NumTeams = (int)props["NumTeams"];
        if (props.ContainsKey("Score")) AliasGameSettings.Score = (int)props["Score"];
        if (props.ContainsKey("Category")) GameManager.Instance.SetCategory((int)props["Category"]);
        if (props.ContainsKey("Difficulty")) GameManager.Instance.SetDifficulty((int)props["Difficulty"]);
        
        InitDropdownValues();
    }

    // שאר הקוד נשאר די דומה, רק מוודאים סנכרון
    void UpdateHostUI()
    {
        bool isHost = PhotonNetwork.IsMasterClient;
        if (startGameButton != null) startGameButton.SetActive(isHost);
        if (settingsButton != null) settingsButton.SetActive(isHost);
        if (categoryDropdown) categoryDropdown.interactable = isHost;
        if (difficultyDropdown) difficultyDropdown.interactable = isHost;
        if (roundTimeDropdown) roundTimeDropdown.interactable = isHost;
        if (teamsDropdown) teamsDropdown.interactable = isHost;
        if (scoreDropdown) scoreDropdown.interactable = isHost;
    }

    void OnCategoryChanged(int index)
    {
        if (PhotonNetwork.IsMasterClient && GameManager.Instance != null)
        {
            GameManager.Instance.state.selectedCategoryIndex = index;
            GameManager.Instance.SetCategory(index);
        }
    }

    void InitDropdownValues()
    {
        if (roundTimeDropdown != null) SetDropdownValue(roundTimeDropdown, AliasGameSettings.RoundTimeSeconds);
        if (teamsDropdown != null) SetDropdownValue(teamsDropdown, AliasGameSettings.NumTeams);
        if (scoreDropdown != null) SetDropdownValue(scoreDropdown, AliasGameSettings.Score);
    }

    void SetDropdownValue(TMP_Dropdown dd, int val)
    {
        for (int i = 0; i < dd.options.Count; i++)
        {
            if (int.TryParse(dd.options[i].text, out int v) && v == val)
            {
                dd.value = i; return;
            }
        }
    }

    void UpdateSettingsSummaryLabel()
    {
        if (settingsSummaryText != null)
        {
            string categoryName = "None";
            if (GameManager.Instance != null && GameManager.Instance.currentCategory != null)
                categoryName = GameManager.Instance.currentCategory.categoryName;
            
            string difficultyStr = GameManager.Instance.selectedDifficulty.ToString();

            settingsSummaryText.text =
                $"Round: {AliasGameSettings.RoundTimeSeconds}s • Teams: {AliasGameSettings.NumTeams} • Score: {AliasGameSettings.Score} \n " +
                $"Category: {categoryName} • Difficulty: {difficultyStr}";
        }
    }

    public void OnSettingsButton() { if (settingsPanel) settingsPanel.SetActive(true); }
    public void OnCloseSettingsButton() { if (settingsPanel) settingsPanel.SetActive(false); }

    public void OnApplySettingsButton()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int newRoundTime = AliasGameSettings.RoundTimeSeconds;
        int newNumTeams = AliasGameSettings.NumTeams;
        int newScore = AliasGameSettings.Score;

        if (roundTimeDropdown) int.TryParse(roundTimeDropdown.options[roundTimeDropdown.value].text, out newRoundTime);
        if (teamsDropdown) int.TryParse(teamsDropdown.options[teamsDropdown.value].text, out newNumTeams);
        if (scoreDropdown) int.TryParse(scoreDropdown.options[scoreDropdown.value].text, out newScore);

        Hashtable props = new Hashtable
        {
            { "RoundTime", newRoundTime },
            { "NumTeams", newNumTeams },
            { "Category", categoryDropdown.value },
            { "Difficulty", difficultyDropdown.value },
            { "Score", newScore }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(props);

        if (settingsPanel) settingsPanel.SetActive(false);
    }

    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
        // אם המשחק התחיל בזמן שהיינו בלובי - כנס מיד!
        if (changedProps.ContainsKey("GameStarted") && (bool)changedProps["GameStarted"])
        {
            PhotonNetwork.LoadLevel("GameScene");
            return;
        }

        if (changedProps.ContainsKey("RoundTime")) AliasGameSettings.RoundTimeSeconds = (int)changedProps["RoundTime"];
        if (changedProps.ContainsKey("NumTeams")) AliasGameSettings.NumTeams = (int)changedProps["NumTeams"];
        if (changedProps.ContainsKey("Score")) AliasGameSettings.Score = (int)changedProps["Score"];
        
        if (changedProps.ContainsKey("Category"))
        {
             GameManager.Instance.SetCategory((int)changedProps["Category"]);
             if(categoryDropdown) categoryDropdown.value = (int)changedProps["Category"];
        }

        if (changedProps.ContainsKey("Difficulty"))
        {
            int diffIdx = (int)changedProps["Difficulty"];
            if (difficultyDropdown != null) difficultyDropdown.value = diffIdx;
            GameManager.Instance.SetDifficulty(diffIdx);
        }

        InitDropdownValues();
        UpdateSettingsSummaryLabel();
        RefreshPlayersTable();
    }

    // ---------- PLAYERS TABLE ----------
    void ClearPlayersTable() { foreach (Transform t in teamTable) Destroy(t.gameObject); }

    void RefreshPlayersTable()
    {
        ClearPlayersTable();
        teamColumns.Clear();

        int numTeams = AliasGameSettings.NumTeams;
        for(int i = 0; i < numTeams; i++)
        {
            GameObject colObj = Instantiate(teamColumnPrefab, teamTable);
            TeamColumnUI col = colObj.GetComponent<TeamColumnUI>();
            col.Init(i, this);
            teamColumns.Add(col);
        }
        int[] counts = new int[numTeams];

        foreach (Player p in PhotonNetwork.PlayerList)
        {
            int teamIndex = GetPlayerTeam(p); 
            if (teamIndex < 0 || teamIndex >= numTeams) continue;

            counts[teamIndex]++;
            GameObject nameObj = Instantiate(playerNamePrefab, teamColumns[teamIndex].playersList);
            TMP_Text txt = nameObj.GetComponent<TMP_Text>();
            if (txt != null) txt.text = p.NickName;
        }

        for (int i = 0; i < numTeams; i++) teamColumns[i].SetCount(counts[i]);
    }

    public int GetPlayerTeam(Player p)
    {
        if (p.CustomProperties.TryGetValue(TEAM_KEY, out object value))
            if (value is int idx) return idx;
        return -1;
    }

    public void OnLocalPlayerChooseTeam(int teamIndex)
    {
        Hashtable props = new Hashtable(); 
        props[TEAM_KEY] = teamIndex;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    public override void OnPlayerEnteredRoom(Player newPlayer) 
    { 
        RefreshPlayersTable(); 
        
        // אם אני הוסט ונכנס שחקן, אני מוודא שהוא מקבל את המצב העדכני
        if (PhotonNetwork.IsMasterClient)
        {
            // אין צורך לשלוח ידנית, ה-RoomProps כבר שם
        }
    }
    public override void OnPlayerLeftRoom(Player otherPlayer) { RefreshPlayersTable(); }
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (changedProps.ContainsKey(TEAM_KEY)) RefreshPlayersTable();
    }
    
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        UpdateHostUI();
    }

    // ---------- START GAME ----------
    private void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (isGameStarting) return;

        isGameStarting = true;
        if (startGameButton != null) startGameButton.GetComponent<UnityEngine.UI.Button>().interactable = false;

        GameManager.Instance.InitializeTeams(AliasGameSettings.NumTeams);
        GameManager.Instance.settings.targetScore = AliasGameSettings.Score;

        // שמירת השחקנים בצוותים בתוך GameManager (לגיבוי)
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            int teamIndex = GetPlayerTeam(p);
            if (teamIndex < 0) teamIndex = 0; 
            if (teamIndex < GameManager.Instance.teams.Count)
            {
                TeamInfo t = GameManager.Instance.teams[teamIndex];
                t.players.Add(new PlayerInfo(p.ActorNumber.ToString(), p.NickName));
            }
        }
        
        // סימון שהמשחק התחיל - זה יגרום לכולם לעבור סצנה
        PhotonNetwork.CurrentRoom.SetCustomProperties(new Hashtable { 
            { "NextTeamIndex", 0 },
            { "GameStarted", true } 
        });

        // גם ההוסט עובר
        PhotonNetwork.LoadLevel("GameScene");
    }

}
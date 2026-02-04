using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using Photon.Voice.Unity;
using ExitGames.Client.Photon;

public class GameManager : MonoBehaviourPunCallbacks
{
    public static GameManager Instance;
    public bool VoiceEnabled;

    [Header("Settings")]
    public GameSettings settings = new GameSettings();

    [Header("Teams")]
    public List<TeamInfo> teams = new List<TeamInfo>();
    public GameState state = new GameState();

    [Header("Word Categories")]
    public WordCategory[] categories;
    public WordCategory currentCategory;

    public enum Difficulty { Easy, Medium, Hard }
    public Difficulty selectedDifficulty = Difficulty.Easy;

    private List<string> currentWords = new List<string>();
    private List<string> deck = new List<string>();
    private int deckIndex = 0;

    public int RoundSeconds => settings.roundSeconds;
    public int NumberOfTeams => settings.numberOfTeams;
    public int TargetScore => settings.targetScore;
    public int NextTeamIndex { get => state.nextTeamIndex; set => state.nextTeamIndex = value; }


    private void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (PhotonNetwork.InRoom)
        {
            
            ReclaimTeam();
            
            SyncStateFromRoom();
        }
        else
        {
            
            if (currentWords == null || currentWords.Count == 0)
            {
                
                if (categories != null && categories.Length > 0)
                {
                    state.selectedCategoryIndex = 0;
                    currentCategory = categories[0];
                    selectedDifficulty = Difficulty.Easy;
                    LoadCurrentCategoryWords();
                }
            }
        }
    }

    // הוסף את הפונקציה הזו בתוך GameManager (בסוף, לפני הסוגר האחרון)
    void ReclaimTeam()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey("team")) return;

        string myName = PhotonNetwork.LocalPlayer.NickName;
        
        for (int i = 0; i < teams.Count; i++)
        {
            foreach (var p in teams[i].players)
            {
                
                if (p.nickname == myName) 
                {
                    Debug.Log($"[GameManager] Welcome back {myName}! Re-assigning to Team {i}");
                    
                    ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
                    props.Add("team", i);
                    PhotonNetwork.LocalPlayer.SetCustomProperties(props);
                    return;
                }
            }
        }
    }
    
    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();
        SyncStateFromRoom();
        SyncScoresFromRoomProps(PhotonNetwork.CurrentRoom.CustomProperties);
    }

    // עדכון תמידי מהענןkakak
    public override void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        SyncScoresFromRoomProps(propertiesThatChanged);
        
        // עדכון הקטגוריה והקושי בזמן אמת אם השתנו
        if (propertiesThatChanged.ContainsKey("Category")) SetCategory((int)propertiesThatChanged["Category"]);
        if (propertiesThatChanged.ContainsKey("Difficulty")) SetDifficulty((int)propertiesThatChanged["Difficulty"]);
        if (propertiesThatChanged.ContainsKey("NextTeamIndex")) NextTeamIndex = (int)propertiesThatChanged["NextTeamIndex"];
        if (propertiesThatChanged.ContainsKey("RoundTime")) settings.roundSeconds = (int)propertiesThatChanged["RoundTime"];
        if (propertiesThatChanged.ContainsKey("NumTeams")) settings.numberOfTeams = (int)propertiesThatChanged["NumTeams"];
        if (propertiesThatChanged.ContainsKey("Score")) settings.targetScore = (int)propertiesThatChanged["Score"];

    }

    // החלפת הוסט - ההוסט החדש לוקח פיקוד ומוודא סנכרון
    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.LocalPlayer == newMasterClient)
        {
            Debug.Log("[GameManager] I am the new Host! Taking control.");
            SyncStateFromRoom();
        }
    }

    // --- הפונקציה הקריטית לשחזור מצב ---
    public void SyncStateFromRoom()
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        
        var props = PhotonNetwork.CurrentRoom.CustomProperties;
        if (props.ContainsKey("RoundTime")) settings.roundSeconds = (int)props["RoundTime"];
        if (props.ContainsKey("NumTeams")) settings.numberOfTeams = (int)props["NumTeams"];
        if (props.ContainsKey("Score")) settings.targetScore = (int)props["Score"];

        // 1. קטגוריות וקושי
        if (categories != null && categories.Length > 0)
        {
            int catIdx = props.ContainsKey("Category") ? (int)props["Category"] : 0;
            if (props.ContainsKey("Difficulty")) selectedDifficulty = (Difficulty)props["Difficulty"];
            
            // טעינת המילים בלי לאפס הכל
            if(currentCategory != categories[catIdx])
                SetCategory(catIdx); 
            else
                LoadCurrentCategoryWords(); // רק רענון מילים
        }

        // 2. שחזור מספר הצוותים והניקוד שלהם
        int numTeams = props.ContainsKey("NumTeams") ? (int)props["NumTeams"] : 2;
        
        // בניית הצוותים אם הם לא קיימים
        InitializeTeams(numTeams);

        // 3. משיכת הניקוד העדכני (כדי שלא יהיה 0)
        SyncScoresFromRoomProps(props);

        // 4. מי הקבוצה שמשחקת עכשיו?
        if (props.ContainsKey("NextTeamIndex"))
            NextTeamIndex = (int)props["NextTeamIndex"];
            
        // 5. בדיקה האם המשחק כבר רץ - אם כן, להודיע למי שמאזין (כמו ה-UI)
        if (props.ContainsKey("GameStarted") && (bool)props["GameStarted"])
        {
            // אם אנחנו בלובי, זה יגרום ללובי לזרוק אותנו למשחק
            // לוגיקה זו תטופל ב-LobbyManager
        }
    }

    void SyncScoresFromRoomProps(Hashtable props)
    {
        for (int i = 0; i < teams.Count; i++)
        {
            string key = "Score_" + i;
            if (props.ContainsKey(key))
            {
                teams[i].score = (int)props[key];
            }
        }
    }

    // --- שאר הפונקציות שלך ---

    void LoadDefaultWords()
    {
        if (currentWords == null || currentWords.Count == 0)
        {
            if (categories != null && categories.Length > 0)
            {
                if (state.selectedCategoryIndex < 0) state.selectedCategoryIndex = 0;
                currentCategory = categories[state.selectedCategoryIndex];
                selectedDifficulty = Difficulty.Easy;
                LoadCurrentCategoryWords();
                ResetDeck();
            }
        }
    }

    public void SetCategory(int index)
    {
        if (categories == null || categories.Length == 0) return;
        if (index < 0 || index >= categories.Length) index = 0;

        state.selectedCategoryIndex = index;
        currentCategory = categories[index];
        LoadCurrentCategoryWords();
        ResetDeck();
    }

    public void SetDifficulty(int difficultyIndex)
    {
        selectedDifficulty = (Difficulty)difficultyIndex;
        LoadCurrentCategoryWords();
        ResetDeck();
    }

    private void LoadCurrentCategoryWords()
    {
        if (currentCategory == null) return;

        string fileToLoad = "";
        switch (selectedDifficulty)
        {
            case Difficulty.Easy: fileToLoad = currentCategory.fileNameEasy; break;
            case Difficulty.Medium: fileToLoad = currentCategory.fileNameMedium; break;
            case Difficulty.Hard: fileToLoad = currentCategory.fileNameHard; break;
        }

        if (string.IsNullOrEmpty(fileToLoad)) return;
        currentWords = WordBank.LoadCategory(fileToLoad);
    }

    private void ResetDeck()
    {
        deck = new List<string>(currentWords);
        Shuffle(deck);
        deckIndex = 0;
    }

    public string GetRandomWord()
    {
        if (deck == null || deck.Count == 0) return "NO WORDS";
        if (deckIndex >= deck.Count) { Shuffle(deck); deckIndex = 0; }
        return deck[deckIndex++];
    }

    private void Shuffle(List<string> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    public void SetMicState(bool enabled)
    {
        var recorder = FindObjectOfType<Recorder>();
        if (recorder) { recorder.RecordingEnabled = enabled; recorder.TransmitEnabled = enabled; }
    }

    public void ForceDisableVoiceEverywhere()
    {
        var recorders = Object.FindObjectsByType<Recorder>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var r in recorders) { if (r != null) { r.TransmitEnabled = false; r.RecordingEnabled = false; } }
        var speakers = Object.FindObjectsByType<Speaker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var s in speakers) if (s != null) s.enabled = false;
    }

    public void InitializeTeams(int numberOfTeams)
    {
        // הגנה: אם הצוותים כבר קיימים (שחזור), אל תאפס אותם!
        if (teams.Count == numberOfTeams) return;

        teams.Clear();
        for (int i = 0; i < numberOfTeams; i++) { 
            TeamInfo t = new TeamInfo(); 
            t.teamName = $"Team {i + 1}"; 
            t.score = 0; // אתחול ראשוני, ידרוס אותו מיד הסנכרון
            teams.Add(t); 
        }
    }
    
    public void ResetMatch(int numberOfTeams)
    {
        InitializeTeams(numberOfTeams);
        foreach (var t in teams) t.score = 0;
        NextTeamIndex = 0;
        
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable();
            for(int i=0; i<numberOfTeams; i++) props.Add("Score_" + i, 0);
            props.Add("NextTeamIndex", 0);
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    public void AddCorrectPoint(int teamIndex)
    {
        if (teamIndex >= teams.Count) return;
        teams[teamIndex].score++;
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable { { "Score_" + teamIndex, teams[teamIndex].score } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    public void AddPenalty(int teamIndex) 
    {
        if (teamIndex >= teams.Count) return;
        teams[teamIndex].AddScore(-1);
        if (PhotonNetwork.IsMasterClient)
        {
            Hashtable props = new Hashtable { { "Score_" + teamIndex, teams[teamIndex].score } };
            PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    public bool CheckWin(int teamIndex) => teams[teamIndex].score >= settings.targetScore;
}

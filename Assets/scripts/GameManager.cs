using System.Collections.Generic;
using UnityEngine;
using Photon.Voice.Unity;
using Photon.Voice.PUN;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public bool VoiceEnabled; // online

    [Header("Settings")]
    public GameSettings settings = new GameSettings();

    [Header("Teams")]
    public List<TeamInfo> teams = new List<TeamInfo>();
    public GameState state = new GameState();

    [Header("Word Categories")]
    public WordCategory[] categories;
    public WordCategory currentCategory;   // ← MUST BE PUBLIC

    // Loaded words for the currently selected category
    private List<string> currentWords = new List<string>();
    private List<string> deck = new List<string>();
    private int deckIndex = 0;
    // Shortcuts
    public int RoundSeconds => settings.roundSeconds;
    public int NumberOfTeams => settings.numberOfTeams;

    public int NextTeamIndex
    {
        get => state.nextTeamIndex;
        set => state.nextTeamIndex = value;
    }

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (categories == null || categories.Length == 0)
            return;

        // Load saved category index
        int index = state.selectedCategoryIndex;

        if (index < 0 || index >= categories.Length)
            index = 0;

        currentCategory = categories[index];
        LoadCurrentCategoryWords();
        ResetDeck();
    }

    // public void ApplyVoiceMode()
    // {
    //     // If you don't have PhotonVoiceNetwork in the project, stop here
    //     if (PhotonVoiceNetwork.Instance == null)
    //     {
    //         Debug.LogWarning("[VOICE] PhotonVoiceNetwork.Instance is null (Voice prefab missing?)");
    //         return;
    //     }

    //     var recorder = PhotonVoiceNetwork.Instance.PrimaryRecorder;

    //     if (!VoiceEnabled)
    //     {
    //         // SAME ROOM => hard OFF
    //         if (recorder != null)
    //         {
    //             recorder.DebugEchoMode = false;
    //             recorder.TransmitEnabled = false;
    //             recorder.RecordingEnabled = false;
    //         }

    //         if (PhotonVoiceNetwork.Instance.ClientState == Photon.Realtime.ClientState.Joined)
    //             PhotonVoiceNetwork.Instance.Disconnect();

    //         DisableAllSpeakers();
    //         return;
    //     }

    //     // ONLINE => ON
    //     if (PhotonNetwork.InRoom && PhotonVoiceNetwork.Instance.ClientState != Photon.Realtime.ClientState.Joined)
    //     {
    //         PhotonVoiceNetwork.Instance.ConnectAndJoinRoom();
    //     }

    //     if (recorder != null)
    //     {
    //         recorder.DebugEchoMode = false;   // important
    //         recorder.RecordingEnabled = true; // mic can be enabled later by RoundManager
    //         recorder.TransmitEnabled = false; // default OFF until active team
    //     }
    // }

    public void SetMicState(bool enabled)
    {
        var recorder = FindObjectOfType<Recorder>();
        if (recorder == null)
        {
            Debug.Log("[VOICE] Recorder not found");
            return;
        }

        recorder.RecordingEnabled = enabled;
        recorder.TransmitEnabled = enabled;

        Debug.Log($"[VOICE] Mic set to {enabled}");
    }
    public void ForceDisableVoiceEverywhere()
    {
        // Disable ALL recorders (sometimes you have more than one)
        var recorders = Object.FindObjectsByType<Recorder>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var r in recorders)
        {
            if (r == null) continue;
            r.TransmitEnabled = false;
            r.RecordingEnabled = false;
        }

        // Disable ALL speakers
        var speakers = Object.FindObjectsByType<Speaker>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (var s in speakers)
            if (s != null) s.enabled = false;
    }
    public void DisableAllSpeakers()
    {
        var speakers = Object.FindObjectsByType<Speaker>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var s in speakers)
            if (s != null) s.enabled = false;
    }


    // ==============================
    //        CATEGORY SYSTEM
    // ==============================
    public void SetCategory(int index)
    {
        if (categories == null || categories.Length == 0)
        {
            Debug.LogError("No categories defined!");
            return;
        }

        if (index < 0 || index >= categories.Length)
            index = 0;

        state.selectedCategoryIndex = index;
        currentCategory = categories[index];
        LoadCurrentCategoryWords();
        ResetDeck();
    }

    private void LoadCurrentCategoryWords()
    {
        if (currentCategory == null || string.IsNullOrEmpty(currentCategory.fileName))
        {
            Debug.LogError("GameManager: currentCategory or fileName is null!");
            currentWords = new List<string>();
            return;
        }

        currentWords = WordBank.LoadCategory(currentCategory.fileName);

        if (currentWords.Count == 0)
        {
            Debug.LogError($"GameManager: FAILED to load words from Resources/Words/{currentCategory.fileName}");
        }
        else
        {
            Debug.Log($"GameManager: Successfully loaded {currentWords.Count} words for {currentCategory.categoryName}");
        }
    }

    private void ResetDeck()
    {
        deck = new List<string>(currentWords);
        Shuffle(deck);
        deckIndex = 0;
    }

    public string GetRandomWord()
    {
        if (deck == null || deck.Count == 0)
            return "NO WORDS";

        if (deckIndex >= deck.Count)
        {
            Shuffle(deck);
            deckIndex = 0;
        }

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


    // ==============================
    //        TEAM SYSTEM
    // ==============================
    public void InitializeTeams(int numberOfTeams)
    {
        teams.Clear();

        for (int i = 0; i < numberOfTeams; i++)
        {
            TeamInfo t = new TeamInfo();
            t.teamName = $"Team {i + 1}";
            teams.Add(t);
        }
    }

    public void AddCorrectPoint(int teamIndex)
    {
        
        teams[teamIndex].score++;

        if (Photon.Pun.PhotonNetwork.IsMasterClient )//|| Photon.Pun.PhotonNetwork.LocalPlayer.IsLocal
        {
            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
            props.Add("Score_" + teamIndex, teams[teamIndex].score);
            Photon.Pun.PhotonNetwork.CurrentRoom.SetCustomProperties(props);
        }
    }

    public void AddPenalty(int teamIndex)
    {
        teams[teamIndex].AddScore(-1);
    }

    public bool CheckWin(int teamIndex)
    {
        return teams[teamIndex].score >= settings.targetScore;
    }


    // 
    public void ResetMatch(int numberOfTeams)
    {
        if (teams == null || teams.Count != numberOfTeams)
            InitializeTeams(numberOfTeams);

        foreach (var t in teams)
            t.score = 0;

        NextTeamIndex = 0;
    }
    //
}

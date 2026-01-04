using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

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
        // עדכון מקומי
        teams[teamIndex].score++;

        // עדכון רשת - רק ה-MasterClient או המקריא מעדכנים
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
}

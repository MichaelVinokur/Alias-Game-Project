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
    }

    public string GetRandomWord()
    {
        if (currentCategory == null || currentCategory.words == null || currentCategory.words.Length == 0)
            return "NO WORDS";

        int r = Random.Range(0, currentCategory.words.Length);
        return currentCategory.words[r];
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
        teams[teamIndex].AddScore(+1);
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

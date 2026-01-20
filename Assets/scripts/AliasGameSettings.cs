using UnityEngine;

/// <summary>
/// Global game settings for the Same Room Alias mode.
/// For now we only store round time and number of teams.
/// Later you can add more fields here (difficulty, points to win, etc.)
/// </summary>
public static class AliasGameSettings
{
    // seconds per round (default = 60)
    public static int RoundTimeSeconds = 60;

    // number of teams (default = 2)
    public static int NumTeams = 2;

    public static int Score = 60;
}

using System;
using System.Collections.Generic;

[Serializable]
public class TeamInfo
{
    public string teamName;
    public List<PlayerInfo> players = new List<PlayerInfo>();
    public int score = 0;

    public void AddScore(int amount)
    {
        score += amount;
        if (score < 0)
            score = 0;  // ⛔ never allow negative score
    }
}

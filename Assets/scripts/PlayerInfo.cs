using System;

[Serializable]
public class PlayerInfo
{
    public string playerId;   // Photon UserId
    public string nickname;

    public PlayerInfo(string id, string name)
    {
        playerId = id;
        nickname = name;
    }
}

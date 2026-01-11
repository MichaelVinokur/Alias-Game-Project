using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;

public class GameSceneManager : MonoBehaviourPunCallbacks
{
    public static GameSceneManager Instance;
    public CanvasGroup scoreboardCG;
    public CanvasGroup roundCG;
    

    void Awake()
    {
        Instance = this;
    }
    
    public override void OnJoinedRoom()
    {
        //
        if (GameManager.Instance != null)
            GameManager.Instance.ResetMatch(AliasGameSettings.NumTeams);
        
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
        {
            var ht = new Hashtable
            {
                { "Phase", "Scoreboard" },
                { "NextTeamIndex", 0 },
                { "ActiveTeam", -1 },
                { "ScoreTick", 0 }
            };

            for (int i = 0; i < AliasGameSettings.NumTeams; i++)
                ht["Score_" + i] = 0;
        //

    PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
}


        RefreshPanels();
    }
    void Start()
    {
        //
        if (PhotonNetwork.InRoom && GameManager.Instance != null)
            GameManager.Instance.ResetMatch(AliasGameSettings.NumTeams);
        //
        
        RefreshPanels();
        GameManager.Instance.SetMicState(true);//test test test
    }
    public override void OnEnable()
    {
        base.OnEnable();
    }


    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
    
         if (changedProps.TryGetValue("Phase", out var ph) && (string)ph == "GameOver")
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.LoadLevel("EndGameScene"); // exact scene name
            return;
        }
        if (changedProps.ContainsKey("Phase") ||
        changedProps.ContainsKey("ActiveTeam") ||
        changedProps.ContainsKey("NextTeamIndex"))
        {
            RefreshPanels();
        }
    }

    public void RefreshPanels()
    {

        if (!PhotonNetwork.InRoom)
            return;

        var props = PhotonNetwork.CurrentRoom.CustomProperties;

        string phase = props.TryGetValue("Phase", out var p) ? (string)p : "Scoreboard";
        int activeTeam = props.TryGetValue("ActiveTeam", out var t) ? (int)t : -1;

        int myTeam = GetMyTeam();
        bool showRound = (phase == "Round" && myTeam == activeTeam);

        SetPanel(scoreboardCG, !showRound);
        SetPanel(roundCG, showRound);
    }

    void SetPanel(CanvasGroup cg, bool show)
    {
        if (!cg) return;
        cg.alpha = show ? 1 : 0;
        cg.interactable = show;
        cg.blocksRaycasts = show;
    }

    int GetMyTeam()
    {
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("team", out object t))
            return (int)t;

        return -1;
    }

    //
    public void ResetForNewMatch_Master()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (!PhotonNetwork.IsConnectedAndReady || !PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null) return;

        // Reset local (host)
        if (GameManager.Instance != null)
            GameManager.Instance.ResetMatch(AliasGameSettings.NumTeams);

        // Reset room props (everyone will sync clean)
        var ht = new Hashtable
        {
            { "Phase", "Scoreboard" },
            { "NextTeamIndex", 0 },
            { "ActiveTeam", -1 },
            { "ScoreTick", 0 },
            { "Timer", 0 }
        };

        for (int i = 0; i < AliasGameSettings.NumTeams; i++)
            ht["Score_" + i] = 0;

        PhotonNetwork.CurrentRoom.SetCustomProperties(ht);
    }
    //

}


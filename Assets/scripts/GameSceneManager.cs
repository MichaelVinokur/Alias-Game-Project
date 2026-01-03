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
        RefreshPanels();
    }
    void Start()
    {
        RefreshPanels();
    }
    public override void OnEnable()
    {
        base.OnEnable();
    }


    public override void OnRoomPropertiesUpdate(Hashtable changedProps)
    {
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
}


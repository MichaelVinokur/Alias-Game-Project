using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class SameRoomNetworkManager : MonoBehaviourPunCallbacks
{
    [Header("UI - Common")]
    public TMPro.TMP_Text hostCodeText;          // text somewhere on screen to show host's code

    [Header("UI - Host popup")]
    public GameObject hostPopupPanel;
    public TMPro.TMP_InputField hostNameInput;

    [Header("UI - Join popup")]
    public GameObject joinPopupPanel;
    public TMPro.TMP_InputField joinCodeInput;   // code field inside join popup
    public TMPro.TMP_InputField joinNameInput;   // name field inside join popup

    void Start()
    {
        // hide both popups at the start
        if (hostPopupPanel != null) hostPopupPanel.SetActive(false);
        if (joinPopupPanel != null) joinPopupPanel.SetActive(false);

        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon");
        PhotonNetwork.JoinLobby();
    }

    // ================= HOST FLOW =================

    // Main screen "Create Game" button
    public void OnOpenHostPopup()
    {
        if (hostPopupPanel != null)
            hostPopupPanel.SetActive(true);
    }

    // X or Cancel in host popup
    public void OnCloseHostPopup()
    {
        if (hostPopupPanel != null)
            hostPopupPanel.SetActive(false);
    }

    // "Create Game" button inside host popup
    public void OnHostCreateGame()
    {
        string playerName = hostNameInput.text.Trim();
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Host name empty");
            return;
        }

        PhotonNetwork.NickName = playerName;

        // random 6-digit code
        string roomCode = Random.Range(100000, 999999).ToString();
        RoomOptions options = new RoomOptions { MaxPlayers = 8 };

        Debug.Log("Creating room: " + roomCode);
        PhotonNetwork.CreateRoom(roomCode, options);

        if (hostCodeText != null)
            hostCodeText.text = "Code: " + roomCode;

        hostPopupPanel.SetActive(false);
    }

    // ================= JOINER FLOW =================

    // Main screen "Join Game" button
    public void OnOpenJoinPopup()
    {
        if (joinPopupPanel != null)
            joinPopupPanel.SetActive(true);
    }

    // X or Cancel in join popup
    public void OnCloseJoinPopup()
    {
        if (joinPopupPanel != null)
            joinPopupPanel.SetActive(false);
    }

    // "Join Game" button inside join popup
    public void OnJoinConfirm()
    {
        string code = joinCodeInput.text.Trim();
        string playerName = joinNameInput.text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Join code empty");
            return;
        }

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Join name empty");
            return;
        }

        PhotonNetwork.NickName = playerName;

        Debug.Log("Joining room: " + code);
        PhotonNetwork.JoinRoom(code);

        joinPopupPanel.SetActive(false);
    }

    // ================= PHOTON CALLBACKS =================

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);
        PhotonNetwork.LoadLevel("SameRoomLobby");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Join failed: " + message);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Create room failed: " + message);
    }
}

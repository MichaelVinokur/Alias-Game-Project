using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

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

    private static bool isHosting; //for loading scene
    private static string savedName; //for loading scene
    private static string savedCode; //for loading scene
    private bool pendingAction;
    public static bool PendingIsHosting { get; private set; }
    private static string pendingName;
    private static string pendingCode;
    void Awake()
    {
        Debug.Log("[SRNM] Awake instance id=" + GetInstanceID());
    }
    void Start()
    {
        Debug.Log("[SRNM] Start state=" + PhotonNetwork.NetworkClientState);

        PhotonNetwork.AutomaticallySyncScene = true;

        var st = PhotonNetwork.NetworkClientState;
        if (st == ClientState.PeerCreated || st == ClientState.Disconnected)
        {
            Debug.Log("[SRNM] Connecting... state=" + st);
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Connected to Photon");
        PhotonNetwork.AutomaticallySyncScene = true;
    }
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError("[SRNM] Disconnected: " + cause);
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
        var name = hostNameInput.text.Trim();
        if (string.IsNullOrEmpty(name)) return;

        PendingIsHosting = true;
        pendingName = name;

        if (hostPopupPanel) hostPopupPanel.SetActive(false);
        SceneManager.LoadScene("LoadingScene");
    }

    private void ExecuteHostLogic()
    {
        PhotonNetwork.NickName = savedName;
        string roomCode = Random.Range(100, 999).ToString();
        RoomOptions options = new RoomOptions { MaxPlayers = 8 };
        PhotonNetwork.CreateRoom(roomCode, options);

        if (hostCodeText != null)
            hostCodeText.text = "Code: " + roomCode;
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
        var code = joinCodeInput.text.Trim();
        var name = joinNameInput.text.Trim();
        if (string.IsNullOrEmpty(code) || string.IsNullOrEmpty(name)) return;

        PendingIsHosting = false;
        pendingCode = code;
        pendingName = name;

        if (joinPopupPanel) joinPopupPanel.SetActive(false);
        SceneManager.LoadScene("LoadingScene");
    }

    private void ExecuteJoinLogic()
    {
        PhotonNetwork.NickName = savedName;
        PhotonNetwork.JoinRoom(savedCode);
    }

    // ================= PHOTON CALLBACKS =================

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined room: " + PhotonNetwork.CurrentRoom.Name);
        PhotonNetwork.LoadLevel("SameRoomLobby_edit");//change new lobby
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Join failed: " + message);
        SceneManager.LoadScene("SameRoom");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Create room failed: " + message);
        SceneManager.LoadScene("SameRoom");
    }

    public static void ExecuteHostNow()
    {
        PhotonNetwork.NickName = pendingName;
        string roomCode = Random.Range(100, 999).ToString();
        RoomOptions options = new RoomOptions { MaxPlayers = 8 };
        PhotonNetwork.CreateRoom(roomCode, options);
    }

    public static void ExecuteJoinNow()
    {
        PhotonNetwork.NickName = pendingName;
        PhotonNetwork.JoinRoom(pendingCode);
    }

    //fix for android studio
    private IEnumerator FocusTMP(TMPro.TMP_InputField field)
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        // try to prevent immediate deselect
        for (int i = 0; i < 10; i++)
        {
            EventSystem.current.SetSelectedGameObject(field.gameObject);
            field.ActivateInputField();

            Debug.Log("Selected: " +
                      (EventSystem.current.currentSelectedGameObject
                          ? EventSystem.current.currentSelectedGameObject.name
                          : "null"));

            yield return null;
        }
    }
}

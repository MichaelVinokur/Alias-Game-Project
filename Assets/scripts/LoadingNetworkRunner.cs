using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class LoadingNetworkRunner : MonoBehaviourPunCallbacks
{
    void Start()
    {
        // Ensure Photon is connected
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.ConnectUsingSettings();
            return;
        }

        TryDoPending();
    }

    public override void OnConnectedToMaster()
    {
        TryDoPending();
    }

    private void TryDoPending()
    {
        if (!PhotonNetwork.IsConnectedAndReady) return;

        // Pull the pending action from your SameRoomNetworkManager static fields
        // (we keep using your existing static vars for now)
        if (SameRoomNetworkManager.PendingIsHosting)
            SameRoomNetworkManager.ExecuteHostNow();
        else
            SameRoomNetworkManager.ExecuteJoinNow();
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("SameRoomLobby_edit");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Join failed: " + message);
        UnityEngine.SceneManagement.SceneManager.LoadScene("SameRoom");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Create room failed: " + message);
        UnityEngine.SceneManagement.SceneManager.LoadScene("SameRoom");
    }
}

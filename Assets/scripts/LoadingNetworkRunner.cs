using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections;

public class LoadingNetworkRunner : MonoBehaviourPunCallbacks
{
    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (!PhotonNetwork.IsConnected)
        {
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
        StopAllCoroutines();
        StartCoroutine(ShowThenBack("Code incorrect"));
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning("Create room failed: " + message);
        UnityEngine.SceneManagement.SceneManager.LoadScene("SameRoom");
    }
    private IEnumerator ShowThenBack(string msg)
    {
        var ui = FindAnyObjectByType<LoadingUI>();
        if (ui != null) ui.ShowError(msg, 1.5f);

        yield return new WaitForSecondsRealtime(1.6f);
        UnityEngine.SceneManagement.SceneManager.LoadScene("SameRoom");
    }
}


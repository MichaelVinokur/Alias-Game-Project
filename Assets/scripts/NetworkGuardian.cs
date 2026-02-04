using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkGuardian : MonoBehaviourPunCallbacks
{
    public static NetworkGuardian Instance;

    [SerializeField] private float rejoinWindowSeconds = 30f;
    [SerializeField] private string fallbackScene = "SameRoom";

    private Coroutine rejoinCo;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        Debug.Log("[NG] Awake (persisting)");
    }
    //ka
    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarning("[NG] Disconnected: " + cause);

        if (rejoinCo != null) StopCoroutine(rejoinCo);
        rejoinCo = StartCoroutine(TryReconnectAndRejoin(rejoinWindowSeconds));
    }

    private IEnumerator TryReconnectAndRejoin(float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds;
        float nextAttempt = 0f;

        while (Time.realtimeSinceStartup < end)
        {
            if (PhotonNetwork.InRoom)
            {
                Debug.Log("[NG] Rejoin success (InRoom)");
                yield break;
            }

            if (Time.realtimeSinceStartup >= nextAttempt)
            {
                nextAttempt = Time.realtimeSinceStartup + 2f;

                var st = PhotonNetwork.NetworkClientState;
                Debug.Log("[NG] Attempt tick. state=" + st);

                if (st == ClientState.Disconnected)
                {
                    Debug.Log("[NG] Calling ReconnectAndRejoin...");
                    PhotonNetwork.ReconnectAndRejoin();
                }
            }

            yield return null;
        }

        Debug.LogWarning("[NG] Rejoin window expired -> leave + menu");

        if (PhotonNetwork.InRoom)
            PhotonNetwork.LeaveRoom();

        PhotonNetwork.Disconnect();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }


    public override void OnJoinedRoom()
    {
        Debug.Log("[NG] Rejoined room: " + PhotonNetwork.CurrentRoom?.Name);
        if (rejoinCo != null) { StopCoroutine(rejoinCo); rejoinCo = null; }
    }
}


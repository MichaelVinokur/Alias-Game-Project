// using System.Collections;
// using Photon.Pun;
// using Photon.Realtime;
// using UnityEngine;
// using UnityEngine.SceneManagement;
// using Photon.Voice.Unity.UtilityScripts;

// public class NetworkGuardian : MonoBehaviourPunCallbacks
// {
//     public static NetworkGuardian Instance;

//     [SerializeField] private float rejoinWindowSeconds = 30f;
//     [SerializeField] private string fallbackScene = "SameRoom";

//     private Coroutine rejoinCo;

//     void Awake()
//     {
//         if (Instance != null) { Destroy(gameObject); return; }
//         Instance = this;
//         DontDestroyOnLoad(gameObject);
//         Debug.Log("[NG] Awake (persisting)");
//     }
//     //ka
//     public override void OnDisconnected(DisconnectCause cause)
//     {
//         Debug.LogWarning("[NG] Disconnected: " + cause);

//         if (rejoinCo != null) StopCoroutine(rejoinCo);
//         rejoinCo = StartCoroutine(TryReconnectAndRejoin(rejoinWindowSeconds));
//     }

//     private IEnumerator TryReconnectAndRejoin(float seconds)
//     {
//         float end = Time.realtimeSinceStartup + seconds;
//         float nextAttempt = 0f;

//         while (Time.realtimeSinceStartup < end)
//         {
//             if (PhotonNetwork.InRoom)
//             {
//                 Debug.Log("[NG] Rejoin success (InRoom)");
//                 yield break;
//             }

//             if (Time.realtimeSinceStartup >= nextAttempt)
//             {
//                 nextAttempt = Time.realtimeSinceStartup + 2f;

//                 var st = PhotonNetwork.NetworkClientState;
//                 Debug.Log("[NG] Attempt tick. state=" + st);

//                 if (st == ClientState.Disconnected)
//                 {
//                     Debug.Log("[NG] Calling ReconnectAndRejoin...");
//                     PhotonNetwork.ReconnectAndRejoin();
//                 }
//             }

//             yield return null;
//         }

//         Debug.LogWarning("[NG] Rejoin window expired -> leave + menu");

//         if (PhotonNetwork.InRoom)
//             PhotonNetwork.LeaveRoom();

//         PhotonNetwork.Disconnect();
//         UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
//     }

//     public override void OnJoinedRoom()
//     {
//         Debug.Log("[NG] Rejoined room: " + PhotonNetwork.CurrentRoom?.Name);
//         if (rejoinCo != null) { StopCoroutine(rejoinCo); rejoinCo = null; }

//         StartCoroutine(RestartVoiceNextFrame());
//     }

//     private IEnumerator RestartVoiceNextFrame()
//     {
//         yield return new WaitForSeconds(1f); 

//         var caj = FindObjectOfType<ConnectAndJoin>();
//         if (caj != null)
//         {
//             caj.RoomName = PhotonNetwork.CurrentRoom.Name;
//             caj.enabled = false;
//             caj.enabled = true;
//             Debug.Log("[NG] Voice ConnectAndJoin restarted.");
//         }

//         var roundMgr = FindObjectOfType<RoundManager>();
//         if (roundMgr != null && PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("ActiveTeam", out var activeTeam))
//         {
//             Debug.Log("[NG] Manually refreshing voice logic for returning player.");
//             roundMgr.ApplyVoiceByActiveTeam((int)activeTeam);
//         }
//     }
// }

using System.Collections;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using Photon.Voice.Unity; 
using Photon.Voice.Unity.UtilityScripts;

public class NetworkGuardian : MonoBehaviourPunCallbacks
{
    public static NetworkGuardian Instance;

    // הגדלנו זמן חלון כדי לתת למובייל זמן להתאושש
    [SerializeField] private float maxWaitTime = 60f; 

    private Coroutine recoveryRoutine;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        // מתעלמים מניתוק שאנחנו יזמנו
        if (cause == DisconnectCause.DisconnectByClientLogic) return;

        Debug.LogWarning($"[NG] Disconnected ({cause}). Blocking connection until network is stable...");
        
        if (recoveryRoutine != null) StopCoroutine(recoveryRoutine);
        recoveryRoutine = StartCoroutine(SmartMobileRejoin());
    }

    private IEnumerator SmartMobileRejoin()
    {
        float timeout = Time.realtimeSinceStartup + maxWaitTime;

        while (Time.realtimeSinceStartup < timeout)
        {
            // 1. האם חזרנו כבר?
            if (PhotonNetwork.InRoom)
            {
                Debug.Log("[NG] Back in room. Stopping recovery.");
                yield break;
            }

            // 2. בדיקת הברזל: האם מערכת ההפעלה מדווחת על אינטרנט?
            // אם זה NotReachable - אנחנו לא עושים כלום! שקט תעשייתי.
            if (Application.internetReachability == NetworkReachability.NotReachable)
            {
                // כאן אנחנו מחכים בשקט. לא פונים לפוטון = אין שגיאות אדומות.
                yield return new WaitForSeconds(1.0f);
                continue; 
            }

            // 3. יש אינטרנט! עכשיו מחכים עוד רגע לוודא יציבות (מונע 10051)
            Debug.Log("[NG] Network detected via OS. Stabilizing...");
            yield return new WaitForSeconds(2.0f);

            // 4. עכשיו, ורק עכשיו, מנסים להתחבר
            var state = PhotonNetwork.NetworkClientState;
            if (state == ClientState.Disconnected || state == ClientState.PeerCreated)
            {
                Debug.Log("[NG] Calling ReconnectAndRejoin...");
                bool started = PhotonNetwork.ReconnectAndRejoin();
                
                if (started)
                {
                    // נותנים לו זמן לעבוד בלי להפריע
                    yield return new WaitForSeconds(5.0f);
                }
            }
            
            yield return new WaitForSeconds(1.0f);
        }

        // אם הגענו לפה, נגמר הזמן
        Debug.LogError("[NG] Timeout. Resetting to Menu.");
        PhotonNetwork.Disconnect();
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("[NG] Joined Room! Initializing Audio...");
        if (recoveryRoutine != null) { StopCoroutine(recoveryRoutine); recoveryRoutine = null; }

        StartCoroutine(RestartVoiceManual());
    }

    private IEnumerator RestartVoiceManual()
    {
        // מחכים שהסצנה תסיים להיטען אם צריך
        yield return new WaitForSeconds(1.0f);

        // 1. טיפול ב-ConnectAndJoin
        var caj = FindObjectOfType<ConnectAndJoin>();
        if (caj != null)
        {
            caj.enabled = false;
            // מעדכנים ידנית את החדר
            if (PhotonNetwork.InRoom) caj.RoomName = PhotonNetwork.CurrentRoom.Name;
            yield return new WaitForSeconds(0.5f);
            caj.enabled = true;
        }

        // 2. טיפול במיקרופון (Recorder) - הפעלה ידנית
        var recorder = FindObjectOfType<Recorder>();
        if (recorder != null)
        {
            Debug.Log("[NG] Force Restarting Recorder...");
            
            // וידוא איפוס
            recorder.RecordingEnabled = false;
            recorder.TransmitEnabled = false;
            
            yield return new WaitForSeconds(0.5f);

            // הדלקה נקייה
            recorder.RecordingEnabled = true;
            recorder.TransmitEnabled = true;
        }

        // 3. החלת הלוגיקה של המשחק
        yield return new WaitForSeconds(1.0f);
        var roundMgr = FindObjectOfType<RoundManager>();
        if (roundMgr != null && PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("ActiveTeam", out var activeTeam))
            {
                Debug.Log($"[NG] Applying Logic for Team {activeTeam}");
                roundMgr.ApplyVoiceByActiveTeam((int)activeTeam);
            }
        }
    }
}


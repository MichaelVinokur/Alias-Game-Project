using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class PhotonIdentity : MonoBehaviourPunCallbacks
{
    private const string UserIdKey = "PHOTON_USER_ID";
    private const string NickKey = "PHOTON_NICK";

    void Awake()
    {
        // 1) Stable UserId (required for ReconnectAndRejoin)
        var userId = PlayerPrefs.GetString(UserIdKey, "");
        if (string.IsNullOrEmpty(userId))
        {
            userId = System.Guid.NewGuid().ToString("N"); // random stable ID
            PlayerPrefs.SetString(UserIdKey, userId);
            PlayerPrefs.Save();
        }

        if (PhotonNetwork.AuthValues == null)
            PhotonNetwork.AuthValues = new AuthenticationValues();

        PhotonNetwork.AuthValues.UserId = userId;

        // 2) Optional: persist nickname too
        var nick = PlayerPrefs.GetString(NickKey, "");
        if (string.IsNullOrEmpty(nick))
        {
            nick = "Player " + Random.Range(1000, 9999);
            PlayerPrefs.SetString(NickKey, nick);
            PlayerPrefs.Save();
        }
        PhotonNetwork.NickName = nick;
        Debug.Log("[PhotonIdentity] UserId=" + PhotonNetwork.AuthValues.UserId);

    }
}


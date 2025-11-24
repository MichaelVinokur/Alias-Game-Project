using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;

public class StartGameButton : MonoBehaviour
{
    public Button startButton;

    void Start()
    {
        // Only Master Client can click it
        if (!PhotonNetwork.IsMasterClient)
            startButton.gameObject.SetActive(false);

        startButton.onClick.AddListener(OnStartClicked);
    }

    void OnStartClicked()
    {
        // Load the game scene for everyone
        PhotonNetwork.LoadLevel("GameScene");   // <-- change to your actual game scene name
    }
}

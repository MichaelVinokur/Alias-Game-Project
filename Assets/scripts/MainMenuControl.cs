using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuControl : MonoBehaviour
{
    public GameObject rulesPanel;

    public void OnRulesButton()
    {
        Debug.Log("Rules button pressed");
        rulesPanel.SetActive(true);
    }

    public void OnCloseRulesButton()
    {
        rulesPanel.SetActive(false);
    }

    public void OnSameRoomButton()
    {
        Debug.Log("Same Room button pressed");
        SceneManager.LoadScene("SameRoom");   // << IMPORTANT
    }

    public void OnOnlineButton()
    {
        Debug.Log("Online button pressed");
    }
}

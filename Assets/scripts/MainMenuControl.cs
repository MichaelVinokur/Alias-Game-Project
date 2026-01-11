using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuControl : MonoBehaviour
{
    public GameObject rulesPanel;
    
    public void OnRulesButton() => rulesPanel.SetActive(true);
    public void OnCloseRulesButton() => rulesPanel.SetActive(false);

    // public void OnRulesButton()
    // {
    //     Debug.Log("Rules button pressed");
    //     rulesPanel.SetActive(true);
    // }

    // public void OnCloseRulesButton()
    // {
    //     rulesPanel.SetActive(false);
    // }

    // public void OnSameRoomButton()
    // {
    //     Debug.Log("Same Room button pressed");
    //     GameManager.Instance.VoiceEnabled = false;
    //     SceneManager.LoadScene("SameRoom");   // << IMPORTANT
    // }


    // public void OnOnlineButton()
    // {
    //     Debug.Log("Online button pressed");
    //     GameManager.Instance.VoiceEnabled = true;
    //     SceneManager.LoadScene("SameRoom");
    // }

    public void OnSameRoomButton()
    {
        Debug.Log("Same Room button pressed");
        GameManager.Instance.VoiceEnabled = false;   // MODE
        SceneManager.LoadScene("SameRoom");          // your lobby scene
    }

    public void OnOnlineButton()
    {
        Debug.Log("Online button pressed");
        GameManager.Instance.VoiceEnabled = true;    // MODE
        SceneManager.LoadScene("SameRoom");          // same lobby scene
    }
    
}

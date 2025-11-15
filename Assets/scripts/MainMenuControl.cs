using UnityEngine; 

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
        // later: open Same Room game mode
    }

    public void OnOnlineButton()
    {
        Debug.Log("Online button pressed");
        // later: open Online game mode
    }
}

using UnityEngine; 

public class MainMenuControl : MonoBehaviour
{
    public void OnRulesButton()
    {
        Debug.Log("Rules button pressed");
        // later: open Rules screen
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

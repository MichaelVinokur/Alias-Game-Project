using UnityEngine;

public class MusicMuteToggleUI : MonoBehaviour
{
    public GameObject soundOn;
    public GameObject soundOff;

    void Start()
    {
        Refresh();
    }

    public void Toggle()
    {
        if (MusicManager.Instance != null)
            MusicManager.Instance.ToggleMute();

        Refresh();
    }

    void Refresh()
    {
        bool muted = PlayerPrefs.GetInt("MusicMuted", 0) == 1;

        soundOn.SetActive(!muted);
        soundOff.SetActive(muted);
    }
}

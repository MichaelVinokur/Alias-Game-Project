using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [Tooltip("Scenes where music should play (menu/lobby).")]
    public string[] scenesWithMusic = { "MainMenu", "SameRoom", "SameRoomLobby", "SameRoomLobby_edit" ,"LoadingScene"};

    private AudioSource audioSrc;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;

        DontDestroyOnLoad(gameObject);
        audioSrc = GetComponent<AudioSource>();
        audioSrc.mute = PlayerPrefs.GetInt("MusicMuted", 0) == 1;


        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool shouldPlay = false;
        foreach (var s in scenesWithMusic)
        {
            if (scene.name == s) { shouldPlay = true; break; }
        }

        if (shouldPlay) Play();
        else Stop();
    }

    void Play()
    {
        if (!audioSrc) audioSrc = GetComponent<AudioSource>();
        if (!audioSrc.isPlaying) audioSrc.Play();
    }

    void Stop()
    {
        if (!audioSrc) audioSrc = GetComponent<AudioSource>();
        if (audioSrc.isPlaying) audioSrc.Stop();
    }

    public void ToggleMute()
    {
        if (!audioSrc)
            audioSrc = GetComponent<AudioSource>();

        bool newMute = !audioSrc.mute;
        audioSrc.mute = newMute;

        PlayerPrefs.SetInt("MusicMuted", newMute ? 1 : 0);
    }

}

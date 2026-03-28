// ============================================================================
// MusicManager.cs — Persistent background music that swaps tracks per scene
// ============================================================================
// This script auto-initialises itself via [RuntimeInitializeOnLoadMethod].
// No manual setup is needed — it loads AudioClips from Resources/Soundtrack.
//
// Place your audio files in:  Assets/Resources/Soundtrack/
//   • login_mainmenu   (for Login & MainMenu scenes)
//   • maingame         (for the Game scene)
// ============================================================================

using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    private AudioSource audioSource;
    private AudioClip menuClip;
    private AudioClip gameClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInit()
    {
        if (Instance != null) return;

        var go = new GameObject("MusicManager");
        DontDestroyOnLoad(go);
        go.AddComponent<MusicManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Set up AudioSource
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.volume = 0.5f;

        // Load clips from Resources/Soundtrack
        menuClip = Resources.Load<AudioClip>("Soundtrack/login_mainmenu");
        gameClip = Resources.Load<AudioClip>("Soundtrack/maingame");

        if (menuClip == null) Debug.LogWarning("[MusicManager] Could not load Resources/Soundtrack/login_mainmenu");
        if (gameClip == null) Debug.LogWarning("[MusicManager] Could not load Resources/Soundtrack/maingame");

        // Listen for scene changes
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Play for the current scene immediately
        PlayMusicForScene(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        PlayMusicForScene(scene.name);
    }

    private void PlayMusicForScene(string sceneName)
    {
        AudioClip desired = null;

        switch (sceneName)
        {
            case "Login":
            case "MainMenu":
                desired = menuClip;
                break;
            case "Game 1":
            case "Game":
                desired = gameClip;
                break;
            default:
                desired = menuClip; // fallback
                break;
        }

        if (desired == null) return;

        // Don't restart if already playing the same clip
        if (audioSource.clip == desired && audioSource.isPlaying) return;

        audioSource.clip = desired;
        audioSource.Play();
        Debug.Log($"[MusicManager] Now playing: {desired.name} (scene: {sceneName})");
    }

    /// <summary>Set music volume (0–1). Call from a settings UI.</summary>
    public void SetVolume(float volume)
    {
        audioSource.volume = Mathf.Clamp01(volume);
    }

    /// <summary>Mute / unmute.</summary>
    public void SetMuted(bool muted)
    {
        audioSource.mute = muted;
    }
}

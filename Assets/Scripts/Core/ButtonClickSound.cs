// ============================================================================
// ButtonClickSound.cs — Plays a click SFX on every UI Button in the scene
// ============================================================================
// Auto-initialises via [RuntimeInitializeOnLoadMethod]. No manual setup needed.
// Loads the clip from Resources/Soundtrack/ButtonClick.
// Re-scans for buttons each time a scene loads.
// ============================================================================

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonClickSound : MonoBehaviour
{
    public static ButtonClickSound Instance { get; private set; }

    private AudioSource sfxSource;
    private AudioClip clickClip;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInit()
    {
        if (Instance != null) return;

        var go = new GameObject("ButtonClickSound");
        DontDestroyOnLoad(go);
        go.AddComponent<ButtonClickSound>();
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

        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.loop = false;
        sfxSource.playOnAwake = false;
        sfxSource.volume = 1f;

        clickClip = Resources.Load<AudioClip>("Soundtrack/ButtonClick");
        if (clickClip == null)
            Debug.LogWarning("[ButtonClickSound] Could not load Resources/Soundtrack/ButtonClick");

        SceneManager.sceneLoaded += OnSceneLoaded;

        // Hook into current scene immediately (delayed one frame so UI is ready)
        StartCoroutine(HookButtonsDelayed());
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(HookButtonsDelayed());
    }

    private System.Collections.IEnumerator HookButtonsDelayed()
    {
        // Wait one frame so all UI objects are initialised
        yield return null;
        HookAllButtons();
    }

    private void HookAllButtons()
    {
        if (clickClip == null) return;

        // Find ALL buttons, including inactive ones
        var buttons = Resources.FindObjectsOfTypeAll<Button>();
        foreach (var btn in buttons)
        {
            // Skip prefab assets (only hook scene instances)
            if (btn.gameObject.scene.name == null) continue;

            // Avoid adding duplicate listeners by using a marker component
            if (btn.GetComponent<_ButtonClickMarker>() != null) continue;
            btn.gameObject.AddComponent<_ButtonClickMarker>();

            btn.onClick.AddListener(PlayClick);
        }
    }

    private void PlayClick()
    {
        if (sfxSource != null && clickClip != null)
            sfxSource.PlayOneShot(clickClip);
    }

    public void SetVolume(float volume)
    {
        sfxSource.volume = Mathf.Clamp01(volume);
    }
}

/// <summary>Tiny marker to prevent adding duplicate click listeners.</summary>
internal class _ButtonClickMarker : MonoBehaviour { }

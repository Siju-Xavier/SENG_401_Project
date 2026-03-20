// ============================================================================
// MainMenuManager.cs — Controls the Main Menu scene
// ============================================================================
// Attach this to an empty GameObject in the MainMenu scene.
// Wire the four buttons' OnClick() events to the public methods below.
// Drag the Settings and No-Save panels into the Inspector fields.
// ============================================================================

using UnityEngine;
using UnityEngine.SceneManagement;

namespace Presentation
{
    public class MainMenuManager : MonoBehaviour
    {
        /// <summary>Flag checked by GameManager on scene load to restore save.</summary>
        public static bool ShouldLoadSave { get; private set; }

        // ── Inspector References ─────────────────────────────────────────────
        [Header("Panels")]
        [Tooltip("The Settings overlay panel. Set inactive by default in the scene.")]
        [SerializeField] private GameObject settingsPanel;

        [Tooltip("Panel shown when Load Game is clicked but no save exists.")]
        [SerializeField] private GameObject noSavePanel;

        // ── Unity Lifecycle ──────────────────────────────────────────────────
        private void Start()
        {
            // Ensure both panels are hidden on menu open
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (noSavePanel   != null) noSavePanel.SetActive(false);
        }

        // ── Button Handlers ──────────────────────────────────────────────────

        /// <summary>
        /// "Start Game" button — begins a fresh game.
        /// </summary>
        public void StartNewGame()
        {
            Debug.Log("[MainMenu] Starting new game...");
            ShouldLoadSave = false;
            SceneManager.LoadScene("Game");
        }

        /// <summary>
        /// "Load Game" button — loads the most recent save, or shows a
        /// "no save found" message if nothing is stored locally.
        /// </summary>
        public void LoadGame()
        {
            if (Persistence.SaveManager.HasLocalSave())
            {
                Debug.Log("[MainMenu] Save found — loading game...");
                ShouldLoadSave = true;
                // SaveManager will restore state once the Game scene starts.
                SceneManager.LoadScene("Game");
            }
            else
            {
                Debug.LogWarning("[MainMenu] No save file found.");
                if (noSavePanel != null)
                    noSavePanel.SetActive(true);
                else
                    Debug.LogWarning("[MainMenu] noSavePanel is not assigned in the Inspector!");
            }
        }

        /// <summary>
        /// "Settings" button — toggles the settings panel open/closed.
        /// </summary>
        public void OpenSettings()
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(!settingsPanel.activeSelf);
            else
                Debug.LogWarning("[MainMenu] settingsPanel is not assigned in the Inspector!");
        }

        /// <summary>
        /// "Quit" button — exits the application.
        /// </summary>
        public void QuitGame()
        {
            Debug.Log("[MainMenu] Quit called.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Called by the "Close" button inside the No-Save panel.
        /// </summary>
        public void CloseNoSavePanel()
        {
            if (noSavePanel != null) noSavePanel.SetActive(false);
        }
    }
}



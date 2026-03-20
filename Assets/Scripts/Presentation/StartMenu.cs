// ============================================================================
// StartMenu.cs — Thin facade delegating to MainMenuManager
// ============================================================================
// Attach to the same GameObject as MainMenuManager, or any UI element.
// Useful if other systems need to trigger menu actions programmatically.
// ============================================================================

namespace Presentation
{
    using UnityEngine;

    public class StartMenu : MonoBehaviour
    {
        [Tooltip("Reference to the MainMenuManager in the scene.")]
        [SerializeField] private MainMenuManager mainMenuManager;

        private void Awake()
        {
            // Auto-find if not assigned in Inspector
            if (mainMenuManager == null)
                mainMenuManager = FindObjectOfType<MainMenuManager>();
        }

        public void TriggerNewGame()   => mainMenuManager?.StartNewGame();
        public void TriggerLoadGame()  => mainMenuManager?.LoadGame();
        public void TriggerSettings()  => mainMenuManager?.OpenSettings();
        public void TriggerExit()      => mainMenuManager?.QuitGame();
    }
}

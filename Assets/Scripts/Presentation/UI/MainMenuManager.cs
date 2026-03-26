using UnityEngine;

namespace Presentation
{
    public class MainMenuManager : MonoBehaviour
    {
        public static bool ShouldLoadSave { get; set; }

        [Header("Panels")]
        [SerializeField] private GameObject buttonsPanel;
        [SerializeField] private GameObject startPanel;
        [SerializeField] private GameObject loadPanel;
        [SerializeField] private GameObject settingsPanel;

        private void Start()
        {
            ShowButtonsPanel();
        }

        // ── Panel Navigation (Hub-and-Spoke) ─────────────────────────────

        public void ShowButtonsPanel()
        {
            SetPanel(buttonsPanel);
        }

        public void OpenStartPanel()
        {
            SetPanel(startPanel);
        }

        public void OpenLoadPanel()
        {
            SetPanel(loadPanel);
        }

        public void OpenSettings()
        {
            SetPanel(settingsPanel);
        }

        // ── Button Handlers ──────────────────────────────────────────────

        public void StartNewGame()
        {
            Debug.Log("[MainMenu] Opening start panel...");
            OpenStartPanel();
        }

        public void LoadGame()
        {
            Debug.Log("[MainMenu] Opening load panel...");
            OpenLoadPanel();
        }

        public void Logout()
        {
            Debug.Log("[MainMenu] Logging out...");
            SceneLoader.LoadScene("Login");
        }

        public void QuitGame()
        {
            Debug.Log("[MainMenu] Quit called.");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ── Helpers ──────────────────────────────────────────────────────

        private void SetPanel(GameObject activePanel)
        {
            if (buttonsPanel != null) buttonsPanel.SetActive(buttonsPanel == activePanel);
            if (startPanel != null) startPanel.SetActive(startPanel == activePanel);
            if (loadPanel != null) loadPanel.SetActive(loadPanel == activePanel);
            if (settingsPanel != null) settingsPanel.SetActive(settingsPanel == activePanel);
        }
    }
}

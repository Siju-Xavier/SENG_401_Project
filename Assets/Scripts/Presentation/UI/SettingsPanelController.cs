// ============================================================================
// SettingsPanelController.cs — Controls the in-menu Settings panel
// ============================================================================
// Attach this to the SettingsPanel GameObject.
// Wire the VolumeSlider and CloseButton in the Inspector.
// ============================================================================

namespace Presentation
{
    using UnityEngine;
    using UnityEngine.UI;

    public class SettingsPanelController : MonoBehaviour
    {
        // ── Inspector References ─────────────────────────────────────────────
        [Header("Audio")]
        [Tooltip("Slider controlling master volume (0 = mute, 1 = full).")]
        [SerializeField] private Slider volumeSlider;

        [Header("Panel")]
        [Tooltip("Reference to the MainMenuManager for closing the panel.")]
        [SerializeField] private MainMenuManager mainMenuManager;

        // ── Unity Lifecycle ──────────────────────────────────────────────────
        private void OnEnable()
        {
            // Sync slider to current volume whenever the panel is shown
            if (volumeSlider != null)
            {
                volumeSlider.value = AudioListener.volume;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
        }

        private void OnDisable()
        {
            if (volumeSlider != null)
                volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
        }

        // ── Callbacks ────────────────────────────────────────────────────────

        /// <summary>
        /// Called by the VolumeSlider's OnValueChanged event.
        /// Adjusts the global AudioListener volume in real time.
        /// </summary>
        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
            Debug.Log($"[Settings] Master volume set to {value:F2}");
        }

        /// <summary>
        /// "Close" button inside the Settings panel.
        /// Hides this panel (which is also what OpenSettings() toggles).
        /// </summary>
        public void CloseSettings()
        {
            gameObject.SetActive(false);
        }
    }
}

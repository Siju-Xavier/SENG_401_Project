namespace Presentation
{
    using Core;
    using UnityEngine;
    using UnityEngine.UI;

    public class SettingsPanelController : MonoBehaviour
    {
        [Header("Audio")]
        [Tooltip("Slider controlling master volume (0 = mute, 1 = full).")]
        [SerializeField] private Slider volumeSlider;

        [Header("Graphics")]
        [Tooltip("Toggle to enable/disable cloud rendering.")]
        [SerializeField] private Toggle cloudToggle;

        private void OnEnable()
        {
            if (volumeSlider != null)
            {
                volumeSlider.value = AudioListener.volume;
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }

            if (cloudToggle != null)
            {
                var orchestrator = FindFirstObjectByType<MapGenerationOrchestrator>();
                if (orchestrator != null)
                    cloudToggle.isOn = orchestrator.enableClouds;
                cloudToggle.onValueChanged.AddListener(OnCloudToggleChanged);
            }
        }

        private void OnDisable()
        {
            if (volumeSlider != null)
                volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            if (cloudToggle != null)
                cloudToggle.onValueChanged.RemoveListener(OnCloudToggleChanged);
        }

        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
            Debug.Log($"[Settings] Master volume set to {value:F2}");
        }

        private void OnCloudToggleChanged(bool value)
        {
            var orchestrator = FindFirstObjectByType<MapGenerationOrchestrator>();
            if (orchestrator != null)
            {
                orchestrator.enableClouds = value;
                Debug.Log($"[Settings] Clouds {(value ? "enabled" : "disabled")}");
            }
        }

        public void CloseSettings()
        {
            gameObject.SetActive(false);
        }
    }
}

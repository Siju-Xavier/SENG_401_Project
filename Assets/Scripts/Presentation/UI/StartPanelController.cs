namespace Presentation
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    public class StartPanelController : MonoBehaviour
    {
        [Header("Game Settings")]
        [SerializeField] private TMP_InputField seedInput;
        [SerializeField] private TMP_Dropdown mapSizeDropdown;
        [SerializeField] private TMP_Dropdown citiesDropdown;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button backButton;

        // Static values for GameManager/MapGenerationOrchestrator to read
        public static int SelectedSeed { get; private set; }
        public static int SelectedMapWidth { get; private set; } = 128;
        public static int SelectedMapHeight { get; private set; } = 128;
        public static int SelectedCityCount { get; private set; } = 1;
        public static bool UseRandomSeed { get; private set; } = true;

        private readonly int[] mapWidths = { 128, 256, 512 };
        private readonly int[] mapHeights = { 128, 256, 512 };

        private void OnEnable()
        {
            // Set up dropdown options if not already populated
            if (mapSizeDropdown != null && mapSizeDropdown.options.Count == 0)
            {
                mapSizeDropdown.ClearOptions();
                mapSizeDropdown.AddOptions(new System.Collections.Generic.List<string>
                    { "Small (128x128)", "Medium (256x256)", "Large (512x512)" });
                mapSizeDropdown.value = 0;
            }

            if (citiesDropdown != null && citiesDropdown.options.Count == 0)
            {
                citiesDropdown.ClearOptions();
                citiesDropdown.AddOptions(new System.Collections.Generic.List<string>
                    { "1 City", "2 Cities", "3 Cities" });
                citiesDropdown.value = 0;
            }

            // Ensure dropdown lists render on top of buttons below them
            EnsureDropdownRendersOnTop(mapSizeDropdown);
            EnsureDropdownRendersOnTop(citiesDropdown);

            if (startButton != null)
                startButton.onClick.AddListener(OnStart);
        }

        private void EnsureDropdownRendersOnTop(TMP_Dropdown dropdown)
        {
            if (dropdown == null) return;
            var template = dropdown.template;
            if (template != null)
            {
                // Override sorting so dropdown renders above sibling buttons
                var canvas = template.GetComponent<Canvas>();
                if (canvas == null) canvas = template.gameObject.AddComponent<Canvas>();
                canvas.overrideSorting = true;
                canvas.sortingOrder = 100;

                if (template.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                    template.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();

                // Flip the dropdown to expand upward instead of downward
                // by changing the pivot to bottom and anchoring to top of the dropdown
                template.pivot = new Vector2(0.5f, 0f);
            }
        }

        private void OnDisable()
        {
            if (startButton != null)
                startButton.onClick.RemoveListener(OnStart);
        }

        private void OnStart()
        {
            // Parse seed
            string seedText = seedInput != null ? seedInput.text.Trim() : "";
            if (string.IsNullOrEmpty(seedText))
            {
                UseRandomSeed = true;
                SelectedSeed = Random.Range(0, int.MaxValue);
            }
            else
            {
                UseRandomSeed = false;
                if (!int.TryParse(seedText, out int parsed))
                    parsed = seedText.GetHashCode();
                SelectedSeed = parsed;
            }

            // Parse map size
            int sizeIdx = mapSizeDropdown != null ? mapSizeDropdown.value : 0;
            sizeIdx = Mathf.Clamp(sizeIdx, 0, mapWidths.Length - 1);
            SelectedMapWidth = mapWidths[sizeIdx];
            SelectedMapHeight = mapHeights[sizeIdx];

            // Parse city count
            int cityIdx = citiesDropdown != null ? citiesDropdown.value : 0;
            SelectedCityCount = cityIdx + 1;

            Debug.Log($"[StartPanel] Seed={SelectedSeed} (random={UseRandomSeed}), Map={SelectedMapWidth}x{SelectedMapHeight}, Cities={SelectedCityCount}");

            MainMenuManager.ShouldLoadSave = false;
            SceneLoader.LoadScene("Game 1");
        }
    }
}

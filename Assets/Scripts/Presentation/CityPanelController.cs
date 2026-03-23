namespace Presentation
{
    using UnityEngine;
    using TMPro;
    using UnityEngine.UI;
    using GameState;
    using BusinessLogic;

    public class CityPanelController : MonoBehaviour
    {
        public static CityPanelController Instance { get; private set; }

        [Header("UI References")]
        [SerializeField] private GameObject panelRoot;
        [SerializeField] private TextMeshProUGUI cityNameText;
        [SerializeField] private TextMeshProUGUI statsText;
        [SerializeField] private Button sendFirefighterButton;
        [SerializeField] private Button policyButton;
        [SerializeField] private Button closeButton;

        private City currentCity;
        private ResourceManager resourceManager;

        private void Awake()
        {
            Debug.Log($"[CityPanelController] Awake running on {gameObject.name}");
            if (Instance != null && Instance != this) {
                Debug.Log($"[CityPanelController] Duplicate found on {gameObject.name}, destroying. Instance is on {Instance.gameObject.name}");
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (panelRoot != null) panelRoot.SetActive(false);

            if (closeButton != null) closeButton.onClick.AddListener(HidePanel);
            if (sendFirefighterButton != null) sendFirefighterButton.onClick.AddListener(OnSendFirefighterClicked);
            if (policyButton != null) policyButton.onClick.AddListener(OnPolicyClicked);
        }

        private void Start()
        {
            resourceManager = FindFirstObjectByType<ResourceManager>();
        }

        public void ShowPanel(City city)
        {
            Debug.Log($"[CityPanelController] ShowPanel triggered for city: {(city != null ? city.CityName : "null")}");
            if (city == null) return;
            currentCity = city;

            if (cityNameText != null) cityNameText.text = city.CityName;
            else Debug.LogWarning("[CityPanelController] cityNameText is null!");

            if (statsText != null) statsText.text = $"Budget: ${city.Budget}\nReputation: {city.Reputation}";
            else Debug.LogWarning("[CityPanelController] statsText is null!");

            if (panelRoot != null) {
                panelRoot.SetActive(true);
                Debug.Log($"[CityPanelController] panelRoot set to active. Current parent: {panelRoot.transform.parent.name}");
            } else {
                Debug.LogWarning("[CityPanelController] panelRoot is null! Cannot activate the panel.");
            }
        }

        public void HidePanel()
        {
            currentCity = null;
            if (panelRoot != null) panelRoot.SetActive(false);
            
            // Clear borders when panel is closed
            if (TerritoryRenderer.Instance != null)
            {
                TerritoryRenderer.Instance.ClearBorders();
            }
        }

        private void OnSendFirefighterClicked()
        {
            if (currentCity == null) return;
            Debug.Log($"[CityPanel] Sending firefighter from {currentCity.CityName}");
            // Future logic: Deploy firefighter from city budget/pool
            // resourceManager?.DeployFirefighter(...);
        }

        private void OnPolicyClicked()
        {
            if (currentCity == null) return;
            Debug.Log($"[CityPanel] Opening policy menu for {currentCity.CityName}");
            // Future logic: Trigger policy UI or enact 'Fire Ban'
        }
    }
}

namespace Presentation
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using Persistence;

    public class LoadPanelController : MonoBehaviour
    {
        [Header("Local Saves")]
        [SerializeField] private Transform localSavesContent;
        [SerializeField] private GameObject localNoSavesText;

        [Header("Online Saves")]
        [SerializeField] private Transform onlineSavesContent;
        [SerializeField] private GameObject onlineNoSavesText;

        [Header("Buttons")]
        [SerializeField] private Button backButton;

        private void OnEnable()
        {
            PopulateLocalSaves();
            PopulateOnlineSaves();
        }

        private void PopulateLocalSaves()
        {
            // Clear existing entries (skip the no-saves text)
            if (localSavesContent != null)
            {
                for (int i = localSavesContent.childCount - 1; i >= 0; i--)
                    Destroy(localSavesContent.GetChild(i).gameObject);
            }

            if (LocalFileProvider.HasLocalSave())
            {
                if (localNoSavesText != null) localNoSavesText.SetActive(false);

                // Show one entry for the local save
                string savePath = LocalFileProvider.SaveFilePath;
                string lastModified = System.IO.File.GetLastWriteTime(savePath).ToString("yyyy-MM-dd HH:mm");
                CreateSaveEntry(localSavesContent, "Local Save", lastModified, () => OnLoadLocal());
            }
            else
            {
                if (localNoSavesText != null) localNoSavesText.SetActive(true);
            }
        }

        private void PopulateOnlineSaves()
        {
            // Clear existing entries
            if (onlineSavesContent != null)
            {
                for (int i = onlineSavesContent.childCount - 1; i >= 0; i--)
                    Destroy(onlineSavesContent.GetChild(i).gameObject);
            }

            // Online saves: stub for now
            if (onlineNoSavesText != null) onlineNoSavesText.SetActive(true);
        }

        private void CreateSaveEntry(Transform parent, string saveName, string dateText, System.Action onLoad)
        {
            if (parent == null) return;

            var entry = new GameObject("SaveEntry", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            entry.transform.SetParent(parent, false);
            entry.layer = 5;

            var hlg = entry.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 15;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;

            var entryRect = entry.GetComponent<RectTransform>();
            entryRect.sizeDelta = new Vector2(0, 50);

            var le = entry.AddComponent<LayoutElement>();
            le.preferredHeight = 50;

            // Save name text
            var nameGO = new GameObject("Name", typeof(RectTransform), typeof(TextMeshProUGUI));
            nameGO.transform.SetParent(entry.transform, false);
            nameGO.layer = 5;
            var nameTMP = nameGO.GetComponent<TextMeshProUGUI>();
            nameTMP.text = saveName;
            nameTMP.fontSize = 30;
            nameTMP.color = Color.white;
            var nameRect = nameGO.GetComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(250, 50);

            // Date text
            var dateGO = new GameObject("Date", typeof(RectTransform), typeof(TextMeshProUGUI));
            dateGO.transform.SetParent(entry.transform, false);
            dateGO.layer = 5;
            var dateTMP = dateGO.GetComponent<TextMeshProUGUI>();
            dateTMP.text = dateText;
            dateTMP.fontSize = 24;
            dateTMP.color = new Color(0.7f, 0.7f, 0.7f, 1f);
            var dateRect = dateGO.GetComponent<RectTransform>();
            dateRect.sizeDelta = new Vector2(200, 50);

            // Load button
            var btnGO = new GameObject("LoadButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGO.transform.SetParent(entry.transform, false);
            btnGO.layer = 5;
            var btnImg = btnGO.GetComponent<Image>();
            btnImg.color = new Color(1f, 0.302f, 0.302f, 1f);
            var btnRect = btnGO.GetComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(120, 45);
            var btn = btnGO.GetComponent<Button>();
            btn.onClick.AddListener(() => onLoad());

            var btnOutline = btnGO.AddComponent<Outline>();
            btnOutline.effectColor = Color.black;
            btnOutline.effectDistance = new Vector2(3, -3);

            var btnTextGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            btnTextGO.transform.SetParent(btnGO.transform, false);
            btnTextGO.layer = 5;
            var btnTMP = btnTextGO.GetComponent<TextMeshProUGUI>();
            btnTMP.text = "LOAD";
            btnTMP.fontSize = 28;
            btnTMP.fontStyle = FontStyles.Bold;
            btnTMP.color = Color.black;
            btnTMP.alignment = TextAlignmentOptions.Center;
            var btnTextRect = btnTextGO.GetComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;
        }

        private void OnLoadLocal()
        {
            Debug.Log("[LoadPanel] Loading local save...");
            MainMenuManager.ShouldLoadSave = true;
            SceneLoader.LoadScene("Game 1");
        }
    }
}

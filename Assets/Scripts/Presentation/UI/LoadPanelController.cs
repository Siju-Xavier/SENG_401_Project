namespace Presentation
{
    using System.Collections;
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

        private SaveManager saveManager;

        private void OnEnable()
        {
            saveManager = FindFirstObjectByType<SaveManager>();
            PopulateLocalSaves();
            PopulateOnlineSaves();
        }

        // ── Local Saves ─────────────────────────────────────────────────

        private void PopulateLocalSaves()
        {
            ClearChildren(localSavesContent);

            var slots = LocalFileProvider.GetSaveSlots();

            // Also check the legacy single save file
            bool hasLegacy = System.IO.File.Exists(LocalFileProvider.SaveFilePath);

            if (slots.Length == 0 && !hasLegacy)
            {
                if (localNoSavesText != null) localNoSavesText.SetActive(true);
                return;
            }

            if (localNoSavesText != null) localNoSavesText.SetActive(false);

            // Show named save slots (newest first — already sorted)
            foreach (var filePath in slots)
            {
                // Try to read the save name from the JSON
                string displayName = GetSaveNameFromFile(filePath);
                string lastModified = System.IO.File.GetLastWriteTime(filePath).ToString("yyyy-MM-dd HH:mm");
                string path = filePath; // capture for closure
                CreateSaveEntry(localSavesContent, displayName, lastModified, () => OnLoadLocalSlot(path));
            }

            // Show legacy single save if it exists and no slots found
            if (hasLegacy && slots.Length == 0)
            {
                string lastModified = System.IO.File.GetLastWriteTime(LocalFileProvider.SaveFilePath).ToString("yyyy-MM-dd HH:mm");
                CreateSaveEntry(localSavesContent, "Local Save", lastModified, () => OnLoadLocal());
            }
        }

        private string GetSaveNameFromFile(string filePath)
        {
            try
            {
                string json = System.IO.File.ReadAllText(filePath);
                var data = JsonUtility.FromJson<SaveManager.GameSaveData>(json);
                if (data != null && !string.IsNullOrEmpty(data.saveName))
                    return data.saveName;
            }
            catch { }

            // Fallback to filename without extension
            return System.IO.Path.GetFileNameWithoutExtension(filePath);
        }

        private void OnLoadLocalSlot(string filePath)
        {
            Debug.Log($"[LoadPanel] Loading local slot: {filePath}");
            Debug.Log($"[LoadPanel] File exists: {System.IO.File.Exists(filePath)}");

            string json = LocalFileProvider.LoadSlot(filePath);
            if (string.IsNullOrEmpty(json))
            {
                Debug.LogWarning($"[LoadPanel] File is empty or unreadable: {filePath}");
                return;
            }

            Debug.Log($"[LoadPanel] JSON length: {json.Length}, preview: {json.Substring(0, Mathf.Min(200, json.Length))}");

            var sm = FindFirstObjectByType<SaveManager>();
            if (sm != null)
            {
                var data = sm.Deserialize(json);
                if (data != null)
                {
                    SaveManager.PendingLoadData = data;
                    MainMenuManager.ShouldLoadSave = true;
                    SceneLoader.LoadScene("Game 1");
                    return;
                }
                Debug.LogWarning("[LoadPanel] Deserialize returned null.");
            }
            else
            {
                Debug.LogWarning("[LoadPanel] SaveManager not found.");
            }
        }

        private void OnLoadLocal()
        {
            Debug.Log("[LoadPanel] Loading legacy local save...");
            MainMenuManager.ShouldLoadSave = true;
            SceneLoader.LoadScene("Game 1");
        }

        // ── Cloud Saves ─────────────────────────────────────────────────

        private void PopulateOnlineSaves()
        {
            ClearChildren(onlineSavesContent);

            var db = DatabaseProvider.Instance;
            if (db == null || !db.IsConfigured)
            {
                if (onlineNoSavesText != null) onlineNoSavesText.SetActive(true);
                return;
            }

            // Show loading state
            if (onlineNoSavesText != null)
            {
                var tmp = onlineNoSavesText.GetComponent<TextMeshProUGUI>();
                if (tmp != null) tmp.text = "Loading cloud saves...";
                onlineNoSavesText.SetActive(true);
            }

            StartCoroutine(FetchCloudSaves(db));
        }

        private IEnumerator FetchCloudSaves(DatabaseProvider db)
        {
            string rawJson = null;
            yield return db.LoadAllSaves(db.ActivePlayerId, json => rawJson = json);

            ClearChildren(onlineSavesContent);

            if (string.IsNullOrEmpty(rawJson) || rawJson == "[]")
            {
                if (onlineNoSavesText != null)
                {
                    var tmp = onlineNoSavesText.GetComponent<TextMeshProUGUI>();
                    if (tmp != null) tmp.text = "No cloud saves found.";
                    onlineNoSavesText.SetActive(true);
                }
                yield break;
            }

            if (onlineNoSavesText != null) onlineNoSavesText.SetActive(false);

            // Parse the array of save records from Supabase
            var saves = ParseCloudSaves(rawJson);
            foreach (var save in saves)
            {
                string gameState = save.gameState;
                CreateSaveEntry(onlineSavesContent, save.displayName, save.savedAt, () => OnLoadCloud(gameState));
            }
        }

        private struct CloudSaveInfo
        {
            public string displayName;
            public string savedAt;
            public string gameState;
        }

        private System.Collections.Generic.List<CloudSaveInfo> ParseCloudSaves(string jsonArray)
        {
            var results = new System.Collections.Generic.List<CloudSaveInfo>();

            // Simple JSON array parsing (Supabase returns [{...}, {...}])
            // Each object has: save_display_name, saved_at, game_state
            try
            {
                // Strip outer brackets
                jsonArray = jsonArray.Trim();
                if (jsonArray.StartsWith("[")) jsonArray = jsonArray.Substring(1);
                if (jsonArray.EndsWith("]")) jsonArray = jsonArray.Substring(0, jsonArray.Length - 1);

                // Split by objects — find each { ... } block
                int depth = 0;
                int start = -1;
                for (int i = 0; i < jsonArray.Length; i++)
                {
                    if (jsonArray[i] == '{') { if (depth == 0) start = i; depth++; }
                    else if (jsonArray[i] == '}')
                    {
                        depth--;
                        if (depth == 0 && start >= 0)
                        {
                            string obj = jsonArray.Substring(start, i - start + 1);
                            var info = new CloudSaveInfo
                            {
                                displayName = ExtractJsonString(obj, "save_display_name"),
                                savedAt = ExtractJsonString(obj, "saved_at"),
                                gameState = ExtractJsonObject(obj, "game_state")
                            };
                            if (string.IsNullOrEmpty(info.displayName))
                                info.displayName = $"Cloud Save";
                            if (!string.IsNullOrEmpty(info.savedAt) && info.savedAt.Length > 16)
                                info.savedAt = info.savedAt.Substring(0, 16).Replace("T", " ");
                            results.Add(info);
                            start = -1;
                        }
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[LoadPanel] Failed to parse cloud saves: {e.Message}");
            }

            return results;
        }

        private string ExtractJsonString(string json, string key)
        {
            string search = $"\"{key}\":\"";
            int idx = json.IndexOf(search);
            if (idx < 0) return "";
            int start = idx + search.Length;
            int end = json.IndexOf("\"", start);
            return end > start ? json.Substring(start, end - start) : "";
        }

        private string ExtractJsonObject(string json, string key)
        {
            string search = $"\"{key}\":";
            int idx = json.IndexOf(search);
            if (idx < 0) return "";
            int start = idx + search.Length;
            // Find the matching closing brace
            int depth = 0;
            for (int i = start; i < json.Length; i++)
            {
                if (json[i] == '{') depth++;
                else if (json[i] == '}')
                {
                    depth--;
                    if (depth == 0) return json.Substring(start, i - start + 1);
                }
            }
            return "";
        }

        private void OnLoadCloud(string gameStateJson)
        {
            Debug.Log($"[LoadPanel] Loading cloud save... JSON length: {gameStateJson?.Length ?? 0}");
            if (string.IsNullOrEmpty(gameStateJson))
            {
                Debug.LogWarning("[LoadPanel] Cloud save game_state is empty.");
                return;
            }

            Debug.Log($"[LoadPanel] Cloud JSON preview: {gameStateJson.Substring(0, Mathf.Min(200, gameStateJson.Length))}");

            var sm = FindFirstObjectByType<SaveManager>();
            if (sm != null)
            {
                try
                {
                    var data = sm.Deserialize(gameStateJson);
                    if (data != null)
                    {
                        SaveManager.PendingLoadData = data;
                        MainMenuManager.ShouldLoadSave = true;
                        SceneLoader.LoadScene("Game 1");
                        return;
                    }
                    Debug.LogWarning("[LoadPanel] Cloud deserialize returned null.");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[LoadPanel] Cloud deserialize error: {e.Message}");
                }
            }
        }

        // ── UI Helpers ──────────────────────────────────────────────────

        private void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
                Destroy(parent.GetChild(i).gameObject);
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
    }
}

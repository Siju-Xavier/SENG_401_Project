namespace Presentation
{
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;
    using Persistence;

    public class SavePanelController : MonoBehaviour
    {
        [Header("Online Save")]
        [SerializeField] private TMP_InputField onlineSaveNameInput;
        [SerializeField] private Button onlineSaveButton;

        [Header("Local Save")]
        [SerializeField] private TMP_InputField localSaveNameInput;
        [SerializeField] private Button localSaveButton;

        private SaveManager saveManager;

        private void OnEnable()
        {
            saveManager = FindFirstObjectByType<SaveManager>();

            if (onlineSaveButton != null)
                onlineSaveButton.onClick.AddListener(OnOnlineSave);
            if (localSaveButton != null)
                localSaveButton.onClick.AddListener(OnLocalSave);
        }

        private void OnDisable()
        {
            if (onlineSaveButton != null)
                onlineSaveButton.onClick.RemoveListener(OnOnlineSave);
            if (localSaveButton != null)
                localSaveButton.onClick.RemoveListener(OnLocalSave);
        }

        private void OnOnlineSave()
        {
            if (saveManager == null) return;
            string saveName = onlineSaveNameInput != null ? onlineSaveNameInput.text : "CloudSave";
            Debug.Log($"[SavePanel] Online save as: {saveName}");
            saveManager.SetStorageMode(StorageMode.Cloud);
            saveManager.SaveFile();
        }

        private void OnLocalSave()
        {
            if (saveManager == null) return;
            string saveName = localSaveNameInput != null ? localSaveNameInput.text : "LocalSave";
            Debug.Log($"[SavePanel] Local save as: {saveName}");
            saveManager.SetStorageMode(StorageMode.Local);
            saveManager.SaveFile();
        }
    }
}

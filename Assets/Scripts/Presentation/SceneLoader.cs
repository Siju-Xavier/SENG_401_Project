using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Presentation
{
    public class SceneLoader : MonoBehaviour
    {
        public static string SceneToLoad;

        [SerializeField] private Slider progressBar;
        [SerializeField] private TextMeshProUGUI progressText;

        // Visual simulation to ensure testing shows a fake loading duration if loading finishes too quickly
        [SerializeField] private float minimumLoadingTime = 1f; 

        private void Start()
        {
            Debug.Log($"[SceneLoader] Start. SceneToLoad is '{SceneToLoad}'");
            if (!string.IsNullOrEmpty(SceneToLoad))
            {
                StartCoroutine(LoadSceneAsync(SceneToLoad));
            }
            else
            {
                // Fallback for testing directly in the scene
                if (progressText != null) progressText.text = "LOADING...";
                Debug.LogWarning("[SceneLoader] No scene specified to load.");
            }
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            Debug.Log($"[SceneLoader] Async loading scene: {sceneName}");
            float elapsedTime = 0f;
            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            if (operation == null)
            {
                Debug.LogError($"[SceneLoader] Failed to load scene: {sceneName}");
                yield break;
            }
            operation.allowSceneActivation = false;

            while (!operation.isDone)
            {
                elapsedTime += Time.deltaTime;
                
                // Unity's progress stops at 0.9 when allowSceneActivation is false
                float loadProgress = Mathf.Clamp01(operation.progress / 0.9f);
                float simulatedProgress = Mathf.Clamp01(elapsedTime / minimumLoadingTime);
                
                // Which one is slower? We want to wait for both actual load AND simulated time
                float displayProgress = Mathf.Min(loadProgress, simulatedProgress);

                if (progressBar != null) progressBar.value = displayProgress;
                if (progressText != null) progressText.text = $"LOADING... {(int)(displayProgress * 100)}%";

                if (operation.progress >= 0.9f && elapsedTime >= minimumLoadingTime)
                {
                    if (progressBar != null) progressBar.value = 1f;
                    if (progressText != null) progressText.text = "LOADING... 100%";
                    operation.allowSceneActivation = true;
                }

                yield return null;
            }
        }

        public static void LoadScene(string sceneName)
        {
            Debug.Log($"[SceneLoader] Transition request to: {sceneName}");
            SceneToLoad = sceneName;
            SceneManager.LoadScene("LoadingScene");
        }
    }
}

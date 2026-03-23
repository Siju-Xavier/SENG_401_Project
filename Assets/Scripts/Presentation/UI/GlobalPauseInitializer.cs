
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

namespace Presentation
{
    /// <summary>
    /// This script ensures that the Pause Menu is always injected into the 
    /// Game scene at runtime, even if the scene file is out of sync.
    /// </summary>
    public static class GlobalPauseInitializer
    {
        private static bool _initialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _initialized = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        public static void Initialize()
        {
            if (_initialized) return;
            _initialized = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
            CheckAndInit(SceneManager.GetActiveScene());
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            CheckAndInit(scene);
        }

        private static void CheckAndInit(Scene scene)
        {
            if (scene.name != "Game") return;

            Debug.Log("[GlobalPause] Initializing Pause Menu in Game scene...");

            // 1. Ensure EventSystem exists and has controller
            var esGo = GameObject.Find("EventSystem");
            if (esGo == null)
            {
                esGo = new GameObject("EventSystem");
                esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGo.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            }

            var pauseCtrl = esGo.GetComponent<PauseMenuController>();
            if (pauseCtrl == null) pauseCtrl = esGo.AddComponent<PauseMenuController>();

            // 2. Ensure Canvas exists
            var canvasGo = GameObject.Find("Canvas");
            if (canvasGo == null)
            {
                canvasGo = new GameObject("Canvas");
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            // 3. Ensure UI elements exist (PauseMenuController.Start() will wire them)
            Transform cT = canvasGo.transform;
            if (cT.Find("HUDPanel") == null) new GameObject("HUDPanel").transform.SetParent(cT, false);
            if (cT.Find("PauseButton") == null) {
                var go = new GameObject("PauseButton");
                go.transform.SetParent(cT, false);
                go.AddComponent<Button>();
            }
            if (cT.Find("PausePanel") == null) {
                var go = new GameObject("PausePanel");
                go.transform.SetParent(cT, false);
                go.SetActive(false);
                
                var res = new GameObject("ResumeButton");
                res.transform.SetParent(go.transform, false);
                res.AddComponent<Button>();
            }

            Debug.Log("[GlobalPause] Initialization complete.");
        }
    }
}

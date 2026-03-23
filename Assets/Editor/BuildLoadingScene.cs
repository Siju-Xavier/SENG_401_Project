using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Presentation;
using System.Linq;
using System.Collections.Generic;

[InitializeOnLoad]
public class BuildLoadingScene
{
    static BuildLoadingScene()
    {
        EditorApplication.delayCall += Execute;
    }

    private static void Execute()
    {
        string scenePath = "Assets/Scenes/LoadingScene.unity";
        if (!System.IO.File.Exists(scenePath))
        {
            // Build the scene only if it doesn't exist
            BuildScene(scenePath);
        }

        // Always ensure it's in the build settings
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Exists(s => s.path == scenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log($"[BuildLoadingScene] Added {scenePath} to Build Settings.");
        }
    }

    private static void BuildScene(string scenePath)
    {
        // Open a new scene
        Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        // ... (rest of the creation logic)

        // Main Camera
        GameObject camGo = new GameObject("Main Camera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.968f, 0.709f, 0.125f); // Pop-art Orangish Yellow
        camGo.tag = "MainCamera";

        // Canvas
        GameObject canvasGo = new GameObject("Canvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGo.AddComponent<GraphicRaycaster>();

        // Background decorative shapes (Pop-Art touches - optional)

        // Loading Text
        GameObject textGo = new GameObject("LoadingText");
        textGo.transform.SetParent(canvasGo.transform, false);
        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = "LOADING... 0%";
        text.fontSize = 100;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        // Text Outline via component
        textGo.AddComponent<UnityEngine.UI.Outline>().effectColor = Color.black;
        textGo.GetComponent<UnityEngine.UI.Outline>().effectDistance = new Vector2(5, -5);
        
        RectTransform textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0, 150);
        textRect.sizeDelta = new Vector2(1000, 200);

        // Progress Bar Background Outline (Pop art black outline)
        GameObject bgGo = new GameObject("ProgressBackground");
        bgGo.transform.SetParent(canvasGo.transform, false);
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = Color.black;
        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = new Vector2(0, -50);
        bgRect.sizeDelta = new Vector2(1020, 80);

        // Progress Bar Inner Empty (White)
        GameObject innerGo = new GameObject("ProgressInner");
        innerGo.transform.SetParent(bgGo.transform, false);
        Image innerImg = innerGo.AddComponent<Image>();
        innerImg.color = Color.white;
        RectTransform innerRect = innerGo.GetComponent<RectTransform>();
        innerRect.anchorMin = new Vector2(0, 0);
        innerRect.anchorMax = new Vector2(1, 1);
        innerRect.offsetMin = new Vector2(10, 10);
        innerRect.offsetMax = new Vector2(-10, -10);

        // Progress Bar Fill Area
        GameObject fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(innerGo.transform, false);
        RectTransform fillAreaRect = fillAreaGo.AddComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0, 0);
        fillAreaRect.anchorMax = new Vector2(1, 1);
        fillAreaRect.offsetMin = Vector2.zero;
        fillAreaRect.offsetMax = Vector2.zero;

        // Progress Bar Fill Color (Red)
        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        Image fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.9f, 0.2f, 0.2f); // Retro red
        RectTransform fillRect = fillGo.GetComponent<RectTransform>();
        fillRect.anchorMin = new Vector2(0, 0);
        fillRect.anchorMax = new Vector2(0, 1); // 0 width initially
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        // Add Slider component to Background
        Slider slider = bgGo.AddComponent<Slider>();
        slider.interactable = false;
        slider.transition = Selectable.Transition.None;
        slider.fillRect = fillRect;
        slider.minValue = 0;
        slider.maxValue = 1;

        // SceneLoader Manager
        GameObject loaderGo = new GameObject("SceneLoader");
        SceneLoader loader = loaderGo.AddComponent<SceneLoader>();
        
        // Use reflection since fields might be private
        var type = typeof(SceneLoader);
        type.GetField("progressBar", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(loader, slider);
        type.GetField("progressText", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).SetValue(loader, text);

        EditorSceneManager.SaveScene(newScene, scenePath);
        Debug.Log("[BuildLoadingScene] Successfully created LoadingScene.unity");

        // Add to build settings
        var scenes = EditorBuildSettings.scenes.ToList();
        if (!scenes.Exists(s => s.path == scenePath))
        {
            scenes.Add(new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }
    }
}

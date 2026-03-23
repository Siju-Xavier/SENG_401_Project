using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Presentation;
using System.Linq;

public class BuildPauseMenu
{
    [MenuItem("Tools/Build Pop-Art Pause Menu")]
    public static void BuildMenu()
    {
        // Get the active scene
        Scene activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.StartsWith("Game"))
        {
            Debug.LogWarning("Please open a Game scene (like Game 1) before running this tool.");
            return;
        }

        // Cleanup previous generations to avoid duplicates
        var oldBtn = GameObject.Find("PopArt_PauseButton");
        var oldPanel = GameObject.Find("PopArt_PausePanel");
        var oldSettings = GameObject.Find("PopArt_SettingsPanel");
        if (oldBtn != null) Object.DestroyImmediate(oldBtn);
        if (oldPanel != null) Object.DestroyImmediate(oldPanel);
        if (oldSettings != null) Object.DestroyImmediate(oldSettings);

        // Find or create Canvas
        Canvas canvas = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (canvas == null)
        {
            GameObject canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            canvasGo.AddComponent<GraphicRaycaster>();
        }

        // Find or create PauseMenuController
        PauseMenuController pmc = Object.FindObjectsByType<PauseMenuController>(FindObjectsInactive.Include, FindObjectsSortMode.None).FirstOrDefault();
        if (pmc == null)
        {
            GameObject pmcGo = new GameObject("PauseMenuController");
            pmc = pmcGo.AddComponent<PauseMenuController>();
        }

        // Create Pause Button (Top Right)
        GameObject pauseBtnGo = new GameObject("PopArt_PauseButton");
        pauseBtnGo.transform.SetParent(canvas.transform, false);
        RectTransform pbRect = pauseBtnGo.AddComponent<RectTransform>();
        pbRect.anchorMin = new Vector2(1, 1);
        pbRect.anchorMax = new Vector2(1, 1);
        pbRect.pivot = new Vector2(1, 1);
        pbRect.anchoredPosition = new Vector2(-15, -15); // Tight to corner
        pbRect.sizeDelta = new Vector2(70, 70);

        Image pbImg = pauseBtnGo.AddComponent<Image>();
        pbImg.color = new Color(0.9f, 0.4f, 0.1f); // Orange
        Button pButton = pauseBtnGo.AddComponent<Button>();
        ComicButtonVisuals pbVis = pauseBtnGo.AddComponent<ComicButtonVisuals>();
        
        // Shadow for Pause Button
        GameObject pbShadow = new GameObject("Shadow");
        pbShadow.transform.SetParent(pauseBtnGo.transform, false);
        pbShadow.transform.SetAsFirstSibling(); // Behind
        RectTransform pbsRect = pbShadow.AddComponent<RectTransform>();
        pbsRect.anchorMin = Vector2.zero; pbsRect.anchorMax = Vector2.one;
        pbsRect.offsetMin = new Vector2(10, -10); pbsRect.offsetMax = new Vector2(10, -10);
        Image pbsImg = pbShadow.AddComponent<Image>();
        pbsImg.color = Color.black;

        // Front Face container (for ComicButtonVisuals)
        GameObject pbFront = new GameObject("Front");
        pbFront.transform.SetParent(pauseBtnGo.transform, false);
        RectTransform pbfRect = pbFront.AddComponent<RectTransform>();
        pbfRect.anchorMin = Vector2.zero; pbfRect.anchorMax = Vector2.one;
        pbfRect.offsetMin = Vector2.zero; pbfRect.offsetMax = Vector2.zero;
        Image pbfImg = pbFront.AddComponent<Image>();
        pbfImg.color = new Color(0.968f, 0.709f, 0.125f);
        pbfImg.GetComponent<Image>().type = Image.Type.Simple;
        pbFront.AddComponent<UnityEngine.UI.Outline>().effectColor = Color.black;
        pbFront.GetComponent<UnityEngine.UI.Outline>().effectDistance = new Vector2(4, -4);
        pbVis.frontFace = pbfRect;
        pbVis.pressOffset = new Vector2(5, -5);

        // Text "||"
        GameObject pbtGo = new GameObject("Text");
        pbtGo.transform.SetParent(pbFront.transform, false);
        RectTransform pbtRect = pbtGo.AddComponent<RectTransform>();
        pbtRect.anchorMin = Vector2.zero; pbtRect.anchorMax = Vector2.one;
        pbtRect.offsetMin = Vector2.zero; pbtRect.offsetMax = Vector2.zero;
        TextMeshProUGUI pbtTxt = pbtGo.AddComponent<TextMeshProUGUI>();
        pbtTxt.text = "||";
        pbtTxt.fontSize = 40;
        pbtTxt.color = Color.black;
        pbtTxt.alignment = TextAlignmentOptions.Center;
        pbtTxt.fontStyle = FontStyles.Bold;

        // Create Pause Panel
        GameObject panelGo = new GameObject("PopArt_PausePanel");
        panelGo.transform.SetParent(canvas.transform, false);
        RectTransform panelRect = panelGo.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
        Image panelImg = panelGo.AddComponent<Image>();
        panelImg.color = new Color(0, 0, 0, 0.7f); // Transparent dark overlay

        // Panel Container
        GameObject containerGo = new GameObject("Container");
        containerGo.transform.SetParent(panelGo.transform, false);
        RectTransform contRect = containerGo.AddComponent<RectTransform>();
        contRect.anchorMin = new Vector2(0.5f, 0.5f); contRect.anchorMax = new Vector2(0.5f, 0.5f);
        contRect.anchoredPosition = Vector2.zero;
        contRect.sizeDelta = new Vector2(320, 360);
        Image contImg = containerGo.AddComponent<Image>();
        contImg.color = new Color(0.968f, 0.709f, 0.125f);
        containerGo.AddComponent<UnityEngine.UI.Outline>().effectColor = Color.black;
        containerGo.GetComponent<UnityEngine.UI.Outline>().effectDistance = new Vector2(8, -8);

        // Title
        GameObject titleGo = new GameObject("Title");
        titleGo.transform.SetParent(containerGo.transform, false);
        RectTransform titleRect = titleGo.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1); titleRect.anchorMax = new Vector2(0.5f, 1);
        titleRect.anchoredPosition = new Vector2(0, -40);
        titleRect.sizeDelta = new Vector2(300, 60);
        TextMeshProUGUI titleTxt = titleGo.AddComponent<TextMeshProUGUI>();
        titleTxt.text = "PAUSED";
        titleTxt.fontSize = 45;
        titleTxt.color = Color.white;
        titleTxt.alignment = TextAlignmentOptions.Center;
        titleGo.AddComponent<UnityEngine.UI.Outline>().effectColor = Color.black;
        titleGo.GetComponent<UnityEngine.UI.Outline>().effectDistance = new Vector2(4, -4);

        // Create Buttons dynamically centered around slightly lower Y
        Button resumeBtn = CreateMenuButton("ResumeButton", "RESUME", containerGo.transform, 30);
        Button settingsBtn = CreateMenuButton("SettingsButton", "SETTINGS", containerGo.transform, -50);
        Button quitBtn = CreateMenuButton("QuitButton", "QUIT", containerGo.transform, -130);

        // --- Create Settings Panel ---
        GameObject settingsPanelGo = new GameObject("PopArt_SettingsPanel");
        settingsPanelGo.transform.SetParent(canvas.transform, false);
        RectTransform spRect = settingsPanelGo.AddComponent<RectTransform>();
        spRect.anchorMin = Vector2.zero; spRect.anchorMax = Vector2.one;
        spRect.offsetMin = Vector2.zero; spRect.offsetMax = Vector2.zero;
        Image spImg = settingsPanelGo.AddComponent<Image>();
        spImg.color = new Color(0, 0, 0, 0.7f);

        GameObject spCont = new GameObject("Container");
        spCont.transform.SetParent(settingsPanelGo.transform, false);
        RectTransform spContRect = spCont.AddComponent<RectTransform>();
        spContRect.anchorMin = new Vector2(0.5f, 0.5f); spContRect.anchorMax = new Vector2(0.5f, 0.5f);
        spContRect.anchoredPosition = Vector2.zero;
        spContRect.sizeDelta = new Vector2(320, 360);
        Image spContImg = spCont.AddComponent<Image>();
        spContImg.color = new Color(0.968f, 0.709f, 0.125f);
        spCont.AddComponent<UnityEngine.UI.Outline>().effectColor = Color.black;
        spCont.GetComponent<UnityEngine.UI.Outline>().effectDistance = new Vector2(8, -8);

        GameObject spTitle = new GameObject("Title");
        spTitle.transform.SetParent(spCont.transform, false);
        RectTransform spTitleRect = spTitle.AddComponent<RectTransform>();
        spTitleRect.anchorMin = new Vector2(0.5f, 1); spTitleRect.anchorMax = new Vector2(0.5f, 1);
        spTitleRect.anchoredPosition = new Vector2(0, -40);
        spTitleRect.sizeDelta = new Vector2(300, 60);
        TextMeshProUGUI spTitleTxt = spTitle.AddComponent<TextMeshProUGUI>();
        spTitleTxt.text = "SETTINGS";
        spTitleTxt.fontSize = 45;
        spTitleTxt.color = Color.white;
        spTitleTxt.alignment = TextAlignmentOptions.Center;
        spTitle.AddComponent<UnityEngine.UI.Outline>().effectColor = Color.black;
        spTitle.GetComponent<UnityEngine.UI.Outline>().effectDistance = new Vector2(4, -4);

        // Volume Label
        GameObject volLabelGo = new GameObject("VolumeLabel");
        volLabelGo.transform.SetParent(spCont.transform, false);
        RectTransform volLabelRect = volLabelGo.AddComponent<RectTransform>();
        volLabelRect.anchorMin = new Vector2(0.5f, 0.5f); volLabelRect.anchorMax = new Vector2(0.5f, 0.5f);
        volLabelRect.anchoredPosition = new Vector2(0, 30);
        volLabelRect.sizeDelta = new Vector2(240, 40);
        TextMeshProUGUI volTxt = volLabelGo.AddComponent<TextMeshProUGUI>();
        volTxt.text = "MASTER VOLUME";
        volTxt.fontSize = 24;
        volTxt.color = Color.white;
        volTxt.alignment = TextAlignmentOptions.Center;
        volLabelGo.AddComponent<UnityEngine.UI.Outline>().effectColor = Color.black;
        volLabelGo.GetComponent<UnityEngine.UI.Outline>().effectDistance = new Vector2(2, -2);

        // Volume Slider
        GameObject volSliderGo = new GameObject("VolumeSlider");
        volSliderGo.transform.SetParent(spCont.transform, false);
        RectTransform volSliderRect = volSliderGo.AddComponent<RectTransform>();
        volSliderRect.anchorMin = new Vector2(0.5f, 0.5f); volSliderRect.anchorMax = new Vector2(0.5f, 0.5f);
        volSliderRect.anchoredPosition = new Vector2(0, -20);
        volSliderRect.sizeDelta = new Vector2(240, 20);
        System.Type tSlider = typeof(Slider);
        Slider actualSlider = null;
        
        // Build slider hierarchy
        GameObject bgGo = new GameObject("Background");
        bgGo.transform.SetParent(volSliderGo.transform, false);
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = Color.black;
        RectTransform bgRect = bgGo.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero; bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero; bgRect.offsetMax = Vector2.zero;

        GameObject fillAreaGo = new GameObject("Fill Area");
        fillAreaGo.transform.SetParent(volSliderGo.transform, false);
        RectTransform faRect = fillAreaGo.AddComponent<RectTransform>();
        faRect.anchorMin = Vector2.zero; faRect.anchorMax = Vector2.one;
        faRect.offsetMin = Vector2.zero; faRect.offsetMax = Vector2.zero;

        GameObject fillGo = new GameObject("Fill");
        fillGo.transform.SetParent(fillAreaGo.transform, false);
        Image fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.9f, 0.4f, 0.1f);
        RectTransform fRect = fillGo.GetComponent<RectTransform>();
        fRect.sizeDelta = Vector2.zero;

        actualSlider = volSliderGo.AddComponent<Slider>();
        actualSlider.fillRect = fRect;

        Button closeBtn = CreateMenuButton("CloseButton", "CLOSE", spCont.transform, -130);
        // Note: The click listener for close is added when needed or you can map it to pmc OpenSettings
        settingsBtn.onClick.AddListener(() => { settingsPanelGo.SetActive(true); });
        closeBtn.onClick.AddListener(() => { settingsPanelGo.SetActive(false); });


        // Assign to PMC
        var so = new SerializedObject(pmc);
        so.FindProperty("pauseButton").objectReferenceValue = pauseBtnGo;
        so.FindProperty("pausePanel").objectReferenceValue = panelGo;
        so.FindProperty("pauseBtn").objectReferenceValue = pButton;
        so.FindProperty("resumeBtn").objectReferenceValue = resumeBtn;
        so.FindProperty("settingsBtn").objectReferenceValue = settingsBtn;
        so.FindProperty("quitBtn").objectReferenceValue = quitBtn;
        so.FindProperty("settingsPanel").objectReferenceValue = settingsPanelGo;
        so.FindProperty("closeSettingsBtn").objectReferenceValue = closeBtn;
        so.ApplyModifiedProperties();

        panelGo.SetActive(false);
        settingsPanelGo.SetActive(false);

        EditorUtility.SetDirty(pmc);
        EditorSceneManager.MarkSceneDirty(activeScene);
        Debug.Log("Successfully built Pop-Art Pause Menu!");
    }

    private static Button CreateMenuButton(string name, string textLabel, Transform parent, float yPos)
    {
        GameObject btnGo = new GameObject(name);
        btnGo.transform.SetParent(parent, false);
        RectTransform rect = btnGo.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0, yPos);
        rect.sizeDelta = new Vector2(240, 60);

        Button btn = btnGo.AddComponent<Button>();
        ComicButtonVisuals vis = btnGo.AddComponent<ComicButtonVisuals>();

        GameObject shadow = new GameObject("Shadow");
        shadow.transform.SetParent(btnGo.transform, false);
        RectTransform sRect = shadow.AddComponent<RectTransform>();
        sRect.anchorMin = Vector2.zero; sRect.anchorMax = Vector2.one;
        sRect.offsetMin = new Vector2(8, -8); sRect.offsetMax = new Vector2(8, -8);
        Image sImg = shadow.AddComponent<Image>();
        sImg.color = Color.black;

        GameObject front = new GameObject("Front");
        front.transform.SetParent(btnGo.transform, false);
        RectTransform fRect = front.AddComponent<RectTransform>();
        fRect.anchorMin = Vector2.zero; fRect.anchorMax = Vector2.one;
        fRect.offsetMin = Vector2.zero; fRect.offsetMax = Vector2.zero;
        Image fImg = front.AddComponent<Image>();
        fImg.color = new Color(0.9f, 0.4f, 0.1f);
        front.AddComponent<UnityEngine.UI.Outline>().effectColor = Color.black;
        front.GetComponent<UnityEngine.UI.Outline>().effectDistance = new Vector2(3, -3);
        vis.frontFace = fRect;
        vis.pressOffset = new Vector2(5, -5);

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(front.transform, false);
        RectTransform tRect = textGo.AddComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero; tRect.anchorMax = Vector2.one;
        tRect.offsetMin = Vector2.zero; tRect.offsetMax = Vector2.zero;
        TextMeshProUGUI txt = textGo.AddComponent<TextMeshProUGUI>();
        txt.text = textLabel;
        txt.fontSize = 28;
        txt.color = Color.white;
        txt.alignment = TextAlignmentOptions.Center;
        textGo.AddComponent<UnityEngine.UI.Outline>().effectColor = Color.black;
        textGo.GetComponent<UnityEngine.UI.Outline>().effectDistance = new Vector2(2, -2);

        return btn;
    }
}

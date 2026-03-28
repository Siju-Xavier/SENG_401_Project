namespace Presentation
{
    using System.Collections.Generic;
    using UnityEngine;
    using UnityEngine.UI;
    using TMPro;

    public class TutorialManager : MonoBehaviour
    {
        [Header("Tutorial Pages")]
        [Tooltip("Each entry is one page of the tutorial. Use {CITIES} as a placeholder for the city names.")]
        [SerializeField, TextArea(5, 15)] private string[] tutorialPages = new string[]
        {
            "Welcome! The situation is dire as a wildfire has erupted! The cities of {CITIES} are coming together to figure out a way to stop this fire before any more damage is done.\n\nIt is your job to help aid these cities by putting out the fires and enacting policies to help with the long term effects of the damage that is dealt. Use firefighters, enact policies, keep your budget in mind and work to help save the region before the wildfires destroy everything!\n\n<b>Use WASD to move the camera.</b>\n\nBest of luck and make the right decisions before it's too late!",

            "Ok, your first fire is about to start. This tutorial will help you understand the mechanics of the game before things get serious.\n\nFires will spread randomly and as each level progresses, the fires will start appearing more quickly. The game ends when all cities have been burned by a wildfire.\n\nOnce a wildfire has engulfed a city's footprint, the city will be destroyed and its territory will be scorched.",

            "Each city has an allocated budget that is meant to be used for resources (firefighters). Click on a city to open its panel.\n\nDeploying each firefighter will use up a certain amount of your budget so you must think carefully about how many firefighters per city to deploy.\n\nAt the end of each level after you put out the fires, you'll be given the chance to enact a policy to help reduce the wildfires. Depending on which policy you choose, you might be able to slow down the speed at which fires appear, increase a city's budget, and more.\n\nCities will generate income over time for you to use — however you need to be smart about the moves you make.",

            "<b>How the game works:</b>\n\n  1. Fires start spreading across the map\n  2. Click cities to deploy firefighters\n  3. Enact policies to gain long-term advantages\n  4. Put out the fires as quickly as possible\n  5. Try to survive as long as possible\n\nThe game ends when all cities have been burned and cannot be saved. Best of luck and try to last as long as possible!"
        };

        // ── Runtime UI ──────────────────────────────────────────────────
        private GameObject panel;
        private TextMeshProUGUI bodyText;
        private TextMeshProUGUI pageIndicator;
        private Button nextButton;
        private ScrollRect scrollRect;

        private List<string> pages = new List<string>();
        private int currentPage;
        private bool tutorialActive;

        // ── Public API ──────────────────────────────────────────────────

        public void StartTutorial(List<string> cityNames)
        {
            BuildPages(cityNames);
            CreateUI();
            ShowPage(0);

            Time.timeScale = 0f;
            tutorialActive = true;
        }

        public bool IsTutorialActive => tutorialActive;

        // ── Page Content ────────────────────────────────────────────────

        private void BuildPages(List<string> cityNames)
        {
            pages.Clear();

            string cityList;
            if (cityNames.Count == 0)
                cityList = "the nearby cities";
            else if (cityNames.Count == 1)
                cityList = cityNames[0];
            else if (cityNames.Count == 2)
                cityList = $"{cityNames[0]} and {cityNames[1]}";
            else
            {
                cityList = "";
                for (int i = 0; i < cityNames.Count; i++)
                {
                    if (i == cityNames.Count - 1)
                        cityList += $"and {cityNames[i]}";
                    else
                        cityList += $"{cityNames[i]}, ";
                }
            }

            foreach (var page in tutorialPages)
            {
                pages.Add(page.Replace("{CITIES}", cityList));
            }
        }

        // ── UI Creation ─────────────────────────────────────────────────

        private void CreateUI()
        {
            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            // Full-screen dark overlay
            panel = new GameObject("TutorialPanel");
            panel.transform.SetParent(canvas.transform, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.8f);
            panel.transform.SetAsLastSibling();

            // Content container
            var container = CreateRect("Container", panel.transform,
                new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.9f));
            var containerBg = container.AddComponent<Image>();
            containerBg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f);

            // Title
            var titleGO = CreateRect("Title", container.transform,
                new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.98f));
            var titleText = titleGO.AddComponent<TextMeshProUGUI>();
            titleText.text = "TUTORIAL";
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.fontSize = 32;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = new Color(1f, 0.8f, 0.2f);

            // ── Scrollable body ─────────────────────────────────
            // ScrollRect sits in the body area
            var scrollGO = CreateRect("ScrollArea", container.transform,
                new Vector2(0.05f, 0.14f), new Vector2(0.95f, 0.88f));
            scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.scrollSensitivity = 30f;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            // Viewport (masks content)
            var viewport = CreateRect("Viewport", scrollGO.transform,
                Vector2.zero, Vector2.one);
            var viewportImg = viewport.AddComponent<Image>();
            viewportImg.color = Color.clear;
            viewport.AddComponent<RectMask2D>();
            scrollRect.viewport = viewport.GetComponent<RectTransform>();

            // Content container (stretches width to viewport, grows vertically)
            var content = new GameObject("Content");
            content.transform.SetParent(viewport.transform, false);
            var contentRT = content.AddComponent<RectTransform>();
            // Stretch horizontally, pin to top
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.sizeDelta = new Vector2(0f, 0f);
            contentRT.anchoredPosition = Vector2.zero;
            scrollRect.content = contentRT;

            // Vertical layout to auto-size content
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.padding = new RectOffset(10, 10, 5, 5);

            var contentCSF = content.AddComponent<ContentSizeFitter>();
            contentCSF.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // Body text inside content
            var bodyGO = new GameObject("BodyText");
            bodyGO.transform.SetParent(content.transform, false);
            bodyText = bodyGO.AddComponent<TextMeshProUGUI>();
            bodyText.alignment = TextAlignmentOptions.TopLeft;
            bodyText.fontSize = 20;
            bodyText.color = Color.white;
            bodyText.enableWordWrapping = true;
            bodyText.overflowMode = TextOverflowModes.Overflow;

            // ── Bottom bar ──────────────────────────────────────

            // Page indicator
            var pageGO = CreateRect("PageIndicator", container.transform,
                new Vector2(0.35f, 0.06f), new Vector2(0.65f, 0.12f));
            pageIndicator = pageGO.AddComponent<TextMeshProUGUI>();
            pageIndicator.alignment = TextAlignmentOptions.Center;
            pageIndicator.fontSize = 16;
            pageIndicator.color = new Color(0.7f, 0.7f, 0.7f);

            // Next button
            var nextGO = CreateRect("NextButton", container.transform,
                new Vector2(0.65f, 0.02f), new Vector2(0.95f, 0.12f));
            var nextBg = nextGO.AddComponent<Image>();
            nextBg.color = new Color(0.2f, 0.6f, 0.3f, 1f);
            nextButton = nextGO.AddComponent<Button>();
            nextButton.targetGraphic = nextBg;
            nextButton.onClick.AddListener(OnNextClicked);
            var nextTextGO = CreateRect("Text", nextGO.transform, Vector2.zero, Vector2.one);
            var nextTmp = nextTextGO.AddComponent<TextMeshProUGUI>();
            nextTmp.text = "Next";
            nextTmp.alignment = TextAlignmentOptions.Center;
            nextTmp.fontSize = 22;
            nextTmp.fontStyle = FontStyles.Bold;
            nextTmp.color = Color.white;

            // Skip button
            var skipGO = CreateRect("SkipButton", container.transform,
                new Vector2(0.05f, 0.02f), new Vector2(0.35f, 0.12f));
            var skipBg = skipGO.AddComponent<Image>();
            skipBg.color = new Color(0.5f, 0.2f, 0.2f, 1f);
            var skipBtn = skipGO.AddComponent<Button>();
            skipBtn.targetGraphic = skipBg;
            skipBtn.onClick.AddListener(OnSkipClicked);
            var skipTextGO = CreateRect("Text", skipGO.transform, Vector2.zero, Vector2.one);
            var skipTmp = skipTextGO.AddComponent<TextMeshProUGUI>();
            skipTmp.text = "Skip Tutorial";
            skipTmp.alignment = TextAlignmentOptions.Center;
            skipTmp.fontSize = 18;
            skipTmp.color = Color.white;
        }

        private GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rect = go.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return go;
        }

        // ── Navigation ──────────────────────────────────────────────────

        private void ShowPage(int index)
        {
            currentPage = Mathf.Clamp(index, 0, pages.Count - 1);

            if (bodyText != null)
                bodyText.text = pages[currentPage];

            // Reset scroll to top
            if (scrollRect != null)
                scrollRect.verticalNormalizedPosition = 1f;

            if (pageIndicator != null)
                pageIndicator.text = $"{currentPage + 1} / {pages.Count}";

            if (nextButton != null)
            {
                var tmp = nextButton.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                    tmp.text = currentPage < pages.Count - 1 ? "Next" : "Start Game";
            }
        }

        private void OnNextClicked()
        {
            if (currentPage < pages.Count - 1)
                ShowPage(currentPage + 1);
            else
                FinishTutorial();
        }

        private void OnSkipClicked()
        {
            Debug.Log("[Tutorial] Player skipped.");
            FinishTutorial();
        }

        private void FinishTutorial()
        {
            tutorialActive = false;
            Time.timeScale = 1f;
            if (panel != null) Destroy(panel);
            Debug.Log("[Tutorial] Complete — game starting.");
        }
    }
}

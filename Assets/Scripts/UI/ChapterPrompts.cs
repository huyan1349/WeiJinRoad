using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.UI
{
    public class ChapterPrompts : MonoBehaviour
    {
        private const string ZpixFontPath = "Fonts/zpix";
        private const float DisplayDuration = 4f;
        private const float FadeInDuration = 1f;
        private const float FadeOutDuration = 1.5f;
        private static readonly string[] ChapterTitles = { "第一章：出发", "第二章：荒原", "第三章：风雪", "第四章：灯塔", "第五章：归途" };
        private static readonly Color BgColor = new Color(0.02f, 0.03f, 0.05f, 0.80f);
        private static readonly Color TitleColor = new Color(0.92f, 0.94f, 0.97f, 1f);
        private static readonly Color SubtitleColor = new Color(0.55f, 0.60f, 0.70f, 1f);

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;
        private GameObject _panel;
        private CanvasGroup _canvasGroup;
        private TMP_Text _titleText, _subtitleText;
        private float _displayTimer;
        private bool _isDisplaying;

        private void OnEnable()
        {
            CreateUI();
            GameEvents.OnGamePhaseChanged += OnGamePhaseChanged;
        }

        private void OnDisable() { GameEvents.OnGamePhaseChanged -= OnGamePhaseChanged; }

        private void Update()
        {
            if (!_isDisplaying) return;
            _displayTimer += Time.deltaTime;
            if (_displayTimer < FadeInDuration) _canvasGroup.alpha = _displayTimer / FadeInDuration;
            else if (_displayTimer < FadeInDuration + DisplayDuration) _canvasGroup.alpha = 1f;
            else if (_displayTimer < FadeInDuration + DisplayDuration + FadeOutDuration)
            {
                float fadeProgress = (_displayTimer - FadeInDuration - DisplayDuration) / FadeOutDuration;
                _canvasGroup.alpha = 1f - fadeProgress;
            }
            else { _canvasGroup.alpha = 0f; _panel.SetActive(false); _isDisplaying = false; }
        }

        private void OnGamePhaseChanged(int journey) { ShowChapter(journey - 1); }

        public void ShowChapter(int chapterIndex)
        {
            if (chapterIndex < 0 || chapterIndex >= ChapterTitles.Length) return;
            if (_titleText != null) _titleText.text = ChapterTitles[chapterIndex];
            if (_subtitleText != null) _subtitleText.text = $"旅程 {chapterIndex + 1}/5";
            _displayTimer = 0f; _isDisplaying = true; _canvasGroup.alpha = 0f; _panel.SetActive(true);
        }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            CreateCanvas();
            CreatePanel();
            Debug.Log("[ChapterPrompts] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 700;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CreatePanel()
        {
            _panel = CreateUIObject("ChapterPromptPanel", transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = BgColor;
            _canvasGroup = _panel.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;

            var centerObj = CreateUIObject("Center", _panel.transform);
            var centerRect = centerObj.AddComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.2f, 0.35f); centerRect.anchorMax = new Vector2(0.8f, 0.65f);
            centerRect.offsetMin = Vector2.zero; centerRect.offsetMax = Vector2.zero;
            var vLayout = centerObj.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 15f; vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;

            var titleObj = CreateUIObject("Title", centerObj.transform);
            _titleText = titleObj.AddComponent<TextMeshProUGUI>();
            _titleText.text = ""; _titleText.fontSize = 42; _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = TitleColor; _titleText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) _titleText.font = _fontAsset;
            titleObj.AddComponent<LayoutElement>().preferredHeight = 60f;

            var subtitleObj = CreateUIObject("Subtitle", centerObj.transform);
            _subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
            _subtitleText.text = ""; _subtitleText.fontSize = 18; _subtitleText.color = SubtitleColor;
            _subtitleText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) _subtitleText.font = _fontAsset;
            subtitleObj.AddComponent<LayoutElement>().preferredHeight = 30f;

            _panel.SetActive(false);
        }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-d9483f58b3aa40df9669e8c8e032085f/cwd.txt'; exit "$__tr_native_ec"
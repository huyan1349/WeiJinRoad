using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WeiJinRoad.UI
{
    public class CinematicIntro : MonoBehaviour
    {
        private const string ZpixFontPath = "Fonts/zpix";
        private const float TotalDuration = 8f;
        private const float FadeInDuration = 2f;
        private const float HoldDuration = 4f;
        private const float FadeOutDuration = 2f;

        private static readonly Color BgColor = new Color(0f, 0f, 0f, 1f);
        private static readonly Color TitleColor = new Color(0.92f, 0.94f, 0.97f, 1f);
        private static readonly Color SubtitleColor = new Color(0.55f, 0.60f, 0.70f, 1f);

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;
        private GameObject _panel;
        private CanvasGroup _canvasGroup;
        private TMP_Text _titleText;
        private TMP_Text _subtitleText;
        private float _timer;
        private bool _isPlaying;

        private void OnEnable() { CreateUI(); }

        private void Update()
        {
            if (!_isPlaying) return;
            _timer += Time.deltaTime;

            if (_timer < FadeInDuration)
            {
                _canvasGroup.alpha = _timer / FadeInDuration;
            }
            else if (_timer < FadeInDuration + HoldDuration)
            {
                _canvasGroup.alpha = 1f;
            }
            else if (_timer < TotalDuration)
            {
                float fadeProgress = (_timer - FadeInDuration - HoldDuration) / FadeOutDuration;
                _canvasGroup.alpha = 1f - fadeProgress;
            }
            else
            {
                _canvasGroup.alpha = 0f;
                _panel.SetActive(false);
                _isPlaying = false;
            }

            // Skip on any key
            if (Input.anyKeyDown && _isPlaying)
            {
                _canvasGroup.alpha = 0f;
                _panel.SetActive(false);
                _isPlaying = false;
            }
        }

        public void Play()
        {
            _timer = 0f; _isPlaying = true; _canvasGroup.alpha = 0f; _panel.SetActive(true);
        }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            CreateCanvas();
            CreatePanel();
            Debug.Log("[CinematicIntro] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 3000;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CreatePanel()
        {
            _panel = CreateUIObject("CinematicPanel", transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = BgColor;
            _canvasGroup = _panel.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;

            var centerObj = CreateUIObject("Center", _panel.transform);
            var centerRect = centerObj.AddComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.2f, 0.3f); centerRect.anchorMax = new Vector2(0.8f, 0.7f);
            centerRect.offsetMin = Vector2.zero; centerRect.offsetMax = Vector2.zero;
            var vLayout = centerObj.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 20f; vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;

            var titleObj = CreateUIObject("Title", centerObj.transform);
            _titleText = titleObj.AddComponent<TextMeshProUGUI>();
            _titleText.text = "未 尽 之 路"; _titleText.fontSize = 48; _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = TitleColor; _titleText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) _titleText.font = _fontAsset;
            titleObj.AddComponent<LayoutElement>().preferredHeight = 70f;

            var subtitleObj = CreateUIObject("Subtitle", centerObj.transform);
            _subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
            _subtitleText.text = "The Road Untraveled"; _subtitleText.fontSize = 20;
            _subtitleText.fontStyle = FontStyles.Italic; _subtitleText.color = SubtitleColor;
            _subtitleText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) _subtitleText.font = _fontAsset;
            subtitleObj.AddComponent<LayoutElement>().preferredHeight = 30f;

            var hintObj = CreateUIObject("Hint", centerObj.transform);
            var hintText = hintObj.AddComponent<TextMeshProUGUI>();
            hintText.text = "按任意键跳过"; hintText.fontSize = 14;
            hintText.color = new Color(0.40f, 0.45f, 0.55f, 1f); hintText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) hintText.font = _fontAsset;
            hintObj.AddComponent<LayoutElement>().preferredHeight = 24f;

            _panel.SetActive(false);
        }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-1851de5e35684bdc8df150dbacfab9ac/cwd.txt'; exit "$__tr_native_ec"
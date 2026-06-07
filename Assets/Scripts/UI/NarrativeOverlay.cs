using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.UI
{
    public class NarrativeOverlay : MonoBehaviour
    {
        private const string ZpixFontPath = "Fonts/zpix";
        private static readonly Color BgColor = new Color(0.02f, 0.03f, 0.05f, 0.92f);
        private static readonly Color TitleColor = new Color(0.92f, 0.94f, 0.97f, 1f);
        private static readonly Color ContentColor = new Color(0.80f, 0.83f, 0.90f, 1f);
        private static readonly Color HintColor = new Color(0.45f, 0.50f, 0.60f, 1f);
        private static readonly Color AccentColor = new Color(0.40f, 0.65f, 0.95f, 1f);

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;
        private GameObject _panel;
        private TMP_Text _titleText, _contentText, _carrierText, _hintText;

        private void OnEnable()
        {
            CreateUI();
            GameEvents.OnNarrativeContentChanged += OnNarrativeContentChanged;
        }

        private void OnDisable() { GameEvents.OnNarrativeContentChanged -= OnNarrativeContentChanged; }

        private void Update()
        {
            if ((Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)) && _panel != null && _panel.activeSelf) Hide();
        }

        private void OnNarrativeContentChanged(NarrativeContent content)
        {
            if (content == null) { Hide(); return; }
            if (_titleText != null) _titleText.text = content.Title ?? "";
            if (_contentText != null) _contentText.text = content.Content ?? "";
            if (_carrierText != null) _carrierText.text = $"{content.CarrierType ?? ""}  ·  {content.Chapter ?? ""}";
            Show();
        }

        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
            var gm = GameManager.Instance; if (gm != null) gm.SetNarrativeOverlayVisible(true);
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
            var gm = GameManager.Instance; if (gm != null) gm.SetNarrativeOverlayVisible(false);
        }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            CreateCanvas();
            CreatePanel();
            Debug.Log("[NarrativeOverlay] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 800;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CreatePanel()
        {
            _panel = CreateUIObject("NarrativePanel", transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = BgColor;

            var centerObj = CreateUIObject("CenterContent", _panel.transform);
            var centerRect = centerObj.AddComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.2f, 0.15f); centerRect.anchorMax = new Vector2(0.8f, 0.85f);
            centerRect.offsetMin = Vector2.zero; centerRect.offsetMax = Vector2.zero;
            var vLayout = centerObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(30, 30, 40, 40); vLayout.spacing = 20f;
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;

            var carrierObj = CreateUIObject("Carrier", centerObj.transform);
            _carrierText = carrierObj.AddComponent<TextMeshProUGUI>();
            _carrierText.text = ""; _carrierText.fontSize = 14; _carrierText.color = AccentColor;
            _carrierText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) _carrierText.font = _fontAsset;
            carrierObj.AddComponent<LayoutElement>().preferredHeight = 24f;

            var titleObj = CreateUIObject("Title", centerObj.transform);
            _titleText = titleObj.AddComponent<TextMeshProUGUI>();
            _titleText.text = ""; _titleText.fontSize = 32; _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = TitleColor; _titleText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) _titleText.font = _fontAsset;
            titleObj.AddComponent<LayoutElement>().preferredHeight = 50f;

            var dividerObj = CreateUIObject("Divider", centerObj.transform);
            dividerObj.AddComponent<LayoutElement>().preferredHeight = 2f;
            dividerObj.AddComponent<Image>().color = new Color(0.30f, 0.35f, 0.45f, 0.60f);

            var contentObj = CreateUIObject("Content", centerObj.transform);
            _contentText = contentObj.AddComponent<TextMeshProUGUI>();
            _contentText.text = ""; _contentText.fontSize = 18; _contentText.color = ContentColor;
            _contentText.alignment = TextAlignmentOptions.Center; _contentText.enableWordWrapping = true;
            if (_fontAsset != null) _contentText.font = _fontAsset;
            contentObj.AddComponent<LayoutElement>().flexibleHeight = 200f;

            var hintObj = CreateUIObject("Hint", centerObj.transform);
            _hintText = hintObj.AddComponent<TextMeshProUGUI>();
            _hintText.text = "点击任意处关闭"; _hintText.fontSize = 14; _hintText.color = HintColor;
            _hintText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) _hintText.font = _fontAsset;
            hintObj.AddComponent<LayoutElement>().preferredHeight = 24f;

            _panel.SetActive(false);
        }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}

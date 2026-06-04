using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.UI
{
    public class InteractionPrompt : MonoBehaviour
    {
        private const string ZpixFontPath = "Fonts/zpix";
        private static readonly Color BgColor = new Color(0.08f, 0.09f, 0.12f, 0.85f);
        private static readonly Color TextColor = new Color(0.90f, 0.92f, 0.96f, 1f);
        private static readonly Color KeyColor = new Color(0.40f, 0.65f, 0.95f, 1f);
        private static readonly Color HintColor = new Color(0.65f, 0.68f, 0.75f, 1f);

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;
        private GameObject _panel;
        private TMP_Text _titleText, _hintText, _keyText;

        private void OnEnable()
        {
            CreateUI();
            GameEvents.OnNearbyInteractableChanged += OnNearbyInteractableChanged;
        }

        private void OnDisable() { GameEvents.OnNearbyInteractableChanged -= OnNearbyInteractableChanged; }

        private void OnNearbyInteractableChanged(NearbyInteractable interactable)
        {
            if (interactable == null) { Hide(); return; }
            if (_titleText != null) _titleText.text = interactable.Title ?? "";
            if (_hintText != null) _hintText.text = interactable.Hint ?? "";
            Show();
        }

        public void Show() { if (_panel != null) _panel.SetActive(true); }
        public void Hide() { if (_panel != null) _panel.SetActive(false); }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            CreateCanvas();
            CreatePanel();
            Debug.Log("[InteractionPrompt] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 200;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CreatePanel()
        {
            _panel = CreateUIObject("InteractionPrompt", transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0f); panelRect.anchorMax = new Vector2(0.5f, 0f);
            panelRect.pivot = new Vector2(0.5f, 0f);
            panelRect.anchoredPosition = new Vector2(0f, 120f); panelRect.sizeDelta = new Vector2(320f, 80f);
            _panel.AddComponent<Image>().color = BgColor;
            var vLayout = _panel.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(15, 15, 10, 10); vLayout.spacing = 4f;
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;

            var titleObj = CreateUIObject("Title", _panel.transform);
            _titleText = titleObj.AddComponent<TextMeshProUGUI>();
            _titleText.text = ""; _titleText.fontSize = 18; _titleText.fontStyle = FontStyles.Bold;
            _titleText.color = TextColor; _titleText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) _titleText.font = _fontAsset;
            titleObj.AddComponent<LayoutElement>().preferredHeight = 26f;

            var hintObj = CreateUIObject("Hint", _panel.transform);
            _hintText = hintObj.AddComponent<TextMeshProUGUI>();
            _hintText.text = ""; _hintText.fontSize = 14; _hintText.color = HintColor;
            _hintText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) _hintText.font = _fontAsset;
            hintObj.AddComponent<LayoutElement>().preferredHeight = 20f;

            var keyObj = CreateUIObject("KeyHint", _panel.transform);
            _keyText = keyObj.AddComponent<TextMeshProUGUI>();
            _keyText.text = "[E] 交互"; _keyText.fontSize = 16; _keyText.fontStyle = FontStyles.Bold;
            _keyText.color = KeyColor; _keyText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) _keyText.font = _fontAsset;
            keyObj.AddComponent<LayoutElement>().preferredHeight = 22f;

            _panel.SetActive(false);
        }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-d9e75c9f283041b6af6b2fcb7dd327c2/cwd.txt'; exit "$__tr_native_ec"
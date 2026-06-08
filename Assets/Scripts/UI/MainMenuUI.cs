using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.UI
{
    public class MainMenuUI : MonoBehaviour
    {
        private const string GameTitle = "未 尽 之 路";
        private const string VersionText = "VER 0.4.0";
        private const string ZpixFontPath = "Fonts/zpix";

        private static readonly Color BgColor       = new Color(0.04f, 0.05f, 0.08f, 0.95f);
        private static readonly Color TitleColor     = new Color(0.92f, 0.94f, 0.97f, 1f);
        private static readonly Color ButtonColor    = new Color(0.15f, 0.17f, 0.22f, 0.90f);
        private static readonly Color ButtonHoverColor = new Color(0.25f, 0.28f, 0.35f, 0.95f);
        private static readonly Color TextColor      = new Color(0.85f, 0.88f, 0.93f, 1f);
        private static readonly Color AccentColor    = new Color(0.40f, 0.65f, 0.95f, 1f);
        private static readonly Color SubtitleColor  = new Color(0.55f, 0.60f, 0.70f, 1f);

        private Canvas _canvas;
        private GameObject _panel;
        private TMP_FontAsset _fontAsset;
        private Button _continueButton;

        private void OnEnable() { CreateUI(); }

        public void Show() { if (_panel != null) { _panel.SetActive(true); RefreshContinueButton(); } }
        public void Hide() { if (_panel != null) _panel.SetActive(false); }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            CreateCanvas();
            CreatePanel();
            Debug.Log("[MainMenuUI] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 2000;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CreatePanel()
        {
            _panel = CreateUIObject("MainMenuPanel", transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = BgColor;

            var centerGroup = CreateUIObject("CenterGroup", _panel.transform);
            var centerRect = centerGroup.AddComponent<RectTransform>();
            centerRect.anchorMin = new Vector2(0.5f, 0.5f); centerRect.anchorMax = new Vector2(0.5f, 0.5f);
            centerRect.pivot = new Vector2(0.5f, 0.5f); centerRect.sizeDelta = new Vector2(400f, 500f);
            var vLayout = centerGroup.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(20, 20, 40, 40); vLayout.spacing = 20f;
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;

            var titleObj = CreateUIObject("Title", centerGroup.transform);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = GameTitle; titleText.fontSize = 56; titleText.fontStyle = FontStyles.Bold;
            titleText.color = TitleColor; titleText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) titleText.font = _fontAsset;
            titleObj.AddComponent<LayoutElement>().preferredHeight = 80f;

            var subtitleObj = CreateUIObject("Subtitle", centerGroup.transform);
            var subtitleText = subtitleObj.AddComponent<TextMeshProUGUI>();
            subtitleText.text = "The Road Untraveled"; subtitleText.fontSize = 18;
            subtitleText.fontStyle = FontStyles.Italic; subtitleText.color = SubtitleColor;
            subtitleText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) subtitleText.font = _fontAsset;
            subtitleObj.AddComponent<LayoutElement>().preferredHeight = 30f;

            var spacerObj = CreateUIObject("Spacer", centerGroup.transform);
            spacerObj.AddComponent<LayoutElement>().preferredHeight = 40f;

            _continueButton = CreateMenuButton(centerGroup.transform, "继 续", OnContinue, ButtonColor);
            CreateMenuButton(centerGroup.transform, "新 游 戏", OnNewGame, AccentColor);
            CreateMenuButton(centerGroup.transform, "设 置", OnSettings, ButtonColor);
            CreateMenuButton(centerGroup.transform, "退 出", OnQuit, ButtonColor);

            var versionObj = CreateUIObject("Version", _panel.transform);
            var versionRect = versionObj.AddComponent<RectTransform>();
            versionRect.anchorMin = new Vector2(1f, 0f); versionRect.anchorMax = new Vector2(1f, 0f);
            versionRect.pivot = new Vector2(1f, 0f); versionRect.anchoredPosition = new Vector2(-20f, 20f);
            versionRect.sizeDelta = new Vector2(200f, 30f);
            var versionText = versionObj.AddComponent<TextMeshProUGUI>();
            versionText.text = VersionText; versionText.fontSize = 14; versionText.color = SubtitleColor;
            versionText.alignment = TextAlignmentOptions.Right;
            if (_fontAsset != null) versionText.font = _fontAsset;

            RefreshContinueButton();
        }

        private Button CreateMenuButton(Transform parent, string label, System.Action onClick, Color bgColor)
        {
            var btnObj = CreateUIObject("Btn_" + label.Replace(" ", ""), parent);
            btnObj.AddComponent<Image>().color = bgColor;
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            var colors = btn.colors; colors.highlightedColor = ButtonHoverColor; colors.normalColor = bgColor; btn.colors = colors;
            var btnLayout = btnObj.AddComponent<LayoutElement>(); btnLayout.preferredHeight = 50f; btnLayout.flexibleWidth = 1f;
            var btnText = btnObj.AddComponent<TextMeshProUGUI>();
            btnText.text = label; btnText.fontSize = 22; btnText.color = TextColor; btnText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) btnText.font = _fontAsset;
            return btn;
        }

        private void OnNewGame() { var gm = GameManager.Instance; if (gm != null) gm.NewGame(); Hide(); Debug.Log("[MainMenuUI] 新游戏开始"); }
        private void OnContinue() { SaveSystem.LoadGame(); Hide(); Debug.Log("[MainMenuUI] 继续游戏"); }
        private void OnSettings() { var sp = FindFirstObjectByType<SettingsPage>(); if (sp != null) sp.Show(); }
        private void OnQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void RefreshContinueButton()
        {
            if (_continueButton != null)
            {
                bool hasSave = SaveSystem.HasSave();
                _continueButton.interactable = hasSave;
                var text = _continueButton.GetComponent<TextMeshProUGUI>();
                if (text != null) text.color = hasSave ? TextColor : SubtitleColor;
            }
        }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}

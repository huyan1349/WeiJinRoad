using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.UI
{
    public class TabMenu : MonoBehaviour
    {
        private const string ZpixFontPath = "Fonts/zpix";
        private static readonly Color BgColor = new Color(0.06f, 0.07f, 0.10f, 0.88f);
        private static readonly Color TextColor = new Color(0.90f, 0.92f, 0.96f, 1f);
        private static readonly Color AccentColor = new Color(0.40f, 0.65f, 0.95f, 1f);
        private static readonly Color BtnColor = new Color(0.12f, 0.13f, 0.17f, 0.90f);
        private static readonly Color BtnHoverColor = new Color(0.20f, 0.22f, 0.28f, 0.95f);

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;
        private GameObject _panel;

        private void OnEnable() { CreateUI(); }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab)) ToggleVisibility();
            if (Input.GetKeyDown(KeyCode.Escape) && _panel != null && _panel.activeSelf) Hide();
        }

        public void ToggleVisibility()
        {
            if (_panel == null) return;
            if (_panel.activeSelf) Hide(); else Show();
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
            Debug.Log("[TabMenu] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 300;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CreatePanel()
        {
            _panel = CreateUIObject("TabMenuPanel", transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.5f); panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f); panelRect.sizeDelta = new Vector2(300f, 350f);
            _panel.AddComponent<Image>().color = BgColor;

            var vLayout = _panel.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(15, 15, 20, 15); vLayout.spacing = 8f;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;

            // Title
            var titleObj = CreateUIObject("Title", _panel.transform);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "快 捷 菜 单"; titleText.fontSize = 22; titleText.fontStyle = FontStyles.Bold;
            titleText.color = TextColor; titleText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) titleText.font = _fontAsset;
            titleObj.AddComponent<LayoutElement>().preferredHeight = 35f;

            // Separator
            var sepObj = CreateUIObject("Sep", _panel.transform);
            sepObj.AddComponent<LayoutElement>().preferredHeight = 2f;
            sepObj.AddComponent<Image>().color = new Color(0.30f, 0.35f, 0.45f, 0.50f);

            // Menu items
            CreateMenuItem(_panel.transform, "🗺️ 地图  [M]", () => { var map = FindFirstObjectByType<MapUI>(); if (map != null) { map.ToggleLargeMap(); Hide(); } });
            CreateMenuItem(_panel.transform, "📖 日志  [J]", () => { var journal = FindFirstObjectByType<JournalUI>(); if (journal != null) { journal.ToggleVisibility(); Hide(); } });
            CreateMenuItem(_panel.transform, "⚙️ 设置  [Tab]", () => { var settings = FindFirstObjectByType<SettingsPage>(); if (settings != null) { settings.ToggleVisibility(); Hide(); } });
            CreateMenuItem(_panel.transform, "🔧 维修", () => { var repair = FindFirstObjectByType<RepairMenu>(); if (repair != null) { repair.Show(); Hide(); } });
            CreateMenuItem(_panel.transform, "🏗️ 建造", () => { var build = FindFirstObjectByType<BuildMenu>(); if (build != null) { build.Show(); Hide(); } });

            // Spacer
            var spacerObj = CreateUIObject("Spacer", _panel.transform);
            spacerObj.AddComponent<LayoutElement>().preferredHeight = 10f;

            // Close hint
            var hintObj = CreateUIObject("Hint", _panel.transform);
            var hintText = hintObj.AddComponent<TextMeshProUGUI>();
            hintText.text = "Tab / Esc 关闭"; hintText.fontSize = 13;
            hintText.color = new Color(0.50f, 0.55f, 0.65f, 1f); hintText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) hintText.font = _fontAsset;
            hintObj.AddComponent<LayoutElement>().preferredHeight = 20f;

            _panel.SetActive(false);
        }

        private void CreateMenuItem(Transform parent, string label, System.Action onClick)
        {
            var btnObj = CreateUIObject("MenuItem", parent);
            btnObj.AddComponent<Image>().color = BtnColor;
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            var colors = btn.colors; colors.highlightedColor = BtnHoverColor; btn.colors = colors;
            btnObj.AddComponent<LayoutElement>().preferredHeight = 40f;
            var text = btnObj.AddComponent<TextMeshProUGUI>();
            text.text = label; text.fontSize = 16; text.color = TextColor;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) text.font = _fontAsset;
        }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}

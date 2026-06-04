using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.UI
{
    public class CampUI : MonoBehaviour
    {
        private const string ZpixFontPath = "Fonts/zpix";
        private static readonly Color BgColor = new Color(0.06f, 0.07f, 0.10f, 0.92f);
        private static readonly Color PanelColor = new Color(0.10f, 0.11f, 0.15f, 0.90f);
        private static readonly Color TextColor = new Color(0.90f, 0.92f, 0.96f, 1f);
        private static readonly Color AccentColor = new Color(0.40f, 0.65f, 0.95f, 1f);
        private static readonly Color BtnColor = new Color(0.15f, 0.17f, 0.22f, 0.90f);
        private static readonly Color BtnHoverColor = new Color(0.25f, 0.28f, 0.35f, 0.95f);
        private static readonly Color CloseBtnColor = new Color(0.70f, 0.25f, 0.25f, 1f);
        private static readonly Color FireColor = new Color(0.90f, 0.55f, 0.20f, 1f);
        private static readonly Color RadioColor = new Color(0.40f, 0.75f, 0.95f, 1f);
        private static readonly Color StarColor = new Color(0.70f, 0.45f, 0.90f, 1f);
        private static readonly Color CookColor = new Color(0.72f, 0.55f, 0.35f, 1f);

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;
        private GameObject _panel;

        private void OnEnable()
        {
            CreateUI();
            GameEvents.OnCampingChanged += OnCampingChanged;
        }

        private void OnDisable() { GameEvents.OnCampingChanged -= OnCampingChanged; }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && _panel != null && _panel.activeSelf) Hide();
        }

        private void OnCampingChanged(bool camping)
        {
            if (camping) Show(); else Hide();
        }

        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
            var gm = GameManager.Instance;
            if (gm != null && gm.Camping) gm.EndCamp();
        }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            CreateCanvas();
            CreatePanel();
            Debug.Log("[CampUI] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 400;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CreatePanel()
        {
            _panel = CreateUIObject("CampPanel", transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.3f, 0.15f); panelRect.anchorMax = new Vector2(0.7f, 0.85f);
            panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = BgColor;

            var contentArea = CreateUIObject("ContentArea", _panel.transform);
            var contentRect = contentArea.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero; contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(10f, 10f); contentRect.offsetMax = new Vector2(-10f, -10f);
            contentArea.AddComponent<Image>().color = PanelColor;

            var vLayout = contentArea.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(15, 15, 15, 15); vLayout.spacing = 15f;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;

            // Header
            var headerObj = CreateUIObject("Header", contentArea.transform);
            var headerHLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
            headerHLayout.childAlignment = TextAnchor.MiddleCenter;
            headerHLayout.childControlWidth = true; headerHLayout.childControlHeight = false;
            headerHLayout.childForceExpandWidth = true; headerHLayout.childForceExpandHeight = false;
            var titleObj = CreateUIObject("Title", headerObj.transform);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "营 地"; titleText.fontSize = 28; titleText.fontStyle = FontStyles.Bold;
            titleText.color = TextColor; titleText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) titleText.font = _fontAsset;
            titleObj.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var closeBtnObj = CreateUIObject("CloseBtn", headerObj.transform);
            closeBtnObj.AddComponent<Image>().color = CloseBtnColor;
            var closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);
            closeBtnObj.AddComponent<LayoutElement>().preferredWidth = 35f;
            var closeBtnText = closeBtnObj.AddComponent<TextMeshProUGUI>();
            closeBtnText.text = "✕"; closeBtnText.fontSize = 18; closeBtnText.color = TextColor;
            closeBtnText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) closeBtnText.font = _fontAsset;

            // Campfire
            CreateActivityButton(contentArea.transform, "🔥 篝火", "休息与恢复", FireColor, OnCampfire);
            // Radio
            CreateActivityButton(contentArea.transform, "📻 电台", "收听信号", RadioColor, OnRadio);
            // Starmap
            CreateActivityButton(contentArea.transform, "🗺️ 星图", "观测星空", StarColor, OnStarmap);
            // Cooking
            CreateActivityButton(contentArea.transform, "🍲 烹饪", "制作食物", CookColor, OnCooking);

            // Leave camp button
            var leaveObj = CreateUIObject("LeaveBtn", contentArea.transform);
            leaveObj.AddComponent<Image>().color = AccentColor;
            var leaveBtn = leaveObj.AddComponent<Button>();
            leaveBtn.onClick.AddListener(Hide);
            leaveObj.AddComponent<LayoutElement>().preferredHeight = 45f;
            var leaveText = leaveObj.AddComponent<TextMeshProUGUI>();
            leaveText.text = "拔营出发"; leaveText.fontSize = 20; leaveText.color = TextColor;
            leaveText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) leaveText.font = _fontAsset;

            _panel.SetActive(false);
        }

        private void CreateActivityButton(Transform parent, string title, string desc, Color color, System.Action onClick)
        {
            var btnObj = CreateUIObject("Activity_" + title, parent);
            btnObj.AddComponent<Image>().color = BtnColor;
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            var colors = btn.colors; colors.highlightedColor = BtnHoverColor; btn.colors = colors;
            btnObj.AddComponent<LayoutElement>().preferredHeight = 60f;
            var vLayout = btnObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(15, 15, 8, 8); vLayout.spacing = 4f;
            vLayout.childAlignment = TextAnchor.MiddleLeft;
            vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;

            var titleObj = CreateUIObject("Title", btnObj.transform);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = title; titleText.fontSize = 18; titleText.fontStyle = FontStyles.Bold;
            titleText.color = color; titleText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) titleText.font = _fontAsset;
            titleObj.AddComponent<LayoutElement>().preferredHeight = 24f;

            var descObj = CreateUIObject("Desc", btnObj.transform);
            var descText = descObj.AddComponent<TextMeshProUGUI>();
            descText.text = desc; descText.fontSize = 13;
            descText.color = new Color(0.65f, 0.68f, 0.75f, 1f); descText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) descText.font = _fontAsset;
            descObj.AddComponent<LayoutElement>().preferredHeight = 18f;
        }

        private void OnCampfire() { Debug.Log("[CampUI] 篝火休息"); }
        private void OnRadio() { Debug.Log("[CampUI] 电台收听"); }
        private void OnStarmap() { Debug.Log("[CampUI] 星图观测"); }
        private void OnCooking() { Debug.Log("[CampUI] 烹饪食物"); }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-f0d210d34b5749c4907c8161c121c747/cwd.txt'; exit "$__tr_native_ec"
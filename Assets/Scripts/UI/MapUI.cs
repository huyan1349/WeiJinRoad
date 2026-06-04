using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.UI
{
    public class MapUI : MonoBehaviour
    {
        private const string ZpixFontPath = "Fonts/zpix";
        private const float MinimapSize = 180f;
        private static readonly Color BgColor = new Color(0.06f, 0.07f, 0.10f, 0.85f);
        private static readonly Color TextColor = new Color(0.90f, 0.92f, 0.96f, 1f);
        private static readonly Color RoadColor = new Color(0.35f, 0.38f, 0.45f, 1f);
        private static readonly Color VehicleColor = new Color(0.40f, 0.75f, 0.95f, 1f);
        private static readonly Color AccentColor = new Color(0.40f, 0.65f, 0.95f, 1f);

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;
        private GameObject _minimapPanel;
        private GameObject _largeMapPanel;
        private Image _vehicleDot;
        private TMP_Text _journeyLabel;

        private void OnEnable()
        {
            CreateUI();
            GameEvents.OnGamePhaseChanged += OnGamePhaseChanged;
        }

        private void OnDisable() { GameEvents.OnGamePhaseChanged -= OnGamePhaseChanged; }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.M)) ToggleLargeMap();
            if (Input.GetKeyDown(KeyCode.Escape) && _largeMapPanel != null && _largeMapPanel.activeSelf) HideLargeMap();
            UpdateVehiclePosition();
        }

        private void OnGamePhaseChanged(int journey)
        {
            if (_journeyLabel != null) _journeyLabel.text = $"旅程 {journey}/5";
        }

        public void ToggleLargeMap()
        {
            if (_largeMapPanel == null) return;
            if (_largeMapPanel.activeSelf) HideLargeMap(); else ShowLargeMap();
        }

        public void ShowLargeMap()
        {
            if (_largeMapPanel != null) _largeMapPanel.SetActive(true);
            var gm = GameManager.Instance; if (gm != null) gm.ShowLargeMap = true;
        }

        public void HideLargeMap()
        {
            if (_largeMapPanel != null) _largeMapPanel.SetActive(false);
            var gm = GameManager.Instance; if (gm != null) gm.ShowLargeMap = false;
        }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            CreateCanvas();
            CreateMinimap();
            CreateLargeMap();
            Debug.Log("[MapUI] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 120;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CreateMinimap()
        {
            _minimapPanel = CreateUIObject("Minimap", transform);
            var panelRect = _minimapPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f); panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-15f, -15f); panelRect.sizeDelta = new Vector2(MinimapSize, MinimapSize + 30f);
            _minimapPanel.AddComponent<Image>().color = BgColor;
            var vLayout = _minimapPanel.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(5, 5, 5, 5); vLayout.spacing = 2f;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;

            var labelObj = CreateUIObject("JourneyLabel", _minimapPanel.transform);
            _journeyLabel = labelObj.AddComponent<TextMeshProUGUI>();
            _journeyLabel.text = "旅程 1/5"; _journeyLabel.fontSize = 12; _journeyLabel.color = TextColor;
            _journeyLabel.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) _journeyLabel.font = _fontAsset;
            labelObj.AddComponent<LayoutElement>().preferredHeight = 20f;

            var contentObj = CreateUIObject("MapContent", _minimapPanel.transform);
            contentObj.AddComponent<LayoutElement>().preferredHeight = MinimapSize - 10f;
            contentObj.AddComponent<Image>().color = new Color(0.10f, 0.11f, 0.15f, 0.95f);

            var roadObj = CreateUIObject("Road", contentObj.transform);
            var roadRect = roadObj.AddComponent<RectTransform>();
            roadRect.anchorMin = new Vector2(0.45f, 0f); roadRect.anchorMax = new Vector2(0.55f, 1f);
            roadRect.offsetMin = Vector2.zero; roadRect.offsetMax = Vector2.zero;
            roadObj.AddComponent<Image>().color = RoadColor;

            var vehicleObj = CreateUIObject("VehicleDot", contentObj.transform);
            var vehicleRect = vehicleObj.AddComponent<RectTransform>();
            vehicleRect.anchorMin = new Vector2(0.5f, 0.5f); vehicleRect.anchorMax = new Vector2(0.5f, 0.5f);
            vehicleRect.sizeDelta = new Vector2(12f, 12f); vehicleRect.anchoredPosition = Vector2.zero;
            _vehicleDot = vehicleObj.AddComponent<Image>();
            _vehicleDot.color = VehicleColor;
        }

        private void CreateLargeMap()
        {
            _largeMapPanel = CreateUIObject("LargeMap", transform);
            var panelRect = _largeMapPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
            _largeMapPanel.AddComponent<Image>().color = new Color(0.04f, 0.05f, 0.08f, 0.95f);

            var titleObj = CreateUIObject("Title", _largeMapPanel.transform);
            var titleRect = titleObj.AddComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0.5f, 1f); titleRect.anchorMax = new Vector2(0.5f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = new Vector2(0f, -20f); titleRect.sizeDelta = new Vector2(300f, 40f);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "地 图  [M 关闭]"; titleText.fontSize = 24; titleText.fontStyle = FontStyles.Bold;
            titleText.color = TextColor; titleText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) titleText.font = _fontAsset;

            var contentObj = CreateUIObject("LargeMapContent", _largeMapPanel.transform);
            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.1f, 0.05f); contentRect.anchorMax = new Vector2(0.9f, 0.9f);
            contentRect.offsetMin = Vector2.zero; contentRect.offsetMax = Vector2.zero;
            contentObj.AddComponent<Image>().color = new Color(0.08f, 0.09f, 0.12f, 0.95f);

            var roadObj = CreateUIObject("Road", contentObj.transform);
            var roadRect = roadObj.AddComponent<RectTransform>();
            roadRect.anchorMin = new Vector2(0.47f, 0f); roadRect.anchorMax = new Vector2(0.53f, 1f);
            roadRect.offsetMin = Vector2.zero; roadRect.offsetMax = Vector2.zero;
            roadObj.AddComponent<Image>().color = RoadColor;

            var vehicleObj = CreateUIObject("VehicleDot", contentObj.transform);
            var vehicleRect = vehicleObj.AddComponent<RectTransform>();
            vehicleRect.anchorMin = new Vector2(0.5f, 0.5f); vehicleRect.anchorMax = new Vector2(0.5f, 0.5f);
            vehicleRect.sizeDelta = new Vector2(20f, 20f); vehicleRect.anchoredPosition = Vector2.zero;
            vehicleObj.AddComponent<Image>().color = VehicleColor;

            _largeMapPanel.SetActive(false);
        }

        private void UpdateVehiclePosition()
        {
            var gm = GameManager.Instance; if (gm == null) return;
            float routeZ = gm.VehicleTransient.Position[1];
            float normalizedPos = Mathf.InverseLerp(0f, 540f, routeZ);
            if (_vehicleDot != null)
            {
                var rect = _vehicleDot.rectTransform;
                rect.anchorMin = new Vector2(0.5f, normalizedPos);
                rect.anchorMax = new Vector2(0.5f, normalizedPos);
                rect.anchoredPosition = Vector2.zero;
            }
        }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-2030b56ca9634da997fbc62bdec4f1f8/cwd.txt'; exit "$__tr_native_ec"
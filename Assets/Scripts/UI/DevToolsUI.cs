using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.UI
{
    public class DevToolsUI : MonoBehaviour
    {
        private const string ZpixFontPath = "Fonts/zpix";
        private static readonly Color BgColor = new Color(0.06f, 0.07f, 0.10f, 0.90f);
        private static readonly Color PanelColor = new Color(0.10f, 0.11f, 0.15f, 0.90f);
        private static readonly Color TextColor = new Color(0.90f, 0.92f, 0.96f, 1f);
        private static readonly Color AccentColor = new Color(0.40f, 0.65f, 0.95f, 1f);
        private static readonly Color BtnColor = new Color(0.15f, 0.17f, 0.22f, 0.90f);
        private static readonly Color BtnHoverColor = new Color(0.25f, 0.28f, 0.35f, 0.95f);
        private static readonly Color CloseBtnColor = new Color(0.70f, 0.25f, 0.25f, 1f);
        private static readonly Color ValueColor = new Color(0.90f, 0.65f, 0.15f, 1f);
        private static readonly Color SectionColor = new Color(0.15f, 0.16f, 0.20f, 0.90f);

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;
        private GameObject _panel;
        private TMP_Text _fpsText;
        private TMP_Text _posText;
        private TMP_Text _perfText;

        private void OnEnable()
        {
            CreateUI();
            GameEvents.OnDevPanelVisibilityChanged += OnDevPanelVisibilityChanged;
        }

        private void OnDisable() { GameEvents.OnDevPanelVisibilityChanged -= OnDevPanelVisibilityChanged; }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) ToggleVisibility();
            if (Input.GetKeyDown(KeyCode.Escape) && _panel != null && _panel.activeSelf) Hide();

            if (_panel != null && _panel.activeSelf)
            {
                UpdatePerfDisplay();
            }
        }

        private void OnDevPanelVisibilityChanged(bool visible)
        {
            if (visible) Show(); else Hide();
        }

        public void ToggleVisibility()
        {
            if (_panel == null) return;
            if (_panel.activeSelf) Hide(); else Show();
        }

        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
            var gm = GameManager.Instance; if (gm != null) gm.DevPanelVisible = true;
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
            var gm = GameManager.Instance; if (gm != null) gm.DevPanelVisible = false;
        }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            CreateCanvas();
            CreatePanel();
            Debug.Log("[DevToolsUI] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 1500;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CreatePanel()
        {
            _panel = CreateUIObject("DevToolsPanel", transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f); panelRect.anchorMax = new Vector2(0.35f, 1f);
            panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = BgColor;

            var contentArea = CreateUIObject("ContentArea", _panel.transform);
            var contentRect = contentArea.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero; contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(5f, 5f); contentRect.offsetMax = new Vector2(-5f, -5f);
            contentArea.AddComponent<Image>().color = PanelColor;

            var vLayout = contentArea.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(10, 10, 10, 10); vLayout.spacing = 8f;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;

            // Header
            var headerObj = CreateUIObject("Header", contentArea.transform);
            var headerHLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
            headerHLayout.childControlWidth = true; headerHLayout.childForceExpandWidth = true;
            var titleObj = CreateUIObject("Title", headerObj.transform);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "开发者工具 [F1]"; titleText.fontSize = 18; titleText.fontStyle = FontStyles.Bold;
            titleText.color = TextColor; titleText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) titleText.font = _fontAsset;
            titleObj.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var closeBtnObj = CreateUIObject("CloseBtn", headerObj.transform);
            closeBtnObj.AddComponent<Image>().color = CloseBtnColor;
            closeBtnObj.AddComponent<Button>().onClick.AddListener(Hide);
            closeBtnObj.AddComponent<LayoutElement>().preferredWidth = 30f;
            var closeBtnText = closeBtnObj.AddComponent<TextMeshProUGUI>();
            closeBtnText.text = "✕"; closeBtnText.fontSize = 16; closeBtnText.color = TextColor;
            closeBtnText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) closeBtnText.font = _fontAsset;

            // Performance section
            CreateSectionHeader(contentArea.transform, "性能");
            _fpsText = CreateInfoRow(contentArea.transform, "FPS", "0");
            _posText = CreateInfoRow(contentArea.transform, "位置", "0, 0");
            _perfText = CreateInfoRow(contentArea.transform, "DrawCalls", "0");

            // Teleport section
            CreateSectionHeader(contentArea.transform, "传送");
            CreateDevButton(contentArea.transform, "传送至起点", () => RequestTeleport(0f, 270f));
            CreateDevButton(contentArea.transform, "传送至中段", () => RequestTeleport(0f, 135f));
            CreateDevButton(contentArea.transform, "传送至终点", () => RequestTeleport(0f, 0f));

            // Cheats section
            CreateSectionHeader(contentArea.transform, "作弊");
            CreateDevButton(contentArea.transform, "添加资源 +10", OnAddResources);
            CreateDevButton(contentArea.transform, "维修全部", OnRepairAll);
            CreateDevButton(contentArea.transform, "上帝模式", OnToggleGodMode);

            _panel.SetActive(false);
        }

        private void CreateSectionHeader(Transform parent, string title)
        {
            var obj = CreateUIObject("Section_" + title, parent);
            obj.AddComponent<Image>().color = SectionColor;
            obj.AddComponent<LayoutElement>().preferredHeight = 28f;
            var hLayout = obj.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(10, 10, 4, 4);
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = true; hLayout.childForceExpandWidth = true;
            var textObj = CreateUIObject("Label", obj.transform);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = title; text.fontSize = 14; text.fontStyle = FontStyles.Bold;
            text.color = AccentColor; text.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) text.font = _fontAsset;
        }

        private TMP_Text CreateInfoRow(Transform parent, string label, string initialValue)
        {
            var rowObj = CreateUIObject("InfoRow_" + label, parent);
            var hLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(10, 10, 2, 2); hLayout.spacing = 8f;
            hLayout.childControlWidth = true; hLayout.childForceExpandWidth = true;
            rowObj.AddComponent<LayoutElement>().preferredHeight = 22f;

            var labelObj = CreateUIObject("Label", rowObj.transform);
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label; labelText.fontSize = 13; labelText.color = TextColor;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) labelText.font = _fontAsset;
            labelObj.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var valueObj = CreateUIObject("Value", rowObj.transform);
            var valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.text = initialValue; valueText.fontSize = 13; valueText.color = ValueColor;
            valueText.alignment = TextAlignmentOptions.MidlineRight;
            if (_fontAsset != null) valueText.font = _fontAsset;
            valueObj.AddComponent<LayoutElement>().preferredWidth = 100f;

            return valueText;
        }

        private void CreateDevButton(Transform parent, string label, System.Action onClick)
        {
            var btnObj = CreateUIObject("DevBtn_" + label, parent);
            btnObj.AddComponent<Image>().color = BtnColor;
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            var colors = btn.colors; colors.highlightedColor = BtnHoverColor; btn.colors = colors;
            btnObj.AddComponent<LayoutElement>().preferredHeight = 30f;
            var text = btnObj.AddComponent<TextMeshProUGUI>();
            text.text = label; text.fontSize = 13; text.color = TextColor;
            text.alignment = TextAlignmentOptions.MiddleCenter;
            if (_fontAsset != null) text.font = _fontAsset;
        }

        private void UpdatePerfDisplay()
        {
            var gm = GameManager.Instance;
            if (_fpsText != null) _fpsText.text = $"{1f / Mathf.Max(0.001f, Time.deltaTime):F0}";
            if (gm != null)
            {
                if (_posText != null) _posText.text = $"{gm.VehicleTransient.Position[0]:F0}, {gm.VehicleTransient.Position[1]:F0}";
                if (_perfText != null) _perfText.text = $"DC:{gm.PerfStatsData.DrawCalls} Tri:{gm.PerfStatsData.Triangles}";
            }
        }

        private void RequestTeleport(float x, float z)
        {
            GameEvents.OnDevTeleportRequested?.Invoke(x, z);
            var gm = GameManager.Instance; if (gm != null) gm.RequestDevTeleport(x, z);
        }

        private void OnAddResources()
        {
            var gm = GameManager.Instance; if (gm == null) return;
            gm.AddResources(new ResourceBag { Metal = 10, Wood = 10, Fuel = 10, Signal = 5, Crystal = 3 });
        }

        private void OnRepairAll()
        {
            var gm = GameManager.Instance; if (gm == null) return;
            foreach (PartId part in System.Enum.GetValues(typeof(PartId)))
                gm.SetPartCondition(part, 1f);
        }

        private void OnToggleGodMode()
        {
            var gm = GameManager.Instance; if (gm == null) return;
            gm.DevGodView = !gm.DevGodView;
        }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-885474d8a0744741954770414a0ccd31/cwd.txt'; exit "$__tr_native_ec"
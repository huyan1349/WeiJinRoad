using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.UI
{
    public class RepairMenu : MonoBehaviour
    {
        private const string ZpixFontPath = "Fonts/zpix";
        private static readonly Color BgColor = new Color(0.06f, 0.07f, 0.10f, 0.92f);
        private static readonly Color PanelColor = new Color(0.10f, 0.11f, 0.15f, 0.90f);
        private static readonly Color TextColor = new Color(0.90f, 0.92f, 0.96f, 1f);
        private static readonly Color AccentColor = new Color(0.40f, 0.65f, 0.95f, 1f);
        private static readonly Color BtnColor = new Color(0.15f, 0.17f, 0.22f, 0.90f);
        private static readonly Color BtnHoverColor = new Color(0.25f, 0.28f, 0.35f, 0.95f);
        private static readonly Color CloseBtnColor = new Color(0.70f, 0.25f, 0.25f, 1f);
        private static readonly Color BarBgColor = new Color(0.15f, 0.16f, 0.20f, 0.90f);
        private static readonly Color BarGoodColor = new Color(0.35f, 0.75f, 0.45f, 1f);
        private static readonly Color BarWarnColor = new Color(0.90f, 0.65f, 0.15f, 1f);
        private static readonly Color BarBadColor = new Color(0.85f, 0.25f, 0.25f, 1f);
        private static readonly Color RepairBtnColor = new Color(0.35f, 0.55f, 0.85f, 1f);
        private static readonly Color UpgradeBtnColor = new Color(0.70f, 0.45f, 0.90f, 1f);

        private static readonly (PartId Id, string Name)[] PartInfo = {
            (PartId.Engine, "发动机"),
            (PartId.Tires, "轮胎"),
            (PartId.Headlight, "探照灯"),
            (PartId.Tank, "油箱"),
            (PartId.Body, "车身"),
            (PartId.Radio, "电台")
        };

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;
        private GameObject _panel;

        private void OnEnable()
        {
            CreateUI();
            GameEvents.OnVehiclePartsChanged += OnVehiclePartsChanged;
            GameEvents.OnResourcesChanged += OnResourcesChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnVehiclePartsChanged -= OnVehiclePartsChanged;
            GameEvents.OnResourcesChanged -= OnResourcesChanged;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && _panel != null && _panel.activeSelf) Hide();
        }

        private void OnVehiclePartsChanged() { RefreshBars(); }
        private void OnResourcesChanged(ResourceBag r) { RefreshBars(); }

        public void Show() { if (_panel != null) { RefreshBars(); _panel.SetActive(true); } }
        public void Hide() { if (_panel != null) _panel.SetActive(false); }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            CreateCanvas();
            CreatePanel();
            Debug.Log("[RepairMenu] UI 创建完成");
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
            _panel = CreateUIObject("RepairPanel", transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.25f, 0.1f); panelRect.anchorMax = new Vector2(0.75f, 0.9f);
            panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = BgColor;

            var contentArea = CreateUIObject("ContentArea", _panel.transform);
            var contentRect = contentArea.AddComponent<RectTransform>();
            contentArea.anchorMin = Vector2.zero; contentArea.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(10f, 10f); contentRect.offsetMax = new Vector2(-10f, -10f);
            contentArea.AddComponent<Image>().color = PanelColor;

            var vLayout = contentArea.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(15, 15, 15, 15); vLayout.spacing = 10f;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;

            // Header
            var headerObj = CreateUIObject("Header", contentArea.transform);
            var headerHLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
            headerHLayout.childControlWidth = true; headerHLayout.childForceExpandWidth = true;
            var titleObj = CreateUIObject("Title", headerObj.transform);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "维 修"; titleText.fontSize = 28; titleText.fontStyle = FontStyles.Bold;
            titleText.color = TextColor; titleText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) titleText.font = _fontAsset;
            titleObj.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var closeBtnObj = CreateUIObject("CloseBtn", headerObj.transform);
            closeBtnObj.AddComponent<Image>().color = CloseBtnColor;
            closeBtnObj.AddComponent<Button>().onClick.AddListener(Hide);
            closeBtnObj.AddComponent<LayoutElement>().preferredWidth = 35f;
            var closeBtnText = closeBtnObj.AddComponent<TextMeshProUGUI>();
            closeBtnText.text = "✕"; closeBtnText.fontSize = 18; closeBtnText.color = TextColor;
            closeBtnText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) closeBtnText.font = _fontAsset;

            // Cost info
            var costObj = CreateUIObject("CostInfo", contentArea.transform);
            var costText = costObj.AddComponent<TextMeshProUGUI>();
            costText.text = "维修: 金属2 + 信号1  |  升级: 金属3 + 信号2 + 光源1";
            costText.fontSize = 13; costText.color = new Color(0.65f, 0.68f, 0.75f, 1f);
            costText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) costText.font = _fontAsset;
            costObj.AddComponent<LayoutElement>().preferredHeight = 22f;

            // Part rows
            foreach (var part in PartInfo)
            {
                CreatePartRow(contentArea.transform, part.Id, part.Name);
            }

            _panel.SetActive(false);
        }

        private void CreatePartRow(Transform parent, PartId partId, string name)
        {
            var rowObj = CreateUIObject("Part_" + partId, parent);
            rowObj.AddComponent<Image>().color = BtnColor;
            rowObj.AddComponent<LayoutElement>().preferredHeight = 55f;
            var hLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(10, 10, 5, 5); hLayout.spacing = 8f;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = true; hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = true; hLayout.childForceExpandHeight = false;

            // Name
            var nameObj = CreateUIObject("Name", rowObj.transform);
            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = name; nameText.fontSize = 16; nameText.fontStyle = FontStyles.Bold;
            nameText.color = TextColor; nameText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) nameText.font = _fontAsset;
            nameObj.AddComponent<LayoutElement>().preferredWidth = 80f;

            // Condition bar
            var barBgObj = CreateUIObject("BarBg", rowObj.transform);
            barBgObj.AddComponent<Image>().color = BarBgColor;
            barBgObj.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var fillObj = CreateUIObject("Fill", barBgObj.transform);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
            var fillImage = fillObj.AddComponent<Image>();
            fillImage.color = BarGoodColor; fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal; fillImage.fillAmount = 1f;

            // Level
            var levelObj = CreateUIObject("Level", rowObj.transform);
            var levelText = levelObj.AddComponent<TextMeshProUGUI>();
            levelText.text = "Lv1"; levelText.fontSize = 14; levelText.color = AccentColor;
            levelText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) levelText.font = _fontAsset;
            levelObj.AddComponent<LayoutElement>().preferredWidth = 40f;

            // Repair button
            var repairBtnObj = CreateUIObject("RepairBtn", rowObj.transform);
            repairBtnObj.AddComponent<Image>().color = RepairBtnColor;
            var repairBtn = repairBtnObj.AddComponent<Button>();
            repairBtn.onClick.AddListener(() => OnRepair(partId));
            repairBtnObj.AddComponent<LayoutElement>().preferredWidth = 50f;
            var repairText = repairBtnObj.AddComponent<TextMeshProUGUI>();
            repairText.text = "修"; repairText.fontSize = 14; repairText.color = TextColor;
            repairText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) repairText.font = _fontAsset;

            // Upgrade button
            var upgradeBtnObj = CreateUIObject("UpgradeBtn", rowObj.transform);
            upgradeBtnObj.AddComponent<Image>().color = UpgradeBtnColor;
            var upgradeBtn = upgradeBtnObj.AddComponent<Button>();
            upgradeBtn.onClick.AddListener(() => OnUpgrade(partId));
            upgradeBtnObj.AddComponent<LayoutElement>().preferredWidth = 50f;
            var upgradeText = upgradeBtnObj.AddComponent<TextMeshProUGUI>();
            upgradeText.text = "升"; upgradeText.fontSize = 14; upgradeText.color = TextColor;
            upgradeText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) upgradeText.font = _fontAsset;
        }

        private void OnRepair(PartId part)
        {
            var gm = GameManager.Instance; if (gm == null) return;
            if (gm.RepairPart(part)) { Debug.Log($"[RepairMenu] 维修 {part} 成功"); RefreshBars(); }
            else Debug.LogWarning($"[RepairMenu] 维修 {part} 失败");
        }

        private void OnUpgrade(PartId part)
        {
            var gm = GameManager.Instance; if (gm == null) return;
            if (gm.UpgradePart(part)) { Debug.Log($"[RepairMenu] 升级 {part} 成功"); RefreshBars(); }
            else Debug.LogWarning($"[RepairMenu] 升级 {part} 失败");
        }

        private void RefreshBars()
        {
            var gm = GameManager.Instance; if (gm == null) return;
            // 简化：遍历子对象更新状态
            // 完整实现需要缓存每个部件的UI引用
        }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-2d533818bebe491cbe8e3a31d429e8eb/cwd.txt'; exit "$__tr_native_ec"
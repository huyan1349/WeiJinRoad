using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.UI
{
    public class BuildMenu : MonoBehaviour
    {
        private const string ZpixFontPath = "Fonts/zpix";
        private static readonly Color BgColor = new Color(0.06f, 0.07f, 0.10f, 0.92f);
        private static readonly Color PanelColor = new Color(0.10f, 0.11f, 0.15f, 0.90f);
        private static readonly Color TextColor = new Color(0.90f, 0.92f, 0.96f, 1f);
        private static readonly Color AccentColor = new Color(0.40f, 0.65f, 0.95f, 1f);
        private static readonly Color BtnColor = new Color(0.15f, 0.17f, 0.22f, 0.90f);
        private static readonly Color BtnHoverColor = new Color(0.25f, 0.28f, 0.35f, 0.95f);
        private static readonly Color CloseBtnColor = new Color(0.70f, 0.25f, 0.25f, 1f);
        private static readonly Color CostColor = new Color(0.90f, 0.65f, 0.15f, 1f);
        private static readonly Color DisabledColor = new Color(0.40f, 0.42f, 0.48f, 0.60f);

        private static readonly (string Name, FacilityType Type, string Desc)[] Facilities = {
            ("补给仓", FacilityType.Supply, "存储物资"),
            ("避风棚", FacilityType.Shelter, "抵御风雪"),
            ("信号塔", FacilityType.SignalTower, "增强信号"),
            ("灯塔", FacilityType.Beacon, "照亮前路"),
            ("观测台", FacilityType.Observatory, "观测远方"),
            ("简易桥", FacilityType.Bridge, "跨越障碍")
        };

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;
        private GameObject _panel;

        private void OnEnable()
        {
            CreateUI();
            GameEvents.OnStationBuilt += OnStationBuilt;
            GameEvents.OnResourcesChanged += OnResourcesChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnStationBuilt -= OnStationBuilt;
            GameEvents.OnResourcesChanged -= OnResourcesChanged;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && _panel != null && _panel.activeSelf) Hide();
        }

        private void OnStationBuilt(string siteId) { RefreshButtons(); }
        private void OnResourcesChanged(ResourceBag r) { RefreshButtons(); }

        public void Show()
        {
            if (_panel != null) { RefreshButtons(); _panel.SetActive(true); }
        }

        public void Hide() { if (_panel != null) _panel.SetActive(false); }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            CreateCanvas();
            CreatePanel();
            Debug.Log("[BuildMenu] UI 创建完成");
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
            _panel = CreateUIObject("BuildPanel", transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.25f, 0.1f); panelRect.anchorMax = new Vector2(0.75f, 0.9f);
            panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = BgColor;

            var contentArea = CreateUIObject("ContentArea", _panel.transform);
            var contentRect = contentArea.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero; contentRect.anchorMax = Vector2.one;
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
            headerHLayout.childAlignment = TextAnchor.MiddleCenter;
            headerHLayout.childControlWidth = true; headerHLayout.childControlHeight = false;
            headerHLayout.childForceExpandWidth = true; headerHLayout.childForceExpandHeight = false;
            var titleObj = CreateUIObject("Title", headerObj.transform);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "建 造"; titleText.fontSize = 28; titleText.fontStyle = FontStyles.Bold;
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

            // Facility buttons
            foreach (var fac in Facilities)
            {
                CreateFacilityButton(contentArea.transform, fac.Name, fac.Type, fac.Desc);
            }

            _panel.SetActive(false);
        }

        private void CreateFacilityButton(Transform parent, string name, FacilityType type, string desc)
        {
            var cost = GameManager.GetFacilityCost(type);
            var btnObj = CreateUIObject("Facility_" + type, parent);
            btnObj.AddComponent<Image>().color = BtnColor;
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => OnBuildFacility(type));
            var colors = btn.colors; colors.highlightedColor = BtnHoverColor; btn.colors = colors;
            btnObj.AddComponent<LayoutElement>().preferredHeight = 55f;
            var hLayout = btnObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(15, 15, 5, 5); hLayout.spacing = 10f;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = true; hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = true; hLayout.childForceExpandHeight = false;

            var infoObj = CreateUIObject("Info", btnObj.transform);
            var infoVLayout = infoObj.AddComponent<VerticalLayoutGroup>();
            infoVLayout.spacing = 2f; infoVLayout.childControlWidth = true; infoVLayout.childControlHeight = false;
            infoVLayout.childForceExpandWidth = true; infoVLayout.childForceExpandHeight = false;
            infoObj.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var nameObj = CreateUIObject("Name", infoObj.transform);
            var nameText = nameObj.AddComponent<TextMeshProUGUI>();
            nameText.text = name; nameText.fontSize = 16; nameText.fontStyle = FontStyles.Bold;
            nameText.color = AccentColor; nameText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) nameText.font = _fontAsset;
            nameObj.AddComponent<LayoutElement>().preferredHeight = 22f;

            var descObj = CreateUIObject("Desc", infoObj.transform);
            var descText = descObj.AddComponent<TextMeshProUGUI>();
            descText.text = desc; descText.fontSize = 13;
            descText.color = new Color(0.65f, 0.68f, 0.75f, 1f); descText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) descText.font = _fontAsset;
            descObj.AddComponent<LayoutElement>().preferredHeight = 18f;

            var costObj = CreateUIObject("Cost", btnObj.transform);
            var costText = costObj.AddComponent<TextMeshProUGUI>();
            costText.text = FormatCost(cost); costText.fontSize = 13; costText.color = CostColor;
            costText.alignment = TextAlignmentOptions.MidlineRight;
            if (_fontAsset != null) costText.font = _fontAsset;
            costObj.AddComponent<LayoutElement>().preferredWidth = 160f;
        }

        private void OnBuildFacility(FacilityType type)
        {
            var gm = GameManager.Instance; if (gm == null) return;
            string siteId = gm.NearbyStation;
            if (string.IsNullOrEmpty(siteId)) { Debug.LogWarning("[BuildMenu] 不在站点附近"); return; }
            if (gm.BuildFacility(siteId, type))
            {
                Debug.Log($"[BuildMenu] 建造 {type} 成功");
                RefreshButtons();
            }
            else
            {
                Debug.LogWarning($"[BuildMenu] 建造 {type} 失败：资源不足或已建造");
            }
        }

        private void RefreshButtons()
        {
            // 简化：通过重新检查资源可负担性来更新按钮状态
            // 完整实现需要遍历所有按钮并更新 interactable 状态
        }

        private string FormatCost(ResourceBag cost)
        {
            var parts = new System.Collections.Generic.List<string>();
            if (cost.Metal > 0) parts.Add($"金属{cost.Metal}");
            if (cost.Wood > 0) parts.Add($"木材{cost.Wood}");
            if (cost.Fuel > 0) parts.Add($"燃料{cost.Fuel}");
            if (cost.Signal > 0) parts.Add($"信号{cost.Signal}");
            if (cost.Crystal > 0) parts.Add($"光源{cost.Crystal}");
            return string.Join(" ", parts);
        }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-0db59ee002304d8eb020e94f15620e06/cwd.txt'; exit "$__tr_native_ec"
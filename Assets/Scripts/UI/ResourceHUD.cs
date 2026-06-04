using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.UI
{
    public class ResourceHUD : MonoBehaviour
    {
        private const string ZpixFontPath = "Fonts/zpix";
        private static readonly Color BgColor = new Color(0.06f, 0.07f, 0.10f, 0.70f);
        private static readonly Color TextColor = new Color(0.90f, 0.92f, 0.96f, 1f);
        private static readonly Color MetalColor = new Color(0.75f, 0.78f, 0.85f, 1f);
        private static readonly Color WoodColor = new Color(0.72f, 0.55f, 0.35f, 1f);
        private static readonly Color FuelColor = new Color(0.90f, 0.65f, 0.15f, 1f);
        private static readonly Color SignalColor = new Color(0.40f, 0.75f, 0.95f, 1f);
        private static readonly Color CrystalColor = new Color(0.70f, 0.45f, 0.90f, 1f);
        private static readonly Color BarBgColor = new Color(0.15f, 0.16f, 0.20f, 0.90f);
        private static readonly Color CarryColor = new Color(0.40f, 0.65f, 0.95f, 1f);

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;
        private TMP_Text _metalText, _woodText, _fuelText, _signalText, _crystalText;
        private Image _carryBarFill;
        private TMP_Text _carryText;

        private void OnEnable()
        {
            CreateUI();
            GameEvents.OnResourcesChanged += OnResourcesChanged;
        }

        private void OnDisable() { GameEvents.OnResourcesChanged -= OnResourcesChanged; }

        private void OnResourcesChanged(ResourceBag r)
        {
            if (r == null) return;
            if (_metalText != null) _metalText.text = $"金属: {r.Metal}";
            if (_woodText != null) _woodText.text = $"木材: {r.Wood}";
            if (_fuelText != null) _fuelText.text = $"燃料: {r.Fuel}";
            if (_signalText != null) _signalText.text = $"信号件: {r.Signal}";
            if (_crystalText != null) _crystalText.text = $"光源晶: {r.Crystal}";
            var gm = GameManager.Instance;
            if (gm != null)
            {
                int total = r.Total(); int max = gm.MaxCarry;
                if (_carryBarFill != null) _carryBarFill.fillAmount = (float)total / Mathf.Max(1, max);
                if (_carryText != null) _carryText.text = $"{total}/{max}";
            }
        }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            CreateCanvas();
            CreateResourcePanel();
            var gm = GameManager.Instance;
            if (gm != null) OnResourcesChanged(gm.Resources);
            Debug.Log("[ResourceHUD] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 110;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CreateResourcePanel()
        {
            var panelObj = CreateUIObject("ResourcePanel", transform);
            var panelRect = panelObj.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f); panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(15f, -100f); panelRect.sizeDelta = new Vector2(220f, 240f);
            panelObj.AddComponent<Image>().color = BgColor;
            var vLayout = panelObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(12, 12, 12, 12); vLayout.spacing = 8f;
            vLayout.childAlignment = TextAnchor.UpperLeft;
            vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;

            var titleObj = CreateUIObject("Title", panelObj.transform);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "资 源"; titleText.fontSize = 16; titleText.fontStyle = FontStyles.Bold;
            titleText.color = TextColor; titleText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) titleText.font = _fontAsset;
            titleObj.AddComponent<LayoutElement>().preferredHeight = 24f;

            _metalText = CreateResourceRow(panelObj.transform, "金属", MetalColor);
            _woodText = CreateResourceRow(panelObj.transform, "木材", WoodColor);
            _fuelText = CreateResourceRow(panelObj.transform, "燃料", FuelColor);
            _signalText = CreateResourceRow(panelObj.transform, "信号件", SignalColor);
            _crystalText = CreateResourceRow(panelObj.transform, "光源晶", CrystalColor);

            var carryLabelObj = CreateUIObject("CarryLabel", panelObj.transform);
            _carryText = carryLabelObj.AddComponent<TextMeshProUGUI>();
            _carryText.text = "0/40"; _carryText.fontSize = 14; _carryText.color = CarryColor;
            _carryText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) _carryText.font = _fontAsset;
            carryLabelObj.AddComponent<LayoutElement>().preferredHeight = 20f;

            var carryBarBg = CreateUIObject("CarryBarBg", panelObj.transform);
            carryBarBg.AddComponent<Image>().color = BarBgColor;
            carryBarBg.AddComponent<LayoutElement>().preferredHeight = 10f;
            var carryFill = CreateUIObject("CarryFill", carryBarBg.transform);
            var carryFillRect = carryFill.AddComponent<RectTransform>();
            carryFillRect.anchorMin = Vector2.zero; carryFillRect.anchorMax = new Vector2(1f, 1f);
            carryFillRect.offsetMin = Vector2.zero; carryFillRect.offsetMax = Vector2.zero;
            _carryBarFill = carryFill.AddComponent<Image>();
            _carryBarFill.color = CarryColor; _carryBarFill.type = Image.Type.Filled;
            _carryBarFill.fillMethod = Image.FillMethod.Horizontal; _carryBarFill.fillAmount = 0f;
        }

        private TMP_Text CreateResourceRow(Transform parent, string name, Color color)
        {
            var rowObj = CreateUIObject("Row_" + name, parent);
            rowObj.AddComponent<LayoutElement>().preferredHeight = 24f;
            var text = rowObj.AddComponent<TextMeshProUGUI>();
            text.text = $"{name}: 0"; text.fontSize = 16; text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) text.font = _fontAsset;
            return text;
        }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-d21aeddc830f470c9776999d01694310/cwd.txt'; exit "$__tr_native_ec"
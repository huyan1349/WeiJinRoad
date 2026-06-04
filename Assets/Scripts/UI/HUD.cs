using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.UI
{
    public class HUD : MonoBehaviour
    {
        private const string ZpixFontPath = "Fonts/zpix";
        private static readonly Color BgColor = new Color(0.06f, 0.07f, 0.10f, 0.75f);
        private static readonly Color TextColor = new Color(0.90f, 0.92f, 0.96f, 1f);
        private static readonly Color FuelColor = new Color(0.90f, 0.65f, 0.15f, 1f);
        private static readonly Color HealthColor = new Color(0.85f, 0.25f, 0.25f, 1f);
        private static readonly Color SpeedColor = new Color(0.40f, 0.75f, 0.95f, 1f);
        private static readonly Color BarBgColor = new Color(0.15f, 0.16f, 0.20f, 0.90f);
        private static readonly Color CooldownColor = new Color(0.60f, 0.60f, 0.65f, 0.80f);
        private static readonly Color CompassColor = new Color(0.80f, 0.82f, 0.88f, 1f);
        private static readonly Color AccentColor = new Color(0.40f, 0.65f, 0.95f, 1f);

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;
        private TMP_Text _speedText;
        private Image _fuelBarFill;
        private TMP_Text _fuelText;
        private Image _healthBarFill;
        private TMP_Text _healthText;
        private TMP_Text _compassText;
        private Image _searchlightCooldownFill;
        private TMP_Text _searchlightText;

        private void OnEnable()
        {
            CreateUI();
            GameEvents.OnFuelChanged += OnFuelChanged;
            GameEvents.OnHealthChanged += OnHealthChanged;
            GameEvents.OnVehiclePartsChanged += OnVehiclePartsChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnFuelChanged -= OnFuelChanged;
            GameEvents.OnHealthChanged -= OnHealthChanged;
            GameEvents.OnVehiclePartsChanged -= OnVehiclePartsChanged;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            float speed = gm.VehicleTransient.Speed;
            if (_speedText != null) _speedText.text = $"{speed:F0} km/h";
            float heading = gm.VehicleTransient.Heading * Mathf.Rad2Deg;
            string dir = GetDirectionName(heading);
            if (_compassText != null) _compassText.text = $"{dir} {heading:F0}°";
            float intensity = gm.VehicleTransient.SearchlightIntensity;
            if (_searchlightCooldownFill != null) _searchlightCooldownFill.fillAmount = intensity;
            if (_searchlightText != null) _searchlightText.text = intensity > 0.01f ? "探照灯: 开启" : "探照灯: 冷却";
        }

        private void OnFuelChanged(int fuel)
        {
            var gm = GameManager.Instance; if (gm == null) return;
            float ratio = (float)fuel / Mathf.Max(1, gm.MaxCarry);
            if (_fuelBarFill != null) _fuelBarFill.fillAmount = ratio;
            if (_fuelText != null) _fuelText.text = $"{fuel}";
        }

        private void OnHealthChanged(float health)
        {
            if (_healthBarFill != null) _healthBarFill.fillAmount = Mathf.Clamp01(health);
            if (_healthText != null) _healthText.text = $"{health * 100f:F0}%";
        }

        private void OnVehiclePartsChanged()
        {
            var gm = GameManager.Instance; if (gm == null) return;
            float health = CalculateOverallHealth(gm);
            OnHealthChanged(health);
        }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            CreateCanvas();
            CreateBottomBar();
            CreateTopBar();
            Debug.Log("[HUD] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 100;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CreateBottomBar()
        {
            var barObj = CreateUIObject("BottomBar", transform);
            var barRect = barObj.AddComponent<RectTransform>();
            barRect.anchorMin = new Vector2(0f, 0f); barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.offsetMin = new Vector2(20f, 15f); barRect.offsetMax = new Vector2(-20f, 85f);
            barObj.AddComponent<Image>().color = BgColor;
            var hLayout = barObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(20, 20, 10, 10); hLayout.spacing = 30f;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = true; hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = true; hLayout.childForceExpandHeight = false;

            // Speed
            var speedObj = CreateUIObject("SpeedDisplay", barObj.transform);
            speedObj.AddComponent<LayoutElement>().preferredWidth = 160f;
            _speedText = speedObj.AddComponent<TextMeshProUGUI>();
            _speedText.text = "0 km/h"; _speedText.fontSize = 24; _speedText.fontStyle = FontStyles.Bold;
            _speedText.color = SpeedColor; _speedText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) _speedText.font = _fontAsset;

            // Fuel bar
            CreateFuelBar(barObj.transform);
            // Health bar
            CreateHealthBar(barObj.transform);
            // Searchlight
            CreateSearchlightIndicator(barObj.transform);
        }

        private void CreateFuelBar(Transform parent)
        {
            var obj = CreateUIObject("FuelBar", parent);
            obj.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var vLayout = obj.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 2f; vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;
            var labelObj = CreateUIObject("Label", obj.transform);
            _fuelText = labelObj.AddComponent<TextMeshProUGUI>();
            _fuelText.text = "燃料: 6"; _fuelText.fontSize = 14; _fuelText.color = FuelColor;
            _fuelText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) _fuelText.font = _fontAsset;
            labelObj.AddComponent<LayoutElement>().preferredHeight = 20f;
            var barBgObj = CreateUIObject("BarBg", obj.transform);
            barBgObj.AddComponent<Image>().color = BarBgColor;
            barBgObj.AddComponent<LayoutElement>().preferredHeight = 16f;
            var fillObj = CreateUIObject("Fill", barBgObj.transform);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
            _fuelBarFill = fillObj.AddComponent<Image>();
            _fuelBarFill.color = FuelColor; _fuelBarFill.type = Image.Type.Filled;
            _fuelBarFill.fillMethod = Image.FillMethod.Horizontal; _fuelBarFill.fillAmount = 0.5f;
        }

        private void CreateHealthBar(Transform parent)
        {
            var obj = CreateUIObject("HealthBar", parent);
            obj.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var vLayout = obj.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 2f; vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;
            var labelObj = CreateUIObject("Label", obj.transform);
            _healthText = labelObj.AddComponent<TextMeshProUGUI>();
            _healthText.text = "耐久: 100%"; _healthText.fontSize = 14; _healthText.color = HealthColor;
            _healthText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) _healthText.font = _fontAsset;
            labelObj.AddComponent<LayoutElement>().preferredHeight = 20f;
            var barBgObj = CreateUIObject("BarBg", obj.transform);
            barBgObj.AddComponent<Image>().color = BarBgColor;
            barBgObj.AddComponent<LayoutElement>().preferredHeight = 16f;
            var fillObj = CreateUIObject("Fill", barBgObj.transform);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
            _healthBarFill = fillObj.AddComponent<Image>();
            _healthBarFill.color = HealthColor; _healthBarFill.type = Image.Type.Filled;
            _healthBarFill.fillMethod = Image.FillMethod.Horizontal; _healthBarFill.fillAmount = 1f;
        }

        private void CreateSearchlightIndicator(Transform parent)
        {
            var obj = CreateUIObject("Searchlight", parent);
            obj.AddComponent<LayoutElement>().preferredWidth = 140f;
            var vLayout = obj.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 2f; vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;
            _searchlightText = obj.AddComponent<TextMeshProUGUI>();
            _searchlightText.text = "探照灯: 冷却"; _searchlightText.fontSize = 14;
            _searchlightText.color = CooldownColor; _searchlightText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) _searchlightText.font = _fontAsset;
            var barBgObj = CreateUIObject("BarBg", obj.transform);
            barBgObj.AddComponent<Image>().color = BarBgColor;
            barBgObj.AddComponent<LayoutElement>().preferredHeight = 8f;
            var fillObj = CreateUIObject("Fill", barBgObj.transform);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
            _searchlightCooldownFill = fillObj.AddComponent<Image>();
            _searchlightCooldownFill.color = AccentColor; _searchlightCooldownFill.type = Image.Type.Filled;
            _searchlightCooldownFill.fillMethod = Image.FillMethod.Horizontal; _searchlightCooldownFill.fillAmount = 0f;
        }

        private void CreateTopBar()
        {
            var compassObj = CreateUIObject("Compass", transform);
            var compassRect = compassObj.AddComponent<RectTransform>();
            compassRect.anchorMin = new Vector2(0.5f, 1f); compassRect.anchorMax = new Vector2(0.5f, 1f);
            compassRect.pivot = new Vector2(0.5f, 1f);
            compassRect.anchoredPosition = new Vector2(0f, -15f); compassRect.sizeDelta = new Vector2(200f, 40f);
            compassObj.AddComponent<Image>().color = BgColor;
            _compassText = compassObj.AddComponent<TextMeshProUGUI>();
            _compassText.text = "N 0°"; _compassText.fontSize = 18; _compassText.color = CompassColor;
            _compassText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) _compassText.font = _fontAsset;
        }

        private float CalculateOverallHealth(GameManager gm)
        {
            var parts = gm.VehicleParts; if (parts == null || parts.Length == 0) return 1f;
            float total = 0f; foreach (var p in parts) total += p.Condition;
            return total / parts.Length;
        }

        private string GetDirectionName(float degrees)
        {
            degrees = ((degrees % 360f) + 360f) % 360f;
            if (degrees < 22.5f || degrees >= 337.5f) return "N";
            if (degrees < 67.5f) return "NE"; if (degrees < 112.5f) return "E";
            if (degrees < 157.5f) return "SE"; if (degrees < 202.5f) return "S";
            if (degrees < 247.5f) return "SW"; if (degrees < 292.5f) return "W";
            return "NW";
        }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-b491268e048843f388f0e666b473654a/cwd.txt'; exit "$__tr_native_ec"
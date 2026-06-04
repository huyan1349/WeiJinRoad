using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.UI
{
    public class AchievementToast : MonoBehaviour
    {
        private const string ZpixFontPath = "Fonts/zpix";
        private const float DisplayDuration = 4f;
        private const float FadeInDuration = 0.5f;
        private const float FadeOutDuration = 1f;

        private static readonly Color BgColor = new Color(0.10f, 0.12f, 0.18f, 0.92f);
        private static readonly Color TextColor = new Color(0.90f, 0.92f, 0.96f, 1f);
        private static readonly Color AccentColor = new Color(0.90f, 0.65f, 0.15f, 1f);
        private static readonly Color DescColor = new Color(0.65f, 0.68f, 0.75f, 1f);

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;
        private GameObject _panel;
        private CanvasGroup _canvasGroup;
        private TMP_Text _nameText;
        private TMP_Text _descText;
        private TMP_Text _iconText;
        private float _displayTimer;
        private bool _isDisplaying;

        private void OnEnable()
        {
            CreateUI();
            GameEvents.OnAchievementUnlocked += OnAchievementUnlocked;
        }

        private void OnDisable() { GameEvents.OnAchievementUnlocked -= OnAchievementUnlocked; }

        private void Update()
        {
            if (!_isDisplaying) return;
            _displayTimer += Time.deltaTime;
            if (_displayTimer < FadeInDuration) _canvasGroup.alpha = _displayTimer / FadeInDuration;
            else if (_displayTimer < FadeInDuration + DisplayDuration) _canvasGroup.alpha = 1f;
            else if (_displayTimer < FadeInDuration + DisplayDuration + FadeOutDuration)
            {
                float fadeProgress = (_displayTimer - FadeInDuration - DisplayDuration) / FadeOutDuration;
                _canvasGroup.alpha = 1f - fadeProgress;
            }
            else { _canvasGroup.alpha = 0f; _panel.SetActive(false); _isDisplaying = false; }
        }

        private void OnAchievementUnlocked(Achievement achievement)
        {
            if (achievement == null) return;
            Show(achievement);
        }

        public void Show(Achievement achievement)
        {
            if (_nameText != null) _nameText.text = achievement.Name ?? "";
            if (_descText != null) _descText.text = achievement.Desc ?? "";
            if (_iconText != null) _iconText.text = achievement.Icon ?? "🏆";
            _displayTimer = 0f; _isDisplaying = true; _canvasGroup.alpha = 0f; _panel.SetActive(true);
        }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            CreateCanvas();
            CreatePanel();
            Debug.Log("[AchievementToast] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 900;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CreatePanel()
        {
            _panel = CreateUIObject("AchievementToast", transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f); panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -20f); panelRect.sizeDelta = new Vector2(360f, 80f);
            _panel.AddComponent<Image>().color = BgColor;
            _canvasGroup = _panel.AddComponent<CanvasGroup>();
            _canvasGroup.alpha = 0f;

            var hLayout = _panel.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(15, 15, 10, 10); hLayout.spacing = 12f;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = true; hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = false; hLayout.childForceExpandHeight = false;

            // Icon
            var iconObj = CreateUIObject("Icon", _panel.transform);
            _iconText = iconObj.AddComponent<TextMeshProUGUI>();
            _iconText.text = "🏆"; _iconText.fontSize = 32;
            _iconText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) _iconText.font = _fontAsset;
            iconObj.AddComponent<LayoutElement>().preferredWidth = 50f;

            // Info
            var infoObj = CreateUIObject("Info", _panel.transform);
            var infoVLayout = infoObj.AddComponent<VerticalLayoutGroup>();
            infoVLayout.spacing = 2f; infoVLayout.childControlWidth = true; infoVLayout.childControlHeight = false;
            infoVLayout.childForceExpandWidth = true; infoVLayout.childForceExpandHeight = false;
            infoObj.AddComponent<LayoutElement>().flexibleWidth = 1f;

            // Achievement label
            var labelObj = CreateUIObject("Label", infoObj.transform);
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = "成就解锁"; labelText.fontSize = 12; labelText.color = AccentColor;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) labelText.font = _fontAsset;
            labelObj.AddComponent<LayoutElement>().preferredHeight = 16f;

            // Name
            var nameObj = CreateUIObject("Name", infoObj.transform);
            _nameText = nameObj.AddComponent<TextMeshProUGUI>();
            _nameText.text = ""; _nameText.fontSize = 18; _nameText.fontStyle = FontStyles.Bold;
            _nameText.color = TextColor; _nameText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) _nameText.font = _fontAsset;
            nameObj.AddComponent<LayoutElement>().preferredHeight = 24f;

            // Desc
            var descObj = CreateUIObject("Desc", infoObj.transform);
            _descText = descObj.AddComponent<TextMeshProUGUI>();
            _descText.text = ""; _descText.fontSize = 13; _descText.color = DescColor;
            _descText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) _descText.font = _fontAsset;
            descObj.AddComponent<LayoutElement>().preferredHeight = 18f;

            _panel.SetActive(false);
        }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-98a4b4dbe99648fe9ae5308c80eed74c/cwd.txt'; exit "$__tr_native_ec"
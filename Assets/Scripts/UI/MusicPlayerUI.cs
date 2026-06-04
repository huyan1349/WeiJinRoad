using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.UI
{
    public class MusicPlayerUI : MonoBehaviour
    {
        private const string ZpixFontPath = "Fonts/zpix";
        private static readonly Color BgColor = new Color(0.06f, 0.07f, 0.10f, 0.80f);
        private static readonly Color TextColor = new Color(0.90f, 0.92f, 0.96f, 1f);
        private static readonly Color AccentColor = new Color(0.40f, 0.65f, 0.95f, 1f);
        private static readonly Color BtnColor = new Color(0.15f, 0.17f, 0.22f, 0.90f);
        private static readonly Color BtnHoverColor = new Color(0.25f, 0.28f, 0.35f, 0.95f);
        private static readonly Color ProgressBgColor = new Color(0.15f, 0.16f, 0.20f, 0.90f);
        private static readonly Color ProgressFillColor = new Color(0.35f, 0.55f, 0.85f, 1f);

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;
        private GameObject _panel;
        private TMP_Text _trackNameText;
        private TMP_Text _timeText;
        private Image _progressFill;
        private Button _playPauseBtn;
        private TMP_Text _playPauseText;
        private AudioSource _audioSource;
        private bool _isPlaying;

        private void OnEnable() { CreateUI(); }

        private void Update()
        {
            if (_audioSource != null && _audioSource.isPlaying)
            {
                if (_progressFill != null && _audioSource.clip != null)
                    _progressFill.fillAmount = _audioSource.time / _audioSource.clip.length;
                if (_timeText != null)
                    _timeText.text = $"{FormatTime(_audioSource.time)} / {FormatTime(_audioSource.clip != null ? _audioSource.clip.length : 0f)}";
            }
        }

        public void Show() { if (_panel != null) _panel.SetActive(true); }
        public void Hide() { if (_panel != null) _panel.SetActive(false); }
        public void ToggleVisibility()
        {
            if (_panel == null) return;
            if (_panel.activeSelf) Hide(); else Show();
        }

        public void SetAudioSource(AudioSource source)
        {
            _audioSource = source;
            if (_trackNameText != null && source != null && source.clip != null)
                _trackNameText.text = source.clip.name;
        }

        public void PlayTrack(AudioClip clip)
        {
            if (_audioSource == null) return;
            _audioSource.clip = clip;
            _audioSource.Play();
            _isPlaying = true;
            UpdatePlayPauseText();
            if (_trackNameText != null) _trackNameText.text = clip != null ? clip.name : "";
        }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            _audioSource = GetComponent<AudioSource>();
            if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

            CreateCanvas();
            CreatePanel();
            Debug.Log("[MusicPlayerUI] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 130;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CreatePanel()
        {
            _panel = CreateUIObject("MusicPlayer", transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 0f); panelRect.anchorMax = new Vector2(1f, 0f);
            panelRect.pivot = new Vector2(1f, 0f);
            panelRect.anchoredPosition = new Vector2(-15f, 95f); panelRect.sizeDelta = new Vector2(280f, 55f);
            _panel.AddComponent<Image>().color = BgColor;

            var vLayout = _panel.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(10, 10, 6, 6); vLayout.spacing = 4f;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;

            // Track name row
            var topRow = CreateUIObject("TopRow", _panel.transform);
            var topHLayout = topRow.AddComponent<HorizontalLayoutGroup>();
            topHLayout.spacing = 8f; topHLayout.childControlWidth = true; topHLayout.childForceExpandWidth = true;

            _trackNameText = topRow.AddComponent<TextMeshProUGUI>();
            _trackNameText.text = "未播放"; _trackNameText.fontSize = 13; _trackNameText.fontStyle = FontStyles.Bold;
            _trackNameText.color = TextColor; _trackNameText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) _trackNameText.font = _fontAsset;

            _timeText = CreateUIObject("Time", topRow.transform).AddComponent<TextMeshProUGUI>();
            _timeText.text = "0:00 / 0:00"; _timeText.fontSize = 11; _timeText.color = AccentColor;
            _timeText.alignment = TextAlignmentOptions.MidlineRight;
            if (_fontAsset != null) _timeText.font = _fontAsset;
            _timeText.gameObject.AddComponent<LayoutElement>().preferredWidth = 90f;

            // Progress bar
            var progressBg = CreateUIObject("ProgressBg", _panel.transform);
            progressBg.AddComponent<Image>().color = ProgressBgColor;
            progressBg.AddComponent<LayoutElement>().preferredHeight = 6f;
            var fillObj = CreateUIObject("Fill", progressBg.transform);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero; fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero; fillRect.offsetMax = Vector2.zero;
            _progressFill = fillObj.AddComponent<Image>();
            _progressFill.color = ProgressFillColor; _progressFill.type = Image.Type.Filled;
            _progressFill.fillMethod = Image.FillMethod.Horizontal; _progressFill.fillAmount = 0f;

            // Controls row
            var ctrlRow = CreateUIObject("CtrlRow", _panel.transform);
            var ctrlHLayout = ctrlRow.AddComponent<HorizontalLayoutGroup>();
            ctrlHLayout.spacing = 5f; ctrlHLayout.childAlignment = TextAnchor.MiddleCenter;
            ctrlHLayout.childControlWidth = true; ctrlHLayout.childForceExpandWidth = true;

            CreateCtrlButton(ctrlRow.transform, "⏮", OnPrev);
            _playPauseBtn = CreateCtrlButton(ctrlRow.transform, "▶", OnPlayPause);
            _playPauseText = _playPauseBtn.GetComponent<TextMeshProUGUI>();
            CreateCtrlButton(ctrlRow.transform, "⏭", OnNext);

            _panel.SetActive(false);
        }

        private Button CreateCtrlButton(Transform parent, string label, System.Action onClick)
        {
            var btnObj = CreateUIObject("Btn_" + label, parent);
            btnObj.AddComponent<Image>().color = BtnColor;
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            var colors = btn.colors; colors.highlightedColor = BtnHoverColor; btn.colors = colors;
            btnObj.AddComponent<LayoutElement>().preferredWidth = 35f;
            var text = btnObj.AddComponent<TextMeshProUGUI>();
            text.text = label; text.fontSize = 16; text.color = TextColor;
            text.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) text.font = _fontAsset;
            return btn;
        }

        private void OnPlayPause()
        {
            if (_audioSource == null) return;
            if (_audioSource.isPlaying) { _audioSource.Pause(); _isPlaying = false; }
            else { _audioSource.Play(); _isPlaying = true; }
            UpdatePlayPauseText();
        }

        private void OnPrev() { if (_audioSource != null) { _audioSource.time = 0f; } }
        private void OnNext() { Debug.Log("[MusicPlayerUI] 下一曲（待实现）"); }

        private void UpdatePlayPauseText()
        {
            if (_playPauseText != null) _playPauseText.text = _isPlaying ? "⏸" : "▶";
        }

        private string FormatTime(float seconds)
        {
            int m = (int)(seconds / 60f); int s = (int)(seconds % 60f);
            return $"{m}:{s:D2}";
        }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-11bd2436e68f4ef5a9d3d5d5710e16c3/cwd.txt'; exit "$__tr_native_ec"
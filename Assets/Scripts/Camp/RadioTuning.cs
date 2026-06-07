using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.Camp
{
    // ═══════════════════════════════════════════════════════════════
    // RadioTuning — 电台调频小游戏
    //
    // 翻译自 RadioTuning.tsx
    // 核心机制：
    //   - 调频到特定频率寻找信号
    //   - 信号清晰度随频率距离变化
    //   - 清晰度>80%时可以锁定信号
    //   - 锁定信号获得叙事碎片和信号件
    //   - 45秒倒计时
    //
    // 操作：← → 调频 / 拖拽旋钮 / 锁定按钮
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 电台调频结果
    /// </summary>
    public class RadioResult
    {
        public bool Success;
        public int LockedSignals;
        public List<string> SignalRewards = new List<string>();
    }

    /// <summary>
    /// 信号点数据
    /// </summary>
    [Serializable]
    public class SignalPoint
    {
        public float Freq;
        public string Name;
        public string Narrative;
        public string FragmentId;
        public bool Locked;
    }

    /// <summary>
    /// 电台调频小游戏：调频寻找隐藏信号，锁定叙事碎片
    /// </summary>
    public class RadioTuning : MonoBehaviour
    {
        // =================================================================
        // 常量
        // =================================================================

        private const float FreqMin = 88.0f;
        private const float FreqMax = 108.0f;
        private const float SignalRange = 1.2f;
        private const float LockThreshold = 0.8f;
        private const float GameDuration = 45f;

        private static readonly Color BgColor          = new Color(0.05f, 0.06f, 0.08f, 0.95f);
        private static readonly Color TextDimColor      = new Color(1f, 1f, 1f, 0.30f);
        private static readonly Color TextNormalColor   = new Color(1f, 1f, 1f, 0.80f);
        private static readonly Color SignalGreen       = new Color(0.31f, 0.86f, 0.55f, 0.90f);
        private static readonly Color SignalBlue        = new Color(0.47f, 0.78f, 1f, 0.70f);
        private static readonly Color SignalYellow      = new Color(1f, 0.78f, 0.31f, 0.70f);
        private static readonly Color SignalRed         = new Color(1f, 0.31f, 0.31f, 0.50f);
        private static readonly Color LockBtnEnabledBg  = new Color(0.31f, 0.86f, 0.55f, 0.15f);
        private static readonly Color LockBtnDisabledBg = new Color(1f, 1f, 1f, 0.03f);
        private static readonly Color LockBtnDisabledText = new Color(1f, 1f, 1f, 0.20f);
        private static readonly Color NarrativeBg       = new Color(0f, 0f, 0f, 0.70f);
        private static readonly Color ResultBg          = new Color(1f, 1f, 1f, 0.05f);

        private const string ZpixFontPath = "Fonts/zpix";

        // =================================================================
        // 默认信号数据
        // =================================================================

        private static readonly SignalPoint[] DefaultSignals = new SignalPoint[]
        {
            new SignalPoint { Freq = 91.3f, Name = "旧日广播", Narrative = "……所有勘测员请注意，R线工程已进入第三阶段……重复，第三阶段……", FragmentId = "radio_ch1_signal" },
            new SignalPoint { Freq = 95.7f, Name = "深夜通话", Narrative = "……你还在路上吗？我已经等了三天了。如果你能听到，请回答……", FragmentId = "radio_ch2_signal" },
            new SignalPoint { Freq = 102.1f, Name = "未知频段", Narrative = "……不要到达终点。不要到达终点。不要到达终点……", FragmentId = "radio_ch3_signal" },
            new SignalPoint { Freq = 106.5f, Name = "微弱回应", Narrative = "……路没有尽头，但走路的人可以选择停下来……你听到了吗？……", FragmentId = "radio_ch4_signal" },
        };

        // =================================================================
        // 状态
        // =================================================================

        private float _frequency = 92.0f;
        private List<SignalPoint> _signals = new List<SignalPoint>();
        private float _timeLeft = GameDuration;
        private bool _finished;
        private float _knobAngle = -60f;
        private string _showNarrative;
        private float _narrativeTimer;

        // =================================================================
        // UI 引用
        // =================================================================

        private Canvas _canvas;
        private GameObject _panel;
        private TMP_FontAsset _fontAsset;

        private TMP_Text _freqDisplay;
        private TMP_Text _clarityPercentText;
        private Image _clarityBarFill;
        private Image _knobPointer;
        private RectTransform _freqPointerRect;
        private Button _lockBtn;
        private TMP_Text _lockBtnText;
        private TMP_Text _lockedCountText;
        private TMP_Text _timeLeftText;
        private GameObject _narrativePanel;
        private TMP_Text _narrativeText;
        private GameObject _resultPanel;
        private TMP_Text _resultText;

        private List<Image> _signalMarkers = new List<Image>();
        private Image[] _waveformBars;

        // =================================================================
        // 公开属性
        // =================================================================

        public bool IsFinished => _finished;

        // =================================================================
        // Unity 生命周期
        // =================================================================

        private void Update()
        {
            if (_finished) return;

            float dt = Time.deltaTime;

            _timeLeft -= dt;
            if (_timeLeft <= 0f) { _timeLeft = 0f; FinishGame(); return; }

            float step = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ? 0.5f : 0.1f;
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                _frequency = Mathf.Max(FreqMin, _frequency - step * 10f * dt);
                UpdateKnobAngle();
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                _frequency = Mathf.Min(FreqMax, _frequency + step * 10f * dt);
                UpdateKnobAngle();
            }

            if (_showNarrative != null)
            {
                _narrativeTimer -= dt;
                if (_narrativeTimer <= 0f)
                {
                    _showNarrative = null;
                    if (_narrativePanel != null) _narrativePanel.SetActive(false);
                }
            }

            RefreshUI();
        }

        private void OnDestroy()
        {
            GameEvents.OnResourcesChanged -= OnResourcesChanged;
        }

        // =================================================================
        // 游戏逻辑
        // =================================================================

        public void ShowGame()
        {
            _frequency = 92.0f;
            _timeLeft = GameDuration;
            _finished = false;
            _showNarrative = null;
            _narrativeTimer = 0f;

            _signals.Clear();
            foreach (var s in DefaultSignals)
            {
                _signals.Add(new SignalPoint
                {
                    Freq = s.Freq, Name = s.Name, Narrative = s.Narrative,
                    FragmentId = s.FragmentId, Locked = false
                });
            }

            CreateUI();
            GameEvents.OnResourcesChanged += OnResourcesChanged;
        }

        public void HideGame()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private (SignalPoint signal, float clarity) GetSignalClarity()
        {
            SignalPoint bestSignal = null;
            float bestClarity = 0f;

            foreach (var sig in _signals)
            {
                float dist = Mathf.Abs(_frequency - sig.Freq);
                if (dist < SignalRange)
                {
                    float clarity = 1f - dist / SignalRange;
                    if (clarity > bestClarity) { bestClarity = clarity; bestSignal = sig; }
                }
            }
            return (bestSignal, bestClarity);
        }

        private void LockSignal()
        {
            var (signal, clarity) = GetSignalClarity();
            if (signal == null || signal.Locked || clarity < LockThreshold || _finished) return;

            signal.Locked = true;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.AddResources(new ResourceBag { Signal = 1 });
                gm.DiscoverFragment(signal.FragmentId);
                gm.AddJournalEntry(new JournalEntry
                {
                    Id = $"radio_{signal.FragmentId}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                    FragmentId = signal.FragmentId, Chapter = "ch1", Title = signal.Name,
                    Content = signal.Narrative, CarrierType = "radio_signal", Biome = "camp",
                    DiscoveredAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), LocationName = "营地电台"
                });
            }

            _showNarrative = signal.Narrative;
            _narrativeTimer = 4f;
            if (_narrativePanel != null) { _narrativePanel.SetActive(true); if (_narrativeText != null) _narrativeText.text = signal.Narrative; }

            UpdateSignalMarkers();
        }

        private void FinishGame()
        {
            if (_finished) return;
            _finished = true;

            int lockedCount = 0;
            foreach (var s in _signals) if (s.Locked) lockedCount++;

            if (_resultPanel != null)
            {
                _resultPanel.SetActive(true);
                if (_resultText != null) _resultText.text = $"锁定信号: {lockedCount}/{_signals.Count}";
            }
        }

        private void UpdateKnobAngle()
        {
            float pct = (_frequency - FreqMin) / (FreqMax - FreqMin);
            _knobAngle = -120f + pct * 240f;
        }

        // =================================================================
        // UI 创建
        // =================================================================

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null)
                _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif

            EnsureCanvas();

            _panel = CreateUIObject("RadioTuningPanel", _canvas.transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;
            var panelImage = _panel.AddComponent<Image>();
            panelImage.color = BgColor;

            // 标题
            CreateCenteredLabel(_panel.transform, "标题", "电台调频", 0f, 340f, 12, TextDimColor, 4f);

            // 频率显示
            var freqObj = CreateUIObject("FreqDisplay", _panel.transform);
            var fRect = freqObj.AddComponent<RectTransform>();
            AnchorCenter(fRect); fRect.anchoredPosition = new Vector2(0f, 290f); fRect.sizeDelta = new Vector2(300f, 50f);
            var fLayout = freqObj.AddComponent<HorizontalLayoutGroup>();
            fLayout.spacing = 4f; fLayout.childAlignment = TextAnchor.MiddleCenter;
            fLayout.childControlWidth = false; fLayout.childForceExpandWidth = false;

            _freqDisplay = CreateUIObject("FreqValue", freqObj.transform).AddComponent<TextMeshProUGUI>();
            _freqDisplay.fontSize = 36; _freqDisplay.color = SignalBlue;
            _freqDisplay.alignment = TextAlignmentOptions.Center; SetFont(_freqDisplay);
            var fdl = _freqDisplay.gameObject.AddComponent<LayoutElement>();
            fdl.preferredWidth = 160f; fdl.preferredHeight = 50f;

            var unitText = CreateUIObject("FreqUnit", freqObj.transform).AddComponent<TextMeshProUGUI>();
            unitText.text = "MHz"; unitText.fontSize = 14; unitText.color = TextDimColor;
            unitText.alignment = TextAlignmentOptions.Center; SetFont(unitText);
            var ul = unitText.gameObject.AddComponent<LayoutElement>();
            ul.preferredWidth = 60f; ul.preferredHeight = 50f;

            // 波形
            CreateWaveform();
            // 信号强度
            CreateClarityBar();
            // 频率刻度
            CreateFreqScale();
            // 旋钮
            CreateKnob();
            // 锁定按钮
            CreateLockButton();
            // 信息栏
            CreateInfoBar();
            // 提示
            CreateCenteredLabel(_panel.transform, "Hint", "← → 调频 · 拖拽旋钮 · 信号>80%时锁定", 0f, -75f, 8, new Color(1f,1f,1f,0.15f), 2f);
            // 叙事弹窗
            CreateNarrativePanel();
            // 结算
            CreateResultPanel();
        }

        private void CreateWaveform()
        {
            var waveContainer = CreateUIObject("Waveform", _panel.transform);
            var wcRect = waveContainer.AddComponent<RectTransform>();
            AnchorCenter(wcRect); wcRect.anchoredPosition = new Vector2(0f, 230f); wcRect.sizeDelta = new Vector2(220f, 50f);
            var bgImage = waveContainer.AddComponent<Image>(); bgImage.color = new Color(0f, 0f, 0f, 0.40f);
            var waveLayout = waveContainer.AddComponent<HorizontalLayoutGroup>();
            waveLayout.padding = new RectOffset(10, 10, 5, 5); waveLayout.spacing = 1f;
            waveLayout.childAlignment = TextAnchor.MiddleCenter;
            waveLayout.childControlWidth = true; waveLayout.childForceExpandWidth = true;

            int barCount = 50;
            _waveformBars = new Image[barCount];
            for (int i = 0; i < barCount; i++)
            {
                var barObj = CreateUIObject($"Bar_{i}", waveContainer.transform);
                var barImg = barObj.AddComponent<Image>();
                barImg.color = new Color(0.47f, 0.78f, 1f, 0.4f);
                var bl = barObj.AddComponent<LayoutElement>();
                bl.flexibleWidth = 1f; bl.minWidth = 2f;
                _waveformBars[i] = barImg;
            }
        }

        private void CreateClarityBar()
        {
            var clarityContainer = CreateUIObject("ClarityBar", _panel.transform);
            var ccRect = clarityContainer.AddComponent<RectTransform>();
            AnchorCenter(ccRect); ccRect.anchoredPosition = new Vector2(0f, 185f); ccRect.sizeDelta = new Vector2(200f, 30f);
            var vLayout = clarityContainer.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 2f; vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true; vLayout.childForceExpandWidth = true;

            var labelRow = CreateUIObject("ClarityLabelRow", clarityContainer.transform);
            var lrLayout = labelRow.AddComponent<HorizontalLayoutGroup>();
            lrLayout.spacing = 6f; lrLayout.childAlignment = TextAnchor.MiddleCenter; lrLayout.childControlWidth = false;

            var clLabel = CreateUIObject("ClarityLabel", labelRow.transform).AddComponent<TextMeshProUGUI>();
            clLabel.text = "信号"; clLabel.fontSize = 8; clLabel.color = new Color(1f,1f,1f,0.25f); SetFont(clLabel);

            _clarityPercentText = CreateUIObject("ClarityPct", labelRow.transform).AddComponent<TextMeshProUGUI>();
            _clarityPercentText.fontSize = 9; _clarityPercentText.color = SignalRed; SetFont(_clarityPercentText);

            var barObj = CreateUIObject("ClarityBarBg", clarityContainer.transform);
            barObj.AddComponent<Image>().color = new Color(1f,1f,1f,0.06f);
            var bl = barObj.AddComponent<LayoutElement>(); bl.preferredHeight = 8f;

            var fillObj = CreateUIObject("ClarityBarFill", barObj.transform);
            var fRect = fillObj.AddComponent<RectTransform>();
            fRect.anchorMin = Vector2.zero; fRect.anchorMax = new Vector2(0f, 1f);
            fRect.offsetMin = Vector2.zero; fRect.offsetMax = Vector2.zero;
            _clarityBarFill = fillObj.AddComponent<Image>(); _clarityBarFill.color = SignalRed;

            var thresholdObj = CreateUIObject("Threshold", barObj.transform);
            var tRect = thresholdObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(LockThreshold, 0f); tRect.anchorMax = new Vector2(LockThreshold, 1f);
            tRect.pivot = new Vector2(0.5f, 0.5f); tRect.sizeDelta = new Vector2(1f, 0f);
            thresholdObj.AddComponent<Image>().color = new Color(0.31f, 0.86f, 0.55f, 0.30f);
        }

        private void CreateFreqScale()
        {
            var scaleContainer = CreateUIObject("FreqScale", _panel.transform);
            var scRect = scaleContainer.AddComponent<RectTransform>();
            AnchorCenter(scRect); scRect.anchoredPosition = new Vector2(0f, 140f); scRect.sizeDelta = new Vector2(320f, 40f);

            int[] freqMarks = { 88, 90, 92, 94, 96, 98, 100, 102, 104, 106, 108 };
            foreach (int f in freqMarks)
            {
                float pct = (f - FreqMin) / (FreqMax - FreqMin);
                var tickObj = CreateUIObject($"Tick_{f}", scaleContainer.transform);
                var tRect = tickObj.AddComponent<RectTransform>();
                tRect.anchorMin = new Vector2(pct, 0.5f); tRect.anchorMax = new Vector2(pct, 0.5f);
                tRect.pivot = new Vector2(0.5f, 0.5f); tRect.sizeDelta = new Vector2(1f, 12f);
                tRect.anchoredPosition = Vector2.zero;
                tickObj.AddComponent<Image>().color = new Color(1f,1f,1f,0.15f);

                var labelObj = CreateUIObject($"TickLabel_{f}", scaleContainer.transform);
                var lRect = labelObj.AddComponent<RectTransform>();
                lRect.anchorMin = new Vector2(pct, 0f); lRect.anchorMax = new Vector2(pct, 0f);
                lRect.pivot = new Vector2(0.5f, 0f); lRect.sizeDelta = new Vector2(24f, 12f);
                lRect.anchoredPosition = new Vector2(0f, -2f);
                var lText = labelObj.AddComponent<TextMeshProUGUI>();
                lText.text = f.ToString(); lText.fontSize = 7; lText.color = new Color(1f,1f,1f,0.20f);
                lText.alignment = TextAlignmentOptions.Center; SetFont(lText);
            }

            _signalMarkers.Clear();
            foreach (var sig in _signals)
            {
                float pct = (sig.Freq - FreqMin) / (FreqMax - FreqMin);
                var markerObj = CreateUIObject($"SignalMarker_{sig.Freq}", scaleContainer.transform);
                var mRect = markerObj.AddComponent<RectTransform>();
                mRect.anchorMin = new Vector2(pct, 0.7f); mRect.anchorMax = new Vector2(pct, 0.7f);
                mRect.pivot = new Vector2(0.5f, 0.5f); mRect.sizeDelta = new Vector2(6f, 6f);
                mRect.anchoredPosition = Vector2.zero;
                var mImg = markerObj.AddComponent<Image>(); mImg.color = new Color(0.47f, 0.78f, 1f, 0.40f);
                _signalMarkers.Add(mImg);
            }

            var pointerObj = CreateUIObject("FreqPointer", scaleContainer.transform);
            _freqPointerRect = pointerObj.AddComponent<RectTransform>();
            _freqPointerRect.anchorMin = new Vector2(0.5f, 0f); _freqPointerRect.anchorMax = new Vector2(0.5f, 1f);
            _freqPointerRect.pivot = new Vector2(0.5f, 0.5f); _freqPointerRect.sizeDelta = new Vector2(2f, 0f);
            pointerObj.AddComponent<Image>().color = new Color(1f,1f,1f,0.80f);
        }

        private void CreateKnob()
        {
            var knobContainer = CreateUIObject("Knob", _panel.transform);
            var kRect = knobContainer.AddComponent<RectTransform>();
            AnchorCenter(kRect); kRect.anchoredPosition = new Vector2(0f, 60f); kRect.sizeDelta = new Vector2(90f, 90f);

            var ringObj = CreateUIObject("KnobRing", knobContainer.transform);
            var rRect = ringObj.AddComponent<RectTransform>();
            rRect.anchorMin = Vector2.zero; rRect.anchorMax = Vector2.one;
            rRect.offsetMin = Vector2.zero; rRect.offsetMax = Vector2.zero;
            ringObj.AddComponent<Image>().color = new Color(1f,1f,1f,0.03f);

            for (int i = 0; i < 12; i++)
            {
                float a = (i / 12f) * Mathf.PI * 2f - Mathf.PI / 2f;
                float cx = 0.5f + Mathf.Cos(a) * 0.38f;
                float cy = 0.5f + Mathf.Sin(a) * 0.38f;
                var dotObj = CreateUIObject($"Dot_{i}", knobContainer.transform);
                var dRect = dotObj.AddComponent<RectTransform>();
                dRect.anchorMin = new Vector2(cx, cy); dRect.anchorMax = new Vector2(cx, cy);
                dRect.pivot = new Vector2(0.5f, 0.5f); dRect.sizeDelta = new Vector2(3f, 3f);
                dRect.anchoredPosition = Vector2.zero;
                dotObj.AddComponent<Image>().color = new Color(1f,1f,1f,0.15f);
            }

            var pointerObj = CreateUIObject("KnobPointer", knobContainer.transform);
            var pRect = pointerObj.AddComponent<RectTransform>();
            pRect.anchorMin = new Vector2(0.5f, 0.5f); pRect.anchorMax = new Vector2(0.5f, 0.5f);
            pRect.pivot = new Vector2(0.5f, 0f); pRect.sizeDelta = new Vector2(3f, 28f);
            pRect.anchoredPosition = Vector2.zero;
            _knobPointer = pointerObj.AddComponent<Image>();
            _knobPointer.color = new Color(0.47f, 0.78f, 1f, 0.80f);

            var centerObj = CreateUIObject("KnobCenter", knobContainer.transform);
            var cRect = centerObj.AddComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0.5f, 0.5f); cRect.anchorMax = new Vector2(0.5f, 0.5f);
            cRect.pivot = new Vector2(0.5f, 0.5f); cRect.sizeDelta = new Vector2(10f, 10f);
            cRect.anchoredPosition = Vector2.zero;
            centerObj.AddComponent<Image>().color = new Color(0.47f, 0.78f, 1f, 0.30f);

            var dragHandler = knobContainer.AddComponent<KnobDragHandler>();
            dragHandler.OnDragDelta = (deltaX) =>
            {
                if (_finished) return;
                _frequency = Mathf.Clamp(_frequency + deltaX * 0.05f, FreqMin, FreqMax);
                _frequency = Mathf.Round(_frequency * 10f) / 10f;
                UpdateKnobAngle();
            };
        }

        private void CreateLockButton()
        {
            var btnObj = CreateUIObject("LockBtn", _panel.transform);
            var btnRect = btnObj.AddComponent<RectTransform>();
            AnchorCenter(btnRect); btnRect.anchoredPosition = new Vector2(0f, -10f); btnRect.sizeDelta = new Vector2(160f, 36f);
            btnObj.AddComponent<Image>().color = LockBtnDisabledBg;
            _lockBtn = btnObj.AddComponent<Button>();
            _lockBtn.onClick.AddListener(LockSignal);
            _lockBtnText = btnObj.AddComponent<TextMeshProUGUI>();
            _lockBtnText.text = "锁定信号"; _lockBtnText.fontSize = 14;
            _lockBtnText.color = LockBtnDisabledText; _lockBtnText.alignment = TextAlignmentOptions.Center;
            _lockBtnText.characterSpacing = 2f; SetFont(_lockBtnText);
        }

        private void CreateInfoBar()
        {
            var infoObj = CreateUIObject("InfoBar", _panel.transform);
            var iRect = infoObj.AddComponent<RectTransform>();
            AnchorCenter(iRect); iRect.anchoredPosition = new Vector2(0f, -50f); iRect.sizeDelta = new Vector2(300f, 20f);
            var hLayout = infoObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 30f; hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = true; hLayout.childForceExpandWidth = true;

            var lockedObj = CreateUIObject("LockedInfo", infoObj.transform);
            var ll = lockedObj.AddComponent<HorizontalLayoutGroup>();
            ll.spacing = 4f; ll.childAlignment = TextAnchor.MiddleCenter; ll.childControlWidth = false;
            var lockedLabel = CreateUIObject("LockedLabel", lockedObj.transform).AddComponent<TextMeshProUGUI>();
            lockedLabel.text = "已锁定:"; lockedLabel.fontSize = 9; lockedLabel.color = TextDimColor; SetFont(lockedLabel);
            _lockedCountText = CreateUIObject("LockedValue", lockedObj.transform).AddComponent<TextMeshProUGUI>();
            _lockedCountText.fontSize = 9; _lockedCountText.color = TextNormalColor; SetFont(_lockedCountText);

            var timeObj = CreateUIObject("TimeInfo", infoObj.transform);
            var tl = timeObj.AddComponent<HorizontalLayoutGroup>();
            tl.spacing = 4f; tl.childAlignment = TextAnchor.MiddleCenter; tl.childControlWidth = false;
            var timeLabel = CreateUIObject("TimeLabel", timeObj.transform).AddComponent<TextMeshProUGUI>();
            timeLabel.text = "剩余:"; timeLabel.fontSize = 9; timeLabel.color = TextDimColor; SetFont(timeLabel);
            _timeLeftText = CreateUIObject("TimeValue", timeObj.transform).AddComponent<TextMeshProUGUI>();
            _timeLeftText.fontSize = 9; _timeLeftText.color = TextNormalColor; SetFont(_timeLeftText);
        }

        private void CreateNarrativePanel()
        {
            _narrativePanel = CreateUIObject("NarrativePanel", _panel.transform);
            var npRect = _narrativePanel.AddComponent<RectTransform>();
            npRect.anchorMin = new Vector2(0.5f, 0f); npRect.anchorMax = new Vector2(0.5f, 0f);
            npRect.pivot = new Vector2(0.5f, 0f); npRect.anchoredPosition = new Vector2(0f, 40f);
            npRect.sizeDelta = new Vector2(320f, 70f);
            _narrativePanel.AddComponent<Image>().color = NarrativeBg;

            var npLayout = _narrativePanel.AddComponent<VerticalLayoutGroup>();
            npLayout.padding = new RectOffset(16, 16, 10, 10); npLayout.spacing = 4f;
            npLayout.childAlignment = TextAnchor.MiddleCenter;
            npLayout.childControlWidth = true; npLayout.childForceExpandWidth = true;

            var npLabel = CreateUIObject("NarrativeLabel", _narrativePanel.transform).AddComponent<TextMeshProUGUI>();
            npLabel.text = "信号捕获"; npLabel.fontSize = 9;
            npLabel.color = new Color(0.31f, 0.86f, 0.55f, 0.50f);
            npLabel.alignment = TextAlignmentOptions.Center; npLabel.characterSpacing = 2f; SetFont(npLabel);

            _narrativeText = CreateUIObject("NarrativeContent", _narrativePanel.transform).AddComponent<TextMeshProUGUI>();
            _narrativeText.fontSize = 12; _narrativeText.color = new Color(1f,1f,1f,0.70f);
            _narrativeText.alignment = TextAlignmentOptions.Center;
            _narrativeText.textWrappingMode = TextWrappingModes.Normal; SetFont(_narrativeText);

            _narrativePanel.SetActive(false);
        }

        private void CreateResultPanel()
        {
            _resultPanel = CreateUIObject("ResultPanel", _panel.transform);
            var rpRect = _resultPanel.AddComponent<RectTransform>();
            rpRect.anchorMin = new Vector2(1f, 1f); rpRect.anchorMax = new Vector2(1f, 1f);
            rpRect.pivot = new Vector2(1f, 1f); rpRect.anchoredPosition = new Vector2(-20f, -20f);
            rpRect.sizeDelta = new Vector2(180f, 60f);
            _resultPanel.AddComponent<Image>().color = ResultBg;

            var rpLayout = _resultPanel.AddComponent<VerticalLayoutGroup>();
            rpLayout.padding = new RectOffset(12, 12, 8, 8); rpLayout.spacing = 4f;
            rpLayout.childAlignment = TextAnchor.MiddleCenter;
            rpLayout.childControlWidth = true; rpLayout.childForceExpandWidth = true;

            var rpLabel = CreateUIObject("ResultLabel", _resultPanel.transform).AddComponent<TextMeshProUGUI>();
            rpLabel.text = "调频结束"; rpLabel.fontSize = 10; rpLabel.color = TextDimColor;
            rpLabel.alignment = TextAlignmentOptions.Center; rpLabel.characterSpacing = 4f; SetFont(rpLabel);

            _resultText = CreateUIObject("ResultContent", _resultPanel.transform).AddComponent<TextMeshProUGUI>();
            _resultText.fontSize = 12; _resultText.color = new Color(1f,1f,1f,0.60f);
            _resultText.alignment = TextAlignmentOptions.Center; SetFont(_resultText);

            _resultPanel.SetActive(false);
        }

        // =================================================================
        // UI 刷新
        // =================================================================

        private void RefreshUI()
        {
            var (signal, clarity) = GetSignalClarity();

            if (_freqDisplay != null)
            {
                _freqDisplay.text = _frequency.ToString("F1");
                _freqDisplay.color = clarity > 0.6f ? SignalGreen : SignalBlue;
            }

            if (_clarityPercentText != null)
            {
                _clarityPercentText.text = Mathf.RoundToInt(clarity * 100f) + "%";
                _clarityPercentText.color = clarity > LockThreshold ? SignalGreen
                    : clarity > 0.4f ? SignalYellow : SignalRed;
            }

            if (_clarityBarFill != null)
            {
                _clarityBarFill.rectTransform.anchorMax = new Vector2(clarity, 1f);
                _clarityBarFill.color = clarity > LockThreshold ? SignalGreen
                    : clarity > 0.4f ? SignalYellow : SignalRed;
            }

            if (_freqPointerRect != null)
            {
                float pct = (_frequency - FreqMin) / (FreqMax - FreqMin);
                _freqPointerRect.anchorMin = new Vector2(pct, 0f);
                _freqPointerRect.anchorMax = new Vector2(pct, 1f);
            }

            if (_knobPointer != null)
                _knobPointer.rectTransform.localEulerAngles = new Vector3(0f, 0f, -_knobAngle);

            if (_lockBtn != null)
            {
                bool canLock = signal != null && !signal.Locked && clarity >= LockThreshold && !_finished;
                _lockBtn.interactable = canLock;
                _lockBtn.GetComponent<Image>().color = canLock ? LockBtnEnabledBg : LockBtnDisabledBg;
                if (_lockBtnText != null)
                {
                    _lockBtnText.text = signal?.Locked == true ? "已锁定" : "锁定信号";
                    _lockBtnText.color = canLock ? SignalGreen : LockBtnDisabledText;
                }
            }

            int lockedCount = 0;
            foreach (var s in _signals) if (s.Locked) lockedCount++;
            if (_lockedCountText != null) _lockedCountText.text = $"{lockedCount}/{_signals.Count}";
            if (_timeLeftText != null) _timeLeftText.text = Mathf.CeilToInt(_timeLeft) + "s";

            UpdateWaveform(clarity);
        }

        private void UpdateWaveform(float clarity)
        {
            if (_waveformBars == null) return;
            for (int i = 0; i < _waveformBars.Length; i++)
            {
                float noiseAmp = (1f - clarity) * 0.5f;
                float signalAmp = clarity * 0.4f;
                float noise = (UnityEngine.Random.value - 0.5f) * 2f * noiseAmp;
                float sig = Mathf.Sin((i / (float)_waveformBars.Length) * Mathf.PI * 6f) * signalAmp;
                float h = Mathf.Abs(noise + sig);
                var bar = _waveformBars[i];
                if (bar != null)
                {
                    var le = bar.GetComponent<LayoutElement>();
                    if (le != null) le.preferredHeight = Mathf.Max(2f, h * 40f + 2f);
                    bar.color = clarity > 0.6f
                        ? new Color(0.31f, 0.86f, 0.55f, 0.70f)
                        : new Color(0.47f, 0.78f, 1f, 0.40f);
                }
            }
        }

        private void UpdateSignalMarkers()
        {
            for (int i = 0; i < _signalMarkers.Count && i < _signals.Count; i++)
            {
                if (_signals[i].Locked)
                    _signalMarkers[i].color = new Color(0.31f, 0.86f, 0.55f, 0.80f);
            }
        }

        private void OnResourcesChanged(ResourceBag resources) { }

        // =================================================================
        // 辅助方法
        // =================================================================

        private void EnsureCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                _canvas.sortingOrder = 900;
                var scaler = gameObject.GetComponent<CanvasScaler>();
                if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
                if (gameObject.GetComponent<GraphicRaycaster>() == null)
                    gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private void AnchorCenter(RectTransform rect)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
        }

        private void CreateCenteredLabel(Transform parent, string name, string text, float x, float y, int fontSize, Color color, float charSpacing)
        {
            var obj = CreateUIObject(name, parent);
            var rect = obj.AddComponent<RectTransform>();
            AnchorCenter(rect); rect.anchoredPosition = new Vector2(x, y); rect.sizeDelta = new Vector2(400f, 24f);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = fontSize; tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center; tmp.characterSpacing = charSpacing; SetFont(tmp);
        }

        private void SetFont(TMP_Text tmp) { if (_fontAsset != null) tmp.font = _fontAsset; }

        // =================================================================
        // 旋钮拖拽辅助类
        // =================================================================

        private class KnobDragHandler : MonoBehaviour, IDragHandler
        {
            public System.Action<float> OnDragDelta;
            public void OnDrag(UnityEngine.EventSystems.PointerEventData eventData)
            {
                OnDragDelta?.Invoke(eventData.delta.x);
            }
        }
    }
}

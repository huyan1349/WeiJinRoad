using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.Camp
{
    // ═══════════════════════════════════════════════════════════════
    // CampfireGame — 篝火维护小游戏
    //
    // 翻译自 CampfireGame.tsx
    // 核心机制：
    //   - 温度自然衰减，需添柴维持
    //   - 温度保持在最佳区间(40~70)越久，奖励越高
    //   - 温度过低火灭，游戏提前结束
    //   - 30秒倒计时
    //   - 奖励：部件耐久恢复（最佳区间>80%→+0.05，>50%→+0.02）
    //
    // 操作：空格键/按钮添柴
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 篝火小游戏结果
    /// </summary>
    public class CampfireResult
    {
        public bool Success;
        public float OptimalPercent;
        public float ConditionBonus;
    }

    /// <summary>
    /// 篝火维护小游戏：保持火焰燃烧，温度维持在最佳区间
    /// </summary>
    public class CampfireGame : MonoBehaviour
    {
        // =================================================================
        // 常量
        // =================================================================

        private const float Duration = 30f;
        private const float DecayRate = 8f;
        private const float AddHeat = 15f;
        private const float WoodCost = 0.5f;
        private const float OptimalLow = 40f;
        private const float OptimalHigh = 70f;
        private const float DangerLow = 20f;
        private const float DangerHigh = 85f;

        // ── 颜色 ──
        private static readonly Color ColdColor       = new Color(0.31f, 0.55f, 1f, 0.9f);
        private static readonly Color CoolColor        = new Color(0.39f, 0.71f, 1f, 0.8f);
        private static readonly Color OptimalColor     = new Color(0.31f, 0.86f, 0.55f, 0.9f);
        private static readonly Color HotColor         = new Color(1f, 0.71f, 0.24f, 0.8f);
        private static readonly Color OverheatColor    = new Color(1f, 0.31f, 0.24f, 0.9f);

        private static readonly Color BgColor          = new Color(0.05f, 0.06f, 0.08f, 0.95f);
        private static readonly Color TextDimColor     = new Color(1f, 1f, 1f, 0.30f);
        private static readonly Color TextNormalColor  = new Color(1f, 1f, 1f, 0.80f);
        private static readonly Color OptimalZoneColor = new Color(0.31f, 0.86f, 0.55f, 0.12f);
        private static readonly Color BarBgColor       = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color BtnNormalBg      = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color BtnDisabledBg    = new Color(1f, 1f, 1f, 0.03f);
        private static readonly Color BtnDisabledText  = new Color(1f, 1f, 1f, 0.20f);
        private static readonly Color ResultBg         = new Color(1f, 1f, 1f, 0.05f);
        private static readonly Color GreenDim         = new Color(0.31f, 0.86f, 0.55f, 0.50f);
        private static readonly Color RedDim           = new Color(1f, 0.40f, 0.31f, 0.60f);

        private const string ZpixFontPath = "Fonts/zpix";

        // =================================================================
        // 状态
        // =================================================================

        private float _temperature = 50f;
        private float _timeLeft = Duration;
        private float _optimalTime;
        private float _totalTime;
        private bool _finished;
        private bool _fireDead;
        private bool _sparkBurst;
        private float _sparkTimer;

        // =================================================================
        // UI 引用
        // =================================================================

        private Canvas _canvas;
        private GameObject _panel;
        private TMP_FontAsset _fontAsset;

        private Image _tempBarFill;
        private TMP_Text _woodText;
        private TMP_Text _optimalPercentText;
        private TMP_Text _timeLeftText;
        private Button _addWoodBtn;
        private TMP_Text _addWoodBtnText;
        private GameObject _fireDeadLabel;
        private GameObject _resultPanel;
        private TMP_Text _resultOptimalText;
        private TMP_Text _resultBonusText;
        private TMP_Text _resultFlavorText;

        // 火焰视觉
        private Image _outerFlame;
        private Image _midFlame;
        private Image _innerFlame;
        private float _flameAnimTime;

        // 火花粒子
        private GameObject[] _sparkParticles = new GameObject[6];

        // =================================================================
        // 公开属性
        // =================================================================

        /// <summary>游戏是否已结束</summary>
        public bool IsFinished => _finished;

        // =================================================================
        // Unity 生命周期
        // =================================================================

        private void Update()
        {
            if (_finished) return;

            float dt = Time.deltaTime;

            // 温度衰减
            if (!_fireDead)
            {
                _temperature = Mathf.Max(0f, _temperature - DecayRate * dt);
                _totalTime += dt;

                if (_temperature >= OptimalLow && _temperature <= OptimalHigh)
                {
                    _optimalTime += dt;
                }

                if (_temperature <= 0f)
                {
                    _fireDead = true;
                    if (_fireDeadLabel != null) _fireDeadLabel.SetActive(true);
                }
            }

            // 倒计时
            _timeLeft -= dt;
            if (_timeLeft <= 0f)
            {
                _timeLeft = 0f;
                FinishGame();
            }

            // 火花效果计时
            if (_sparkBurst)
            {
                _sparkTimer -= dt;
                if (_sparkTimer <= 0f)
                {
                    _sparkBurst = false;
                    foreach (var sp in _sparkParticles)
                        if (sp != null) sp.SetActive(false);
                }
            }

            RefreshUI();
            UpdateFlameVisual(dt);

            // 空格键添柴
            if (Input.GetKeyDown(KeyCode.Space))
            {
                AddWood();
            }
        }

        private void OnDestroy()
        {
            GameEvents.OnResourcesChanged -= OnResourcesChanged;
        }

        // =================================================================
        // 游戏逻辑
        // =================================================================

        /// <summary>
        /// 初始化并显示篝火小游戏
        /// </summary>
        public void ShowGame()
        {
            _temperature = 50f;
            _timeLeft = Duration;
            _optimalTime = 0f;
            _totalTime = 0f;
            _finished = false;
            _fireDead = false;
            _sparkBurst = false;

            CreateUI();
            GameEvents.OnResourcesChanged += OnResourcesChanged;
        }

        /// <summary>
        /// 隐藏篝火小游戏
        /// </summary>
        public void HideGame()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void AddWood()
        {
            if (_finished || _fireDead) return;

            var gm = GameManager.Instance;
            if (gm == null) return;

            if (gm.Resources.Wood < 1) return;

            var cost = new ResourceBag { Wood = 1 };
            if (!gm.SpendResources(cost)) return;

            _temperature = Mathf.Min(100f, _temperature + AddHeat);

            _sparkBurst = true;
            _sparkTimer = 0.3f;
            foreach (var sp in _sparkParticles)
                if (sp != null) sp.SetActive(true);
        }

        private void FinishGame()
        {
            if (_finished) return;
            _finished = true;

            float optPercent = _totalTime > 0f ? _optimalTime / _totalTime : 0f;
            float conditionBonus = 0f;

            if (optPercent > 0.8f) conditionBonus = 0.05f;
            else if (optPercent > 0.5f) conditionBonus = 0.02f;

            if (conditionBonus > 0f)
            {
                var gm = GameManager.Instance;
                if (gm != null)
                {
                    foreach (PartId pid in Enum.GetValues(typeof(PartId)))
                    {
                        float cur = gm.GetPartState(pid).Condition;
                        gm.SetPartCondition(pid, Mathf.Min(1f, cur + conditionBonus));
                    }
                }
            }

            ShowResult(optPercent, conditionBonus);
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

            _panel = CreateUIObject("CampfireGamePanel", _canvas.transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = _panel.AddComponent<Image>();
            panelImage.color = BgColor;

            // ── 标题 ──
            var titleObj = CreateUIObject("Title", _panel.transform);
            var titleRect = titleObj.AddComponent<RectTransform>();
            AnchorCenter(titleRect);
            titleRect.anchoredPosition = new Vector2(0f, 280f);
            titleRect.sizeDelta = new Vector2(200f, 24f);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "篝火维护";
            titleText.fontSize = 12;
            titleText.color = TextDimColor;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.characterSpacing = 4f;
            SetFont(titleText);

            CreateFlameVisual();
            CreateTemperatureBar();
            CreateInfoBar();
            CreateAddWoodButton();

            // ── 火灭提示 ──
            _fireDeadLabel = CreateUIObject("FireDeadLabel", _panel.transform);
            var fdlRect = _fireDeadLabel.AddComponent<RectTransform>();
            AnchorCenter(fdlRect);
            fdlRect.anchoredPosition = new Vector2(0f, -200f);
            fdlRect.sizeDelta = new Vector2(200f, 20f);
            var fdlText = _fireDeadLabel.AddComponent<TextMeshProUGUI>();
            fdlText.text = "火已熄灭，等待结束...";
            fdlText.fontSize = 10;
            fdlText.color = RedDim;
            fdlText.alignment = TextAlignmentOptions.Center;
            fdlText.characterSpacing = 2f;
            SetFont(fdlText);
            _fireDeadLabel.SetActive(false);

            // ── 结算面板 ──
            _resultPanel = CreateUIObject("ResultPanel", _panel.transform);
            var rpRect = _resultPanel.AddComponent<RectTransform>();
            AnchorCenter(rpRect);
            rpRect.anchoredPosition = new Vector2(0f, -240f);
            rpRect.sizeDelta = new Vector2(280f, 120f);
            var rpImage = _resultPanel.AddComponent<Image>();
            rpImage.color = ResultBg;
            _resultPanel.SetActive(false);

            var rpLayout = _resultPanel.AddComponent<VerticalLayoutGroup>();
            rpLayout.padding = new RectOffset(20, 20, 15, 15);
            rpLayout.spacing = 6f;
            rpLayout.childAlignment = TextAnchor.MiddleCenter;
            rpLayout.childControlWidth = true;
            rpLayout.childForceExpandWidth = true;
            rpLayout.childForceExpandHeight = false;

            var rpTitleObj = CreateUIObject("ResultTitle", _resultPanel.transform);
            var rpTitleText = rpTitleObj.AddComponent<TextMeshProUGUI>();
            rpTitleText.text = "结 算";
            rpTitleText.fontSize = 10;
            rpTitleText.color = TextDimColor;
            rpTitleText.alignment = TextAlignmentOptions.Center;
            rpTitleText.characterSpacing = 4f;
            SetFont(rpTitleText);
            var rpTitleLayout = rpTitleObj.AddComponent<LayoutElement>();
            rpTitleLayout.preferredHeight = 18f;

            _resultOptimalText = CreateLabel(_resultPanel.transform, "");
            _resultBonusText = CreateLabel(_resultPanel.transform, "");
            _resultFlavorText = CreateLabel(_resultPanel.transform, "");
            _resultFlavorText.color = GreenDim;
        }

        private void CreateFlameVisual()
        {
            var flameContainer = CreateUIObject("FlameContainer", _panel.transform);
            var fcRect = flameContainer.AddComponent<RectTransform>();
            AnchorCenter(fcRect);
            fcRect.anchoredPosition = new Vector2(0f, 120f);
            fcRect.sizeDelta = new Vector2(120f, 140f);

            _outerFlame = CreateFlameLayer(flameContainer.transform, new Color(1f, 0.39f, 0.08f, 0.30f), new Vector2(60f, 90f));
            _midFlame = CreateFlameLayer(flameContainer.transform, new Color(1f, 0.63f, 0.16f, 0.60f), new Vector2(44f, 68f));
            _innerFlame = CreateFlameLayer(flameContainer.transform, new Color(1f, 0.90f, 0.47f, 0.80f), new Vector2(26f, 42f));

            // 柴堆
            var wood1 = CreateUIObject("Wood1", flameContainer.transform);
            var w1Rect = wood1.AddComponent<RectTransform>();
            AnchorCenter(w1Rect);
            w1Rect.anchoredPosition = new Vector2(0f, -48f);
            w1Rect.sizeDelta = new Vector2(60f, 8f);
            var w1Img = wood1.AddComponent<Image>();
            w1Img.color = new Color(0.29f, 0.20f, 0.13f, 1f);

            var wood2 = CreateUIObject("Wood2", flameContainer.transform);
            var w2Rect = wood2.AddComponent<RectTransform>();
            AnchorCenter(w2Rect);
            w2Rect.anchoredPosition = new Vector2(0f, -42f);
            w2Rect.sizeDelta = new Vector2(50f, 6f);
            w2Rect.localEulerAngles = new Vector3(0f, 0f, -8f);
            var w2Img = wood2.AddComponent<Image>();
            w2Img.color = new Color(0.23f, 0.16f, 0.09f, 1f);

            // 火花粒子
            for (int i = 0; i < _sparkParticles.Length; i++)
            {
                var spark = CreateUIObject($"Spark_{i}", flameContainer.transform);
                var sRect = spark.AddComponent<RectTransform>();
                AnchorCenter(sRect);
                sRect.anchoredPosition = new Vector2(
                    (UnityEngine.Random.value - 0.5f) * 60f,
                    -20f - UnityEngine.Random.value * 40f
                );
                sRect.sizeDelta = new Vector2(4f, 4f);
                var sImg = spark.AddComponent<Image>();
                sImg.color = new Color(1f, 0.78f, 0.31f, 0.9f);
                spark.SetActive(false);
                _sparkParticles[i] = spark;
            }
        }

        private Image CreateFlameLayer(Transform parent, Color color, Vector2 size)
        {
            var obj = CreateUIObject("FlameLayer", parent);
            var rect = obj.AddComponent<RectTransform>();
            AnchorCenter(rect);
            rect.sizeDelta = size;
            var img = obj.AddComponent<Image>();
            img.color = color;
            return img;
        }

        private void CreateTemperatureBar()
        {
            var barContainer = CreateUIObject("TempBarContainer", _panel.transform);
            var bcRect = barContainer.AddComponent<RectTransform>();
            AnchorCenter(bcRect);
            bcRect.anchoredPosition = new Vector2(0f, 20f);
            bcRect.sizeDelta = new Vector2(340f, 40f);

            // 最佳区间标记
            var optZone = CreateUIObject("OptimalZone", barContainer.transform);
            var ozRect = optZone.AddComponent<RectTransform>();
            ozRect.anchorMin = new Vector2(OptimalLow / 100f, 0f);
            ozRect.anchorMax = new Vector2(OptimalHigh / 100f, 1f);
            ozRect.offsetMin = Vector2.zero;
            ozRect.offsetMax = Vector2.zero;
            var ozImg = optZone.AddComponent<Image>();
            ozImg.color = OptimalZoneColor;

            // 温度条背景
            var barBg = CreateUIObject("BarBg", barContainer.transform);
            var bgRect = barBg.AddComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;
            var bgImg = barBg.AddComponent<Image>();
            bgImg.color = BarBgColor;

            // 温度条填充
            var barFill = CreateUIObject("BarFill", barBg.transform);
            var bfRect = barFill.AddComponent<RectTransform>();
            bfRect.anchorMin = Vector2.zero;
            bfRect.anchorMax = new Vector2(0.5f, 1f);
            bfRect.offsetMin = Vector2.zero;
            bfRect.offsetMax = Vector2.zero;
            _tempBarFill = barFill.AddComponent<Image>();
            _tempBarFill.color = OptimalColor;

            // 温度指示器
            var indicator = CreateUIObject("Indicator", barFill.transform);
            var iRect = indicator.AddComponent<RectTransform>();
            iRect.anchorMin = new Vector2(1f, 0f);
            iRect.anchorMax = new Vector2(1f, 1f);
            iRect.sizeDelta = new Vector2(3f, 0f);
            iRect.pivot = new Vector2(0.5f, 0.5f);
            var iImg = indicator.AddComponent<Image>();
            iImg.color = new Color(1f, 1f, 1f, 0.9f);

            // 刻度标签
            var labelContainer = CreateUIObject("Labels", barContainer.transform);
            var lcRect = labelContainer.AddComponent<RectTransform>();
            lcRect.anchorMin = Vector2.zero;
            lcRect.anchorMax = new Vector2(1f, 0f);
            lcRect.offsetMin = new Vector2(0f, -16f);
            lcRect.offsetMax = new Vector2(0f, 0f);
            var lcLayout = labelContainer.AddComponent<HorizontalLayoutGroup>();
            lcLayout.childAlignment = TextAnchor.MiddleCenter;
            lcLayout.childControlWidth = true;
            lcLayout.childForceExpandWidth = true;

            CreateSmallLabel(labelContainer.transform, "冷", new Color(0.5f, 0.7f, 1f, 0.4f));
            CreateSmallLabel(labelContainer.transform, "最佳", new Color(0.5f, 0.86f, 0.55f, 0.4f));
            CreateSmallLabel(labelContainer.transform, "过热", new Color(1f, 0.5f, 0.4f, 0.4f));
        }

        private void CreateInfoBar()
        {
            var infoContainer = CreateUIObject("InfoBar", _panel.transform);
            var icRect = infoContainer.AddComponent<RectTransform>();
            AnchorCenter(icRect);
            icRect.anchoredPosition = new Vector2(0f, -30f);
            icRect.sizeDelta = new Vector2(400f, 24f);

            var hLayout = infoContainer.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 40f;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = true;
            hLayout.childForceExpandWidth = true;

            // 木材
            var woodObj = CreateUIObject("WoodInfo", infoContainer.transform);
            var woodLayout = woodObj.AddComponent<HorizontalLayoutGroup>();
            woodLayout.spacing = 6f;
            woodLayout.childAlignment = TextAnchor.MiddleCenter;
            woodLayout.childControlWidth = false;
            woodLayout.childForceExpandWidth = false;
            CreateSmallLabel(woodObj.transform, "木材", TextDimColor);
            _woodText = CreateSmallLabel(woodObj.transform, "0", TextNormalColor);

            // 最佳区间
            var optObj = CreateUIObject("OptInfo", infoContainer.transform);
            var optLayout = optObj.AddComponent<HorizontalLayoutGroup>();
            optLayout.spacing = 6f;
            optLayout.childAlignment = TextAnchor.MiddleCenter;
            optLayout.childControlWidth = false;
            optLayout.childForceExpandWidth = false;
            CreateSmallLabel(optObj.transform, "最佳区间", TextDimColor);
            _optimalPercentText = CreateSmallLabel(optObj.transform, "0%", OptimalColor);

            // 倒计时
            var timeObj = CreateUIObject("TimeInfo", infoContainer.transform);
            var timeLayout = timeObj.AddComponent<HorizontalLayoutGroup>();
            timeLayout.spacing = 6f;
            timeLayout.childAlignment = TextAnchor.MiddleCenter;
            timeLayout.childControlWidth = false;
            timeLayout.childForceExpandWidth = false;
            CreateSmallLabel(timeObj.transform, "剩余", TextDimColor);
            _timeLeftText = CreateSmallLabel(timeObj.transform, "30s", TextNormalColor);
        }

        private void CreateAddWoodButton()
        {
            var btnObj = CreateUIObject("AddWoodBtn", _panel.transform);
            var btnRect = btnObj.AddComponent<RectTransform>();
            AnchorCenter(btnRect);
            btnRect.anchoredPosition = new Vector2(0f, -80f);
            btnRect.sizeDelta = new Vector2(160f, 40f);

            var btnImage = btnObj.AddComponent<Image>();
            btnImage.color = BtnNormalBg;

            _addWoodBtn = btnObj.AddComponent<Button>();
            _addWoodBtn.onClick.AddListener(AddWood);

            _addWoodBtnText = btnObj.AddComponent<TextMeshProUGUI>();
            _addWoodBtnText.text = "添柴 (空格)";
            _addWoodBtnText.fontSize = 14;
            _addWoodBtnText.color = TextNormalColor;
            _addWoodBtnText.alignment = TextAlignmentOptions.Center;
            _addWoodBtnText.characterSpacing = 2f;
            SetFont(_addWoodBtnText);
        }

        // =================================================================
        // UI 刷新
        // =================================================================

        private void RefreshUI()
        {
            if (_tempBarFill != null)
            {
                var fillRect = _tempBarFill.rectTransform;
                fillRect.anchorMax = new Vector2(_temperature / 100f, 1f);
                _tempBarFill.color = GetTempColor(_temperature);
            }

            var gm = GameManager.Instance;
            if (_woodText != null && gm != null)
                _woodText.text = gm.Resources.Wood.ToString();

            if (_optimalPercentText != null)
            {
                float optPct = _totalTime > 0f ? _optimalTime / _totalTime : 0f;
                _optimalPercentText.text = Mathf.RoundToInt(optPct * 100f) + "%";
                _optimalPercentText.color = optPct > 0.8f ? OptimalColor
                    : optPct > 0.5f ? new Color(1f, 0.78f, 0.31f, 0.8f)
                    : new Color(1f, 0.39f, 0.31f, 0.7f);
            }

            if (_timeLeftText != null)
                _timeLeftText.text = Mathf.CeilToInt(_timeLeft) + "s";

            if (_addWoodBtn != null && gm != null)
            {
                bool disabled = _finished || _fireDead || gm.Resources.Wood < 1;
                _addWoodBtn.interactable = !disabled;
                var img = _addWoodBtn.GetComponent<Image>();
                img.color = disabled ? BtnDisabledBg : BtnNormalBg;
                if (_addWoodBtnText != null)
                    _addWoodBtnText.color = disabled ? BtnDisabledText : TextNormalColor;
            }
        }

        private void UpdateFlameVisual(float dt)
        {
            _flameAnimTime += dt;

            float flameScale = Mathf.Max(0.15f, _temperature / 70f);
            float flameOpacity = Mathf.Max(0.2f, _temperature / 80f);

            if (_fireDead) { flameScale = 0f; flameOpacity = 0f; }

            if (_outerFlame != null)
            {
                float pulse = Mathf.Sin(_flameAnimTime * 4f) * 0.08f;
                _outerFlame.rectTransform.sizeDelta = new Vector2(
                    60f * flameScale * (1f + pulse),
                    90f * flameScale * (1f - pulse * 0.5f)
                );
                var c = _outerFlame.color; c.a = 0.30f * flameOpacity; _outerFlame.color = c;
            }

            if (_midFlame != null)
            {
                float pulse = Mathf.Sin(_flameAnimTime * 5.5f) * 0.1f;
                _midFlame.rectTransform.sizeDelta = new Vector2(
                    44f * flameScale * (1f - pulse * 0.3f),
                    68f * flameScale * (1f + pulse * 0.5f)
                );
                var c = _midFlame.color; c.a = 0.60f * flameOpacity; _midFlame.color = c;
            }

            if (_innerFlame != null)
            {
                float pulse = Mathf.Sin(_flameAnimTime * 7f) * 0.12f;
                _innerFlame.rectTransform.sizeDelta = new Vector2(
                    26f * flameScale,
                    42f * flameScale * (1f + pulse)
                );
                var c = _innerFlame.color; c.a = 0.80f * flameOpacity; _innerFlame.color = c;
            }
        }

        private void ShowResult(float optPercent, float conditionBonus)
        {
            if (_resultPanel == null) return;
            _resultPanel.SetActive(true);

            if (_resultOptimalText != null)
                _resultOptimalText.text = $"最佳区间时间: {Mathf.RoundToInt(optPercent * 100f)}%";

            if (_resultBonusText != null)
                _resultBonusText.text = $"部件恢复: +{conditionBonus:F2}";

            if (_resultFlavorText != null)
            {
                _resultFlavorText.gameObject.SetActive(optPercent > 0.5f);
                _resultFlavorText.text = "篝火温暖修复了车辆部件";
            }
        }

        // =================================================================
        // 事件回调
        // =================================================================

        private void OnResourcesChanged(ResourceBag resources) { }

        // =================================================================
        // 辅助方法
        // =================================================================

        private Color GetTempColor(float t)
        {
            if (t < DangerLow) return ColdColor;
            if (t < OptimalLow) return CoolColor;
            if (t <= OptimalHigh) return OptimalColor;
            if (t < DangerHigh) return HotColor;
            return OverheatColor;
        }

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

        private TMP_Text CreateLabel(Transform parent, string text)
        {
            var obj = CreateUIObject("Label", parent);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 12;
            tmp.color = new Color(1f, 1f, 1f, 0.60f);
            tmp.alignment = TextAlignmentOptions.Center;
            SetFont(tmp);
            var layout = obj.AddComponent<LayoutElement>();
            layout.preferredHeight = 18f;
            layout.flexibleWidth = 1f;
            return tmp;
        }

        private TMP_Text CreateSmallLabel(Transform parent, string text, Color color)
        {
            var obj = CreateUIObject("SmallLabel", parent);
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 10;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            SetFont(tmp);
            var layout = obj.AddComponent<LayoutElement>();
            layout.preferredHeight = 16f;
            return tmp;
        }

        private void SetFont(TMP_Text tmp)
        {
            if (_fontAsset != null) tmp.font = _fontAsset;
        }
    }
}

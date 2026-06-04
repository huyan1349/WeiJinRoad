using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.UI
{
    // ═══════════════════════════════════════════════════════════════
    // SettingsPage — 游戏设置页面
    //
    // 程序化创建 UI，包含：
    // - 版本号 VER 0.4.0
    // - 图形设置（阴影/后处理/粒子/雪/雾/地形渲染模式）
    // - 音频设置（主音量/音乐/音效）
    // - 相机设置（跟随距离/高度/灵敏度）
    // - 开发者工具（开发模式/FPS/线框/上帝模式）
    //
    // UI 风格：深色半透明背景、白色文字、zpix 字体
    // 持久化：PlayerPrefs
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 游戏设置页面：程序化创建 UI，管理图形/音频/相机/开发者设置
    /// </summary>
    public class SettingsPage : MonoBehaviour
    {
        // =================================================================
        // 常量
        // =================================================================

        private const string VersionText = "VER 0.4.0";

        // ── PlayerPrefs 键名 ──
        private const string KeyShadowQuality   = "Settings_ShadowQuality";
        private const string KeyPostProcessing  = "Settings_PostProcessing";
        private const string KeyParticles       = "Settings_Particles";
        private const string KeySnow            = "Settings_Snow";
        private const string KeyFog             = "Settings_Fog";
        private const string KeyTerrainRender   = "Settings_TerrainRender";
        private const string KeyMasterVolume    = "Settings_MasterVolume";
        private const string KeyMusicVolume     = "Settings_MusicVolume";
        private const string KeySfxVolume       = "Settings_SfxVolume";
        private const string KeyCamDistance      = "Settings_CamDistance";
        private const string KeyCamHeight       = "Settings_CamHeight";
        private const string KeyCamSensitivity  = "Settings_CamSensitivity";
        private const string KeyDevMode         = "Settings_DevMode";
        private const string KeyFpsCounter      = "Settings_FpsCounter";
        private const string KeyWireframe       = "Settings_Wireframe";
        private const string KeyGodMode         = "Settings_GodMode";

        // ── 颜色常量 ──
        private static readonly Color BgColor       = new Color(0.08f, 0.09f, 0.12f, 0.92f);
        private static readonly Color PanelColor     = new Color(0.12f, 0.13f, 0.17f, 0.85f);
        private static readonly Color SectionColor   = new Color(0.15f, 0.16f, 0.20f, 0.90f);
        private static readonly Color TextColor      = new Color(0.92f, 0.94f, 0.97f, 1f);
        private static readonly Color AccentColor    = new Color(0.40f, 0.65f, 0.95f, 1f);
        private static readonly Color SliderBgColor  = new Color(0.20f, 0.22f, 0.28f, 1f);
        private static readonly Color SliderFillColor = new Color(0.35f, 0.55f, 0.85f, 1f);
        private static readonly Color ToggleOnColor  = new Color(0.35f, 0.65f, 0.95f, 1f);
        private static readonly Color ToggleOffColor = new Color(0.30f, 0.32f, 0.38f, 1f);
        private static readonly Color ButtonColor    = new Color(0.25f, 0.27f, 0.33f, 1f);
        private static readonly Color ButtonHoverColor = new Color(0.35f, 0.38f, 0.45f, 1f);
        private static readonly Color CloseButtonColor = new Color(0.70f, 0.25f, 0.25f, 1f);

        // ── 字体 ──
        private const string ZpixFontPath = "Fonts/zpix";

        // =================================================================
        // UI 引用
        // =================================================================

        private Canvas _canvas;
        private GameObject _panel;
        private ScrollRect _scrollRect;
        private TMP_FontAsset _fontAsset;

        // 设置值缓存
        private int _shadowQuality;     // 0=Off, 1=Low, 2=Medium, 3=High
        private bool _postProcessing;
        private bool _particles;
        private bool _snow;
        private bool _fog;
        private int _terrainRender;     // 0=Full, 1=Visible, 2=Corridor
        private float _masterVolume;
        private float _musicVolume;
        private float _sfxVolume;
        private float _camDistance;
        private float _camHeight;
        private float _camSensitivity;
        private bool _devMode;
        private bool _fpsCounter;
        private bool _wireframe;
        private bool _godMode;

        // UI 元素引用
        private TMP_Text _shadowQualityLabel;
        private Toggle _postProcessingToggle;
        private Toggle _particlesToggle;
        private Toggle _snowToggle;
        private Toggle _fogToggle;
        private TMP_Text _terrainRenderLabel;
        private Slider _masterVolumeSlider;
        private Slider _musicVolumeSlider;
        private Slider _sfxVolumeSlider;
        private Slider _camDistanceSlider;
        private Slider _camHeightSlider;
        private Slider _camSensitivitySlider;
        private Toggle _devModeToggle;
        private Toggle _fpsCounterToggle;
        private Toggle _wireframeToggle;
        private Toggle _godModeToggle;

        // =================================================================
        // 公开属性
        // =================================================================

        /// <summary>设置页面是否可见</summary>
        public bool IsVisible => _panel != null && _panel.activeSelf;

        /// <summary>版本号</summary>
        public static string Version => VersionText;

        // =================================================================
        // Unity 生命周期
        // =================================================================

        private void OnEnable()
        {
            LoadSettings();
            CreateUI();
        }

        private void OnDisable()
        {
            SaveSettings();
        }

        private void Update()
        {
            // Tab 键切换设置页面
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                ToggleVisibility();
            }
        }

        // =================================================================
        // 可见性控制
        // =================================================================

        /// <summary>
        /// 切换设置页面可见性
        /// </summary>
        public void ToggleVisibility()
        {
            if (_panel == null) return;
            _panel.SetActive(!_panel.activeSelf);

            if (_panel.activeSelf)
            {
                LoadSettings();
                RefreshUI();
            }
            else
            {
                SaveSettings();
                ApplySettings();
            }
        }

        /// <summary>
        /// 显示设置页面
        /// </summary>
        public void Show()
        {
            if (_panel == null) return;
            LoadSettings();
            RefreshUI();
            _panel.SetActive(true);
        }

        /// <summary>
        /// 隐藏设置页面
        /// </summary>
        public void Hide()
        {
            if (_panel == null) return;
            _panel.SetActive(false);
            SaveSettings();
            ApplySettings();
        }

        // =================================================================
        // UI 创建
        // =================================================================

        private void CreateUI()
        {
            // 加载字体
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
            if (_fontAsset == null)
            {
                // 尝试从 AssetDatabase 加载（Editor 模式）
#if UNITY_EDITOR
                _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                    "Assets/Fonts/zpix.asset");
#endif
            }

            // 创建 Canvas
            CreateCanvas();

            // 创建主面板
            CreatePanel();

            Debug.Log("[SettingsPage] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
            }
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 1000;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = gameObject.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private void CreatePanel()
        {
            // ── 全屏半透明遮罩 ──
            _panel = CreateUIObject("SettingsPanel", transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            var panelImage = _panel.AddComponent<Image>();
            panelImage.color = BgColor;
            panelImage.raycastTarget = true;

            // ── 内容区域 ──
            var contentArea = CreateUIObject("ContentArea", _panel.transform);
            var contentRect = contentArea.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.15f, 0.05f);
            contentRect.anchorMax = new Vector2(0.85f, 0.95f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            var contentImage = contentArea.AddComponent<Image>();
            contentImage.color = PanelColor;

            // ── ScrollRect ──
            var scrollObj = CreateUIObject("ScrollView", contentArea.transform);
            var scrollRect = scrollObj.AddComponent<RectTransform>();
            scrollRect.anchorMin = Vector2.zero;
            scrollRect.anchorMax = Vector2.one;
            scrollRect.offsetMin = new Vector4(20f, 60f, 20f, 20f);
            scrollRect.offsetMax = new Vector4(-20f, -60f, -20f, -20f);

            var scrollImage = scrollObj.AddComponent<Image>();
            scrollImage.color = Color.clear;

            _scrollRect = scrollObj.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.scrollSensitivity = 40f;

            // Viewport
            var viewportObj = CreateUIObject("Viewport", scrollObj.transform);
            var viewportRect = viewportObj.AddComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            var viewportImage = viewportObj.AddComponent<Image>();
            viewportImage.color = Color.clear;
            var viewportMask = viewportObj.AddComponent<Mask>();
            viewportMask.showMaskGraphic = false;

            // Content
            var contentObj = CreateUIObject("Content", viewportObj.transform);
            var contentInnerRect = contentObj.AddComponent<RectTransform>();
            contentInnerRect.anchorMin = new Vector2(0f, 1f);
            contentInnerRect.anchorMax = new Vector2(1f, 1f);
            contentInnerRect.pivot = new Vector2(0.5f, 1f);
            var contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(10, 10, 10, 10);
            contentLayout.spacing = 8f;
            contentLayout.childAlignment = TextAnchor.UpperCenter;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var contentSizeFitter = contentObj.AddComponent<ContentSizeFitter>();
            contentSizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _scrollRect.viewport = viewportRect;
            _scrollRect.content = contentInnerRect;

            // ── 标题栏 ──
            CreateHeader(contentArea.transform);

            // ── 设置分区 ──
            float yPos = 0f;

            // 版本号
            CreateVersionLabel(contentObj.transform);
            yPos += 40f;

            // 图形设置
            CreateGraphicsSection(contentObj.transform);
            yPos += 320f;

            // 音频设置
            CreateAudioSection(contentObj.transform);
            yPos += 200f;

            // 相机设置
            CreateCameraSection(contentObj.transform);
            yPos += 200f;

            // 开发者工具
            CreateDevToolsSection(contentObj.transform);

            // ── 底部按钮 ──
            CreateFooter(contentArea.transform);

            // 初始隐藏
            _panel.SetActive(false);
        }

        // ── 标题栏 ──

        private void CreateHeader(Transform parent)
        {
            var headerObj = CreateUIObject("Header", parent);
            var headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f);
            headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.offsetMin = new Vector4(0f, -55f, 0f, 0f);
            headerRect.offsetMax = new Vector4(0f, 0f, 0f, 0f);

            var headerLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
            headerLayout.padding = new RectOffset(20, 20, 12, 12);
            headerLayout.spacing = 10f;
            headerLayout.childAlignment = TextAnchor.MiddleCenter;
            headerLayout.childControlWidth = true;
            headerLayout.childControlHeight = false;
            headerLayout.childForceExpandWidth = true;
            headerLayout.childForceExpandHeight = false;

            // 标题文字
            var titleObj = CreateUIObject("Title", headerObj.transform);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "设 置";
            titleText.fontSize = 28;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = TextColor;
            titleText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) titleText.font = _fontAsset;

            var titleLayout = titleObj.AddComponent<LayoutElement>();
            titleLayout.flexibleWidth = 1f;
            titleLayout.preferredHeight = 40f;

            // 关闭按钮
            var closeBtnObj = CreateUIObject("CloseButton", headerObj.transform);
            var closeBtnImage = closeBtnObj.AddComponent<Image>();
            closeBtnImage.color = CloseButtonColor;
            var closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);

            var closeBtnText = closeBtnObj.AddComponent<TextMeshProUGUI>();
            closeBtnText.text = "✕";
            closeBtnText.fontSize = 22;
            closeBtnText.color = TextColor;
            closeBtnText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) closeBtnText.font = _fontAsset;

            var closeBtnLayout = closeBtnObj.AddComponent<LayoutElement>();
            closeBtnLayout.preferredWidth = 40f;
            closeBtnLayout.preferredHeight = 40f;
        }

        // ── 版本号 ──

        private void CreateVersionLabel(Transform parent)
        {
            var obj = CreateUIObject("VersionLabel", parent);
            var text = obj.AddComponent<TextMeshProUGUI>();
            text.text = VersionText;
            text.fontSize = 16;
            text.fontStyle = FontStyles.Normal;
            text.color = new Color(0.5f, 0.55f, 0.65f, 1f);
            text.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) text.font = _fontAsset;

            var layout = obj.AddComponent<LayoutElement>();
            layout.preferredHeight = 30f;
        }

        // ── 图形设置 ──

        private void CreateGraphicsSection(Transform parent)
        {
            CreateSectionHeader(parent, "图 形 设 置");

            // 阴影质量
            _shadowQualityLabel = CreateCycleOption(parent, "阴影质量",
                new string[] { "关闭", "低", "中", "高" },
                _shadowQuality,
                val => { _shadowQuality = val; });

            // 后处理
            _postProcessingToggle = CreateToggle(parent, "后处理效果", _postProcessing,
                val => { _postProcessing = val; });

            // 粒子效果
            _particlesToggle = CreateToggle(parent, "粒子效果", _particles,
                val => { _particles = val; });

            // 雪
            _snowToggle = CreateToggle(parent, "雪效果", _snow,
                val => { _snow = val; });

            // 雾
            _fogToggle = CreateToggle(parent, "雾效果", _fog,
                val => { _fog = val; });

            // 地形渲染模式
            _terrainRenderLabel = CreateCycleOption(parent, "地形渲染模式",
                new string[] { "完整", "可见区域", "走廊" },
                _terrainRender,
                val => { _terrainRender = val; });
        }

        // ── 音频设置 ──

        private void CreateAudioSection(Transform parent)
        {
            CreateSectionHeader(parent, "音 频 设 置");

            _masterVolumeSlider = CreateSlider(parent, "主音量", 0f, 100f, _masterVolume,
                val => { _masterVolume = val; });

            _musicVolumeSlider = CreateSlider(parent, "音乐音量", 0f, 100f, _musicVolume,
                val => { _musicVolume = val; });

            _sfxVolumeSlider = CreateSlider(parent, "音效音量", 0f, 100f, _sfxVolume,
                val => { _sfxVolume = val; });
        }

        // ── 相机设置 ──

        private void CreateCameraSection(Transform parent)
        {
            CreateSectionHeader(parent, "相 机 设 置");

            _camDistanceSlider = CreateSlider(parent, "跟随距离", 5f, 40f, _camDistance,
                val => { _camDistance = val; });

            _camHeightSlider = CreateSlider(parent, "跟随高度", 5f, 35f, _camHeight,
                val => { _camHeight = val; });

            _camSensitivitySlider = CreateSlider(parent, "相机灵敏度", 1f, 20f, _camSensitivity,
                val => { _camSensitivity = val; });
        }

        // ── 开发者工具 ──

        private void CreateDevToolsSection(Transform parent)
        {
            CreateSectionHeader(parent, "开 发 者 工 具");

            _devModeToggle = CreateToggle(parent, "开发模式", _devMode,
                val => { _devMode = val; });

            _fpsCounterToggle = CreateToggle(parent, "FPS 计数器", _fpsCounter,
                val => { _fpsCounter = val; });

            _wireframeToggle = CreateToggle(parent, "线框模式", _wireframe,
                val => { _wireframe = val; });

            _godModeToggle = CreateToggle(parent, "上帝模式", _godMode,
                val => { _godMode = val; });
        }

        // ── 底部按钮 ──

        private void CreateFooter(Transform parent)
        {
            var footerObj = CreateUIObject("Footer", parent);
            var footerRect = footerObj.AddComponent<RectTransform>();
            footerRect.anchorMin = new Vector2(0f, 0f);
            footerRect.anchorMax = new Vector2(1f, 0f);
            footerRect.pivot = new Vector2(0.5f, 0f);
            footerRect.offsetMin = new Vector4(20f, 10f, 20f, 0f);
            footerRect.offsetMax = new Vector4(-20f, 0f, 10f, 50f);

            var footerLayout = footerObj.AddComponent<HorizontalLayoutGroup>();
            footerLayout.spacing = 20f;
            footerLayout.childAlignment = TextAnchor.MiddleCenter;
            footerLayout.childControlWidth = true;
            footerLayout.childControlHeight = false;
            footerLayout.childForceExpandWidth = true;
            footerLayout.childForceExpandHeight = false;

            // 应用按钮
            CreateFooterButton(footerObj.transform, "应用", ApplyAndClose, AccentColor);

            // 重置按钮
            CreateFooterButton(footerObj.transform, "重置默认", ResetToDefaults, ButtonColor);

            // 取消按钮
            CreateFooterButton(footerObj.transform, "取消", Hide, ButtonColor);
        }

        // =================================================================
        // UI 辅助方法
        // =================================================================

        private GameObject CreateUIObject(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj;
        }

        private void CreateSectionHeader(Transform parent, string title)
        {
            var obj = CreateUIObject("Section_" + title.Replace(" ", ""), parent);

            var bg = obj.AddComponent<Image>();
            bg.color = SectionColor;

            var layout = obj.AddComponent<LayoutElement>();
            layout.preferredHeight = 36f;

            var hLayout = obj.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(15, 15, 6, 6);
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = false;

            var textObj = CreateUIObject("Label", obj.transform);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = title;
            text.fontSize = 18;
            text.fontStyle = FontStyles.Bold;
            text.color = AccentColor;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) text.font = _fontAsset;
        }

        private Toggle CreateToggle(Transform parent, string label, bool initialValue, System.Action<bool> onChanged)
        {
            var rowObj = CreateUIObject("ToggleRow_" + label, parent);

            var layout = rowObj.AddComponent<LayoutElement>();
            layout.preferredHeight = 36f;

            var hLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(20, 20, 4, 4);
            hLayout.spacing = 12f;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = false;

            // 标签
            var labelObj = CreateUIObject("Label", rowObj.transform);
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 16;
            labelText.color = TextColor;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) labelText.font = _fontAsset;

            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            labelLayout.preferredHeight = 28f;

            // Toggle
            var toggleObj = CreateUIObject("Toggle", rowObj.transform);
            var toggleBg = toggleObj.AddComponent<Image>();
            toggleBg.color = initialValue ? ToggleOnColor : ToggleOffColor;

            var toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = initialValue;
            toggle.targetGraphic = toggleBg;

            var toggleLayout = toggleObj.AddComponent<LayoutElement>();
            toggleLayout.preferredWidth = 50f;
            toggleLayout.preferredHeight = 28f;

            // Toggle 内的勾选标记
            var checkObj = CreateUIObject("Checkmark", toggleObj.transform);
            var checkRect = checkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0.05f, 0.1f);
            checkRect.anchorMax = new Vector2(0.95f, 0.9f);
            checkRect.offsetMin = Vector2.zero;
            checkRect.offsetMax = Vector2.zero;
            var checkImage = checkObj.AddComponent<Image>();
            checkImage.color = Color.white;
            toggle.graphic = checkImage;

            // 事件
            toggle.onValueChanged.AddListener(val =>
            {
                toggleBg.color = val ? ToggleOnColor : ToggleOffColor;
                onChanged?.Invoke(val);
            });

            return toggle;
        }

        private Slider CreateSlider(Transform parent, string label, float min, float max, float initialValue, System.Action<float> onChanged)
        {
            var rowObj = CreateUIObject("SliderRow_" + label, parent);

            var layout = rowObj.AddComponent<LayoutElement>();
            layout.preferredHeight = 40f;

            var vLayout = rowObj.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(20, 20, 2, 2);
            vLayout.spacing = 2f;
            vLayout.childAlignment = TextAnchor.UpperLeft;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            // 标签行
            var labelRow = CreateUIObject("LabelRow", rowObj.transform);
            var labelHLayout = labelRow.AddComponent<HorizontalLayoutGroup>();
            labelHLayout.childControlWidth = true;
            labelHLayout.childControlHeight = false;
            labelHLayout.childForceExpandWidth = true;
            labelHLayout.childForceExpandHeight = false;

            var labelObj = CreateUIObject("Label", labelRow.transform);
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 14;
            labelText.color = TextColor;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) labelText.font = _fontAsset;

            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            labelLayout.preferredHeight = 20f;

            var valueObj = CreateUIObject("Value", labelRow.transform);
            var valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.fontSize = 14;
            valueText.color = AccentColor;
            valueText.alignment = TextAlignmentOptions.MidlineRight;
            if (_fontAsset != null) valueText.font = _fontAsset;
            valueText.text = initialValue.ToString("F0");

            var valueLayout = valueObj.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 60f;
            valueLayout.preferredHeight = 20f;

            // 滑动条
            var sliderObj = CreateUIObject("Slider", rowObj.transform);
            var sliderBg = sliderObj.AddComponent<Image>();
            sliderBg.color = SliderBgColor;

            var slider = sliderObj.AddComponent<Slider>();
            slider.minValue = min;
            slider.maxValue = max;
            slider.value = initialValue;
            slider.wholeNumbers = (min >= 0f && max <= 100f && label.Contains("音量"));
            slider.targetGraphic = sliderBg;

            var sliderLayout = sliderObj.AddComponent<LayoutElement>();
            sliderLayout.preferredHeight = 16f;

            // 填充区域
            var fillAreaObj = CreateUIObject("FillArea", sliderObj.transform);
            var fillAreaRect = fillAreaObj.AddComponent<RectTransform>();
            fillAreaRect.anchorMin = Vector2.zero;
            fillAreaRect.anchorMax = Vector2.one;
            fillAreaRect.offsetMin = Vector2.zero;
            fillAreaRect.offsetMax = Vector2.zero;

            var fillObj = CreateUIObject("Fill", fillAreaObj.transform);
            var fillRect = fillObj.AddComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            var fillImage = fillObj.AddComponent<Image>();
            fillImage.color = SliderFillColor;

            slider.fillRect = fillRect;

            // 手柄
            var handleObj = CreateUIObject("Handle", sliderObj.transform);
            var handleRect = handleObj.AddComponent<RectTransform>();
            handleRect.anchorMin = new Vector2(0.5f, 0f);
            handleRect.anchorMax = new Vector2(0.5f, 1f);
            handleRect.sizeDelta = new Vector2(12f, 0f);
            var handleImage = handleObj.AddComponent<Image>();
            handleImage.color = Color.white;

            slider.handleRect = handleRect;

            // 事件
            slider.onValueChanged.AddListener(val =>
            {
                valueText.text = val.ToString("F0");
                onChanged?.Invoke(val);
            });

            return slider;
        }

        private TMP_Text CreateCycleOption(Transform parent, string label, string[] options, int currentIndex, System.Action<int> onChanged)
        {
            var rowObj = CreateUIObject("CycleRow_" + label, parent);

            var layout = rowObj.AddComponent<LayoutElement>();
            layout.preferredHeight = 36f;

            var hLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(20, 20, 4, 4);
            hLayout.spacing = 12f;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = false;

            // 标签
            var labelObj = CreateUIObject("Label", rowObj.transform);
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label;
            labelText.fontSize = 16;
            labelText.color = TextColor;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) labelText.font = _fontAsset;

            var labelLayout = labelObj.AddComponent<LayoutElement>();
            labelLayout.flexibleWidth = 1f;
            labelLayout.preferredHeight = 28f;

            // 左箭头
            var leftBtnObj = CreateUIObject("LeftBtn", rowObj.transform);
            var leftBtnImage = leftBtnObj.AddComponent<Image>();
            leftBtnImage.color = ButtonColor;
            var leftBtn = leftBtnObj.AddComponent<Button>();
            var leftBtnLayout = leftBtnObj.AddComponent<LayoutElement>();
            leftBtnLayout.preferredWidth = 32f;
            leftBtnLayout.preferredHeight = 28f;
            var leftBtnText = leftBtnObj.AddComponent<TextMeshProUGUI>();
            leftBtnText.text = "◀";
            leftBtnText.fontSize = 14;
            leftBtnText.color = TextColor;
            leftBtnText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) leftBtnText.font = _fontAsset;

            // 当前值
            var valueObj = CreateUIObject("Value", rowObj.transform);
            var valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.text = options[Mathf.Clamp(currentIndex, 0, options.Length - 1)];
            valueText.fontSize = 16;
            valueText.color = AccentColor;
            valueText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) valueText.font = _fontAsset;

            var valueLayout = valueObj.AddComponent<LayoutElement>();
            valueLayout.preferredWidth = 80f;
            valueLayout.preferredHeight = 28f;

            // 右箭头
            var rightBtnObj = CreateUIObject("RightBtn", rowObj.transform);
            var rightBtnImage = rightBtnObj.AddComponent<Image>();
            rightBtnImage.color = ButtonColor;
            var rightBtn = rightBtnObj.AddComponent<Button>();
            var rightBtnLayout = rightBtnObj.AddComponent<LayoutElement>();
            rightBtnLayout.preferredWidth = 32f;
            rightBtnLayout.preferredHeight = 28f;
            var rightBtnText = rightBtnObj.AddComponent<TextMeshProUGUI>();
            rightBtnText.text = "▶";
            rightBtnText.fontSize = 14;
            rightBtnText.color = TextColor;
            rightBtnText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) rightBtnText.font = _fontAsset;

            // 闭包变量
            int idx = currentIndex;

            leftBtn.onClick.AddListener(() =>
            {
                idx = (idx - 1 + options.Length) % options.Length;
                valueText.text = options[idx];
                onChanged?.Invoke(idx);
            });

            rightBtn.onClick.AddListener(() =>
            {
                idx = (idx + 1) % options.Length;
                valueText.text = options[idx];
                onChanged?.Invoke(idx);
            });

            return valueText;
        }

        private void CreateFooterButton(Transform parent, string label, System.Action onClick, Color bgColor)
        {
            var btnObj = CreateUIObject("Btn_" + label, parent);
            var btnImage = btnObj.AddComponent<Image>();
            btnImage.color = bgColor;

            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());

            // 悬停效果
            var colors = btn.colors;
            colors.highlightedColor = ButtonHoverColor;
            btn.colors = colors;

            var btnLayout = btnObj.AddComponent<LayoutElement>();
            btnLayout.preferredHeight = 36f;
            btnLayout.flexibleWidth = 1f;

            var btnText = btnObj.AddComponent<TextMeshProUGUI>();
            btnText.text = label;
            btnText.fontSize = 16;
            btnText.color = TextColor;
            btnText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) btnText.font = _fontAsset;
        }

        // =================================================================
        // 设置持久化
        // =================================================================

        private void LoadSettings()
        {
            _shadowQuality = PlayerPrefs.GetInt(KeyShadowQuality, 3);
            _postProcessing = PlayerPrefs.GetInt(KeyPostProcessing, 1) == 1;
            _particles = PlayerPrefs.GetInt(KeyParticles, 1) == 1;
            _snow = PlayerPrefs.GetInt(KeySnow, 1) == 1;
            _fog = PlayerPrefs.GetInt(KeyFog, 1) == 1;
            _terrainRender = PlayerPrefs.GetInt(KeyTerrainRender, 0);
            _masterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, 80f);
            _musicVolume = PlayerPrefs.GetFloat(KeyMusicVolume, 70f);
            _sfxVolume = PlayerPrefs.GetFloat(KeySfxVolume, 80f);
            _camDistance = PlayerPrefs.GetFloat(KeyCamDistance, 14f);
            _camHeight = PlayerPrefs.GetFloat(KeyCamHeight, 18f);
            _camSensitivity = PlayerPrefs.GetFloat(KeyCamSensitivity, 8f);
            _devMode = PlayerPrefs.GetInt(KeyDevMode, 0) == 1;
            _fpsCounter = PlayerPrefs.GetInt(KeyFpsCounter, 0) == 1;
            _wireframe = PlayerPrefs.GetInt(KeyWireframe, 0) == 1;
            _godMode = PlayerPrefs.GetInt(KeyGodMode, 0) == 1;
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt(KeyShadowQuality, _shadowQuality);
            PlayerPrefs.SetInt(KeyPostProcessing, _postProcessing ? 1 : 0);
            PlayerPrefs.SetInt(KeyParticles, _particles ? 1 : 0);
            PlayerPrefs.SetInt(KeySnow, _snow ? 1 : 0);
            PlayerPrefs.SetInt(KeyFog, _fog ? 1 : 0);
            PlayerPrefs.SetInt(KeyTerrainRender, _terrainRender);
            PlayerPrefs.SetFloat(KeyMasterVolume, _masterVolume);
            PlayerPrefs.SetFloat(KeyMusicVolume, _musicVolume);
            PlayerPrefs.SetFloat(KeySfxVolume, _sfxVolume);
            PlayerPrefs.SetFloat(KeyCamDistance, _camDistance);
            PlayerPrefs.SetFloat(KeyCamHeight, _camHeight);
            PlayerPrefs.SetFloat(KeyCamSensitivity, _camSensitivity);
            PlayerPrefs.SetInt(KeyDevMode, _devMode ? 1 : 0);
            PlayerPrefs.SetInt(KeyFpsCounter, _fpsCounter ? 1 : 0);
            PlayerPrefs.SetInt(KeyWireframe, _wireframe ? 1 : 0);
            PlayerPrefs.SetInt(KeyGodMode, _godMode ? 1 : 0);
            PlayerPrefs.Save();
        }

        // =================================================================
        // 应用设置到 GameManager
        // =================================================================

        private void ApplySettings()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // 图形
            gm.DevShadows = _shadowQuality > 0;
            gm.DevPostProcessing = _postProcessing;
            gm.DevParticles = _particles;
            gm.DevSnow = _snow;
            gm.DevFog = _fog;

            // 地形渲染模式
            gm.TerrainRenderModeType = _terrainRender switch
            {
                0 => TerrainRenderMode.Full,
                1 => TerrainRenderMode.Visible,
                2 => TerrainRenderMode.Corridor,
                _ => TerrainRenderMode.Full
            };

            // 相机
            gm.CameraDistance = _camDistance;
            gm.CameraHeight = _camHeight;

            // 开发者
            gm.DevPanelVisible = _devMode;
            gm.DevGodView = _godMode;

            // 应用阴影质量
            ApplyShadowQuality(_shadowQuality);

            // 应用雾效果
            RenderSettings.fog = _fog;

            // 应用线框模式
            ApplyWireframe(_wireframe);

            // 应用音量
            AudioListener.volume = _masterVolume / 100f;

            // 应用相机灵敏度
            var followCam = FindFirstObjectByType<FollowCamera>();
            if (followCam != null)
            {
                followCam.SetDistance(_camDistance);
                followCam.SetHeight(_camHeight);
                followCam.SetSensitivity(_camSensitivity);
            }

            Debug.Log("[SettingsPage] 设置已应用");
        }

        private void ApplyShadowQuality(int quality)
        {
            switch (quality)
            {
                case 0:
                    QualitySettings.shadows = ShadowQuality.Disable;
                    break;
                case 1:
                    QualitySettings.shadows = ShadowQuality.HardOnly;
                    QualitySettings.shadowResolution = ShadowResolution.Low;
                    break;
                case 2:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.Medium;
                    break;
                case 3:
                    QualitySettings.shadows = ShadowQuality.All;
                    QualitySettings.shadowResolution = ShadowResolution.High;
                    break;
            }
        }

        private void ApplyWireframe(bool enabled)
        {
            // 线框模式通过修改相机渲染方式实现
            var cam = Camera.main;
            if (cam != null && enabled)
            {
                // 在实际项目中需要自定义渲染管线支持
                Debug.Log("[SettingsPage] 线框模式: " + (enabled ? "开启" : "关闭"));
            }
        }

        // =================================================================
        // UI 刷新
        // =================================================================

        private void RefreshUI()
        {
            if (_postProcessingToggle != null) _postProcessingToggle.isOn = _postProcessing;
            if (_particlesToggle != null) _particlesToggle.isOn = _particles;
            if (_snowToggle != null) _snowToggle.isOn = _snow;
            if (_fogToggle != null) _fogToggle.isOn = _fog;
            if (_masterVolumeSlider != null) _masterVolumeSlider.value = _masterVolume;
            if (_musicVolumeSlider != null) _musicVolumeSlider.value = _musicVolume;
            if (_sfxVolumeSlider != null) _sfxVolumeSlider.value = _sfxVolume;
            if (_camDistanceSlider != null) _camDistanceSlider.value = _camDistance;
            if (_camHeightSlider != null) _camHeightSlider.value = _camHeight;
            if (_camSensitivitySlider != null) _camSensitivitySlider.value = _camSensitivity;
            if (_devModeToggle != null) _devModeToggle.isOn = _devMode;
            if (_fpsCounterToggle != null) _fpsCounterToggle.isOn = _fpsCounter;
            if (_wireframeToggle != null) _wireframeToggle.isOn = _wireframe;
            if (_godModeToggle != null) _godModeToggle.isOn = _godMode;
        }

        // =================================================================
        // 操作方法
        // =================================================================

        private void ApplyAndClose()
        {
            SaveSettings();
            ApplySettings();
            Hide();
        }

        private void ResetToDefaults()
        {
            _shadowQuality = 3;
            _postProcessing = true;
            _particles = true;
            _snow = true;
            _fog = true;
            _terrainRender = 0;
            _masterVolume = 80f;
            _musicVolume = 70f;
            _sfxVolume = 80f;
            _camDistance = 14f;
            _camHeight = 18f;
            _camSensitivity = 8f;
            _devMode = false;
            _fpsCounter = false;
            _wireframe = false;
            _godMode = false;

            RefreshUI();
            Debug.Log("[SettingsPage] 已重置为默认值");
        }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-c9955ba222a744019a313ef95b4dcd1b/cwd.txt'; exit "$__tr_native_ec"
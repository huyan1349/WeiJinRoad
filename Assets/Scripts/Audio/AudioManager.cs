using System.Collections.Generic;
using UnityEngine;
using WeiJinRoad.Core;

namespace WeiJinRoad.Audio
{
    // =====================================================================
    // 音频类型枚举
    // =====================================================================

    /// <summary>
    /// 音效种类，用于从 AudioClip 池中查找对应片段
    /// </summary>
    public enum SoundKind
    {
        /// <summary>引擎低频隆隆声（循环）</summary>
        EngineRumble,
        /// <summary>引擎噪声层（循环）</summary>
        EngineNoise,
        /// <summary>风声（循环）</summary>
        Wind,
        /// <summary>探照灯开启</summary>
        SearchlightOn,
        /// <summary>探照灯关闭</summary>
        SearchlightOff,
        /// <summary>全照明</summary>
        FullIllumination,
        /// <summary>碎片发现</summary>
        FragmentDiscover,
        /// <summary>交互点击</summary>
        InteractClick,
        /// <summary>刹车</summary>
        Brake,
        /// <summary>树木撞击</summary>
        TreeImpact,
        /// <summary>章节过渡</summary>
        ChapterTransition,
        /// <summary>低频嗡鸣</summary>
        LowHum,
        /// <summary>心跳</summary>
        Heartbeat,
        /// <summary>菜单开始</summary>
        MenuStart,
        /// <summary>UI悬停</summary>
        UIHover,
        /// <summary>UIClick</summary>
        UIClick,
        /// <summary>背景音乐</summary>
        BGM
    }

    // =====================================================================
    // AudioManager — 音频管理器单例
    // =====================================================================

    /// <summary>
    /// 音频管理器：将 WebAudio 程序化音频引擎近似翻译为 Unity AudioSource 系统。
    ///
    /// 原版 TypeScript 使用 OscillatorNode / BiquadFilterNode / 噪声缓冲区
    /// 在浏览器端实时合成声音。Unity 中没有等价的程序化合成管线，
    /// 因此采用 AudioSource + AudioClip 的方式近似实现：
    /// - 引擎声：循环播放低频片段，通过 pitch/volume 曲线模拟转速变化
    /// - 风声：循环播放噪声片段，通过 volume 模拟风速
    /// - 碰撞/交互：一次性播放 (PlayOneShot)
    /// - 心跳：定时播放脉冲音
    /// - 背景音乐：播放 The_Long_Way_Up.mp3
    ///
    /// 所有音量受 MasterVolume 控制，音效/BGM 可分别调节。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        // =================================================================
        // 单例
        // =================================================================

        private static AudioManager _instance;

        /// <summary>
        /// 全局单例实例
        /// </summary>
        public static AudioManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<AudioManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[AudioManager]");
                        _instance = go.AddComponent<AudioManager>();
                    }
                }
                return _instance;
            }
        }

        // =================================================================
        // Inspector 配置 — AudioClip 引用
        // =================================================================

        [Header("引擎音效")]
        [Tooltip("引擎低频隆隆声循环片段")]
        public AudioClip EngineRumbleClip;
        [Tooltip("引擎噪声层循环片段")]
        public AudioClip EngineNoiseClip;

        [Header("环境音效")]
        [Tooltip("风声循环片段")]
        public AudioClip WindClip;

        [Header("交互音效")]
        [Tooltip("探照灯开启音效")]
        public AudioClip SearchlightOnClip;
        [Tooltip("探照灯关闭音效")]
        public AudioClip SearchlightOffClip;
        [Tooltip("全照明音效")]
        public AudioClip FullIlluminationClip;
        [Tooltip("碎片发现音效")]
        public AudioClip FragmentDiscoverClip;
        [Tooltip("交互点击音效")]
        public AudioClip InteractClickClip;
        [Tooltip("刹车音效")]
        public AudioClip BrakeClip;
        [Tooltip("树木撞击音效")]
        public AudioClip TreeImpactClip;

        [Header("叙事音效")]
        [Tooltip("章节过渡音效")]
        public AudioClip ChapterTransitionClip;
        [Tooltip("低频嗡鸣音效")]
        public AudioClip LowHumClip;

        [Header("心跳")]
        [Tooltip("心跳脉冲音效")]
        public AudioClip HeartbeatClip;

        [Header("菜单音效")]
        [Tooltip("菜单开始音效")]
        public AudioClip MenuStartClip;
        [Tooltip("UI悬停音效")]
        public AudioClip UIHoverClip;
        [Tooltip("UI点击音效")]
        public AudioClip UIClickClip;

        [Header("背景音乐")]
        [Tooltip("背景音乐片段 (The_Long_Way_Up.mp3)")]
        public AudioClip BGMClip;

        [Header("音量控制")]
        [Range(0f, 1f)]
        [Tooltip("主音量")]
        public float MasterVolume = 0.8f;
        [Range(0f, 1f)]
        [Tooltip("音效音量")]
        public float SFXVolume = 1f;
        [Range(0f, 1f)]
        [Tooltip("背景音乐音量")]
        public float BGMVolume = 0.6f;

        [Header("引擎参数")]
        [Tooltip("引擎空闲时 pitch")]
        public float EngineIdlePitch = 0.4f;
        [Tooltip("引擎最大速度时 pitch")]
        public float EngineMaxPitch = 1.6f;
        [Tooltip("引擎空闲时音量")]
        public float EngineIdleVolume = 0.08f;
        [Tooltip("引擎最大速度时音量")]
        public float EngineMaxVolume = 0.25f;
        [Tooltip("引擎噪声空闲音量")]
        public float EngineNoiseIdleVolume = 0.03f;
        [Tooltip("引擎噪声最大速度音量")]
        public float EngineNoiseMaxVolume = 0.12f;
        [Tooltip("引擎参数插值速度")]
        public float EngineLerpSpeed = 3f;

        [Header("风声参数")]
        [Tooltip("风声最小音量")]
        public float WindMinVolume = 0.02f;
        [Tooltip("风声最大音量")]
        public float WindMaxVolume = 0.15f;
        [Tooltip("风声参数插值速度")]
        public float WindLerpSpeed = 1f;

        [Header("心跳参数")]
        [Tooltip("心跳最低BPM")]
        public float HeartbeatMinBPM = 40f;
        [Tooltip("心跳最高BPM")]
        public float HeartbeatMaxBPM = 120f;
        [Tooltip("触发心跳的生命值阈值 (0~1)")]
        public float HeartbeatHealthThreshold = 0.35f;

        // =================================================================
        // 运行时状态
        // =================================================================

        // 引擎 AudioSource
        private AudioSource _engineRumbleSource;
        private AudioSource _engineNoiseSource;
        private bool _engineActive;

        // 风 AudioSource
        private AudioSource _windSource;
        private bool _windActive;

        // 心跳
        private float _heartbeatInterval;
        private float _heartbeatTimer;
        private bool _heartbeatActive;

        // 背景音乐
        private AudioSource _bgmSource;
        private bool _bgmPlaying;

        // 音效池
        private readonly List<AudioSource> _sfxPool = new List<AudioSource>();
        private const int SfxPoolSize = 12;

        // 章节风强度（0~1，由 WindAudio 逻辑控制）
        private float _windIntensityLevel;

        // 引擎参数目标（平滑插值用）
        private float _engineTargetPitch;
        private float _engineTargetRumbleVol;
        private float _engineTargetNoiseVol;
        private float _windTargetVol;

        // 车辆引用
        private Vehicle.VehicleController _vehicle;

        // =================================================================
        // Unity 生命周期
        // =================================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            InitializeSfxPool();
        }

        private void OnEnable()
        {
            SubscribeEvents();
        }

        private void OnDisable()
        {
            UnsubscribeEvents();
        }

        private void Update()
        {
            UpdateWindByChapter();
            UpdateEngine();
            UpdateWind();
            UpdateHeartbeat();
            UpdateBGM();
        }

        // =================================================================
        // 初始化
        // =================================================================

        /// <summary>
        /// 初始化循环播放的 AudioSource（引擎、风、BGM）
        /// </summary>
        private void InitializeAudioSources()
        {
            // 引擎低频
            _engineRumbleSource = CreateAudioSource("EngineRumble", false, true);
            _engineRumbleSource.clip = EngineRumbleClip;
            _engineRumbleSource.pitch = EngineIdlePitch;
            _engineRumbleSource.volume = 0f;

            // 引擎噪声
            _engineNoiseSource = CreateAudioSource("EngineNoise", false, true);
            _engineNoiseSource.clip = EngineNoiseClip;
            _engineNoiseSource.pitch = EngineIdlePitch;
            _engineNoiseSource.volume = 0f;

            // 风
            _windSource = CreateAudioSource("Wind", false, true);
            _windSource.clip = WindClip;
            _windSource.volume = 0f;

            // BGM
            _bgmSource = CreateAudioSource("BGM", true, true);
            _bgmSource.clip = BGMClip;
            _bgmSource.volume = 0f;
            _bgmSource.loop = true;
        }

        /// <summary>
        /// 初始化音效对象池
        /// </summary>
        private void InitializeSfxPool()
        {
            for (int i = 0; i < SfxPoolSize; i++)
            {
                var src = CreateAudioSource($"SFX_{i}", false, false);
                _sfxPool.Add(src);
            }
        }

        /// <summary>
        /// 创建一个挂载在当前 GameObject 上的 AudioSource
        /// </summary>
        /// <param name="name">调试用名称</param>
        /// <param name="is2D">是否为2D音源（BGM等）</param>
        /// <param name="loop">是否循环</param>
        /// <returns>新建的 AudioSource</returns>
        private AudioSource CreateAudioSource(string name, bool is2D, bool loop)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = loop;
            if (is2D)
            {
                src.spatialBlend = 0f; // 2D
            }
            else
            {
                src.spatialBlend = 0.3f; // 略带空间感
            }
            return src;
        }

        // =================================================================
        // 事件订阅
        // =================================================================

        /// <summary>
        /// 订阅游戏事件，触发对应音效
        /// </summary>
        private void SubscribeEvents()
        {
            GameEvents.OnFragmentDiscovered += OnFragmentDiscovered;
            GameEvents.OnGamePhaseChanged += OnGamePhaseChanged;
            GameEvents.OnObstacleCleared += OnObstacleCleared;
            GameEvents.OnHealthChanged += OnHealthChanged;
        }

        /// <summary>
        /// 取消订阅游戏事件
        /// </summary>
        private void UnsubscribeEvents()
        {
            GameEvents.OnFragmentDiscovered -= OnFragmentDiscovered;
            GameEvents.OnGamePhaseChanged -= OnGamePhaseChanged;
            GameEvents.OnObstacleCleared -= OnObstacleCleared;
            GameEvents.OnHealthChanged -= OnHealthChanged;
        }

        // =================================================================
        // 引擎音效
        // =================================================================

        /// <summary>
        /// 启动引擎声（对应 TS: startEngine）
        ///
        /// 原版使用双振荡器（锯齿波35Hz + 正弦波70Hz）+ 低通滤波器 + 噪声层。
        /// Unity 版使用循环 AudioClip，通过 pitch 和 volume 曲线模拟转速变化。
        /// </summary>
        public void StartEngine()
        {
            if (_engineActive) return;

            _vehicle = FindFirstObjectByType<Vehicle.VehicleController>();

            if (EngineRumbleClip != null && _engineRumbleSource != null)
            {
                _engineRumbleSource.volume = 0f;
                _engineRumbleSource.Play();
            }

            if (EngineNoiseClip != null && _engineNoiseSource != null)
            {
                _engineNoiseSource.volume = 0f;
                _engineNoiseSource.Play();
            }

            _engineActive = true;
        }

        /// <summary>
        /// 更新引擎音效参数（对应 TS: updateEngine）
        ///
        /// 原版根据速度比动态调整振荡器频率、滤波器截止频率和增益。
        /// Unity 版通过 pitch 和 volume 的平滑插值近似实现。
        /// </summary>
        private void UpdateEngine()
        {
            if (!_engineActive) return;

            float speed = _vehicle != null ? Mathf.Abs(_vehicle.Velocity) : 0f;
            float maxSpeed = Vehicle.VehicleController.MaxSpeed;
            float ratio = Mathf.Clamp01(speed / maxSpeed);

            // 目标 pitch：空闲0.4 → 最大1.6（模拟原版 35Hz→75Hz 的频率变化）
            _engineTargetPitch = Mathf.Lerp(EngineIdlePitch, EngineMaxPitch, ratio);

            // 目标音量：空闲低音量 → 最大高音量（模拟原版 gain1: 0.015→0.04）
            _engineTargetRumbleVol = Mathf.Lerp(EngineIdleVolume, EngineMaxVolume, ratio);

            // 噪声层音量：空闲0.006 → 最大0.021（模拟原版 noiseGain: 0.006→0.021）
            _engineTargetNoiseVol = Mathf.Lerp(EngineNoiseIdleVolume, EngineNoiseMaxVolume, ratio);

            // 平滑插值
            float lerpT = 1f - Mathf.Exp(-EngineLerpSpeed * Time.deltaTime);

            if (_engineRumbleSource != null)
            {
                _engineRumbleSource.pitch = Mathf.Lerp(_engineRumbleSource.pitch, _engineTargetPitch, lerpT);
                _engineRumbleSource.volume = Mathf.Lerp(_engineRumbleSource.volume,
                    _engineTargetRumbleVol * SFXVolume * MasterVolume, lerpT);
            }

            if (_engineNoiseSource != null)
            {
                _engineNoiseSource.pitch = Mathf.Lerp(_engineNoiseSource.pitch, _engineTargetPitch * 1.2f, lerpT);
                _engineNoiseSource.volume = Mathf.Lerp(_engineNoiseSource.volume,
                    _engineTargetNoiseVol * SFXVolume * MasterVolume, lerpT);
            }
        }

        /// <summary>
        /// 停止引擎声（对应 TS: stopEngine）
        ///
        /// 原版使用1.5秒渐隐。Unity 版同样渐隐后停止。
        /// </summary>
        /// <param name="fadeDuration">渐隐时长（秒）</param>
        public void StopEngine(float fadeDuration = 1.5f)
        {
            if (!_engineActive) return;

            StartCoroutine(FadeOutAndStop(_engineRumbleSource, fadeDuration));
            StartCoroutine(FadeOutAndStop(_engineNoiseSource, fadeDuration));
            _engineActive = false;
        }

        /// <summary>
        /// 引擎加速音效（对应 Shift 加速时的额外音调提升）
        /// </summary>
        /// <param name="boosting">是否正在加速</param>
        public void SetEngineBoost(bool boosting)
        {
            if (!_engineActive) return;

            // 加速时额外提升 pitch 0.3
            float boostOffset = boosting ? 0.3f : 0f;
            if (_engineRumbleSource != null)
            {
                _engineTargetPitch += boostOffset;
            }
        }

        // =================================================================
        // 风声音效
        // =================================================================

        /// <summary>
        /// 根据当前章节更新风力强度（对应 React WindAudio 组件）
        /// Chapter 3+: windLevel = 0.8
        /// Chapter 2: windLevel = 0.5
        /// Chapter 1: windLevel = 0.25
        /// </summary>
        private void UpdateWindByChapter()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            string chapter = gm.CurrentChapter ?? "ch1";
            int chapterNumber = 1;
            var digits = new System.Text.StringBuilder();
            foreach (char c in chapter)
            {
                if (char.IsDigit(c)) digits.Append(c);
            }
            if (digits.Length > 0 && !int.TryParse(digits.ToString(), out chapterNumber))
                chapterNumber = 1;

            float windLevel = chapterNumber >= 3 ? 0.8f : chapterNumber >= 2 ? 0.5f : 0.25f;
            SetWindIntensity(windLevel);
        }

        /// <summary>
        /// 设置风力强度（对应 TS: setWindIntensity）
        /// 将风力强度映射到风声目标音量范围
        /// </summary>
        /// <param name="intensity">风力强度 0~1</param>
        public void SetWindIntensity(float intensity)
        {
            _windIntensityLevel = Mathf.Clamp01(intensity);
        }

        /// <summary>
        /// 启动风声（对应 TS: startWind）
        ///
        /// 原版使用噪声缓冲区 + 带通滤波器 + LFO 调制。
        /// Unity 版使用循环风声片段，通过音量曲线模拟风速。
        /// </summary>
        public void StartWind()
        {
            if (_windActive) return;

            if (WindClip != null && _windSource != null)
            {
                _windSource.volume = 0f;
                _windSource.Play();
            }

            _windActive = true;
        }

        /// <summary>
        /// 更新风声参数（对应 TS: setWindIntensity）
        ///
        /// 原版根据强度调整增益、滤波器频率和LFO参数。
        /// Unity 版通过音量平滑变化近似实现。
        /// </summary>
        private void UpdateWind()
        {
            if (!_windActive) return;

            float speed = _vehicle != null ? Mathf.Abs(_vehicle.Velocity) : 0f;
            float maxSpeed = Vehicle.VehicleController.MaxSpeed;
            float ratio = Mathf.Clamp01(speed / maxSpeed);

            // 目标音量：低速0.02 → 高速0.15，叠加章节风力强度
            float speedVol = Mathf.Lerp(WindMinVolume, WindMaxVolume, ratio);
            _windTargetVol = speedVol * (0.3f + 0.7f * _windIntensityLevel);

            if (_windSource != null)
            {
                float lerpT = 1f - Mathf.Exp(-WindLerpSpeed * Time.deltaTime);
                _windSource.volume = Mathf.Lerp(_windSource.volume,
                    _windTargetVol * SFXVolume * MasterVolume, lerpT);

                // 微调 pitch 模拟原版 LFO 效果，风力越强 pitch 越高
                _windSource.pitch = Mathf.Lerp(0.9f, 1.15f, ratio) + _windIntensityLevel * 0.1f;
            }
        }

        /// <summary>
        /// 停止风声（对应 TS: stopWind）
        ///
        /// 原版使用2秒渐隐。Unity 版同样渐隐后停止。
        /// </summary>
        /// <param name="fadeDuration">渐隐时长（秒）</param>
        public void StopWind(float fadeDuration = 2f)
        {
            if (!_windActive) return;

            StartCoroutine(FadeOutAndStop(_windSource, fadeDuration));
            _windActive = false;
        }

        // =================================================================
        // 碰撞音效
        // =================================================================

        /// <summary>
        /// 播放树木撞击音效（对应 TS: treeImpact）
        ///
        /// 原版使用三角波振荡器（低频冲击）+ 噪声缓冲区（碎片声）。
        /// power 参数控制音量和频率偏移。
        /// </summary>
        /// <param name="power">碰撞力度 0~1</param>
        public void PlayTreeImpact(float power = 1f)
        {
            float p = Mathf.Clamp01(Mathf.Max(0.25f, power));
            float vol = p * 0.4f * SFXVolume * MasterVolume;
            PlayOneShot(TreeImpactClip, vol, 0.8f + (1f - p) * 0.4f);
        }

        /// <summary>
        /// 播放刹车音效（对应 TS: brake）
        ///
        /// 原版使用噪声缓冲区 + 带通滤波器（1500Hz）。
        /// </summary>
        public void PlayBrake()
        {
            PlayOneShot(BrakeClip, 0.15f * SFXVolume * MasterVolume);
        }

        // =================================================================
        // 探照灯音效
        // =================================================================

        /// <summary>
        /// 播放探照灯开启音效（对应 TS: searchlightOn）
        ///
        /// 原版使用正弦波从300Hz指数滑到600Hz，0.3秒衰减。
        /// </summary>
        public void PlaySearchlightOn()
        {
            PlayOneShot(SearchlightOnClip, 0.2f * SFXVolume * MasterVolume, 1.2f);
        }

        /// <summary>
        /// 播放探照灯关闭音效（对应 TS: searchlightOff）
        ///
        /// 原版使用正弦波从500Hz指数滑到200Hz，0.25秒衰减。
        /// </summary>
        public void PlaySearchlightOff()
        {
            PlayOneShot(SearchlightOffClip, 0.15f * SFXVolume * MasterVolume, 0.8f);
        }

        // =================================================================
        // 全照明音效
        // =================================================================

        /// <summary>
        /// 播放全照明音效（对应 TS: fullIllumination）
        ///
        /// 原版使用正弦波从200Hz指数滑到1200Hz + 高通噪声层。
        /// 双击Space触发全照明时调用。
        /// </summary>
        public void PlayFullIllumination()
        {
            PlayOneShot(FullIlluminationClip, 0.3f * SFXVolume * MasterVolume, 1.0f);
        }

        // =================================================================
        // 碎片发现音效
        // =================================================================

        /// <summary>
        /// 播放碎片发现音效（对应 TS: fragmentDiscover）
        ///
        /// 原版使用双三角波振荡器（523→784Hz + 1047Hz延迟叠加），
        /// 模拟清脆的发现提示音。
        /// </summary>
        public void PlayFragmentDiscover()
        {
            PlayOneShot(FragmentDiscoverClip, 0.3f * SFXVolume * MasterVolume, 1.0f);
        }

        // =================================================================
        // 章节过渡音效
        // =================================================================

        /// <summary>
        /// 播放章节过渡音效（对应 TS: chapterTransition）
        ///
        /// 原版使用正弦波从80Hz指数滑到200Hz + 低通滤波器，1秒渐隐。
        /// </summary>
        public void PlayChapterTransition()
        {
            PlayOneShot(ChapterTransitionClip, 0.3f * SFXVolume * MasterVolume, 0.9f);
        }

        // =================================================================
        // 低频嗡鸣
        // =================================================================

        /// <summary>
        /// 播放低频嗡鸣音效（对应 TS: lowHum）
        ///
        /// 原版使用正弦波从60Hz指数滑到2000Hz，3秒渐隐。
        /// </summary>
        public void PlayLowHum()
        {
            PlayOneShot(LowHumClip, 0.1f * SFXVolume * MasterVolume, 1.0f);
        }

        // =================================================================
        // 心跳音效
        // =================================================================

        /// <summary>
        /// 启动心跳音效（对应 TS: heartbeat）
        ///
        /// 原版使用定时器按BPM触发55Hz正弦波脉冲。
        /// Unity 版按间隔播放心跳片段。
        /// </summary>
        /// <param name="bpm">每分钟心跳次数，0或负值则停止</param>
        public void StartHeartbeat(float bpm)
        {
            if (bpm <= 0f)
            {
                StopHeartbeat();
                return;
            }

            _heartbeatInterval = 60f / bpm;
            _heartbeatTimer = 0f;
            _heartbeatActive = true;

            // 立即播放一次
            PlayHeartbeatPulse();
        }

        /// <summary>
        /// 停止心跳音效（对应 TS: stopHeartbeat）
        /// </summary>
        public void StopHeartbeat()
        {
            _heartbeatActive = false;
            _heartbeatTimer = 0f;
        }

        /// <summary>
        /// 更新心跳定时器
        /// </summary>
        private void UpdateHeartbeat()
        {
            if (!_heartbeatActive || HeartbeatClip == null) return;

            _heartbeatTimer += Time.deltaTime;
            if (_heartbeatTimer >= _heartbeatInterval)
            {
                _heartbeatTimer -= _heartbeatInterval;
                PlayHeartbeatPulse();
            }
        }

        /// <summary>
        /// 播放一次心跳脉冲
        /// </summary>
        private void PlayHeartbeatPulse()
        {
            PlayOneShot(HeartbeatClip, 0.5f * SFXVolume * MasterVolume);
        }

        // =================================================================
        // 菜单音效
        // =================================================================

        /// <summary>
        /// 播放菜单开始音效（对应 TS: menuStart）
        ///
        /// 原版使用双三角波振荡器（220→440Hz + 660Hz延迟叠加）。
        /// </summary>
        public void PlayMenuStart()
        {
            PlayOneShot(MenuStartClip, 0.3f * SFXVolume * MasterVolume);
        }

        /// <summary>
        /// 播放UI悬停音效
        /// </summary>
        public void PlayUIHover()
        {
            PlayOneShot(UIHoverClip, 0.1f * SFXVolume * MasterVolume, 1.0f + Random.Range(-0.05f, 0.05f));
        }

        /// <summary>
        /// 播放UI点击音效（对应 TS: interactClick）
        ///
        /// 原版使用900~1100Hz随机频率正弦波，极短衰减。
        /// </summary>
        public void PlayUIClick()
        {
            float pitch = 1.0f + Random.Range(-0.1f, 0.1f);
            PlayOneShot(UIClickClip, 0.2f * SFXVolume * MasterVolume, pitch);
        }

        // =================================================================
        // 背景音乐
        // =================================================================

        /// <summary>
        /// 开始播放背景音乐（The_Long_Way_Up.mp3）
        /// </summary>
        /// <param name="fadeDuration">淡入时长（秒）</param>
        public void PlayBGM(float fadeDuration = 3f)
        {
            if (_bgmPlaying || BGMClip == null) return;

            _bgmSource.clip = BGMClip;
            _bgmSource.volume = 0f;
            _bgmSource.Play();
            _bgmPlaying = true;

            StartCoroutine(FadeIn(_bgmSource, BGMVolume * MasterVolume, fadeDuration));
        }

        /// <summary>
        /// 停止背景音乐
        /// </summary>
        /// <param name="fadeDuration">淡出时长（秒）</param>
        public void StopBGM(float fadeDuration = 2f)
        {
            if (!_bgmPlaying) return;

            StartCoroutine(FadeOutAndStop(_bgmSource, fadeDuration));
            _bgmPlaying = false;
        }

        /// <summary>
        /// 更新BGM音量（响应设置变更）
        /// </summary>
        private void UpdateBGM()
        {
            if (_bgmPlaying && _bgmSource != null)
            {
                _bgmSource.volume = Mathf.Lerp(_bgmSource.volume,
                    BGMVolume * MasterVolume, Time.deltaTime * 2f);
            }
        }

        // =================================================================
        // 全局控制
        // =================================================================

        /// <summary>
        /// 停止所有声音（对应 TS: killAll）
        ///
        /// 原版对所有活跃 GainNode 执行0.5秒渐隐。
        /// Unity 版停止所有 AudioSource。
        /// </summary>
        public void KillAll()
        {
            StopHeartbeat();
            StopEngine(0.5f);
            StopWind(0.5f);
            StopBGM(0.5f);

            // 停止所有 SFX 池中的播放
            foreach (var src in _sfxPool)
            {
                if (src != null && src.isPlaying)
                {
                    StartCoroutine(FadeOutAndStop(src, 0.5f));
                }
            }
        }

        /// <summary>
        /// 设置主音量
        /// </summary>
        /// <param name="volume">0~1</param>
        public void SetMasterVolume(float volume)
        {
            MasterVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// 设置音效音量
        /// </summary>
        /// <param name="volume">0~1</param>
        public void SetSFXVolume(float volume)
        {
            SFXVolume = Mathf.Clamp01(volume);
        }

        /// <summary>
        /// 设置背景音乐音量
        /// </summary>
        /// <param name="volume">0~1</param>
        public void SetBGMVolume(float volume)
        {
            BGMVolume = Mathf.Clamp01(volume);
        }

        // =================================================================
        // 事件回调
        // =================================================================

        /// <summary>
        /// 碎片发现事件回调
        /// </summary>
        private void OnFragmentDiscovered(string fragmentId)
        {
            PlayFragmentDiscover();
        }

        /// <summary>
        /// 游戏阶段变更事件回调（章节过渡）
        /// </summary>
        private void OnGamePhaseChanged(int journey)
        {
            PlayChapterTransition();
        }

        /// <summary>
        /// 障碍物清除事件回调（碰撞音效）
        /// </summary>
        private void OnObstacleCleared(string obstacleId)
        {
            PlayTreeImpact(0.5f);
        }

        /// <summary>
        /// 生命值变更事件回调（低血量心跳）
        /// </summary>
        private void OnHealthChanged(float health)
        {
            if (health < HeartbeatHealthThreshold && health > 0f)
            {
                // 血量越低，心跳越快
                float t = 1f - (health / HeartbeatHealthThreshold);
                float bpm = Mathf.Lerp(HeartbeatMinBPM, HeartbeatMaxBPM, t);
                StartHeartbeat(bpm);
            }
            else
            {
                StopHeartbeat();
            }
        }

        // =================================================================
        // 音效播放辅助
        // =================================================================

        /// <summary>
        /// 从对象池中获取空闲 AudioSource 播放一次性音效
        /// </summary>
        /// <param name="clip">音频片段</param>
        /// <param name="volume">音量</param>
        /// <param name="pitch">音调</param>
        private void PlayOneShot(AudioClip clip, float volume, float pitch = 1f)
        {
            if (clip == null) return;

            AudioSource src = GetAvailableSfxSource();
            if (src == null) return;

            src.pitch = pitch;
            src.volume = Mathf.Clamp01(volume);
            src.PlayOneShot(clip);
        }

        /// <summary>
        /// 从 SFX 池中获取一个可用的 AudioSource
        /// </summary>
        /// <returns>空闲的 AudioSource，池满时返回 null</returns>
        private AudioSource GetAvailableSfxSource()
        {
            // 优先找空闲的
            foreach (var src in _sfxPool)
            {
                if (src != null && !src.isPlaying)
                    return src;
            }

            // 池满时找最早播放的覆盖
            if (_sfxPool.Count > 0)
                return _sfxPool[0];

            return null;
        }

        // =================================================================
        // 协程：淡入 / 淡出
        // =================================================================

        /// <summary>
        /// 渐入协程
        /// </summary>
        private System.Collections.IEnumerator FadeIn(AudioSource source, float targetVolume, float duration)
        {
            if (source == null) yield break;

            float startVol = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                source.volume = Mathf.Lerp(startVol, targetVolume, t);
                yield return null;
            }

            source.volume = targetVolume;
        }

        /// <summary>
        /// 渐出并停止协程
        /// </summary>
        private System.Collections.IEnumerator FadeOutAndStop(AudioSource source, float duration)
        {
            if (source == null) yield break;

            float startVol = source.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                source.volume = Mathf.Lerp(startVol, 0f, t);
                yield return null;
            }

            source.volume = 0f;
            source.Stop();
        }

        // =================================================================
        // 便捷静态方法（供外部快速调用）
        // =================================================================

        /// <summary>
        /// 静态快捷方法：播放交互点击音效
        /// </summary>
        public static void PlayInteractClick()
        {
            if (_instance != null)
                _instance.PlayUIClick();
        }

        /// <summary>
        /// 静态快捷方法：播放UI悬停音效
        /// </summary>
        public static void PlayHover()
        {
            if (_instance != null)
                _instance.PlayUIHover();
        }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-f0381e6190794fe39bcbdc664146a86b/cwd.txt'; exit "$__tr_native_ec"
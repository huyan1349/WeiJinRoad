using UnityEngine;
using WeiJinRoad.Core;
using WeiJinRoad.Vehicle;
using WeiJinRoad.World;

namespace WeiJinRoad.Core
{
    // ═══════════════════════════════════════════════════════════════
    // SceneBootstrap — 主场景启动引导器
    //
    // 挂载到场景中的任意 GameObject 上，负责：
    // 1. 初始化核心系统（GameManager / RoadSpline / SaveSystem）
    // 2. 搭建场景层级（相机 / 灯光 / 地形 / 载具 / 环境光照）
    // 3. 游戏循环（暂停 / 自动存档）
    // 4. 相机跟随逻辑
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 主场景启动引导器
    /// 在 Awake 中初始化核心系统，在 Start 中搭建场景层级，
    /// 在 Update 中处理暂停菜单和自动存档。
    /// </summary>
    public class SceneBootstrap : MonoBehaviour
    {
        // =================================================================
        // Inspector 配置
        // =================================================================

        [Header("相机设置")]
        [Tooltip("相机跟随距离")]
        [SerializeField] private float _followDistance = 14f;
        [Tooltip("相机跟随高度")]
        [SerializeField] private float _followHeight = 18f;
        [Tooltip("相机位置插值速度")]
        [SerializeField] private float _positionSmoothSpeed = 8f;
        [Tooltip("相机目标插值速度")]
        [SerializeField] private float _targetSmoothSpeed = 10f;
        [Tooltip("相机侧向偏移")]
        [SerializeField] private float _lateralOffset = 3f;

        [Header("自动存档")]
        [Tooltip("自动存档间隔（秒）")]
        [SerializeField] private float _autoSaveInterval = 60f;

        [Header("暂停")]
        [Tooltip("暂停时的时间缩放")]
        [SerializeField] private float _pausedTimeScale = 0f;

        // =================================================================
        // 运行时引用
        // =================================================================

        private Camera _mainCamera;
        private FollowCamera _followCamera;
        private Light _sunLight;
        private GameObject _terrainGeneratorObj;
        private GameObject _vehicleObj;
        private GameObject _environmentLightingObj;

        private bool _isPaused;
        private float _autoSaveTimer;

        /// <summary>是否暂停</summary>
        public bool IsPaused => _isPaused;

        // =================================================================
        // Awake — 初始化核心系统
        // =================================================================

        private void Awake()
        {
            InitializeCoreSystems();
        }

        // =================================================================
        // Start — 搭建场景层级
        // =================================================================

        private void Start()
        {
            SetupSceneHierarchy();
        }

        // =================================================================
        // Update — 游戏循环
        // =================================================================

        private void Update()
        {
            HandlePauseInput();
            HandleAutoSave();
        }

        // =================================================================
        // 核心系统初始化
        // =================================================================

        /// <summary>
        /// 初始化核心系统：GameManager、RoadSpline、SaveSystem
        /// </summary>
        private void InitializeCoreSystems()
        {
            // 1. 确保 GameManager 单例存在
            var gameManager = GameManager.Instance;
            if (gameManager == null)
            {
                var gmObj = new GameObject("[GameManager]");
                gameManager = gmObj.AddComponent<GameManager>();
                Debug.Log("[SceneBootstrap] GameManager 已创建");
            }

            // 2. 确保 RoadSpline 单例存在
            if (RoadSpline.Instance == null)
            {
                var rsObj = new GameObject("[RoadSpline]");
                rsObj.AddComponent<RoadSpline>();
                Debug.Log("[SceneBootstrap] RoadSpline 已创建");
            }

            // 3. 初始化 TerrainHeight 的道路采样器
            if (TerrainHeight.RoadSampler == null && RoadSpline.Instance != null)
            {
                TerrainHeight.RoadSampler = new RoadSamplerAdapter(RoadSpline.Instance.Data);
                Debug.Log("[SceneBootstrap] TerrainHeight.RoadSampler 已设置");
            }

            // 4. 加载存档或开始新游戏
            if (SaveSystem.HasSave())
            {
                bool loaded = SaveSystem.LoadGame();
                if (loaded)
                {
                    Debug.Log("[SceneBootstrap] 存档加载成功");
                }
                else
                {
                    Debug.LogWarning("[SceneBootstrap] 存档加载失败，开始新游戏");
                    gameManager.NewGame();
                }
            }
            else
            {
                gameManager.NewGame();
                Debug.Log("[SceneBootstrap] 无存档，开始新游戏");
            }
        }

        // =================================================================
        // 场景层级搭建
        // =================================================================

        /// <summary>
        /// 搭建场景层级：相机、灯光、地形、载具、环境光照
        /// </summary>
        private void SetupSceneHierarchy()
        {
            CreateMainCamera();
            CreateSunLight();
            CreateTerrainGenerator();
            CreateVehicle();
            CreateEnvironmentLighting();
            SetupCameraFollow();
            Debug.Log("[SceneBootstrap] 场景层级搭建完成");
        }

        // ── 创建主相机 ──

        private void CreateMainCamera()
        {
            _mainCamera = Camera.main;
            if (_mainCamera != null) return;

            var camObj = new GameObject("Main Camera");
            camObj.transform.position = new Vector3(0f, 20f, 280f);
            camObj.transform.rotation = Quaternion.Euler(35f, 180f, 0f);

            _mainCamera = camObj.AddComponent<Camera>();
            _mainCamera.clearFlags = CameraClearFlags.SolidColor;
            _mainCamera.backgroundColor = new Color(0.12f, 0.14f, 0.18f, 1f);
            _mainCamera.fieldOfView = 60f;
            _mainCamera.nearClipPlane = 0.3f;
            _mainCamera.farClipPlane = 2000f;
            _mainCamera.allowHDR = true;
            _mainCamera.allowMSAA = true;

            camObj.AddComponent<AudioListener>();
            camObj.tag = "MainCamera";

            Debug.Log("[SceneBootstrap] 主相机已创建");
        }

        // ── 创建方向光（太阳）──

        private void CreateSunLight()
        {
            var existingSun = FindFirstObjectByType<Light>();
            if (existingSun != null && existingSun.type == LightType.Directional)
            {
                _sunLight = existingSun;
                return;
            }

            var sunObj = new GameObject("Directional Light (Sun)");
            _sunLight = sunObj.AddComponent<Light>();
            _sunLight.type = LightType.Directional;

            // 冬季氛围：低角度冷色调光照
            _sunLight.transform.rotation = Quaternion.Euler(25f, -30f, 0f);
            _sunLight.color = new Color(0.85f, 0.88f, 0.95f, 1f);
            _sunLight.intensity = 0.8f;
            _sunLight.shadows = LightShadows.Soft;
            _sunLight.shadowStrength = 0.7f;
            _sunLight.shadowDistance = 150f;
            _sunLight.shadowNormalBias = 1f;
            _sunLight.shadowBias = 0.5f;

            // 环境光设置
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.25f, 0.28f, 0.35f, 1f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogColor = new Color(0.65f, 0.70f, 0.78f, 1f);
            RenderSettings.fogDensity = 0.003f;

            Debug.Log("[SceneBootstrap] 方向光（太阳）已创建");
        }

        // ── 创建地形生成器 ──

        private void CreateTerrainGenerator()
        {
            var existing = FindFirstObjectByType<TerrainGenerator>();
            if (existing != null)
            {
                _terrainGeneratorObj = existing.gameObject;
                return;
            }

            _terrainGeneratorObj = new GameObject("[TerrainGenerator]");
            _terrainGeneratorObj.AddComponent<TerrainGenerator>();
            Debug.Log("[SceneBootstrap] TerrainGenerator 已创建");
        }

        // ── 创建载具 ──

        private void CreateVehicle()
        {
            var existing = FindFirstObjectByType<VehicleController>();
            if (existing != null)
            {
                _vehicleObj = existing.gameObject;
                return;
            }

            _vehicleObj = new GameObject("[Vehicle]");
            _vehicleObj.AddComponent<VehicleController>();
            _vehicleObj.AddComponent<VehicleDamageSystem>();

            float initialZ = 500f;
            float initialX = RoadSpline.GetRoadCenter(initialZ);
            float initialY = TerrainHeight.GetTerrainHeight(initialX, initialZ) + VehicleController.TerrainYOffset;

            _vehicleObj.transform.position = new Vector3(initialX, initialY, initialZ);
            _vehicleObj.transform.rotation = Quaternion.AngleAxis(180f, Vector3.up);

            Debug.Log("[SceneBootstrap] 载具已创建");
        }

        // ── 创建环境光照 ──

        private void CreateEnvironmentLighting()
        {
            var existing = GameObject.Find("[EnvironmentLighting]");
            if (existing != null)
            {
                _environmentLightingObj = existing;
                return;
            }

            _environmentLightingObj = new GameObject("[EnvironmentLighting]");

            // 环境补光
            var fillLightObj = new GameObject("FillLight");
            fillLightObj.transform.SetParent(_environmentLightingObj.transform, false);
            fillLightObj.transform.rotation = Quaternion.Euler(50f, 120f, 0f);
            var fillLight = fillLightObj.AddComponent<Light>();
            fillLight.type = LightType.Directional;
            fillLight.color = new Color(0.6f, 0.65f, 0.75f, 1f);
            fillLight.intensity = 0.3f;
            fillLight.shadows = LightShadows.None;

            // 底部反弹光
            var bounceLightObj = new GameObject("BounceLight");
            bounceLightObj.transform.SetParent(_environmentLightingObj.transform, false);
            bounceLightObj.transform.rotation = Quaternion.Euler(-80f, 0f, 0f);
            var bounceLight = bounceLightObj.AddComponent<Light>();
            bounceLight.type = LightType.Directional;
            bounceLight.color = new Color(0.4f, 0.45f, 0.55f, 1f);
            bounceLight.intensity = 0.15f;
            bounceLight.shadows = LightShadows.None;

            Debug.Log("[SceneBootstrap] 环境光照已创建");
        }

        // ── 设置相机跟随 ──

        private void SetupCameraFollow()
        {
            if (_mainCamera == null) return;

            var vc = FindFirstObjectByType<VehicleController>();
            if (vc != null)
            {
                vc.FollowCamera = _mainCamera;
            }

            _followCamera = _mainCamera.gameObject.AddComponent<FollowCamera>();
            _followCamera.Initialize(
                _vehicleObj != null ? _vehicleObj.transform : null,
                _followDistance,
                _followHeight,
                _lateralOffset,
                _positionSmoothSpeed,
                _targetSmoothSpeed
            );

            Debug.Log("[SceneBootstrap] 相机跟随已设置");
        }

        // =================================================================
        // 暂停输入处理
        // =================================================================

        private void HandlePauseInput()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                TogglePause();
            }
        }

        /// <summary>
        /// 切换暂停状态
        /// </summary>
        public void TogglePause()
        {
            _isPaused = !_isPaused;
            Time.timeScale = _isPaused ? _pausedTimeScale : 1f;

            if (_isPaused)
            {
                GameEvents.OnGamePhaseChanged?.Invoke(-1);
                Debug.Log("[SceneBootstrap] 游戏已暂停");
            }
            else
            {
                Debug.Log("[SceneBootstrap] 游戏已恢复");
            }
        }

        /// <summary>
        /// 设置暂停状态
        /// </summary>
        public void SetPaused(bool paused)
        {
            if (_isPaused == paused) return;
            _isPaused = paused;
            Time.timeScale = _isPaused ? _pausedTimeScale : 1f;
        }

        // =================================================================
        // 自动存档
        // =================================================================

        private void HandleAutoSave()
        {
            if (_isPaused) return;

            _autoSaveTimer += Time.unscaledTime > 0f ? Time.deltaTime : 0f;
            if (_autoSaveTimer >= _autoSaveInterval)
            {
                _autoSaveTimer = 0f;
                SaveSystem.SaveGame();
            }
        }

        // =================================================================
        // 清理
        // =================================================================

        private void OnDestroy()
        {
            Time.timeScale = 1f;
        }

        // =================================================================
        // RoadSamplerAdapter — 适配 RoadSplineData 到 IRoadSampler
        // =================================================================

        /// <summary>
        /// 将全局 RoadSplineData 适配为 WeiJinRoad.World.IRoadSampler 接口
        /// </summary>
        private class RoadSamplerAdapter : IRoadSampler
        {
            private readonly RoadSplineData _data;

            public RoadSamplerAdapter(RoadSplineData data)
            {
                _data = data;
            }

            public RoadSample Sample(float z)
            {
                var s = _data.Sample(z);
                return new RoadSample
                {
                    Z = s.z,
                    CenterX = s.centerX,
                    Height = s.height,
                    Width = s.width,
                    HalfWidth = s.halfWidth,
                    Direction = s.direction,
                    RouteZ = s.routeZ,
                    IsMountainRoad = s.isMountainRoad,
                    IsForestRoad = s.isForestRoad,
                    MountainBedHalfWidth = s.mountainBedHalfWidth,
                };
            }

            public ClosestRoadPointResult ClosestRoadPoint(float x, float z, float searchRadius, float step)
            {
                var result = _data.ClosestRoadPoint(x, z, searchRadius, step);
                if (result == null)
                {
                    return new ClosestRoadPointResult { Found = false };
                }
                var (sample, distSq) = result.Value;
                return new ClosestRoadPointResult
                {
                    Sample = new RoadSample
                    {
                        Z = sample.z,
                        CenterX = sample.centerX,
                        Height = sample.height,
                        Width = sample.width,
                        HalfWidth = sample.halfWidth,
                        Direction = sample.direction,
                        RouteZ = sample.routeZ,
                        IsMountainRoad = sample.isMountainRoad,
                        IsForestRoad = sample.isForestRoad,
                        MountainBedHalfWidth = sample.mountainBedHalfWidth,
                    },
                    DistSq = distSq,
                    Found = true,
                };
            }
        }
    }

    // ═══════════════════════════════════════════════════════════════
    // FollowCamera — 简易相机跟随脚本
    //
    // 相机在载具后上方跟随，使用平滑插值。
    // 距离、高度、灵敏度可调。
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 相机跟随脚本：在载具后上方跟随，平滑插值位置和旋转
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        private Transform _target;
        private float _distance;
        private float _height;
        private float _lateralOffset;
        private float _positionSmoothSpeed;
        private float _targetSmoothSpeed;

        /// <summary>相机灵敏度（影响旋转跟随速度）</summary>
        public float Sensitivity { get; set; } = 8f;

        private Vector3 _smoothedTarget;
        private bool _initialized;

        /// <summary>
        /// 初始化跟随相机参数
        /// </summary>
        public void Initialize(
            Transform target,
            float distance,
            float height,
            float lateralOffset,
            float positionSmoothSpeed,
            float targetSmoothSpeed)
        {
            _target = target;
            _distance = distance;
            _height = height;
            _lateralOffset = lateralOffset;
            _positionSmoothSpeed = positionSmoothSpeed;
            _targetSmoothSpeed = targetSmoothSpeed;

            if (_target != null)
            {
                _smoothedTarget = _target.position + Vector3.up * 0.5f;
                _initialized = true;
            }
        }

        private void LateUpdate()
        {
            if (!_initialized || _target == null) return;

            float camHeight = _height;
            float camDist = _distance;
            if (GameManager.Instance != null)
            {
                camHeight = GameManager.Instance.CameraHeight;
                camDist = GameManager.Instance.CameraDistance;
            }

            Vector3 lookAtTarget = _target.position;
            lookAtTarget.y += 0.5f;

            if (Vector3.Distance(_smoothedTarget, lookAtTarget) > 100f)
            {
                _smoothedTarget = lookAtTarget;
            }
            else
            {
                _smoothedTarget = Vector3.Lerp(
                    _smoothedTarget,
                    lookAtTarget,
                    Time.deltaTime * _targetSmoothSpeed
                );
            }

            float heading = Mathf.Atan2(_target.forward.x, _target.forward.z);

            Vector3 idealOffset = new Vector3(_lateralOffset, camHeight + 1.5f, -camDist + 4f);
            idealOffset = Quaternion.AngleAxis(heading * Mathf.Rad2Deg, Vector3.up) * idealOffset;

            Vector3 idealPos = _smoothedTarget + idealOffset;

            transform.position = Vector3.Lerp(
                transform.position,
                idealPos,
                Time.deltaTime * Sensitivity
            );

            transform.LookAt(_smoothedTarget);
        }

        /// <summary>
        /// 立即将相机跳转到目标位置（无插值）
        /// </summary>
        public void SnapToTarget()
        {
            if (_target == null) return;

            float heading = Mathf.Atan2(_target.forward.x, _target.forward.z);
            float camHeight = _height;
            float camDist = _distance;
            if (GameManager.Instance != null)
            {
                camHeight = GameManager.Instance.CameraHeight;
                camDist = GameManager.Instance.CameraDistance;
            }

            Vector3 idealOffset = new Vector3(_lateralOffset, camHeight + 1.5f, -camDist + 4f);
            idealOffset = Quaternion.AngleAxis(heading * Mathf.Rad2Deg, Vector3.up) * idealOffset;

            _smoothedTarget = _target.position + Vector3.up * 0.5f;
            transform.position = _smoothedTarget + idealOffset;
            transform.LookAt(_smoothedTarget);
        }

        /// <summary>更新跟随距离</summary>
        public void SetDistance(float distance) => _distance = distance;

        /// <summary>更新跟随高度</summary>
        public void SetHeight(float height) => _height = height;

        /// <summary>更新相机灵敏度</summary>
        public void SetSensitivity(float sensitivity) => Sensitivity = sensitivity;
    }
}

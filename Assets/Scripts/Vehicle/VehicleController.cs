using UnityEngine;
using WeiJinRoad.World;
using WeiJinRoad.Core;

namespace WeiJinRoad.Vehicle
{
    // =====================================================================
    // 碰撞方向类型
    // =====================================================================

    /// <summary>
    /// 碰撞方向：正面/侧面/低矮物
    /// </summary>
    public enum ImpactDirection
    {
        Front,
        Side,
        Low
    }

    // =====================================================================
    // 碰撞结果结构体（用于树/栅栏碰撞检测返回值）
    // =====================================================================

    /// <summary>
    /// 碰撞检测结果
    /// </summary>
    public struct TreeCollisionHit
    {
        public float Nx;            // 法线X
        public float Nz;            // 法线Z
        public float Penetration;   // 穿透深度
        public float Power;         // 碰撞力度 0~1
        public ColliderKind Kind;   // 碰撞体类型
        public float BodyX;         // 碰撞体位置X
        public float BodyZ;         // 碰撞体位置Z
        public float FallDirX;      // 倒向方向X
        public float FallDirZ;      // 倒向方向Z
    }

    /// <summary>
    /// 碰撞体类型
    /// </summary>
    public enum ColliderKind
    {
        Tree,
        Fence
    }

    // =====================================================================
    // 障碍物碰撞结果
    // =====================================================================

    /// <summary>
    /// 路面障碍碰撞结果
    /// </summary>
    public struct ObstacleResult
    {
        public float PushX;
        public float PushZ;
        public float VelocityMul;
        public ObstacleCleared Cleared;
    }

    /// <summary>
    /// 被清除的障碍物信息
    /// </summary>
    public struct ObstacleCleared
    {
        public int Id;
        public string Kind;
        public float X;
        public float Z;
    }

    // =====================================================================
    // 车辆控制器 - 主驾驶物理与逻辑
    // =====================================================================

    /// <summary>
    /// 车辆控制器：翻译自 TypeScript 版 Vehicle.tsx
    /// 包含驾驶物理、输入处理、地形跟随、探照灯系统、灯光、碰撞响应、相机震动、车轮旋转
    /// 所有物理常量与原版 TypeScript 代码完全一致
    /// </summary>
    [RequireComponent(typeof(VehicleDamageSystem))]
    public class VehicleController : MonoBehaviour
    {
        // =================================================================
        // 常量 - 与 TypeScript 版本完全一致
        // =================================================================

        /// <summary>最大速度 (m/s)</summary>
        public const float MaxSpeed = 10f;

        /// <summary>加速力度</summary>
        public const float Accel = 14f;

        /// <summary>摩擦力/阻力</summary>
        public const float Friction = 10f;

        /// <summary>转向速度</summary>
        public const float TurnSpeed = 4f;

        /// <summary>加速最大速度 (Shift)</summary>
        public const float BoostMaxSpeed = 15f;

        /// <summary>加速力度倍率 (Shift)</summary>
        public const float BoostAccelMult = 2f;

        /// <summary>最大转向角</summary>
        public const float MaxSteerAngle = 0.6f;

        /// <summary>转向角插值速度</summary>
        public const float SteerLerpSpeed = 15f;

        /// <summary>地形Y偏移（车辆悬浮高度）</summary>
        public const float TerrainYOffset = 0.35f;

        /// <summary>Y位置插值速度</summary>
        public const float YLerpSpeed = 6f;

        /// <summary>地形法线对齐插值速度</summary>
        public const float NormalAlignSpeed = 6f;

        /// <summary>车轮滚动速率系数</summary>
        public const float WheelRollRate = 2.5f;

        /// <summary>世界X边界</summary>
        public const float WorldXMin = -620f;
        public const float WorldXMax = 620f;

        /// <summary>世界Z边界上限</summary>
        public const float WorldZMax = 540f;

        /// <summary>探照灯目标强度</summary>
        public const float SearchlightTargetIntensity = 200f;

        /// <summary>探照灯强度插值速度</summary>
        public const float SearchlightLerpSpeed = 4f;

        /// <summary>双击Space时间窗口 (ms)</summary>
        public const float DoubleTapWindow = 350f;

        /// <summary>全照明持续时间 (s)</summary>
        public const float FullIllumDuration = 3f;

        /// <summary>全照明冷却时间 (s)</summary>
        public const float FullIllumCooldown = 13f;

        /// <summary>全照明强度上升速度</summary>
        public const float FullIllumRiseSpeed = 3f;

        /// <summary>全照明强度下降速度</summary>
        public const float FullIllumFallSpeed = 1.5f;

        /// <summary>全照明最大光强</summary>
        public const float FullIllumMaxIntensity = 600f;

        /// <summary>碰撞视觉持续时间基础 (s)</summary>
        public const float ImpactDurationBase = 0.72f;

        /// <summary>碰撞视觉持续时间力度系数</summary>
        public const float ImpactDurationPowerScale = 0.22f;

        /// <summary>车身弹跳幅度系数</summary>
        public const float BodyBounceAmplitude = 0.05f;

        /// <summary>车身弹跳频率</summary>
        public const float BodyBounceFrequency = 15f;

        /// <summary>碰撞视觉Y偏移</summary>
        public const float ImpactVisualYOffset = 0.22f;

        /// <summary>碰撞视觉X旋转</summary>
        public const float ImpactVisualXRot = 0.34f;

        /// <summary>碰撞视觉Z旋转</summary>
        public const float ImpactVisualZRot = 0.3f;

        /// <summary>碰撞震动频率</summary>
        public const float ImpactShakeFrequency = 52f;

        /// <summary>碰撞视觉阻尼速度</summary>
        public const float ImpactVisualDampSpeed = 10f;

        // =================================================================
        // Inspector 可配置引用
        // =================================================================

        [Header("Vehicle Body")]
        [Tooltip("车身组（用于弹跳和碰撞视觉偏移）")]
        public Transform VehicleBody;

        [Header("Wheels")]
        [Tooltip("左前轮组（用于转向旋转）")]
        public Transform FrontLeftWheelGroup;
        [Tooltip("右前轮组（用于转向旋转）")]
        public Transform FrontRightWheelGroup;
        [Tooltip("左前轮网格（用于滚动旋转）")]
        public Transform FrontLeftWheel;
        [Tooltip("右前轮网格（用于滚动旋转）")]
        public Transform FrontRightWheel;
        [Tooltip("左后轮网格（用于滚动旋转）")]
        public Transform BackLeftWheel;
        [Tooltip("右后轮网格（用于滚动旋转）")]
        public Transform BackRightWheel;

        [Header("Lights")]
        [Tooltip("左前灯 SpotLight")]
        public Light LeftHeadlight;
        [Tooltip("右前灯 SpotLight")]
        public Light RightHeadlight;
        [Tooltip("左前灯目标")]
        public Transform LeftHeadlightTarget;
        [Tooltip("右前灯目标")]
        public Transform RightHeadlightTarget;

        [Tooltip("左尾灯 PointLight")]
        public Light LeftTailLight;
        [Tooltip("右尾灯 PointLight")]
        public Light RightTailLight;

        [Tooltip("探照灯 SpotLight")]
        public Light Searchlight;
        [Tooltip("探照灯目标")]
        public Transform SearchlightTarget;
        [Tooltip("探照灯体 PointLight")]
        public Light SearchlightBodyLight;
        [Tooltip("全照明 PointLight")]
        public Light FullIlluminationLight;

        [Header("Headlight Materials")]
        [Tooltip("前灯发光材质")]
        public Material HeadlightEmissiveMat;
        [Tooltip("车窗发光材质")]
        public Material WindowEmissiveMat;
        [Tooltip("探照灯镜头材质")]
        public Material SearchlightLensMat;
        [Tooltip("探照灯光束材质")]
        public Material SearchlightBeamMat;
        [Tooltip("左尾灯材质")]
        public Material LeftTailLightMat;
        [Tooltip("右尾灯材质")]
        public Material RightTailLightMat;

        [Header("Camera")]
        [Tooltip("跟随相机")]
        public Camera FollowCamera;

        [Header("Settings")]
        [Tooltip("是否暂停")]
        public bool Paused = false;
        [Tooltip("强制探照灯开启")]
        public bool ForceSearchlight = false;
        [Tooltip("灯光是否启用")]
        public bool LightsEnabled = true;

        // =================================================================
        // 运行时状态
        // =================================================================

        // 驾驶状态
        private float _velocity;
        private float _heading;
        private float _steerAngle;

        // 探照灯状态
        private float _searchlightIntensity;
        private float _searchlightFlickerTimer;
        private bool _prevSearchlightOn;
        private bool _prevSpaceDown;
        private float _lastSpacePressTime;
        private float _fullIllumTimer;
        private float _fullIllumCooldown;
        private float _fullIllumIntensity;

        // 碰撞视觉状态
        private float _impactTimer;
        private float _impactPower;
        private float _impactSideSign;
        private float _impactDuration;
        private float _impactVisual;
        private float _impactPhase;
        private float _lastImpactPower;
        private ImpactDirection _lastImpactDirection;

        // 引擎状态
        private bool _engineStarted;
        private bool _prevBraking;
        private bool _outOfFuel;

        // 损耗系统引用
        private VehicleDamageSystem _damageSystem;

        // 缓存
        private Vector3 _prevPos;
        private Vector3 _smoothedCamTarget;

        // =================================================================
        // 公开属性（供其他系统读取）
        // =================================================================

        /// <summary>当前速度 (m/s)</summary>
        public float Velocity => _velocity;

        /// <summary>当前航向角 (弧度)</summary>
        public float Heading => _heading;

        /// <summary>当前转向角</summary>
        public float SteerAngleValue => _steerAngle;

        /// <summary>是否油尽</summary>
        public bool IsOutOfFuel => _outOfFuel;

        /// <summary>探照灯强度 (0~1)</summary>
        public float SearchlightIntensity01 => Mathf.Min(1f, _searchlightIntensity / SearchlightTargetIntensity);

        /// <summary>全照明强度 (0~1)</summary>
        public float FullIlluminationIntensity01 => _fullIllumIntensity;

        /// <summary>当前碰撞冲击力（供损耗系统读取）</summary>
        public float CurrentImpactPower => _impactTimer > _impactDuration * 0.8f ? _impactPower : 0f;

        /// <summary>当前碰撞方向（供损耗系统读取）</summary>
        public ImpactDirection? CurrentImpactDirection => CurrentImpactPower > 0f ? (ImpactDirection?)_lastImpactDirection : null;

        // =================================================================
        // Unity 生命周期
        // =================================================================

        private void Awake()
        {
            _damageSystem = GetComponent<VehicleDamageSystem>();
        }

        private void Start()
        {
            // 初始位置（与 TS 版 stdInitialZ=500 一致）
            float initialZ = 500f;
            float initialX = GetRoadCenter(initialZ);
            float initialY = TerrainHeight.GetTerrainHeight(initialX, initialZ) + TerrainYOffset;

            _heading = Mathf.PI;
            _prevPos = new Vector3(initialX, initialY, initialZ);
            _smoothedCamTarget = new Vector3(initialX, initialY + 0.5f, initialZ);

            transform.position = new Vector3(initialX, initialY, initialZ);
            transform.rotation = Quaternion.AngleAxis(_heading * Mathf.Rad2Deg, Vector3.up);
        }

        private void Update()
        {
            if (Paused) return;

            float delta = Time.deltaTime;
            if (delta <= 0f) return;

            // ── 输入读取 ──
            bool forward = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            bool backward = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            bool left = Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            bool right = Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            bool boost = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            bool spaceDown = Input.GetKey(KeyCode.Space);

            // ── 性能修正 ──
            var perfMods = _damageSystem != null
                ? _damageSystem.GetPerformanceModifiers()
                : PerformanceModifiers.Default;

            float currentMaxSpeed = boost ? BoostMaxSpeed : MaxSpeed;
            float currentAccel = boost ? Accel * BoostAccelMult : Accel;
            float effectiveMaxSpeed = currentMaxSpeed * perfMods.MaxSpeedFactor;
            float effectiveTurnSpeed = TurnSpeed * perfMods.TurnSpeedFactor;

            // ── 加速/减速 ──
            if (forward) _velocity += currentAccel * delta;
            if (backward) _velocity -= currentAccel * delta;

            // 摩擦力（无输入时）
            if (!forward && !backward)
            {
                if (_velocity > 0f)
                {
                    _velocity -= Friction * delta;
                    if (_velocity < 0f) _velocity = 0f;
                }
                else if (_velocity < 0f)
                {
                    _velocity += Friction * delta;
                    if (_velocity > 0f) _velocity = 0f;
                }
            }

            // 释放Shift后额外阻力（与 TS: velocity > maxSpeed 时 friction*2）
            if (!boost && _velocity > MaxSpeed)
            {
                _velocity -= Friction * 2f * delta;
            }

            // 油尽渐变减速（与 TS: velocity *= (1 - delta * 2)）
            if (perfMods.OutOfFuel)
            {
                _velocity *= (1f - delta * 2f);
                if (Mathf.Abs(_velocity) < 0.05f) _velocity = 0f;
                _outOfFuel = true;
            }
            else
            {
                _outOfFuel = false;
            }

            // 速度钳制（与 TS: clamp(-effectiveMaxSpeed/2, effectiveMaxSpeed)）
            _velocity = Mathf.Clamp(_velocity, -effectiveMaxSpeed / 2f, effectiveMaxSpeed);

            // ── 转向 ──
            float targetSteerAngle = (left ? MaxSteerAngle : 0f) + (right ? -MaxSteerAngle : 0f);
            _steerAngle = Mathf.Lerp(_steerAngle, targetSteerAngle, delta * SteerLerpSpeed);

            // 转向应用（与 TS: heading += steerAngle * turnSpeed * dir * (0.5 + speedFactor*0.5) * delta）
            if (Mathf.Abs(_velocity) > 0.1f)
            {
                float dir = _velocity >= 0f ? 1f : -1f;
                float speedFactor = Mathf.Abs(_velocity) / effectiveMaxSpeed;
                _heading += _steerAngle * effectiveTurnSpeed * dir * (0.5f + speedFactor * 0.5f) * delta;
            }

            // ── 位置更新 ──
            float startX = transform.position.x;
            float startZ = transform.position.z;
            float nextX = startX + Mathf.Sin(_heading) * _velocity * delta;
            float nextZ = startZ + Mathf.Cos(_heading) * _velocity * delta;

            // 世界边界（与 TS: clamp X -620~620, Z summit-80~540）
            nextX = Mathf.Clamp(nextX, WorldXMin, WorldXMax);
            nextZ = Mathf.Clamp(nextZ, TerrainHeight.RouteToWorldZ(TerrainHeight.SummitRouteZ - 80f), WorldZMax);

            // ── 碰撞检测 ──
            // 碰撞检测由外部系统驱动，调用 ApplyCollision()
            // 此处预留：若项目有 TreeCollisionSystem 组件，自动调用
            var treeCollision = GetComponent<TreeCollisionSystem>();
            if (treeCollision != null)
            {
                var hit = treeCollision.CheckCollision(nextX, nextZ, _velocity, Time.time);
                if (hit != null)
                {
                    ApplyCollision(hit.Value, ref nextX, ref nextZ);
                }
            }

            // ── 应用位置 ──
            transform.position = new Vector3(nextX, transform.position.y, nextZ);

            // ── 地形Y跟随（与 TS: lerp(currentY, groundY+0.35, delta*6)）──
            float groundY = TerrainHeight.GetTerrainHeight(nextX, nextZ);
            float targetY = groundY + TerrainYOffset;
            float newY = Mathf.Lerp(transform.position.y, targetY, delta * YLerpSpeed);
            transform.position = new Vector3(nextX, newY, nextZ);

            // ── 地形法线对齐（与 TS: slerp(quat, delta*6)）──
            Vector3 normal = TerrainHeight.GetTerrainNormal(nextX, nextZ, 1.5f);
            Quaternion targetQuat = Quaternion.FromToRotation(Vector3.up, normal);
            Quaternion rotationY = Quaternion.AngleAxis(_heading * Mathf.Rad2Deg, Vector3.up);
            targetQuat = targetQuat * rotationY;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetQuat, delta * NormalAlignSpeed);

            // ── 前轮转向视觉 ──
            if (FrontLeftWheelGroup != null)
                FrontLeftWheelGroup.localRotation = Quaternion.Euler(0f, _steerAngle * Mathf.Rad2Deg, 0f);
            if (FrontRightWheelGroup != null)
                FrontRightWheelGroup.localRotation = Quaternion.Euler(0f, _steerAngle * Mathf.Rad2Deg, 0f);

            // ── 车轮滚动（与 TS: rotation.x -= velocity * delta * 2.5）──
            float rollDelta = _velocity * delta * WheelRollRate;
            if (FrontLeftWheel != null)
                FrontLeftWheel.Rotate(Vector3.right, -rollDelta * Mathf.Rad2Deg);
            if (FrontRightWheel != null)
                FrontRightWheel.Rotate(Vector3.right, -rollDelta * Mathf.Rad2Deg);
            if (BackLeftWheel != null)
                BackLeftWheel.Rotate(Vector3.right, -rollDelta * Mathf.Rad2Deg);
            if (BackRightWheel != null)
                BackRightWheel.Rotate(Vector3.right, -rollDelta * Mathf.Rad2Deg);

            // ── 探照灯系统 ──
            UpdateSearchlight(delta, spaceDown, perfMods);

            // ── 碰撞视觉 ──
            UpdateImpactVisual(delta);

            // ── 车灯 ──
            UpdateLights(perfMods);

            // ── 损耗系统 tick ──
            UpdateDamageSystem(delta, nextX, nextZ, currentMaxSpeed, boost);

            // ── 相机 ──
            UpdateCamera(delta);

            _prevPos = transform.position;
        }

        // =================================================================
        // 探照灯系统
        // =================================================================

        private void UpdateSearchlight(float delta, bool spaceDown, PerformanceModifiers perfMods)
        {
            bool searchlightOn = ForceSearchlight || spaceDown;

            // 探照灯强度受 headlight condition 影响
            float target = searchlightOn ? SearchlightTargetIntensity : 0f;
            float conditionFactor = perfMods.SearchlightFactor;

            // 探照灯闪烁效果
            _searchlightFlickerTimer += delta;
            float flickerMult = 1f;
            if (perfMods.SearchlightFlicker > 0f)
            {
                float flickerSpeed = 8f + perfMods.SearchlightFlicker * 20f;
                float flickerRaw = Mathf.Sin(_searchlightFlickerTimer * flickerSpeed)
                    * Mathf.Sin(_searchlightFlickerTimer * flickerSpeed * 1.7f + 1.3f);
                flickerMult = 1f - perfMods.SearchlightFlicker * (0.5f + 0.5f * flickerRaw);
            }

            float effectiveTarget = target * conditionFactor * flickerMult;
            _searchlightIntensity += (effectiveTarget - _searchlightIntensity) * delta * SearchlightLerpSpeed;

            // 应用探照灯
            if (Searchlight != null)
            {
                Searchlight.intensity = _searchlightIntensity;
            }

            // 探照灯镜头材质
            if (SearchlightLensMat != null)
            {
                float t = Mathf.Min(1f, _searchlightIntensity / SearchlightTargetIntensity);
                if (t > 0.01f)
                {
                    SearchlightLensMat.EnableKeyword("_EMISSION");
                    SearchlightLensMat.SetColor("_EmissionColor", new Color(0.8f, 0.933f, 1f) * t * 3f);
                }
                else
                {
                    SearchlightLensMat.SetColor("_EmissionColor", Color.black);
                }
            }

            // 探照灯体光
            if (SearchlightBodyLight != null)
            {
                float t = Mathf.Min(1f, _searchlightIntensity / SearchlightTargetIntensity);
                SearchlightBodyLight.intensity = 12f + t * 30f;
            }

            // 探照灯光束材质
            if (SearchlightBeamMat != null)
            {
                float t = Mathf.Min(1f, _searchlightIntensity / SearchlightTargetIntensity);
                Color beamColor = SearchlightBeamMat.color;
                beamColor.a = t * 0.35f;
                SearchlightBeamMat.color = beamColor;
            }

            _prevSearchlightOn = searchlightOn;

            // ── 双击Space → 全照明 ──
            bool spaceJustPressed = spaceDown && !_prevSpaceDown;
            _prevSpaceDown = spaceDown;

            if (spaceJustPressed)
            {
                float now = Time.realtimeSinceStartup * 1000f;
                if (now - _lastSpacePressTime < DoubleTapWindow && _fullIllumCooldown <= 0f)
                {
                    _fullIllumTimer = FullIllumDuration;
                    _fullIllumCooldown = FullIllumCooldown;
                }
                _lastSpacePressTime = now;
            }

            // 全照明计时器
            if (_fullIllumTimer > 0f)
            {
                _fullIllumTimer = Mathf.Max(0f, _fullIllumTimer - delta);
                _fullIllumIntensity = Mathf.Min(1f, _fullIllumIntensity + delta * FullIllumRiseSpeed);
            }
            else
            {
                _fullIllumIntensity = Mathf.Max(0f, _fullIllumIntensity - delta * FullIllumFallSpeed);
            }

            if (_fullIllumCooldown > 0f)
            {
                _fullIllumCooldown = Mathf.Max(0f, _fullIllumCooldown - delta);
            }

            // 全照明光
            if (FullIlluminationLight != null)
            {
                FullIlluminationLight.intensity = _fullIllumIntensity * FullIllumMaxIntensity;
            }
        }

        // =================================================================
        // 碰撞视觉
        // =================================================================

        private void UpdateImpactVisual(float delta)
        {
            float speedRatio = Mathf.Abs(_velocity) / MaxSpeed;
            float time = Time.time;

            if (_impactTimer > 0f)
            {
                _impactTimer = Mathf.Max(0f, _impactTimer - delta);
            }

            float impactT = _impactTimer > 0f ? _impactTimer / _impactDuration : 0f;
            // smoothstep: t*t*(3-2*t)
            float targetImpactVisual = impactT * impactT * (3f - 2f * impactT) * _impactPower;

            // 阻尼（与 TS: damp(current, target, 10, delta)）
            _impactVisual = Mathf.Lerp(_impactVisual, targetImpactVisual, 1f - Mathf.Exp(-ImpactVisualDampSpeed * delta));

            float impactShake = Mathf.Sin(time * ImpactShakeFrequency + _impactPhase) * _impactVisual;

            if (VehicleBody != null)
            {
                // 弹跳 + 碰撞偏移
                VehicleBody.localPosition = new Vector3(
                    VehicleBody.localPosition.x,
                    Mathf.Sin(time * BodyBounceFrequency * speedRatio) * BodyBounceAmplitude * speedRatio
                        + _impactVisual * ImpactVisualYOffset,
                    VehicleBody.localPosition.z
                );

                // 碰撞旋转
                VehicleBody.localRotation = Quaternion.Euler(
                    -_impactVisual * ImpactVisualXRot + Mathf.Sin(time * 36f + _impactPhase) * _impactVisual * 0.055f,
                    VehicleBody.localRotation.eulerAngles.y,
                    _impactSideSign * _impactVisual * ImpactVisualZRot + impactShake * 0.035f
                );
            }
        }

        // =================================================================
        // 灯光系统
        // =================================================================

        private void UpdateLights(PerformanceModifiers perfMods)
        {
            // 判断是否夜间
            bool isNight = GameManager.Instance != null
                ? GameManager.Instance.TimeOfDay > 17.5f || GameManager.Instance.TimeOfDay < 6f
                : false;

            // 判断前灯模式
            bool headlightsOn = false;
            if (LightsEnabled && GameManager.Instance != null)
            {
                var mode = GameManager.Instance.HeadlightsModeType;
                headlightsOn = mode == HeadlightsMode.On || (mode == HeadlightsMode.Auto && isNight);
            }

            float lightIntensity = headlightsOn && LightsEnabled ? 80f : 0f;
            float emissiveIntensityWindow = headlightsOn && LightsEnabled ? 2f : 0f;
            float emissiveIntensityLamp = headlightsOn && LightsEnabled ? 5f : 0f;

            // 前灯
            if (LeftHeadlight != null)
            {
                LeftHeadlight.intensity = lightIntensity * 1.5f;
                LeftHeadlight.enabled = lightIntensity > 0f;
            }
            if (RightHeadlight != null)
            {
                RightHeadlight.intensity = lightIntensity * 1.5f;
                RightHeadlight.enabled = lightIntensity > 0f;
            }

            // 前灯目标
            if (LeftHeadlightTarget != null && LeftHeadlight != null)
            {
                LeftHeadlight.transform.LookAt(LeftHeadlightTarget);
            }
            if (RightHeadlightTarget != null && RightHeadlight != null)
            {
                RightHeadlight.transform.LookAt(RightHeadlightTarget);
            }

            // 前灯发光材质
            if (HeadlightEmissiveMat != null)
            {
                if (headlightsOn)
                {
                    HeadlightEmissiveMat.EnableKeyword("_EMISSION");
                    HeadlightEmissiveMat.SetColor("_EmissionColor", new Color(1f, 0.667f, 0f) * emissiveIntensityLamp);
                }
                else
                {
                    HeadlightEmissiveMat.SetColor("_EmissionColor", Color.black);
                }
            }

            // 车窗发光材质
            if (WindowEmissiveMat != null)
            {
                if (headlightsOn)
                {
                    WindowEmissiveMat.EnableKeyword("_EMISSION");
                    WindowEmissiveMat.SetColor("_EmissionColor", new Color(1f, 0.533f, 0f) * emissiveIntensityWindow);
                }
                else
                {
                    WindowEmissiveMat.SetColor("_EmissionColor", Color.black);
                }
            }

            // 刹车灯
            bool isBraking = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            float tailLightEmissive = isBraking ? 15f : 0f;
            float tailLightIntensity = isBraking ? 50f : 0f;

            if (LeftTailLight != null)
            {
                LeftTailLight.intensity = tailLightIntensity;
                LeftTailLight.enabled = isBraking && LightsEnabled;
            }
            if (RightTailLight != null)
            {
                RightTailLight.intensity = tailLightIntensity;
                RightTailLight.enabled = isBraking && LightsEnabled;
            }

            // 尾灯材质
            if (LeftTailLightMat != null)
            {
                if (isBraking)
                {
                    LeftTailLightMat.EnableKeyword("_EMISSION");
                    LeftTailLightMat.SetColor("_EmissionColor", new Color(1f, 0f, 0.067f) * tailLightEmissive);
                }
                else
                {
                    LeftTailLightMat.SetColor("_EmissionColor", Color.black);
                }
            }
            if (RightTailLightMat != null)
            {
                if (isBraking)
                {
                    RightTailLightMat.EnableKeyword("_EMISSION");
                    RightTailLightMat.SetColor("_EmissionColor", new Color(1f, 0f, 0.067f) * tailLightEmissive);
                }
                else
                {
                    RightTailLightMat.SetColor("_EmissionColor", Color.black);
                }
            }

            _prevBraking = isBraking;
        }

        // =================================================================
        // 碰撞处理
        // =================================================================

        /// <summary>
        /// 应用树/栅栏碰撞结果
        /// 由外部碰撞系统调用，或由 Update 内自动检测
        /// </summary>
        public void ApplyCollision(TreeCollisionHit hit, ref float nextX, ref float nextZ)
        {
            bool isFenceHit = hit.Kind == ColliderKind.Fence;
            float resistance = isFenceHit ? 0.36f : 1f;

            // 位置修正（与 TS: nextX += nx * (penetration * (fence?0.2:0.45) + 0.08*resistance)）
            nextX += hit.Nx * (hit.Penetration * (isFenceHit ? 0.2f : 0.45f) + 0.08f * resistance);
            nextZ += hit.Nz * (hit.Penetration * (isFenceHit ? 0.2f : 0.45f) + 0.08f * resistance);

            // 碰撞方向分析
            Vector3 impactPush = new Vector3(hit.Nx, 0f, hit.Nz);
            Vector3 impactForward = new Vector3(Mathf.Sin(_heading), 0f, Mathf.Cos(_heading)).normalized;
            Vector3 impactSide = new Vector3(Mathf.Cos(_heading), 0f, -Mathf.Sin(_heading)).normalized;

            float headOn = Mathf.Abs(Vector3.Dot(impactForward, impactPush));
            float sideHit = Vector3.Dot(impactSide, impactPush);

            // 速度修正（与 TS: velocity *= fence?(0.58-headOn*0.18):-(0.12+headOn*0.18)）
            _velocity *= isFenceHit ? (0.58f - headOn * 0.18f) : -(0.12f + headOn * 0.18f);
            _velocity += Mathf.Sign(_velocity != 0f ? _velocity : 1f) * 0.8f * (1f - headOn) * hit.Power * resistance;

            // 转向修正（与 TS: steerAngle += clamp(sideHit*power*0.68*resistance, -0.5, 0.5)）
            _steerAngle += Mathf.Clamp(sideHit * hit.Power * 0.68f * resistance, -0.5f, 0.5f);
            _heading += Mathf.Clamp(sideHit * hit.Power * 0.14f * resistance, -0.17f, 0.17f);

            // 碰撞视觉
            _impactDuration = ImpactDurationBase + hit.Power * ImpactDurationPowerScale * resistance;
            _impactTimer = _impactDuration;
            _impactPower = hit.Power;
            _impactSideSign = Mathf.Sign(sideHit != 0f ? sideHit : 1f);

            // 碰撞方向分类
            if (isFenceHit)
            {
                _lastImpactDirection = ImpactDirection.Low;
            }
            else if (headOn > 0.6f)
            {
                _lastImpactDirection = ImpactDirection.Front;
            }
            else
            {
                _lastImpactDirection = ImpactDirection.Side;
            }

            _lastImpactPower = hit.Power;
            _impactVisual = Mathf.Max(_impactVisual, hit.Power);
            _impactPhase = Random.value * Mathf.PI * 2f;
        }

        /// <summary>
        /// 应用障碍物碰撞结果
        /// 由外部碰撞系统调用
        /// </summary>
        public void ApplyObstacleResult(ObstacleResult ob, ref float nextX, ref float nextZ)
        {
            nextX += ob.PushX;
            nextZ += ob.PushZ;
            _velocity *= ob.VelocityMul;
        }

        // =================================================================
        // 损耗系统更新
        // =================================================================

        private void UpdateDamageSystem(float delta, float nextX, float nextZ, float currentMaxSpeed, bool boost)
        {
            if (_damageSystem == null) return;

            // 判断是否夜间
            bool isNightTime = GameManager.Instance != null
                ? GameManager.Instance.TimeOfDay > 17.5f || GameManager.Instance.TimeOfDay < 6f
                : false;

            // 判断是否在已清理的路面上
            float roadCenterX = GetRoadCenter(nextZ);
            float distFromRoad = Mathf.Abs(nextX - roadCenterX);
            bool isOnClearedRoad = distFromRoad < 5f;

            // 碰撞冲击力
            float frameImpactPower = CurrentImpactPower;
            ImpactDirection? frameImpactDir = CurrentImpactDirection;

            // tick 累加器
            _damageSystem.AccumulatorTick(
                delta,
                _velocity,
                _steerAngle,
                isOnClearedRoad,
                frameImpactPower,
                frameImpactDir,
                isNightTime,
                boost,
                _damageSystem.TankCondition,
                currentMaxSpeed
            );

            // flush 并应用
            var damageResult = _damageSystem.AccumulatorFlush();
            if (damageResult != null)
            {
                // 扣油
                float totalFuelUsed = damageResult.Value.FuelUsed + damageResult.Value.TankLeak;
                if (totalFuelUsed > 0f && GameManager.Instance != null)
                {
                    GameManager.Instance.SpendResources(new ResourceBag { Fuel = Mathf.Min((int)totalFuelUsed, GameManager.Instance.Resources.Fuel) });
                }

                // 扣部件 condition
                _damageSystem.ApplyDamageResult(damageResult.Value);
            }
        }

        // =================================================================
        // 相机系统
        // =================================================================

        private void UpdateCamera(float delta)
        {
            if (FollowCamera == null) return;

            Vector3 lookAtTarget = transform.position;
            lookAtTarget.y += 0.5f;

            // 平滑相机目标
            if (Vector3.Distance(_smoothedCamTarget, lookAtTarget) > 100f)
            {
                _smoothedCamTarget = lookAtTarget;
            }
            else
            {
                _smoothedCamTarget = Vector3.Lerp(_smoothedCamTarget, lookAtTarget, delta * 10f);
            }

            Vector3 targetPos = _smoothedCamTarget;

            // 相机偏移
            float camHeight = 18f;
            float camDist = 14f;
            if (GameManager.Instance != null)
            {
                camHeight = GameManager.Instance.CameraHeight;
                camDist = GameManager.Instance.CameraDistance;
            }

            Vector3 idealOffset = new Vector3(3f, camHeight + 1.5f, -camDist + 4f);
            idealOffset = Quaternion.AngleAxis(_heading * Mathf.Rad2Deg, Vector3.up) * idealOffset;

            Vector3 idealPos = targetPos + idealOffset;

            // 碰撞震动
            float time = Time.time;
            if (_impactTimer > 0f)
            {
                idealPos.x += Mathf.Sin(time * 38f + _impactPhase) * _impactVisual * 0.42f;
                idealPos.y += Mathf.Cos(time * 31f + _impactPhase) * _impactVisual * 0.24f;
            }

            FollowCamera.transform.position = Vector3.Lerp(FollowCamera.transform.position, idealPos, delta * 8f);
            FollowCamera.transform.LookAt(targetPos);
        }

        // =================================================================
        // 工具方法
        // =================================================================

        /// <summary>
        /// 获取道路中心X坐标
        /// </summary>
        private float GetRoadCenter(float z)
        {
            if (TerrainHeight.RoadSampler != null)
            {
                var sample = TerrainHeight.RoadSampler.Sample(z);
                return sample.CenterX;
            }
            return 0f;
        }

        /// <summary>
        /// 重置车辆到指定位置（用于传送等）
        /// </summary>
        public void TeleportTo(float x, float z)
        {
            float y = TerrainHeight.GetTerrainHeight(x, z) + TerrainYOffset;
            transform.position = new Vector3(x, y, z);
            _prevPos = new Vector3(x, y, z);
            _smoothedCamTarget = new Vector3(x, y + 0.5f, z);
            _velocity = 0f;
            _heading = Mathf.PI;
            transform.rotation = Quaternion.AngleAxis(_heading * Mathf.Rad2Deg, Vector3.up);

            if (FollowCamera != null)
            {
                FollowCamera.transform.position = new Vector3(x, y + 18f, z + 80f);
            }
        }

        /// <summary>
        /// 重置碰撞视觉状态
        /// </summary>
        public void ResetImpactVisual()
        {
            _impactTimer = 0f;
            _impactPower = 0f;
            _impactVisual = 0f;
            _impactSideSign = 0f;
        }
    }

    // =====================================================================
    // 树碰撞系统占位（需要实际实现）
    // =====================================================================

    /// <summary>
    /// 树碰撞检测系统占位接口
    /// 实际实现需在 Collision 目录下完成
    /// </summary>
    public class TreeCollisionSystem : MonoBehaviour
    {
        /// <summary>
        /// 检测碰撞，返回 null 表示无碰撞
        /// </summary>
        public virtual TreeCollisionHit? CheckCollision(float x, float z, float velocity, float time)
        {
            return null;
        }
    }
}

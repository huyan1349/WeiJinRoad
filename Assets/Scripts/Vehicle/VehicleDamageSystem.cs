using UnityEngine;
using WeiJinRoad.Core;

namespace WeiJinRoad.Vehicle
{
    // =====================================================================
    // 性能修正结构体
    // =====================================================================

    /// <summary>
    /// 性能修正参数
    /// 翻译自 TypeScript 版 vehicleDamage.ts PerformanceModifiers
    /// </summary>
    public struct PerformanceModifiers
    {
        /// <summary>最大速度因子 (0~1), engine condition 1→0 对应 100%→40%</summary>
        public float MaxSpeedFactor;

        /// <summary>转向速度因子 (0~1), tires condition 1→0 对应 100%→30%</summary>
        public float TurnSpeedFactor;

        /// <summary>探照灯强度因子 (0~1), headlight condition 1→0 对应 100%→15%</summary>
        public float SearchlightFactor;

        /// <summary>探照灯闪烁强度 (0~1)</summary>
        public float SearchlightFlicker;

        /// <summary>是否油尽</summary>
        public bool OutOfFuel;

        /// <summary>油耗倍率（油箱受损时增加）</summary>
        public float FuelDrainMult;

        /// <summary>油箱是否在漏油</summary>
        public bool TankLeaking;

        /// <summary>电台信号断续程度 (0~1)</summary>
        public float RadioSignalBreak;

        /// <summary>轮胎滑动概率 (0~1)</summary>
        public float TireSlipProb;

        /// <summary>默认值（全部正常）</summary>
        public static PerformanceModifiers Default => new PerformanceModifiers
        {
            MaxSpeedFactor = 1f,
            TurnSpeedFactor = 1f,
            SearchlightFactor = 1f,
            SearchlightFlicker = 0f,
            OutOfFuel = false,
            FuelDrainMult = 1f,
            TankLeaking = false,
            RadioSignalBreak = 0f,
            TireSlipProb = 0f,
        };
    }

    // =====================================================================
    // 损耗结果结构体
    // =====================================================================

    /// <summary>
    /// 损耗系统 flush 结果
    /// 翻译自 TypeScript 版 vehicleDamage.ts DamageResult
    /// </summary>
    public struct DamageResult
    {
        /// <summary>消耗的燃油量</summary>
        public float FuelUsed;

        /// <summary>发动机损耗</summary>
        public float EngineDamage;

        /// <summary>轮胎损耗</summary>
        public float TiresDamage;

        /// <summary>探照灯损耗</summary>
        public float HeadlightDamage;

        /// <summary>油箱损耗</summary>
        public float TankDamage;

        /// <summary>车身损耗</summary>
        public float BodyDamage;

        /// <summary>电台损耗</summary>
        public float RadioDamage;

        /// <summary>油箱漏油量</summary>
        public float TankLeak;
    }

    // =====================================================================
    // 部件ID枚举
    // =====================================================================

    /// <summary>
    /// 车辆部件ID
    /// </summary>
    public enum PartId
    {
        Engine,
        Tires,
        Headlight,
        Tank,
        Body,
        Radio
    }

    // =====================================================================
    // 车辆损耗系统
    // =====================================================================

    /// <summary>
    /// 车辆损耗 + 油耗系统
    /// 翻译自 TypeScript 版 vehicleDamage.ts
    ///
    /// 核心机制：
    /// - VehicleDamageAccumulator 累加器模式：每帧 tick 累加，到时间后 flush 返回结果
    /// - 油耗曲线：怠速极低消耗，中速线性，高速指数增长
    /// - 部件损耗曲线：使用 smoothstep 而非线性，高速时急剧增加
    /// - 碰撞损耗：根据碰撞方向区分（正面→发动机+车身，侧面→轮胎+车身，低矮物→探照灯）
    /// - 油箱损耗：碰撞时油箱可能受损→漏油加速
    /// - 电台损耗：时间老化
    /// - 性能修正使用连续曲线而非阈值跳变
    ///
    /// 所有常量与原版 TypeScript 代码完全一致
    /// </summary>
    public class VehicleDamageSystem : MonoBehaviour
    {
        // =================================================================
        // 常量 - 与 TypeScript 版本完全一致
        // =================================================================

        // ── 油耗常量 ──

        /// <summary>怠速油耗 fuel/s</summary>
        private const float FuelIdle = 0.002f;

        /// <summary>中速线性系数</summary>
        private const float FuelLinearCoeff = 0.0012f;

        /// <summary>速度占比 > 70% 开始指数</summary>
        private const float FuelExpThreshold = 0.7f;

        /// <summary>高速指数增长系数</summary>
        private const float FuelExpRate = 0.008f;

        /// <summary>路况差油耗倍率</summary>
        private const float FuelOffroadMult = 1.5f;

        /// <summary>加速油耗倍率</summary>
        private const float FuelBoostMult = 1.8f;

        /// <summary>油箱 condition &lt; 0.2 漏油</summary>
        private const float FuelTankLeakThreshold = 0.2f;

        /// <summary>油箱受损额外油耗/s (condition &lt; 0.5)</summary>
        private const float FuelTankDamageMult = 0.003f;

        /// <summary>油箱漏油率/s (condition &lt; 0.2)</summary>
        private const float FuelTankLeakRate = 0.008f;

        // ── 发动机损耗常量 ──

        /// <summary>基础损耗 condition/s</summary>
        private const float EngineWearBase = 0.000020f;

        /// <summary>速度对损耗的线性系数</summary>
        private const float EngineWearSpeedScale = 0.08f;

        /// <summary>速度占比 > 60% 开始 smoothstep 急增</summary>
        private const float EngineWearHighSpeedStart = 0.6f;

        /// <summary>高速额外损耗峰值</summary>
        private const float EngineWearHighSpeedRate = 0.00012f;

        /// <summary>低温额外损耗</summary>
        private const float EngineWearCold = 0.000025f;

        /// <summary>路况差额外倍率</summary>
        private const float EngineWearOffroadMult = 1.3f;

        // ── 轮胎损耗常量 ──

        /// <summary>基础损耗</summary>
        private const float TiresWearBase = 0.000025f;

        /// <summary>速度系数</summary>
        private const float TiresWearSpeedScale = 0.06f;

        /// <summary>路况差额外</summary>
        private const float TiresWearOffroad = 0.00005f;

        /// <summary>转向额外</summary>
        private const float TiresWearSteer = 0.000035f;

        /// <summary>高速急增起点</summary>
        private const float TiresWearHighSpeedStart = 0.65f;

        /// <summary>高速急增峰值</summary>
        private const float TiresWearHighSpeedRate = 0.00008f;

        // ── 车身碰撞损耗常量 ──

        /// <summary>正面碰撞 per impact power</summary>
        private const float BodyWearFrontImpact = 0.035f;

        /// <summary>侧面碰撞</summary>
        private const float BodyWearSideImpact = 0.025f;

        /// <summary>低矮物碰撞</summary>
        private const float BodyWearLowImpact = 0.015f;

        // ── 探照灯碰撞损耗常量 ──

        /// <summary>低矮物碰撞（树枝等）</summary>
        private const float HeadlightWearLowImpact = 0.04f;

        /// <summary>正面碰撞</summary>
        private const float HeadlightWearFrontImpact = 0.02f;

        // ── 油箱碰撞损耗常量 ──

        /// <summary>正面碰撞</summary>
        private const float TankWearFrontImpact = 0.03f;

        /// <summary>侧面碰撞</summary>
        private const float TankWearSideImpact = 0.025f;

        // ── 电台老化损耗常量 ──

        /// <summary>基础时间老化</summary>
        private const float RadioWearBase = 0.000008f;

        // ── 碰撞额外损耗 ──

        /// <summary>正面碰撞发动机额外损耗</summary>
        private const float EngineWearFrontImpact = 0.02f;

        /// <summary>侧面碰撞轮胎额外损耗</summary>
        private const float TiresWearSideImpact = 0.015f;

        // =================================================================
        // Inspector 可配置 - 部件状态
        // =================================================================

        [Header("Vehicle Parts - Condition (0~1)")]
        [Range(0f, 1f)]
        [Tooltip("发动机状态")]
        public float EngineCondition = 1f;

        [Range(0f, 1f)]
        [Tooltip("轮胎状态")]
        public float TiresCondition = 1f;

        [Range(0f, 1f)]
        [Tooltip("探照灯状态")]
        public float HeadlightCondition = 1f;

        [Range(0f, 1f)]
        [Tooltip("油箱状态")]
        public float TankCondition = 1f;

        [Range(0f, 1f)]
        [Tooltip("车身状态")]
        public float BodyCondition = 1f;

        [Range(0f, 1f)]
        [Tooltip("电台状态")]
        public float RadioCondition = 1f;

        [Header("Resources")]
        [Tooltip("当前燃油量")]
        public float Fuel = 100f;

        [Tooltip("最大携带量")]
        public float MaxCarry = 100f;

        // =================================================================
        // 累加器状态
        // =================================================================

        private float _fuelAccum;
        private float _engineAccum;
        private float _tiresAccum;
        private float _headlightAccum;
        private float _tankAccum;
        private float _bodyAccum;
        private float _radioAccum;
        private float _tankLeakAccum;
        private float _timer;

        /// <summary>flush 间隔 (s)</summary>
        private const float FlushInterval = 0.1f;

        // =================================================================
        // 公开属性
        // =================================================================

        /// <summary>油箱状态（供 VehicleController 读取）</summary>
        public float TankConditionValue => TankCondition;

        // =================================================================
        // 累加器 Tick
        // =================================================================

        /// <summary>
        /// 每帧调用，累加损耗
        /// 翻译自 VehicleDamageAccumulator.tick()
        /// </summary>
        public void AccumulatorTick(
            float delta,
            float speed,
            float steerRate,
            bool isOnClearedRoad,
            float impactPower,
            ImpactDirection? impactDirection,
            bool isNight,
            bool isBoosting,
            float tankCondition,
            float maxSpeed)
        {
            float absSpeed = Mathf.Abs(speed);
            float speedRatio = maxSpeed > 0f ? absSpeed / maxSpeed : 0f;

            // ── Fuel 消耗 ──
            // 怠速极低，中速线性，高速指数
            float fuelRate = FuelIdle;
            fuelRate += absSpeed * FuelLinearCoeff;
            if (speedRatio > FuelExpThreshold)
            {
                float expT = (speedRatio - FuelExpThreshold) / (1f - FuelExpThreshold);
                fuelRate += FuelExpRate * Mathf.Exp(expT * 2.5f);
            }
            if (!isOnClearedRoad) fuelRate *= FuelOffroadMult;
            if (isBoosting) fuelRate *= FuelBoostMult;

            // 油箱受损增加油耗
            if (tankCondition < 0.5f)
            {
                float damageT = 1f - tankCondition / 0.5f; // 0→1
                fuelRate *= 1f + damageT * 0.8f; // 最多 1.8x
            }

            _fuelAccum += fuelRate * delta;

            // 油箱漏油
            if (tankCondition < FuelTankLeakThreshold)
            {
                float leakT = 1f - tankCondition / FuelTankLeakThreshold;
                _tankLeakAccum += FuelTankLeakRate * leakT * delta;
            }

            // ── Engine 损耗 ──
            float engineWear = EngineWearBase * (1f + absSpeed * EngineWearSpeedScale);
            // 高速 smoothstep 急增
            if (speedRatio > EngineWearHighSpeedStart)
            {
                float t = (speedRatio - EngineWearHighSpeedStart) / (1f - EngineWearHighSpeedStart);
                engineWear += EngineWearHighSpeedRate * Smoothstep(t);
            }
            if (isNight) engineWear += EngineWearCold;
            if (!isOnClearedRoad) engineWear *= EngineWearOffroadMult;
            _engineAccum += engineWear * delta;

            // ── Tires 损耗 ──
            float tiresWear = TiresWearBase * (1f + absSpeed * TiresWearSpeedScale);
            if (!isOnClearedRoad) tiresWear += TiresWearOffroad;
            tiresWear += Mathf.Abs(steerRate) * TiresWearSteer * (1f + absSpeed * 0.1f);
            // 高速 smoothstep 急增
            if (speedRatio > TiresWearHighSpeedStart)
            {
                float t = (speedRatio - TiresWearHighSpeedStart) / (1f - TiresWearHighSpeedStart);
                tiresWear += TiresWearHighSpeedRate * Smoothstep(t);
            }
            _tiresAccum += tiresWear * delta;

            // ── Headlight 损耗 ──
            // 仅碰撞时损耗，低矮物碰撞为主
            if (impactPower > 0f && impactDirection.HasValue)
            {
                if (impactDirection.Value == ImpactDirection.Low)
                {
                    _headlightAccum += HeadlightWearLowImpact * impactPower;
                }
                else if (impactDirection.Value == ImpactDirection.Front)
                {
                    _headlightAccum += HeadlightWearFrontImpact * impactPower;
                }
            }

            // ── Body 碰撞损耗 ──
            if (impactPower > 0f && impactDirection.HasValue)
            {
                if (impactDirection.Value == ImpactDirection.Front)
                {
                    _bodyAccum += BodyWearFrontImpact * impactPower;
                }
                else if (impactDirection.Value == ImpactDirection.Side)
                {
                    _bodyAccum += BodyWearSideImpact * impactPower;
                }
                else
                {
                    _bodyAccum += BodyWearLowImpact * impactPower;
                }
            }

            // ── Tank 碰撞损耗 ──
            if (impactPower > 0f && impactDirection.HasValue)
            {
                if (impactDirection.Value == ImpactDirection.Front)
                {
                    _tankAccum += TankWearFrontImpact * impactPower;
                }
                else if (impactDirection.Value == ImpactDirection.Side)
                {
                    _tankAccum += TankWearSideImpact * impactPower;
                }
            }

            // ── Engine 碰撞损耗（正面碰撞）──
            if (impactPower > 0f && impactDirection.HasValue && impactDirection.Value == ImpactDirection.Front)
            {
                _engineAccum += EngineWearFrontImpact * impactPower;
            }

            // ── Tires 碰撞损耗（侧面碰撞）──
            if (impactPower > 0f && impactDirection.HasValue && impactDirection.Value == ImpactDirection.Side)
            {
                _tiresAccum += TiresWearSideImpact * impactPower;
            }

            // ── Radio 时间老化 ──
            _radioAccum += RadioWearBase * delta;

            // ── Timer ──
            _timer += delta;
        }

        // =================================================================
        // 累加器 Flush
        // =================================================================

        /// <summary>
        /// 每 0.1 秒 flush 一次，返回损耗结果
        /// 翻译自 VehicleDamageAccumulator.flush()
        /// </summary>
        /// <returns>损耗结果，若未到间隔则返回 null</returns>
        public DamageResult? AccumulatorFlush()
        {
            if (_timer < FlushInterval) return null;

            var result = new DamageResult
            {
                FuelUsed = _fuelAccum,
                EngineDamage = _engineAccum,
                TiresDamage = _tiresAccum,
                HeadlightDamage = _headlightAccum,
                TankDamage = _tankAccum,
                BodyDamage = _bodyAccum,
                RadioDamage = _radioAccum,
                TankLeak = _tankLeakAccum,
            };

            // 重置累加器
            _fuelAccum = 0f;
            _engineAccum = 0f;
            _tiresAccum = 0f;
            _headlightAccum = 0f;
            _tankAccum = 0f;
            _bodyAccum = 0f;
            _radioAccum = 0f;
            _tankLeakAccum = 0f;
            _timer = 0f;

            return result;
        }

        // =================================================================
        // 应用损耗结果
        // =================================================================

        /// <summary>
        /// 将 flush 结果应用到部件 condition
        /// </summary>
        public void ApplyDamageResult(DamageResult result)
        {
            EngineCondition = Mathf.Max(0f, EngineCondition - result.EngineDamage);
            TiresCondition = Mathf.Max(0f, TiresCondition - result.TiresDamage);
            HeadlightCondition = Mathf.Max(0f, HeadlightCondition - result.HeadlightDamage);
            TankCondition = Mathf.Max(0f, TankCondition - result.TankDamage);
            BodyCondition = Mathf.Max(0f, BodyCondition - result.BodyDamage);
            RadioCondition = Mathf.Max(0f, RadioCondition - result.RadioDamage);
        }

        // =================================================================
        // 性能修正计算
        // =================================================================

        /// <summary>
        /// 计算性能修正参数
        /// 翻译自 getPerformanceModifiers()
        /// 使用连续曲线（smoothstep）而非阈值跳变
        /// </summary>
        public PerformanceModifiers GetPerformanceModifiers()
        {
            var mods = new PerformanceModifiers();

            // 发动机：condition 1.0→0.0 对应 maxSpeed 100%→40%
            // 使用 smoothstep 曲线，低 condition 时急剧下降
            mods.MaxSpeedFactor = 0.4f + 0.6f * Smoothstep(EngineCondition);

            // 轮胎：condition 1.0→0.0 对应 turnSpeed 100%→30%
            mods.TurnSpeedFactor = 0.3f + 0.7f * Smoothstep(TiresCondition);

            // 轮胎滑动概率：condition < 0.5 开始增加
            mods.TireSlipProb = TiresCondition < 0.5f
                ? Smoothstep(1f - TiresCondition / 0.5f) * 0.3f
                : 0f;

            // 探照灯：condition 1.0→0.0 对应 intensity 100%→15%
            mods.SearchlightFactor = 0.15f + 0.85f * Smoothstep(HeadlightCondition);

            // 探照灯闪烁：condition < 0.3 时增加
            mods.SearchlightFlicker = HeadlightCondition < 0.3f
                ? Smoothstep(1f - HeadlightCondition / 0.3f) * 0.6f
                : 0f;

            // 油箱：condition < 0.5 时油耗增加
            mods.FuelDrainMult = TankCondition < 0.5f
                ? 1f + (1f - TankCondition / 0.5f) * 0.8f
                : 1f;

            // 油箱漏油
            mods.TankLeaking = TankCondition < FuelTankLeakThreshold;

            // 电台：condition < 0.3 时信号断续
            mods.RadioSignalBreak = RadioCondition < 0.3f
                ? Smoothstep(1f - RadioCondition / 0.3f)
                : 0f;

            // 油尽
            mods.OutOfFuel = Fuel <= 0f;

            return mods;
        }

        // =================================================================
        // 修理逻辑
        // =================================================================

        /// <summary>
        /// 修理指定部件
        /// </summary>
        /// <param name="partId">部件ID</param>
        /// <param name="amount">修理量（正值增加 condition）</param>
        public void RepairPart(PartId partId, float amount)
        {
            switch (partId)
            {
                case PartId.Engine:
                    EngineCondition = Mathf.Min(1f, EngineCondition + amount);
                    break;
                case PartId.Tires:
                    TiresCondition = Mathf.Min(1f, TiresCondition + amount);
                    break;
                case PartId.Headlight:
                    HeadlightCondition = Mathf.Min(1f, HeadlightCondition + amount);
                    break;
                case PartId.Tank:
                    TankCondition = Mathf.Min(1f, TankCondition + amount);
                    break;
                case PartId.Body:
                    BodyCondition = Mathf.Min(1f, BodyCondition + amount);
                    break;
                case PartId.Radio:
                    RadioCondition = Mathf.Min(1f, RadioCondition + amount);
                    break;
            }
        }

        /// <summary>
        /// 修理所有部件到满状态
        /// </summary>
        public void RepairAll()
        {
            EngineCondition = 1f;
            TiresCondition = 1f;
            HeadlightCondition = 1f;
            TankCondition = 1f;
            BodyCondition = 1f;
            RadioCondition = 1f;
        }

        /// <summary>
        /// 加油
        /// </summary>
        /// <param name="amount">加油量</param>
        public void Refuel(float amount)
        {
            Fuel = Mathf.Min(MaxCarry, Fuel + amount);
        }

        /// <summary>
        /// 消耗燃油
        /// </summary>
        /// <param name="amount">消耗量</param>
        public void SpendFuel(float amount)
        {
            Fuel = Mathf.Max(0f, Fuel - amount);
        }

        // =================================================================
        // 工具函数
        // =================================================================

        /// <summary>
        /// smoothstep: 3t² - 2t³，用于渐进曲线
        /// 翻译自 TypeScript 版 smoothstep()
        /// </summary>
        public static float Smoothstep(float t)
        {
            float c = Mathf.Max(0f, Mathf.Min(1f, t));
            return c * c * (3f - 2f * c);
        }

        /// <summary>
        /// 线性映射 clamp
        /// 翻译自 TypeScript 版 remap()
        /// </summary>
        public static float Remap(float value, float inMin, float inMax, float outMin, float outMax)
        {
            float t = Mathf.Max(0f, Mathf.Min(1f, (value - inMin) / (inMax - inMin)));
            return outMin + t * (outMax - outMin);
        }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-67fdc3b7f53245f7b8d25055b456b3ce/cwd.txt'; exit "$__tr_native_ec"
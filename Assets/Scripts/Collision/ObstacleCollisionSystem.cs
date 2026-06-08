// =============================================================================
// ObstacleCollisionSystem.cs — 路面障碍碰撞 + 清除系统
// 翻译自 TypeScript 版 obstacleCollision.ts
//
// 核心机制：
// - 障碍类型：雪堆（非实心）、冰块、倒木、落石（实心）
// - 清除逻辑：铲雪犁（对雪/冰高效）或绞盘（对倒木/落石高效）
// - 碰撞响应：实心障碍推开车身+减速，雪堆仅减速
// - 障碍状态追踪：已清除障碍保存到 GameManager.ClearedObstacles
// - 交互提示：靠近障碍时显示清除方式提示
//
// 物理只返回"位移增量 + 速度系数"，由 VehicleController 应用
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using WeiJinRoad.Core;
using WeiJinRoad.Data;
using WeiJinRoad.Vehicle;

namespace WeiJinRoad.Collision
{
    // =========================================================================
    // 数据结构
    // =========================================================================

    /// <summary>
    /// 障碍物运行时碰撞体
    /// 对应 TypeScript 版 ObstacleBody
    /// </summary>
    [Serializable]
    public class ObstacleBody
    {
        /// <summary>障碍ID</summary>
        public string Id;
        /// <summary>障碍类型</summary>
        public ObstacleKind Kind;
        /// <summary>世界坐标X</summary>
        public float X;
        /// <summary>世界坐标Y</summary>
        public float Y;
        /// <summary>世界坐标Z</summary>
        public float Z;
        /// <summary>Y轴旋转（弧度）</summary>
        public float RotationY;
        /// <summary>缩放</summary>
        public float Scale;
        /// <summary>最大生命值</summary>
        public float HpMax;
        /// <summary>当前生命值</summary>
        public float Hp;
        /// <summary>碰撞半径</summary>
        public float Radius;
        /// <summary>是否实心阻挡</summary>
        public bool Blocking;
        /// <summary>是否已清除</summary>
        public bool Cleared;
        /// <summary>上次碰撞时间</summary>
        public float LastHit = -999f;
    }

    /// <summary>
    /// 障碍碰撞解算结果
    /// 对应 TypeScript 版 ObstacleResolve
    /// </summary>
    public struct ObstacleResolve
    {
        /// <summary>推力位移X</summary>
        public float PushX;
        /// <summary>推力位移Z</summary>
        public float PushZ;
        /// <summary>速度乘数</summary>
        public float VelocityMul;
        /// <summary>被清除的障碍物信息（null表示未清除）</summary>
        public ObstacleClearedInfo? Cleared;
    }

    /// <summary>
    /// 被清除的障碍物信息
    /// 对应 TypeScript 版 ObstacleResolve.cleared
    /// </summary>
    [Serializable]
    public struct ObstacleClearedInfo
    {
        /// <summary>障碍ID</summary>
        public string Id;
        /// <summary>障碍类型</summary>
        public ObstacleKind Kind;
        /// <summary>世界坐标X</summary>
        public float X;
        /// <summary>世界坐标Z</summary>
        public float Z;
        /// <summary>缩放</summary>
        public float Scale;
    }

    /// <summary>
    /// 障碍交互提示数据
    /// </summary>
    public struct ObstaclePrompt
    {
        /// <summary>障碍ID</summary>
        public string Id;
        /// <summary>障碍类型</summary>
        public ObstacleKind Kind;
        /// <summary>距离</summary>
        public float Distance;
        /// <summary>提示文本</summary>
        public string Hint;
        /// <summary>推荐附件</summary>
        public FrontAttachment RecommendedAttachment;
    }

    // =========================================================================
    // 障碍碰撞系统 — 主类
    // =========================================================================

    /// <summary>
    /// 路面障碍碰撞 + 清除系统
    ///
    /// 使用空间网格加速结构，与 TreeCollisionSystem 类似。
    /// 碰撞结果通过 ObstacleResult 返回给 VehicleController.ApplyObstacleResult()。
    ///
    /// 挂载方式：附加到与 VehicleController 相同的 GameObject 上，
    /// 由 VehicleController.Update() 调用 ResolveObstacles()。
    /// </summary>
    public class ObstacleCollisionSystem : MonoBehaviour
    {
        // =================================================================
        // 常量 — 与 TypeScript 版本完全一致
        // =================================================================

        /// <summary>空间网格单元大小</summary>
        private const int CellSize = 12;

        /// <summary>车辆碰撞半径</summary>
        private const float CarRadius = 1.0f;

        // ── 伤害倍率常量 ──

        /// <summary>实心障碍 + 绞盘倍率</summary>
        private const float DmgBlockingWinch = 3.0f;
        /// <summary>实心障碍 + 铲雪犁倍率</summary>
        private const float DmgBlockingPlow = 1.5f;
        /// <summary>实心障碍 + 无附件倍率</summary>
        private const float DmgBlockingNone = 0.8f;

        /// <summary>非实心障碍 + 铲雪犁倍率</summary>
        private const float DmgNonBlockingPlow = 3.2f;
        /// <summary>非实心障碍 + 绞盘倍率</summary>
        private const float DmgNonBlockingWinch = 1.0f;
        /// <summary>非实心障碍 + 无附件倍率</summary>
        private const float DmgNonBlockingNone = 1.1f;

        // ── 实心障碍碰撞参数 ──

        /// <summary>实心障碍碰撞冷却时间（秒）</summary>
        private const float BlockingHitCooldown = 0.35f;

        /// <summary>实心障碍碰撞最低速度</summary>
        private const float BlockingMinSpeed = 1.5f;

        /// <summary>实心障碍碰撞力度基础映射下限速度</summary>
        private const float BlockingPowerSpeedBase = 1.0f;

        /// <summary>实心障碍碰撞力度映射范围</summary>
        private const float BlockingPowerSpeedRange = 6f;

        /// <summary>实心障碍碰撞力度最小值</summary>
        private const float BlockingPowerMin = 0.12f;

        /// <summary>实心障碍碰撞力度最大值</summary>
        private const float BlockingPowerMax = 1.0f;

        // ── 雪堆碾压参数 ──

        /// <summary>雪堆碾压基础伤害/s</summary>
        private const float SnowBaseDmg = 0.6f;

        /// <summary>雪堆碾压速度系数</summary>
        private const float SnowSpeedCoeff = 0.25f;

        // ── 推力参数 ──

        /// <summary>实心障碍推力系数</summary>
        private const float BlockingPushK = 0.5f;

        /// <summary>实心障碍推力最小值</summary>
        private const float BlockingPushMin = 0.05f;

        /// <summary>实心障碍高速反弹速度系数</summary>
        private const float BlockingHighSpeedBounce = -0.12f;

        /// <summary>实心障碍高速判定速度</summary>
        private const float BlockingHighSpeedThreshold = 3f;

        // ── 雪堆减速参数 ──

        /// <summary>雪堆减速系数（无铲）</summary>
        private const float SnowDragNoPlow = 2.4f;

        /// <summary>雪堆减速系数（有铲）</summary>
        private const float SnowDragPlow = 0.6f;

        /// <summary>雪堆最低速度乘数</summary>
        private const float SnowMinVelocityMul = 0.2f;

        // ── 清除后速度系数 ──

        /// <summary>实心障碍清除后速度系数</summary>
        private const float ClearedBlockingVelocityMul = 0.6f;

        /// <summary>非实心障碍清除后速度系数</summary>
        private const float ClearedNonBlockingVelocityMul = 0.94f;

        // ── 交互提示参数 ──

        /// <summary>交互提示检测范围</summary>
        private const float PromptRange = 8f;

        // =================================================================
        // 数据存储
        // =================================================================

        /// <summary>所有障碍碰撞体</summary>
        private readonly List<ObstacleBody> _bodies = new List<ObstacleBody>();

        /// <summary>空间网格</summary>
        private readonly Dictionary<string, List<ObstacleBody>> _grid = new Dictionary<string, List<ObstacleBody>>();

        /// <summary>是否已构建</summary>
        private bool _built;

        /// <summary>当前交互提示</summary>
        private ObstaclePrompt? _currentPrompt;

        // =================================================================
        // 公开属性
        // =================================================================

        /// <summary>所有障碍碰撞体（只读）</summary>
        public IReadOnlyList<ObstacleBody> AllBodies => _bodies.AsReadOnly();

        /// <summary>当前交互提示</summary>
        public ObstaclePrompt? CurrentPrompt => _currentPrompt;

        // =================================================================
        // 空间网格
        // =================================================================

        /// <summary>
        /// 计算空间网格键
        /// </summary>
        private static string GridKey(float x, float z)
        {
            int cx = Mathf.FloorToInt(x / CellSize);
            int cz = Mathf.FloorToInt(z / CellSize);
            return $"{cx},{cz}";
        }

        /// <summary>
        /// 重建空间网格
        /// </summary>
        private void BuildGrid()
        {
            _grid.Clear();
            for (int i = 0; i < _bodies.Count; i++)
            {
                var body = _bodies[i];
                string key = GridKey(body.X, body.Z);
                if (_grid.TryGetValue(key, out var cell))
                {
                    cell.Add(body);
                }
                else
                {
                    _grid[key] = new List<ObstacleBody> { body };
                }
            }
        }

        // =================================================================
        // 障碍体初始化
        // =================================================================

        /// <summary>
        /// 确保障碍碰撞体已初始化
        /// 对应 TypeScript 版 ensureObstacleBodies()
        /// </summary>
        public void EnsureObstacleBodies()
        {
            if (_built) return;

            var defs = GenerateObstacles();
            _bodies.Clear();
            for (int i = 0; i < defs.Count; i++)
            {
                var def = defs[i];
                _bodies.Add(new ObstacleBody
                {
                    Id = def.id,
                    Kind = def.kind,
                    X = def.position.x,
                    Y = def.position.y,
                    Z = def.position.z,
                    RotationY = def.rotationY,
                    Scale = def.scale,
                    HpMax = def.hp,
                    Hp = def.hp,
                    Radius = def.radius,
                    Blocking = def.blocking,
                    Cleared = false,
                    LastHit = -999f,
                });
            }

            BuildGrid();
            _built = true;
        }

        /// <summary>
        /// 重置所有障碍为未清除、满血（用于新游戏）
        /// 对应 TypeScript 版 resetObstacleRuntime()
        /// </summary>
        public void ResetObstacleRuntime()
        {
            _built = false;
            _bodies.Clear();
            _grid.Clear();
            EnsureObstacleBodies();
        }

        /// <summary>
        /// 将持久化的已清除ID应用到运行时
        /// 对应 TypeScript 版 syncClearedObstacles()
        /// </summary>
        /// <param name="clearedIds">已清除的障碍ID列表</param>
        public void SyncClearedObstacles(List<string> clearedIds)
        {
            EnsureObstacleBodies();
            var set = new HashSet<string>(clearedIds);
            for (int i = 0; i < _bodies.Count; i++)
            {
                if (set.Contains(_bodies[i].Id))
                {
                    _bodies[i].Cleared = true;
                    _bodies[i].Hp = 0f;
                }
            }
        }

        // =================================================================
        // 伤害倍率计算
        // =================================================================

        /// <summary>
        /// 计算伤害倍率
        /// 对应 TypeScript 版 damageMultiplier()
        /// </summary>
        /// <param name="blocking">是否实心障碍</param>
        /// <param name="attachment">当前前挂件类型</param>
        /// <returns>伤害倍率</returns>
        private static float DamageMultiplier(bool blocking, FrontAttachment attachment)
        {
            if (blocking)
            {
                return attachment == FrontAttachment.Winch ? DmgBlockingWinch
                    : attachment == FrontAttachment.Plow ? DmgBlockingPlow
                    : DmgBlockingNone;
            }
            return attachment == FrontAttachment.Plow ? DmgNonBlockingPlow
                : attachment == FrontAttachment.Winch ? DmgNonBlockingWinch
                : DmgNonBlockingNone;
        }

        // =================================================================
        // 碰撞解算
        // =================================================================

        /// <summary>
        /// 解算一帧的障碍碰撞
        /// 对应 TypeScript 版 resolveObstacles()
        ///
        /// 返回 null 表示无接触
        /// </summary>
        /// <param name="carX">车辆世界坐标X</param>
        /// <param name="carZ">车辆世界坐标Z</param>
        /// <param name="speed">车辆速度</param>
        /// <param name="attachment">当前前挂件</param>
        /// <param name="dt">帧间隔</param>
        /// <param name="now">当前时间</param>
        /// <returns>碰撞结果，null表示无碰撞</returns>
        public ObstacleResolve? ResolveObstacles(float carX, float carZ, float speed, FrontAttachment attachment, float dt, float now)
        {
            EnsureObstacleBodies();

            float absSpeed = Mathf.Abs(speed);
            int cellX = Mathf.FloorToInt(carX / CellSize);
            int cellZ = Mathf.FloorToInt(carZ / CellSize);

            ObstacleBody best = null;
            float bestPen = 0f;
            float bestNx = 0f;
            float bestNz = 1f;

            // 遍历周围3x3网格
            for (int cz = -1; cz <= 1; cz++)
            {
                for (int cx = -1; cx <= 1; cx++)
                {
                    string key = $"{cellX + cx},{cellZ + cz}";
                    if (!_grid.TryGetValue(key, out var cell)) continue;

                    for (int i = 0; i < cell.Count; i++)
                    {
                        var body = cell[i];
                        if (body.Cleared) continue;

                        float dx = carX - body.X;
                        float dz = carZ - body.Z;
                        float minDist = CarRadius + body.Radius;
                        float distSq = dx * dx + dz * dz;
                        if (distSq >= minDist * minDist) continue;

                        float dist = Mathf.Max(0.001f, Mathf.Sqrt(distSq));
                        float pen = minDist - dist;

                        if (best == null || pen > bestPen)
                        {
                            best = body;
                            bestPen = pen;
                            bestNx = dx / dist;
                            bestNz = dz / dist;
                        }
                    }
                }
            }

            if (best == null) return null;

            float mult = DamageMultiplier(best.Blocking, attachment);

            // ── 伤害计算 ──
            float dmg = 0f;
            if (best.Blocking)
            {
                // 实心：按撞击事件计伤（带冷却），需要有一定速度
                if (now - best.LastHit > BlockingHitCooldown && absSpeed > BlockingMinSpeed)
                {
                    dmg = Mathf.Clamp((absSpeed - BlockingPowerSpeedBase) / BlockingPowerSpeedRange, BlockingPowerMin, BlockingPowerMax) * mult;
                    best.LastHit = now;
                }
            }
            else
            {
                // 雪堆：持续碾压清除
                dmg = dt * (SnowBaseDmg + absSpeed * SnowSpeedCoeff) * mult;
            }
            best.Hp -= dmg;

            // ── 清除判定 ──
            if (best.Hp <= 0f)
            {
                best.Cleared = true;
                return new ObstacleResolve
                {
                    PushX = 0f,
                    PushZ = 0f,
                    VelocityMul = best.Blocking ? ClearedBlockingVelocityMul : ClearedNonBlockingVelocityMul,
                    Cleared = new ObstacleClearedInfo
                    {
                        Id = best.Id,
                        Kind = best.Kind,
                        X = best.X,
                        Z = best.Z,
                        Scale = best.Scale,
                    },
                };
            }

            // ── 未清除时的物理响应 ──
            if (best.Blocking)
            {
                // 推开车身，挡住去路
                float k = bestPen * BlockingPushK + BlockingPushMin;
                return new ObstacleResolve
                {
                    PushX = bestNx * k,
                    PushZ = bestNz * k,
                    VelocityMul = absSpeed > BlockingHighSpeedThreshold ? BlockingHighSpeedBounce : 0f,
                    Cleared = null,
                };
            }

            // 雪堆：可通过但减速（装铲子时几乎无感）
            float drag = attachment == FrontAttachment.Plow ? dt * SnowDragPlow : dt * SnowDragNoPlow;
            return new ObstacleResolve
            {
                PushX = 0f,
                PushZ = 0f,
                VelocityMul = Mathf.Max(SnowMinVelocityMul, 1f - drag),
                Cleared = null,
            };
        }

        // =================================================================
        // 血量查询
        // =================================================================

        /// <summary>
        /// 获取障碍物血量比例
        /// 对应 TypeScript 版 getObstacleHpRatio()
        /// </summary>
        /// <param name="id">障碍ID</param>
        /// <returns>血量比例 (0~1)，0表示已清除</returns>
        public float GetObstacleHpRatio(string id)
        {
            for (int i = 0; i < _bodies.Count; i++)
            {
                if (_bodies[i].Id == id)
                {
                    return _bodies[i].Cleared ? 0f : Mathf.Max(0f, _bodies[i].Hp / _bodies[i].HpMax);
                }
            }
            return 0f;
        }

        // =================================================================
        // 交互提示
        // =================================================================

        /// <summary>
        /// 更新交互提示（检测附近障碍物并生成提示信息）
        /// </summary>
        /// <param name="carX">车辆世界坐标X</param>
        /// <param name="carZ">车辆世界坐标Z</param>
        /// <param name="attachment">当前前挂件</param>
        public void UpdatePrompt(float carX, float carZ, FrontAttachment attachment)
        {
            EnsureObstacleBodies();

            ObstacleBody nearest = null;
            float nearestDist = float.MaxValue;

            for (int i = 0; i < _bodies.Count; i++)
            {
                var body = _bodies[i];
                if (body.Cleared) continue;

                float dx = carX - body.X;
                float dz = carZ - body.Z;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);

                if (dist < PromptRange && dist < nearestDist)
                {
                    nearest = body;
                    nearestDist = dist;
                }
            }

            if (nearest != null)
            {
                string hint;
                FrontAttachment recommended;

                if (nearest.Blocking)
                {
                    // 实心障碍：推荐绞盘
                    recommended = FrontAttachment.Winch;
                    hint = nearest.Kind == ObstacleKind.FallenLog
                        ? "倒木挡路 — 使用绞盘拖开"
                        : "落石挡路 — 使用绞盘清除";
                }
                else
                {
                    // 雪堆：推荐铲雪犁
                    recommended = FrontAttachment.Plow;
                    hint = "雪堆 — 使用铲雪犁清除";
                }

                // 根据当前附件调整提示
                if (nearest.Blocking && attachment == FrontAttachment.Plow)
                {
                    hint += "（铲雪犁效率较低）";
                }
                else if (!nearest.Blocking && attachment == FrontAttachment.Winch)
                {
                    hint += "（绞盘效率较低）";
                }
                else if (attachment == recommended)
                {
                    hint += "（最佳装备）";
                }

                _currentPrompt = new ObstaclePrompt
                {
                    Id = nearest.Id,
                    Kind = nearest.Kind,
                    Distance = nearestDist,
                    Hint = hint,
                    RecommendedAttachment = recommended,
                };
            }
            else
            {
                _currentPrompt = null;
            }
        }

        // =================================================================
        // 障碍物确定性生成
        // =================================================================

        /// <summary>
        /// 确定性生成障碍物列表
        /// 对应 TypeScript 版 generateObstacles()
        /// 使用与原版相同的种子和算法，确保每次生成结果一致
        /// </summary>
        /// <returns>障碍定义列表</returns>
        private static List<ObstacleDef> GenerateObstacles()
        {
            var rng = new Mulberry32(GameData.ObstacleSeed);
            var list = new List<ObstacleDef>();
            int index = 0;

            float routeStart = GameData.ObstacleRouteStart;
            float routeEnd = GameData.ObstacleRouteEnd;
            float clusterStep = GameData.ObstacleClusterStep;

            for (float routeZ = routeStart; routeZ >= routeEnd; routeZ -= clusterStep)
            {
                // 按区段决定这一处放不放
                if (rng.Next() < SkipChance(routeZ)) continue;

                float jitterRouteZ = routeZ + (rng.Next() - 0.5f) * 8f;
                float worldZ = TerrainHeight.RouteToWorldZ(jitterRouteZ);

                // 通过 RoadSampler 获取道路数据
                var sampler = TerrainHeight.RoadSampler;
                if (sampler == null) continue;
                var roadSample = sampler.Sample(worldZ);
                

                float centerX = roadSample.CenterX;
                float halfW = roadSample.HalfWidth;
                if (halfW < 1f) continue;

                var kind = PickKind(jitterRouteZ, rng.Next());
                var props = GameData.GetObstacleKindProps(kind);

                // 群内障碍数量：1~2个
                int count = 1 + (int)(rng.Next() * 2f);

                // 是否横跨整条路
                bool fullBlockade = props.blocking && rng.Next() < 0.45f;

                for (int n = 0; n < count; n++)
                {
                    float lateral;
                    if (fullBlockade)
                    {
                        lateral = count == 1
                            ? 0f
                            : (n / (float)(count - 1) - 0.5f) * 2f * halfW * 0.78f;
                    }
                    else
                    {
                        lateral = (rng.Next() - 0.5f) * 2f * halfW * 0.85f;
                    }

                    float localZ = (rng.Next() - 0.5f) * 6f;
                    float x = centerX + lateral;
                    float z = worldZ + localZ;
                    float scale = 0.85f + rng.Next() * 0.6f;

                    list.Add(new ObstacleDef
                    {
                        id = $"obs_{index++}",
                        kind = kind,
                        position = new Vector3(x, TerrainHeight.GetTerrainHeight(x, z), z),
                        rotationY = rng.Next() * Mathf.PI * 2f,
                        scale = scale,
                        hp = props.hp * (0.85f + scale * 0.3f),
                        radius = props.radius * scale,
                        blocking = props.blocking,
                    });
                }
            }

            return list;
        }

        /// <summary>
        /// 根据路线Z坐标选择障碍类型
        /// 对应 TypeScript 版 pickKind()
        /// </summary>
        private static ObstacleKind PickKind(float routeZ, float r)
        {
            if (routeZ > 180f)
            {
                // 平原/公路：雪为主，偶有冰块
                return r < 0.82f ? ObstacleKind.SnowDrift : ObstacleKind.IceBlock;
            }
            if (routeZ > -100f)
            {
                // 森林/湖区：雪+倒木
                return r < 0.55f ? ObstacleKind.SnowDrift
                    : r < 0.85f ? ObstacleKind.FallenLog
                    : ObstacleKind.IceBlock;
            }
            // 山脊/村口接近段：落石+雪
            return r < 0.5f ? ObstacleKind.Rockfall
                : r < 0.8f ? ObstacleKind.SnowDrift
                : ObstacleKind.FallenLog;
        }

        /// <summary>
        /// 各区段"留空"概率
        /// 对应 TypeScript 版 skipChance()
        /// </summary>
        private static float SkipChance(float routeZ)
        {
            if (routeZ > 180f) return 0.8f;   // 平原：大多数路段本就畅通
            if (routeZ > -100f) return 0.45f;  // 森林：倒木较常见
            return 0.5f;                        // 山脊：落石中等
        }

        // =================================================================
        // Unity 生命周期
        // =================================================================

        private void Update()
        {
            // 更新交互提示
            if (GameManager.Instance != null)
            {
                var vt = GameManager.Instance.VehicleTransient;
                if (vt != null && vt.Position != null && vt.Position.Length >= 2)
                {
                    UpdatePrompt(vt.Position[0], vt.Position[1], GameManager.Instance.FrontAttachmentType);
                }
            }
        }

        // =================================================================
        // 清理
        // =================================================================

        /// <summary>
        /// 清除所有碰撞体和网格数据
        /// </summary>
        public void ClearAll()
        {
            _bodies.Clear();
            _grid.Clear();
            _built = false;
            _currentPrompt = null;
        }
    }

    // =========================================================================
    // 确定性随机数生成器
    // =========================================================================

    /// <summary>
    /// Mulberry32 确定性伪随机数生成器
    /// 对应 TypeScript 版 mulberry32()
    /// 保证同一种子产生完全相同的随机序列
    /// </summary>
    public class Mulberry32
    {
        private uint _state;

        /// <summary>
        /// 使用指定种子初始化
        /// </summary>
        /// <param name="seed">随机种子</param>
        public Mulberry32(int seed)
        {
            _state = (uint)seed;
        }

        /// <summary>
        /// 生成下一个 [0, 1) 范围的随机浮点数
        /// </summary>
        /// <returns>随机浮点数</returns>
        public float Next()
        {
            _state |= 0;
            _state = _state + 0x6D2B79F5;
            uint t = MathIMul(_state ^ (_state >> 15), 1 | _state);
            t = (t + MathIMul(t ^ (t >> 7), 61 | t)) ^ t;
            return ((t ^ (t >> 14)) >> 0) / 4294967296f;
        }

        /// <summary>
        /// 32位整数乘法（模拟 JavaScript Math.imul）
        /// </summary>
        private static uint MathIMul(uint a, uint b)
        {
            // 拆分为高低16位相乘再组合
            uint aLow = a & 0xFFFF;
            uint aHigh = a >> 16;
            uint bLow = b & 0xFFFF;
            uint bHigh = b >> 16;

            return (aLow * bLow) + (((aHigh * bLow + aLow * bHigh) << 16) >> 0);
        }
    }
}

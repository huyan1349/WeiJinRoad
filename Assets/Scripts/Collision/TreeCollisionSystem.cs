// =============================================================================
// TreeCollisionSystem.cs — 树木/栅栏碰撞系统
// 翻译自 TypeScript 版 treeCollision.ts
//
// 核心机制：
// - 空间网格加速结构，高效检测大量树木碰撞
// - 车速 > 2.2 时触发树倒动画
// - 栅栏碰撞阻力 = 0.36
// - 碰撞方向分类（正面/侧面/低矮物）
// - 树木扬尘粒子效果触发
// =============================================================================

using System;
using System.Collections.Generic;
using UnityEngine;
using WeiJinRoad.Core;
using WeiJinRoad.Vehicle;

namespace WeiJinRoad.Collision
{
    // =========================================================================
    // 枚举与数据结构
    // =========================================================================

    /// <summary>
    /// 树木种类：松树、雪松、枯树、栅栏
    /// </summary>
    public enum TreeKind
    {
        /// <summary>松树</summary>
        Pine,
        /// <summary>雪松</summary>
        SnowPine,
        /// <summary>枯树</summary>
        Dead,
        /// <summary>栅栏</summary>
        Fence
    }

    /// <summary>
    /// 树木状态：站立、倒下中、已倒
    /// </summary>
    public enum TreeState
    {
        /// <summary>站立</summary>
        Standing,
        /// <summary>倒下中</summary>
        Falling,
        /// <summary>已倒</summary>
        Fallen
    }

    /// <summary>
    /// 树木碰撞体数据
    /// 对应 TypeScript 版 TreeCollisionBody
    /// </summary>
    [Serializable]
    public class TreeCollisionBody
    {
        /// <summary>唯一ID</summary>
        public string Id;
        /// <summary>所属组ID</summary>
        public string GroupId;
        /// <summary>树木种类</summary>
        public TreeKind Kind;
        /// <summary>世界坐标X</summary>
        public float X;
        /// <summary>世界坐标Y</summary>
        public float Y;
        /// <summary>世界坐标Z</summary>
        public float Z;
        /// <summary>碰撞半径</summary>
        public float Radius;
        /// <summary>缩放</summary>
        public float Scale;
        /// <summary>Y轴旋转（弧度）</summary>
        public float RotationY;
        /// <summary>弯曲度</summary>
        public float Bend;
        /// <summary>当前状态</summary>
        public TreeState State = TreeState.Standing;
        /// <summary>倒下进度 (0~1)</summary>
        public float FallProgress;
        /// <summary>树冠淡出 (0~1)</summary>
        public float FoliageFade;
        /// <summary>倒向方向X</summary>
        public float FallDirX;
        /// <summary>倒向方向Z</summary>
        public float FallDirZ;
        /// <summary>碰撞力度</summary>
        public float Impact;
        /// <summary>上次碰撞时间</summary>
        public float LastHit = -999f;
    }

    /// <summary>
    /// 树木碰撞检测结果
    /// 对应 TypeScript 版 TreeHit
    /// </summary>
    public struct TreeHit
    {
        /// <summary>碰撞体引用</summary>
        public TreeCollisionBody Body;
        /// <summary>碰撞法线X</summary>
        public float Nx;
        /// <summary>碰撞法线Z</summary>
        public float Nz;
        /// <summary>穿透深度</summary>
        public float Penetration;
        /// <summary>碰撞力度</summary>
        public float Power;
    }

    /// <summary>
    /// 树木扬尘粒子效果数据
    /// </summary>
    public struct TreeDustEffect
    {
        /// <summary>效果序列号（每次递增，用于检测变更）</summary>
        public int Serial;
        /// <summary>扬尘位置X</summary>
        public float X;
        /// <summary>扬尘位置Z</summary>
        public float Z;
        /// <summary>扬尘力度</summary>
        public float Power;
    }

    // =========================================================================
    // 树木碰撞系统 — 主类
    // =========================================================================

    /// <summary>
    /// 树木/栅栏碰撞检测系统
    ///
    /// 使用空间网格加速结构，高效处理大量树木的碰撞检测。
    /// 碰撞结果通过 TreeCollisionHit 返回给 VehicleController.ApplyCollision()。
    ///
    /// 挂载方式：附加到与 VehicleController 相同的 GameObject 上，
    /// VehicleController.Update() 会自动调用 CheckCollision()。
    /// </summary>
    public class TreeCollisionSystem : Vehicle.TreeCollisionSystem
    {
        // =================================================================
        // 常量 — 与 TypeScript 版本完全一致
        // =================================================================

        /// <summary>空间网格单元大小</summary>
        private const int CellSize = 12;

        /// <summary>车辆碰撞半径</summary>
        private const float CarRadius = 0.95f;

        /// <summary>触发碰撞的最低速度</summary>
        private const float MinHitSpeed = 2.2f;

        /// <summary>碰撞冷却时间（秒）</summary>
        private const float HitCooldown = 0.7f;

        /// <summary>碰撞力度基础映射下限速度</summary>
        private const float PowerSpeedBase = 1.6f;

        /// <summary>碰撞力度映射范围</summary>
        private const float PowerSpeedRange = 8f;

        /// <summary>碰撞力度最小值</summary>
        private const float PowerMin = 0.38f;

        /// <summary>碰撞力度最大值</summary>
        private const float PowerMax = 1.25f;

        /// <summary>栅栏碰撞力度倍率</summary>
        private const float FencePowerMult = 0.42f;

        /// <summary>栅栏碰撞阻力</summary>
        public const float FenceResistance = 0.36f;

        /// <summary>快速距离剔除范围</summary>
        private const float QuickRejectDist = 7f;

        // =================================================================
        // 数据存储
        // =================================================================

        /// <summary>按组ID索引的碰撞体</summary>
        private readonly Dictionary<string, List<TreeCollisionBody>> _bodiesByGroup = new Dictionary<string, List<TreeCollisionBody>>();

        /// <summary>所有碰撞体列表</summary>
        private readonly List<TreeCollisionBody> _allBodies = new List<TreeCollisionBody>();

        /// <summary>空间网格</summary>
        private readonly Dictionary<string, List<TreeCollisionBody>> _grid = new Dictionary<string, List<TreeCollisionBody>>();

        /// <summary>正在播放动画的组ID集合</summary>
        private readonly HashSet<string> _activeGroupIds = new HashSet<string>();

        /// <summary>树木碰撞冲击事件序列号</summary>
        private int _treeImpactSerial;

        /// <summary>树木碰撞冲击数据</summary>
        private TreeCollisionHit _lastTreeImpact;

        /// <summary>树木扬尘效果数据</summary>
        private TreeDustEffect _treeDust;

        // =================================================================
        // 公开属性
        // =================================================================

        /// <summary>树木碰撞冲击事件序列号（每次递增，用于检测变更）</summary>
        public int TreeImpactSerial => _treeImpactSerial;

        /// <summary>最近一次树木碰撞冲击数据</summary>
        public TreeCollisionHit LastTreeImpact => _lastTreeImpact;

        /// <summary>树木扬尘效果数据</summary>
        public TreeDustEffect TreeDust => _treeDust;

        /// <summary>所有碰撞体（只读）</summary>
        public IReadOnlyList<TreeCollisionBody> AllBodies => _allBodies.AsReadOnly();

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
        private void RebuildGrid()
        {
            _grid.Clear();
            for (int i = 0; i < _allBodies.Count; i++)
            {
                var body = _allBodies[i];
                string key = GridKey(body.X, body.Z);
                if (_grid.TryGetValue(key, out var cell))
                {
                    cell.Add(body);
                }
                else
                {
                    _grid[key] = new List<TreeCollisionBody> { body };
                }
            }
        }

        // =================================================================
        // 碰撞体管理
        // =================================================================

        /// <summary>
        /// 创建树木碰撞体数组
        /// 对应 TypeScript 版 makeTreeCollisionBodies()
        /// </summary>
        /// <param name="groupId">组ID</param>
        /// <param name="trees">树木数据数组</param>
        /// <param name="kind">树木种类</param>
        /// <returns>碰撞体数组</returns>
        public static TreeCollisionBody[] MakeTreeCollisionBodies(
            string groupId,
            TreeInputData[] trees,
            TreeKind kind)
        {
            var bodies = new TreeCollisionBody[trees.Length];
            for (int i = 0; i < trees.Length; i++)
            {
                ref readonly var tree = ref trees[i];
                float trunkRadius = kind == TreeKind.Fence ? 1.1f
                    : kind == TreeKind.Dead ? 0.28f
                    : 0.32f;

                bodies[i] = new TreeCollisionBody
                {
                    Id = $"{groupId}-{i}",
                    GroupId = groupId,
                    Kind = kind,
                    X = tree.PosX,
                    Y = tree.PosY,
                    Z = tree.PosZ,
                    Radius = trunkRadius * tree.Scale,
                    Scale = tree.Scale,
                    RotationY = tree.RotationY,
                    Bend = tree.Bend,
                    State = TreeState.Standing,
                    FallProgress = 0f,
                    FoliageFade = 0f,
                    FallDirX = 0f,
                    FallDirZ = 1f,
                    Impact = 0f,
                    LastHit = -999f,
                };
            }
            return bodies;
        }

        /// <summary>
        /// 注册树木碰撞体
        /// 对应 TypeScript 版 registerTreeColliders()
        /// </summary>
        /// <param name="groupId">组ID</param>
        /// <param name="bodies">碰撞体数组</param>
        public void RegisterTreeColliders(string groupId, TreeCollisionBody[] bodies)
        {
            UnregisterTreeColliders(groupId);
            _bodiesByGroup[groupId] = new List<TreeCollisionBody>(bodies);
            _allBodies.AddRange(bodies);
            RebuildGrid();
        }

        /// <summary>
        /// 注销树木碰撞体
        /// 对应 TypeScript 版 unregisterTreeColliders()
        /// </summary>
        /// <param name="groupId">组ID</param>
        public void UnregisterTreeColliders(string groupId)
        {
            if (!_bodiesByGroup.TryGetValue(groupId, out var bodies)) return;
            _bodiesByGroup.Remove(groupId);

            // 从全局列表移除
            for (int i = _allBodies.Count - 1; i >= 0; i--)
            {
                if (bodies.Contains(_allBodies[i]))
                {
                    _allBodies.RemoveAt(i);
                }
            }

            _activeGroupIds.Remove(groupId);
            RebuildGrid();
        }

        // =================================================================
        // 碰撞检测
        // =================================================================

        /// <summary>
        /// 检测碰撞，返回 null 表示无碰撞
        /// 对应 TypeScript 版 hitTreeColliders()
        ///
        /// 由 VehicleController.Update() 每帧调用
        /// </summary>
        /// <param name="carX">车辆世界坐标X</param>
        /// <param name="carZ">车辆世界坐标Z</param>
        /// <param name="speed">车辆速度</param>
        /// <param name="time">当前时间（Time.time）</param>
        /// <returns>碰撞结果，null表示无碰撞</returns>
        public override TreeCollisionHit? CheckCollision(float carX, float carZ, float speed, float time)
        {
            float absSpeed = Mathf.Abs(speed);
            if (absSpeed < MinHitSpeed) return null;

            int cellX = Mathf.FloorToInt(carX / CellSize);
            int cellZ = Mathf.FloorToInt(carZ / CellSize);

            TreeHit? bestHit = null;
            float bestPen = 0f;

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

                        // 只检测站立中的树，且冷却时间已过
                        if (body.State != TreeState.Standing || time - body.LastHit < HitCooldown)
                            continue;

                        // 快速距离剔除
                        if (Mathf.Abs(body.Z - carZ) > QuickRejectDist || Mathf.Abs(body.X - carX) > QuickRejectDist)
                            continue;

                        // 圆-圆碰撞检测
                        float dx = carX - body.X;
                        float dz = carZ - body.Z;
                        float minDist = CarRadius + body.Radius;
                        float distSq = dx * dx + dz * dz;
                        if (distSq >= minDist * minDist) continue;

                        float dist = Mathf.Max(0.001f, Mathf.Sqrt(distSq));
                        float nx = dx / dist;
                        float nz = dz / dist;
                        float penetration = minDist - dist;

                        // 碰撞力度计算（与 TS: clamp((absSpeed - 1.6) / 8, 0.38, 1.25)）
                        float basePower = Mathf.Clamp((absSpeed - PowerSpeedBase) / PowerSpeedRange, PowerMin, PowerMax);
                        float power = body.Kind == TreeKind.Fence ? basePower * FencePowerMult : basePower;

                        // 取穿透最深的碰撞
                        if (!bestHit.HasValue || penetration > bestPen)
                        {
                            bestHit = new TreeHit
                            {
                                Body = body,
                                Nx = nx,
                                Nz = nz,
                                Penetration = penetration,
                                Power = power,
                            };
                            bestPen = penetration;
                        }
                    }
                }
            }

            if (!bestHit.HasValue) return null;

            // 触发树倒动画
            var hitBody = bestHit.Value.Body;
            hitBody.State = TreeState.Falling;
            hitBody.FallProgress = 0.001f;
            hitBody.FallDirX = bestHit.Value.Nx;
            hitBody.FallDirZ = bestHit.Value.Nz;
            hitBody.Impact = bestHit.Value.Power;
            hitBody.LastHit = time;
            _activeGroupIds.Add(hitBody.GroupId);

            // 记录碰撞冲击数据（供 VehicleController 读取）
            _treeImpactSerial++;
            _lastTreeImpact = new TreeCollisionHit
            {
                Nx = bestHit.Value.Nx,
                Nz = bestHit.Value.Nz,
                Penetration = bestHit.Value.Penetration,
                Power = bestHit.Value.Power,
                Kind = hitBody.Kind == TreeKind.Fence ? ColliderKind.Fence : ColliderKind.Tree,
                BodyX = hitBody.X,
                BodyZ = hitBody.Z,
                FallDirX = hitBody.FallDirX,
                FallDirZ = hitBody.FallDirZ,
            };

            // 记录扬尘效果数据
            _treeDust.Serial++;
            _treeDust.X = carX;
            _treeDust.Z = carZ;
            _treeDust.Power = bestHit.Value.Power * 0.75f;

            return _lastTreeImpact;
        }

        /// <summary>
        /// 检测碰撞（重载，带航向角用于扬尘位置计算）
        /// </summary>
        /// <param name="carX">车辆世界坐标X</param>
        /// <param name="carZ">车辆世界坐标Z</param>
        /// <param name="speed">车辆速度</param>
        /// <param name="time">当前时间</param>
        /// <param name="heading">车辆航向角（弧度）</param>
        /// <returns>碰撞结果</returns>
        public TreeCollisionHit? CheckCollision(float carX, float carZ, float speed, float time, float heading)
        {
            var result = CheckCollision(carX, carZ, speed, time);
            if (result.HasValue)
            {
                // 更新扬尘位置（沿车辆前方偏移3.2m）
                _treeDust.X = carX + Mathf.Sin(heading) * 3.2f;
                _treeDust.Z = carZ + Mathf.Cos(heading) * 3.2f;
            }
            return result;
        }

        // =================================================================
        // 动画状态管理
        // =================================================================

        /// <summary>
        /// 判断指定组是否正在播放动画
        /// 对应 TypeScript 版 isTreeGroupAnimating()
        /// </summary>
        /// <param name="groupId">组ID</param>
        /// <returns>是否正在动画</returns>
        public bool IsTreeGroupAnimating(string groupId)
        {
            return _activeGroupIds.Contains(groupId);
        }

        /// <summary>
        /// 完成树木倒下动画
        /// 对应 TypeScript 版 finishTreeFall()
        /// </summary>
        /// <param name="body">碰撞体</param>
        public void FinishTreeFall(TreeCollisionBody body)
        {
            body.State = TreeState.Fallen;

            if (!_bodiesByGroup.TryGetValue(body.GroupId, out var groupBodies)) return;

            // 检查组内是否还有正在动画的树
            bool hasActive = false;
            for (int i = 0; i < groupBodies.Count; i++)
            {
                var tree = groupBodies[i];
                if (tree.State == TreeState.Falling)
                {
                    hasActive = true;
                    break;
                }
                // 非栅栏的已倒树木，树冠淡出未完成也算活跃
                if (tree.Kind != TreeKind.Fence && tree.State == TreeState.Fallen && tree.FoliageFade < 1f)
                {
                    hasActive = true;
                    break;
                }
            }

            if (!hasActive)
            {
                _activeGroupIds.Remove(body.GroupId);
            }
        }

        /// <summary>
        /// 更新所有树木的倒下动画（每帧调用）
        /// </summary>
        /// <param name="delta">帧间隔时间</param>
        public void UpdateFallingAnimations(float delta)
        {
            if (_activeGroupIds.Count == 0) return;

            // 遍历活跃组中的所有树
            var groupsToRemove = new List<string>();

            foreach (var groupId in _activeGroupIds)
            {
                if (!_bodiesByGroup.TryGetValue(groupId, out var bodies)) continue;

                bool groupStillActive = false;

                for (int i = 0; i < bodies.Count; i++)
                {
                    var body = bodies[i];

                    if (body.State == TreeState.Falling)
                    {
                        // 倒下进度递增
                        body.FallProgress = Mathf.Min(1f, body.FallProgress + delta * 1.2f);

                        // 倒下完成
                        if (body.FallProgress >= 1f)
                        {
                            FinishTreeFall(body);
                        }
                        else
                        {
                            groupStillActive = true;
                        }
                    }
                    else if (body.Kind != TreeKind.Fence && body.State == TreeState.Fallen && body.FoliageFade < 1f)
                    {
                        // 树冠淡出
                        body.FoliageFade = Mathf.Min(1f, body.FoliageFade + delta * 0.5f);
                        groupStillActive = true;
                    }
                }

                if (!groupStillActive)
                {
                    groupsToRemove.Add(groupId);
                }
            }

            // 清除不再活跃的组
            for (int i = 0; i < groupsToRemove.Count; i++)
            {
                _activeGroupIds.Remove(groupsToRemove[i]);
            }
        }

        // =================================================================
        // Unity 生命周期
        // =================================================================

        private void Update()
        {
            float delta = Time.deltaTime;
            if (delta <= 0f) return;

            UpdateFallingAnimations(delta);
        }

        // =================================================================
        // 清理
        // =================================================================

        /// <summary>
        /// 清除所有碰撞体和网格数据
        /// </summary>
        public void ClearAll()
        {
            _bodiesByGroup.Clear();
            _allBodies.Clear();
            _grid.Clear();
            _activeGroupIds.Clear();
            _treeImpactSerial = 0;
            _treeDust = default;
        }
    }

    // =========================================================================
    // 辅助数据结构
    // =========================================================================

    /// <summary>
    /// 树木输入数据（用于批量创建碰撞体）
    /// </summary>
    [Serializable]
    public struct TreeInputData
    {
        /// <summary>位置X</summary>
        public float PosX;
        /// <summary>位置Y</summary>
        public float PosY;
        /// <summary>位置Z</summary>
        public float PosZ;
        /// <summary>Y轴旋转（弧度）</summary>
        public float RotationY;
        /// <summary>缩放</summary>
        public float Scale;
        /// <summary>弯曲度</summary>
        public float Bend;
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-143126869b5942eab8a175a04c207d24/cwd.txt'; exit "$__tr_native_ec"
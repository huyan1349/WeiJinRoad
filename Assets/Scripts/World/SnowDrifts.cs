using System.Collections.Generic;
using UnityEngine;

namespace WeiJinRoad.World
{
    /// <summary>
    /// 道路雪堆数据结构
    /// </summary>
    public struct RoadSnowDriftData
    {
        /// <summary>雪堆ID</summary>
        public int Id;
        /// <summary>世界Z坐标</summary>
        public float Z;
        /// <summary>相对道路中心的X偏移</summary>
        public float Offset;
        /// <summary>雪堆缩放</summary>
        public float Scale;
        /// <summary>世界X坐标</summary>
        public float X;
        /// <summary>碰撞半径</summary>
        public float Radius;
    }

    /// <summary>
    /// 道路雪堆系统
    ///
    /// 沿道路生成 12 个雪堆，提供点碰撞和线段碰撞检测。
    /// 使用确定性随机函数保证每次生成结果一致。
    /// 参考 TerrainHeight 获取道路中心和宽度。
    /// </summary>
    public static class SnowDrifts
    {
        // =================================================================
        // 确定性随机
        // =================================================================

        /// <summary>
        /// 确定性伪随机函数，输入种子返回 [0,1) 范围值
        /// </summary>
        /// <param name="seed">种子值</param>
        /// <returns>[0,1) 范围的伪随机数</returns>
        private static float Seeded01(int seed)
        {
            float val = Mathf.Sin(seed * 127.1f + 311.7f) * 43758.5453f;
            return val - Mathf.Floor(val);
        }

        // =================================================================
        // 已清除雪堆追踪
        // =================================================================

        /// <summary>
        /// 已清除的雪堆ID集合
        /// </summary>
        public static readonly HashSet<int> ClearedSnowDrifts = new HashSet<int>();

        // =================================================================
        // 雪堆生成
        // =================================================================

        /// <summary>
        /// 生成沿道路分布的 12 个雪堆数据
        /// </summary>
        /// <returns>雪堆数据数组</returns>
        public static RoadSnowDriftData[] GetRoadSnowDrifts()
        {
            var drifts = new RoadSnowDriftData[12];

            for (int i = 0; i < 12; i++)
            {
                float z = TerrainHeight.RouteToWorldZ(-512 - i * 15.2f);
                int side = i % 2 == 0 ? -1 : 1;
                float roadEdge = GetRoadWidth(z) / 2f;
                float rand = Mathf.Abs(Seeded01(i + 1));
                float offset = side * (roadEdge + 7.2f + rand * 2.6f);
                float scale = 0.32f + Mathf.Abs(Seeded01(i + 20)) * 0.34f;

                drifts[i] = new RoadSnowDriftData
                {
                    Id = i,
                    Z = z,
                    Offset = offset,
                    Scale = scale,
                    X = GetRoadCenter(z) + offset,
                    Radius = 0.85f + scale * 0.62f
                };
            }

            return drifts;
        }

        // =================================================================
        // 碰撞检测
        // =================================================================

        /// <summary>
        /// 点碰撞检测：检查指定位置是否命中雪堆
        /// </summary>
        /// <param name="x">世界X坐标</param>
        /// <param name="z">世界Z坐标</param>
        /// <param name="radius">碰撞检测半径，默认 2.45</param>
        /// <returns>命中的雪堆数据，未命中返回 null</returns>
        public static RoadSnowDriftData? FindSnowDriftHit(float x, float z, float radius = 2.45f)
        {
            var drifts = GetRoadSnowDrifts();
            for (int i = 0; i < drifts.Length; i++)
            {
                if (ClearedSnowDrifts.Contains(drifts[i].Id)) continue;

                float dx = x - drifts[i].X;
                float dz = z - drifts[i].Z;
                float r = radius + drifts[i].Radius;

                if (dx * dx + dz * dz < r * r)
                    return drifts[i];
            }
            return null;
        }

        /// <summary>
        /// 线段碰撞检测：检查移动线段是否命中雪堆
        /// </summary>
        /// <param name="fromX">起点X</param>
        /// <param name="fromZ">起点Z</param>
        /// <param name="toX">终点X</param>
        /// <param name="toZ">终点Z</param>
        /// <param name="radius">碰撞检测半径，默认 1.65</param>
        /// <returns>命中的雪堆数据，未命中返回 null</returns>
        public static RoadSnowDriftData? FindSnowDriftSweepHit(
            float fromX, float fromZ, float toX, float toZ, float radius = 1.65f)
        {
            float vx = toX - fromX;
            float vz = toZ - fromZ;
            float lenSq = vx * vx + vz * vz;
            if (lenSq < 0.0001f) lenSq = 1f;

            var drifts = GetRoadSnowDrifts();
            for (int i = 0; i < drifts.Length; i++)
            {
                if (ClearedSnowDrifts.Contains(drifts[i].Id)) continue;

                float t = Mathf.Max(0f, Mathf.Min(1f,
                    ((drifts[i].X - fromX) * vx + (drifts[i].Z - fromZ) * vz) / lenSq));
                float cx = fromX + vx * t;
                float cz = fromZ + vz * t;
                float dx = cx - drifts[i].X;
                float dz = cz - drifts[i].Z;
                float r = radius + drifts[i].Radius;

                if (dx * dx + dz * dz < r * r)
                    return drifts[i];
            }
            return null;
        }

        // =================================================================
        // 道路辅助
        // =================================================================

        /// <summary>
        /// 获取指定Z坐标处的道路中心X
        /// </summary>
        private static float GetRoadCenter(float z)
        {
            if (TerrainHeight.RoadSampler != null)
                return TerrainHeight.RoadSampler.Sample(z).CenterX;
            return 0f;
        }

        /// <summary>
        /// 获取指定Z坐标处的道路宽度
        /// </summary>
        private static float GetRoadWidth(float z)
        {
            if (TerrainHeight.RoadSampler != null)
                return TerrainHeight.RoadSampler.Sample(z).Width;
            return 8f;
        }
    }
}

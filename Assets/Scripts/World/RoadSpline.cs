using UnityEngine;
using System;

// ═══════════════════════════════════════════════════════════════
// RoadSpline — 道路样条数据系统
//
// Unity 架构：道路是唯一的真相来源（Single Source of Truth）。
// 所有系统（地形、车辆、场景）从同一份预计算道路数据中读取。
//
// 流程：原始数学函数 → 烘焙 → RoadSplineData（查找表）→ 查询 API
//
// 优势：
// - O(log n) 查找，而非每次查询的复杂分支
// - 所有系统读取相同数据 → 无同步 Bug
// - 道路数据在初始化时计算一次，非每帧计算
// - 批量查询用于网格生成
// ═══════════════════════════════════════════════════════════════

// ═══════════════════════════════════════════════════════════════
// 常量
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// 道路样条系统的全局常量定义
/// </summary>
public static class RoadConstants
{
    /// <summary>森林侵入区域起始 Z</summary>
    public const float ForestIntrusionStartZ = 292.9f;

    /// <summary>森林侵入区域长度</summary>
    public const float ForestIntrusionLength = 300f;

    /// <summary>森林侵入区域结束 Z</summary>
    public const float ForestIntrusionEndZ = ForestIntrusionStartZ - ForestIntrusionLength;

    /// <summary>山路起始 Z</summary>
    public const float MountainRoadStartZ = -700f;

    /// <summary>山路入口结束 Z</summary>
    public const float MountainRoadEntryEndZ = -760f;

    /// <summary>山路开阔段起始 Z</summary>
    public const float MountainRoadClearStartZ = -803.6f;

    /// <summary>山路结束 Z</summary>
    public const float MountainRoadEndZ = -1480f;

    /// <summary>山顶路线 Z</summary>
    public const float SummitRouteZ = -1532f;

    /// <summary>采样步长（每 0.5 个世界单位采样一次）</summary>
    public const float SampleStep = 0.5f;

    /// <summary>道路 Z 最大值</summary>
    public const float RoadZMax = 540f;

    /// <summary>道路 Z 最小值（routeToWorldZ(SUMMIT_ROUTE_Z) - 余量）</summary>
    public const float RoadZMin = -1860f;
}

// ═══════════════════════════════════════════════════════════════
// 坐标转换
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// 道路坐标转换工具：路线空间 ↔ 世界空间
/// </summary>
public static class RoadCoord
{
    /// <summary>
    /// 将路线空间 Z 坐标转换为世界空间 Z 坐标
    /// </summary>
    public static float RouteToWorldZ(float routeZ)
    {
        return routeZ < RoadConstants.ForestIntrusionStartZ
            ? routeZ - RoadConstants.ForestIntrusionLength
            : routeZ;
    }

    /// <summary>
    /// 将世界空间 Z 坐标转换为路线空间 Z 坐标
    /// </summary>
    public static float WorldToRouteZ(float worldZ)
    {
        return worldZ <= RoadConstants.ForestIntrusionEndZ
            ? worldZ + RoadConstants.ForestIntrusionLength
            : worldZ;
    }

    /// <summary>
    /// 判断给定 Z 坐标是否处于森林侵入区域
    /// </summary>
    public static bool IsForestIntrusionZ(float z)
    {
        return z <= RoadConstants.ForestIntrusionStartZ && z > RoadConstants.ForestIntrusionEndZ;
    }
}

// ═══════════════════════════════════════════════════════════════
// RoadSample — 在单个 Z 处的预计算道路数据
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// 道路采样点：在某个世界 Z 坐标处的预计算道路数据
/// </summary>
[Serializable]
public struct RoadSample
{
    /// <summary>世界 Z 坐标</summary>
    public float z;

    /// <summary>道路中心 X</summary>
    public float centerX;

    /// <summary>道路高度 Y</summary>
    public float height;

    /// <summary>道路宽度</summary>
    public float width;

    /// <summary>宽度的一半</summary>
    public float halfWidth;

    /// <summary>道路方向角（XZ 平面，弧度）</summary>
    public float direction;

    /// <summary>路线空间 Z 坐标</summary>
    public float routeZ;

    /// <summary>是否在山路路段</summary>
    public bool isMountainRoad;

    /// <summary>是否在森林侵入路段</summary>
    public bool isForestRoad;

    /// <summary>山路路基半宽（非山路时为 0）</summary>
    public float mountainBedHalfWidth;
}

// ═══════════════════════════════════════════════════════════════
// 平滑插值 & 烘焙函数
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// 道路烘焙函数：定义道路形状的原始数学函数。
/// 仅在初始化时运行一次以填充查找表，运行时切勿调用。
/// </summary>
internal static class RoadBake
{
    /// <summary>
    /// 在 [zStart, zEnd] 区间内对 a 和 b 进行 smoothstep 插值
    /// </summary>
    internal static float SmoothMix(float a, float b, float z, float zStart, float zEnd)
    {
        float t = Mathf.Max(0f, Mathf.Min(1f, (z - zStart) / (zEnd - zStart)));
        float st = t * t * (3f - 2f * t);
        return a + (b - a) * st;
    }

    // ── 道路中心 X ──

    internal static float DepartureZ(float z) { return 0f; }

    internal static float HighwayZ(float z) { return Mathf.Sin((520f - z) * 0.008f) * 1.2f; }

    internal static float ForestZ(float z)
    {
        float t = 180f - z;
        return Mathf.Sin(t * (Mathf.PI / 30f)) * 10f;
    }

    internal static float LakeZ(float z)
    {
        float t = 120f - z;
        return -Mathf.Sin(t * (Mathf.PI / 120f)) * 30f;
    }

    internal static float AscentZ(float z)
    {
        float t = -z;
        return Mathf.Sin(t * 0.0392f) * 50f;
    }

    internal static float RidgeZ(float z)
    {
        float t = -240f - z;
        return Mathf.Sin(t * 0.1f) * 8f;
    }

    internal static float EndZ(float z)
    {
        return RidgeZ(-300f);
    }

    internal static float VillageApproachZ(float z)
    {
        float t = (-340f - z) / 160f;
        return EndZ(z) + Mathf.Sin(t * Mathf.PI) * 22f + t * 16f;
    }

    internal static float VillageZ(float z)
    {
        return 16f + Mathf.Sin((-500f - z) * 0.035f) * 7f;
    }

    internal static float MountainRoadT(float z)
    {
        return Mathf.Max(0f, Mathf.Min(1f,
            (RoadConstants.MountainRoadStartZ - z) /
            (RoadConstants.MountainRoadStartZ - RoadConstants.MountainRoadEndZ)));
    }

    internal static float MountainRoadZ(float z)
    {
        float t = MountainRoadT(z);
        float basePath = VillageZ(RoadConstants.MountainRoadStartZ);
        float cliffBias = 10f + t * 48f;
        float hairpins = Mathf.Sin(t * Mathf.PI * 8.4f) * 54f;
        float cliffLine = Mathf.Sin(t * Mathf.PI * 3.2f + 0.8f) * 24f;
        float tightTurns = Mathf.Sin(t * Mathf.PI * 17.5f) * 9f;
        return basePath + cliffBias + hairpins + cliffLine + tightTurns;
    }

    internal static float SummitZ(float z)
    {
        return Mathf.Sin((RoadConstants.MountainRoadEndZ - z) * 0.02f) * 4f;
    }

    internal static float GetOriginalRoadCenter(float z)
    {
        if (z >= 280f)
        {
            if (z >= 300f) return HighwayZ(z);
            return SmoothMix(DepartureZ(z), HighwayZ(z), z, 280f, 300f);
        }
        if (z >= 180f)
        {
            if (z >= 200f) return DepartureZ(z);
            return SmoothMix(ForestZ(z), DepartureZ(z), z, 180f, 200f);
        }
        if (z >= 120f)
        {
            if (z >= 140f) return ForestZ(z);
            return SmoothMix(LakeZ(z), ForestZ(z), z, 120f, 140f);
        }
        if (z >= 0f)
        {
            if (z >= 20f) return LakeZ(z);
            return SmoothMix(AscentZ(z), LakeZ(z), z, 0f, 20f);
        }
        if (z >= -240f)
        {
            if (z >= -220f) return AscentZ(z);
            return SmoothMix(RidgeZ(z), AscentZ(z), z, -240f, -220f);
        }
        if (z >= -300f) return RidgeZ(z);
        if (z >= -500f)
        {
            if (z >= -360f) return SmoothMix(VillageApproachZ(z), EndZ(z), z, -500f, -360f);
            return VillageApproachZ(z);
        }
        if (z >= -700f)
        {
            if (z >= -520f) return SmoothMix(VillageZ(z), VillageApproachZ(z), z, -700f, -520f);
            return VillageZ(z);
        }
        if (z >= RoadConstants.MountainRoadEndZ)
        {
            if (z >= RoadConstants.MountainRoadEntryEndZ)
                return SmoothMix(MountainRoadZ(z), VillageZ(z), z,
                    RoadConstants.MountainRoadEntryEndZ, RoadConstants.MountainRoadStartZ);
            return MountainRoadZ(z);
        }
        return SummitZ(z);
    }

    internal static float ForestIntrusionT(float z)
    {
        return Mathf.Max(0f, Mathf.Min(1f,
            (RoadConstants.ForestIntrusionStartZ - z) / RoadConstants.ForestIntrusionLength));
    }

    internal static float BakeRoadCenter(float z)
    {
        if (RoadCoord.IsForestIntrusionZ(z))
        {
            float t = ForestIntrusionT(z);
            float gate = Mathf.Sin(t * Mathf.PI);
            float broadCurve = Mathf.Sin(t * Mathf.PI * 1.35f - 0.35f) * 18f;
            float switchbacks = Mathf.Sin(t * Mathf.PI * 4.8f + 0.5f) * 9.5f;
            float tightWiggle = Mathf.Sin(t * Mathf.PI * 9.5f) * 3.4f;
            return GetOriginalRoadCenter(RoadConstants.ForestIntrusionStartZ)
                + gate * (18f + broadCurve + switchbacks + tightWiggle);
        }
        return GetOriginalRoadCenter(RoadCoord.WorldToRouteZ(z));
    }

    // ── 道路高度 Y ──

    internal static float DepartureY(float z) { return 4f; }

    internal static float HighwayY(float z) { return 2.5f + (520f - z) * 0.006f; }

    internal static float ForestY(float z)
    {
        float t = (180f - z) / 60f;
        return 4f + t * 4f;
    }

    internal static float LakeY(float z) { return 8f; }

    internal static float AscentY(float z)
    {
        float t = -z;
        float rX = AscentZ(z);
        return 8f + 0.25f * t + 0.08f * rX;
    }

    internal static float RidgeY(float z)
    {
        float t = -240f - z;
        return 68f + t * 0.05f;
    }

    internal static float EndY(float z) { return 71f; }

    internal static float VillageApproachY(float z)
    {
        float t = (-340f - z) / 160f;
        return 71f + t * 9f;
    }

    internal static float VillageY(float z)
    {
        float t = (-500f - z) / 200f;
        return 80f + t * 6f + Mathf.Sin(t * Mathf.PI * 2f) * 1.5f;
    }

    internal static float MountainRoadY(float z)
    {
        float t = MountainRoadT(z);
        return VillageY(RoadConstants.MountainRoadStartZ)
            + t * 132f
            + Mathf.Sin(t * Mathf.PI * 7.5f) * 4.8f;
    }

    internal static float SummitY(float z) { return 208f; }

    internal static float GetOriginalRoadHeight(float z)
    {
        if (z >= 280f)
        {
            if (z >= 300f) return HighwayY(z);
            return SmoothMix(DepartureY(z), HighwayY(z), z, 280f, 300f);
        }
        if (z >= 180f)
        {
            if (z >= 200f) return DepartureY(z);
            return SmoothMix(ForestY(z), DepartureY(z), z, 180f, 200f);
        }
        if (z >= 120f)
        {
            if (z >= 140f) return ForestY(z);
            return SmoothMix(LakeY(z), ForestY(z), z, 120f, 140f);
        }
        if (z >= 0f)
        {
            if (z >= 20f) return LakeY(z);
            return SmoothMix(AscentY(z), LakeY(z), z, 0f, 20f);
        }
        if (z >= -240f)
        {
            if (z >= -220f) return AscentY(z);
            return SmoothMix(RidgeY(z), AscentY(z), z, -240f, -220f);
        }
        if (z >= -300f) return RidgeY(z);
        if (z >= -500f)
        {
            if (z >= -360f) return SmoothMix(VillageApproachY(z), EndY(z), z, -500f, -360f);
            return VillageApproachY(z);
        }
        if (z >= -700f)
        {
            if (z >= -520f) return SmoothMix(VillageY(z), VillageApproachY(z), z, -700f, -520f);
            return VillageY(z);
        }
        if (z >= RoadConstants.MountainRoadEndZ)
        {
            if (z >= RoadConstants.MountainRoadEntryEndZ)
                return SmoothMix(MountainRoadY(z), VillageY(z), z,
                    RoadConstants.MountainRoadEntryEndZ, RoadConstants.MountainRoadStartZ);
            return MountainRoadY(z);
        }
        return SummitY(z);
    }

    internal static float BakeRoadHeight(float z)
    {
        if (RoadCoord.IsForestIntrusionZ(z))
        {
            float t = ForestIntrusionT(z);
            float basePath = GetOriginalRoadHeight(RoadConstants.ForestIntrusionStartZ);
            float climb = Mathf.Sin(t * Mathf.PI) * 6.8f;
            float rolling = Mathf.Sin(t * Mathf.PI * 4.2f - 0.4f) * 2.8f
                + Mathf.Sin(t * Mathf.PI * 9.2f) * 1.25f;
            float dip = -Mathf.Max(0f, Mathf.Sin((t - 0.58f) * Mathf.PI * 2.2f)) * 2.4f;
            return basePath + climb + (rolling + dip) * Mathf.Sin(t * Mathf.PI);
        }
        return GetOriginalRoadHeight(RoadCoord.WorldToRouteZ(z));
    }

    // ── 道路宽度 ──

    internal static float DepartureW() { return 10f; }
    internal static float ForestW() { return 10f; }
    internal static float LakeW() { return 10f; }

    internal static float AscentW(float z)
    {
        float t = -z / 240f;
        return 10f - t * 4f;
    }

    internal static float RidgeW() { return 6f; }

    internal static float EndW(float z)
    {
        if (z > -340f)
        {
            float t = (-z - 300f) / 40f;
            return 6f - t * 2.5f;
        }
        return 3.5f;
    }

    internal static float HighwayW() { return 13f; }
    internal static float VillageW() { return 8f; }

    internal static float MountainW(float z)
    {
        float t = MountainRoadT(z);
        return 6.2f - t * 1.35f;
    }

    internal static float BakeRoadWidth(float z)
    {
        if (RoadCoord.IsForestIntrusionZ(z)) return 4.2f;
        float routeZ = RoadCoord.WorldToRouteZ(z);
        if (routeZ >= 280f) return HighwayW();
        if (routeZ >= 180f) return DepartureW();
        if (routeZ >= 120f) return ForestW();
        if (routeZ >= 0f) return LakeW();
        if (routeZ >= -240f) return AscentW(routeZ);
        if (routeZ >= -300f) return RidgeW();
        if (routeZ >= -500f) return 6f;
        if (routeZ >= -700f) return VillageW();
        if (routeZ >= RoadConstants.MountainRoadEndZ) return MountainW(routeZ);
        return 10f;
    }

    internal static float BakeMountainBedHalfWidth(float z)
    {
        float routeZ = RoadCoord.WorldToRouteZ(z);
        if (routeZ > RoadConstants.MountainRoadStartZ || routeZ < RoadConstants.MountainRoadEndZ) return 0f;
        float t = MountainRoadT(routeZ);
        if (routeZ <= RoadConstants.MountainRoadClearStartZ && routeZ >= RoadConstants.MountainRoadEndZ) return 7.6f;
        return 6.8f - t * 0.35f;
    }
}

// ═══════════════════════════════════════════════════════════════
// RoadSplineData — 预计算查找表
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// 道路样条数据：预计算查找表，提供 O(log n) 的道路数据查询。
/// 在构造时完成所有烘焙计算，运行时只读取。
/// </summary>
public class RoadSplineData
{
    /// <summary>采样点数组（按 Z 降序排列）</summary>
    public readonly RoadSample[] samples;

    /// <summary>采样点总数</summary>
    public readonly int count;

    /// <summary>
    /// 构造函数：烘焙所有道路数据到查找表
    /// </summary>
    public RoadSplineData()
    {
        count = Mathf.CeilToInt(
            (RoadConstants.RoadZMax - RoadConstants.RoadZMin) / RoadConstants.SampleStep) + 1;
        samples = new RoadSample[count];

        // 阶段 1：烘焙所有采样点
        for (int i = 0; i < count; i++)
        {
            float z = RoadConstants.RoadZMax - i * RoadConstants.SampleStep;
            float routeZ = RoadCoord.WorldToRouteZ(z);
            float centerX = RoadBake.BakeRoadCenter(z);
            float height = RoadBake.BakeRoadHeight(z);
            float width = RoadBake.BakeRoadWidth(z);
            bool isForestRoad = RoadCoord.IsForestIntrusionZ(z);
            bool isMountainRoad = routeZ <= RoadConstants.MountainRoadStartZ
                && routeZ >= RoadConstants.MountainRoadEndZ;
            float mountainBedHalfWidth = isMountainRoad
                ? RoadBake.BakeMountainBedHalfWidth(z)
                : 0f;

            samples[i] = new RoadSample
            {
                z = z,
                centerX = centerX,
                height = height,
                width = width,
                halfWidth = width / 2f,
                direction = 0f, // 在阶段 2 中计算
                routeZ = routeZ,
                isMountainRoad = isMountainRoad,
                isForestRoad = isForestRoad,
                mountainBedHalfWidth = mountainBedHalfWidth,
            };
        }

        // 阶段 2：从相邻采样点计算道路方向
        for (int i = 0; i < count; i++)
        {
            RoadSample curr = samples[i];
            RoadSample next = i < count - 1 ? samples[i + 1] : curr;
            float dz = next.z - curr.z;
            float dx = next.centerX - curr.centerX;
            curr.direction = Mathf.Atan2(dx, -dz);
            samples[i] = curr; // struct 是值类型，需写回
        }
    }

    /// <summary>
    /// 二分查找最接近 z 的采样点索引。
    /// 数组按 Z 降序排列（samples[0].z = RoadZMax）。
    /// </summary>
    private int FindIndex(float z)
    {
        int lo = 0;
        int hi = count - 1;
        while (lo < hi)
        {
            int mid = (lo + hi + 1) >> 1;
            if (samples[mid].z <= z) hi = mid - 1;
            else lo = mid;
        }
        // lo 是 samples[lo].z > z 的索引，限制在有效插值范围内
        return Mathf.Max(0, Mathf.Min(count - 2, lo));
    }

    /// <summary>
    /// 在指定世界 Z 坐标处采样道路数据。O(log n) 查找 + 插值。
    /// </summary>
    public RoadSample Sample(float z)
    {
        int idx = FindIndex(z);
        RoadSample a = samples[idx];
        RoadSample b = samples[idx + 1];

        // 精确匹配
        if (Mathf.Abs(z - a.z) < 0.001f) return a;
        if (Mathf.Abs(z - b.z) < 0.001f) return b;

        // 插值因子（数组降序，a.z > b.z）
        float t = Mathf.Max(0f, Mathf.Min(1f, (a.z - z) / (a.z - b.z)));
        float st = t * t * (3f - 2f * t); // smoothstep 用于位置/高度

        return new RoadSample
        {
            z = z,
            centerX = a.centerX + (b.centerX - a.centerX) * st,
            height = a.height + (b.height - a.height) * st,
            width = a.width + (b.width - a.width) * t,
            halfWidth = (a.halfWidth + b.halfWidth) * 0.5f,
            direction = a.direction + (b.direction - a.direction) * t,
            routeZ = a.routeZ + (b.routeZ - a.routeZ) * t,
            isMountainRoad = a.isMountainRoad,
            isForestRoad = a.isForestRoad,
            mountainBedHalfWidth = (a.mountainBedHalfWidth + b.mountainBedHalfWidth) * 0.5f,
        };
    }

    /// <summary>
    /// 批量采样，用于网格生成。
    /// 返回在规则 Z 间隔处的预计算采样点。
    /// 比循环调用 Sample() 更快，因为线性遍历数组。
    /// </summary>
    public RoadSample[] SampleRange(float zStart, float zEnd, int segmentCount)
    {
        var result = new RoadSample[segmentCount + 1];
        for (int i = 0; i <= segmentCount; i++)
        {
            float t = (float)i / segmentCount;
            float z = zStart + (zEnd - zStart) * t;
            result[i] = Sample(z);
        }
        return result;
    }

    /// <summary>
    /// 查找距离任意 (x, z) 位置最近的道路点。
    /// 用于带状道路逻辑（山路/森林段道路急弯处）。
    /// 在查询点的 Z 范围内搜索。
    /// </summary>
    /// <param name="x">查询点 X</param>
    /// <param name="z">查询点 Z</param>
    /// <param name="searchRadius">搜索半径（默认 28）</param>
    /// <param name="step">搜索步长（默认 2）</param>
    /// <returns>最近采样点及距离平方，未找到返回 null</returns>
    public (RoadSample sample, float distSq)? ClosestRoadPoint(float x, float z, float searchRadius = 28f, float step = 2f)
    {
        float bestDistSq = float.PositiveInfinity;
        int bestIdx = -1;

        int startIdx = FindIndex(z + searchRadius);
        int endIdx = FindIndex(z - searchRadius);

        int stepIndices = Mathf.Max(1, Mathf.RoundToInt(step / RoadConstants.SampleStep));

        for (int i = Mathf.Max(0, startIdx); i <= Mathf.Min(count - 1, endIdx); i += stepIndices)
        {
            RoadSample s = samples[i];
            float dx = x - s.centerX;
            float dz = z - s.z;
            float distSq = dx * dx + dz * dz;
            if (distSq < bestDistSq)
            {
                bestDistSq = distSq;
                bestIdx = i;
            }
        }

        if (bestIdx < 0) return null;
        return (samples[bestIdx], bestDistSq);
    }
}

// ═══════════════════════════════════════════════════════════════
// RoadSpline — MonoBehaviour 单例
// ═══════════════════════════════════════════════════════════════

/// <summary>
/// 道路样条 MonoBehaviour：在 Awake() 中初始化样条数据，
/// 提供全局单例访问，并封装便捷查询方法。
/// </summary>
public class RoadSpline : MonoBehaviour
{
    /// <summary>全局单例实例</summary>
    public static RoadSpline Instance { get; private set; }

    /// <summary>预计算的道路样条数据</summary>
    public RoadSplineData Data { get; private set; }

    private void Awake()
    {
        // 单例设置
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 烘焙道路数据
        Data = new RoadSplineData();
    }

    // ═══════════════════════════════════════════════════════════
    // 便捷查询方法（与 TypeScript 版本向后兼容的 API）
    // ═══════════════════════════════════════════════════════════

    /// <summary>
    /// 获取指定 Z 坐标处的道路中心 X
    /// </summary>
    public static float GetRoadCenter(float z)
    {
        return Instance.Data.Sample(z).centerX;
    }

    /// <summary>
    /// 获取指定 Z 坐标处的道路高度 Y
    /// </summary>
    public static float GetRoadHeight(float z)
    {
        return Instance.Data.Sample(z).height;
    }

    /// <summary>
    /// 获取指定 Z 坐标处的道路宽度
    /// </summary>
    public static float GetRoadWidth(float z)
    {
        return Instance.Data.Sample(z).width;
    }

    /// <summary>
    /// 获取指定 Z 坐标处的山路路基半宽
    /// </summary>
    public static float GetMountainRoadBedHalfWidth(float z)
    {
        return Instance.Data.Sample(z).mountainBedHalfWidth;
    }
}
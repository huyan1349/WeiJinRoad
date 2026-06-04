using UnityEngine;

namespace WeiJinRoad.World
{
    /// <summary>
    /// 地表类型枚举
    /// </summary>
    public enum SurfaceType
    {
        /// <summary>碎石</summary>
        Gravel,
        /// <summary>泥土</summary>
        Dirt,
        /// <summary>雪</summary>
        Snow,
        /// <summary>冰</summary>
        Ice,
        /// <summary>悬崖</summary>
        Cliff
    }

    /// <summary>
    /// 道路采样数据结构，存储某一Z坐标处的道路信息
    /// </summary>
    public struct RoadSample
    {
        /// <summary>世界Z坐标</summary>
        public float Z;
        /// <summary>道路中心X</summary>
        public float CenterX;
        /// <summary>道路高度Y</summary>
        public float Height;
        /// <summary>道路宽度</summary>
        public float Width;
        /// <summary>道路半宽（宽度/2）</summary>
        public float HalfWidth;
        /// <summary>道路方向角（XZ平面，弧度）</summary>
        public float Direction;
        /// <summary>路线空间Z坐标</summary>
        public float RouteZ;
        /// <summary>是否在山路段</summary>
        public bool IsMountainRoad;
        /// <summary>是否在森林侵入段</summary>
        public bool IsForestRoad;
        /// <summary>山路路基半宽（非山路为0）</summary>
        public float MountainBedHalfWidth;
    }

    /// <summary>
    /// 最近道路点查询结果
    /// </summary>
    public struct ClosestRoadPointResult
    {
        /// <summary>采样数据</summary>
        public RoadSample Sample;
        /// <summary>距离平方</summary>
        public float DistSq;
        /// <summary>是否找到有效结果</summary>
        public bool Found;
    }

    /// <summary>
    /// 道路采样器接口，提供道路数据的查询能力
    /// </summary>
    public interface IRoadSampler
    {
        /// <summary>
        /// 在指定世界Z坐标采样道路数据
        /// </summary>
        /// <param name="z">世界Z坐标</param>
        /// <returns>道路采样数据</returns>
        RoadSample Sample(float z);

        /// <summary>
        /// 查找距离指定位置最近的道路点
        /// </summary>
        /// <param name="x">世界X坐标</param>
        /// <param name="z">世界Z坐标</param>
        /// <param name="searchRadius">搜索半径</param>
        /// <param name="step">搜索步长</param>
        /// <returns>查询结果</returns>
        ClosestRoadPointResult ClosestRoadPoint(float x, float z, float searchRadius, float step);
    }

    /// <summary>
    /// 地形高度系统
    ///
    /// Unity风格TerrainRoadCutter架构：
    /// 1. 计算自然地形高度（无道路）
    /// 2. 从RoadSampler采样道路数据（唯一数据源）
    /// 3. 应用道路切割（沿道路走廊压平地形）
    ///
    /// 所有道路数据查询通过RoadSampler进行，保证一致性。
    /// </summary>
    public static class TerrainHeight
    {
        // =================================================================
        // 道路采样器引用
        // =================================================================

        private static IRoadSampler _roadSampler;

        /// <summary>
        /// 道路采样器实例，使用前必须设置
        /// </summary>
        public static IRoadSampler RoadSampler
        {
            get => _roadSampler;
            set => _roadSampler = value;
        }

        // =================================================================
        // 常量 - 与 TypeScript 版本完全一致
        // =================================================================

        /// <summary>森林侵入起始Z</summary>
        public const float ForestIntrusionStartZ = 292.9f;
        /// <summary>森林侵入长度</summary>
        public const float ForestIntrusionLength = 300f;
        /// <summary>森林侵入结束Z</summary>
        public const float ForestIntrusionEndZ = ForestIntrusionStartZ - ForestIntrusionLength;
        /// <summary>山路起始Z</summary>
        public const float MountainRoadStartZ = -700f;
        /// <summary>山路入口结束Z</summary>
        public const float MountainRoadEntryEndZ = -760f;
        /// <summary>山路清晰起始Z</summary>
        public const float MountainRoadClearStartZ = -803.6f;
        /// <summary>山路结束Z</summary>
        public const float MountainRoadEndZ = -1480f;
        /// <summary>山顶路线Z</summary>
        public const float SummitRouteZ = -1532f;

        // =================================================================
        // 坐标转换
        // =================================================================

        /// <summary>
        /// 路线空间Z转世界空间Z
        /// </summary>
        /// <param name="routeZ">路线空间Z坐标</param>
        /// <returns>世界空间Z坐标</returns>
        public static float RouteToWorldZ(float routeZ)
        {
            return routeZ < ForestIntrusionStartZ ? routeZ - ForestIntrusionLength : routeZ;
        }

        /// <summary>
        /// 世界空间Z转路线空间Z
        /// </summary>
        /// <param name="worldZ">世界空间Z坐标</param>
        /// <returns>路线空间Z坐标</returns>
        public static float WorldToRouteZ(float worldZ)
        {
            return worldZ <= ForestIntrusionEndZ ? worldZ + ForestIntrusionLength : worldZ;
        }

        /// <summary>
        /// 判断Z是否在森林侵入区域
        /// </summary>
        /// <param name="z">世界Z坐标</param>
        /// <returns>是否在森林侵入区域</returns>
        public static bool IsForestIntrusionZ(float z)
        {
            return z <= ForestIntrusionStartZ && z > ForestIntrusionEndZ;
        }

        // =================================================================
        // 带状道路辅助函数
        // 用于山路/森林段道路急弯处，搜索最近道路点而非直接用Z查询
        // =================================================================

        /// <summary>
        /// 获取山路带状道路高度
        /// 在山路急弯处搜索最近道路点，若在路基范围内则返回路基高度
        /// </summary>
        /// <param name="x">世界X坐标</param>
        /// <param name="z">世界Z坐标</param>
        /// <returns>道路高度，若不在范围内返回null</returns>
        private static float? GetMountainRibbonRoadHeight(float x, float z)
        {
            var result = _roadSampler.ClosestRoadPoint(x, z, 28f, 2f);
            if (!result.Found) return null;
            if (!result.Sample.IsMountainRoad) return null;
            float clearWidth = result.Sample.MountainBedHalfWidth + 2.2f;
            return result.DistSq <= clearWidth * clearWidth ? result.Sample.Height - 0.08f : (float?)null;
        }

        /// <summary>
        /// 获取森林带状道路高度
        /// 在森林急弯处搜索最近道路点，若在道路范围内则返回道路高度
        /// </summary>
        /// <param name="x">世界X坐标</param>
        /// <param name="z">世界Z坐标</param>
        /// <returns>道路高度，若不在范围内返回null</returns>
        private static float? GetForestRibbonRoadHeight(float x, float z)
        {
            var result = _roadSampler.ClosestRoadPoint(x, z, 18f, 1.5f);
            if (!result.Found) return null;
            if (!result.Sample.IsForestRoad) return null;
            float halfWidth = result.Sample.HalfWidth + 0.62f;
            return result.DistSq <= halfWidth * halfWidth ? result.Sample.Height : (float?)null;
        }

        // =================================================================
        // 自然地形高度（无道路切割）
        // =================================================================

        /// <summary>
        /// 森林侵入区域进度参数（0~1）
        /// </summary>
        private static float ForestIntrusionT(float z)
        {
            return Mathf.Max(0f, Mathf.Min(1f, (ForestIntrusionStartZ - z) / ForestIntrusionLength));
        }

        /// <summary>
        /// 计算自然地形高度（不考虑道路切割）
        /// 根据路线位置分为多个区域，每个区域有不同的地形生成逻辑
        /// </summary>
        /// <param name="x">世界X坐标</param>
        /// <param name="z">世界Z坐标</param>
        /// <param name="road">道路采样数据</param>
        /// <returns>自然地形高度</returns>
        private static float GetNaturalTerrainHeight(float x, float z, RoadSample road)
        {
            float rX = road.CenterX;
            float rY = road.Height;
            float routeZ = road.RouteZ;
            float baseNoise = Noise.Fbm(x * 0.01f + 1000f, z * 0.01f + 1000f, 4) * 15f;

            // ── 森林侵入区域 ──
            if (IsForestIntrusionZ(z))
            {
                float t = ForestIntrusionT(z);
                float close = Mathf.Max(0f, 1f - Mathf.Abs(x - rX) / 24f);
                float bank = (x - rX) * Mathf.Sin(t * Mathf.PI * 5.2f) * 0.095f;
                float snowMounds = Noise.RidgedNoise(x * 0.075f + 6f, z * 0.055f - 3f, 4) * 4.1f;
                float crossCuts = Mathf.Sin(x * 0.18f + z * 0.055f) * 1.1f
                    + Noise.RidgedNoise(x * 0.16f - 8f, z * 0.11f + 4f, 2) * 1.6f;
                return rY + baseNoise * 0.78f + snowMounds + crossCuts + bank
                    + close * (2.9f + Mathf.Sin(t * Mathf.PI * 4.2f) * 1.6f);
            }

            // ── 高速公路段（routeZ > 280）──
            if (routeZ > 280f)
            {
                return rY + baseNoise * 0.25f + Mathf.Sin(x * 0.015f) * 2f;
            }

            // ── 森林段（routeZ > 120）──
            if (routeZ > 120f)
            {
                return rY + baseNoise * 0.8f + Mathf.Sin(x * 0.03f) * 5f;
            }

            // ── 湖泊区域（routeZ > 0）──
            if (routeZ > 0f)
            {
                const float lakeZ = 60f;
                const float lakeX = 40f;
                float dx = x - lakeX;
                float dz = routeZ - lakeZ;
                float r = Mathf.Sqrt(dx * dx + dz * dz);
                if (r < 40f)
                {
                    return 6f; // 湖面冰面
                }
                float blend = Mathf.Min((r - 40f) / 15f, 1.0f);
                float groundY = 8f + baseNoise * 0.5f + Mathf.Sin(x * 0.02f) * 5f;
                return 6f + (groundY - 6f) * blend;
            }

            // ── 上升段（routeZ > -240）──
            if (routeZ > -240f)
            {
                float t = -routeZ;
                float baseY = 8f + 0.25f * t + 0.08f * x;
                return baseY + Noise.RidgedNoise(x * 0.015f, z * 0.015f, 4) * 12f;
            }

            // ── 山脊段（routeZ > -300）──
            if (routeZ > -300f)
            {
                float t = -240f - routeZ;
                float baseY = 68f + t * 0.05f + 0.08f * x;
                if (x > rX)
                {
                    return baseY + (x - rX) * 0.2f + Noise.Fbm(x * 0.1f, z * 0.1f, 2) * 6f;
                }
                return baseY - Mathf.Min(rX - x, 5f) * 1.0f - (rX - x - 5f) * 2.5f
                    - Noise.Fbm(x * 0.1f, z * 0.1f, 2) * 5f;
            }

            // ── 过渡段（routeZ > -500）──
            if (routeZ > -500f)
            {
                float t = (-300f - routeZ) / 200f;
                float y = 71f + t * 9f + Noise.Fbm(x * 0.03f, z * 0.03f, 4) * 10f;
                if (routeZ < -340f && routeZ > -360f)
                {
                    float cZ = -350f + Mathf.Sin(x * 0.2f) * 2f;
                    float cDist = Mathf.Abs(routeZ - cZ);
                    if (cDist < 4f)
                    {
                        y -= (4f - cDist) * 15f;
                    }
                }
                return y;
            }

            // ── 村落段（routeZ > -700）──
            if (routeZ > -700f)
            {
                bool villagePad = Mathf.Abs(x - rX) < 95f;
                float snowRipples = Noise.Fbm(x * 0.04f + 4f, z * 0.04f, 3) * (villagePad ? 2.2f : 7f);
                return rY + snowRipples + Mathf.Abs(x - rX) * 0.018f;
            }

            // ── 山路段（routeZ > MountainRoadEndZ）──
            if (routeZ > MountainRoadEndZ)
            {
                float t = Mathf.Max(0f, Mathf.Min(1f,
                    (MountainRoadStartZ - routeZ) / (MountainRoadStartZ - MountainRoadEndZ)));
                float rightDist = x - rX;
                float leftDist = rX - x;
                float away = Mathf.Min(1f, Mathf.Max(0f,
                    (Mathf.Abs(x - rX) - road.HalfWidth - 6f) / 46f));
                float snowRidges = Noise.RidgedNoise(x * 0.024f, z * 0.02f, 4) * (10f + t * 8f);
                float windCuts = Mathf.Sin(x * 0.07f + z * 0.026f) * (2.2f + t * 2.6f) * away;
                float cliffEdge = Mathf.Max(0f, rightDist - road.HalfWidth - 1.2f);
                float cliffDrop = -Mathf.Pow(cliffEdge, 1.18f) * (0.42f + t * 0.55f);
                float mountainEdge = Mathf.Max(0f, leftDist - road.HalfWidth - 2.5f);
                float mountainWall = mountainEdge * (0.32f + t * 0.32f)
                    + Noise.RidgedNoise(x * 0.052f - 12f, z * 0.045f + 8f, 3) * 5f * away;
                float roadBank = rightDist * (0.035f + t * 0.035f);
                return rY + snowRidges + windCuts + cliffDrop + mountainWall + roadBank;
            }

            // ── 山顶 ──
            {
                float dx = x - rX;
                float dz = routeZ - SummitRouteZ;
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                float summitDetail = Noise.RidgedNoise(x * 0.045f + 12f, z * 0.045f, 3) * 3.2f
                    + Noise.RidgedNoise(x * 0.105f - 8f, z * 0.088f + 14f, 2) * 2.4f
                    + Mathf.Sin(x * 0.06f - z * 0.018f) * 1.5f;
                return 208f + Mathf.Min(d * 0.05f, 14f)
                    + Noise.Fbm(x * 0.02f, z * 0.02f, 3) * 2f + summitDetail;
            }
        }

        // =================================================================
        // TerrainRoadCutter - 对地形应用道路切割
        // =================================================================

        /// <summary>
        /// 获取地形高度（含道路切割）
        ///
        /// 计算流程：
        /// 1. 从RoadSampler采样道路数据（唯一数据源）
        /// 2. 检查带状道路（山路/森林急弯）
        /// 3. 计算自然地形高度
        /// 4. 应用道路切割（路面、路肩混合）
        /// </summary>
        /// <param name="x">世界X坐标</param>
        /// <param name="z">世界Z坐标</param>
        /// <returns>地形高度</returns>
        public static float GetTerrainHeight(float x, float z)
        {
            // 第1步：从唯一数据源采样道路数据
            var road = _roadSampler.Sample(z);
            float rX = road.CenterX;
            float rY = road.Height;
            float rHW = road.HalfWidth;
            float dist = Mathf.Abs(x - rX);
            float routeZ = road.RouteZ;

            // 第2步：检查带状道路（山路/森林急弯）
            float? forestRibbonHeight = GetForestRibbonRoadHeight(x, z);
            if (forestRibbonHeight.HasValue) return forestRibbonHeight.Value;

            // 森林侵入边缘混合
            if (road.IsForestRoad && dist < rHW + 2.8f)
            {
                float t = Mathf.Max(0f, Mathf.Min(1f, (dist - (rHW + 0.62f)) / 2.18f));
                float st = t * t * (3f - 2f * t);
                return rY + st * 0.18f;
            }

            // 第3步：计算自然地形高度
            float naturalY = GetNaturalTerrainHeight(x, z, road);

            // 第4步：应用道路切割

            // 山路带状检查
            float? mountainRibbonHeight = GetMountainRibbonRoadHeight(x, z);
            if (mountainRibbonHeight.HasValue) return mountainRibbonHeight.Value;

            // 山路路基
            if (road.IsMountainRoad && dist < road.MountainBedHalfWidth)
            {
                return rY - 0.04f;
            }

            // 路面
            if (dist < rHW)
            {
                float bump = Noise.Fbm(x, z, 3) * 0.05f;
                if (road.IsForestRoad)
                {
                    float t = ForestIntrusionT(z);
                    float lateral = (x - rX) / Mathf.Max(1f, rHW);
                    float absLateralMinus048 = Mathf.Abs(lateral) - 0.48f;
                    float ruts = -Mathf.Exp(-(absLateralMinus048 * absLateralMinus048) / 0.018f) * 0.22f;
                    float packedSnow = Mathf.Sin(t * Mathf.PI * 12f + lateral * 2.4f) * 0.1f
                        + Noise.Fbm(x * 0.14f, z * 0.16f, 3) * 0.12f;
                    bump += ruts + packedSnow;
                }
                if (routeZ < -330f && routeZ > -340f)
                {
                    bump += Mathf.Max(0f, Noise.Fbm(x * 0.5f, z * 0.5f, 3) * 2.5f - 1f);
                }
                return rY + bump;
            }

            // 路肩混合
            float shoulder = rHW + (routeZ < -500f && routeZ > -700f ? 4f : routeZ > -240f ? 5f : 2f);
            if (dist < shoulder)
            {
                float t = (dist - rHW) / (shoulder - rHW);
                float st = t * t * (3f - 2f * t);

                if (naturalY > rY)
                {
                    return rY + (naturalY - rY) * st * st;
                }
                else
                {
                    return rY - (rY - naturalY) * st;
                }
            }

            return naturalY;
        }

        // =================================================================
        // 地形法线
        // =================================================================

        /// <summary>
        /// 获取地形法线向量
        /// 使用有限差分法计算地形表面法线
        /// </summary>
        /// <param name="x">世界X坐标</param>
        /// <param name="z">世界Z坐标</param>
        /// <param name="offset">采样偏移量，默认0.5</param>
        /// <returns>归一化的地形法线向量</returns>
        public static Vector3 GetTerrainNormal(float x, float z, float offset = 0.5f)
        {
            float hL = GetTerrainHeight(x - offset, z);
            float hR = GetTerrainHeight(x + offset, z);
            float hD = GetTerrainHeight(x, z - offset);
            float hU = GetTerrainHeight(x, z + offset);
            Vector3 normal = new Vector3(hL - hR, 2f * offset, hD - hU);
            normal.Normalize();
            return normal;
        }

        // =================================================================
        // 地表类型
        // =================================================================

        /// <summary>
        /// 获取地表类型
        /// 根据位置判断地表材质：碎石、泥土、雪、冰、悬崖
        /// </summary>
        /// <param name="x">世界X坐标</param>
        /// <param name="z">世界Z坐标</param>
        /// <returns>地表类型</returns>
        public static SurfaceType GetSurfaceType(float x, float z)
        {
            var road = _roadSampler.Sample(z);
            float rX = road.CenterX;
            float rHW = road.HalfWidth;
            float routeZ = road.RouteZ;

            // 路面区域
            if (Mathf.Abs(x - rX) < rHW)
            {
                if (road.IsForestRoad) return SurfaceType.Snow;
                if (road.IsMountainRoad) return SurfaceType.Gravel;
                if (routeZ > 280f) return SurfaceType.Gravel;
                if (routeZ > 180f) return SurfaceType.Gravel;
                if (routeZ > 120f) return SurfaceType.Dirt;
                return SurfaceType.Snow;
            }

            // 湖泊冰面
            if (routeZ <= 120f && routeZ > 0f)
            {
                const float lakeZ = 60f;
                const float lakeX = 40f;
                float dx = x - lakeX;
                float dz = routeZ - lakeZ;
                if (Mathf.Sqrt(dx * dx + dz * dz) < 40f) return SurfaceType.Ice;
            }

            // 悬崖区域高密度岩石
            if (routeZ <= -240f && routeZ > -300f && x > rX + rHW)
            {
                return SurfaceType.Cliff;
            }

            return SurfaceType.Snow;
        }
    }
}

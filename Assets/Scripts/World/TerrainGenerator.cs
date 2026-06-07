using UnityEngine;
using WeiJinRoad.Core;

namespace WeiJinRoad.World
{
    // ═══════════════════════════════════════════════════════════════
    // TerrainGenerator — 程序化地形网格生成器
    //
    // 从 TypeScript 版本 Terrain.tsx 完整翻译。
    // 负责生成三大网格：地形表面、道路、护栏。
    //
    // 架构：
    // - TerrainHeight / RoadSpline 提供高度和道路数据（唯一数据源）
    // - 本组件读取数据生成 Mesh，挂载 MeshFilter + MeshRenderer
    // - 地形网格跟随车辆动态重新居中
    // - 顶点颜色用于地表类型着色
    // ═══════════════════════════════════════════════════════════════

    /// <summary>
    /// 程序化地形网格生成器
    /// 生成地形表面网格、道路网格和护栏网格，支持动态跟随车辆
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TerrainGenerator : MonoBehaviour
    {
        // =================================================================
        // 常量 — 与 TypeScript 版本完全一致
        // =================================================================

        /// <summary>世界地形总尺寸</summary>
        private const float WorldTerrainSize = 4300f;

        /// <summary>可见地形尺寸</summary>
        private const float VisibleTerrainSize = 980f;

        /// <summary>可见地形分段数</summary>
        private const int VisibleTerrainSegments = 260;

        /// <summary>地形重新居中步长</summary>
        private const float TerrainRecenterStep = 140f;

        /// <summary>地形重新居中触发距离</summary>
        private const float TerrainRecenterDistance = 170f;

        /// <summary>走廊区域 Z 起始</summary>
        private const float CorridorZStart = 540f;

        /// <summary>走廊区域 Z 结束</summary>
        private const float CorridorZEnd = -1860f;

        /// <summary>走廊宽度</summary>
        private const float CorridorWidth = 260f;

        /// <summary>走廊 Z 分段数</summary>
        private const int CorridorZSegments = 360;

        /// <summary>走廊 X 分段数</summary>
        private const int CorridorXSegments = 42;

        /// <summary>道路网格分段数</summary>
        private const int RoadSegments = 400;

        /// <summary>护栏网格分段数</summary>
        private const int GuardrailSegments = 320;

        // =================================================================
        // 地表颜色 — 与 TypeScript 版本完全一致
        // =================================================================

        private static readonly Color ColorGravel  = HexToColor("#6a6b63");
        private static readonly Color ColorDirt    = HexToColor("#4a443a");
        private static readonly Color ColorAsphalt = HexToColor("#4b4f54");
        private static readonly Color ColorSnow    = HexToColor("#e0e8f0");
        private static readonly Color ColorIce     = HexToColor("#88b5ba");
        private static readonly Color ColorCliff   = HexToColor("#42454a");

        // 高海拔雪面调色
        private static readonly Color ColorSnowRidge  = HexToColor("#f6fbff");
        private static readonly Color ColorSnowShadow = HexToColor("#b9c8d6");

        // =================================================================
        // Inspector 配置
        // =================================================================

        [Header("材质")]
        [Tooltip("地形表面材质（需支持顶点颜色）")]
        [SerializeField] private Material _terrainMaterial;

        [Tooltip("道路材质")]
        [SerializeField] private Material _roadMaterial;

        [Tooltip("护栏材质")]
        [SerializeField] private Material _guardrailMaterial;

        [Header("跟随目标")]
        [Tooltip("车辆 Transform，为空则自动查找")]
        [SerializeField] private Transform _vehicleTransform;

        [Header("调试")]
        [Tooltip("是否在 Scene 视图显示网格边界")]
        [SerializeField] private bool _debugShowBounds;

        // =================================================================
        // 运行时状态
        // =================================================================

        /// <summary>当前地形中心（x=世界X, y=世界Z）</summary>
        private Vector2 _terrainCenter;

        /// <summary>地形网格对象</summary>
        private Mesh _terrainMesh;

        /// <summary>道路网格对象</summary>
        private Mesh _roadMesh;

        /// <summary>护栏网格对象</summary>
        private Mesh _guardrailMesh;

        /// <summary>道路纹理</summary>
        private Texture2D _roadTexture;

        /// <summary>子 GameObject 容器</summary>
        private GameObject _terrainGO;
        private GameObject _roadGO;
        private GameObject _guardrailGO;

        private MeshFilter _terrainMF;
        private MeshFilter _roadMF;
        private MeshFilter _guardrailMF;

        private MeshRenderer _terrainMR;
        private MeshRenderer _roadMR;
        private MeshRenderer _guardrailMR;

        /// <summary>上一帧的渲染模式，用于检测变化</summary>
        private TerrainRenderMode _lastRenderMode;

        // =================================================================
        // Unity 生命周期
        // =================================================================

        private void Awake()
        {
            // 查找车辆
            if (_vehicleTransform == null)
            {
                var vc = FindFirstObjectByType<VehicleController>();
                if (vc != null) _vehicleTransform = vc.transform;
            }

            // 初始化道路采样器引用
            if (TerrainHeight.RoadSampler == null)
            {
                var rs = FindFirstObjectByType<RoadSpline>();
                if (rs != null) TerrainHeight.RoadSampler = rs.Data;
            }

            // 创建子对象
            CreateChildObjects();

            // 创建默认材质（如果未指定）
            EnsureMaterials();

            // 生成道路纹理
            GenerateRoadTexture();

            // 初始地形中心
            _terrainCenter = GetVisibleTerrainCenter();

            // 生成所有网格
            RebuildAllMeshes();
        }

        private void Update()
        {
            // 检查渲染模式变化
            var currentMode = GetCurrentRenderMode();
            if (currentMode != _lastRenderMode)
            {
                _lastRenderMode = currentMode;
                RebuildAllMeshes();
                return;
            }

            // 动态地形中心跟随
            if (currentMode == TerrainRenderMode.Visible)
            {
                var next = GetVisibleTerrainCenter();
                if (Mathf.Abs(next.x - _terrainCenter.x) > TerrainRecenterDistance ||
                    Mathf.Abs(next.y - _terrainCenter.z) > TerrainRecenterDistance)
                {
                    _terrainCenter = next;
                    RebuildTerrainMesh();
                }
            }
        }

        private void OnDestroy()
        {
            DestroyMesh(ref _terrainMesh);
            DestroyMesh(ref _roadMesh);
            DestroyMesh(ref _guardrailMesh);
            if (_roadTexture != null) Destroy(_roadTexture);
            if (_terrainGO != null) Destroy(_terrainGO);
            if (_roadGO != null) Destroy(_roadGO);
            if (_guardrailGO != null) Destroy(_guardrailGO);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_debugShowBounds) return;

            var mode = GetCurrentRenderMode();
            Gizmos.color = Color.green;

            if (mode == TerrainRenderMode.Visible)
            {
                Vector3 center = new Vector3(_terrainCenter.x, 0, _terrainCenter.y);
                Gizmos.DrawWireCube(center, new Vector3(VisibleTerrainSize, 10, VisibleTerrainSize));
            }
            else if (mode == TerrainRenderMode.Full)
            {
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(WorldTerrainSize, 10, WorldTerrainSize));
            }
        }
#endif

        // =================================================================
        // 子对象创建
        // =================================================================

        private void CreateChildObjects()
        {
            // 地形
            _terrainGO = new GameObject("TerrainMesh");
            _terrainGO.transform.SetParent(transform, false);
            _terrainMF = _terrainGO.AddComponent<MeshFilter>();
            _terrainMR = _terrainGO.AddComponent<MeshRenderer>();
            _terrainMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            _terrainMR.receiveShadows = true;

            // 道路
            _roadGO = new GameObject("RoadMesh");
            _roadGO.transform.SetParent(transform, false);
            _roadMF = _roadGO.AddComponent<MeshFilter>();
            _roadMR = _roadGO.AddComponent<MeshRenderer>();
            _roadMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _roadMR.receiveShadows = true;

            // 护栏
            _guardrailGO = new GameObject("GuardrailMesh");
            _guardrailGO.transform.SetParent(transform, false);
            _guardrailMF = _guardrailGO.AddComponent<MeshFilter>();
            _guardrailMR = _guardrailGO.AddComponent<MeshRenderer>();
            _guardrailMR.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.TwoSided;
            _guardrailMR.receiveShadows = true;
        }

        private void EnsureMaterials()
        {
            if (_terrainMaterial == null)
            {
                _terrainMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                _terrainMaterial.EnableKeyword("_VERTEX_COLORS");
                _terrainMaterial.SetFloat("_Smoothness", 0.08f);
                _terrainMaterial.SetFloat("_Metallic", 0.05f);
            }

            if (_roadMaterial == null)
            {
                _roadMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                _roadMaterial.SetFloat("_Smoothness", 0.14f);
                _roadMaterial.SetFloat("_Metallic", 0.04f);
            }

            if (_guardrailMaterial == null)
            {
                _guardrailMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                _guardrailMaterial.color = HexToColor("#8c4e2f");
                _guardrailMaterial.SetFloat("_Smoothness", 0.2f);
                _guardrailMaterial.SetFloat("_Metallic", 0.5f);
            }
        }

        // =================================================================
        // 网格重建
        // =================================================================

        private void RebuildAllMeshes()
        {
            RebuildTerrainMesh();
            RebuildRoadMesh();
            RebuildGuardrailMesh();
        }

        private void RebuildTerrainMesh()
        {
            DestroyMesh(ref _terrainMesh);

            var mode = GetCurrentRenderMode();
            switch (mode)
            {
                case TerrainRenderMode.Full:
                    _terrainMesh = BuildTerrainGeometry(WorldTerrainSize, 520, Vector2.zero);
                    break;
                case TerrainRenderMode.Corridor:
                    _terrainMesh = BuildCorridorTerrainGeometry();
                    break;
                default: // Visible
                    _terrainMesh = BuildTerrainGeometry(VisibleTerrainSize, VisibleTerrainSegments, _terrainCenter);
                    break;
            }

            _terrainMF.mesh = _terrainMesh;
            _terrainMR.material = _terrainMaterial;
        }

        private void RebuildRoadMesh()
        {
            DestroyMesh(ref _roadMesh);
            _roadMesh = BuildRoadGeometry();
            _roadMF.mesh = _roadMesh;

            if (_roadTexture != null && _roadMaterial != null)
            {
                _roadMaterial.mainTexture = _roadTexture;
            }
            _roadMR.material = _roadMaterial;
        }

        private void RebuildGuardrailMesh()
        {
            DestroyMesh(ref _guardrailMesh);
            _guardrailMesh = BuildGuardrailGeometry();
            _guardrailMF.mesh = _guardrailMesh;
            _guardrailMR.material = _guardrailMaterial;
        }

        // =================================================================
        // 地形中心计算
        // =================================================================

        /// <summary>
        /// 获取可见地形中心，将车辆位置对齐到步长网格
        /// 对应 TypeScript 的 getVisibleTerrainCenter()
        /// </summary>
        private Vector2 GetVisibleTerrainCenter()
        {
            float vx = 0f, vz = 270f;
            if (_vehicleTransform != null)
            {
                vx = _vehicleTransform.position.x;
                vz = _vehicleTransform.position.z;
            }
            else if (GameManager.Instance != null)
            {
                var pos = GameManager.Instance.VehicleTransient.Position;
                vx = pos[0];
                vz = pos[1];
            }

            return new Vector2(
                Mathf.Round(vx / TerrainRecenterStep) * TerrainRecenterStep,
                Mathf.Round(vz / TerrainRecenterStep) * TerrainRecenterStep
            );
        }

        /// <summary>
        /// 获取当前地形渲染模式
        /// </summary>
        private TerrainRenderMode GetCurrentRenderMode()
        {
            if (GameManager.Instance != null)
            {
                return GameManager.Instance.TerrainRenderModeType;
            }
            return TerrainRenderMode.Visible;
        }

        // =================================================================
        // 地形表面网格生成
        // =================================================================

        /// <summary>
        /// 构建地形表面网格（方形区域）
        /// 对应 TypeScript 的 buildTerrainGeometry()
        /// </summary>
        /// <param name="size">网格尺寸</param>
        /// <param name="segments">分段数</param>
        /// <param name="center">中心坐标（x=世界X, y=世界Z）</param>
        private Mesh BuildTerrainGeometry(float size, int segments, Vector2 center)
        {
            int vertCount = (segments + 1) * (segments + 1);
            int triCount = segments * segments * 6;

            var vertices = new Vector3[vertCount];
            var colors = new Color[vertCount];
            var triangles = new int[triCount];

            float halfSize = size / 2f;
            float step = size / segments;

            // 生成顶点
            for (int iz = 0; iz <= segments; iz++)
            {
                for (int ix = 0; ix <= segments; ix++)
                {
                    int idx = iz * (segments + 1) + ix;

                    float x = -halfSize + ix * step + center.x;
                    float z = -halfSize + iz * step + center.y;

                    // 道路数据
                    float rX = RoadSpline.GetRoadCenter(z);
                    float rW = RoadSpline.GetRoadWidth(z);
                    float rHW = rW / 2f;
                    float dist = Mathf.Abs(x - rX);
                    float routeZ = RoadCoord.WorldToRouteZ(z);
                    bool isMountainRoad = routeZ <= TerrainHeight.MountainRoadStartZ
                        && routeZ >= TerrainHeight.MountainRoadEndZ;
                    bool isForestRoad = RoadCoord.IsForestIntrusionZ(z);
                    float roadBedHW = isMountainRoad
                        ? RoadSpline.GetMountainRoadBedHalfWidth(z)
                        : isForestRoad ? rHW + 2.8f : rHW + 1f;

                    // 地形高度（含道路切割）
                    float y = TerrainHeight.GetTerrainHeight(x, z);
                    if (dist < roadBedHW)
                    {
                        float roadH = RoadSpline.GetRoadHeight(z);
                        y = isMountainRoad
                            ? roadH - 0.12f
                            : isForestRoad
                                ? roadH
                                : Mathf.Min(y, roadH - 0.08f);
                    }

                    vertices[idx] = new Vector3(x, y, z);

                    // 地表类型着色
                    colors[idx] = GetSurfaceColor(x, z, y, dist, rX, rW, rHW, routeZ, isMountainRoad, isForestRoad);
                }
            }

            // 生成三角形索引
            int ti = 0;
            for (int iz = 0; iz < segments; iz++)
            {
                for (int ix = 0; ix < segments; ix++)
                {
                    int a = iz * (segments + 1) + ix;
                    int b = a + 1;
                    int c = a + (segments + 1);
                    int d = c + 1;

                    triangles[ti++] = a;
                    triangles[ti++] = c;
                    triangles[ti++] = b;
                    triangles[ti++] = b;
                    triangles[ti++] = c;
                    triangles[ti++] = d;
                }
            }

            var mesh = new Mesh
            {
                name = "TerrainSurface",
                vertices = vertices,
                triangles = triangles,
                colors = colors
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        /// <summary>
        /// 构建走廊地形网格
        /// 对应 TypeScript 的 buildCorridorTerrainGeometry()
        /// </summary>
        private Mesh BuildCorridorTerrainGeometry()
        {
            int vertCount = (CorridorZSegments + 1) * (CorridorXSegments + 1);
            int triCount = CorridorZSegments * CorridorXSegments * 6;

            var vertices = new Vector3[vertCount];
            var colors = new Color[vertCount];
            var triangles = new int[triCount];

            for (int zi = 0; zi <= CorridorZSegments; zi++)
            {
                float t = (float)zi / CorridorZSegments;
                float z = CorridorZStart + (CorridorZEnd - CorridorZStart) * t;
                float centerX = RoadSpline.GetRoadCenter(z);
                float roadW = RoadSpline.GetRoadWidth(z);

                for (int xi = 0; xi <= CorridorXSegments; xi++)
                {
                    float u = (float)xi / CorridorXSegments;
                    float lateral = (u - 0.5f) * CorridorWidth;
                    float x = centerX + lateral;
                    float dist = Mathf.Abs(lateral);
                    float rHW = roadW / 2f;
                    float routeZ = RoadCoord.WorldToRouteZ(z);
                    bool isMountainRoad = routeZ <= TerrainHeight.MountainRoadStartZ
                        && routeZ >= TerrainHeight.MountainRoadEndZ;
                    bool isForestRoad = RoadCoord.IsForestIntrusionZ(z);
                    float roadBedHW = isMountainRoad
                        ? RoadSpline.GetMountainRoadBedHalfWidth(z)
                        : isForestRoad ? rHW + 2.8f : rHW + 1f;

                    float y = TerrainHeight.GetTerrainHeight(x, z);
                    if (dist < roadBedHW)
                    {
                        float roadH = RoadSpline.GetRoadHeight(z);
                        y = isMountainRoad
                            ? roadH - 0.12f
                            : isForestRoad
                                ? roadH
                                : Mathf.Min(y, roadH - 0.08f);
                    }

                    int idx = zi * (CorridorXSegments + 1) + xi;
                    vertices[idx] = new Vector3(x, y, z);
                    colors[idx] = GetSurfaceColor(x, z, y, dist, centerX, roadW, rHW, routeZ, isMountainRoad, isForestRoad);
                }
            }

            int row = CorridorXSegments + 1;
            int ti = 0;
            for (int zi = 0; zi < CorridorZSegments; zi++)
            {
                for (int xi = 0; xi < CorridorXSegments; xi++)
                {
                    int a = zi * row + xi;
                    int b = a + 1;
                    int c = a + row;
                    int d = c + 1;

                    triangles[ti++] = a;
                    triangles[ti++] = c;
                    triangles[ti++] = b;
                    triangles[ti++] = b;
                    triangles[ti++] = c;
                    triangles[ti++] = d;
                }
            }

            var mesh = new Mesh
            {
                name = "TerrainCorridor",
                vertices = vertices,
                triangles = triangles,
                colors = colors
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // =================================================================
        // 地表类型着色
        // =================================================================

        /// <summary>
        /// 获取顶点颜色，根据地表类型和高海拔调色
        /// 对应 TypeScript 中 buildTerrainGeometry / buildCorridorTerrainGeometry 的着色逻辑
        /// </summary>
        private Color GetSurfaceColor(
            float x, float z, float y,
            float dist, float rX, float rW, float rHW,
            float routeZ, bool isMountainRoad, bool isForestRoad)
        {
            Color color;

            // ── 地表类型判定 ──
            if (isMountainRoad && dist < RoadSpline.GetMountainRoadBedHalfWidth(z))
            {
                color = ColorAsphalt;
            }
            else if (dist < rW / 2f)
            {
                if (isForestRoad) color = ColorSnow;
                else if (routeZ > 180f) color = ColorGravel;
                else if (routeZ > 120f) color = ColorDirt;
                else if (routeZ > 0f) color = ColorAsphalt;
                else if (routeZ > -240f) color = ColorAsphalt;
                else if (routeZ > -500f) color = ColorAsphalt;
                else if (routeZ > -700f) color = ColorAsphalt;
                else color = ColorAsphalt;
            }
            else if (dist < rW / 2f + 3f)
            {
                if (isForestRoad) color = ColorSnow;
                else if (routeZ > 180f) color = ColorGravel;
                else if (routeZ > 120f) color = ColorDirt;
                else if (routeZ > 0f) color = ColorDirt;
                else color = ColorDirt;
            }
            else if (routeZ <= 120f && routeZ > 0f
                && Mathf.Sqrt((x - 40f) * (x - 40f) + (routeZ - 60f) * (routeZ - 60f)) < 40f)
            {
                color = ColorIce;
            }
            else if (routeZ <= -240f && routeZ > -300f && x > rX + rW / 2f)
            {
                color = ColorCliff;
            }
            else if (routeZ < -340f && routeZ > -360f
                && Mathf.Abs(routeZ - (-350f + Mathf.Sin(x * 0.2f) * 2f)) < 4f)
            {
                color = ColorCliff;
            }
            else
            {
                color = ColorSnow;
            }

            // ── 高海拔雪面调色 ──
            if (color == ColorSnow && routeZ < -650f && dist > rW)
            {
                float ridgeTint = Mathf.Sin(x * 0.045f + z * 0.018f) * 0.5f + 0.5f;
                float facetTint = Mathf.Sin(x * 0.11f - z * 0.07f) * 0.5f + 0.5f;
                float heightTint = Mathf.Max(0f, Mathf.Min(1f, (y - 90f) / 70f));
                Color tintTarget = ridgeTint + facetTint * 0.35f > 0.78f
                    ? ColorSnowRidge
                    : ColorSnowShadow;
                float lerpFactor = 0.18f + heightTint * 0.13f;
                color = Color.Lerp(color, tintTarget, lerpFactor);
            }

            // ── 微噪点 ──
            float noise = (Random.value - 0.5f) * 0.05f;
            color.r = Mathf.Clamp01(color.r + noise);
            color.g = Mathf.Clamp01(color.g + noise);
            color.b = Mathf.Clamp01(color.b + noise);

            return color;
        }

        // =================================================================
        // 道路网格生成
        // =================================================================

        /// <summary>
        /// 构建道路表面网格
        /// 对应 TypeScript 的 roadGeometry
        /// </summary>
        private Mesh BuildRoadGeometry()
        {
            int vertCount = (RoadSegments + 1) * 2;
            int triCount = RoadSegments * 6;

            var vertices = new Vector3[vertCount];
            var uvs = new Vector2[vertCount];
            var triangles = new int[triCount];

            float zStart = 520f;
            float zEnd = RoadCoord.RouteToWorldZ(TerrainHeight.SummitRouteZ);

            for (int i = 0; i <= RoadSegments; i++)
            {
                float t = (float)i / RoadSegments;
                float z = zStart - t * (zStart - zEnd);
                float routeZ = RoadCoord.WorldToRouteZ(z);
                bool isMountainRoad = routeZ <= TerrainHeight.MountainRoadStartZ
                    && routeZ >= TerrainHeight.MountainRoadEndZ;

                float cX = RoadSpline.GetRoadCenter(z);
                bool forestBuriedRoad = RoadCoord.IsForestIntrusionZ(z);
                float cY = RoadSpline.GetRoadHeight(z)
                    + (forestBuriedRoad ? -1.2f : isMountainRoad ? 0.14f : 0.2f);
                float w = forestBuriedRoad
                    ? 0.02f
                    : isMountainRoad
                        ? Mathf.Max(RoadSpline.GetRoadWidth(z) * 0.95f, 8.2f)
                        : RoadSpline.GetRoadWidth(z) * 0.95f;

                // 计算道路方向法线
                float dz = 0.5f;
                float cX1 = RoadSpline.GetRoadCenter(z - dz);
                float dx = cX1 - cX;
                float angle = Mathf.Atan2(dx, -dz);
                float normX = Mathf.Cos(angle);
                float normZ = -Mathf.Sin(angle);
                float hw = w / 2f;

                // 左侧顶点
                int leftIdx = i * 2;
                vertices[leftIdx] = new Vector3(cX - normX * hw, cY, z - normZ * hw);
                uvs[leftIdx] = new Vector2(0f, t);

                // 右侧顶点
                int rightIdx = i * 2 + 1;
                vertices[rightIdx] = new Vector3(cX + normX * hw, cY, z + normZ * hw);
                uvs[rightIdx] = new Vector2(1f, t);
            }

            // 生成三角形
            int ti = 0;
            for (int i = 0; i < RoadSegments; i++)
            {
                int a = i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;

                triangles[ti++] = a;
                triangles[ti++] = c;
                triangles[ti++] = b;
                triangles[ti++] = b;
                triangles[ti++] = c;
                triangles[ti++] = d;
            }

            var mesh = new Mesh
            {
                name = "RoadSurface",
                vertices = vertices,
                uv = uvs,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // =================================================================
        // 护栏网格生成
        // =================================================================

        /// <summary>
        /// 构建护栏网格
        /// 对应 TypeScript 的 guardRailGeometry
        /// 沿山路和悬崖段生成连续护栏
        /// </summary>
        private Mesh BuildGuardrailGeometry()
        {
            int vertCount = (GuardrailSegments + 1) * 2;
            int triCount = GuardrailSegments * 6;

            var vertices = new Vector3[vertCount];
            var triangles = new int[triCount];

            float zStart = 0f;
            float zEnd = RoadCoord.RouteToWorldZ(TerrainHeight.MountainRoadEndZ);

            for (int i = 0; i <= GuardrailSegments; i++)
            {
                float t = (float)i / GuardrailSegments;
                float z = zStart - t * (zStart - zEnd);

                float cX = RoadSpline.GetRoadCenter(z);
                float cY = RoadSpline.GetRoadHeight(z) + 0.6f;
                float rW = RoadSpline.GetRoadWidth(z);

                // 道路方向法线
                float dz = 0.5f;
                float dx = RoadSpline.GetRoadCenter(z - dz) - cX;
                float angle = Mathf.Atan2(dx, -dz);
                float normX = Mathf.Cos(angle);
                float normZ = -Mathf.Sin(angle);

                float hw = rW / 2f + 0.12f;

                // 护栏顶部
                int topIdx = i * 2;
                vertices[topIdx] = new Vector3(cX + normX * hw, cY + 0.2f, z + normZ * hw);

                // 护栏底部
                int bottomIdx = i * 2 + 1;
                vertices[bottomIdx] = new Vector3(cX + normX * hw, cY - 0.2f, z + normZ * hw);
            }

            // 生成三角形
            int ti = 0;
            for (int i = 0; i < GuardrailSegments; i++)
            {
                int a = i * 2;
                int b = a + 1;
                int c = a + 2;
                int d = a + 3;

                triangles[ti++] = a;
                triangles[ti++] = c;
                triangles[ti++] = b;
                triangles[ti++] = b;
                triangles[ti++] = c;
                triangles[ti++] = d;
            }

            var mesh = new Mesh
            {
                name = "Guardrail",
                vertices = vertices,
                triangles = triangles
            };
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // =================================================================
        // 道路纹理生成
        // =================================================================

        /// <summary>
        /// 程序化生成道路纹理
        /// 对应 TypeScript 中 roadTexture 的 Canvas 绘制逻辑
        /// </summary>
        private void GenerateRoadTexture()
        {
            int texWidth = 512;
            int texHeight = 512;

            _roadTexture = new Texture2D(texWidth, texHeight, TextureFormat.RGB24, false);
            _roadTexture.name = "RoadTexture";

            Color32[] pixels = new Color32[texWidth * texHeight];

            // 基底沥青色 #33383d
            Color32 asphalt = HexToColor32("#33383d");
            // 左边线白色 #f3f7fb
            Color32 edgeLine = HexToColor32("#f3f7fb");
            // 中心虚线黄色 #e0aa10
            Color32 centerLine = HexToColor32("#e0aa10");

            for (int y = 0; y < texHeight; y++)
            {
                for (int x = 0; x < texWidth; x++)
                {
                    Color32 c = asphalt;

                    // 左边线 (x: 25~40)
                    if (x >= 25 && x < 40) c = edgeLine;

                    // 右边线 (x: 472~487)
                    if (x >= 472 && x < 487) c = edgeLine;

                    // 中心虚线 (x: 248~264, 每64像素画32像素)
                    int lineY = y % 64;
                    if (x >= 248 && x < 264 && lineY < 32)
                    {
                        c = centerLine;
                    }

                    pixels[y * texWidth + x] = c;
                }
            }

            _roadTexture.SetPixels32(pixels);
            _roadTexture.Apply();

            _roadTexture.wrapModeU = TextureWrapMode.Repeat;
            _roadTexture.wrapModeV = TextureWrapMode.Repeat;
            _roadTexture.filterMode = FilterMode.Bilinear;

            // 设置纹理重复（与 TS 版本 texture.repeat.set(1, 240) 一致）
            if (_roadMaterial != null)
            {
                _roadMaterial.mainTexture = _roadTexture;
                _roadMaterial.mainTextureScale = new Vector2(1f, 240f);
            }
        }

        // =================================================================
        // 工具方法
        // =================================================================

        /// <summary>
        /// 十六进制颜色字符串转 Unity Color
        /// </summary>
        private static Color HexToColor(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        /// <summary>
        /// 十六进制颜色字符串转 Color32
        /// </summary>
        private static Color32 HexToColor32(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }

        /// <summary>
        /// 安全销毁 Mesh 对象
        /// </summary>
        private static void DestroyMesh(ref Mesh mesh)
        {
            if (mesh != null)
            {
                Destroy(mesh);
                mesh = null;
            }
        }

        // =================================================================
        // 公开 API — 供外部系统查询
        // =================================================================

        /// <summary>
        /// 获取当前地形中心（世界坐标 X, Z）
        /// </summary>
        public Vector2 CurrentTerrainCenter => _terrainCenter;

        /// <summary>
        /// 强制重建所有网格（用于设置变更后）
        /// </summary>
        public void ForceRebuild()
        {
            _terrainCenter = GetVisibleTerrainCenter();
            RebuildAllMeshes();
        }

        /// <summary>
        /// 设置地形渲染模式并重建
        /// </summary>
        public void SetRenderMode(TerrainRenderMode mode)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TerrainRenderModeType = mode;
            }
            _lastRenderMode = mode;
            RebuildAllMeshes();
        }
    }
}

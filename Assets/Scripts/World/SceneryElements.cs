using System;
using System.Collections.Generic;
using UnityEngine;
using WeiJinRoad.Core;

namespace WeiJinRoad.World
{
    // =================================================================
    // SceneryElements — 场景装饰物系统
    //
    // 翻译自 TypeScript 版 SceneryElements.tsx
    // 所有地标建筑使用程序化几何体（Box/Cylinder/Cone/Sphere），
    // 不依赖 GLB 模型。树木/岩石/草丛使用 Graphics.DrawMeshInstanced
    // 进行高效实例化渲染。
    // =================================================================

    /// <summary>
    /// 地标类型枚举
    /// </summary>
    public enum LandmarkKind
    {
        AbandonedOutpost,
        FireLookoutTower,
        ScienceTent,
        PickupTruck,
        WarningSign,
        HighwayVillageSign,
        IceArchMonument,
        LanternVillagePlaza,
        SnowBeaconLighthouse,
        SummitObservatory,
        RidgeSignalPylon,
        AFrameCabin,
        AbandonedCompactCar,
        Bollard,
        PowerPole,
    }

    /// <summary>
    /// 地标放置数据
    /// </summary>
    [Serializable]
    public struct LandmarkPlacement
    {
        public LandmarkKind Kind;
        public Vector3 Position;
        public float RotationY;
        public float Scale;
    }

    /// <summary>
    /// 场景装饰物管理器：负责沿道路放置并渲染所有地标建筑和实例化植被。
    /// 使用 Graphics.DrawMeshInstanced 进行高效批量渲染。
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class SceneryElements : MonoBehaviour
    {
        [Header("Road Reference")]
        [Tooltip("道路样条引用（若为空则自动查找）")]
        public RoadSpline RoadSplineRef;

        [Header("Landmark Placements")]
        [Tooltip("地标放置列表")]
        public List<LandmarkPlacement> Landmarks = new List<LandmarkPlacement>();

        [Header("Instanced Scenery")]
        [Tooltip("树木数量")]
        public int TreeCount = 800;
        [Tooltip("岩石数量")]
        public int RockCount = 400;
        [Tooltip("草丛数量")]
        public int GrassGroupCount = 600;
        [Tooltip("枯树数量")]
        public int DeadTreeCount = 200;
        [Tooltip("石堆数量")]
        public int StoneGroupCount = 300;

        [Header("Tree Settings")]
        [Tooltip("树木最小距路中心距离")]
        public float TreeMinRoadDist = 6f;
        [Tooltip("树木最大距路中心距离")]
        public float TreeMaxRoadDist = 80f;
        [Tooltip("树木高度范围")]
        public Vector2 TreeHeightRange = new Vector2(3f, 12f);

        [Header("Materials")]
        public Material TreeMaterial;
        public Material TrunkMaterial;
        public Material RockMaterial;
        public Material GrassMaterial;
        public Material DeadTreeMaterial;

        [Header("Power Line")]
        public Material PowerLineMaterial;
        public float PowerPoleScale = 5.5f;

        private RoadSplineData _roadData;
        private List<Matrix4x4> _treeMatrices = new List<Matrix4x4>();
        private List<Matrix4x4> _rockMatrices = new List<Matrix4x4>();
        private List<Matrix4x4> _grassMatrices = new List<Matrix4x4>();
        private List<Matrix4x4> _deadTreeMatrices = new List<Matrix4x4>();
        private List<Matrix4x4> _stoneMatrices = new List<Matrix4x4>();

        private Mesh _treeMesh;
        private Mesh _trunkMesh;
        private Mesh _rockMesh;
        private Mesh _grassMesh;
        private Mesh _deadTreeMesh;
        private Mesh _stoneMesh;

        private List<GameObject> _landmarkObjects = new List<GameObject>();
        private List<PowerPoleData> _powerPoles = new List<PowerPoleData>();

        private struct PowerPoleData
        {
            public Vector3 Position;
            public float RotationY;
        }

        private float WireHeight => (0.953f + 0.906f) * PowerPoleScale;
        private float ArmOffset => 0.2f * PowerPoleScale;

        private void Start()
        {
            if (RoadSplineRef == null)
                RoadSplineRef = FindFirstObjectByType<RoadSpline>();
            _roadData = RoadSplineRef != null ? RoadSplineRef.Data : new RoadSplineData();

            CreateProceduralMeshes();
            GenerateInstancedScenery();
            GenerateLandmarks();
            GeneratePowerLines();
        }

        private void Update()
        {
            RenderInstancedScenery();
        }

        private void OnDestroy()
        {
            if (_treeMesh != null) Destroy(_treeMesh);
            if (_trunkMesh != null) Destroy(_trunkMesh);
            if (_rockMesh != null) Destroy(_rockMesh);
            if (_grassMesh != null) Destroy(_grassMesh);
            if (_deadTreeMesh != null) Destroy(_deadTreeMesh);
            if (_stoneMesh != null) Destroy(_stoneMesh);
            foreach (var obj in _landmarkObjects) { if (obj != null) Destroy(obj); }
            _landmarkObjects.Clear();
        }

        private void CreateProceduralMeshes()
        {
            _treeMesh = CreateConeMesh(1.5f, 4f, 6);
            _trunkMesh = CreateCylinderMesh(0.15f, 0.2f, 2f, 6);
            _rockMesh = CreateDodecahedronMesh();
            _grassMesh = CreateCylinderMesh(0.8f, 0.8f, 0.6f, 6);
            _deadTreeMesh = CreateCylinderMesh(0.05f, 0.2f, 3f, 4);
            _stoneMesh = CreateSphereMesh();
        }

        private static Mesh CreateConeMesh(float radius, float height, int segments)
        {
            var mesh = new Mesh();
            int vertCount = segments + 2;
            var vertices = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var triangles = new int[segments * 3 * 2];
            vertices[0] = new Vector3(0, height, 0);
            normals[0] = Vector3.up;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;
                vertices[i + 1] = new Vector3(x, 0, z);
                normals[i + 1] = new Vector3(x, radius, z).normalized;
            }
            int tri = 0;
            for (int i = 0; i < segments; i++) { triangles[tri++] = 0; triangles[tri++] = i + 1; triangles[tri++] = i + 2; }
            mesh.vertices = vertices; mesh.normals = normals; mesh.triangles = triangles;
            return mesh;
        }

        private static Mesh CreateCylinderMesh(float radiusTop, float radiusBottom, float height, int segments)
        {
            var mesh = new Mesh();
            int vertCount = (segments + 1) * 2;
            var vertices = new Vector3[vertCount];
            var normals = new Vector3[vertCount];
            var triangles = new int[segments * 6];
            float halfH = height * 0.5f;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f;
                float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
                vertices[i] = new Vector3(c * radiusTop, halfH, s * radiusTop);
                normals[i] = new Vector3(c, 0, s).normalized;
                vertices[segments + 1 + i] = new Vector3(c * radiusBottom, -halfH, s * radiusBottom);
                normals[segments + 1 + i] = new Vector3(c, 0, s).normalized;
            }
            int tri = 0;
            for (int i = 0; i < segments; i++)
            {
                int a = i, b = i + 1, c2 = segments + 1 + i, d = segments + 2 + i;
                triangles[tri++] = a; triangles[tri++] = c2; triangles[tri++] = b;
                triangles[tri++] = b; triangles[tri++] = c2; triangles[tri++] = d;
            }
            mesh.vertices = vertices; mesh.normals = normals; mesh.triangles = triangles;
            return mesh;
        }

        private static Mesh CreateDodecahedronMesh()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Destroy(go);
            return mesh;
        }

        private static Mesh CreateSphereMesh()
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var mesh = go.GetComponent<MeshFilter>().sharedMesh;
            Destroy(go);
            return mesh;
        }

        private void GenerateInstancedScenery()
        {
            System.Random rng = new System.Random(42);

            for (int i = 0; i < TreeCount; i++)
            {
                float z = Mathf.Lerp(RoadConstants.RoadZMax, RoadConstants.RoadZMin, (float)rng.NextDouble());
                var sample = _roadData.Sample(z);
                float side = rng.NextDouble() > 0.5 ? 1f : -1f;
                float dist = TreeMinRoadDist + (float)rng.NextDouble() * (TreeMaxRoadDist - TreeMinRoadDist);
                float x = sample.centerX + side * dist;
                float y = TerrainHeight.GetTerrainHeight(x, z);
                float h = Mathf.Lerp(TreeHeightRange.x, TreeHeightRange.y, (float)rng.NextDouble());
                float rotY = (float)rng.NextDouble() * 360f;
                _treeMatrices.Add(Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(0, rotY, 0), new Vector3(1f, h / 4f, 1f)));
            }

            for (int i = 0; i < RockCount; i++)
            {
                float z = Mathf.Lerp(RoadConstants.RoadZMax, RoadConstants.RoadZMin, (float)rng.NextDouble());
                var sample = _roadData.Sample(z);
                float side = rng.NextDouble() > 0.5 ? 1f : -1f;
                float dist = 4f + (float)rng.NextDouble() * 60f;
                float x = sample.centerX + side * dist;
                float y = TerrainHeight.GetTerrainHeight(x, z);
                float s = 0.3f + (float)rng.NextDouble() * 1.2f;
                float rotY = (float)rng.NextDouble() * 360f;
                _rockMatrices.Add(Matrix4x4.TRS(new Vector3(x, y + s * 0.3f, z), Quaternion.Euler((float)rng.NextDouble() * 20f, rotY, (float)rng.NextDouble() * 20f), Vector3.one * s));
            }

            for (int i = 0; i < GrassGroupCount; i++)
            {
                float z = Mathf.Lerp(RoadConstants.RoadZMax, RoadConstants.RoadZMin, (float)rng.NextDouble());
                var sample = _roadData.Sample(z);
                float side = rng.NextDouble() > 0.5 ? 1f : -1f;
                float dist = 3f + (float)rng.NextDouble() * 30f;
                float x = sample.centerX + side * dist;
                float y = TerrainHeight.GetTerrainHeight(x, z);
                float s = 0.5f + (float)rng.NextDouble() * 0.8f;
                float rotY = (float)rng.NextDouble() * 360f;
                _grassMatrices.Add(Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(0, rotY, 0), Vector3.one * s));
            }

            for (int i = 0; i < DeadTreeCount; i++)
            {
                float z = Mathf.Lerp(RoadConstants.RoadZMax, RoadConstants.RoadZMin, (float)rng.NextDouble());
                var sample = _roadData.Sample(z);
                float side = rng.NextDouble() > 0.5 ? 1f : -1f;
                float dist = 5f + (float)rng.NextDouble() * 50f;
                float x = sample.centerX + side * dist;
                float y = TerrainHeight.GetTerrainHeight(x, z);
                float s = 0.8f + (float)rng.NextDouble() * 0.4f;
                float rotY = (float)rng.NextDouble() * 360f;
                _deadTreeMatrices.Add(Matrix4x4.TRS(new Vector3(x, y, z), Quaternion.Euler(0, rotY, 0), Vector3.one * s));
            }

            for (int i = 0; i < StoneGroupCount; i++)
            {
                float z = Mathf.Lerp(RoadConstants.RoadZMax, RoadConstants.RoadZMin, (float)rng.NextDouble());
                var sample = _roadData.Sample(z);
                float side = rng.NextDouble() > 0.5 ? 1f : -1f;
                float dist = 5f + (float)rng.NextDouble() * 50f;
                float x = sample.centerX + side * dist;
                float y = TerrainHeight.GetTerrainHeight(x, z);
                float s = 0.4f + (float)rng.NextDouble() * 1f;
                float rotY = (float)rng.NextDouble() * 360f;
                _stoneMatrices.Add(Matrix4x4.TRS(new Vector3(x, y + s * 0.3f, z), Quaternion.Euler((float)rng.NextDouble() * 30f, rotY, (float)rng.NextDouble() * 30f), Vector3.one * s));
            }
        }

        private void RenderInstancedScenery()
        {
            RenderBatch(_treeMesh, TreeMaterial, _treeMatrices);
            RenderBatch(_trunkMesh, TrunkMaterial, _treeMatrices);
            RenderBatch(_rockMesh, RockMaterial, _rockMatrices);
            RenderBatch(_grassMesh, GrassMaterial, _grassMatrices);
            RenderBatch(_deadTreeMesh, DeadTreeMaterial, _deadTreeMatrices);
            RenderBatch(_stoneMesh, RockMaterial, _stoneMatrices);
        }

        private static void RenderBatch(Mesh mesh, Material mat, List<Matrix4x4> matrices)
        {
            if (mesh == null || mat == null || matrices.Count == 0) return;
            const int batchSize = 1023;
            for (int i = 0; i < matrices.Count; i += batchSize)
            {
                int count = Mathf.Min(batchSize, matrices.Count - i);
                Graphics.DrawMeshInstanced(mesh, 0, mat, matrices.GetRange(i, count), null,
                    UnityEngine.Rendering.ShadowCastingMode.On, true, 0, null,
                    UnityEngine.Rendering.LightProbeUsage.Off, null);
            }
        }

        private void GenerateLandmarks()
        {
            foreach (var lm in Landmarks)
            {
                GameObject go = CreateLandmark(lm.Kind, lm.Position, lm.RotationY, lm.Scale);
                if (go != null) { go.transform.SetParent(transform); _landmarkObjects.Add(go); }
            }
        }

        public static GameObject CreateLandmark(LandmarkKind kind, Vector3 position, float rotationY = 0f, float scale = 1f)
        {
            var root = new GameObject(kind.ToString());
            root.transform.position = position;
            root.transform.rotation = Quaternion.Euler(0, rotationY * Mathf.Rad2Deg, 0);
            root.transform.localScale = Vector3.one * scale;

            switch (kind)
            {
                case LandmarkKind.FireLookoutTower: BuildFireLookoutTower(root.transform); break;
                case LandmarkKind.AbandonedOutpost: BuildAbandonedOutpost(root.transform); break;
                case LandmarkKind.ScienceTent: BuildScienceTent(root.transform); break;
                case LandmarkKind.PickupTruck: BuildPickupTruck(root.transform); break;
                case LandmarkKind.WarningSign: BuildWarningSign(root.transform); break;
                case LandmarkKind.HighwayVillageSign: BuildHighwayVillageSign(root.transform); break;
                case LandmarkKind.IceArchMonument: BuildIceArchMonument(root.transform); break;
                case LandmarkKind.LanternVillagePlaza: BuildLanternVillagePlaza(root.transform); break;
                case LandmarkKind.SnowBeaconLighthouse: BuildSnowBeaconLighthouse(root.transform); break;
                case LandmarkKind.SummitObservatory: BuildSummitObservatory(root.transform); break;
                case LandmarkKind.RidgeSignalPylon: BuildRidgeSignalPylon(root.transform); break;
                case LandmarkKind.AFrameCabin: BuildAFrameCabin(root.transform); break;
                case LandmarkKind.AbandonedCompactCar: BuildAbandonedCompactCar(root.transform); break;
                case LandmarkKind.Bollard: BuildBollard(root.transform); break;
                case LandmarkKind.PowerPole: BuildPowerPole(root.transform); break;
            }
            return root;
        }

        #region Landmark Builders

        private static void BuildFireLookoutTower(Transform p)
        {
            var stiltCol = new Color(0.24f, 0.16f, 0.09f);
            foreach (var pos in new[] { new Vector3(-2,6,-2), new Vector3(2,6,-2), new Vector3(-2,6,2), new Vector3(2,6,2) })
                AddCylinder(p, pos, Quaternion.identity, 0.1f, 0.2f, 12f, stiltCol, 1f);
            AddBox(p, new Vector3(0,12,0), Quaternion.identity, new Vector3(5,0.5f,5), new Color(0.18f,0.11f,0.06f), 1f);
            AddBox(p, new Vector3(0,13.5f,0), Quaternion.identity, new Vector3(4,2.5f,4), new Color(0.29f,0.30f,0.31f), 0.7f);
            AddBox(p, new Vector3(0,13.5f,2.05f), Quaternion.identity, new Vector3(3,1.5f,0.1f), new Color(1f,0.76f,0.2f), 1f, emissiveColor: new Color(1f,0.67f,0f), emissiveIntensity: 8f);
            AddPointLight(p, new Vector3(0,13.5f,2.5f), new Color(1f,0.67f,0f), 20f, 50f);
            AddCone(p, new Vector3(0,15.5f,0), Quaternion.Euler(0,45,0), 4f, 1.5f, 4, new Color(0.15f,0.15f,0.16f), 0.9f);
        }

        private static void BuildAbandonedOutpost(Transform p)
        {
            AddBox(p, new Vector3(0,4,0), Quaternion.identity, new Vector3(8,0.5f,8), new Color(0.76f,0.20f,0.20f), 0.8f);
            var pillarCol = new Color(0.33f,0.33f,0.33f);
            AddCylinder(p, new Vector3(-3,2,-3), Quaternion.identity, 0.2f, 0.2f, 4f, pillarCol, 1f);
            AddCylinder(p, new Vector3(3,2,-3), Quaternion.identity, 0.2f, 0.2f, 4f, pillarCol, 1f);
            AddBox(p, new Vector3(-1,1,0), Quaternion.identity, new Vector3(1,2,1), new Color(0.54f,0.19f,0.16f), 0.9f);
            AddBox(p, new Vector3(-1,1.5f,0.55f), Quaternion.identity, new Vector3(0.8f,0.6f,0.1f), new Color(0.07f,0.07f,0.07f), 0.5f);
            AddBox(p, new Vector3(1.5f,1.2f,0), Quaternion.Euler(0,-11.5f,0), new Vector3(1.2f,2.4f,1), new Color(0.21f,0.37f,0.29f), 0.9f);
            AddBox(p, new Vector3(1.5f,1.4f,0.55f), Quaternion.Euler(0,-11.5f,0), new Vector3(0.9f,1.2f,0.1f), new Color(0.10f,0.15f,0.13f), 0.3f, emissiveColor: new Color(0.05f,0.08f,0.07f), emissiveIntensity: 0.5f);
        }

        private static void BuildScienceTent(Transform p)
        {
            AddSphere(p, Vector3.zero, Quaternion.identity, 2.5f, new Color(0.76f,0.15f,0.15f), 0.9f, hemisphere: true);
            var blinkGo = AddSphere(p, new Vector3(0,2.7f,0), Quaternion.identity, 0.2f, new Color(1f,0f,0f), 1f, emissiveColor: new Color(1f,0f,0f), emissiveIntensity: 2f);
            var blink = p.gameObject.AddComponent<ScienceTentBlink>();
            blink.EmissiveRenderer = blinkGo.GetComponent<Renderer>();
            blink.PointLight = AddPointLight(p, new Vector3(0,2.7f,0), Color.red, 10f, 20f);
            AddCylinder(p, new Vector3(3,1,0), Quaternion.identity, 0.05f, 0.05f, 2f, new Color(0.33f,0.33f,0.33f), 1f);
            AddBox(p, new Vector3(3,2,0), Quaternion.identity, new Vector3(0.5f,0.5f,0.5f), new Color(0.8f,0.8f,0.8f), 1f);
            AddCylinder(p, new Vector3(3,2.3f,0), Quaternion.Euler(0,0,90), 0.2f, 0.2f, 0.4f, Color.white, 1f);
        }

        private static void BuildPickupTruck(Transform p)
        {
            AddBox(p, new Vector3(0,0.5f,0), Quaternion.identity, new Vector3(1.8f,0.6f,4), new Color(0.23f,0.29f,0.42f), 0.8f);
            AddBox(p, new Vector3(0,1.2f,0.5f), Quaternion.identity, new Vector3(1.6f,0.8f,1.5f), new Color(0.17f,0.23f,0.36f), 0.8f);
            AddBox(p, new Vector3(-1.2f,1.0f,0.5f), Quaternion.Euler(0,-45,0), new Vector3(0.05f,1.0f,1.0f), new Color(0.23f,0.29f,0.42f), 0.8f);
            var wheelCol = new Color(0.07f,0.07f,0.07f);
            foreach (var wp in new[] { new Vector3(-1,0.2f,1.2f), new Vector3(1,0.2f,1.2f), new Vector3(-1,0.2f,-1.2f), new Vector3(1,0.2f,-1.2f) })
                AddCylinder(p, wp, Quaternion.Euler(0,0,90), 0.3f, 0.3f, 0.2f, wheelCol, 1f);
        }

        private static void BuildWarningSign(Transform p)
        {
            AddCylinder(p, new Vector3(0,1,0), Quaternion.identity, 0.05f, 0.05f, 2f, new Color(0.29f,0.25f,0.21f), 1f);
            AddBox(p, new Vector3(0,1.8f,0.06f), Quaternion.identity, new Vector3(1.2f,0.6f,0.05f), new Color(0.76f,0.20f,0.20f), 0.9f);
            AddBox(p, new Vector3(0,1.8f,0.09f), Quaternion.identity, new Vector3(1.0f,0.4f,0.01f), new Color(0.91f,0.84f,0.72f), 1f);
        }

        private static void BuildHighwayVillageSign(Transform p)
        {
            var metalCol = new Color(0.40f,0.44f,0.43f);
            AddCylinder(p, new Vector3(-2.6f,2.2f,0), Quaternion.identity, 0.08f, 0.12f, 4.4f, metalCol, 1f);
            AddCylinder(p, new Vector3(2.6f,2.2f,0), Quaternion.identity, 0.08f, 0.12f, 4.4f, metalCol, 1f);
            AddBox(p, new Vector3(0,3.6f,0), Quaternion.identity, new Vector3(6.4f,2,0.14f), new Color(0.12f,0.44f,0.29f), 0.7f);
            float[] textY = { 0.65f, 0.15f, -0.35f };
            for (int i = 0; i < textY.Length; i++)
                AddBox(p, new Vector3(0,3.6f+textY[i],0.09f), Quaternion.identity, new Vector3(4.8f-i*0.8f,0.08f,0.04f), new Color(0.87f,0.96f,0.89f), 1f, emissiveColor: new Color(0.62f,0.83f,0.66f), emissiveIntensity: 0.25f);
            AddBox(p, new Vector3(0,3.58f,0.15f), Quaternion.Euler(0,0,-19.5f), new Vector3(5.5f,0.22f,0.06f), new Color(0.61f,0.08f,0.08f), 1f, emissiveColor: new Color(0.33f,0f,0f), emissiveIntensity: 0.4f);
            AddBox(p, new Vector3(0,3.58f,0.16f), Quaternion.Euler(0,0,19.5f), new Vector3(5.5f,0.22f,0.06f), new Color(0.61f,0.08f,0.08f), 1f, emissiveColor: new Color(0.33f,0f,0f), emissiveIntensity: 0.4f);
        }

        private static void BuildIceArchMonument(Transform p)
        {
            var iceCol = new Color(0.73f,0.89f,0.93f);
            foreach (int side in new[] { -1, 1 })
                AddBox(p, new Vector3(side*2.7f,3.1f,0), Quaternion.Euler(0,0,side*10.3f), new Vector3(1.1f,6.2f,1.2f), iceCol, 0.18f);
            AddPointLight(p, new Vector3(0,3.4f,0), new Color(0.61f,0.91f,1f), 10f, 34f);
        }

        private static void BuildLanternVillagePlaza(Transform p)
        {
            for (int i = 0; i < 6; i++)
            {
                float a = (i / 6f) * Mathf.PI * 2f;
                AddBox(p, new Vector3(Mathf.Sin(a)*5.8f,0.18f,Mathf.Cos(a)*5.8f), Quaternion.Euler(0,a*Mathf.Rad2Deg,0), new Vector3(0.9f,0.32f,0.42f), new Color(0.75f,0.78f,0.81f), 0.9f);
            }
            // Campfire
            var fire = new GameObject("Campfire").transform; fire.SetParent(p); fire.localPosition = new Vector3(3.2f,0.12f,2.2f);
            for (int i = 0; i < 3; i++)
                AddCylinder(fire, new Vector3(Mathf.Sin(i*2.1f)*0.7f,0.18f,Mathf.Cos(i*2.1f)*0.7f), Quaternion.Euler(90,11.5f,i*63f), 0.16f,0.22f,1.6f, new Color(0.23f,0.15f,0.09f), 1f);
            AddOctahedron(fire, new Vector3(0,0.45f,0), 0.52f, new Color(1f,0.48f,0.1f), emissiveColor: new Color(1f,0.35f,0f), emissiveIntensity: 8f);
            AddOctahedron(fire, new Vector3(0,0.8f,0), 0.34f, new Color(1f,0.88f,0.57f), emissiveColor: new Color(1f,0.69f,0.23f), emissiveIntensity: 9f);
            AddPointLight(fire, Vector3.up*1.1f, new Color(1f,0.55f,0.15f), 18f, 28f);
            // Lantern girl (simplified)
            AddCylinder(p, new Vector3(0,3.8f,-1.2f), Quaternion.identity, 0.3f,0.4f,7f, new Color(0.54f,0.56f,0.60f), 0.95f);
            AddSphere(p, new Vector3(0,5.5f,-0.9f), Quaternion.identity, 0.4f, new Color(0.67f,0.80f,1f), 1f, emissiveColor: new Color(0.53f,0.73f,1f), emissiveIntensity: 5.8f);
            AddPointLight(p, new Vector3(0,5.5f,-0.9f), new Color(0.53f,0.73f,1f), 30f, 45f);
        }

        private static void BuildSnowBeaconLighthouse(Transform p)
        {
            AddCylinder(p, new Vector3(0,2.8f,0), Quaternion.identity, 1.05f,1.45f,5.6f, new Color(0.85f,0.87f,0.89f), 0.82f);
            AddCylinder(p, new Vector3(0,6.05f,0), Quaternion.identity, 1.35f,1.1f,0.65f, new Color(0.30f,0.35f,0.38f), 0.65f);
            AddCylinder(p, new Vector3(0,6.58f,0), Quaternion.identity, 0.92f,0.92f,0.48f, new Color(1f,0.84f,0.42f), 1f, emissiveColor: new Color(1f,0.60f,0.14f), emissiveIntensity: 5.5f);
            AddCone(p, new Vector3(0,7.15f,0), Quaternion.identity, 1.35f,0.9f,8, new Color(0.18f,0.20f,0.23f), 0.72f);
            AddPointLight(p, new Vector3(0,6.5f,0), new Color(1f,0.69f,0.31f), 22f, 52f);
        }

        private static void BuildSummitObservatory(Transform p)
        {
            AddCylinder(p, new Vector3(0,0.75f,14), Quaternion.identity, 25,30,1.5f, new Color(0.36f,0.39f,0.42f), 0.94f);
            AddCylinder(p, new Vector3(0,1.9f,14), Quaternion.identity, 21,25,1f, new Color(0.85f,0.89f,0.93f), 0.96f);
            AddBox(p, new Vector3(0,5.4f,14), Quaternion.identity, new Vector3(22,7.2f,13), new Color(0.22f,0.27f,0.30f), 0.92f);
            AddBox(p, new Vector3(0,10.4f,14), Quaternion.identity, new Vector3(18,2.2f,10), new Color(0.84f,0.88f,0.91f), 0.9f);
            AddCylinder(p, new Vector3(0,47.8f,10), Quaternion.Euler(0,-16f,0), 0.5f,0.5f,10f, new Color(0.51f,0.59f,0.71f), 0.98f);
            AddCone(p, new Vector3(0,53f,10), Quaternion.Euler(0,-16f,0), 5f,3f,8, new Color(0.51f,0.59f,0.71f), 0.98f);
            foreach (var pos in new[] { new Vector3(-27,86,2), new Vector3(29,82,4) })
                AddPointLight(p, pos, new Color(1f,0.18f,0.13f), 18f, 42f);
            foreach (var pos in new[] { new Vector3(-12,8.4f,24), new Vector3(0,8.2f,25), new Vector3(12,8.4f,24) })
                AddPointLight(p, pos, new Color(1f,0.71f,0.35f), 12f, 55f);
            AddPointLight(p, new Vector3(0,46,2), new Color(0.57f,0.74f,1f), 42f, 160f);
            AddPointLight(p, new Vector3(0,11,26), new Color(1f,0.70f,0.42f), 18f, 82f);
        }

        private static void BuildRidgeSignalPylon(Transform p)
        {
            AddCylinder(p, new Vector3(0,5,0), Quaternion.identity, 0.18f,0.42f,10f, new Color(0.41f,0.45f,0.49f), 0.7f);
            for (int i = 0; i < 3; i++)
                AddTorus(p, new Vector3(0,6+i*1.25f,0), Quaternion.Euler(90,i*40f,0), 1.35f+i*0.55f, 0.055f, new Color(0.85f,0.91f,0.95f), 0.42f);
            AddOctahedron(p, new Vector3(0,10.5f,0), 0.45f, new Color(0.62f,0.85f,1f), emissiveColor: new Color(0.31f,0.74f,1f), emissiveIntensity: 4.5f);
        }

        private static void BuildAFrameCabin(Transform p)
        {
            AddBox(p, new Vector3(0,1.1f,0), Quaternion.identity, new Vector3(3.2f,2.2f,4), new Color(0.29f,0.17f,0.11f), 0.9f);
            AddBox(p, new Vector3(-0.95f,2.45f,0), Quaternion.Euler(0,0,-35.5f), new Vector3(0.38f,3.8f,4.35f), new Color(0.13f,0.10f,0.09f), 0.95f);
            AddBox(p, new Vector3(0.95f,2.45f,0), Quaternion.Euler(0,0,35.5f), new Vector3(0.38f,3.8f,4.35f), new Color(0.13f,0.10f,0.09f), 0.95f);
            AddBox(p, new Vector3(-1.05f,2.72f,0), Quaternion.Euler(0,0,-35.5f), new Vector3(0.42f,3.65f,4.55f), new Color(0.93f,0.96f,0.98f), 1f);
            AddBox(p, new Vector3(1.05f,2.72f,0), Quaternion.Euler(0,0,35.5f), new Vector3(0.42f,3.65f,4.55f), new Color(0.93f,0.96f,0.98f), 1f);
            AddBox(p, new Vector3(0,1.25f,2.05f), Quaternion.identity, new Vector3(1.8f,0.9f,0.08f), new Color(1f,0.82f,0.42f), 1f, emissiveColor: new Color(1f,0.54f,0.12f), emissiveIntensity: 5.8f);
            AddCylinder(p, new Vector3(0,0.08f,0), Quaternion.identity, 3.2f,3.8f,0.18f, new Color(0.94f,0.96f,0.98f), 1f);
            AddPointLight(p, new Vector3(0,1.5f,2.5f), new Color(1f,0.67f,0.3f), 15f, 20f);
        }

        private static void BuildAbandonedCompactCar(Transform p)
        {
            AddBox(p, new Vector3(0,0.45f,0), Quaternion.identity, new Vector3(2.2f,0.65f,3.2f), new Color(0.42f,0.45f,0.49f), 0.85f);
            AddBox(p, new Vector3(0,1.0f,-0.15f), Quaternion.identity, new Vector3(1.65f,0.62f,1.45f), new Color(0.19f,0.23f,0.26f), 0.7f);
            AddBox(p, new Vector3(0,1.02f,0.62f), Quaternion.identity, new Vector3(1.35f,0.36f,0.05f), new Color(0.07f,0.09f,0.13f), 1f, emissiveColor: new Color(0.06f,0.09f,0.13f), emissiveIntensity: 0.2f);
            var wheelCol = new Color(0.04f,0.04f,0.04f);
            foreach (float x in new[] { -0.9f, 0.9f })
                foreach (float z in new[] { 1.05f, -1.05f })
                    AddCylinder(p, new Vector3(x,0.2f,z), Quaternion.Euler(0,0,90), 0.32f,0.32f,0.24f, wheelCol, 1f);
        }

        private static void BuildBollard(Transform p)
        {
            AddCylinder(p, new Vector3(0,0.746f,0), Quaternion.identity, 0.2f,0.25f,1.5f, new Color(0.35f,0.37f,0.38f), 0.85f);
            AddBox(p, new Vector3(0,0.6f,0.12f), Quaternion.identity, new Vector3(0.15f,0.35f,0.02f), new Color(0.83f,0.53f,0.06f), 1f, emissiveColor: new Color(0.83f,0.53f,0.06f), emissiveIntensity: 0.3f);
            AddBox(p, new Vector3(0,0.6f,-0.12f), Quaternion.identity, new Vector3(0.15f,0.35f,0.02f), new Color(0.83f,0.53f,0.06f), 1f, emissiveColor: new Color(0.83f,0.53f,0.06f), emissiveIntensity: 0.3f);
        }

        private static void BuildPowerPole(Transform p)
        {
            AddCylinder(p, new Vector3(0,0.953f*5.5f,0), Quaternion.identity, 0.08f*5.5f,0.12f*5.5f,1.902f*5.5f, new Color(0.29f,0.21f,0.15f), 0.95f);
            foreach (float x in new[] { -0.2f, 0f, 0.2f })
                AddCylinder(p, new Vector3(x*5.5f,(0.953f+1.02f)*5.5f,0), Quaternion.identity, 0.015f*5.5f,0.025f*5.5f,0.06f*5.5f, new Color(0.35f,0.48f,0.54f), 0.5f);
        }

        #endregion

        private void GeneratePowerLines()
        {
            _powerPoles.Clear();
            foreach (var lm in Landmarks)
                if (lm.Kind == LandmarkKind.PowerPole)
                    _powerPoles.Add(new PowerPoleData { Position = lm.Position, RotationY = lm.RotationY });
            if (_powerPoles.Count < 2 || PowerLineMaterial == null) return;
            for (int i = 0; i < _powerPoles.Count - 1; i++)
            {
                var a = _powerPoles[i]; var b = _powerPoles[i + 1];
                float dx = b.Position.x - a.Position.x, dz = b.Position.z - a.Position.z;
                float dist = Mathf.Sqrt(dx*dx + dz*dz);
                if (dist > 80f) continue;
                foreach (int armSign in new[] { -1, 1, 0 })
                {
                    var armDirA = new Vector3(Mathf.Cos(a.RotationY),0,Mathf.Sin(a.RotationY));
                    var armDirB = new Vector3(Mathf.Cos(b.RotationY),0,Mathf.Sin(b.RotationY));
                    float offA = armSign * ArmOffset, offB = armSign * ArmOffset;
                    var start = new Vector3(a.Position.x+armDirA.x*offA, a.Position.y+WireHeight, a.Position.z+armDirA.z*offA);
                    var end = new Vector3(b.Position.x+armDirB.x*offB, b.Position.y+WireHeight, b.Position.z+armDirB.z*offB);
                    float sag = Mathf.Min(dist*0.035f, 3f);
                    var mid = new Vector3((start.x+end.x)*0.5f, Mathf.Min(start.y,end.y)-sag, (start.z+end.z)*0.5f);
                    CreateWireCurve(start, mid, end);
                }
            }
        }

        private void CreateWireCurve(Vector3 start, Vector3 mid, Vector3 end)
        {
            var wireObj = new GameObject("PowerLine");
            wireObj.transform.SetParent(transform);
            var lr = wireObj.AddComponent<LineRenderer>();
            lr.material = PowerLineMaterial;
            lr.startWidth = 0.03f; lr.endWidth = 0.03f;
            lr.positionCount = 13;
            for (int i = 0; i <= 12; i++)
            {
                float t = i / 12f;
                Vector3 pt = (1-t)*(1-t)*start + 2*(1-t)*t*mid + t*t*end;
                lr.SetPosition(i, pt);
            }
        }

        #region Geometry Helpers

        private static GameObject AddBox(Transform parent, Vector3 pos, Quaternion rot, Vector3 size, Color color, float roughness = 1f, float metalness = 0f, Color? emissiveColor = null, float emissiveIntensity = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = size;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color; mat.SetFloat("_Smoothness", 1f-roughness); mat.SetFloat("_Metallic", metalness);
            if (emissiveColor.HasValue && emissiveIntensity > 0f) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emissiveColor.Value * emissiveIntensity); }
            go.GetComponent<Renderer>().material = mat;
            return go;
        }

        private static GameObject AddCylinder(Transform parent, Vector3 pos, Quaternion rot, float radiusTop, float radiusBottom, float height, Color color, float roughness = 1f, float metalness = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localRotation = rot;
            float scaleX = (radiusTop+radiusBottom)*0.5f/0.5f, scaleY = height/2f;
            go.transform.localScale = new Vector3(scaleX, scaleY, scaleX);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color; mat.SetFloat("_Smoothness", 1f-roughness); mat.SetFloat("_Metallic", metalness);
            go.GetComponent<Renderer>().material = mat;
            return go;
        }

        private static GameObject AddCone(Transform parent, Vector3 pos, Quaternion rot, float radius, float height, int segments, Color color, float roughness = 1f)
        {
            var go = new GameObject("Cone");
            go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localRotation = rot;
            var filter = go.AddComponent<MeshFilter>(); filter.mesh = CreateConeMesh(radius, height, segments);
            var rend = go.AddComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = color; mat.SetFloat("_Smoothness", 1f-roughness);
            rend.material = mat;
            return go;
        }

        private static GameObject AddSphere(Transform parent, Vector3 pos, Quaternion rot, float radius, Color color, float roughness = 1f, bool hemisphere = false, Color? emissiveColor = null, float emissiveIntensity = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localRotation = rot;
            go.transform.localScale = hemisphere ? new Vector3(radius*2f, radius, radius*2f) : Vector3.one * radius * 2f;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color; mat.SetFloat("_Smoothness", 1f-roughness);
            if (emissiveColor.HasValue && emissiveIntensity > 0f) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emissiveColor.Value * emissiveIntensity); }
            go.GetComponent<Renderer>().material = mat;
            return go;
        }

        private static GameObject AddOctahedron(Transform parent, Vector3 pos, float radius, Color color, Color? emissiveColor = null, float emissiveIntensity = 0f)
        {
            var go = new GameObject("Octahedron");
            go.transform.SetParent(parent); go.transform.localPosition = pos;
            var mesh = new Mesh();
            mesh.vertices = new Vector3[] { Vector3.up*radius, Vector3.down*radius, Vector3.right*radius, Vector3.left*radius, Vector3.forward*radius, Vector3.back*radius };
            mesh.triangles = new int[] { 0,4,2, 0,2,5, 0,5,3, 0,3,4, 1,2,4, 1,5,2, 1,3,5, 1,4,3 };
            mesh.RecalculateNormals();
            go.AddComponent<MeshFilter>().mesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = color;
            if (emissiveColor.HasValue && emissiveIntensity > 0f) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emissiveColor.Value * emissiveIntensity); }
            rend.material = mat;
            return go;
        }

        private static GameObject AddTorus(Transform parent, Vector3 pos, Quaternion rot, float majorRadius, float minorRadius, Color color, float roughness = 1f, float metalness = 0f)
        {
            var go = new GameObject("Torus");
            go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localRotation = rot;
            int segments = 16;
            for (int i = 0; i < segments; i++)
            {
                float angle = (i/(float)segments)*Mathf.PI*2f;
                var segPos = new Vector3(Mathf.Cos(angle)*majorRadius, 0, Mathf.Sin(angle)*majorRadius);
                var seg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                seg.transform.SetParent(go.transform); seg.transform.localPosition = segPos;
                seg.transform.localRotation = Quaternion.Euler(0, -angle*Mathf.Rad2Deg, 0);
                float segLen = 2f*Mathf.PI*majorRadius/segments*1.1f;
                seg.transform.localScale = new Vector3(minorRadius*2f, segLen*0.5f, minorRadius*2f);
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = color; mat.SetFloat("_Smoothness", 1f-roughness); mat.SetFloat("_Metallic", metalness);
                seg.GetComponent<Renderer>().material = mat;
            }
            return go;
        }

        private static Light AddPointLight(Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var obj = new GameObject("PointLight");
            obj.transform.SetParent(parent); obj.transform.localPosition = pos;
            var light = obj.AddComponent<Light>();
            light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range; light.shadows = LightShadows.Soft;
            return light;
        }

        #endregion
    }

    /// <summary>
    /// 科考帐篷红色闪烁灯效果
    /// </summary>
    public class ScienceTentBlink : MonoBehaviour
    {
        public Renderer EmissiveRenderer;
        public Light PointLight;
        private Material _mat;
        private static readonly Color EmissiveColor = new Color(1f, 0f, 0f);

        private void Start() { if (EmissiveRenderer != null) _mat = EmissiveRenderer.material; }
        private void Update()
        {
            bool isBlink = (Time.time % 1.0f) < 0.1f;
            if (_mat != null) _mat.SetColor("_EmissionColor", EmissiveColor * (isBlink ? 8f : 1f));
            if (PointLight != null) PointLight.intensity = isBlink ? 30f : 0f;
        }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-136300035bd74a5c9764aa485e915c3a/cwd.txt'; exit "$__tr_native_ec"
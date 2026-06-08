using System;
using System.Collections.Generic;
using UnityEngine;
using WeiJinRoad.Core;
using WeiJinRoad.Data;
using ObstacleKind = WeiJinRoad.Data.ObstacleKind;

namespace WeiJinRoad.World
{
    // =================================================================
    // RoadObstacles — 道路障碍物系统
    // 翻译自 TypeScript 版 RoadObstacles.tsx
    // 4种障碍类型：snowDrift(雪堆)、iceBlock(冰块)、fallenLog(倒木)、rockfall(落石)
    // 使用 InstancedMesh 高效渲染，清除动画：缩放+淡出 0.36s
    // =================================================================

    /// <summary>
    /// 障碍物视觉定义（用于渲染层，与 Data.ObstacleDef 碰撞定义分离）
    /// </summary>
    [Serializable]
    public class ObstacleVisualDef
    {
        public string Id;
        public ObstacleKind Kind;
        public float X, Y, Z, Scale, RotationY;
    }

    public enum ObstaclePhase { Present, Clearing, Gone }

    [DefaultExecutionOrder(60)]
    public class RoadObstacles : MonoBehaviour
    {
        private const float HiddenY = -9000f;
        private const float ClearDuration = 0.36f;

        [Header("Obstacle Data")]
        public List<ObstacleVisualDef> Obstacles = new List<ObstacleVisualDef>();

        [Header("Materials")]
        public Material SnowDriftMaterial;
        public Material RockfallMaterial;
        public Material FallenLogMaterial;
        public Material IceBlockMaterial;

        private Dictionary<ObstacleKind, List<ObstacleVisualDef>> _byKind = new Dictionary<ObstacleKind, List<ObstacleVisualDef>>();
        private Dictionary<ObstacleKind, InstancedObstacleLayer> _layers = new Dictionary<ObstacleKind, InstancedObstacleLayer>();

        private void Start()
        {
            CategorizeObstacles();
            CreateLayers();
            ApplyPersistedClearState();
        }

        private void OnDestroy()
        {
            foreach (var layer in _layers.Values) layer.Dispose();
        }

        private void Update()
        {
            float delta = Time.deltaTime;
            bool anyDirty = false;
            foreach (var kvp in _layers)
                if (kvp.Value.UpdateAnimations(delta)) anyDirty = true;
            if (anyDirty)
                foreach (var kvp in _layers) kvp.Value.ApplyMatrices();
        }

        private void CategorizeObstacles()
        {
            _byKind.Clear();
            foreach (ObstacleKind kind in Enum.GetValues(typeof(ObstacleKind)))
                _byKind[kind] = new List<ObstacleVisualDef>();
            foreach (var ob in Obstacles) _byKind[ob.Kind].Add(ob);
        }

        private void CreateLayers()
        {
            _layers.Clear();
            var snowMesh = CreateIcosahedronMesh();
            var rockMesh = CreateDodecahedronMesh();
            var logMesh = CreateCylinderMesh(0.5f, 0.55f, 1f, 7);
            var iceMesh = CreateBoxMesh();

            if (_byKind[ObstacleKind.SnowDrift].Count > 0)
                _layers[ObstacleKind.SnowDrift] = new InstancedObstacleLayer(_byKind[ObstacleKind.SnowDrift], ObstacleKind.SnowDrift, snowMesh, SnowDriftMaterial);
            if (_byKind[ObstacleKind.Rockfall].Count > 0)
                _layers[ObstacleKind.Rockfall] = new InstancedObstacleLayer(_byKind[ObstacleKind.Rockfall], ObstacleKind.Rockfall, rockMesh, RockfallMaterial);
            if (_byKind[ObstacleKind.FallenLog].Count > 0)
                _layers[ObstacleKind.FallenLog] = new InstancedObstacleLayer(_byKind[ObstacleKind.FallenLog], ObstacleKind.FallenLog, logMesh, FallenLogMaterial);
            if (_byKind[ObstacleKind.IceBlock].Count > 0)
                _layers[ObstacleKind.IceBlock] = new InstancedObstacleLayer(_byKind[ObstacleKind.IceBlock], ObstacleKind.IceBlock, iceMesh, IceBlockMaterial);

            foreach (var kvp in _layers) { kvp.Value.Initialize(transform); kvp.Value.ApplyMatrices(); }
        }

        private void ApplyPersistedClearState()
        {
            var gm = GameManager.Instance;
            var clearedSet = new HashSet<string>(gm.ClearedObstacles);
            foreach (var kvp in _layers) kvp.Value.ApplyPersistedState(clearedSet);
            GameEvents.OnObstacleCleared += OnObstacleCleared;
        }

        private void OnObstacleCleared(string id)
        {
            foreach (var kvp in _layers) if (kvp.Value.TryStartClearing(id)) break;
        }

        private static float YLift(ObstacleKind kind, float scale) => kind switch
        {
            ObstacleKind.SnowDrift => scale * 0.4f,
            ObstacleKind.Rockfall => scale * 0.5f,
            ObstacleKind.FallenLog => scale * 0.42f,
            ObstacleKind.IceBlock => scale * 0.7f,
            _ => 0f
        };

        private static Mesh CreateIcosahedronMesh() { var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); var m = go.GetComponent<MeshFilter>().sharedMesh; Destroy(go); return m; }
        private static Mesh CreateDodecahedronMesh() { var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); var m = go.GetComponent<MeshFilter>().sharedMesh; Destroy(go); return m; }
        private static Mesh CreateBoxMesh() { var go = GameObject.CreatePrimitive(PrimitiveType.Cube); var m = go.GetComponent<MeshFilter>().sharedMesh; Destroy(go); return m; }

        private static Mesh CreateCylinderMesh(float radiusTop, float radiusBottom, float height, int segments)
        {
            var mesh = new Mesh();
            int vertCount = (segments + 1) * 2;
            var vertices = new Vector3[vertCount]; var normals = new Vector3[vertCount]; var triangles = new int[segments * 6];
            float halfH = height * 0.5f;
            for (int i = 0; i <= segments; i++)
            {
                float angle = (i / (float)segments) * Mathf.PI * 2f; float c = Mathf.Cos(angle), s = Mathf.Sin(angle);
                vertices[i] = new Vector3(c * radiusTop, halfH, s * radiusTop); normals[i] = new Vector3(c, 0, s).normalized;
                vertices[segments + 1 + i] = new Vector3(c * radiusBottom, -halfH, s * radiusBottom); normals[segments + 1 + i] = new Vector3(c, 0, s).normalized;
            }
            int tri = 0;
            for (int i = 0; i < segments; i++) { int a = i, b = i + 1, c2 = segments + 1 + i, d = segments + 2 + i; triangles[tri++] = a; triangles[tri++] = c2; triangles[tri++] = b; triangles[tri++] = b; triangles[tri++] = c2; triangles[tri++] = d; }
            mesh.vertices = vertices; mesh.normals = normals; mesh.triangles = triangles; return mesh;
        }

        public static List<ObstacleVisualDef> GenerateObstacles(int seed = 42, int count = 60)
        {
            var result = new List<ObstacleVisualDef>(); var rng = new System.Random(seed);
            ObstacleKind[] kinds = { ObstacleKind.SnowDrift, ObstacleKind.IceBlock, ObstacleKind.FallenLog, ObstacleKind.Rockfall };
            for (int i = 0; i < count; i++)
            {
                var kind = kinds[rng.Next(kinds.Length)];
                float routeZ = Mathf.Lerp(280f, -1500f, (float)rng.NextDouble());
                var sample = TerrainHeight.RoadSampler?.Sample(TerrainHeight.RouteToWorldZ(routeZ));
                if (sample == null) continue;
                float side = rng.NextDouble() > 0.5 ? 1f : -1f;
                float dist = (float)rng.NextDouble() * sample.Value.HalfWidth * 0.8f;
                float x = sample.Value.CenterX + side * dist;
                float z = TerrainHeight.RouteToWorldZ(routeZ);
                float y = TerrainHeight.GetTerrainHeight(x, z);
                result.Add(new ObstacleVisualDef { Id = $"obs_{i}", Kind = kind, X = x, Y = y, Z = z, Scale = 0.6f + (float)rng.NextDouble() * 1.2f, RotationY = (float)rng.NextDouble() * Mathf.PI * 2f });
            }
            return result;
        }

        private class InstancedObstacleLayer
        {
            private readonly List<ObstacleVisualDef> _items; private readonly ObstacleKind _kind; private readonly Mesh _mesh; private readonly Material _material;
            private readonly ObstaclePhase[] _phases; private readonly float[] _progress; private readonly HashSet<string> _knownCleared = new HashSet<string>();
            private Matrix4x4[] _matrices; private bool _dirty;

            public InstancedObstacleLayer(List<ObstacleVisualDef> items, ObstacleKind kind, Mesh mesh, Material material)
            { _items = items; _kind = kind; _mesh = mesh; _material = material; _phases = new ObstaclePhase[items.Count]; _progress = new float[items.Count]; _matrices = new Matrix4x4[items.Count]; for (int i = 0; i < items.Count; i++) { _phases[i] = ObstaclePhase.Present; _progress[i] = 0f; } }

            public void Initialize(Transform parent) { for (int i = 0; i < _items.Count; i++) WriteMatrix(i, 1f); }

            public void ApplyPersistedState(HashSet<string> clearedSet)
            { for (int i = 0; i < _items.Count; i++) { if (clearedSet.Contains(_items[i].Id)) { _phases[i] = ObstaclePhase.Gone; _progress[i] = 1f; _knownCleared.Add(_items[i].Id); WriteMatrix(i, 0f); } else WriteMatrix(i, 1f); } _dirty = true; }

            public bool TryStartClearing(string id)
            { for (int i = 0; i < _items.Count; i++) if (_items[i].Id == id && _phases[i] == ObstaclePhase.Present) { _phases[i] = ObstaclePhase.Clearing; _progress[i] = 0f; _knownCleared.Add(id); return true; } return false; }

            public bool UpdateAnimations(float delta)
            { bool any = false; for (int i = 0; i < _phases.Length; i++) { if (_phases[i] != ObstaclePhase.Clearing) continue; _progress[i] = Mathf.Min(1f, _progress[i] + delta / ClearDuration); float p = _progress[i]; float ease = 1f - (1f - p) * (1f - p); WriteMatrix(i, 1f - ease); if (p >= 1f) { _phases[i] = ObstaclePhase.Gone; WriteMatrix(i, 0f); } any = true; } _dirty = any || _dirty; return any; }

            public void ApplyMatrices()
            { if (!_dirty || _mesh == null || _material == null || _items.Count == 0) return; _dirty = false; Graphics.DrawMeshInstanced(_mesh, 0, _material, _matrices, _items.Count, null, UnityEngine.Rendering.ShadowCastingMode.On, true, 0, null, UnityEngine.Rendering.LightProbeUsage.Off, null); }

            public void Dispose() { }

            private void WriteMatrix(int i, float scaleFactor)
            {
                var it = _items[i]; Vector3 pos; Quaternion rot; Vector3 scale;
                if (scaleFactor <= 0.001f) { pos = new Vector3(0, HiddenY, 0); rot = Quaternion.identity; scale = Vector3.one * 0.001f; }
                else
                {
                    float lift = YLift(_kind, it.Scale); pos = new Vector3(it.X, it.Y + lift, it.Z);
                    rot = _kind == ObstacleKind.FallenLog ? Quaternion.Euler(90f, it.RotationY * Mathf.Rad2Deg, 0f) : Quaternion.Euler(0f, it.RotationY * Mathf.Rad2Deg, 0f);
                    float s = it.Scale * scaleFactor;
                    scale = _kind switch { ObstacleKind.SnowDrift => new Vector3(s * 1.15f, s * 0.6f, s * 1.15f), ObstacleKind.FallenLog => new Vector3(s * 0.45f, s * 2.6f, s * 0.45f), ObstacleKind.IceBlock => new Vector3(s, s * 0.9f, s), _ => Vector3.one * s };
                }
                _matrices[i] = Matrix4x4.TRS(pos, rot, scale);
            }
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using WeiJinRoad.Core;

namespace WeiJinRoad.World
{
    public enum NodeType { Wreck, Logpile, FuelDrum, Device, Crystal }

    [Serializable]
    public class ResourceNodeDef
    {
        public string Id;
        public NodeType NodeTypeVal;
        public ResourceKind Kind;
        public int Amount;
        public float X, Y, Z, RotationY;
    }

    [DefaultExecutionOrder(61)]
    public class ResourceNodes : MonoBehaviour
    {
        private const float PickRange = 9f;

        private static readonly Color AccentWreck = new Color(0.788f, 0.635f, 0.353f);
        private static readonly Color AccentLogpile = new Color(0.725f, 0.565f, 0.353f);
        private static readonly Color AccentFuelDrum = new Color(0.878f, 0.667f, 0.063f);
        private static readonly Color AccentDevice = new Color(0.369f, 0.780f, 1f);
        private static readonly Color AccentCrystal = new Color(0.706f, 0.549f, 1f);

        private static Color GetAccentColor(NodeType type) => type switch
        {
            NodeType.Wreck => AccentWreck, NodeType.Logpile => AccentLogpile,
            NodeType.FuelDrum => AccentFuelDrum, NodeType.Device => AccentDevice,
            NodeType.Crystal => AccentCrystal, _ => Color.white
        };

        [Header("Resource Node Data")]
        public List<ResourceNodeDef> Nodes = new List<ResourceNodeDef>();
        [Header("Pickup Settings")]
        public KeyCode PickupKey = KeyCode.E;
        public float PickupRange = PickRange;

        private List<GameObject> _nodeObjects = new List<GameObject>();
        private Dictionary<string, NodeVisual> _visuals = new Dictionary<string, NodeVisual>();
        private string _lastNearbyId;

        private void Start()
        {
            GenerateNodeVisuals();
            GameEvents.OnResourcesChanged += OnResourcesChanged;
        }

        private void OnDestroy()
        {
            GameEvents.OnResourcesChanged -= OnResourcesChanged;
            foreach (var obj in _nodeObjects) if (obj != null) Destroy(obj);
            _nodeObjects.Clear(); _visuals.Clear();
        }

        private void Update()
        {
            UpdateNearbyDetection();
            UpdateAnimations();
            HandlePickupInput();
        }

        private void GenerateNodeVisuals()
        {
            var gm = GameManager.Instance;
            var pickedSet = new HashSet<string>(gm.PickedResources);
            foreach (var node in Nodes)
            {
                if (pickedSet.Contains(node.Id)) continue;
                var go = CreateNodeVisual(node);
                if (go != null)
                {
                    go.transform.SetParent(transform);
                    _nodeObjects.Add(go);
                    _visuals[node.Id] = new NodeVisual
                    {
                        GameObject = go, Def = node,
                        AccentMaterial = FindAccentMaterial(go),
                        Phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f)
                    };
                }
            }
        }

        private void UpdateNearbyDetection()
        {
            var gm = GameManager.Instance;
            var pos = gm.VehicleTransient.Position;
            float cx = pos[0], cz = pos[1];
            ResourceNodeDef nearest = null;
            float nearestDist = PickupRange;
            var pickedSet = new HashSet<string>(gm.PickedResources);
            foreach (var node in Nodes)
            {
                if (pickedSet.Contains(node.Id)) continue;
                float dx = node.X - cx, dz = node.Z - cz;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist < nearestDist) { nearestDist = dist; nearest = node; }
            }
            string id = nearest?.Id;
            if (id != _lastNearbyId)
            {
                _lastNearbyId = id;
                gm.SetNearbyResource(nearest != null ? new NearbyResource
                {
                    Id = nearest.Id, Kind = nearest.Kind, Amount = nearest.Amount, Distance = nearestDist
                } : null);
            }
        }

        private void UpdateAnimations()
        {
            float t = Time.time;
            foreach (var kvp in _visuals)
            {
                var vis = kvp.Value;
                if (vis.GameObject == null || !vis.GameObject.activeSelf) continue;
                if (vis.AccentMaterial != null)
                {
                    float intensity = 0.8f + Mathf.Sin(t * 2.2f + vis.Phase) * 0.5f;
                    vis.AccentMaterial.SetColor("_EmissionColor", GetAccentColor(vis.Def.NodeTypeVal) * intensity);
                }
                if (vis.Def.NodeTypeVal == NodeType.Crystal)
                    vis.GameObject.transform.rotation *= Quaternion.Euler(0f, 0.6f * Time.deltaTime * 60f, 0f);
            }
        }

        private void HandlePickupInput()
        {
            if (!Input.GetKeyDown(PickupKey)) return;
            var gm = GameManager.Instance;
            var near = gm.NearbyResourceData;
            if (near == null) return;
            int added = gm.PickupResource(near.Id, near.Kind, near.Amount);
            if (added > 0)
            {
                if (Audio.AudioManager.Instance != null) Audio.AudioManager.Instance.PlayInteractClick();
                Core.AchievementSystem.CheckExploreAchievements();
                Core.AchievementSystem.CheckCollectAchievements();
                if (_visuals.TryGetValue(near.Id, out var vis) && vis.GameObject != null)
                    vis.GameObject.SetActive(false);
                gm.SetNearbyResource(null);
                _lastNearbyId = null;
            }
        }

        private void OnResourcesChanged(ResourceBag _) { }

        private GameObject CreateNodeVisual(ResourceNodeDef node)
        {
            var root = new GameObject("Node_" + node.Id);
            root.transform.position = new Vector3(node.X, node.Y, node.Z);
            root.transform.rotation = Quaternion.Euler(0f, node.RotationY * Mathf.Rad2Deg, 0f);
            switch (node.NodeTypeVal)
            {
                case NodeType.Wreck: BuildWreck(root.transform); break;
                case NodeType.Logpile: BuildLogpile(root.transform); break;
                case NodeType.FuelDrum: BuildFuelDrum(root.transform); break;
                case NodeType.Device: BuildDevice(root.transform); break;
                case NodeType.Crystal: BuildCrystal(root.transform); break;
            }
            return root;
        }

        private void BuildWreck(Transform p)
        {
            var accent = GetAccentColor(NodeType.Wreck);
            AddBox(p, new Vector3(0, 0.45f, 0), Quaternion.identity, new Vector3(1.5f, 0.55f, 2.6f), new Color(0.357f, 0.310f, 0.263f), 1f);
            AddBox(p, new Vector3(0, 0.95f, -0.2f), Quaternion.identity, new Vector3(1.3f, 0.5f, 1.2f), new Color(0.290f, 0.251f, 0.212f), 1f);
            AddBox(p, new Vector3(0, 0.55f, 1.32f), Quaternion.identity, new Vector3(1.2f, 0.25f, 0.1f), accent, 0.5f, 0.4f, accent, 1f, true);
        }

        private void BuildLogpile(Transform p)
        {
            var accent = GetAccentColor(NodeType.Logpile);
            foreach (float x in new[] { -0.45f, 0.45f })
                AddCylinder(p, new Vector3(x, 0.35f, 0), Quaternion.Euler(90, 0, 0), 0.35f, 0.38f, 2.4f, new Color(0.353f, 0.251f, 0.188f), 1f);
            AddCylinder(p, new Vector3(0, 0.95f, 0), Quaternion.Euler(90, 0, 0), 0.35f, 0.38f, 2.4f, new Color(0.396f, 0.286f, 0.184f), 1f);
            AddSphere(p, new Vector3(0, 0.62f, 1.25f), 0.22f, accent, 0.6f, accent, 1f, true);
        }

        private void BuildFuelDrum(Transform p)
        {
            var accent = GetAccentColor(NodeType.FuelDrum);
            AddCylinder(p, new Vector3(0, 0.6f, 0), Quaternion.identity, 0.5f, 0.5f, 1.2f, new Color(0.478f, 0.353f, 0.165f), 0.8f, 0.3f);
            AddCylinder(p, new Vector3(0, 0.95f, 0), Quaternion.identity, 0.52f, 0.52f, 0.12f, accent, 0.5f, 0f, accent, 1f, true);
        }

        private void BuildDevice(Transform p)
        {
            var accent = GetAccentColor(NodeType.Device);
            AddBox(p, new Vector3(0, 0.5f, 0), Quaternion.identity, new Vector3(0.9f, 1.0f, 0.7f), new Color(0.227f, 0.251f, 0.282f), 0.7f, 0.3f);
            AddCylinder(p, new Vector3(0, 1.3f, 0), Quaternion.identity, 0.03f, 0.03f, 0.8f, new Color(0.533f, 0.533f, 0.533f), 0.6f);
            AddBox(p, new Vector3(0, 0.55f, 0.37f), Quaternion.identity, new Vector3(0.5f, 0.3f, 0.06f), accent, 0.4f, 0f, accent, 1f, true);
        }

        private void BuildCrystal(Transform p)
        {
            var accent = GetAccentColor(NodeType.Crystal);
            AddSphere(p, new Vector3(0, 0.7f, 0), 0.55f, accent, 0.2f, accent, 1.4f, true);
            AddCone(p, new Vector3(0, 0.12f, 0), Quaternion.identity, 0.5f, 0.3f, 6, new Color(0.227f, 0.208f, 0.314f), 0.8f);
        }

        private Material FindAccentMaterial(GameObject go)
        {
            foreach (var r in go.GetComponentsInChildren<Renderer>())
                if (r.gameObject.CompareTag("Accent")) return r.material;
            return null;
        }

        private static GameObject AddBox(Transform parent, Vector3 pos, Quaternion rot, Vector3 size, Color color, float roughness = 1f, float metalness = 0f, Color? emissiveColor = null, float emissiveIntensity = 0f, bool isAccent = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = size;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color; mat.SetFloat("_Smoothness", 1f - roughness); mat.SetFloat("_Metallic", metalness);
            if (emissiveColor.HasValue && emissiveIntensity > 0f) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emissiveColor.Value * emissiveIntensity); }
            go.GetComponent<Renderer>().material = mat;
            if (isAccent) go.tag = "Accent";
            return go;
        }

        private static GameObject AddCylinder(Transform parent, Vector3 pos, Quaternion rot, float radiusTop, float radiusBottom, float height, Color color, float roughness = 1f, float metalness = 0f, Color? emissiveColor = null, float emissiveIntensity = 0f, bool isAccent = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localRotation = rot;
            float avgR = (radiusTop + radiusBottom) * 0.5f;
            go.transform.localScale = new Vector3(avgR * 2f, height * 0.5f, avgR * 2f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color; mat.SetFloat("_Smoothness", 1f - roughness); mat.SetFloat("_Metallic", metalness);
            if (emissiveColor.HasValue && emissiveIntensity > 0f) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emissiveColor.Value * emissiveIntensity); }
            go.GetComponent<Renderer>().material = mat;
            if (isAccent) go.tag = "Accent";
            return go;
        }

        private static GameObject AddSphere(Transform parent, Vector3 pos, float radius, Color color, float roughness = 1f, Color? emissiveColor = null, float emissiveIntensity = 0f, bool isAccent = false)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localScale = Vector3.one * radius * 2f;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color; mat.SetFloat("_Smoothness", 1f - roughness);
            if (emissiveColor.HasValue && emissiveIntensity > 0f) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emissiveColor.Value * emissiveIntensity); }
            go.GetComponent<Renderer>().material = mat;
            if (isAccent) go.tag = "Accent";
            return go;
        }

        private static GameObject AddCone(Transform parent, Vector3 pos, Quaternion rot, float radius, float height, int segments, Color color, float roughness = 1f)
        {
            var go = new GameObject("Cone");
            go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localRotation = rot;
            var mesh = SceneryElements.CreateConeMesh(radius, height, segments);
            go.AddComponent<MeshFilter>().mesh = mesh;
            var rend = go.AddComponent<MeshRenderer>();
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = color; mat.SetFloat("_Smoothness", 1f - roughness);
            rend.material = mat;
            return go;
        }

        public static List<ResourceNodeDef> GenerateResourceNodes(int seed = 123, int count = 40)
        {
            var result = new List<ResourceNodeDef>();
            var rng = new System.Random(seed);
            NodeType[] types = { NodeType.Wreck, NodeType.Logpile, NodeType.FuelDrum, NodeType.Device, NodeType.Crystal };
            ResourceKind[] kinds = { ResourceKind.Metal, ResourceKind.Wood, ResourceKind.Fuel, ResourceKind.Signal, ResourceKind.Crystal };
            for (int i = 0; i < count; i++)
            {
                int typeIdx = rng.Next(types.Length);
                var nodeType = types[typeIdx];
                float routeZ = Mathf.Lerp(260f, -1480f, (float)rng.NextDouble());
                var sample = TerrainHeight.RoadSampler?.Sample(TerrainHeight.RouteToWorldZ(routeZ));
                if (sample == null) continue;
                float side = rng.NextDouble() > 0.5 ? 1f : -1f;
                float dist = sample.Value.HalfWidth + 2f + (float)rng.NextDouble() * 20f;
                float x = sample.Value.CenterX + side * dist;
                float z = TerrainHeight.RouteToWorldZ(routeZ);
                float y = TerrainHeight.GetTerrainHeight(x, z);
                result.Add(new ResourceNodeDef
                {
                    Id = "res_" + i, NodeTypeVal = nodeType, Kind = kinds[typeIdx],
                    Amount = 1 + rng.Next(3), X = x, Y = y, Z = z,
                    RotationY = (float)rng.NextDouble() * Mathf.PI * 2f
                });
            }
            return result;
        }

        private class NodeVisual
        {
            public GameObject GameObject;
            public ResourceNodeDef Def;
            public Material AccentMaterial;
            public float Phase;
        }
    }
}

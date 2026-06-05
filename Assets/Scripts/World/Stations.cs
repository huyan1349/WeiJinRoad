using System;
using System.Collections.Generic;
using UnityEngine;
using WeiJinRoad.Core;

namespace WeiJinRoad.World
{
    public enum FacilityType { Supply, Shelter, SignalTower, Beacon, Observatory, Bridge }

    [Serializable]
    public class StationSiteDef
    {
        public string Id;
        public string Name;
        public float X, Y, Z;
    }

    [DefaultExecutionOrder(62)]
    public class Stations : MonoBehaviour
    {
        private const float StationDetectRange = 10f;

        private static readonly Color MetalDark = new Color(0.353f, 0.416f, 0.471f);
        private static readonly Color MetalMid = new Color(0.478f, 0.541f, 0.596f);
        private static readonly Color MetalLight = new Color(0.604f, 0.671f, 0.722f);
        private static readonly Color WoodDark = new Color(0.227f, 0.165f, 0.102f);
        private static readonly Color WoodMid = new Color(0.353f, 0.290f, 0.227f);
        private static readonly Color WoodLight = new Color(0.478f, 0.416f, 0.290f);
        private static readonly Color CanvasColor = new Color(0.541f, 0.478f, 0.353f);
        private static readonly Color Concrete = new Color(0.416f, 0.416f, 0.384f);
        private static readonly Color GlassWarm = new Color(1f, 0.910f, 0.627f);
        private static readonly Color LightBlue = new Color(0.369f, 0.780f, 1f);
        private static readonly Color LightGreen = new Color(0.290f, 1f, 0.541f);

        [Header("Station Sites")]
        public List<StationSiteDef> Sites = new List<StationSiteDef>();
        [Header("Detection")]
        public float DetectRange = StationDetectRange;

        private List<GameObject> _facilityObjects = new List<GameObject>();
        private List<GameObject> _markerObjects = new List<GameObject>();
        private string _currentNearbyStation;

        private void Start()
        {
            GenerateSites();
            GameEvents.OnStationBuilt += OnStationBuilt;
        }

        private void OnDestroy()
        {
            GameEvents.OnStationBuilt -= OnStationBuilt;
            foreach (var obj in _facilityObjects) if (obj != null) Destroy(obj);
            foreach (var obj in _markerObjects) if (obj != null) Destroy(obj);
        }

        private void Update() { UpdateNearbyDetection(); }

        private void GenerateSites()
        {
            var gm = GameManager.Instance;
            foreach (var site in Sites)
            {
                if (gm.Stations.TryGetValue(site.Id, out var state) && state.Built)
                    RenderBuiltFacilities(site, state);
                else
                    RenderSiteMarker(site);
            }
        }

        private void UpdateNearbyDetection()
        {
            var gm = GameManager.Instance;
            var pos = gm.VehicleTransient.Position;
            float cx = pos[0], cz = pos[1];
            string nearestId = null;
            float nearestDist = DetectRange;
            foreach (var site in Sites)
            {
                float dx = site.X - cx, dz = site.Z - cz;
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist < nearestDist) { nearestDist = dist; nearestId = site.Id; }
            }
            if (nearestId != _currentNearbyStation) { _currentNearbyStation = nearestId; gm.SetNearbyStation(nearestId); }
        }

        private void OnStationBuilt(string siteId)
        {
            var site = Sites.Find(s => s.Id == siteId);
            if (site == null) return;
            var gm = GameManager.Instance;
            if (gm.Stations.TryGetValue(siteId, out var state) && state.Built)
            {
                foreach (var obj in _markerObjects)
                {
                    if (obj != null && obj.name == "Marker_" + siteId) { Destroy(obj); _markerObjects.Remove(obj); break; }
                }
                RenderBuiltFacilities(site, state);
            }
        }

        private void RenderSiteMarker(StationSiteDef site)
        {
            var go = new GameObject("Marker_" + site.Id);
            go.transform.SetParent(transform);
            go.transform.position = new Vector3(site.X, site.Y + 0.3f, site.Z);
            var diamond = CreateOctahedron(go.transform, Vector3.zero, 0.45f, new Color(0.471f, 0.784f, 1f), 0.3f, new Color(0.471f, 0.784f, 1f), 0.6f);
            diamond.transform.rotation = Quaternion.Euler(0, 45f, 0);
            var floater = go.AddComponent<SiteMarkerAnimation>();
            floater.BaseY = site.Y + 0.3f;
            _markerObjects.Add(go);
        }

        private void RenderBuiltFacilities(StationSiteDef site, StationState state)
        {
            if (state.Facilities == null) return;
            for (int i = 0; i < state.Facilities.Count; i++)
            {
                float offsetX = (i - (state.Facilities.Count - 1) / 2f) * 4f;
                float fx = site.X + offsetX;
                float fy = TerrainHeight.GetTerrainHeight(fx, site.Z);
                float fz = site.Z;
                var facGo = new GameObject("Facility_" + site.Id + "_" + state.Facilities[i]);
                facGo.transform.SetParent(transform);
                facGo.transform.position = new Vector3(fx, fy, fz);
                BuildFacility(facGo.transform, state.Facilities[i]);
                var fadeIn = facGo.AddComponent<FacilityFadeIn>();
                fadeIn.Duration = 0.5f;
                _facilityObjects.Add(facGo);
            }
        }

        private void BuildFacility(Transform parent, FacilityType type)
        {
            switch (type)
            {
                case FacilityType.Supply: BuildSupplyDepot(parent); break;
                case FacilityType.Shelter: BuildShelter(parent); break;
                case FacilityType.SignalTower: BuildSignalTower(parent); break;
                case FacilityType.Beacon: BuildBeacon(parent); break;
                case FacilityType.Observatory: BuildObservatory(parent); break;
                case FacilityType.Bridge: BuildBridge(parent); break;
            }
        }

        private void BuildSupplyDepot(Transform p)
        {
            foreach (float z in new[] { -0.6f, 0f, 0.6f }) AddBox(p, new Vector3(0, 0.08f, z), Quaternion.identity, new Vector3(2.0f, 0.12f, 0.08f), MetalDark, 0.6f, 0.5f);
            foreach (float x in new[] { -0.8f, 0.8f }) AddBox(p, new Vector3(x, 0.08f, 0), Quaternion.identity, new Vector3(0.08f, 0.12f, 1.4f), MetalDark, 0.6f, 0.5f);
            AddBox(p, new Vector3(0, 0.14f, 0), Quaternion.Euler(0, 0, 30f), new Vector3(2.2f, 0.04f, 0.04f), MetalDark, 0.7f, 0.4f);
            AddBox(p, new Vector3(0, 0.14f, 0), Quaternion.Euler(0, 0, -30f), new Vector3(2.2f, 0.04f, 0.04f), MetalDark, 0.7f, 0.4f);
            AddBox(p, new Vector3(0, 0.5f, 0), Quaternion.identity, new Vector3(1.6f, 0.7f, 1.2f), MetalMid, 0.7f, 0.4f);
            AddBox(p, new Vector3(0, 0.86f, 0), Quaternion.identity, new Vector3(1.62f, 0.03f, 1.22f), MetalLight, 0.5f, 0.5f);
            AddBox(p, new Vector3(-0.3f, 1.05f, 0.1f), Quaternion.identity, new Vector3(0.9f, 0.5f, 0.85f), new Color(0.541f, 0.604f, 0.659f), 0.65f, 0.35f);
            AddBox(p, new Vector3(0.45f, 0.98f, -0.1f), Quaternion.identity, new Vector3(0.65f, 0.4f, 0.7f), new Color(0.416f, 0.478f, 0.533f), 0.7f, 0.35f);
            AddBox(p, new Vector3(-0.15f, 1.42f, 0), Quaternion.identity, new Vector3(0.5f, 0.25f, 0.5f), MetalLight, 0.6f, 0.3f);
            var lightGo = AddSphere(p, new Vector3(0, 1.65f, 0.6f), 0.08f, LightGreen, 0.2f, LightGreen, 2f);
            var blink = p.gameObject.AddComponent<SupplyDepotLight>();
            blink.LightRenderer = lightGo.GetComponent<Renderer>();
            AddPointLight(p, new Vector3(0, 0.3f, 0), new Color(1f, 0.667f, 0.333f), 2f, 6f);
        }

        private void BuildShelter(Transform p)
        {
            AddBox(p, new Vector3(0, 0.04f, 0), Quaternion.identity, new Vector3(3.0f, 0.08f, 2.4f), WoodDark, 0.95f);
            AddBox(p, new Vector3(-0.75f, 1.3f, 0), Quaternion.Euler(0, 0, 40f), new Vector3(2.4f, 0.1f, 2.3f), WoodMid, 0.9f);
            AddBox(p, new Vector3(0.75f, 1.3f, 0), Quaternion.Euler(0, 0, -40f), new Vector3(2.4f, 0.1f, 2.3f), WoodMid, 0.9f);
            AddBox(p, new Vector3(0, 2.0f, 0), Quaternion.identity, new Vector3(0.1f, 0.1f, 2.4f), WoodDark, 1f);
            foreach (float h in new[] { 0.5f, 0.9f, 1.3f }) AddBox(p, new Vector3(0, h, 0), Quaternion.identity, new Vector3(1.5f - h * 0.4f, 0.06f, 0.06f), WoodDark, 0.9f);
            AddBox(p, new Vector3(-0.55f, 1.45f, 0), Quaternion.Euler(0, 0, 40f), new Vector3(2.2f, 0.03f, 2.2f), CanvasColor, 0.95f);
            AddBox(p, new Vector3(0.55f, 1.45f, 0), Quaternion.Euler(0, 0, -40f), new Vector3(2.2f, 0.03f, 2.2f), CanvasColor, 0.95f);
            AddBox(p, new Vector3(0, 1.0f, -1.15f), Quaternion.identity, new Vector3(1.6f, 1.8f, 0.05f), new Color(0.416f, 0.353f, 0.227f), 1f);
            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 8f) * Mathf.PI * 2f;
                AddSphere(p, new Vector3(Mathf.Cos(angle) * 0.25f, 0.05f, Mathf.Sin(angle) * 0.25f - 0.3f), 0.07f, new Color(0.353f, 0.353f, 0.353f), 1f);
            }
            AddCone(p, new Vector3(0, 0.3f, -0.3f), Quaternion.identity, 0.08f, 0.35f, 6, new Color(1f, 0.533f, 0.133f), 1f, new Color(1f, 0.4f, 0.067f), 2f);
            AddPointLight(p, new Vector3(0, 0.4f, -0.3f), new Color(1f, 0.533f, 0.2f), 4f, 8f);
            AddCylinder(p, new Vector3(0, 2.15f, -0.5f), Quaternion.identity, 0.08f, 0.1f, 0.25f, MetalDark, 0.7f, 0.4f);
        }

        private void BuildSignalTower(Transform p)
        {
            AddBox(p, new Vector3(0, 0.15f, 0), Quaternion.identity, new Vector3(1.4f, 0.3f, 1.4f), Concrete, 0.9f);
            foreach (float x in new[] { -0.6f, 0.6f }) foreach (float z in new[] { -0.6f, 0.6f }) AddBox(p, new Vector3(x, 0.25f, z), Quaternion.identity, new Vector3(0.15f, 0.2f, 0.15f), MetalDark, 0.7f, 0.4f);
            AddCylinder(p, new Vector3(0, 2.8f, 0), Quaternion.identity, 0.1f, 0.16f, 5.2f, MetalMid, 0.6f, 0.5f);
            int pi = 0;
            foreach (float h in new[] { 1.5f, 3.0f, 4.2f })
            {
                float w = 1.0f - pi * 0.15f;
                AddBox(p, new Vector3(0, h, 0), Quaternion.identity, new Vector3(w, 0.06f, 0.06f), MetalDark, 0.7f, 0.4f);
                AddBox(p, new Vector3(0, h, 0), Quaternion.identity, new Vector3(0.06f, 0.06f, w), MetalDark, 0.7f, 0.4f);
                pi++;
            }
            AddCylinder(p, new Vector3(0, 5.8f, 0), Quaternion.identity, 0.02f, 0.03f, 1.0f, MetalDark, 0.5f, 0.6f);
            AddCylinder(p, new Vector3(0.2f, 5.5f, 0), Quaternion.Euler(0, 0, 17f), 0.015f, 0.02f, 0.6f, MetalDark, 0.5f, 0.6f);
            AddCylinder(p, new Vector3(-0.15f, 5.6f, 0.15f), Quaternion.Euler(11f, 17f, -11f), 0.015f, 0.02f, 0.5f, MetalDark, 0.5f, 0.6f);
            var sosGo = AddSphere(p, new Vector3(0, 6.4f, 0), 0.15f, LightBlue, 0.2f, LightBlue, 2f);
            var sos = p.gameObject.AddComponent<SignalTowerSOS>();
            sos.SignalLightRenderer = sosGo.GetComponent<Renderer>();
            sos.PointLight = AddPointLight(p, new Vector3(0, 6.4f, 0), LightBlue, 10f, 35f);
        }

        private void BuildBeacon(Transform p)
        {
            AddCylinder(p, new Vector3(0, 0.25f, 0), Quaternion.identity, 0.9f, 1.1f, 0.5f, new Color(0.353f, 0.353f, 0.314f), 0.95f);
            AddCylinder(p, new Vector3(0, 2.25f, 0), Quaternion.identity, 0.4f, 0.65f, 3.8f, new Color(0.541f, 0.541f, 0.478f), 0.65f, 0.2f);
            AddCylinder(p, new Vector3(0, 4.5f, 0), Quaternion.identity, 0.55f, 0.45f, 0.9f, new Color(0.627f, 0.627f, 0.565f), 0.5f, 0.2f);
            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 8f) * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * 0.42f, z = Mathf.Sin(angle) * 0.42f;
                AddBox(p, new Vector3(x, 4.5f, z), Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0), new Vector3(0.25f, 0.55f, 0.01f), GlassWarm, 0.1f, GlassWarm, 0.5f);
            }
            AddCone(p, new Vector3(0, 5.05f, 0), Quaternion.identity, 0.6f, 0.3f, 8, new Color(0.478f, 0.478f, 0.416f), 0.6f);
            AddSphere(p, new Vector3(0, 5.0f, 0), 0.2f, new Color(1f, 0.957f, 0.816f), 0.1f, new Color(1f, 0.957f, 0.816f), 3f);
            AddPointLight(p, new Vector3(0, 5.0f, 0), new Color(1f, 0.957f, 0.816f), 80f, 90f);
        }

        private void BuildObservatory(Transform p)
        {
            AddCylinder(p, new Vector3(0, 1.5f, 0), Quaternion.identity, 0.06f, 0.08f, 3f, MetalDark, 0.6f, 0.5f);
            for (int i = 0; i < 16; i++)
            {
                float angle = (i / 16f) * Mathf.PI * 3f;
                float y = (i / 16f) * 3.0f;
                float r = 0.6f;
                AddBox(p, new Vector3(Mathf.Cos(angle) * r, y, Mathf.Sin(angle) * r), Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0), new Vector3(0.4f, 0.04f, 0.15f), MetalMid, 0.7f, 0.3f);
            }
            foreach (var pos in new[] { new Vector3(-0.8f, 1.5f, -0.8f), new Vector3(0.8f, 1.5f, -0.8f), new Vector3(-0.8f, 1.5f, 0.8f), new Vector3(0.8f, 1.5f, 0.8f) })
                AddCylinder(p, pos, Quaternion.identity, 0.06f, 0.08f, 3f, MetalDark, 0.7f, 0.3f);
            AddBox(p, new Vector3(0, 3.05f, 0), Quaternion.identity, new Vector3(2.2f, 0.1f, 2.2f), new Color(0.478f, 0.478f, 0.416f), 0.8f, 0.2f);
            for (int i = 0; i < 12; i++)
            {
                float angle = (i / 12f) * Mathf.PI * 2f;
                float fr = 1.05f;
                AddCylinder(p, new Vector3(Mathf.Cos(angle) * fr, 3.35f, Mathf.Sin(angle) * fr), Quaternion.identity, 0.02f, 0.02f, 0.5f, MetalDark, 0.6f, 0.5f);
            }
            var teleGroup = new GameObject("Telescope").transform;
            teleGroup.SetParent(p); teleGroup.localPosition = new Vector3(0.4f, 3.3f, 0);
            teleGroup.localRotation = Quaternion.Euler(17f, 29f, 0f);
            AddCylinder(teleGroup, Vector3.zero, Quaternion.identity, 0.06f, 0.08f, 0.9f, new Color(0.227f, 0.227f, 0.165f), 0.4f, 0.6f);
            AddCylinder(teleGroup, new Vector3(0, 0.5f, 0), Quaternion.identity, 0.1f, 0.06f, 0.08f, MetalDark, 0.3f, 0.7f);
            AddPointLight(p, new Vector3(0, 3.8f, 0), new Color(0.667f, 0.8f, 1f), 1.5f, 5f);
        }

        private void BuildBridge(Transform p)
        {
            foreach (float x in new[] { -1.6f, 1.6f }) foreach (float z in new[] { -2.0f, 0f, 2.0f })
            {
                AddCylinder(p, new Vector3(x, -0.5f, z), Quaternion.identity, 0.12f, 0.15f, 1.5f, WoodDark, 0.9f);
                AddCylinder(p, new Vector3(x, -1.2f, z), Quaternion.identity, 0.18f, 0.22f, 0.5f, new Color(0.290f, 0.290f, 0.259f), 0.95f);
            }
            foreach (float z in new[] { -2.0f, -0.7f, 0.7f, 2.0f }) AddBox(p, new Vector3(0, 0.05f, z), Quaternion.identity, new Vector3(3.4f, 0.18f, 0.12f), WoodMid, 0.9f);
            for (int i = 0; i < 14; i++)
            {
                float x = -1.45f + i * 0.22f;
                Color plankColor = i % 3 == 0 ? WoodLight : i % 3 == 1 ? WoodMid : WoodDark;
                AddBox(p, new Vector3(x, 0.17f, 0), Quaternion.identity, new Vector3(0.2f, 0.05f, 5.0f), plankColor, 0.92f);
            }
            foreach (float x in new[] { -1.55f, 1.55f })
            {
                foreach (float z in new[] { -2.2f, -1.1f, 0f, 1.1f, 2.2f }) AddCylinder(p, new Vector3(x, 0.5f, z), Quaternion.identity, 0.03f, 0.035f, 0.65f, WoodDark, 0.9f);
                foreach (float y in new[] { 0.55f, 0.75f }) AddBox(p, new Vector3(x, y, 0), Quaternion.identity, new Vector3(0.02f, 0.02f, 4.8f), new Color(0.353f, 0.353f, 0.353f), 0.5f, 0.6f);
            }
            AddPointLight(p, new Vector3(0, -1.5f, 0), new Color(0.267f, 0.533f, 0.667f), 1f, 6f);
        }

        private static GameObject AddBox(Transform parent, Vector3 pos, Quaternion rot, Vector3 size, Color color, float roughness = 1f, float metalness = 0f, Color? emissiveColor = null, float emissiveIntensity = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube); go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = size;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = color; mat.SetFloat("_Smoothness", 1f - roughness); mat.SetFloat("_Metallic", metalness);
            if (emissiveColor.HasValue && emissiveIntensity > 0f) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emissiveColor.Value * emissiveIntensity); }
            go.GetComponent<Renderer>().material = mat; return go;
        }

        private static GameObject AddCylinder(Transform parent, Vector3 pos, Quaternion rot, float radiusTop, float radiusBottom, float height, Color color, float roughness = 1f, float metalness = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder); go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localRotation = rot;
            float avgR = (radiusTop + radiusBottom) * 0.5f; go.transform.localScale = new Vector3(avgR * 2f, height * 0.5f, avgR * 2f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = color; mat.SetFloat("_Smoothness", 1f - roughness); mat.SetFloat("_Metallic", metalness);
            go.GetComponent<Renderer>().material = mat; return go;
        }

        private static GameObject AddSphere(Transform parent, Vector3 pos, float radius, Color color, float roughness = 1f, Color? emissiveColor = null, float emissiveIntensity = 0f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localScale = Vector3.one * radius * 2f;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = color; mat.SetFloat("_Smoothness", 1f - roughness);
            if (emissiveColor.HasValue && emissiveIntensity > 0f) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emissiveColor.Value * emissiveIntensity); }
            go.GetComponent<Renderer>().material = mat; return go;
        }

        private static GameObject AddCone(Transform parent, Vector3 pos, Quaternion rot, float radius, float height, int segments, Color color, float roughness = 1f, Color? emissiveColor = null, float emissiveIntensity = 0f)
        {
            var go = new GameObject("Cone"); go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localRotation = rot;
            var mesh = SceneryElements.CreateConeMesh(radius, height, segments); go.AddComponent<MeshFilter>().mesh = mesh;
            var rend = go.AddComponent<MeshRenderer>(); var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = color; mat.SetFloat("_Smoothness", 1f - roughness);
            if (emissiveColor.HasValue && emissiveIntensity > 0f) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emissiveColor.Value * emissiveIntensity); }
            rend.material = mat; return go;
        }

        private static GameObject CreateOctahedron(Transform parent, Vector3 pos, float radius, Color color, float roughness = 1f, Color? emissiveColor = null, float emissiveIntensity = 0f)
        {
            var go = new GameObject("Octahedron"); go.transform.SetParent(parent); go.transform.localPosition = pos;
            var mesh = new Mesh();
            mesh.vertices = new Vector3[] { Vector3.up * radius, Vector3.down * radius, Vector3.right * radius, Vector3.left * radius, Vector3.forward * radius, Vector3.back * radius };
            mesh.triangles = new int[] { 0, 4, 2, 0, 2, 5, 0, 5, 3, 0, 3, 4, 1, 2, 4, 1, 5, 2, 1, 3, 5, 1, 4, 3 };
            mesh.RecalculateNormals(); go.AddComponent<MeshFilter>().mesh = mesh;
            var rend = go.AddComponent<MeshRenderer>(); var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = color; mat.SetFloat("_Smoothness", 1f - roughness);
            if (emissiveColor.HasValue && emissiveIntensity > 0f) { mat.EnableKeyword("_EMISSION"); mat.SetColor("_EmissionColor", emissiveColor.Value * emissiveIntensity); }
            rend.material = mat; return go;
        }

        private static Light AddPointLight(Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var obj = new GameObject("PointLight"); obj.transform.SetParent(parent); obj.transform.localPosition = pos;
            var light = obj.AddComponent<Light>(); light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range; light.shadows = LightShadows.Soft; return light;
        }

        public static List<StationSiteDef> GenerateStationSites(int seed = 777, int count = 9)
        {
            var result = new List<StationSiteDef>(); var rng = new System.Random(seed);
            string[] names = { "霜原驿站", "松林庇护所", "冰湖哨站", "山脊信号站", "雪谷灯塔", "高地观测台", "峡谷桥梁", "冻土补给点", "极光营地" };
            for (int i = 0; i < count; i++)
            {
                float routeZ = Mathf.Lerp(240f, -1400f, (i + 0.5f) / count);
                var sample = TerrainHeight.RoadSampler?.Sample(TerrainHeight.RouteToWorldZ(routeZ)); if (sample == null) continue;
                float side = rng.NextDouble() > 0.5 ? 1f : -1f;
                float dist = sample.Value.HalfWidth + 5f + (float)rng.NextDouble() * 15f;
                float x = sample.Value.CenterX + side * dist;
                float z = TerrainHeight.RouteToWorldZ(routeZ);
                float y = TerrainHeight.GetTerrainHeight(x, z);
                result.Add(new StationSiteDef { Id = "station_" + i, Name = i < names.Length ? names[i] : "站点" + i, X = x, Y = y, Z = z });
            }
            return result;
        }
    }

    public class SiteMarkerAnimation : MonoBehaviour
    {
        public float BaseY; private float _phase;
        private void Start() { _phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f); }
        private void Update()
        {
            float t = Time.time + _phase;
            transform.position = new Vector3(transform.position.x, BaseY + Mathf.Sin(t * 1.5f) * 0.15f, transform.position.z);
            if (transform.childCount > 0) transform.GetChild(0).Rotate(0f, 0.5f * Time.deltaTime * 60f, 0f);
        }
    }

    public class FacilityFadeIn : MonoBehaviour
    {
        public float Duration = 0.5f; private float _elapsed;
        private void Update() { _elapsed += Time.deltaTime; float progress = Mathf.Min(1f, _elapsed / Duration); transform.localScale = Vector3.one * (0.8f + progress * 0.2f); if (progress >= 1f) enabled = false; }
    }

    public class SupplyDepotLight : MonoBehaviour
    {
        public Renderer LightRenderer; private Material _mat;
        private static readonly Color GreenColor = new Color(0.290f, 1f, 0.541f);
        private static readonly Color RedColor = new Color(1f, 0.290f, 0.290f);
        private void Start() { if (LightRenderer != null) _mat = LightRenderer.material; }
        private void Update() { if (_mat == null) return; float t = Time.time; int cycle = Mathf.FloorToInt(t * 0.8f) % 2; var color = cycle == 0 ? GreenColor : RedColor; _mat.SetColor("_EmissionColor", color * (1.5f + Mathf.Sin(t * 6f) * 0.5f)); }
    }

    public class SignalTowerSOS : MonoBehaviour
    {
        public Renderer SignalLightRenderer; public Light PointLight; private Material _mat;
        private void Start() { if (SignalLightRenderer != null) _mat = SignalLightRenderer.material; }
        private void Update()
        {
            if (_mat == null) return; float cycleT = Time.time % 4.8f; int blink = 0;
            if (cycleT < 0.3f) blink = 1; else if (cycleT < 0.6f) blink = 0; else if (cycleT < 0.9f) blink = 1;
            else if (cycleT < 1.2f) blink = 0; else if (cycleT < 1.5f) blink = 1; else if (cycleT < 2.1f) blink = 0;
            else if (cycleT < 2.7f) blink = 1; else if (cycleT < 3.0f) blink = 0; else if (cycleT < 3.6f) blink = 1;
            else if (cycleT < 3.9f) blink = 0; else if (cycleT < 4.5f) blink = 1;
            _mat.SetColor("_EmissionColor", new Color(0.369f, 0.780f, 1f) * (blink * 3f));
            if (PointLight != null) PointLight.intensity = 10f * blink;
        }
    }
}
ENDOFFILE; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-edb31cd03c28417289b5dc600a4c68d8/cwd.txt'; exit "$__tr_native_ec"
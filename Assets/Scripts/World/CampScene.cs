using System;
using System.Collections.Generic;
using UnityEngine;
using WeiJinRoad.Core;

namespace WeiJinRoad.World
{
    [DefaultExecutionOrder(63)]
    public class CampScene : MonoBehaviour
    {
        private static readonly Color CanvasColor = new Color(0.760f, 0.659f, 0.471f);
        private static readonly Color BarkDark = new Color(0.239f, 0.169f, 0.102f);
        private static readonly Color BarkMid = new Color(0.290f, 0.200f, 0.133f);
        private static readonly Color BarkLight = new Color(0.353f, 0.251f, 0.188f);
        private static readonly Color StoneColor = new Color(0.420f, 0.420f, 0.420f);

        [Header("Camp Settings")]
        public float CampRange = 5f;

        private GameObject _tentGroup;
        private GameObject _fireGroup;
        private GameObject _itemsGroup;
        private CampfireAnimator _campfireAnimator;
        private float _popProgress, _fireGrowProgress, _itemsStaggerProgress;

        private void Start() { GameEvents.OnCampingChanged += OnCampingChanged; }

        private void OnDestroy()
        {
            GameEvents.OnCampingChanged -= OnCampingChanged;
            DestroyCampObjects();
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (!gm.Camping || gm.CampSiteData == null) { _popProgress = 0f; _fireGrowProgress = 0f; _itemsStaggerProgress = 0f; return; }
            _popProgress = Mathf.Lerp(_popProgress, 1f, 1f - Mathf.Exp(-5f * Time.deltaTime));
            if (_tentGroup != null) { _tentGroup.transform.localScale = Vector3.one * _popProgress; _tentGroup.transform.localRotation = Quaternion.Euler((1f - _popProgress) * -15f, _tentGroup.transform.localRotation.eulerAngles.y, 0f); }
            _fireGrowProgress = Mathf.Lerp(_fireGrowProgress, 1f, 1f - Mathf.Exp(-4f * Time.deltaTime));
            if (_fireGroup != null) _fireGroup.transform.localScale = Vector3.one * _fireGrowProgress;
            _itemsStaggerProgress = Mathf.Lerp(_itemsStaggerProgress, 1f, 1f - Mathf.Exp(-3.5f * Time.deltaTime));
            if (_itemsGroup != null) _itemsGroup.transform.localScale = Vector3.one * _itemsStaggerProgress;
        }

        private void OnCampingChanged(bool isCamping) { if (isCamping) BuildCamp(); else DestroyCampObjects(); }

        private void BuildCamp()
        {
            var gm = GameManager.Instance; var site = gm.CampSiteData; if (site == null) return;
            float x = site.X, z = site.Z, heading = site.Heading;
            float rightX = Mathf.Cos(heading), rightZ = -Mathf.Sin(heading);
            float fwdX = Mathf.Sin(heading), fwdZ = Mathf.Cos(heading);
            float tentX = x + rightX * 3.4f, tentZ = z + rightZ * 3.4f;
            float tentY = TerrainHeight.GetTerrainHeight(tentX, tentZ);
            float fireX = x + rightX * 1.8f + fwdX * 1.6f, fireZ = z + rightZ * 1.8f + fwdZ * 1.6f;
            float fireY = TerrainHeight.GetTerrainHeight(fireX, fireZ);

            _tentGroup = new GameObject("Tent"); _tentGroup.transform.SetParent(transform);
            _tentGroup.transform.position = new Vector3(tentX, tentY, tentZ);
            _tentGroup.transform.rotation = Quaternion.Euler(0, heading * Mathf.Rad2Deg, 0);
            _tentGroup.transform.localScale = Vector3.zero;
            BuildATent(_tentGroup.transform);

            _fireGroup = new GameObject("Campfire"); _fireGroup.transform.SetParent(transform);
            _fireGroup.transform.position = new Vector3(fireX, fireY, fireZ);
            _fireGroup.transform.localScale = Vector3.zero;
            _campfireAnimator = BuildCampfire(_fireGroup.transform);

            _itemsGroup = new GameObject("CampItems"); _itemsGroup.transform.SetParent(transform);
            _itemsGroup.transform.position = new Vector3(tentX, tentY, tentZ);
            _itemsGroup.transform.rotation = Quaternion.Euler(0, heading * Mathf.Rad2Deg, 0);
            _itemsGroup.transform.localScale = Vector3.zero;
            BuildWoodPile(_itemsGroup.transform);
            BuildKettle(_itemsGroup.transform);
            BuildBackpack(_itemsGroup.transform);
        }

        private void DestroyCampObjects()
        {
            if (_tentGroup != null) { Destroy(_tentGroup); _tentGroup = null; }
            if (_fireGroup != null) { Destroy(_fireGroup); _fireGroup = null; }
            if (_itemsGroup != null) { Destroy(_itemsGroup); _itemsGroup = null; }
            _campfireAnimator = null;
        }

        private void BuildATent(Transform p)
        {
            float tw = 2.2f, th = 1.65f, td = 2.6f;
            AddPlane(p, new Vector3(0, 0.02f, 0), Quaternion.Euler(90, 0, 0), new Vector2(tw + 0.4f, td + 0.2f), new Color(0.227f, 0.188f, 0.149f), 1f);
            AddBox(p, new Vector3(-tw * 0.25f, th * 0.5f, 0), Quaternion.Euler(0, 0, 40f), new Vector3(tw * 0.7f, 0.03f, td), CanvasColor, 0.95f);
            AddBox(p, new Vector3(tw * 0.25f, th * 0.5f, 0), Quaternion.Euler(0, 0, -40f), new Vector3(tw * 0.7f, 0.03f, td), CanvasColor, 0.95f);
            AddBox(p, new Vector3(0, th * 0.5f, -td * 0.5f), Quaternion.identity, new Vector3(tw, th, 0.03f), CanvasColor, 0.95f);
            AddBox(p, new Vector3(0, th * 0.5f, td * 0.5f), Quaternion.identity, new Vector3(tw, th, 0.03f), CanvasColor, 0.95f);
            AddPlane(p, new Vector3(0, th - 0.08f, td * 0.5f + 0.01f), Quaternion.Euler(90, 0, 0), new Vector2(0.3f, 0.15f), new Color(0.102f, 0.082f, 0.063f), 1f);
            var flapGo = AddPlane(p, new Vector3(0.15f, th * 0.35f, td * 0.5f + 0.05f), Quaternion.Euler(-20f, 7f, 0), new Vector2(tw * 0.45f, th * 0.7f), new Color(0.722f, 0.620f, 0.416f), 0.95f);
            flapGo.AddComponent<TentFlapAnimation>();
            AddBox(p, new Vector3(0, 0.06f, -0.3f), Quaternion.Euler(0, 5.7f, 0), new Vector3(0.55f, 0.08f, 1.6f), new Color(0.102f, 0.165f, 0.102f), 0.9f);
            AddBox(p, new Vector3(-0.5f, 0.18f, 0.4f), Quaternion.Euler(0, -17.2f, 0), new Vector3(0.28f, 0.35f, 0.22f), new Color(0.227f, 0.290f, 0.165f), 0.85f);
            foreach (float bx in new[] { 0.35f, -0.15f })
            {
                var bootGroup = new GameObject("Boot"); bootGroup.transform.SetParent(p);
                bootGroup.transform.localPosition = new Vector3(bx, 0, td * 0.5f + 0.15f);
                AddBox(bootGroup.transform, new Vector3(0, 0.04f, 0), Quaternion.identity, new Vector3(0.08f, 0.08f, 0.18f), new Color(0.165f, 0.125f, 0.094f), 0.9f);
                AddCylinder(bootGroup.transform, new Vector3(0, 0.06f, -0.09f), Quaternion.Euler(11.5f, 0, 0), 0.035f, 0.04f, 0.1f, new Color(0.165f, 0.125f, 0.094f), 0.9f);
            }
            AddSphere(p, new Vector3(0, th * 0.7f, 0), 0.5f, new Color(0.910f, 0.910f, 0.941f), 0.5f);
            float[] stakeX = { tw * 0.5f + 0.8f, -(tw * 0.5f + 0.8f), tw * 0.5f + 0.8f, -(tw * 0.5f + 0.8f) };
            float[] stakeZ = { td * 0.5f + 0.4f, td * 0.5f + 0.4f, -(td * 0.5f + 0.4f), -(td * 0.5f + 0.4f) };
            for (int i = 0; i < 4; i++) AddCylinder(p, new Vector3(stakeX[i], 0.12f, stakeZ[i]), Quaternion.Euler(8.6f, 0, 0), 0.02f, 0.015f, 0.3f, new Color(0.353f, 0.275f, 0.204f), 1f);
            AddCylinder(p, new Vector3(0, th, 0), Quaternion.identity, 0.025f, 0.025f, td + 0.3f, new Color(0.353f, 0.275f, 0.204f), 1f);
        }

        private CampfireAnimator BuildCampfire(Transform p)
        {
            AddDodecahedron(p, new Vector3(0.28f, 0.05f, 0.22f), new Vector3(1.2f, 0.8f, 1.1f), StoneColor, 0.95f);
            AddDodecahedron(p, new Vector3(-0.3f, 0.05f, 0.15f), new Vector3(1.0f, 0.9f, 1.3f), StoneColor, 0.95f);
            AddDodecahedron(p, new Vector3(0.0f, 0.05f, -0.3f), new Vector3(1.3f, 0.7f, 1.0f), StoneColor, 0.95f);
            AddCylinder(p, new Vector3(0.15f, 0.1f, 0.05f), Quaternion.Euler(0, 28.6f, 81.8f), 0.06f, 0.065f, 0.85f, BarkMid, 1f);
            AddCylinder(p, new Vector3(-0.15f, 0.1f, -0.05f), Quaternion.Euler(0, -22.9f, -78.3f), 0.06f, 0.065f, 0.8f, BarkMid, 1f);
            AddCylinder(p, new Vector3(0.05f, 0.16f, 0.12f), Quaternion.Euler(0, 51.6f, 85.7f), 0.05f, 0.055f, 0.7f, BarkLight, 1f);
            AddCylinder(p, new Vector3(-0.08f, 0.16f, -0.1f), Quaternion.Euler(0, -40.1f, -90f), 0.05f, 0.055f, 0.75f, BarkLight, 1f);
            AddCylinder(p, new Vector3(0.1f, 0.22f, -0.05f), Quaternion.Euler(11.5f, 17.2f, 82.9f), 0.04f, 0.045f, 0.6f, BarkDark, 1f);
            AddCylinder(p, new Vector3(-0.05f, 0.22f, 0.08f), Quaternion.Euler(-8.6f, -34.4f, -84f), 0.04f, 0.045f, 0.65f, BarkDark, 1f);
            AddCylinder(p, new Vector3(0.0f, 0.28f, 0.0f), Quaternion.Euler(5.7f, 68.8f, 83.5f), 0.035f, 0.04f, 0.5f, BarkDark, 1f);
            var innerFlame = AddCone(p, new Vector3(0, 0.30f, 0), Quaternion.identity, 0.12f, 0.45f, 7, new Color(1f, 0.878f, 0.439f), 1f, new Color(1f, 0.753f, 0.267f), 2f);
            var outerFlame = AddCone(p, new Vector3(0.03f, 0.35f, 0.02f), Quaternion.identity, 0.18f, 0.6f, 7, new Color(1f, 0.541f, 0.165f), 1f, new Color(1f, 0.267f, 0f), 1.5f);
            var mainLight = AddPointLight(p, new Vector3(0, 0.7f, 0), new Color(1f, 0.604f, 0.235f), 16f, 25f);
            var groundLight = AddPointLight(p, new Vector3(0, 0.15f, 0), new Color(1f, 0.478f, 0.125f), 3f, 8f);
            var animator = p.gameObject.AddComponent<CampfireAnimator>();
            animator.InnerFlame = innerFlame.transform; animator.OuterFlame = outerFlame.transform;
            animator.MainLight = mainLight; animator.GroundLight = groundLight;
            return animator;
        }

        private void BuildWoodPile(Transform p)
        {
            var group = new GameObject("WoodPile"); group.transform.SetParent(p);
            group.transform.localPosition = new Vector3(1.8f, 0, 0.6f);
            group.transform.localRotation = Quaternion.Euler(0, 22.9f, 0);
            AddCylinder(group.transform, new Vector3(-0.15f, 0.04f, 0), Quaternion.Euler(0, 0, 90), 0.05f, 0.055f, 0.7f, BarkDark, 1f);
            AddCylinder(group.transform, new Vector3(0, 0.04f, 0.08f), Quaternion.Euler(0, 5.7f, 90), 0.045f, 0.05f, 0.65f, BarkLight, 1f);
            AddCylinder(group.transform, new Vector3(0.15f, 0.04f, -0.04f), Quaternion.Euler(0, -8.6f, 90), 0.05f, 0.06f, 0.72f, BarkMid, 1f);
            AddCylinder(group.transform, new Vector3(-0.05f, 0.11f, 0.03f), Quaternion.Euler(0, 11.5f, 90), 0.04f, 0.045f, 0.6f, BarkDark, 1f);
            AddCylinder(group.transform, new Vector3(0.08f, 0.11f, -0.02f), Quaternion.Euler(0, -5.7f, 90), 0.042f, 0.048f, 0.58f, BarkLight, 1f);
            AddBox(group.transform, new Vector3(0, 0.14f, 0), Quaternion.Euler(-11.5f, 17.2f, 0), new Vector3(0.5f, 0.02f, 0.35f), new Color(0.478f, 0.416f, 0.314f), 0.95f);
        }

        private void BuildKettle(Transform p)
        {
            var group = new GameObject("Kettle"); group.transform.SetParent(p);
            group.transform.localPosition = new Vector3(-0.5f, 0, -1.2f);
            group.transform.localRotation = Quaternion.Euler(0, 45.8f, 0);
            AddCylinder(group.transform, new Vector3(0, 0.14f, 0), Quaternion.identity, 0.1f, 0.09f, 0.18f, new Color(0.541f, 0.541f, 0.541f), 0.3f, 0.8f);
            AddCylinder(group.transform, new Vector3(0, 0.24f, 0), Quaternion.identity, 0.08f, 0.1f, 0.03f, new Color(0.604f, 0.604f, 0.604f), 0.3f, 0.8f);
            AddSphere(group.transform, new Vector3(0, 0.27f, 0), 0.025f, new Color(0.416f, 0.416f, 0.416f), 0.4f);
            AddCylinder(group.transform, new Vector3(-0.12f, 0.18f, 0), Quaternion.Euler(0, 0, -45f), 0.02f, 0.03f, 0.1f, new Color(0.541f, 0.541f, 0.541f), 0.3f, 0.8f);
            AddDodecahedron(group.transform, new Vector3(0, 0.04f, 0.12f), new Vector3(1.1f, 0.6f, 0.9f), StoneColor, 0.95f);
        }

        private void BuildBackpack(Transform p)
        {
            var group = new GameObject("Backpack"); group.transform.SetParent(p);
            group.transform.localPosition = new Vector3(-1.2f, 0, 1.0f);
            group.transform.localRotation = Quaternion.Euler(0, -28.6f, 0);
            AddBox(group.transform, new Vector3(0, 0.28f, 0), Quaternion.identity, new Vector3(0.35f, 0.5f, 0.25f), new Color(0.290f, 0.353f, 0.227f), 0.85f);
            AddBox(group.transform, new Vector3(0, 0.54f, 0.02f), Quaternion.Euler(-8.6f, 0, 0), new Vector3(0.36f, 0.06f, 0.28f), new Color(0.227f, 0.290f, 0.165f), 0.85f);
            AddBox(group.transform, new Vector3(0, 0.2f, 0.13f), Quaternion.identity, new Vector3(0.25f, 0.2f, 0.04f), new Color(0.227f, 0.290f, 0.165f), 0.9f);
            AddBox(group.transform, new Vector3(0.19f, 0.25f, 0), Quaternion.identity, new Vector3(0.04f, 0.2f, 0.18f), new Color(0.227f, 0.290f, 0.165f), 0.9f);
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

        private static GameObject AddSphere(Transform parent, Vector3 pos, float radius, Color color, float roughness = 1f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localScale = Vector3.one * radius * 2f;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = color; mat.SetFloat("_Smoothness", 1f - roughness);
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

        private static GameObject AddPlane(Transform parent, Vector3 pos, Quaternion rot, Vector2 size, Color color, float roughness = 1f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad); go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localRotation = rot; go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = color; mat.SetFloat("_Smoothness", 1f - roughness);
            go.GetComponent<Renderer>().material = mat; return go;
        }

        private static void AddDodecahedron(Transform parent, Vector3 pos, Vector3 scale, Color color, float roughness = 1f)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere); go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localScale = scale * 0.3f;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = color; mat.SetFloat("_Smoothness", 1f - roughness);
            go.GetComponent<Renderer>().material = mat;
        }

        private static Light AddPointLight(Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var obj = new GameObject("PointLight"); obj.transform.SetParent(parent); obj.transform.localPosition = pos;
            var light = obj.AddComponent<Light>(); light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range; light.shadows = LightShadows.Soft; return light;
        }
    }

    public class CampfireAnimator : MonoBehaviour
    {
        public Transform InnerFlame; public Transform OuterFlame; public Light MainLight; public Light GroundLight;
        private void Update()
        {
            float t = Time.time;
            if (InnerFlame != null) { float sy = 0.8f + Mathf.Sin(t * 8f) * 0.15f + Mathf.Sin(t * 13f) * 0.08f; InnerFlame.localScale = new Vector3(0.9f + Mathf.Sin(t * 6f + 1f) * 0.1f, sy, 0.9f + Mathf.Cos(t * 7f) * 0.1f); }
            if (OuterFlame != null) { float sy = 0.85f + Mathf.Sin(t * 7f + 2f) * 0.12f; OuterFlame.localScale = new Vector3(0.9f + Mathf.Sin(t * 5f) * 0.08f, sy, 0.9f + Mathf.Cos(t * 6f) * 0.08f); }
            float flicker = 0.7f + Mathf.Sin(t * 11f) * 0.15f + Mathf.Sin(t * 23.3f) * 0.1f + Mathf.Sin(t * 7.1f) * 0.05f;
            if (MainLight != null) MainLight.intensity = 10f + flicker * 8f;
            if (GroundLight != null) GroundLight.intensity = 2f + flicker * 1.5f;
        }
    }

    public class TentFlapAnimation : MonoBehaviour
    {
        private void Update() { float t = Time.time; transform.localRotation = Quaternion.Euler(-20f + Mathf.Sin(t * 1.8f) * 3.4f + Mathf.Sin(t * 2.7f) * 1.7f, 7f, 0f); }
    }
}

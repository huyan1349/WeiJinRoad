using System;
using System.Collections.Generic;
using UnityEngine;
using WeiJinRoad.Core;

namespace WeiJinRoad.World
{
    public enum ShopType { Garage, Supply, Trade, Tavern, Signal, Fuel }

    [Serializable]
    public class ShopDef
    {
        public string Id;
        public ShopType Type;
        public Vector3 Position;
        public float Rotation;
        public Vector3 Size;
        public Color WallColor;
        public Color RoofColor;
        public Color LightColor;
    }

    [DefaultExecutionOrder(64)]
    public class TownScene : MonoBehaviour
    {
        private static readonly Color SnowColor = new Color(0.910f, 0.933f, 0.957f);
        private static readonly Color RoadColor = new Color(0.227f, 0.227f, 0.227f);
        private static readonly Color WoodDark = new Color(0.227f, 0.165f, 0.102f);
        private static readonly Color WoodMid = new Color(0.353f, 0.290f, 0.227f);
        private static readonly Color FenceColor = new Color(0.416f, 0.353f, 0.251f);

        [Header("Town Center")]
        public Vector3 TownCenter = new Vector3(-10f, 0f, -500f);
        public float TownDetectRange = 20f;

        [Header("Shops")]
        public List<ShopDef> Shops = new List<ShopDef>();

        [Header("Fork Sign")]
        public Vector3 ForkPosition = new Vector3(-5f, 0f, -460f);

        private List<GameObject> _townObjects = new List<GameObject>();

        private void Start() { GenerateTown(); }

        private void OnDestroy()
        {
            foreach (var obj in _townObjects) if (obj != null) Destroy(obj);
            _townObjects.Clear();
        }

        private void Update() { UpdateTownDetection(); }

        private void GenerateTown()
        {
            BuildForkSign();
            BuildBranchRoad();
            BuildTownGround();
            foreach (var shop in Shops) BuildShop(shop);
            BuildMarketSquare();
            BuildStreetLamps();
            BuildFencePerimeter();
            BuildSnowman(new Vector3(TownCenter.x + 8f, 0f, TownCenter.z + 5f));
            BuildSnowman(new Vector3(TownCenter.x - 5f, 0f, TownCenter.z - 8f));
            BuildAbandonedCar(new Vector3(TownCenter.x + 12f, 0f, TownCenter.z - 3f), 0.3f);
            BuildAbandonedCar(new Vector3(TownCenter.x - 10f, 0f, TownCenter.z + 8f), -0.8f);
            float centerY = TerrainHeight.GetTerrainHeight(TownCenter.x, TownCenter.z);
            AddPointLight(transform, new Vector3(TownCenter.x, centerY + 4f, TownCenter.z), new Color(1f, 0.910f, 0.753f), 1.5f, 25f);
        }

        private void UpdateTownDetection()
        {
            var gm = GameManager.Instance; var pos = gm.VehicleTransient.Position; float cx = pos[0], cz = pos[1];
            float dx = TownCenter.x - cx, dz = TownCenter.z - cz;
            float dist = Mathf.Sqrt(dx * dx + dz * dz);
            if (dist < TownDetectRange)
            {
                if (!gm.TownVisited) { gm.SetTownVisited(true); Core.AchievementSystem.CheckAchievement("visit_town"); }
                string nearestShopId = null; float nearestShopDist = 8f;
                foreach (var shop in Shops)
                {
                    float sx = TownCenter.x + shop.Position.x, sz = TownCenter.z + shop.Position.z;
                    float sdx = sx - cx, sdz = sz - cz;
                    float sDist = Mathf.Sqrt(sdx * sdx + sdz * sdz);
                    if (sDist < nearestShopDist) { nearestShopDist = sDist; nearestShopId = shop.Id; }
                }
                gm.SetNearbyShop(nearestShopId);
            }
            else { if (gm.NearbyShop != null) gm.SetNearbyShop(null); }
        }

        private void BuildForkSign()
        {
            float forkY = TerrainHeight.GetTerrainHeight(ForkPosition.x, ForkPosition.z);
            var go = new GameObject("ForkSign"); go.transform.SetParent(transform);
            go.transform.position = new Vector3(ForkPosition.x, forkY, ForkPosition.z);
            AddCylinder(go.transform, new Vector3(0, 1.2f, 0), Quaternion.identity, 0.06f, 0.08f, 2.4f, WoodDark, 0.9f);
            AddBox(go.transform, new Vector3(0, 2.6f, 0.04f), Quaternion.identity, new Vector3(1.8f, 0.7f, 0.06f), new Color(0.353f, 0.227f, 0.102f), 0.95f);
            AddBox(go.transform, new Vector3(-0.3f, 2.65f, 0.08f), Quaternion.identity, new Vector3(0.8f, 0.12f, 0.02f), new Color(0.910f, 0.878f, 0.816f), 1f, new Color(0.910f, 0.878f, 0.816f), 0.15f);
            AddBox(go.transform, new Vector3(0.3f, 2.45f, 0.08f), Quaternion.identity, new Vector3(0.8f, 0.12f, 0.02f), new Color(0.910f, 0.878f, 0.816f), 1f, new Color(0.910f, 0.878f, 0.816f), 0.15f);
            AddBox(go.transform, new Vector3(0, 2.98f, 0), Quaternion.identity, new Vector3(1.9f, 0.06f, 0.12f), SnowColor, 0.8f);
            _townObjects.Add(go);
        }

        private void BuildBranchRoad()
        {
            var roadObj = new GameObject("BranchRoad"); roadObj.transform.SetParent(transform);
            int segments = 30;
            for (int i = 0; i < segments; i++)
            {
                float t = i / (float)segments;
                float x = Mathf.Lerp(ForkPosition.x, TownCenter.x, t) + Mathf.Sin(t * Mathf.PI) * 3f;
                float z = Mathf.Lerp(ForkPosition.z, TownCenter.z, t);
                float y = TerrainHeight.GetTerrainHeight(x, z) + 0.05f;
                var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
                quad.transform.SetParent(roadObj.transform); quad.transform.position = new Vector3(x, y, z);
                quad.transform.rotation = Quaternion.Euler(90, 0, 0); quad.transform.localScale = new Vector3(7f, 1f, 1f);
                var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = RoadColor; mat.SetFloat("_Smoothness", 0.15f);
                quad.GetComponent<Renderer>().material = mat;
            }
            _townObjects.Add(roadObj);
        }

        private void BuildTownGround()
        {
            float centerY = TerrainHeight.GetTerrainHeight(TownCenter.x, TownCenter.z);
            var groundObj = new GameObject("TownGround"); groundObj.transform.SetParent(transform);
            var groundQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            groundQuad.transform.SetParent(groundObj.transform);
            groundQuad.transform.position = new Vector3(TownCenter.x, centerY + 0.01f, TownCenter.z);
            groundQuad.transform.rotation = Quaternion.Euler(90, 0, 0); groundQuad.transform.localScale = new Vector3(32f, 32f, 1f);
            var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit")); groundMat.color = SnowColor; groundMat.SetFloat("_Smoothness", 0.1f);
            groundQuad.GetComponent<Renderer>().material = groundMat;
            _townObjects.Add(groundObj);
        }

        private void BuildShop(ShopDef shop)
        {
            float worldX = TownCenter.x + shop.Position.x, worldZ = TownCenter.z + shop.Position.z;
            float baseY = TerrainHeight.GetTerrainHeight(worldX, worldZ);
            float w = shop.Size.x, h = shop.Size.y, d = shop.Size.z;
            var go = new GameObject("Shop_" + shop.Id); go.transform.SetParent(transform);
            go.transform.position = new Vector3(worldX, baseY, worldZ);
            go.transform.rotation = Quaternion.Euler(0, shop.Rotation * Mathf.Rad2Deg, 0);
            AddBox(go.transform, new Vector3(0, h / 2f, 0), Quaternion.identity, new Vector3(w, h, d), shop.WallColor, 0.85f);
            AddBox(go.transform, new Vector3(0, h + h * 0.225f, 0), Quaternion.identity, new Vector3(w + 0.4f, h * 0.45f, d + 0.3f), shop.RoofColor, 0.8f);
            AddBox(go.transform, new Vector3(0, h + h * 0.45f + 0.05f, 0), Quaternion.identity, new Vector3(w + 0.5f, 0.08f, d + 0.4f), SnowColor, 0.9f);
            AddBox(go.transform, new Vector3(0, h * 0.3f, d / 2f + 0.02f), Quaternion.identity, new Vector3(w * 0.25f, h * 0.55f, 0.06f), new Color(0.165f, 0.102f, 0.039f), 0.9f);
            foreach (int side in new[] { -1, 1 })
                AddBox(go.transform, new Vector3(side * w * 0.28f, h * 0.6f, d / 2f + 0.02f), Quaternion.identity, new Vector3(w * 0.18f, h * 0.22f, 0.04f), new Color(1f, 0.910f, 0.627f), 0.2f, new Color(1f, 0.910f, 0.627f), 0.6f);
            AddBox(go.transform, new Vector3(0, h + h * 0.45f + 0.3f, d / 2f + 0.15f), Quaternion.identity, new Vector3(w * 0.5f, 0.4f, 0.06f), shop.RoofColor, 0.7f);
            AddPointLight(go.transform, new Vector3(0, h + h * 0.45f + 0.5f, d / 2f + 0.5f), shop.LightColor, 3f, 10f);
            switch (shop.Type)
            {
                case ShopType.Garage: BuildGarageDecor(go.transform, w, h, d); break;
                case ShopType.Supply: BuildSupplyDecor(go.transform, w, h, d); break;
                case ShopType.Trade: BuildTradeDecor(go.transform, w, h, d); break;
                case ShopType.Tavern: BuildTavernDecor(go.transform, w, h, d); break;
                case ShopType.Signal: BuildSignalDecor(go.transform, w, h, d); break;
                case ShopType.Fuel: BuildFuelDecor(go.transform, w, h, d); break;
            }
            _townObjects.Add(go);
        }

        private void BuildGarageDecor(Transform p, float w, float h, float d)
        {
            for (int i = 0; i < 6; i++) AddBox(p, new Vector3(0, h * 0.15f + i * h * 0.08f, d / 2f + 0.03f), Quaternion.identity, new Vector3(w * 0.4f, 0.03f, 0.02f), new Color(0.353f, 0.353f, 0.353f), 0.6f, 0.4f);
            AddBox(p, new Vector3(w / 2f + 0.3f, h * 0.3f, 0), Quaternion.identity, new Vector3(0.4f, h * 0.5f, 0.8f), new Color(0.353f, 0.353f, 0.353f), 0.7f, 0.3f);
            for (int i = 0; i < 3; i++) AddCylinder(p, new Vector3(-w / 2f - 0.5f, 0.3f + i * 0.35f, d * 0.2f), Quaternion.Euler(90, 11.5f * i, 0), 0.3f, 0.3f, 0.12f, new Color(0.102f, 0.102f, 0.102f), 0.9f);
        }

        private void BuildSupplyDecor(Transform p, float w, float h, float d)
        {
            for (int i = 0; i < 3; i++) AddBox(p, new Vector3(d / 2f + 0.5f + i * 0.3f, 0.25f, 0.3f), Quaternion.identity, new Vector3(0.4f, 0.4f, 0.4f), WoodMid, 0.9f);
            AddCylinder(p, new Vector3(-d / 2f - 0.4f, 0.35f, -0.3f), Quaternion.identity, 0.2f, 0.2f, 0.6f, new Color(0.627f, 0.188f, 0.188f), 0.7f, 0.2f);
            AddCylinder(p, new Vector3(-d / 2f - 0.4f, 0.35f, 0.3f), Quaternion.identity, 0.2f, 0.2f, 0.6f, new Color(0.627f, 0.188f, 0.188f), 0.7f, 0.2f);
        }

        private void BuildTradeDecor(Transform p, float w, float h, float d)
        {
            AddCylinder(p, new Vector3(0, 0.15f, d / 2f + 0.6f), Quaternion.identity, 0.15f, 0.2f, 0.3f, new Color(0.541f, 0.478f, 0.353f), 0.8f);
            AddBox(p, new Vector3(0, 0.45f, d / 2f + 0.6f), Quaternion.identity, new Vector3(0.8f, 0.03f, 0.03f), new Color(0.541f, 0.478f, 0.353f), 0.7f, 0.3f);
            foreach (float x in new[] { -0.35f, 0.35f }) AddCylinder(p, new Vector3(x, 0.4f, d / 2f + 0.6f), Quaternion.Euler(90, 0, 0), 0.12f, 0.12f, 0.02f, new Color(0.753f, 0.627f, 0.376f), 0.5f, 0.5f);
        }

        private void BuildTavernDecor(Transform p, float w, float h, float d)
        {
            foreach (float x in new[] { -0.4f, 0.4f }) AddCylinder(p, new Vector3(x, 0.3f, d / 2f + 0.5f), Quaternion.identity, 0.2f, 0.22f, 0.55f, new Color(0.353f, 0.227f, 0.102f), 0.9f);
            AddCylinder(p, new Vector3(w * 0.25f, h + h * 0.45f + 0.5f, -d * 0.2f), Quaternion.identity, 0.12f, 0.15f, 0.8f, new Color(0.290f, 0.227f, 0.165f), 0.9f);
        }

        private void BuildSignalDecor(Transform p, float w, float h, float d)
        {
            AddCylinder(p, new Vector3(0, h + h * 0.45f + 1.5f, 0), Quaternion.identity, 0.03f, 0.05f, 3f, new Color(0.353f, 0.416f, 0.478f), 0.5f, 0.6f);
            AddBox(p, new Vector3(0, h + h * 0.45f + 2.5f, 0), Quaternion.identity, new Vector3(1.2f, 0.03f, 0.03f), new Color(0.353f, 0.416f, 0.478f), 0.5f, 0.6f);
            var antennaLight = AddSphere(p, new Vector3(0, h + h * 0.45f + 3.0f, 0), 0.08f, new Color(1f, 0.125f, 0.125f), 0.2f, new Color(1f, 0.125f, 0.125f), 3f);
            var blink = p.gameObject.AddComponent<AntennaBlink>();
            blink.LightRenderer = antennaLight.GetComponent<Renderer>();
            blink.PointLight = AddPointLight(p, new Vector3(0, h + h * 0.45f + 3.0f, 0), new Color(1f, 0.125f, 0.125f), 2f, 8f);
        }

        private void BuildFuelDecor(Transform p, float w, float h, float d)
        {
            AddBox(p, new Vector3(0, 3.2f, d / 2f + 1.5f), Quaternion.identity, new Vector3(4f, 0.08f, 3f), new Color(0.353f, 0.416f, 0.478f), 0.7f);
            foreach (float x in new[] { -1.5f, 1.5f }) AddCylinder(p, new Vector3(x, 1.6f, d / 2f + 1.5f), Quaternion.identity, 0.05f, 0.06f, 3.2f, new Color(0.541f, 0.541f, 0.541f), 0.6f, 0.3f);
            foreach (float x in new[] { -0.5f, 0.5f })
            {
                AddBox(p, new Vector3(x, 0.7f, d / 2f + 1.2f), Quaternion.identity, new Vector3(0.4f, 1.2f, 0.3f), new Color(0.753f, 0.753f, 0.753f), 0.5f, 0.3f);
                AddBox(p, new Vector3(x, 0.9f, d / 2f + 1.36f), Quaternion.identity, new Vector3(0.3f, 0.2f, 0.02f), new Color(0.102f, 0.102f, 0.165f), 0.3f, new Color(0.125f, 0.665f, 0.251f), 0.5f);
            }
        }

        private void BuildMarketSquare()
        {
            float baseY = TerrainHeight.GetTerrainHeight(TownCenter.x, TownCenter.z);
            var go = new GameObject("MarketSquare"); go.transform.SetParent(transform);
            go.transform.position = new Vector3(TownCenter.x, baseY, TownCenter.z);
            var groundQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            groundQuad.transform.SetParent(go.transform); groundQuad.transform.localPosition = new Vector3(0, 0.02f, 0);
            groundQuad.transform.localRotation = Quaternion.Euler(90, 0, 0); groundQuad.transform.localScale = new Vector3(12f, 12f, 1f);
            var groundMat = new Material(Shader.Find("Universal Render Pipeline/Lit")); groundMat.color = new Color(0.541f, 0.502f, 0.439f); groundMat.SetFloat("_Smoothness", 0.1f);
            groundQuad.GetComponent<Renderer>().material = groundMat;
            for (int i = 0; i < 3; i++)
            {
                float angle = (i / 3f) * Mathf.PI * 2f - Mathf.PI / 2f; float r = 3.5f;
                float sx = Mathf.Cos(angle) * r, sz = Mathf.Sin(angle) * r;
                AddBox(go.transform, new Vector3(sx, 0.6f, sz), Quaternion.Euler(0, (-angle + Mathf.PI / 2f) * Mathf.Rad2Deg, 0), new Vector3(1.5f, 0.06f, 0.8f), WoodMid, 0.9f);
                foreach (float lx in new[] { -0.6f, 0.6f }) foreach (float lz in new[] { -0.3f, 0.3f }) AddBox(go.transform, new Vector3(sx + lx, 0.3f, sz + lz), Quaternion.identity, new Vector3(0.06f, 0.6f, 0.06f), WoodDark, 0.9f);
                AddBox(go.transform, new Vector3(sx, 1.2f, sz), Quaternion.identity, new Vector3(1.8f, 0.04f, 1.0f), new Color(0.541f, 0.290f, 0.165f), 0.9f);
            }
            AddCylinder(go.transform, new Vector3(0, 0.3f, 0), Quaternion.identity, 0.4f, 0.5f, 0.6f, new Color(0.416f, 0.416f, 0.353f), 0.9f);
            AddCylinder(go.transform, new Vector3(0, 1.0f, 0), Quaternion.identity, 0.03f, 0.03f, 1.2f, WoodDark, 0.9f);
            foreach (float x in new[] { -0.5f, 0.5f }) AddCylinder(go.transform, new Vector3(x, 0.7f, 0), Quaternion.identity, 0.03f, 0.04f, 1.2f, WoodDark, 0.9f);
            _townObjects.Add(go);
        }

        private void BuildStreetLamps()
        {
            float[] angles = { 0f, Mathf.PI / 3f, Mathf.PI * 2f / 3f, Mathf.PI, Mathf.PI * 4f / 3f, Mathf.PI * 5f / 3f };
            for (int i = 0; i < angles.Length; i++)
            {
                float r = 8f; float x = TownCenter.x + Mathf.Cos(angles[i]) * r, z = TownCenter.z + Mathf.Sin(angles[i]) * r;
                float y = TerrainHeight.GetTerrainHeight(x, z);
                var lampObj = new GameObject("StreetLamp_" + i); lampObj.transform.SetParent(transform);
                lampObj.transform.position = new Vector3(x, y, z);
                AddCylinder(lampObj.transform, new Vector3(0, 1.5f, 0), Quaternion.identity, 0.04f, 0.06f, 3f, new Color(0.353f, 0.353f, 0.353f), 0.6f, 0.4f);
                AddSphere(lampObj.transform, new Vector3(0.35f, 3.0f, 0), 0.12f, new Color(1f, 0.910f, 0.627f), 0.2f, new Color(1f, 0.910f, 0.627f), 1.5f);
                if (i < 4) AddPointLight(lampObj.transform, new Vector3(0.35f, 3.0f, 0), new Color(1f, 0.910f, 0.627f), 2f, 12f);
                _townObjects.Add(lampObj);
            }
        }

        private void BuildFencePerimeter()
        {
            int count = 24; float radius = 14f;
            for (int i = 0; i < count; i++)
            {
                float angle = (i / (float)count) * Mathf.PI * 2f;
                float x = TownCenter.x + Mathf.Cos(angle) * radius, z = TownCenter.z + Mathf.Sin(angle) * radius;
                float y = TerrainHeight.GetTerrainHeight(x, z);
                var fenceObj = new GameObject("Fence_" + i); fenceObj.transform.SetParent(transform);
                fenceObj.transform.position = new Vector3(x, y, z);
                fenceObj.transform.rotation = Quaternion.Euler(0, (-angle + Mathf.PI / 2f) * Mathf.Rad2Deg, 0);
                foreach (float fx in new[] { -0.4f, 0f, 0.4f }) AddBox(fenceObj.transform, new Vector3(fx, 0.35f, 0), Quaternion.identity, new Vector3(0.06f, 0.7f, 0.06f), FenceColor, 0.9f);
                AddBox(fenceObj.transform, new Vector3(0, 0.25f, 0), Quaternion.identity, new Vector3(1.0f, 0.04f, 0.04f), FenceColor, 0.9f);
                AddBox(fenceObj.transform, new Vector3(0, 0.5f, 0), Quaternion.identity, new Vector3(1.0f, 0.04f, 0.04f), FenceColor, 0.9f);
                _townObjects.Add(fenceObj);
            }
        }

        private void BuildSnowman(Vector3 pos)
        {
            float y = TerrainHeight.GetTerrainHeight(pos.x, pos.z);
            var go = new GameObject("Snowman"); go.transform.SetParent(transform);
            go.transform.position = new Vector3(pos.x, y, pos.z);
            AddSphere(go.transform, new Vector3(0, 0.4f, 0), 0.4f, SnowColor, 0.9f);
            AddSphere(go.transform, new Vector3(0, 1.0f, 0), 0.3f, SnowColor, 0.9f);
            AddSphere(go.transform, new Vector3(0, 1.45f, 0), 0.2f, SnowColor, 0.9f);
            AddCone(go.transform, new Vector3(0, 1.45f, 0.2f), Quaternion.Euler(90, 0, 0), 0.04f, 0.15f, 5, new Color(0.878f, 0.439f, 0.125f), 0.8f);
            foreach (float ex in new[] { -0.06f, 0.06f }) AddSphere(go.transform, new Vector3(ex, 1.52f, 0.17f), 0.025f, new Color(0.102f, 0.102f, 0.102f), 0.5f);
            _townObjects.Add(go);
        }

        private void BuildAbandonedCar(Vector3 pos, float rotationY)
        {
            float y = TerrainHeight.GetTerrainHeight(pos.x, pos.z);
            var go = new GameObject("AbandonedCar"); go.transform.SetParent(transform);
            go.transform.position = new Vector3(pos.x, y, pos.z);
            go.transform.rotation = Quaternion.Euler(0, rotationY * Mathf.Rad2Deg, 0);
            AddBox(go.transform, new Vector3(0, 0.4f, 0), Quaternion.identity, new Vector3(1.6f, 0.5f, 3.5f), new Color(0.290f, 0.353f, 0.416f), 0.85f);
            AddBox(go.transform, new Vector3(0, 0.85f, 0.3f), Quaternion.identity, new Vector3(1.4f, 0.4f, 1.8f), new Color(0.251f, 0.314f, 0.376f), 0.85f);
            AddBox(go.transform, new Vector3(0, 1.1f, 0.3f), Quaternion.identity, new Vector3(1.42f, 0.06f, 1.82f), new Color(0.478f, 0.541f, 0.596f), 0.3f);
            foreach (float wx in new[] { -0.85f, 0.85f }) foreach (float wz in new[] { -1.0f, 1.0f })
                AddCylinder(go.transform, new Vector3(wx, 0.25f, wz), Quaternion.Euler(0, 0, 90), 0.3f, 0.3f, 0.2f, new Color(0.102f, 0.102f, 0.102f), 0.9f);
            AddBox(go.transform, new Vector3(0, 0.68f, 1.76f), Quaternion.identity, new Vector3(1.2f, 0.35f, 0.04f), new Color(0.533f, 0.722f, 0.843f), 0.2f);
            AddBox(go.transform, new Vector3(0, 0.68f, -1.76f), Quaternion.identity, new Vector3(1.2f, 0.35f, 0.04f), new Color(0.416f, 0.533f, 0.627f), 0.2f);
            AddBox(go.transform, new Vector3(0, 0.7f, 0), Quaternion.identity, new Vector3(1.62f, 0.04f, 3.52f), SnowColor, 0.9f);
            _townObjects.Add(go);
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

        private static GameObject AddCone(Transform parent, Vector3 pos, Quaternion rot, float radius, float height, int segments, Color color, float roughness = 1f)
        {
            var go = new GameObject("Cone"); go.transform.SetParent(parent); go.transform.localPosition = pos; go.transform.localRotation = rot;
            var mesh = SceneryElements.CreateConeMesh(radius, height, segments); go.AddComponent<MeshFilter>().mesh = mesh;
            var rend = go.AddComponent<MeshRenderer>(); var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = color; mat.SetFloat("_Smoothness", 1f - roughness);
            rend.material = mat; return go;
        }

        private static Light AddPointLight(Transform parent, Vector3 pos, Color color, float intensity, float range)
        {
            var obj = new GameObject("PointLight"); obj.transform.SetParent(parent); obj.transform.localPosition = pos;
            var light = obj.AddComponent<Light>(); light.type = LightType.Point; light.color = color; light.intensity = intensity; light.range = range; light.shadows = LightShadows.Soft; return light;
        }

        public static List<ShopDef> GenerateDefaultShops()
        {
            return new List<ShopDef>
            {
                new ShopDef { Id = "garage", Type = ShopType.Garage, Position = new Vector3(-6f, 0, -4f), Rotation = 0.3f, Size = new Vector3(4f, 3f, 5f), WallColor = new Color(0.353f, 0.353f, 0.384f), RoofColor = new Color(0.251f, 0.251f, 0.290f), LightColor = new Color(1f, 0.878f, 0.565f) },
                new ShopDef { Id = "supply", Type = ShopType.Supply, Position = new Vector3(6f, 0, -3f), Rotation = -0.2f, Size = new Vector3(3.5f, 2.8f, 4f), WallColor = new Color(0.416f, 0.384f, 0.314f), RoofColor = new Color(0.290f, 0.251f, 0.188f), LightColor = new Color(1f, 0.910f, 0.753f) },
                new ShopDef { Id = "trade", Type = ShopType.Trade, Position = new Vector3(-5f, 0, 5f), Rotation = 0.5f, Size = new Vector3(3f, 2.5f, 3.5f), WallColor = new Color(0.478f, 0.416f, 0.314f), RoofColor = new Color(0.353f, 0.290f, 0.200f), LightColor = new Color(1f, 0.843f, 0.565f) },
                new ShopDef { Id = "tavern", Type = ShopType.Tavern, Position = new Vector3(5f, 0, 6f), Rotation = -0.4f, Size = new Vector3(4.5f, 3.2f, 5.5f), WallColor = new Color(0.353f, 0.251f, 0.165f), RoofColor = new Color(0.227f, 0.165f, 0.102f), LightColor = new Color(1f, 0.753f, 0.376f) },
                new ShopDef { Id = "signal", Type = ShopType.Signal, Position = new Vector3(-8f, 0, 0), Rotation = 0.1f, Size = new Vector3(3f, 3.5f, 3f), WallColor = new Color(0.314f, 0.353f, 0.416f), RoofColor = new Color(0.227f, 0.251f, 0.314f), LightColor = new Color(0.369f, 0.780f, 1f) },
                new ShopDef { Id = "fuel", Type = ShopType.Fuel, Position = new Vector3(8f, 0, -1f), Rotation = -0.15f, Size = new Vector3(3.5f, 2.8f, 4f), WallColor = new Color(0.384f, 0.384f, 0.353f), RoofColor = new Color(0.290f, 0.290f, 0.251f), LightColor = new Color(1f, 0.910f, 0.627f) }
            };
        }
    }

    public class AntennaBlink : MonoBehaviour
    {
        public Renderer LightRenderer; public Light PointLight; private Material _mat;
        private void Start() { if (LightRenderer != null) _mat = LightRenderer.material; }
        private void Update()
        {
            if (_mat == null) return;
            float t = Time.time; int blink = Mathf.FloorToInt(t * 1.5f) % 2;
            _mat.SetColor("_EmissionColor", new Color(1f, 0.125f, 0.125f) * (blink * 3f));
            if (PointLight != null) PointLight.intensity = 2f * blink;
        }
    }
}

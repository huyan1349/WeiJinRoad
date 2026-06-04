using UnityEngine;
using WeiJinRoad.Core;

namespace WeiJinRoad.World
{
    /// <summary>
    /// 日夜循环光照控制器
    /// 翻译自 TypeScript 版 EnvironmentLighting.tsx
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public class EnvironmentLighting : MonoBehaviour
    {
        [System.Serializable]
        private struct LightStop
        {
            public float Time;
            public Color BgColor;
            public Color AmbColor;
            public Color SunColor;
            public float SunIntensity;
        }

        [Header("Lighting Timeline")]
        [SerializeField] private LightStop[] _stops = new LightStop[]
        {
            new LightStop { Time = 0,    BgColor = H("#030612"), AmbColor = H("#0a1025"), SunColor = H("#102040"), SunIntensity = 0.15f },
            new LightStop { Time = 3,    BgColor = H("#050a1a"), AmbColor = H("#0c1428"), SunColor = H("#102040"), SunIntensity = 0.15f },
            new LightStop { Time = 4.5f, BgColor = H("#0c1428"), AmbColor = H("#141c35"), SunColor = H("#182850"), SunIntensity = 0.2f },
            new LightStop { Time = 5.3f, BgColor = H("#182242"), AmbColor = H("#1c2845"), SunColor = H("#3a3050"), SunIntensity = 0.35f },
            new LightStop { Time = 5.8f, BgColor = H("#3a3055"), AmbColor = H("#282840"), SunColor = H("#704830"), SunIntensity = 0.6f },
            new LightStop { Time = 6.2f, BgColor = H("#8a4050"), AmbColor = H("#2a2230"), SunColor = H("#d07030"), SunIntensity = 0.85f },
            new LightStop { Time = 6.5f, BgColor = H("#b54e38"), AmbColor = H("#251b1f"), SunColor = H("#ff9900"), SunIntensity = 1.0f },
            new LightStop { Time = 7,    BgColor = H("#c08060"), AmbColor = H("#383850"), SunColor = H("#ffcc80"), SunIntensity = 1.2f },
            new LightStop { Time = 8,    BgColor = H("#8ea8c0"), AmbColor = H("#3d5570"), SunColor = H("#ffe8d0"), SunIntensity = 1.4f },
            new LightStop { Time = 10,   BgColor = H("#7ea3cc"), AmbColor = H("#405b75"), SunColor = H("#ffffee"), SunIntensity = 1.5f },
            new LightStop { Time = 14,   BgColor = H("#7ea3cc"), AmbColor = H("#405b75"), SunColor = H("#ffffee"), SunIntensity = 1.5f },
            new LightStop { Time = 17,   BgColor = H("#5a4050"), AmbColor = H("#2a2030"), SunColor = H("#ff9940"), SunIntensity = 1.2f },
            new LightStop { Time = 17.5f,BgColor = H("#2a1525"), AmbColor = H("#1a1215"), SunColor = H("#ff5500"), SunIntensity = 1.2f },
            new LightStop { Time = 18.3f,BgColor = H("#0a0c1a"), AmbColor = H("#0c1425"), SunColor = H("#152040"), SunIntensity = 0.2f },
            new LightStop { Time = 19,   BgColor = H("#040714"), AmbColor = H("#0a1025"), SunColor = H("#102040"), SunIntensity = 0.15f },
            new LightStop { Time = 24,   BgColor = H("#030612"), AmbColor = H("#0a1025"), SunColor = H("#102040"), SunIntensity = 0.15f },
        };

        [Header("Light References")]
        public Light AmbientLight;
        public Light SunLight;
        public Transform SunMesh;
        public ParticleSystem StarsParticleSystem;

        [Header("Sun Path")]
        public float SunRadius = 600f;
        public float SunYOffset = 50f;
        public float SunZForward = 200f;

        [Header("Fog")]
        public float SnowFogDensity = 0.012f;
        public float ClearFogDensity = 0.005f;
        public float FogTransitionSpeed = 0.02f;

        [Header("Winter Atmosphere")]
        [Range(0f, 1f)] public float WinterTintStrength = 0.15f;
        public Color WinterTint = new Color(0.7f, 0.8f, 1f);

        private Color _currentBgColor;
        private float _currentFogDensity;

        private void Start()
        {
            if (AmbientLight == null)
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Ambient) { AmbientLight = l; break; }
            }
            if (SunLight == null)
            {
                foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                    if (l.type == LightType.Directional) { SunLight = l; break; }
            }
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            float t = gm.TimeOfDay;
            float lightingT = Mathf.Max(5f, Mathf.Min(19f, t));

            InterpolateStops(lightingT, out Color bgColor, out Color ambColor, out Color sunColor, out float sunInt);

            if (WinterTintStrength > 0f)
            {
                bgColor = Color.Lerp(bgColor, WinterTint, WinterTintStrength * 0.3f);
                ambColor = Color.Lerp(ambColor, WinterTint, WinterTintStrength * 0.2f);
            }

            _currentBgColor = Color.Lerp(_currentBgColor, bgColor, Time.deltaTime * 2f);
            if (RenderSettings.skybox != null) RenderSettings.skybox.SetColor("_Tint", _currentBgColor);

            UpdateFog(gm, _currentBgColor);
            UpdateAmbientLight(t, gm.Brightness, ambColor, gm);
            UpdateSun(t, sunColor, sunInt, gm);
            UpdateStars(t, gm);
        }

        private void InterpolateStops(float t, out Color bgColor, out Color ambColor, out Color sunColor, out float sunInt)
        {
            LightStop start = _stops[0], end = _stops[_stops.Length - 1];
            for (int i = 0; i < _stops.Length - 1; i++)
            {
                if (t >= _stops[i].Time && t <= _stops[i + 1].Time)
                { start = _stops[i]; end = _stops[i + 1]; break; }
            }
            float progress = end.Time > start.Time ? (t - start.Time) / (end.Time - start.Time) : 0f;
            bgColor = Color.Lerp(start.BgColor, end.BgColor, progress);
            ambColor = Color.Lerp(start.AmbColor, end.AmbColor, progress);
            sunColor = Color.Lerp(start.SunColor, end.SunColor, progress);
            sunInt = Mathf.Lerp(start.SunIntensity, end.SunIntensity, progress);
        }

        private void UpdateFog(GameManager gm, Color fogColor)
        {
            float targetDensity = gm.IsSnowing ? SnowFogDensity : ClearFogDensity;
            if (!gm.DevFog || gm.DevGodView)
            {
                RenderSettings.fogMode = FogMode.Linear;
                RenderSettings.fogStartDistance = 1000f;
                RenderSettings.fogEndDistance = 2000f;
                return;
            }
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            _currentFogDensity = Mathf.Lerp(_currentFogDensity, targetDensity, FogTransitionSpeed);
            RenderSettings.fogDensity = _currentFogDensity;
            RenderSettings.fogColor = fogColor;
        }

        private void UpdateAmbientLight(float t, float brightness, Color ambColor, GameManager gm)
        {
            if (AmbientLight == null) return;
            float mult = 1.0f;
            if (t < 6f || t > 18f) mult = gm.DevLights ? brightness : 0.25f;
            else if (t >= 6f && t < 7f) mult = Mathf.Lerp(brightness, 1.0f, t - 6f);
            else if (t > 17f && t <= 18f) mult = Mathf.Lerp(1.0f, brightness, t - 17f);

            Color finalAmb = ambColor * mult;
            AmbientLight.color = finalAmb;
            AmbientLight.intensity = gm.DevGodView ? 1.55f : (gm.DevLights ? 1f : 0.45f);
            RenderSettings.ambientLight = finalAmb;
        }

        private void UpdateSun(float t, Color sunColor, float sunInt, GameManager gm)
        {
            if (SunLight == null) return;
            SunLight.color = sunColor;
            SunLight.intensity = gm.DevLights ? sunInt : 0f;

            float angle = ((t - 6f) / 12f) * Mathf.PI;
            var cam = Camera.main;
            float camZ = cam != null ? cam.transform.position.z : 0f;

            float sunX, sunY, sunZ;
            bool isDaytime = t > 4f && t < 20f;
            if (isDaytime)
            {
                sunX = -Mathf.Cos(angle) * SunRadius;
                sunY = Mathf.Sin(angle) * 300f + SunYOffset;
                sunZ = Mathf.Sin(angle) * 400f + SunZForward + camZ;
            }
            else
            {
                float moonAngle = ((t + 6f) / 12f) * Mathf.PI;
                sunX = -Mathf.Cos(moonAngle) * SunRadius;
                sunY = Mathf.Max(50f, Mathf.Sin(moonAngle) * 250f);
                sunZ = 500f + camZ;
            }

            SunLight.transform.position = new Vector3(sunX, sunY, sunZ);
            SunLight.transform.rotation = Quaternion.LookRotation(new Vector3(0, 0, camZ) - SunLight.transform.position);

            if (SunMesh != null)
            {
                SunMesh.position = new Vector3(sunX, sunY, sunZ);
                var rend = SunMesh.GetComponent<Renderer>();
                if (rend != null && rend.material != null) rend.material.color = isDaytime ? sunColor : Color.white;
            }
        }

        private void UpdateStars(float t, GameManager gm)
        {
            if (StarsParticleSystem == null) return;
            var cam = Camera.main;
            if (cam != null)
                StarsParticleSystem.transform.position = new Vector3(StarsParticleSystem.transform.position.x, StarsParticleSystem.transform.position.y, cam.transform.position.z);
            bool isNight = t < 6f || t > 18f;
            float targetAlpha = isNight && !gm.IsSnowing ? 1f : 0f;
            var main = StarsParticleSystem.main;
            var c = main.startColor.color;
            c.a = Mathf.Lerp(c.a, targetAlpha, 0.05f);
            main.startColor = new ParticleSystem.MinMaxGradient(c);
        }

        private static Color H(string hex) { ColorUtility.TryParseHtmlString(hex, out Color c); return c; }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-e3dcd8efdfa04f37a4fbd8192ab9a83c/cwd.txt'; exit "$__tr_native_ec"
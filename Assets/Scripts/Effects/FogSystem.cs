using UnityEngine;
using WeiJinRoad.Core;

namespace WeiJinRoad.Effects
{
    /// <summary>
    /// 雾粒子系统：基于 Unity ParticleSystem 的低空雾气效果
    /// 翻译自 TypeScript 版 FogParticles.tsx
    /// </summary>
    [ExecuteInEditMode]
    public class FogSystem : MonoBehaviour
    {
        [Header("Particle Settings")]
        [Range(50, 5000)] public int MaxParticles = 2000;
        public Vector3 FieldSize = new Vector3(160f, 50f, 160f);
        public float MinSpeed = 0.3f;
        public float MaxSpeed = 0.9f;
        public Vector2 ParticleSizeRange = new Vector2(3f, 8f);
        public float ParticleLifetime = 25f;

        [Header("Fog Behavior")]
        public float GroundOffset = 1f;
        public float MaxFogHeight = 8f;
        [Range(0f, 3f)] public float DriftStrength = 1f;
        [Range(0f, 2f)] public float VerticalDrift = 0.2f;

        [Header("Visual")]
        public Color FogColor = new Color(0.85f, 0.9f, 0.98f, 0.06f);
        public Material FogMaterial;

        [Header("Density")]
        public float FogEmissionRate = 150f;
        public float NoFogEmissionRate = 0f;
        public float DensityTransitionSpeed = 1.5f;

        [Header("Wind")]
        public Vector3 WindDirection = new Vector3(0.2f, 0f, 0.05f);
        [Range(0f, 3f)] public float WindStrength = 0.5f;

        private ParticleSystem _particleSystem;
        private ParticleSystem.EmissionModule _emission;
        private ParticleSystem.VelocityOverLifetimeModule _velocityModule;
        private ParticleSystem.NoiseModule _noiseModule;
        private float _currentEmissionRate;

        private void Awake() { InitializeParticleSystem(); }
        private void OnEnable() { if (_particleSystem != null) _particleSystem.Play(); }
        private void OnDisable() { if (_particleSystem != null) _particleSystem.Stop(); }

        private void Update()
        {
            FollowCamera();
            UpdateDensity();
            UpdateWind();
        }

        private void InitializeParticleSystem()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            if (_particleSystem == null) _particleSystem = gameObject.AddComponent<ParticleSystem>();

            var main = _particleSystem.main;
            main.maxParticles = MaxParticles;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startColor = FogColor;
            main.startSize = new ParticleSystem.MinMaxCurve(ParticleSizeRange.x, ParticleSizeRange.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(MinSpeed, MaxSpeed);
            main.startLifetime = ParticleLifetime;
            main.gravityModifier = -0.01f;
            main.playOnAwake = true;
            main.loop = true;
            main.prewarm = true;

            _emission = _particleSystem.emission;
            _emission.rateOverTime = FogEmissionRate;

            var shape = _particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(FieldSize.x, MaxFogHeight, FieldSize.z);
            shape.position = new Vector3(0, GroundOffset, 0);

            _velocityModule = _particleSystem.velocityOverLifetime;
            _velocityModule.enabled = true;
            _velocityModule.space = ParticleSystemSimulationSpace.World;
            _velocityModule.x = new ParticleSystem.MinMaxCurve(-DriftStrength * 0.3f, DriftStrength * 0.3f);
            _velocityModule.y = new ParticleSystem.MinMaxCurve(-VerticalDrift, VerticalDrift);
            _velocityModule.z = new ParticleSystem.MinMaxCurve(-DriftStrength * 0.2f, DriftStrength * 0.2f);

            _noiseModule = _particleSystem.noise;
            _noiseModule.enabled = true;
            _noiseModule.strength = 2f;
            _noiseModule.frequency = 0.15f;
            _noiseModule.scrollSpeed = 0.2f;
            _noiseModule.damping = true;
            _noiseModule.separateAxes = true;
            _noiseModule.strengthY = 0.5f;

            var colorModule = _particleSystem.colorOverLifetime;
            colorModule.enabled = true;
            var gradient = new Gradient();
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(FogColor, 0f),
                new GradientColorKey(FogColor, 0.5f),
                new GradientColorKey(FogColor, 1f),
            };
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(FogColor.a, 0.2f),
                new GradientAlphaKey(FogColor.a, 0.7f),
                new GradientAlphaKey(0f, 1f),
            };
            gradient.SetKeys(colorKeys, alphaKeys);
            colorModule.color = new ParticleSystem.MinMaxGradient(gradient);

            var renderer = GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.billboardMode = ParticleSystemBillboardMode.Camera;
                if (FogMaterial != null) renderer.material = FogMaterial;
                else
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                    mat.color = FogColor;
                    mat.SetInt("_BlendOp", (int)UnityEngine.Rendering.BlendOp.Add);
                    mat.renderQueue = 3000;
                    renderer.material = mat;
                }
            }
            _currentEmissionRate = FogEmissionRate;
        }

        private void FollowCamera()
        {
            var cam = Camera.main;
            if (cam != null) transform.position = new Vector3(cam.transform.position.x, 0f, cam.transform.position.z);
        }

        private void UpdateDensity()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            float targetRate = gm.DevFog ? FogEmissionRate : NoFogEmissionRate;
            if (gm.IsSnowing && gm.DevFog) targetRate *= 1.5f;
            _currentEmissionRate = Mathf.Lerp(_currentEmissionRate, targetRate, Time.deltaTime * DensityTransitionSpeed);
            _emission.rateOverTime = _currentEmissionRate;
        }

        private void UpdateWind()
        {
            if (!_velocityModule.enabled) return;
            float time = Time.time;
            float windX = WindDirection.x * WindStrength + Mathf.Sin(time * 0.3f) * 0.2f;
            float windZ = WindDirection.z * WindStrength + Mathf.Cos(time * 0.15f) * 0.15f;
            _velocityModule.x = new ParticleSystem.MinMaxCurve(windX - DriftStrength * 0.3f, windX + DriftStrength * 0.3f);
            _velocityModule.z = new ParticleSystem.MinMaxCurve(windZ - DriftStrength * 0.2f, windZ + DriftStrength * 0.2f);
        }

        public void SetDensity(float density)
        {
            density = Mathf.Clamp01(density);
            _currentEmissionRate = Mathf.Lerp(NoFogEmissionRate, FogEmissionRate, density);
            _emission.rateOverTime = _currentEmissionRate;
        }

        public void SetWind(Vector3 direction, float strength)
        {
            WindDirection = direction.normalized;
            WindStrength = strength;
        }
    }
}

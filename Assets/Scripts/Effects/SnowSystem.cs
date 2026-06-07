using UnityEngine;
using WeiJinRoad.Core;

namespace WeiJinRoad.Effects
{
    /// <summary>
    /// 雪粒子系统：基于 Unity ParticleSystem 的降雪效果
    /// 翻译自 TypeScript 版 Snow.tsx
    /// </summary>
    [ExecuteInEditMode]
    public class SnowSystem : MonoBehaviour
    {
        [Header("Particle Settings")]
        [Range(100, 30000)] public int MaxParticles = 7600;
        public Vector3 FieldSize = new Vector3(190f, 75f, 190f);
        public float MinSpeed = 2f;
        public float MaxSpeed = 5f;
        public Vector2 ParticleSizeRange = new Vector2(0.05f, 0.15f);
        public float ParticleLifetime = 15f;

        [Header("Wind")]
        public Vector3 WindDirection = new Vector3(0.3f, 0f, 0.1f);
        [Range(0f, 5f)] public float WindStrength = 1f;
        public float WindNoiseFrequency = 0.5f;

        [Header("Visual")]
        public Color SnowColor = new Color(0.98f, 0.995f, 1f, 0.86f);
        public Material SnowMaterial;

        [Header("Density")]
        public float SnowingEmissionRate = 500f;
        public float NoSnowEmissionRate = 0f;
        public float DensityTransitionSpeed = 2f;

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
            main.startColor = SnowColor;
            main.startSize = new ParticleSystem.MinMaxCurve(ParticleSizeRange.x, ParticleSizeRange.y);
            main.startSpeed = new ParticleSystem.MinMaxCurve(MinSpeed, MaxSpeed);
            main.startLifetime = ParticleLifetime;
            main.gravityModifier = 0.1f;
            main.playOnAwake = true;
            main.loop = true;
            main.prewarm = true;

            _emission = _particleSystem.emission;
            _emission.rateOverTime = SnowingEmissionRate;

            var shape = _particleSystem.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = FieldSize;
            shape.position = new Vector3(0, FieldSize.y * 0.35f, 0);

            _velocityModule = _particleSystem.velocityOverLifetime;
            _velocityModule.enabled = true;
            _velocityModule.space = ParticleSystemSimulationSpace.World;

            _noiseModule = _particleSystem.noise;
            _noiseModule.enabled = true;
            _noiseModule.strength = 1.5f;
            _noiseModule.frequency = 0.3f;
            _noiseModule.scrollSpeed = 0.5f;
            _noiseModule.damping = true;

            var sizeModule = _particleSystem.sizeOverLifetime;
            sizeModule.enabled = true;
            sizeModule.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 0.5f, 1f, 0f));

            var renderer = GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.billboardMode = ParticleSystemBillboardMode.Stretched;
                renderer.lengthScale = 0.5f;
                if (SnowMaterial != null) renderer.material = SnowMaterial;
                else
                {
                    var mat = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
                    mat.color = SnowColor;
                    mat.renderQueue = 3000;
                    renderer.material = mat;
                }
            }
            _currentEmissionRate = SnowingEmissionRate;
        }

        private void FollowCamera()
        {
            var cam = Camera.main;
            if (cam != null) transform.position = cam.transform.position;
        }

        private void UpdateDensity()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            float targetRate = gm.IsSnowing ? SnowingEmissionRate : NoSnowEmissionRate;
            if (!gm.DevSnow) targetRate = 0f;
            _currentEmissionRate = Mathf.Lerp(_currentEmissionRate, targetRate, Time.deltaTime * DensityTransitionSpeed);
            _emission.rateOverTime = _currentEmissionRate;
        }

        private void UpdateWind()
        {
            if (!_velocityModule.enabled) return;
            float time = Time.time;
            float windX = WindDirection.x * WindStrength + Mathf.Sin(time * WindNoiseFrequency) * 0.5f;
            float windZ = WindDirection.z * WindStrength + Mathf.Cos(time * WindNoiseFrequency * 0.7f) * 0.3f;
            _velocityModule.x = new ParticleSystem.MinMaxCurve(windX * 0.5f, windX * 1.5f);
            _velocityModule.y = new ParticleSystem.MinMaxCurve(-1f, -0.2f);
            _velocityModule.z = new ParticleSystem.MinMaxCurve(windZ * 0.5f, windZ * 1.5f);
        }

        public void SetDensity(float density)
        {
            density = Mathf.Clamp01(density);
            _currentEmissionRate = Mathf.Lerp(NoSnowEmissionRate, SnowingEmissionRate, density);
            _emission.rateOverTime = _currentEmissionRate;
        }

        public void SetWind(Vector3 direction, float strength)
        {
            WindDirection = direction.normalized;
            WindStrength = strength;
        }
    }
}

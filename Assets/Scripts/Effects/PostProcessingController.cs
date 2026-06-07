using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using WeiJinRoad.Core;

namespace WeiJinRoad.Effects
{
    /// <summary>
    /// URP 后处理控制器：管理 Bloom、Vignette、ColorGrading、DepthOfField
    /// 翻译自 TypeScript 版 PostProcessing.tsx
    /// </summary>
    [RequireComponent(typeof(Volume))]
    [DefaultExecutionOrder(100)]
    public class PostProcessingController : MonoBehaviour
    {
        [Header("Volume Reference")]
        public Volume PostProcessVolume;

        [Header("Bloom Settings")]
        [Range(0f, 2f)] public float BloomThreshold = 0.5f;
        [Range(0f, 1f)] public float BloomScatter = 0.9f;
        [Range(0f, 5f)] public float BloomIntensityNormal = 2f;
        [Range(0f, 5f)] public float BloomIntensityLightweight = 1.2f;

        [Header("Vignette Settings")]
        [Range(0f, 1f)] public float VignetteIntensity = 0.25f;
        [Range(0.01f, 1f)] public float VignetteSmoothness = 0.4f;
        [Range(0f, 1f)] public float VignetteRoundness = 0.8f;

        [Header("Color Grading Settings")]
        public TonemappingMode Tonemapping = TonemappingMode.ACES;
        [Range(-1f, 1f)] public float SaturationOffset = 0.1f;
        [Range(-100f, 100f)] public float Contrast = 5f;
        [Range(-100f, 100f)] public float Temperature = -10f;
        [Range(-180f, 180f)] public float Tint = 0f;

        [Header("Depth of Field Settings")]
        public bool EnableDepthOfField;
        [Range(0.1f, 100f)] public float FocusDistance = 15f;
        [Range(0.1f, 50f)] public float NearBlurRange = 5f;
        [Range(0.1f, 100f)] public float FarBlurRange = 30f;

        [Header("Film Grain Settings")]
        [Range(0f, 1f)] public float FilmGrainIntensity = 0.02f;
        [Range(0f, 1f)] public float FilmGrainResponse = 0.8f;

        [Header("Mode")]
        public bool LightweightMode;

        private Bloom _bloom;
        private Vignette _vignette;
        private ColorGrading _colorGrading;
        private DepthOfField _depthOfField;
        private FilmGrain _filmGrain;

        private void Awake() { InitializeVolume(); }
        private void OnEnable() { if (PostProcessVolume != null) PostProcessVolume.enabled = true; }
        private void OnDisable() { if (PostProcessVolume != null) PostProcessVolume.enabled = false; }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && !gm.DevPostProcessing)
            { if (PostProcessVolume != null) PostProcessVolume.enabled = false; return; }
            if (PostProcessVolume != null) PostProcessVolume.enabled = true;
            UpdateBloom(gm);
            UpdateVignette(gm);
            UpdateColorGrading(gm);
            UpdateDepthOfField();
            UpdateFilmGrain(gm);
        }

        private void InitializeVolume()
        {
            PostProcessVolume = GetComponent<Volume>();
            if (PostProcessVolume == null) return;
            if (PostProcessVolume.profile == null) PostProcessVolume.profile = ScriptableObject.CreateInstance<VolumeProfile>();
            var profile = PostProcessVolume.profile;

            if (!profile.TryGet(out _bloom)) _bloom = profile.Add<Bloom>(true);
            _bloom.active = true;
            _bloom.threshold.Override(BloomThreshold);
            _bloom.scatter.Override(BloomScatter);
            _bloom.intensity.Override(BloomIntensityNormal);
            _bloom.highQualityFiltering.Override(true);

            if (!profile.TryGet(out _vignette)) _vignette = profile.Add<Vignette>(true);
            _vignette.active = true;
            _vignette.intensity.Override(VignetteIntensity);
            _vignette.smoothness.Override(VignetteSmoothness);
            _vignette.roundness.Override(VignetteRoundness);
            _vignette.color.Override(new Color(0f, 0f, 0.02f, 1f));

            if (!profile.TryGet(out _colorGrading)) _colorGrading = profile.Add<ColorGrading>(true);
            _colorGrading.active = true;
            _colorGrading.tonemapping.Override(Tonemapping);
            _colorGrading.saturation.Override(SaturationOffset);
            _colorGrading.contrast.Override(Contrast);
            _colorGrading.temperature.Override(Temperature);
            _colorGrading.tint.Override(Tint);

            if (!profile.TryGet(out _depthOfField)) _depthOfField = profile.Add<DepthOfField>(false);
            _depthOfField.active = EnableDepthOfField;
            _depthOfField.focusDistance.Override(FocusDistance);
            _depthOfField.nearFocusStart.Override(0f);
            _depthOfField.nearFocusEnd.Override(NearBlurRange);
            _depthOfField.farFocusStart.Override(FocusDistance);
            _depthOfField.farFocusEnd.Override(FocusDistance + FarBlurRange);

            if (!profile.TryGet(out _filmGrain)) _filmGrain = profile.Add<FilmGrain>(true);
            _filmGrain.active = true;
            _filmGrain.intensity.Override(FilmGrainIntensity);
            _filmGrain.response.Override(FilmGrainResponse);
        }

        private void UpdateBloom(GameManager gm)
        {
            if (_bloom == null) return;
            _bloom.active = gm == null || gm.DevBloom;
            if (_bloom.active)
            {
                float target = LightweightMode ? BloomIntensityLightweight : BloomIntensityNormal;
                _bloom.intensity.value = Mathf.Lerp(_bloom.intensity.value, target, Time.deltaTime * 3f);
            }
        }

        private void UpdateVignette(GameManager gm)
        {
            if (_vignette == null) return;
            float t = gm != null ? gm.TimeOfDay : 12f;
            float nightFactor = 0f;
            if (t < 6f || t > 18f) nightFactor = 1f;
            else if (t < 7f) nightFactor = 1f - (t - 6f);
            else if (t > 17f) nightFactor = t - 17f;
            _vignette.intensity.value = Mathf.Lerp(_vignette.intensity.value, VignetteIntensity + nightFactor * 0.15f, Time.deltaTime * 2f);
        }

        private void UpdateColorGrading(GameManager gm)
        {
            if (_colorGrading == null) return;
            float t = gm != null ? gm.TimeOfDay : 12f;
            bool isNight = t < 6f || t > 18f;
            float targetTemp = isNight ? Temperature - 15f : Temperature;
            _colorGrading.temperature.value = Mathf.Lerp(_colorGrading.temperature.value, targetTemp, Time.deltaTime * 2f);
            if (gm != null && gm.IsSnowing)
                _colorGrading.postExposure.value = Mathf.Lerp(_colorGrading.postExposure.value, 0.1f, Time.deltaTime);
            else
                _colorGrading.postExposure.value = Mathf.Lerp(_colorGrading.postExposure.value, 0f, Time.deltaTime);
        }

        private void UpdateDepthOfField()
        {
            if (_depthOfField == null) return;
            _depthOfField.active = EnableDepthOfField && !LightweightMode;
            if (_depthOfField.active)
                _depthOfField.focusDistance.value = Mathf.Lerp(_depthOfField.focusDistance.value, FocusDistance, Time.deltaTime * 2f);
        }

        private void UpdateFilmGrain(GameManager gm)
        {
            if (_filmGrain == null) return;
            float target = gm != null ? gm.NoiseIntensity : FilmGrainIntensity;
            _filmGrain.active = !LightweightMode && target > 0.001f;
            _filmGrain.intensity.value = Mathf.Lerp(_filmGrain.intensity.value, target, Time.deltaTime * 3f);
        }

        public void SetLightweightMode(bool lightweight) { LightweightMode = lightweight; }
        public void SetBloomIntensity(float intensity) { if (_bloom != null) _bloom.intensity.Override(intensity); }
        public void SetDepthOfField(bool enabled, float focusDist, float farRange)
        {
            EnableDepthOfField = enabled; FocusDistance = focusDist; FarBlurRange = farRange;
            if (_depthOfField != null) { _depthOfField.active = enabled; _depthOfField.focusDistance.Override(focusDist); _depthOfField.farFocusEnd.Override(focusDist + farRange); }
        }
    }
}

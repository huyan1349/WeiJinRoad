using UnityEngine;

namespace WeiJinRoad.Effects
{
    /// <summary>
    /// 后处理控制器（占位实现，需在 Editor 中配置 URP Volume）
    /// </summary>
    public class PostProcessingController : MonoBehaviour
    {
        [Header("后处理配置")]
        public float bloomIntensity = 0.5f;
        public float vignetteIntensity = 0.3f;
        public float fogDensity = 0.02f;

        private void Start()
        {
            // TODO: 在 Unity Editor 中创建 Volume Profile 并配置后处理效果
        }

        public void SetFogDensity(float density) { fogDensity = density; }
        public void SetBloomIntensity(float intensity) { bloomIntensity = intensity; }
        public void SetVignetteIntensity(float intensity) { vignetteIntensity = intensity; }
    }
}

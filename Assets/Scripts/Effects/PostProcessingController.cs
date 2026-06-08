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
ENDOFFILE; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-56462c82309f4c018b4c382d959042eb/cwd.txt'; exit "$__tr_native_ec"
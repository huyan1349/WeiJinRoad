using UnityEngine;

namespace WeiJinRoad.Core
{
    // =====================================================================
    // 前灯模式枚举
    // =====================================================================

    /// <summary>
    /// 前灯模式
    /// </summary>
    public enum HeadlightsMode
    {
        Auto,
        On,
        Off
    }

    // =====================================================================
    // 游戏管理器
    // =====================================================================

    /// <summary>
    /// 游戏全局状态管理器（单例）
    /// 提供 VehicleController / VehicleDamageSystem 所需的全局状态接口
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        private static GameManager _instance;

        /// <summary>单例实例</summary>
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameManager>();
                }
                return _instance;
            }
        }

        // ── 时间 ──

        [Header("Time")]
        [Range(0f, 24f)]
        [Tooltip("当前时间（小时，0~24）")]
        public float TimeOfDay = 12f;

        // ── 前灯 ──

        [Header("Headlights")]
        [Tooltip("前灯模式")]
        public HeadlightsMode HeadlightsMode = HeadlightsMode.Auto;

        // ── 相机 ──

        [Header("Camera")]
        [Tooltip("相机高度")]
        public float CameraHeight = 18f;

        [Tooltip("相机距离")]
        public float CameraDistance = 14f;

        [Tooltip("相机偏航偏移")]
        public float CameraYawOffset = 0f;

        [Tooltip("相机俯仰偏移")]
        public float CameraPitchOffset = 0f;

        [Tooltip("相机是否跟随")]
        public bool CameraFollow = true;

        // ── 资源 ──

        [Header("Resources")]
        [Tooltip("当前燃油")]
        public float Fuel = 100f;

        [Tooltip("最大携带量")]
        public float MaxCarry = 100f;

        // ── 开发 ──

        [Header("Dev")]
        [Tooltip("开发者上帝视角")]
        public bool DevGodView = false;

        // =================================================================
        // 公开属性
        // =================================================================

        /// <summary>当前燃油量</summary>
        public float FuelValue => Fuel;

        // =================================================================
        // 方法
        // =================================================================

        /// <summary>
        /// 消耗燃油
        /// </summary>
        public void SpendFuel(float amount)
        {
            Fuel = Mathf.Max(0f, Fuel - amount);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-aa07cc1d4a394b82aa24fa7425ec4632/cwd.txt'; exit "$__tr_native_ec"
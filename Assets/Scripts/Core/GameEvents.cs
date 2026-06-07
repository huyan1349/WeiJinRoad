using System;

namespace WeiJinRoad.Core
{
    /// <summary>
    /// 游戏事件系统 — 替代 Zustand 的订阅机制
    ///
    /// 提供静态 C# 事件，当 GameManager 中的状态发生变更时触发。
    /// 所有事件使用 System.Action 委托，支持泛型参数传递变更数据。
    ///
    /// 使用方式：
    /// <code>
    /// // 订阅事件
    /// GameEvents.OnResourcesChanged += HandleResourcesChanged;
    ///
    /// // 取消订阅
    /// GameEvents.OnResourcesChanged -= HandleResourcesChanged;
    ///
    /// // 处理事件
    /// private void HandleResourcesChanged(ResourceBag newResources)
    /// {
    ///     // 更新UI等
    /// }
    /// </code>
    /// </summary>
    public static class GameEvents
    {
        // =================================================================
        // 资源事件
        // =================================================================

        /// <summary>
        /// 资源变更事件
        /// </summary>
        public static event Action<ResourceBag> OnResourcesChanged;

        /// <summary>
        /// 燃料变更事件（资源变更的快捷子事件）
        /// </summary>
        public static event Action<int> OnFuelChanged;

        // =================================================================
        // 载具事件
        // =================================================================

        /// <summary>
        /// 载具部件状态变更事件（耐久度、等级、前挂件等）
        /// </summary>
        public static event Action OnVehiclePartsChanged;

        /// <summary>
        /// 载具耐久度变更事件（传入部件ID和新耐久度）
        /// </summary>
        public static event Action<PartId, float> OnPartConditionChanged;

        /// <summary>
        /// 载具生命值/整体状态变更事件
        /// </summary>
        public static event Action<float> OnHealthChanged;

        // =================================================================
        // 游戏阶段事件
        // =================================================================

        /// <summary>
        /// 游戏阶段变更事件（旅程进度变化时触发）
        /// </summary>
        public static event Action<int> OnGamePhaseChanged;

        // =================================================================
        // 障碍物事件
        // =================================================================

        /// <summary>
        /// 障碍物清除事件（传入障碍物ID）
        /// </summary>
        public static event Action<string> OnObstacleCleared;

        // =================================================================
        // 碎片事件
        // =================================================================

        /// <summary>
        /// 碎片发现事件（传入碎片ID）
        /// </summary>
        public static event Action<string> OnFragmentDiscovered;

        // =================================================================
        // 营地事件
        // =================================================================

        /// <summary>
        /// 营地建造事件（传入已建营地总数）
        /// </summary>
        public static event Action<int> OnCampBuilt;

        /// <summary>
        /// 扎营状态变更事件（true=开始扎营，false=结束扎营）
        /// </summary>
        public static event Action<bool> OnCampingChanged;

        // =================================================================
        // 站点/设施事件
        // =================================================================

        /// <summary>
        /// 站点设施建造事件（传入站点ID）
        /// </summary>
        public static event Action<string> OnStationBuilt;

        // =================================================================
        // 成就事件
        // =================================================================

        /// <summary>
        /// 成就解锁事件（传入已解锁的成就数据）
        /// </summary>
        public static event Action<Achievement> OnAchievementUnlocked;

        // =================================================================
        // 存档事件
        // =================================================================

        /// <summary>
        /// 游戏保存完成事件
        /// </summary>
        public static event Action OnGameSaved;

        /// <summary>
        /// 游戏加载完成事件
        /// </summary>
        public static event Action OnGameLoaded;

        // =================================================================
        // 环境事件
        // =================================================================

        /// <summary>
        /// 时间变更事件（传入当前时间，0~24小时）
        /// </summary>
        public static event Action<float> OnTimeOfDayChanged;

        /// <summary>
        /// 天气变更事件（true=下雪，false=停雪）
        /// </summary>
        public static event Action<bool> OnSnowingChanged;

        // =================================================================
        // 交互事件
        // =================================================================

        /// <summary>
        /// 附近可交互对象变更事件
        /// </summary>
        public static event Action<NearbyInteractable> OnNearbyInteractableChanged;

        /// <summary>
        /// 叙事内容展示事件
        /// </summary>
        public static event Action<NarrativeContent> OnNarrativeContentChanged;

        /// <summary>
        /// 日志覆盖层可见性变更事件
        /// </summary>
        public static event Action<bool> OnJournalOverlayVisibilityChanged;

        // =================================================================
        // 旅程通知事件
        // =================================================================

        /// <summary>
        /// 旅程通知事件（传入通知数据）
        /// </summary>
        public static event Action<JourneyNotification> OnJourneyNotification;

        // =================================================================
        // 小镇事件
        // =================================================================

        /// <summary>
        /// 小镇访问事件（首次访问时触发）
        /// </summary>
        public static event Action OnTownVisited;

        /// <summary>
        /// 物品购买事件（传入物品ID）
        /// </summary>
        public static event Action<string> OnItemPurchased;

        // =================================================================
        // 开发者事件
        // =================================================================

        /// <summary>
        /// 开发者传送请求事件（传入目标X和Z坐标）
        /// </summary>
        public static event Action<float, float> OnDevTeleportRequested;

        /// <summary>
        /// 开发者面板可见性变更事件
        /// </summary>
        public static event Action<bool> OnDevPanelVisibilityChanged;

        // =================================================================
        // 事件触发辅助方法
        // =================================================================

        /// <summary>
        /// 安全触发事件（防止订阅者为空时抛出异常）
        /// </summary>
        /// <typeparam name="T">事件参数类型</typeparam>
        /// <param name="eventDelegate">事件委托</param>
        /// <param name="arg">事件参数</param>
        internal static void Invoke<T>(Action<T> eventDelegate, T arg)
        {
            eventDelegate?.Invoke(arg);
        }

        /// <summary>
        /// 安全触发无参数事件
        /// </summary>
        /// <param name="eventDelegate">事件委托</param>
        internal static void Invoke(Action eventDelegate)
        {
            eventDelegate?.Invoke();
        }

        /// <summary>
        /// 清除所有事件订阅（用于场景切换或测试清理）
        ///
        /// 警告：仅在确定需要重置所有监听器时调用，
        /// 正常游戏流程中不应使用。
        /// </summary>
        public static void ClearAllSubscriptions()
        {
            OnResourcesChanged = null;
            OnFuelChanged = null;
            OnVehiclePartsChanged = null;
            OnPartConditionChanged = null;
            OnHealthChanged = null;
            OnGamePhaseChanged = null;
            OnObstacleCleared = null;
            OnFragmentDiscovered = null;
            OnCampBuilt = null;
            OnCampingChanged = null;
            OnStationBuilt = null;
            OnAchievementUnlocked = null;
            OnGameSaved = null;
            OnGameLoaded = null;
            OnTimeOfDayChanged = null;
            OnSnowingChanged = null;
            OnNearbyInteractableChanged = null;
            OnNarrativeContentChanged = null;
            OnJournalOverlayVisibilityChanged = null;
            OnJourneyNotification = null;
            OnTownVisited = null;
            OnItemPurchased = null;
            OnDevTeleportRequested = null;
            OnDevPanelVisibilityChanged = null;
        }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;

namespace WeiJinRoad.Core
{
    // =================================================================
    // 成就定义
    // =================================================================

    /// <summary>
    /// 成就定义模板（静态数据）
    /// </summary>
    [Serializable]
    public class AchievementDef
    {
        /// <summary>成就ID</summary>
        public string Id;
        /// <summary>名称</summary>
        public string Name;
        /// <summary>描述</summary>
        public string Desc;
        /// <summary>图标（emoji）</summary>
        public string Icon;
        /// <summary>分类</summary>
        public AchievementCategory Category;
        /// <summary>是否为隐藏成就</summary>
        public bool Hidden;
    }

    /// <summary>
    /// 成就分类元数据
    /// </summary>
    [Serializable]
    public class AchievementCategoryMeta
    {
        /// <summary>分类标签</summary>
        public string Label;
        /// <summary>分类图标</summary>
        public string Icon;
    }

    // =================================================================
    // AchievementSystem — 成就触发检查系统
    // =================================================================

    /// <summary>
    /// 成就触发检查系统
    ///
    /// 在关键事件发生时调用对应检查函数，自动解锁满足条件的成就。
    /// 解锁后通过 GameEvents.OnAchievementUnlocked 触发 UI 通知。
    ///
    /// 成就分类：
    /// - 旅途(Journey): 旅程进度相关
    /// - 营地(Camp): 扎营、篝火、电台、星图、烹饪
    /// - 探索(Explore): 障碍物清除、资源拾取、建造
    /// - 载具(Vehicle): 修理、升级、满级
    /// - 收集(Collect): 碎片发现、载重满
    /// </summary>
    public class AchievementSystem : MonoBehaviour
    {
        // =============================================================
        // 成就定义数据（对应 TS ACHIEVEMENT_DEFS）
        // =============================================================

        /// <summary>
        /// 成就分类元数据
        /// </summary>
        public static readonly Dictionary<AchievementCategory, AchievementCategoryMeta> CategoryMeta =
            new Dictionary<AchievementCategory, AchievementCategoryMeta>
        {
            { AchievementCategory.Journey, new AchievementCategoryMeta { Label = "旅途", Icon = "🧭" } },
            { AchievementCategory.Camp, new AchievementCategoryMeta { Label = "营地", Icon = "🏕" } },
            { AchievementCategory.Explore, new AchievementCategoryMeta { Label = "探索", Icon = "🔍" } },
            { AchievementCategory.Vehicle, new AchievementCategoryMeta { Label = "载具", Icon = "🚗" } },
            { AchievementCategory.Collect, new AchievementCategoryMeta { Label = "收集", Icon = "📦" } },
        };

        /// <summary>
        /// 所有成就定义（29个）
        /// </summary>
        public static readonly AchievementDef[] AchievementDefs = new AchievementDef[]
        {
            // ─── 旅途 ───
            new AchievementDef { Id = "first_drive", Name = "启程", Desc = "第一次启动车辆", Icon = "🚗", Category = AchievementCategory.Journey, Hidden = false },
            new AchievementDef { Id = "reach_forest", Name = "林间穿行", Desc = "到达森林路段", Icon = "🌲", Category = AchievementCategory.Journey, Hidden = false },
            new AchievementDef { Id = "reach_valley", Name = "裂谷深处", Desc = "到达裂谷路段", Icon = "🏔", Category = AchievementCategory.Journey, Hidden = false },
            new AchievementDef { Id = "reach_mountain", Name = "山巅之上", Desc = "到达山路段", Icon = "⛰", Category = AchievementCategory.Journey, Hidden = false },
            new AchievementDef { Id = "reach_summit", Name = "未尽之路的终点", Desc = "到达山顶", Icon = "🌅", Category = AchievementCategory.Journey, Hidden = true },
            new AchievementDef { Id = "journey_5", Name = "全线通行", Desc = "解锁第5旅程", Icon = "🗺", Category = AchievementCategory.Journey, Hidden = false },

            // ─── 营地 ───
            new AchievementDef { Id = "first_camp", Name = "首次扎营", Desc = "第一次扎营休息", Icon = "🏕", Category = AchievementCategory.Camp, Hidden = false },
            new AchievementDef { Id = "camp_5", Name = "老练旅人", Desc = "累计扎营5次", Icon = "⛺", Category = AchievementCategory.Camp, Hidden = false },
            new AchievementDef { Id = "camp_10", Name = "以路为家", Desc = "累计扎营10次", Icon = "🏠", Category = AchievementCategory.Camp, Hidden = false },
            new AchievementDef { Id = "campfire_master", Name = "篝火大师", Desc = "篝火维护达到90%最佳区间", Icon = "🔥", Category = AchievementCategory.Camp, Hidden = true },
            new AchievementDef { Id = "radio_lock", Name = "信号猎人", Desc = "锁定第一个电台信号", Icon = "📻", Category = AchievementCategory.Camp, Hidden = false },
            new AchievementDef { Id = "stargazer", Name = "观星者", Desc = "发现第一个星座", Icon = "⭐", Category = AchievementCategory.Camp, Hidden = false },
            new AchievementDef { Id = "all_constellations", Name = "天文学家", Desc = "发现全部4个星座", Icon = "🌌", Category = AchievementCategory.Camp, Hidden = true },
            new AchievementDef { Id = "first_cook", Name = "野外厨师", Desc = "第一次烹饪", Icon = "🍲", Category = AchievementCategory.Camp, Hidden = false },

            // ─── 探索 ───
            new AchievementDef { Id = "first_obstacle", Name = "开路先锋", Desc = "清除第一个路障", Icon = "🚧", Category = AchievementCategory.Explore, Hidden = false },
            new AchievementDef { Id = "obstacle_20", Name = "道路清道夫", Desc = "清除20个路障", Icon = "🛤", Category = AchievementCategory.Explore, Hidden = false },
            new AchievementDef { Id = "obstacle_50", Name = "无畏开拓者", Desc = "清除50个路障", Icon = "⚡", Category = AchievementCategory.Explore, Hidden = true },
            new AchievementDef { Id = "first_resource", Name = "拾荒者", Desc = "拾取第一个资源", Icon = "📦", Category = AchievementCategory.Explore, Hidden = false },
            new AchievementDef { Id = "first_build", Name = "建设者", Desc = "建造第一个设施", Icon = "🏗", Category = AchievementCategory.Explore, Hidden = false },
            new AchievementDef { Id = "build_all_types", Name = "全能建筑师", Desc = "建造全部6种设施", Icon = "🏛", Category = AchievementCategory.Explore, Hidden = true },
            new AchievementDef { Id = "visit_town", Name = "旅人驿站", Desc = "第一次到达小镇", Icon = "🏘", Category = AchievementCategory.Explore, Hidden = false },

            // ─── 载具 ───
            new AchievementDef { Id = "first_repair", Name = "修理工", Desc = "第一次修理部件", Icon = "🔧", Category = AchievementCategory.Vehicle, Hidden = false },
            new AchievementDef { Id = "first_upgrade", Name = "改装师", Desc = "第一次升级部件", Icon = "⬆", Category = AchievementCategory.Vehicle, Hidden = false },
            new AchievementDef { Id = "all_max_level", Name = "完美载具", Desc = "所有部件升至最高等级", Icon = "💎", Category = AchievementCategory.Vehicle, Hidden = true },
            new AchievementDef { Id = "survive_low_fuel", Name = "最后一滴", Desc = "油量低于5%仍行驶1分钟", Icon = "⛽", Category = AchievementCategory.Vehicle, Hidden = true },

            // ─── 收集 ───
            new AchievementDef { Id = "fragment_10", Name = "碎片收集者", Desc = "发现10个碎片", Icon = "📜", Category = AchievementCategory.Collect, Hidden = false },
            new AchievementDef { Id = "fragment_30", Name = "历史探寻者", Desc = "发现30个碎片", Icon = "📖", Category = AchievementCategory.Collect, Hidden = false },
            new AchievementDef { Id = "fragment_all", Name = "完整的记忆", Desc = "发现全部碎片", Icon = "🏆", Category = AchievementCategory.Collect, Hidden = true },
            new AchievementDef { Id = "resource_full", Name = "满载而归", Desc = "背包载重达到上限", Icon = "🎒", Category = AchievementCategory.Collect, Hidden = false },
        };

        // =============================================================
        // 运行时状态
        // =============================================================

        private GameManager _gameManager;

        /// <summary>低油量计时起始时间（毫秒），null表示未开始计时</summary>
        private long? _lowFuelStartMs;

        // =============================================================
        // Unity 生命周期
        // =============================================================

        private void Awake()
        {
            _gameManager = GameManager.Instance;
        }

        private void OnEnable()
        {
            GameEvents.OnGamePhaseChanged += OnGamePhaseChanged;
            GameEvents.OnObstacleCleared += OnObstacleCleared;
            GameEvents.OnCampBuilt += OnCampBuilt;
            GameEvents.OnStationBuilt += OnStationBuilt;
            GameEvents.OnFragmentDiscovered += OnFragmentDiscovered;
            GameEvents.OnResourcesChanged += OnResourcesChanged;
            GameEvents.OnVehiclePartsChanged += OnVehiclePartsChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnGamePhaseChanged -= OnGamePhaseChanged;
            GameEvents.OnObstacleCleared -= OnObstacleCleared;
            GameEvents.OnCampBuilt -= OnCampBuilt;
            GameEvents.OnStationBuilt -= OnStationBuilt;
            GameEvents.OnFragmentDiscovered -= OnFragmentDiscovered;
            GameEvents.OnResourcesChanged -= OnResourcesChanged;
            GameEvents.OnVehiclePartsChanged -= OnVehiclePartsChanged;
        }

        // =============================================================
        // 事件驱动的自动检查
        // =============================================================

        private void OnGamePhaseChanged(int journey)
        {
            CheckJourneyAchievements();
        }

        private void OnObstacleCleared(string id)
        {
            CheckExploreAchievements();
        }

        private void OnCampBuilt(int count)
        {
            CheckCampAchievements();
        }

        private void OnStationBuilt(string siteId)
        {
            CheckBuildAchievements();
        }

        private void OnFragmentDiscovered(string id)
        {
            CheckCollectAchievements();
        }

        private void OnResourcesChanged(ResourceBag resources)
        {
            CheckCarryFullAchievement();
        }

        private void OnVehiclePartsChanged()
        {
            CheckVehicleAchievements();
        }

        // =============================================================
        // 旅途成就
        // =============================================================

        /// <summary>
        /// 检查旅途相关成就
        /// </summary>
        public void CheckJourneyAchievements()
        {
            if (_gameManager == null) return;
            int currentJourney = _gameManager.CurrentJourney;

            if (currentJourney >= 2) TryUnlock("reach_forest");
            if (currentJourney >= 3) TryUnlock("reach_valley");
            if (currentJourney >= 4) TryUnlock("reach_mountain");
            if (currentJourney >= 5)
            {
                TryUnlock("reach_summit");
                TryUnlock("journey_5");
            }
        }

        // =============================================================
        // 营地成就
        // =============================================================

        /// <summary>
        /// 检查营地相关成就
        /// </summary>
        public void CheckCampAchievements()
        {
            if (_gameManager == null) return;
            int campCount = _gameManager.BuiltCamps.Count;

            if (campCount >= 1) TryUnlock("first_camp");
            if (campCount >= 5) TryUnlock("camp_5");
            if (campCount >= 10) TryUnlock("camp_10");
        }

        /// <summary>
        /// 篝火小游戏完成时调用
        /// </summary>
        /// <param name="optimalPercent">最佳区间百分比（0~1）</param>
        public void CheckCampfireAchievement(float optimalPercent)
        {
            if (optimalPercent >= 0.9f) TryUnlock("campfire_master");
        }

        /// <summary>
        /// 电台锁定信号时调用
        /// </summary>
        /// <param name="lockedCount">已锁定信号数</param>
        public void CheckRadioAchievement(int lockedCount)
        {
            if (lockedCount >= 1) TryUnlock("radio_lock");
        }

        /// <summary>
        /// 星图发现星座时调用
        /// </summary>
        /// <param name="discoveredCount">已发现星座数</param>
        public void CheckStarmapAchievement(int discoveredCount)
        {
            if (discoveredCount >= 1) TryUnlock("stargazer");
            if (discoveredCount >= 4) TryUnlock("all_constellations");
        }

        /// <summary>
        /// 烹饪完成时调用
        /// </summary>
        public void CheckCookingAchievement()
        {
            TryUnlock("first_cook");
        }

        // =============================================================
        // 探索成就
        // =============================================================

        /// <summary>
        /// 检查探索相关成就
        /// </summary>
        public void CheckExploreAchievements()
        {
            if (_gameManager == null) return;
            int clearedCount = _gameManager.ClearedObstacles.Count;
            int pickedCount = _gameManager.PickedResources.Count;

            if (clearedCount >= 1) TryUnlock("first_obstacle");
            if (clearedCount >= 20) TryUnlock("obstacle_20");
            if (clearedCount >= 50) TryUnlock("obstacle_50");

            if (pickedCount >= 1) TryUnlock("first_resource");

            CheckBuildAchievements();
            CheckCarryFullAchievement();
        }

        /// <summary>
        /// 检查建造相关成就
        /// </summary>
        public void CheckBuildAchievements()
        {
            if (_gameManager == null) return;
            var allFacilities = new HashSet<FacilityType>();

            foreach (var kvp in _gameManager.Stations)
            {
                for (int i = 0; i < kvp.Value.Facilities.Count; i++)
                    allFacilities.Add(kvp.Value.Facilities[i]);
            }

            if (allFacilities.Count >= 1) TryUnlock("first_build");
            if (allFacilities.Count >= 6) TryUnlock("build_all_types");
        }

        // =============================================================
        // 载具成就
        // =============================================================

        /// <summary>
        /// 检查载具相关成就
        /// </summary>
        public void CheckVehicleAchievements()
        {
            if (_gameManager == null) return;
            var parts = _gameManager.VehicleParts;
            if (parts == null) return;

            PartId[] partIds = { PartId.Engine, PartId.Tires, PartId.Headlight, PartId.Tank, PartId.Body, PartId.Radio };
            bool allMaxLevel = true;
            for (int i = 0; i < partIds.Length; i++)
            {
                if (parts[(int)partIds[i]].Level < 5)
                {
                    allMaxLevel = false;
                    break;
                }
            }
            if (allMaxLevel) TryUnlock("all_max_level");
        }

        /// <summary>
        /// 修理部件成功时调用
        /// </summary>
        public void OnRepairPart()
        {
            TryUnlock("first_repair");
        }

        /// <summary>
        /// 升级部件成功时调用
        /// </summary>
        public void OnUpgradePart()
        {
            TryUnlock("first_upgrade");
            CheckVehicleAchievements();
        }

        // =============================================================
        // 收集成就
        // =============================================================

        /// <summary>
        /// 检查收集相关成就
        /// </summary>
        public void CheckCollectAchievements()
        {
            if (_gameManager == null) return;
            int fragmentCount = _gameManager.DiscoveredFragmentIds.Count;

            if (fragmentCount >= 10) TryUnlock("fragment_10");
            if (fragmentCount >= 30) TryUnlock("fragment_30");
            if (fragmentCount >= 40) TryUnlock("fragment_all");

            CheckCarryFullAchievement();
        }

        /// <summary>
        /// 检查载重满成就
        /// </summary>
        public void CheckCarryFullAchievement()
        {
            if (_gameManager == null) return;
            int carryTotal = _gameManager.CarryTotal();
            if (carryTotal >= _gameManager.MaxCarry) TryUnlock("resource_full");
        }

        // =============================================================
        // 低油量存活成就
        // =============================================================

        /// <summary>
        /// 低油量存活检查（由 Vehicle 帧循环调用）
        /// </summary>
        /// <param name="fuelPercent">当前油量百分比（0~1）</param>
        /// <param name="nowMs">当前时间戳（毫秒）</param>
        public void CheckLowFuelAchievement(float fuelPercent, long nowMs)
        {
            if (fuelPercent < 0.05f)
            {
                if (_lowFuelStartMs == null)
                {
                    _lowFuelStartMs = nowMs;
                }
                else if (nowMs - _lowFuelStartMs.Value >= 60000L)
                {
                    // 持续1分钟
                    TryUnlock("survive_low_fuel");
                }
            }
            else
            {
                _lowFuelStartMs = null;
            }
        }

        // =============================================================
        // 直接解锁
        // =============================================================

        /// <summary>
        /// 直接解锁指定成就（外部调用）
        /// </summary>
        /// <param name="id">成就ID</param>
        public void CheckAchievement(string id)
        {
            TryUnlock(id);
        }

        // =============================================================
        // 初始化
        // =============================================================

        /// <summary>
        /// 初始化成就系统：将定义同步到 GameManager 的成就列表
        /// </summary>
        public void InitializeAchievements()
        {
            if (_gameManager == null) return;

            var existingIds = new HashSet<string>();
            for (int i = 0; i < _gameManager.Achievements.Count; i++)
                existingIds.Add(_gameManager.Achievements[i].Id);

            for (int i = 0; i < AchievementDefs.Length; i++)
            {
                if (existingIds.Contains(AchievementDefs[i].Id)) continue;

                _gameManager.Achievements.Add(new Achievement
                {
                    Id = AchievementDefs[i].Id,
                    Name = AchievementDefs[i].Name,
                    Desc = AchievementDefs[i].Desc,
                    Icon = AchievementDefs[i].Icon,
                    Category = AchievementDefs[i].Category,
                    Hidden = AchievementDefs[i].Hidden,
                    Unlocked = false,
                    UnlockedAt = 0,
                });
            }
        }

        // =============================================================
        // 成就进度查询
        // =============================================================

        /// <summary>
        /// 获取指定分类的成就进度
        /// </summary>
        /// <param name="category">成就分类</param>
        /// <returns>(已解锁数, 总数)</returns>
        public (int unlocked, int total) GetCategoryProgress(AchievementCategory category)
        {
            if (_gameManager == null) return (0, 0);

            int unlocked = 0;
            int total = 0;

            for (int i = 0; i < _gameManager.Achievements.Count; i++)
            {
                if (_gameManager.Achievements[i].Category == category)
                {
                    total++;
                    if (_gameManager.Achievements[i].Unlocked) unlocked++;
                }
            }

            return (unlocked, total);
        }

        /// <summary>
        /// 获取总体成就进度
        /// </summary>
        /// <returns>(已解锁数, 总数)</returns>
        public (int unlocked, int total) GetTotalProgress()
        {
            if (_gameManager == null) return (0, 0);

            int unlocked = 0;
            int total = _gameManager.Achievements.Count;

            for (int i = 0; i < _gameManager.Achievements.Count; i++)
            {
                if (_gameManager.Achievements[i].Unlocked) unlocked++;
            }

            return (unlocked, total);
        }

        // =============================================================
        // 内部方法
        // =============================================================

        /// <summary>
        /// 尝试解锁单个成就（已解锁则跳过）
        /// </summary>
        /// <param name="id">成就ID</param>
        private void TryUnlock(string id)
        {
            if (_gameManager == null) return;
            _gameManager.UnlockAchievement(id);
        }
    }
}

# Handoff 文档

## 已完成任务
- 将 Zustand store (store.ts) 完整迁移为 Unity C# 游戏状态管理系统
- 创建 GameManager.cs (1692行) — 单例MonoBehaviour，持有所有游戏状态和逻辑
- 创建 SaveSystem.cs (242行) — JSON存档系统，含自动存档
- 创建 GameEvents.cs (262行) — 静态事件系统，22个事件
- PR已合并到main: https://github.com/huyan1349/WeiJinRoad/pull/1

## 迁移覆盖范围
- 所有枚举: ResourceKind, FrontAttachment, PartId, FacilityType, AchievementCategory, TerrainRenderMode, HeadlightsMode
- 所有数据结构: ResourceBag, PartState, NearbyResource, JournalEntry, Achievement, StationState, BuiltCamp, CampSite, JourneyNotification, PerfStats, DevTeleportTarget, VehicleTransientState, SaveData, StationEntry
- 所有持久化状态: resources, maxCarry, frontAttachment, vehicleParts, clearedObstacles, pickedResources, currentJourney, stations, builtCamps, achievements, purchasedItems, discoveredFragmentIds, journal, currentChapter, townVisited
- 所有运行时状态: timeOfDay, isSnowing, brightness, dev面板开关, 相机参数, 提灯, 扎营, 交互等
- 所有游戏逻辑: CanAfford, AddResources, SpendResources, RepairPart, UpgradePart, BuildFacility, ClearObstacle, PickupResource, DiscoverFragment, UnlockAchievement等

## 未完成/待注意
- GameManager.AddBuiltCamp() 直接引用 World.TerrainHeight.WorldToRouteZ()，存在Core→World的命名空间依赖，若需解耦可改用事件或接口
- SaveData 中 List<FacilityType> 的 JsonUtility 序列化可能需要验证（Unity对enum List的序列化支持有限，必要时可改为int列表）
- 成就定义模板（ACHIEVEMENT_DEFS）尚未迁移为C#数据，目前只迁移了运行时成就状态
- 站点选址定义（SITE_DEFS）尚未迁移
ENDOFFILE; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-696ef992ed604e148765ef99b2128795/cwd.txt'; exit "$__tr_native_ec"
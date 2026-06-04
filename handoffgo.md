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
---

## 2026-06-05: AudioManager 音频管理器

### 已完成
- 创建 AudioManager.cs (986行) — 将 WebAudio 程序化音频引擎 (audioEngine.ts) 近似翻译为 Unity AudioSource 系统
- 命名空间: WeiJinRoad.Audio
- PR已合并到main: https://github.com/huyan1349/WeiJinRoad/pull/4

### 翻译对照
| TS 函数 | C# 方法 | 实现方式 |
|---------|---------|---------|
| startEngine / updateEngine / stopEngine | StartEngine / UpdateEngine / StopEngine | AudioSource + pitch/volume 曲线 |
| startWind / setWindIntensity / stopWind | StartWind / UpdateWind / StopWind | AudioSource + volume 曲线 |
| searchlightOn / searchlightOff | PlaySearchlightOn / PlaySearchlightOff | PlayOneShot |
| fullIllumination | PlayFullIllumination | PlayOneShot |
| fragmentDiscover | PlayFragmentDiscover | PlayOneShot + GameEvents订阅 |
| brake | PlayBrake | PlayOneShot |
| treeImpact | PlayTreeImpact | PlayOneShot + power参数 |
| chapterTransition | PlayChapterTransition | PlayOneShot + GameEvents订阅 |
| lowHum | PlayLowHum | PlayOneShot |
| heartbeat / stopHeartbeat | StartHeartbeat / StopHeartbeat | 定时器脉冲播放 |
| menuStart | PlayMenuStart | PlayOneShot |
| interactClick | PlayUIClick | PlayOneShot + 随机pitch |
| killAll | KillAll | 全部渐隐停止 |

### 架构特点
- Singleton + DontDestroyOnLoad
- 12个 AudioSource 对象池用于一次性音效
- 订阅 GameEvents (OnFragmentDiscovered, OnGamePhaseChanged, OnObstacleCleared, OnHealthChanged)
- 三级音量控制: MasterVolume / SFXVolume / BGMVolume
- 渐入/渐出协程
- BGM: The_Long_Way_Up.mp3 循环播放

### 未完成/待注意
- 所有 AudioClip 引用需在 Inspector 中拖入实际音频文件（目前为 null 占位）
- 引擎/风声的 AudioClip 需要制作或获取（低频隆隆声、噪声、风声循环片段）
- 原版 WebAudio 的实时合成效果（振荡器频率滑动、滤波器截止频率变化）无法完全用 AudioClip 还原，pitch/volume 曲线仅为近似
- 心跳音效的BPM与生命值关联逻辑依赖 GameEvents.OnHealthChanged，需确认 VehicleDamageSystem 是否触发该事件
- SetEngineBoost() 需由 VehicleController 在 Shift 加速时调用
EOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-56a593cec9ae41738c2564f623d8d10f/cwd.txt'; exit "$__tr_native_ec"
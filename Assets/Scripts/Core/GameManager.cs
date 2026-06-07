using System;
using System.Collections.Generic;
using UnityEngine;

namespace WeiJinRoad.Core
{
    // =================================================================
    // 枚举类型 — 对应 TypeScript 的联合类型
    // =================================================================

    /// <summary>
    /// 资源种类：金属、木材、燃料、信号件、光源晶
    /// </summary>
    public enum ResourceKind
    {
        /// <summary>金属</summary>
        Metal,
        /// <summary>木材</summary>
        Wood,
        /// <summary>燃料</summary>
        Fuel,
        /// <summary>信号件</summary>
        Signal,
        /// <summary>光源晶</summary>
        Crystal
    }

    /// <summary>
    /// 前挂件类型：无、铲雪犁、绞盘
    /// </summary>
    public enum FrontAttachment
    {
        /// <summary>无</summary>
        None,
        /// <summary>铲雪犁</summary>
        Plow,
        /// <summary>绞盘</summary>
        Winch
    }

    /// <summary>
    /// 载具部件ID：发动机、轮胎、探照灯、油箱、车身、电台
    /// </summary>
    public enum PartId
    {
        /// <summary>发动机</summary>
        Engine,
        /// <summary>轮胎</summary>
        Tires,
        /// <summary>探照灯</summary>
        Headlight,
        /// <summary>油箱</summary>
        Tank,
        /// <summary>车身</summary>
        Body,
        /// <summary>电台</summary>
        Radio
    }

    /// <summary>
    /// 设施类型：补给仓、避风棚、信号塔、灯塔、观测台、简易桥
    /// </summary>
    public enum FacilityType
    {
        /// <summary>补给仓</summary>
        Supply,
        /// <summary>避风棚</summary>
        Shelter,
        /// <summary>信号塔</summary>
        SignalTower,
        /// <summary>灯塔</summary>
        Beacon,
        /// <summary>观测台</summary>
        Observatory,
        /// <summary>简易桥</summary>
        Bridge
    }

    /// <summary>
    /// 成就分类：旅途、营地、探索、载具、收集
    /// </summary>
    public enum AchievementCategory
    {
        /// <summary>旅途</summary>
        Journey,
        /// <summary>营地</summary>
        Camp,
        /// <summary>探索</summary>
        Explore,
        /// <summary>载具</summary>
        Vehicle,
        /// <summary>收集</summary>
        Collect
    }

    /// <summary>
    /// 地形渲染模式：完整、可见区域、走廊
    /// </summary>
    public enum TerrainRenderMode
    {
        /// <summary>完整渲染</summary>
        Full,
        /// <summary>仅可见区域</summary>
        Visible,
        /// <summary>走廊模式</summary>
        Corridor
    }


    /// <summary>
    /// 游戏阶段：菜单、游戏中、暂停
    /// </summary>
    public enum GamePhase
    {
        /// <summary>主菜单</summary>
        Menu,
        /// <summary>游戏中</summary>
        Playing,
        /// <summary>暂停</summary>
        Paused
    }

    /// <summary>
    /// 车灯模式：自动、常开、关闭
    /// </summary>
    public enum HeadlightsMode
    {
        /// <summary>自动</summary>
        Auto,
        /// <summary>常开</summary>
        On,
        /// <summary>关闭</summary>
        Off
    }

    // =================================================================
    // 可序列化数据结构
    // =================================================================

    /// <summary>
    /// 资源背包：存储五种资源的数量
    /// </summary>
    [Serializable]
    public class ResourceBag
    {
        /// <summary>金属</summary>
        public int Metal;
        /// <summary>木材</summary>
        public int Wood;
        /// <summary>燃料</summary>
        public int Fuel;
        /// <summary>信号件</summary>
        public int Signal;
        /// <summary>光源晶</summary>
        public int Crystal;

        /// <summary>
        /// 计算背包总资源数
        /// </summary>
        /// <returns>所有资源数量之和</returns>
        public int Total() => Metal + Wood + Fuel + Signal + Crystal;

        /// <summary>
        /// 创建资源背包的深拷贝
        /// </summary>
        public ResourceBag Clone() => new ResourceBag
        {
            Metal = Metal,
            Wood = Wood,
            Fuel = Fuel,
            Signal = Signal,
            Crystal = Crystal
        };

        /// <summary>
        /// 按资源种类获取/设置数量
        /// </summary>
        public int this[ResourceKind kind]
        {
            get => kind switch
            {
                ResourceKind.Metal => Metal,
                ResourceKind.Wood => Wood,
                ResourceKind.Fuel => Fuel,
                ResourceKind.Signal => Signal,
                ResourceKind.Crystal => Crystal,
                _ => 0
            };
            set
            {
                switch (kind)
                {
                    case ResourceKind.Metal: Metal = value; break;
                    case ResourceKind.Wood: Wood = value; break;
                    case ResourceKind.Fuel: Fuel = value; break;
                    case ResourceKind.Signal: Signal = value; break;
                    case ResourceKind.Crystal: Crystal = value; break;
                }
            }
        }
    }

    /// <summary>
    /// 载具部件状态：耐久度(0~1)和等级(1+)
    /// </summary>
    [Serializable]
    public class PartState
    {
        /// <summary>耐久度，范围0~1</summary>
        [Range(0f, 1f)]
        public float Condition = 1f;
        /// <summary>等级，最小为1</summary>
        public int Level = 1;
    }

    /// <summary>
    /// 附近资源点信息
    /// </summary>
    [Serializable]
    public class NearbyResource
    {
        /// <summary>资源点ID</summary>
        public string Id;
        /// <summary>资源种类</summary>
        public ResourceKind Kind;
        /// <summary>数量</summary>
        public int Amount;
        /// <summary>距离</summary>
        public float Distance;
    }

    /// <summary>
    /// 日志条目：发现的碎片记录
    /// </summary>
    [Serializable]
    public class JournalEntry
    {
        /// <summary>条目ID</summary>
        public string Id;
        /// <summary>碎片ID</summary>
        public string FragmentId;
        /// <summary>章节</summary>
        public string Chapter;
        /// <summary>标题</summary>
        public string Title;
        /// <summary>内容</summary>
        public string Content;
        /// <summary>载体类型</summary>
        public string CarrierType;
        /// <summary>生态区</summary>
        public string Biome;
        /// <summary>发现时间戳</summary>
        public long DiscoveredAt;
        /// <summary>地点名称</summary>
        public string LocationName;
    }

    /// <summary>
    /// 附近可交互对象
    /// </summary>
    [Serializable]
    public class NearbyInteractable
    {
        /// <summary>对象ID</summary>
        public string Id;
        /// <summary>类型</summary>
        public string Type;
        /// <summary>标题</summary>
        public string Title;
        /// <summary>提示文本</summary>
        public string Hint;
        /// <summary>距离</summary>
        public float Distance;
        /// <summary>世界坐标X</summary>
        public float X;
        /// <summary>世界坐标Y</summary>
        public float Y;
        /// <summary>世界坐标Z</summary>
        public float Z;
    }

    /// <summary>
    /// 叙事内容：碎片叙事展示数据
    /// </summary>
    [Serializable]
    public class NarrativeContent
    {
        /// <summary>标题</summary>
        public string Title;
        /// <summary>正文内容</summary>
        public string Content;
        /// <summary>载体类型</summary>
        public string CarrierType;
        /// <summary>章节</summary>
        public string Chapter;
        /// <summary>生态区</summary>
        public string Biome;
    }

    /// <summary>
    /// 成就数据
    /// </summary>
    [Serializable]
    public class Achievement
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
        /// <summary>是否已解锁</summary>
        public bool Unlocked;
        /// <summary>解锁时间戳（0表示未解锁）</summary>
        public long UnlockedAt;
    }

    /// <summary>
    /// 营地站点状态
    /// </summary>
    [Serializable]
    public class StationState
    {
        /// <summary>是否已建造</summary>
        public bool Built;
        /// <summary>已建设施列表</summary>
        public List<FacilityType> Facilities = new List<FacilityType>();
        /// <summary>站点等级</summary>
        public int Level;
    }

    /// <summary>
    /// 已建营地记录
    /// </summary>
    [Serializable]
    public class BuiltCamp
    {
        /// <summary>世界坐标X</summary>
        public float X;
        /// <summary>世界坐标Z</summary>
        public float Z;
        /// <summary>路线空间Z坐标</summary>
        public float RouteZ;
    }

    /// <summary>
    /// 营地位置信息
    /// </summary>
    [Serializable]
    public class CampSite
    {
        /// <summary>世界坐标X</summary>
        public float X;
        /// <summary>世界坐标Z</summary>
        public float Z;
        /// <summary>朝向角度（弧度）</summary>
        public float Heading;
    }

    /// <summary>
    /// 旅程通知
    /// </summary>
    [Serializable]
    public class JourneyNotification
    {
        /// <summary>起始旅程</summary>
        public int From;
        /// <summary>目标旅程</summary>
        public int To;
        /// <summary>通知消息</summary>
        public string Message;
    }

    /// <summary>
    /// 性能统计数据
    /// </summary>
    [Serializable]
    public class PerfStats
    {
        /// <summary>帧率</summary>
        public float Fps;
        /// <summary>帧耗时(ms)</summary>
        public float FrameMs;
        /// <summary>Draw Call数</summary>
        public int DrawCalls;
        /// <summary>三角形数</summary>
        public int Triangles;
        /// <summary>纹理数</summary>
        public int Textures;
        /// <summary>几何体数</summary>
        public int Geometries;
    }

    /// <summary>
    /// 开发者传送目标
    /// </summary>
    [Serializable]
    public class DevTeleportTarget
    {
        /// <summary>序列号（每次递增，用于检测变更）</summary>
        public int Serial;
        /// <summary>目标X坐标</summary>
        public float X;
        /// <summary>目标Z坐标</summary>
        public float Z;
    }

    /// <summary>
    /// 载具瞬态数据（运行时，不持久化）
    /// </summary>
    [Serializable]
    public class VehicleTransientState
    {
        /// <summary>位置 [X, Z]</summary>
        public float[] Position = { 0f, 270f };
        /// <summary>朝向角度（弧度）</summary>
        public float Heading = Mathf.PI;
        /// <summary>速度</summary>
        public float Speed;
        /// <summary>转向速率</summary>
        public float SteerRate;
        /// <summary>探照灯强度</summary>
        public float SearchlightIntensity;
        /// <summary>全照明强度</summary>
        public float FullIlluminationIntensity;
    }

    // =================================================================
    // 持久化数据容器 — 仅包含需要保存的字段
    // =================================================================

    /// <summary>
    /// 持久化存档数据，对应 Zustand persist 的 partialize 字段
    /// </summary>
    [Serializable]
    public class SaveData
    {
        /// <summary>资源背包</summary>
        public ResourceBag Resources;
        /// <summary>最大载重量</summary>
        public int MaxCarry;
        /// <summary>前挂件类型</summary>
        public FrontAttachment FrontAttachment;
        /// <summary>载具部件状态（6个部件）</summary>
        public PartState[] VehicleParts = new PartState[6];
        /// <summary>已清除的障碍物ID列表</summary>
        public List<string> ClearedObstacles = new List<string>();
        /// <summary>已拾取的资源ID列表</summary>
        public List<string> PickedResources = new List<string>();
        /// <summary>当前旅程（1~5）</summary>
        public int CurrentJourney;
        /// <summary>站点建造状态（序列化为键值对列表）</summary>
        public List<StationEntry> Stations = new List<StationEntry>();
        /// <summary>已建营地列表</summary>
        public List<BuiltCamp> BuiltCamps = new List<BuiltCamp>();
        /// <summary>成就列表</summary>
        public List<Achievement> Achievements = new List<Achievement>();
        /// <summary>已购买物品ID列表</summary>
        public List<string> PurchasedItems = new List<string>();
        /// <summary>已发现碎片ID列表</summary>
        public List<string> DiscoveredFragmentIds = new List<string>();
        /// <summary>日志条目列表</summary>
        public List<JournalEntry> Journal = new List<JournalEntry>();
        /// <summary>当前章节</summary>
        public string CurrentChapter;
        /// <summary>是否已访问小镇</summary>
        public bool TownVisited;
    }

    /// <summary>
    /// 站点序列化条目（Dictionary的JSON兼容替代）
    /// </summary>
    [Serializable]
    public class StationEntry
    {
        /// <summary>站点ID</summary>
        public string SiteId;
        /// <summary>站点状态</summary>
        public StationState State;
    }

    // =================================================================
    // GameManager — 游戏状态管理单例
    // =================================================================

    /// <summary>
    /// 游戏状态管理器，单例MonoBehaviour
    ///
    /// 持有所有游戏状态，替代 Zustand store 的功能。
    /// 使用 DontDestroyOnLoad 确保跨场景持久化。
    /// 属性变更时触发 GameEvents 中的对应事件。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        // =============================================================
        // 单例
        // =============================================================

        private static GameManager _instance;

        /// <summary>
        /// 全局单例实例
        /// </summary>
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindFirstObjectByType<GameManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("[GameManager]");
                        _instance = go.AddComponent<GameManager>();
                    }
                }
                return _instance;
            }
        }

        // =============================================================
        // 默认值常量
        // =============================================================

        private static readonly ResourceBag InitialResources = new ResourceBag
        {
            Metal = 3, Wood = 3, Fuel = 6, Signal = 1, Crystal = 0
        };

        private const int DefaultMaxCarry = 40;
        private const FrontAttachment DefaultFrontAttachment = FrontAttachment.Plow;
        private const int DefaultCurrentJourney = 1;
        private const string DefaultCurrentChapter = "ch1";

        private static PartState[] CreateInitialParts() => new PartState[]
        {
            new PartState { Condition = 1f, Level = 1 }, // Engine
            new PartState { Condition = 1f, Level = 1 }, // Tires
            new PartState { Condition = 1f, Level = 1 }, // Headlight
            new PartState { Condition = 1f, Level = 1 }, // Tank
            new PartState { Condition = 1f, Level = 1 }, // Body
            new PartState { Condition = 1f, Level = 1 }, // Radio
        };

        // =============================================================
        // 持久化状态（对应 Zustand persist partialize）
        // =============================================================

        [Header("资源与载重")]
        [SerializeField] private ResourceBag _resources;
        [SerializeField] private int _maxCarry = DefaultMaxCarry;
        [SerializeField] private FrontAttachment _frontAttachment = DefaultFrontAttachment;

        [Header("载具部件")]
        [SerializeField] private PartState[] _vehicleParts;

        [Header("世界进度")]
        [SerializeField] private List<string> _clearedObstacles = new List<string>();
        [SerializeField] private List<string> _pickedResources = new List<string>();
        [SerializeField] private int _currentJourney = DefaultCurrentJourney;

        [Header("站点与营地")]
        [SerializeField] private Dictionary<string, StationState> _stations = new Dictionary<string, StationState>();
        [SerializeField] private List<BuiltCamp> _builtCamps = new List<BuiltCamp>();

        [Header("成就")]
        [SerializeField] private List<Achievement> _achievements = new List<Achievement>();

        [Header("小镇")]
        [SerializeField] private bool _townVisited;
        [SerializeField] private List<string> _purchasedItems = new List<string>();

        [Header("叙事与日志")]
        [SerializeField] private List<JournalEntry> _journal = new List<JournalEntry>();
        [SerializeField] private List<string> _discoveredFragmentIds = new List<string>();
        [SerializeField] private string _currentChapter = DefaultCurrentChapter;

        // =============================================================
        // 运行时状态（不持久化）
        // =============================================================

        [Header("游戏阶段")]
        [SerializeField] private GamePhase _gamePhase = GamePhase.Menu;

        [Header("环境")]
        [SerializeField] private float _timeOfDay = 5f;
        private float _timeFlowTimer;
        [SerializeField] private bool _isSnowing = true;
        [SerializeField] private float _brightness = 1f;
        [SerializeField] private float _noiseIntensity = 0.02f;
        [SerializeField] private bool _ambientOcclusion;
        [SerializeField] private TerrainRenderMode _terrainRenderMode = TerrainRenderMode.Full;

        [Header("开发者面板")]
        [SerializeField] private bool _devPanelVisible;
        [SerializeField] private bool _devPostProcessing = true;
        [SerializeField] private bool _devBloom = true;
        [SerializeField] private bool _devShadows = true;
        [SerializeField] private bool _devSnow = true;
        [SerializeField] private bool _devFog = true;
        [SerializeField] private bool _devLights = true;
        [SerializeField] private bool _devParticles = true;
        [SerializeField] private bool _devTireTracks = true;
        [SerializeField] private bool _devGodView;
        [SerializeField] private PerfStats _perfStats = new PerfStats();
        [SerializeField] private DevTeleportTarget _devTeleportTarget = new DevTeleportTarget { Serial = 0, X = 0f, Z = 270f };

        [Header("相机与灯光")]
        [SerializeField] private HeadlightsMode _headlightsMode = HeadlightsMode.On;
        [SerializeField] private bool _cameraFollow = true;
        [SerializeField] private float _cameraHeight = 10f;
        [SerializeField] private float _cameraDistance = 60f;
        [SerializeField] private float _cameraYawOffset;
        [SerializeField] private float _cameraPitchOffset;
        [SerializeField] private bool _summitCameraAssistVisible;

        [Header("提灯")]
        [SerializeField] private int _teleportToLanternVillageSerial;
        [SerializeField] private float _lanternLitProgress;
        [SerializeField] private bool _nearLantern;

        [Header("地图")]
        [SerializeField] private bool _showLargeMap;
        [SerializeField] private bool _experimentalTerrain;

        [Header("交互")]
        [SerializeField] private NearbyResource _nearbyResource;
        [SerializeField] private NearbyInteractable _nearbyInteractable;
        [SerializeField] private bool _narrativeOverlayVisible;
        [SerializeField] private NarrativeContent _currentNarrativeContent;
        [SerializeField] private bool _journalOverlayVisible;

        [Header("扎营")]
        [SerializeField] private bool _camping;
        [SerializeField] private CampSite _campSite;

        [Header("站点运行时")]
        [SerializeField] private string _nearbyStation;

        [Header("旅程通知")]
        [SerializeField] private JourneyNotification _journeyNotification;
        [SerializeField] private Achievement _achievementNotification;

        [Header("小镇运行时")]
        [SerializeField] private string _nearbyShop;

        [Header("载具瞬态")]
        [SerializeField] private VehicleTransientState _vehicleTransient = new VehicleTransientState();

        // =============================================================
        // 公共属性 — 带事件触发
        // =============================================================

        // ─── 资源 ───

        /// <summary>资源背包</summary>
        public ResourceBag Resources
        {
            get => _resources;
            set
            {
                _resources = value;
                GameEvents.OnResourcesChanged?.Invoke(value);
            }
        }

        /// <summary>最大载重量</summary>
        public int MaxCarry
        {
            get => _maxCarry;
            set => _maxCarry = value;
        }

        /// <summary>前挂件类型</summary>
        public FrontAttachment FrontAttachmentType
        {
            get => _frontAttachment;
            set
            {
                _frontAttachment = value;
                GameEvents.OnVehiclePartsChanged?.Invoke();
            }
        }

        /// <summary>载具部件状态数组（按PartId枚举顺序）</summary>
        public PartState[] VehicleParts
        {
            get => _vehicleParts;
            set
            {
                _vehicleParts = value;
                GameEvents.OnVehiclePartsChanged?.Invoke();
            }
        }

        /// <summary>已清除的障碍物ID列表</summary>
        public List<string> ClearedObstacles => _clearedObstacles;

        /// <summary>已拾取的资源ID列表</summary>
        public List<string> PickedResources => _pickedResources;

        /// <summary>当前旅程（1~5）</summary>
        public int CurrentJourney
        {
            get => _currentJourney;
            set
            {
                _currentJourney = Mathf.Max(1, Mathf.Min(5, value));
                GameEvents.OnGamePhaseChanged?.Invoke(_currentJourney);
            }
        }

        /// <summary>站点建造状态字典</summary>
        public Dictionary<string, StationState> Stations => _stations;

        /// <summary>已建营地列表</summary>
        public List<BuiltCamp> BuiltCamps => _builtCamps;

        /// <summary>成就列表</summary>
        public List<Achievement> Achievements => _achievements;

        /// <summary>是否已访问小镇</summary>
        public bool TownVisited
        {
            get => _townVisited;
            set
            {
                _townVisited = value;
                if (value) GameEvents.OnStationBuilt?.Invoke("town");
            }
        }

        /// <summary>已购买物品ID列表</summary>
        public List<string> PurchasedItems => _purchasedItems;

        /// <summary>日志条目列表</summary>
        public List<JournalEntry> Journal => _journal;

        /// <summary>已发现碎片ID列表</summary>
        public List<string> DiscoveredFragmentIds => _discoveredFragmentIds;

        /// <summary>当前章节</summary>
        public string CurrentChapter
        {
            get => _currentChapter;
            set => _currentChapter = value;
        }

        // ─── 游戏阶段 ───

        /// <summary>游戏阶段</summary>
        public GamePhase Phase
        {
            get => _gamePhase;
            set => _gamePhase = value;
        }

        // ─── 环境 ───

        /// <summary>时间（小时）</summary>
        public float TimeOfDay
        {
            get => _timeOfDay;
            set => _timeOfDay = value;
        }

        /// <summary>是否下雪</summary>
        public bool IsSnowing
        {
            get => _isSnowing;
            set => _isSnowing = value;
        }

        /// <summary>亮度</summary>
        public float Brightness
        {
            get => _brightness;
            set => _brightness = value;
        }

        /// <summary>噪声强度</summary>
        public float NoiseIntensity
        {
            get => _noiseIntensity;
            set => _noiseIntensity = value;
        }

        /// <summary>环境光遮蔽</summary>
        public bool AmbientOcclusion
        {
            get => _ambientOcclusion;
            set => _ambientOcclusion = value;
        }

        /// <summary>地形渲染模式</summary>
        public TerrainRenderMode TerrainRenderModeType
        {
            get => _terrainRenderMode;
            set => _terrainRenderMode = value;
        }

        // ─── 开发者 ───

        /// <summary>开发者面板是否可见</summary>
        public bool DevPanelVisible
        {
            get => _devPanelVisible;
            set => _devPanelVisible = value;
        }

        /// <summary>开发者后处理开关</summary>
        public bool DevPostProcessing { get => _devPostProcessing; set => _devPostProcessing = value; }
        /// <summary>开发者泛光开关</summary>
        public bool DevBloom { get => _devBloom; set => _devBloom = value; }
        /// <summary>开发者阴影开关</summary>
        public bool DevShadows { get => _devShadows; set => _devShadows = value; }
        /// <summary>开发者雪效果开关</summary>
        public bool DevSnow { get => _devSnow; set => _devSnow = value; }
        /// <summary>开发者雾效果开关</summary>
        public bool DevFog { get => _devFog; set => _devFog = value; }
        /// <summary>开发者灯光开关</summary>
        public bool DevLights { get => _devLights; set => _devLights = value; }
        /// <summary>开发者粒子开关</summary>
        public bool DevParticles { get => _devParticles; set => _devParticles = value; }
        /// <summary>开发者轮胎痕迹开关</summary>
        public bool DevTireTracks { get => _devTireTracks; set => _devTireTracks = value; }

        /// <summary>开发者上帝视角</summary>
        public bool DevGodView
        {
            get => _devGodView;
            set
            {
                _devGodView = value;
                if (value) _cameraFollow = false;
            }
        }

        /// <summary>性能统计数据</summary>
        public PerfStats PerfStatsData { get => _perfStats; set => _perfStats = value; }

        /// <summary>开发者传送目标</summary>
        public DevTeleportTarget DevTeleportTargetData => _devTeleportTarget;

        // ─── 相机与灯光 ───

        /// <summary>车灯模式</summary>
        public HeadlightsMode HeadlightsModeType
        {
            get => _headlightsMode;
            set => _headlightsMode = value;
        }

        /// <summary>相机是否跟随</summary>
        public bool CameraFollow
        {
            get => _cameraFollow;
            set => _cameraFollow = value;
        }

        /// <summary>相机高度</summary>
        public float CameraHeight
        {
            get => _cameraHeight;
            set => _cameraHeight = value;
        }

        /// <summary>相机距离</summary>
        public float CameraDistance
        {
            get => _cameraDistance;
            set => _cameraDistance = value;
        }

        /// <summary>相机偏航偏移</summary>
        public float CameraYawOffset { get => _cameraYawOffset; set => _cameraYawOffset = value; }

        /// <summary>相机俯仰偏移</summary>
        public float CameraPitchOffset { get => _cameraPitchOffset; set => _cameraPitchOffset = value; }

        /// <summary>山顶相机辅助是否可见</summary>
        public bool SummitCameraAssistVisible
        {
            get => _summitCameraAssistVisible;
            set => _summitCameraAssistVisible = value;
        }

        // ─── 提灯 ───

        /// <summary>传送至提灯村序列号</summary>
        public int TeleportToLanternVillageSerial => _teleportToLanternVillageSerial;

        /// <summary>提灯点亮进度（0=关，1=全亮）</summary>
        public float LanternLitProgress
        {
            get => _lanternLitProgress;
            set => _lanternLitProgress = value;
        }

        /// <summary>玩家是否靠近提灯</summary>
        public bool NearLantern
        {
            get => _nearLantern;
            set => _nearLantern = value;
        }

        // ─── 地图 ───

        /// <summary>是否显示大地图</summary>
        public bool ShowLargeMap
        {
            get => _showLargeMap;
            set => _showLargeMap = value;
        }

        /// <summary>实验性地形开关</summary>
        public bool ExperimentalTerrain
        {
            get => _experimentalTerrain;
            set => _experimentalTerrain = value;
        }

        // ─── 交互 ───

        /// <summary>附近资源点</summary>
        public NearbyResource NearbyResourceData
        {
            get => _nearbyResource;
            set => _nearbyResource = value;
        }

        /// <summary>附近可交互对象</summary>
        public NearbyInteractable NearbyInteractableData
        {
            get => _nearbyInteractable;
            set => _nearbyInteractable = value;
        }

        /// <summary>叙事覆盖层是否可见</summary>
        public bool NarrativeOverlayVisible
        {
            get => _narrativeOverlayVisible;
            set => _narrativeOverlayVisible = value;
        }

        /// <summary>当前叙事内容</summary>
        public NarrativeContent CurrentNarrativeContentData
        {
            get => _currentNarrativeContent;
            set => _currentNarrativeContent = value;
        }

        /// <summary>日志覆盖层是否可见</summary>
        public bool JournalOverlayVisible
        {
            get => _journalOverlayVisible;
            set => _journalOverlayVisible = value;
        }

        // ─── 扎营 ───

        /// <summary>是否正在扎营</summary>
        public bool Camping
        {
            get => _camping;
            set => _camping = value;
        }

        /// <summary>营地位置</summary>
        public CampSite CampSiteData
        {
            get => _campSite;
            set => _campSite = value;
        }

        // ─── 站点运行时 ───

        /// <summary>当前靠近的站点ID</summary>
        public string NearbyStation
        {
            get => _nearbyStation;
            set => _nearbyStation = value;
        }

        // ─── 旅程通知 ───

        /// <summary>旅程通知</summary>
        public JourneyNotification JourneyNotificationData
        {
            get => _journeyNotification;
            set => _journeyNotification = value;
        }

        /// <summary>成就通知</summary>
        public Achievement AchievementNotificationData
        {
            get => _achievementNotification;
            set => _achievementNotification = value;
        }

        // ─── 小镇运行时 ───

        /// <summary>当前靠近的商店ID</summary>
        public string NearbyShop
        {
            get => _nearbyShop;
            set => _nearbyShop = value;
        }

        // ─── 载具瞬态 ───

        /// <summary>载具瞬态数据（运行时，不持久化）</summary>
        public VehicleTransientState VehicleTransient => _vehicleTransient;

        // =============================================================
        // Unity 生命周期
        // =============================================================

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeDefaults();
        }

        private void Update()
        {
            SaveSystem.AutoSaveCheck();
            UpdateTimeFlow();
        }

        /// <summary>
        /// 时间流动逻辑（对应 React TimeFlow 组件）
        /// 每250ms增加0.025小时，24小时循环，仅在游戏运行时
        /// </summary>
        private void UpdateTimeFlow()
        {
            if (_gamePhase != GamePhase.Playing) return;

            _timeFlowTimer += Time.deltaTime * 1000f;
            if (_timeFlowTimer >= 250f)
            {
                _timeFlowTimer -= 250f;
                _timeOfDay = (_timeOfDay + 0.025f) % 24f;
                GameEvents.OnTimeOfDayChanged?.Invoke(_timeOfDay);
            }
        }

        // =============================================================
        // 初始化
        // =============================================================

        /// <summary>
        /// 初始化所有状态为默认值
        /// </summary>
        private void InitializeDefaults()
        {
            _resources = InitialResources.Clone();
            _maxCarry = DefaultMaxCarry;
            _frontAttachment = DefaultFrontAttachment;
            _vehicleParts = CreateInitialParts();
            _clearedObstacles = new List<string>();
            _pickedResources = new List<string>();
            _currentJourney = DefaultCurrentJourney;
            _stations = new Dictionary<string, StationState>();
            _builtCamps = new List<BuiltCamp>();
            _achievements = new List<Achievement>();
            _purchasedItems = new List<string>();
            _discoveredFragmentIds = new List<string>();
            _journal = new List<JournalEntry>();
            _currentChapter = DefaultCurrentChapter;
            _townVisited = false;

            // 运行时状态
            _gamePhase = GamePhase.Menu;
            _timeFlowTimer = 0f;
            _timeOfDay = 5f;
            _isSnowing = true;
            _brightness = 1f;
            _noiseIntensity = 0.02f;
            _ambientOcclusion = false;
            _terrainRenderMode = TerrainRenderMode.Full;
            _headlightsMode = HeadlightsMode.On;
            _cameraFollow = true;
            _cameraHeight = 10f;
            _cameraDistance = 60f;
            _camping = false;
            _campSite = null;
            _nearbyResource = null;
            _nearbyInteractable = null;
            _narrativeOverlayVisible = false;
            _currentNarrativeContent = null;
            _journalOverlayVisible = false;
            _nearbyStation = null;
            _nearbyShop = null;
            _journeyNotification = null;
            _achievementNotification = null;
            _lanternLitProgress = 0f;
            _nearLantern = false;
            _showLargeMap = false;
            _experimentalTerrain = false;
            _devPanelVisible = false;
            _devPostProcessing = true;
            _devBloom = true;
            _devShadows = true;
            _devSnow = true;
            _devFog = true;
            _devLights = true;
            _devParticles = true;
            _devTireTracks = true;
            _devGodView = false;
            _perfStats = new PerfStats();
            _devTeleportTarget = new DevTeleportTarget { Serial = 0, X = 0f, Z = 270f };
            _vehicleTransient = new VehicleTransientState();
        }

        /// <summary>
        /// 开始新游戏，重置所有状态到初始值
        /// </summary>
        public void NewGame()
        {
            InitializeDefaults();
            SaveSystem.DeleteSave();
            GameEvents.OnGamePhaseChanged?.Invoke(_currentJourney);
        }

        /// <summary>
        /// 重置游戏进度（保留运行时设置，重置持久化数据）
        /// </summary>
        public void ResetToDefaults()
        {
            _resources = InitialResources.Clone();
            _vehicleParts = CreateInitialParts();
            _clearedObstacles.Clear();
            _pickedResources.Clear();
            _nearbyResource = null;
            _frontAttachment = DefaultFrontAttachment;
            _stations.Clear();
            _builtCamps.Clear();
            _currentJourney = DefaultCurrentJourney;
            _journeyNotification = null;
            _achievements.Clear();
            _achievementNotification = null;
            _purchasedItems.Clear();
            _discoveredFragmentIds.Clear();
            _journal.Clear();
            _currentChapter = DefaultCurrentChapter;
            _townVisited = false;

            GameEvents.OnResourcesChanged?.Invoke(_resources);
            GameEvents.OnVehiclePartsChanged?.Invoke();
            GameEvents.OnGamePhaseChanged?.Invoke(_currentJourney);
        }

        // =============================================================
        // 游戏逻辑方法 — 对应 Zustand store 的 actions
        // =============================================================

        #region 资源管理

        /// <summary>
        /// 计算当前背包总载重
        /// </summary>
        /// <returns>资源总数</returns>
        public int CarryTotal() => _resources.Total();

        /// <summary>
        /// 判断是否能够支付指定资源消耗
        /// </summary>
        /// <param name="cost">消耗的资源数量</param>
        /// <returns>是否足够</returns>
        public bool CanAfford(ResourceBag cost)
        {
            if (cost == null) return true;
            return _resources.Metal >= cost.Metal
                && _resources.Wood >= cost.Wood
                && _resources.Fuel >= cost.Fuel
                && _resources.Signal >= cost.Signal
                && _resources.Crystal >= cost.Crystal;
        }

        /// <summary>
        /// 添加资源到背包（受最大载重限制）
        /// </summary>
        /// <param name="delta">要添加的资源</param>
        public void AddResources(ResourceBag delta)
        {
            if (delta == null) return;
            int space = _maxCarry - _resources.Total();

            if (delta.Metal > 0) { int add = Mathf.Min(delta.Metal, space); _resources.Metal += add; space -= add; }
            if (space <= 0) goto Done;
            if (delta.Wood > 0) { int add = Mathf.Min(delta.Wood, space); _resources.Wood += add; space -= add; }
            if (space <= 0) goto Done;
            if (delta.Fuel > 0) { int add = Mathf.Min(delta.Fuel, space); _resources.Fuel += add; space -= add; }
            if (space <= 0) goto Done;
            if (delta.Signal > 0) { int add = Mathf.Min(delta.Signal, space); _resources.Signal += add; space -= add; }
            if (space <= 0) goto Done;
            if (delta.Crystal > 0) { int add = Mathf.Min(delta.Crystal, space); _resources.Crystal += add; space -= add; }

        Done:
            GameEvents.OnResourcesChanged?.Invoke(_resources);
        }

        /// <summary>
        /// 消耗资源，若不足则不消耗
        /// </summary>
        /// <param name="cost">消耗量</param>
        /// <returns>是否消耗成功</returns>
        public bool SpendResources(ResourceBag cost)
        {
            if (!CanAfford(cost)) return false;
            _resources.Metal = Mathf.Max(0, _resources.Metal - (cost?.Metal ?? 0));
            _resources.Wood = Mathf.Max(0, _resources.Wood - (cost?.Wood ?? 0));
            _resources.Fuel = Mathf.Max(0, _resources.Fuel - (cost?.Fuel ?? 0));
            _resources.Signal = Mathf.Max(0, _resources.Signal - (cost?.Signal ?? 0));
            _resources.Crystal = Mathf.Max(0, _resources.Crystal - (cost?.Crystal ?? 0));
            GameEvents.OnResourcesChanged?.Invoke(_resources);
            return true;
        }

        /// <summary>
        /// 设置前挂件类型
        /// </summary>
        /// <param name="attachment">前挂件类型</param>
        public void SetFrontAttachment(FrontAttachment attachment)
        {
            _frontAttachment = attachment;
            GameEvents.OnVehiclePartsChanged?.Invoke();
        }

        #endregion

        #region 载具部件

        /// <summary>
        /// 获取指定部件的状态
        /// </summary>
        /// <param name="part">部件ID</param>
        /// <returns>部件状态</returns>
        public PartState GetPartState(PartId part)
        {
            return _vehicleParts[(int)part];
        }

        /// <summary>
        /// 设置部件耐久度
        /// </summary>
        /// <param name="part">部件ID</param>
        /// <param name="condition">耐久度（0~1）</param>
        public void SetPartCondition(PartId part, float condition)
        {
            _vehicleParts[(int)part].Condition = Mathf.Clamp01(condition);
            GameEvents.OnVehiclePartsChanged?.Invoke();
        }

        /// <summary>
        /// 维修部件（消耗金属2 + 信号件1，恢复耐久至1.0）
        /// </summary>
        /// <param name="part">要维修的部件</param>
        /// <returns>是否维修成功</returns>
        public bool RepairPart(PartId part)
        {
            var cost = new ResourceBag { Metal = 2, Signal = 1 };
            if (!CanAfford(cost)) return false;
            if (_vehicleParts[(int)part].Condition >= 1f) return false;
            SpendResources(cost);
            _vehicleParts[(int)part].Condition = 1f;
            GameEvents.OnVehiclePartsChanged?.Invoke();
            return true;
        }

        /// <summary>
        /// 升级部件（消耗金属3 + 信号件2 + 光源晶1，等级+1）
        /// </summary>
        /// <param name="part">要升级的部件</param>
        /// <returns>是否升级成功</returns>
        public bool UpgradePart(PartId part)
        {
            var cost = new ResourceBag { Metal = 3, Signal = 2, Crystal = 1 };
            if (!CanAfford(cost)) return false;
            SpendResources(cost);
            _vehicleParts[(int)part].Level++;
            GameEvents.OnVehiclePartsChanged?.Invoke();
            return true;
        }

        #endregion

        #region 障碍物与资源拾取

        /// <summary>
        /// 清除障碍物
        /// </summary>
        /// <param name="id">障碍物ID</param>
        public void ClearObstacle(string id)
        {
            if (_clearedObstacles.Contains(id)) return;
            _clearedObstacles.Add(id);
            GameEvents.OnObstacleCleared?.Invoke(id);
        }

        /// <summary>
        /// 判断障碍物是否已清除
        /// </summary>
        /// <param name="id">障碍物ID</param>
        /// <returns>是否已清除</returns>
        public bool IsObstacleCleared(string id) => _clearedObstacles.Contains(id);

        /// <summary>
        /// 拾取资源点
        /// </summary>
        /// <param name="id">资源点ID</param>
        /// <param name="kind">资源种类</param>
        /// <param name="amount">数量</param>
        /// <returns>实际拾取数量</returns>
        public int PickupResource(string id, ResourceKind kind, int amount)
        {
            if (_pickedResources.Contains(id)) return 0;
            int space = Mathf.Max(0, _maxCarry - _resources.Total());
            int add = Mathf.Min(amount, space);
            if (add <= 0) return 0;
            _resources[kind] += add;
            _pickedResources.Add(id);
            GameEvents.OnResourcesChanged?.Invoke(_resources);
            return add;
        }

        /// <summary>
        /// 判断资源点是否已拾取
        /// </summary>
        /// <param name="id">资源点ID</param>
        /// <returns>是否已拾取</returns>
        public bool IsResourcePicked(string id) => _pickedResources.Contains(id);

        /// <summary>
        /// 设置附近资源点
        /// </summary>
        /// <param name="resource">附近资源数据，null表示无</param>
        public void SetNearbyResource(NearbyResource resource) => _nearbyResource = resource;

        #endregion

        #region 扎营

        /// <summary>
        /// 开始扎营（自动记录营地位置）
        /// </summary>
        public void StartCamp()
        {
            float x = _vehicleTransient.Position[0];
            float z = _vehicleTransient.Position[1];
            AddBuiltCamp(x, z);
            _camping = true;
            _campSite = new CampSite
            {
                X = x,
                Z = z,
                Heading = _vehicleTransient.Heading
            };
            GameEvents.OnCampingChanged?.Invoke(true);
        }

        /// <summary>
        /// 结束扎营
        /// </summary>
        public void EndCamp()
        {
            _camping = false;
            _campSite = null;
            GameEvents.OnCampingChanged?.Invoke(false);
        }

        #endregion

        #region 营地建造

        /// <summary>
        /// 建造设施
        /// </summary>
        /// <param name="siteId">选址点ID</param>
        /// <param name="facility">设施类型</param>
        /// <returns>是否建造成功</returns>
        public bool BuildFacility(string siteId, FacilityType facility)
        {
            var cost = GetFacilityCost(facility);
            if (!CanAfford(cost)) return false;

            if (_stations.TryGetValue(siteId, out var existing))
            {
                if (existing.Facilities.Contains(facility)) return false;
            }

            SpendResources(cost);

            if (!_stations.TryGetValue(siteId, out var prev))
            {
                prev = new StationState { Built = false, Facilities = new List<FacilityType>(), Level = 0 };
            }
            prev.Built = true;
            prev.Facilities.Add(facility);
            _stations[siteId] = prev;

            GameEvents.OnStationBuilt?.Invoke(siteId);
            return true;
        }

        /// <summary>
        /// 设置当前靠近的站点ID
        /// </summary>
        /// <param name="siteId">站点ID，null表示不在任何站点附近</param>
        public void SetNearbyStation(string siteId) => _nearbyStation = siteId;

        #endregion

        #region 已建营地

        /// <summary>
        /// 添加已建营地（距离5m内自动去重）
        /// </summary>
        /// <param name="x">世界坐标X</param>
        /// <param name="z">世界坐标Z</param>
        public void AddBuiltCamp(float x, float z)
        {
            bool isDuplicate = _builtCamps.Exists(c =>
                Mathf.Sqrt((c.X - x) * (c.X - x) + (c.Z - z) * (c.Z - z)) < 5f);
            if (isDuplicate) return;

            float routeZ = World.TerrainHeight.WorldToRouteZ(z);
            _builtCamps.Add(new BuiltCamp { X = x, Z = z, RouteZ = routeZ });
            GameEvents.OnCampBuilt?.Invoke(_builtCamps.Count);
        }

        #endregion

        #region 旅程通知

        /// <summary>
        /// 设置旅程通知
        /// </summary>
        /// <param name="from">起始旅程</param>
        /// <param name="to">目标旅程</param>
        /// <param name="message">通知消息</param>
        public void SetJourneyNotification(int from, int to, string message)
        {
            _journeyNotification = new JourneyNotification { From = from, To = to, Message = message };
        }

        /// <summary>
        /// 清除旅程通知
        /// </summary>
        public void ClearJourneyNotification() => _journeyNotification = null;

        #endregion

        #region 小镇

        /// <summary>
        /// 设置是否已访问小镇
        /// </summary>
        /// <param name="visited">是否已访问</param>
        public void SetTownVisited(bool visited) => TownVisited = visited;

        /// <summary>
        /// 设置当前靠近的商店
        /// </summary>
        /// <param name="id">商店ID，null表示不在商店附近</param>
        public void SetNearbyShop(string id) => _nearbyShop = id;

        /// <summary>
        /// 添加已购买物品
        /// </summary>
        /// <param name="id">物品ID</param>
        public void AddPurchasedItem(string id)
        {
            if (_purchasedItems.Contains(id)) return;
            _purchasedItems.Add(id);
        }

        #endregion

        #region 成就

        /// <summary>
        /// 解锁成就
        /// </summary>
        /// <param name="id">成就ID</param>
        public void UnlockAchievement(string id)
        {
            int idx = _achievements.FindIndex(a => a.Id == id);
            if (idx == -1 || _achievements[idx].Unlocked) return;
            _achievements[idx].Unlocked = true;
            _achievements[idx].UnlockedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _achievementNotification = _achievements[idx];
            GameEvents.OnAchievementUnlocked?.Invoke(_achievements[idx]);
        }

        /// <summary>
        /// 清除成就通知
        /// </summary>
        public void ClearAchievementNotification() => _achievementNotification = null;

        #endregion

        #region 叙事与日志

        /// <summary>
        /// 添加日志条目（按fragmentId去重）
        /// </summary>
        /// <param name="entry">日志条目</param>
        public void AddJournalEntry(JournalEntry entry)
        {
            if (_journal.Exists(e => e.FragmentId == entry.FragmentId)) return;
            _journal.Add(entry);
        }

        /// <summary>
        /// 设置附近可交互对象
        /// </summary>
        /// <param name="interactable">可交互对象，null表示无</param>
        public void SetNearbyInteractable(NearbyInteractable interactable) => _nearbyInteractable = interactable;

        /// <summary>
        /// 设置叙事覆盖层可见性
        /// </summary>
        /// <param name="visible">是否可见</param>
        public void SetNarrativeOverlayVisible(bool visible) => _narrativeOverlayVisible = visible;

        /// <summary>
        /// 设置当前叙事内容
        /// </summary>
        /// <param name="content">叙事内容，null表示无</param>
        public void SetCurrentNarrativeContent(NarrativeContent content) => _currentNarrativeContent = content;

        /// <summary>
        /// 设置日志覆盖层可见性
        /// </summary>
        /// <param name="visible">是否可见</param>
        public void SetJournalOverlayVisible(bool visible) => _journalOverlayVisible = visible;

        /// <summary>
        /// 发现碎片
        /// </summary>
        /// <param name="fragmentId">碎片ID</param>
        public void DiscoverFragment(string fragmentId)
        {
            if (_discoveredFragmentIds.Contains(fragmentId)) return;
            _discoveredFragmentIds.Add(fragmentId);
            GameEvents.OnFragmentDiscovered?.Invoke(fragmentId);
        }

        /// <summary>
        /// 判断碎片是否已发现
        /// </summary>
        /// <param name="fragmentId">碎片ID</param>
        /// <returns>是否已发现</returns>
        public bool IsFragmentDiscovered(string fragmentId) => _discoveredFragmentIds.Contains(fragmentId);

        #endregion

        #region 开发者功能

        /// <summary>
        /// 请求开发者传送
        /// </summary>
        /// <param name="x">目标X</param>
        /// <param name="z">目标Z</param>
        public void RequestDevTeleport(float x, float z)
        {
            _devTeleportTarget.Serial++;
            _devTeleportTarget.X = x;
            _devTeleportTarget.Z = z;
        }

        /// <summary>
        /// 请求传送至提灯村
        /// </summary>
        public void RequestTeleportToLanternVillage() => _teleportToLanternVillageSerial++;

        /// <summary>
        /// 设置性能统计数据
        /// </summary>
        /// <param name="stats">性能数据</param>
        public void SetPerfStats(PerfStats stats) => _perfStats = stats;

        #endregion

        #region 载具瞬态

        /// <summary>
        /// 重置载具瞬态数据
        /// </summary>
        /// <param name="posX">位置X</param>
        /// <param name="posZ">位置Z</param>
        /// <param name="heading">朝向</param>
        public void ResetVehicleTransientState(float posX = 0f, float posZ = 270f, float heading = Mathf.PI)
        {
            _vehicleTransient.Position[0] = posX;
            _vehicleTransient.Position[1] = posZ;
            _vehicleTransient.Heading = heading;
            _vehicleTransient.Speed = 0f;
            _vehicleTransient.SteerRate = 0f;
            _vehicleTransient.SearchlightIntensity = 0f;
            _vehicleTransient.FullIlluminationIntensity = 0f;
        }

        #endregion

        #region 设施消耗查询

        /// <summary>
        /// 获取指定设施类型的建造消耗
        /// </summary>
        /// <param name="facility">设施类型</param>
        /// <returns>资源消耗</returns>
        public static ResourceBag GetFacilityCost(FacilityType facility) => facility switch
        {
            FacilityType.Supply => new ResourceBag { Metal = 3, Wood = 2 },
            FacilityType.Shelter => new ResourceBag { Metal = 2, Wood = 4 },
            FacilityType.SignalTower => new ResourceBag { Metal = 2, Signal = 2 },
            FacilityType.Beacon => new ResourceBag { Metal = 3, Crystal = 1, Wood = 2 },
            FacilityType.Observatory => new ResourceBag { Metal = 2, Wood = 3, Signal = 1 },
            FacilityType.Bridge => new ResourceBag { Metal = 4, Wood = 3 },
            _ => new ResourceBag()
        };

        #endregion

        #region 存档序列化辅助

        /// <summary>
        /// 从当前状态生成存档数据
        /// </summary>
        /// <returns>存档数据</returns>
        public SaveData ToSaveData()
        {
            var data = new SaveData
            {
                Resources = _resources.Clone(),
                MaxCarry = _maxCarry,
                FrontAttachment = _frontAttachment,
                VehicleParts = new PartState[_vehicleParts.Length],
                ClearedObstacles = new List<string>(_clearedObstacles),
                PickedResources = new List<string>(_pickedResources),
                CurrentJourney = _currentJourney,
                BuiltCamps = new List<BuiltCamp>(_builtCamps),
                Achievements = new List<Achievement>(_achievements),
                PurchasedItems = new List<string>(_purchasedItems),
                DiscoveredFragmentIds = new List<string>(_discoveredFragmentIds),
                Journal = new List<JournalEntry>(_journal),
                CurrentChapter = _currentChapter,
                TownVisited = _townVisited,
                Stations = new List<StationEntry>()
            };

            for (int i = 0; i < _vehicleParts.Length; i++)
            {
                data.VehicleParts[i] = new PartState
                {
                    Condition = _vehicleParts[i].Condition,
                    Level = _vehicleParts[i].Level
                };
            }

            foreach (var kvp in _stations)
            {
                data.Stations.Add(new StationEntry
                {
                    SiteId = kvp.Key,
                    State = new StationState
                    {
                        Built = kvp.Value.Built,
                        Level = kvp.Value.Level,
                        Facilities = new List<FacilityType>(kvp.Value.Facilities)
                    }
                });
            }

            return data;
        }

        /// <summary>
        /// 从存档数据恢复状态
        /// </summary>
        /// <param name="data">存档数据</param>
        public void FromSaveData(SaveData data)
        {
            _resources = data.Resources?.Clone() ?? InitialResources.Clone();
            _maxCarry = data.MaxCarry > 0 ? data.MaxCarry : DefaultMaxCarry;
            _frontAttachment = data.FrontAttachment;
            _vehicleParts = data.VehicleParts ?? CreateInitialParts();
            _clearedObstacles = data.ClearedObstacles ?? new List<string>();
            _pickedResources = data.PickedResources ?? new List<string>();
            _currentJourney = data.CurrentJourney > 0 ? data.CurrentJourney : DefaultCurrentJourney;
            _builtCamps = data.BuiltCamps ?? new List<BuiltCamp>();
            _achievements = data.Achievements ?? new List<Achievement>();
            _purchasedItems = data.PurchasedItems ?? new List<string>();
            _discoveredFragmentIds = data.DiscoveredFragmentIds ?? new List<string>();
            _journal = data.Journal ?? new List<JournalEntry>();
            _currentChapter = !string.IsNullOrEmpty(data.CurrentChapter) ? data.CurrentChapter : DefaultCurrentChapter;
            _townVisited = data.TownVisited;

            // 恢复站点字典
            _stations.Clear();
            if (data.Stations != null)
            {
                foreach (var entry in data.Stations)
                {
                    _stations[entry.SiteId] = entry.State;
                }
            }

            // 清除运行时状态
            _nearbyResource = null;
            _nearbyStation = null;
            _nearbyShop = null;
            _journeyNotification = null;
            _achievementNotification = null;

            GameEvents.OnResourcesChanged?.Invoke(_resources);
            GameEvents.OnVehiclePartsChanged?.Invoke();
            GameEvents.OnGamePhaseChanged?.Invoke(_currentJourney);
        }

        #endregion
    }
}

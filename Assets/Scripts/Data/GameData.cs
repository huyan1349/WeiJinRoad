// =============================================================================
// GameData.cs — 未尽之路 全局静态数据定义
// 由 TypeScript 数据文件翻译而来，包含所有游戏数据
// =============================================================================

using System;
using UnityEngine;

namespace WeiJinRoad.Data
{
    // =========================================================================
    // 枚举定义
    // =========================================================================

    /// <summary>
    /// 资源种类：金属、木材、燃料、信号件、光源晶
    /// </summary>
    public enum ResourceKind
    {
        Metal,
        Wood,
        Fuel,
        Signal,
        Crystal
    }

    /// <summary>
    /// 资源点类型：残骸、木材堆、油桶、设备、光源晶
    /// </summary>
    public enum NodeType
    {
        Wreck,
        Logpile,
        FuelDrum,
        Device,
        Crystal
    }

    /// <summary>
    /// 障碍物类型：雪堆、落石、倒木、冰块
    /// </summary>
    public enum ObstacleKind
    {
        SnowDrift,
        Rockfall,
        FallenLog,
        IceBlock
    }

    /// <summary>
    /// 设施类型：补给仓、避风棚、信号塔、灯塔、观测台、简易桥
    /// </summary>
    public enum FacilityType
    {
        Supply,
        Shelter,
        SignalTower,
        Beacon,
        Observatory,
        Bridge
    }

    /// <summary>
    /// 成就分类：旅途、营地、探索、载具、收集
    /// </summary>
    public enum AchievementCategory
    {
        Journey,
        Camp,
        Explore,
        Vehicle,
        Collect
    }

    /// <summary>
    /// 店铺类型：车库、补给站、交易所、酒馆、信号站、加油站
    /// </summary>
    public enum ShopType
    {
        Garage,
        Supply,
        Trade,
        Tavern,
        Signal,
        Fuel
    }

    /// <summary>
    /// 小镇商品类型：升级、资源、情报、修理
    /// </summary>
    public enum TownItemType
    {
        Upgrade,
        Resource,
        Info,
        Repair
    }

    // =========================================================================
    // 数据类定义
    // =========================================================================

    /// <summary>
    /// 章节定义
    /// </summary>
    [Serializable]
    public class ChapterDef
    {
        public string id;
        public string name;
        public string biome;
        public string theme;
    }

    /// <summary>
    /// 故事碎片
    /// </summary>
    [Serializable]
    public class StoryFragment
    {
        public string id;
        public string chapter;
        public string biome;
        public string carrierType;
        public string title;
        public string content;
        public string hint;
        public int order;
    }

    /// <summary>
    /// 营地选址定义
    /// </summary>
    [Serializable]
    public class StationSiteDef
    {
        public string id;
        public float routeZ;
        public float lateralOffset;
        public string name;
        public string biome;
        public FacilityType[] availableFacilities;
    }

    /// <summary>
    /// 设施建造消耗
    /// </summary>
    [Serializable]
    public class FacilityCost
    {
        public int metal;
        public int wood;
        public int fuel;
        public int signal;
        public int crystal;
    }

    /// <summary>
    /// 设施元数据（名称、描述、图标）
    /// </summary>
    [Serializable]
    public class FacilityMeta
    {
        public string label;
        public string desc;
        public string icon;
    }

    /// <summary>
    /// 资源点定义
    /// </summary>
    [Serializable]
    public class ResourceNodeDef
    {
        public string id;
        public NodeType nodeType;
        public ResourceKind kind;
        public Vector3 position;
        public int amount;
        public float rotationY;
    }

    /// <summary>
    /// 障碍物定义
    /// </summary>
    [Serializable]
    public class ObstacleDef
    {
        public string id;
        public ObstacleKind kind;
        public Vector3 position;
        public float rotationY;
        public float scale;
        public float hp;
        public float radius;
        public bool blocking;
    }

    /// <summary>
    /// 障碍物类型属性
    /// </summary>
    [Serializable]
    public class ObstacleKindProps
    {
        public float hp;
        public float radius;
        public bool blocking;
    }

    /// <summary>
    /// 成就定义
    /// </summary>
    [Serializable]
    public class AchievementDef
    {
        public string id;
        public string name;
        public string desc;
        public string icon;
        public AchievementCategory category;
        public bool hidden;
    }

    /// <summary>
    /// 成就分类元数据
    /// </summary>
    [Serializable]
    public class AchievementCategoryMeta
    {
        public string label;
        public string icon;
    }

    /// <summary>
    /// 店铺定义
    /// </summary>
    [Serializable]
    public class ShopDef
    {
        public string id;
        public string name;
        public ShopType type;
        public string desc;
        public Vector3 position;
        public float rotation;
        public Vector3 size;
        public string roofColor;
        public string wallColor;
        public string signText;
        public string lightColor;
        public string ownerLine;
    }

    /// <summary>
    /// 小镇商品
    /// </summary>
    [Serializable]
    public class TownItem
    {
        public string id;
        public string name;
        public string desc;
        public TownItemType type;
        public FacilityCost cost;
        public string effect;
        public ShopType shopType;
        public bool unique;
    }

    // =========================================================================
    // 静态数据表
    // =========================================================================

    /// <summary>
    /// 全局游戏数据 — 所有静态数据的入口
    /// </summary>
    public static class GameData
    {
        // ── 章节 ──────────────────────────────────────────────────────────

        /// <summary>章节列表（4章）</summary>
        public static readonly ChapterDef[] Chapters = new ChapterDef[]
        {
            new ChapterDef { id = "ch1", name = "出发", biome = "B04", theme = "遗忘平原上的第一道车辙" },
            new ChapterDef { id = "ch2", name = "记忆", biome = "B02", theme = "落叶掩埋了谁的名字" },
            new ChapterDef { id = "ch3", name = "发现", biome = "B03", theme = "裂谷深处藏着答案" },
            new ChapterDef { id = "ch4", name = "对峙", biome = "B01", theme = "风雪尽头，路还在吗" },
        };

        // ── 故事碎片 ──────────────────────────────────────────────────────

        /// <summary>故事碎片列表（40条）</summary>
        public static readonly StoryFragment[] StoryFragments = new StoryFragment[]
        {
            // ── 第一章：出发 (B04 平原) ──
            new StoryFragment { id = "ch1_01", chapter = "ch1", biome = "B04", carrierType = "stone_layout", title = "起点", content = "路从这里开始，但没有人告诉我它在哪里结束。", hint = "地面的石块排成了字", order = 1 },
            new StoryFragment { id = "ch1_02", chapter = "ch1", biome = "B04", carrierType = "sign_text", title = "编号", content = "工程编号 R-0117。勘测员：林。任务：完成全线地形测绘。我接下这份工作的时候，以为只是走路。", hint = "一块歪斜的路牌，字迹尚新", order = 2 },
            new StoryFragment { id = "ch1_03", chapter = "ch1", biome = "B04", carrierType = "stone_layout", title = "车辙", content = "平原上只有我一辆车的痕迹。但风一吹，什么都不剩。", hint = "碎石排列成一条弧线", order = 3 },
            new StoryFragment { id = "ch1_04", chapter = "ch1", biome = "B04", carrierType = "sign_text", title = "方向", content = "第三天。罗盘正常，地图正常，可我总觉得方向不对。不是路的方向——是我的。", hint = "路牌背面潦草地写着字", order = 4 },
            new StoryFragment { id = "ch1_05", chapter = "ch1", biome = "B04", carrierType = "hillside_relief", title = "标记", content = "每公里我刻一个记号。不是为后来的人——是怕自己忘了来路。", hint = "土坡上有浅浅的凹痕", order = 5 },
            new StoryFragment { id = "ch1_06", chapter = "ch1", biome = "B04", carrierType = "journal_page", title = "平原", content = "平原太安静了。安静到能听见自己的心跳。第五天，我开始跟自己说话。第六天，我听到了回答。", hint = "一张被风吹开的纸页", order = 6 },
            new StoryFragment { id = "ch1_07", chapter = "ch1", biome = "B04", carrierType = "standing_stones", title = "前人", content = "在第七公里的路桩下，我挖出了一段旧桩。木头上刻着别人的名字，已经被风雨磨平了。", hint = "几块竖立的石柱上隐约有字", order = 7 },
            new StoryFragment { id = "ch1_08", chapter = "ch1", biome = "B04", carrierType = "sign_text", title = "通知", content = "电台已经三天收不到信号了。基地最后的回复是：继续前进，按原计划执行。", hint = "路牌上贴着一张褪色的通知", order = 8 },
            new StoryFragment { id = "ch1_09", chapter = "ch1", biome = "B04", carrierType = "journal_page", title = "影子", content = "第十天。我开始分不清车辙是自己的还是别人的。也许从来就只有一条路，所有人都在上面走。", hint = "纸页夹在路边的碎石间", order = 9 },
            new StoryFragment { id = "ch1_10", chapter = "ch1", biome = "B04", carrierType = "stone_layout", title = "告别", content = "平原的尽头是树。我回头看了一眼——来时的路已经消失了。好像它从来就不存在。", hint = "地面的石块排成一行，指向远方", order = 10 },

            // ── 第二章：记忆 (B02 森林) ──
            new StoryFragment { id = "ch2_01", chapter = "ch2", biome = "B02", carrierType = "journal_page", title = "落叶", content = "第十四天。树叶落尽的时候，我终于承认——我在绕圈。路标编号在重复。", hint = "一张被风吹开的纸页", order = 11 },
            new StoryFragment { id = "ch2_02", chapter = "ch2", biome = "B02", carrierType = "cliff_carving", title = "名字", content = "我把名字刻在这里。不是为了纪念，是怕自己忘了自己叫什么。", hint = "崖壁上有浅浅的刻痕", order = 12 },
            new StoryFragment { id = "ch2_03", chapter = "ch2", biome = "B02", carrierType = "journal_page", title = "原因", content = "有人问我为什么接这个项目。我说为了钱。其实是因为我想知道，一条没有尽头的路会通向哪里。", hint = "纸页被苔藓半掩着", order = 13 },
            new StoryFragment { id = "ch2_04", chapter = "ch2", biome = "B02", carrierType = "standing_stones", title = "旧路", content = "林子里有另一条路。更老，更窄，已经被树根吞了一半。它和我的路平行，但方向相反。", hint = "几块巨石上刻着字", order = 14 },
            new StoryFragment { id = "ch2_05", chapter = "ch2", biome = "B02", carrierType = "ice_text", title = "冻结", content = "第二十一天。气温骤降。水壶里的水一夜之间冻成了冰，冰面上映出我不认识的脸。", hint = "冰面上隐约有字迹", order = 15 },
            new StoryFragment { id = "ch2_06", chapter = "ch2", biome = "B02", carrierType = "journal_page", title = "家", content = "我试过回去。走了三天，路标编号在递减。但风景没有变。好像这片林子只有一种样子，无论往哪走都一样。", hint = "纸页钉在树干上", order = 16 },
            new StoryFragment { id = "ch2_07", chapter = "ch2", biome = "B02", carrierType = "hillside_relief", title = "勘测", content = "勘测记录：地质结构异常。岩层走向与地图标注不符。不是地图错了——是地在动。", hint = "坡面上有刻出的文字", order = 17 },
            new StoryFragment { id = "ch2_08", chapter = "ch2", biome = "B02", carrierType = "cliff_carving", title = "声音", content = "夜里我听到了声音。不是风，不是动物。是某种低频的震动，从地底传来，像心跳。", hint = "崖壁上有深深的凿痕", order = 18 },
            new StoryFragment { id = "ch2_09", chapter = "ch2", biome = "B02", carrierType = "journal_page", title = "选择", content = "第二十八天。我站在岔路口。一条往回，一条往前。我选了往前。不是因为勇敢，是因为回去的路已经不在了。", hint = "一张揉皱的纸页", order = 19 },
            new StoryFragment { id = "ch2_10", chapter = "ch2", biome = "B02", carrierType = "standing_stones", title = "渡口", content = "林子到了尽头。前方是裂谷。我站在边缘往下看，看不见底。但那里有光。", hint = "巨石上刻着字，石缝间长满了苔", order = 20 },

            // ── 第三章：发现 (B03 裂谷) ──
            new StoryFragment { id = "ch3_01", chapter = "ch3", biome = "B03", carrierType = "cliff_carving", title = "裂谷", content = "他们修这条路不是为了让人走出去，是为了让什么东西进来。", hint = "崖壁上有深深的凿痕", order = 21 },
            new StoryFragment { id = "ch3_02", chapter = "ch3", biome = "B03", carrierType = "journal_page", title = "图纸", content = "第三十一天。我找到了工程图纸的副本，藏在路桩里。图上标注的终点，和指挥部告诉我的不一样。", hint = "纸页被压在碎石下", order = 22 },
            new StoryFragment { id = "ch3_03", chapter = "ch3", biome = "B03", carrierType = "permafrost_crack", title = "永冻", content = "裂谷壁上有永冻层。冻层里有东西——不是化石，不是岩石。是某种结构。太规则了，不可能是自然形成的。", hint = "冻土裂缝中隐约有字", order = 23 },
            new StoryFragment { id = "ch3_04", chapter = "ch3", biome = "B03", carrierType = "cliff_carving", title = "警告", content = "如果你在勘测途中发现异常地质结构，立即停止前进，原地待命。这是规程第十七条。但没有人来接我。", hint = "崖壁上刻着规整的字", order = 24 },
            new StoryFragment { id = "ch3_05", chapter = "ch3", biome = "B03", carrierType = "journal_page", title = "信号", content = "第三十五天。电台突然恢复了。只有一段录音，循环播放：\"不要到达终点。不要到达终点。不要到达终点。\"", hint = "纸页被冰晶覆盖", order = 25 },
            new StoryFragment { id = "ch3_06", chapter = "ch3", biome = "B03", carrierType = "hillside_relief", title = "前勘", content = "我在谷壁上发现了前一批勘测员的记录。只有一行字：路是对的，方向是错的。退不回去了。", hint = "坡面上刻着潦草的文字", order = 26 },
            new StoryFragment { id = "ch3_07", chapter = "ch3", biome = "B03", carrierType = "permafrost_crack", title = "深处", content = "越往下走，那种低频震动越强。不是从地底传来的——是从路的前方传来的。路在呼唤什么东西。", hint = "冻土裂缝中有模糊的字迹", order = 27 },
            new StoryFragment { id = "ch3_08", chapter = "ch3", biome = "B03", carrierType = "cliff_carving", title = "真相", content = "我终于明白了。这条路不是交通线，是引线。他们需要一个人走完全程，激活终点。那个人就是我。", hint = "崖壁上有用力刻出的字", order = 28 },
            new StoryFragment { id = "ch3_09", chapter = "ch3", biome = "B03", carrierType = "journal_page", title = "决定", content = "第三十九天。我可以停下来。我可以不走完。但如果不走完，就永远没有人知道这条路通向哪里。", hint = "纸页被压在石头下", order = 29 },
            new StoryFragment { id = "ch3_10", chapter = "ch3", biome = "B03", carrierType = "standing_stones", title = "上升", content = "裂谷开始收窄，路开始上升。我知道高地就在上面。最后一程了。", hint = "几块巨石上刻着字，石面覆着薄冰", order = 30 },

            // ── 第四章：对峙 (B01 山脊) ──
            new StoryFragment { id = "ch4_01", chapter = "ch4", biome = "B01", carrierType = "ice_text", title = "高地", content = "第四十一天。风雪太大，能见度不足五米。但路还在。它一直在。", hint = "冰面上凝着字迹", order = 31 },
            new StoryFragment { id = "ch4_02", chapter = "ch4", biome = "B01", carrierType = "cliff_carving", title = "终点", content = "路标编号到了最后一位。前方没有编号了。前方没有路了。但脚下的地面在震动。", hint = "崖壁上刻着最后的编号", order = 32 },
            new StoryFragment { id = "ch4_03", chapter = "ch4", biome = "B01", carrierType = "journal_page", title = "记录", content = "勘测记录，最终条目。路的终点不是某个地方——是一个状态。走完的人，就是终点本身。", hint = "纸页被冻在冰里", order = 33 },
            new StoryFragment { id = "ch4_04", chapter = "ch4", biome = "B01", carrierType = "standing_stones", title = "前人", content = "我在终点的石碑上看到了所有前任勘测员的名字。他们没有消失。他们变成了路的一部分。", hint = "几块巨石上刻满了名字", order = 34 },
            new StoryFragment { id = "ch4_05", chapter = "ch4", biome = "B01", carrierType = "permafrost_crack", title = "震动", content = "地面裂开了。裂缝下面不是黑暗——是光。温暖的、脉动的光，像某种巨大的心脏正在苏醒。", hint = "冻土裂缝中透出微光", order = 35 },
            new StoryFragment { id = "ch4_06", chapter = "ch4", biome = "B01", carrierType = "cliff_carving", title = "选择", content = "我可以停下来。路会重新沉睡，一切恢复原状。但那些名字——那些走在我前面的人——他们将永远困在路上。", hint = "崖壁上刻着颤抖的字", order = 36 },
            new StoryFragment { id = "ch4_07", chapter = "ch4", biome = "B01", carrierType = "journal_page", title = "告别", content = "如果你读到这里，说明你也走上了这条路。不要为我难过。我只是走到了该去的地方。", hint = "纸页被风吹到你脚边", order = 37 },
            new StoryFragment { id = "ch4_08", chapter = "ch4", biome = "B01", carrierType = "standing_stones", title = "托付", content = "如果你读到这里，请替我看看路的尽头。然后替我决定——这条路，该继续，还是该终结。", hint = "几块巨石上刻着字", order = 38 },
            new StoryFragment { id = "ch4_09", chapter = "ch4", biome = "B01", carrierType = "ice_text", title = "最后", content = "路没有尽头。但走路的人可以选择停下来。这是我最后学会的事。", hint = "冰面上凝着最后的字迹", order = 39 },
            new StoryFragment { id = "ch4_10", chapter = "ch4", biome = "B01", carrierType = "stone_layout", title = "未尽", content = "路未尽。我亦未尽。后来的人啊——这条路的结局，交给你了。", hint = "地面的石块排成了字，风雪中依稀可辨", order = 40 },
        };

        // ── 营地选址 ──────────────────────────────────────────────────────

        /// <summary>营地选址列表（9个）</summary>
        public static readonly StationSiteDef[] StationSites = new StationSiteDef[]
        {
            // ── 平原 (routeZ > 180) ──
            new StationSiteDef { id = "station_plains_1", routeZ = 400f, lateralOffset = -12f, name = "废弃加油站", biome = "平原", availableFacilities = new FacilityType[] { FacilityType.Supply, FacilityType.Shelter } },
            new StationSiteDef { id = "station_plains_2", routeZ = 260f, lateralOffset = 10f, name = "公路服务区", biome = "平原", availableFacilities = new FacilityType[] { FacilityType.Supply, FacilityType.Shelter, FacilityType.SignalTower } },

            // ── 森林 (routeZ 0~180) ──
            new StationSiteDef { id = "station_forest_1", routeZ = 140f, lateralOffset = -14f, name = "林间空地", biome = "森林", availableFacilities = new FacilityType[] { FacilityType.Shelter, FacilityType.Observatory } },
            new StationSiteDef { id = "station_forest_2", routeZ = 40f, lateralOffset = 11f, name = "湖畔营地", biome = "森林", availableFacilities = new FacilityType[] { FacilityType.Supply, FacilityType.Shelter, FacilityType.Bridge } },

            // ── 裂谷 (routeZ -240~0) ──
            new StationSiteDef { id = "station_rift_1", routeZ = -80f, lateralOffset = -10f, name = "断崖哨站", biome = "裂谷", availableFacilities = new FacilityType[] { FacilityType.SignalTower, FacilityType.Observatory, FacilityType.Bridge } },
            new StationSiteDef { id = "station_rift_2", routeZ = -200f, lateralOffset = 8f, name = "深谷驿站", biome = "裂谷", availableFacilities = new FacilityType[] { FacilityType.Shelter, FacilityType.Bridge, FacilityType.Supply } },

            // ── 山脊 (routeZ < -240) ──
            new StationSiteDef { id = "station_ridge_1", routeZ = -320f, lateralOffset = -9f, name = "风雪前哨", biome = "山脊", availableFacilities = new FacilityType[] { FacilityType.Beacon, FacilityType.SignalTower, FacilityType.Shelter } },
            new StationSiteDef { id = "station_ridge_2", routeZ = -480f, lateralOffset = 7f, name = "冰封营地", biome = "山脊", availableFacilities = new FacilityType[] { FacilityType.Supply, FacilityType.Shelter, FacilityType.Beacon } },
            new StationSiteDef { id = "station_ridge_3", routeZ = -620f, lateralOffset = -8f, name = "山巅灯塔", biome = "山脊", availableFacilities = new FacilityType[] { FacilityType.Beacon, FacilityType.Observatory, FacilityType.SignalTower } },
        };

        // ── 设施建造消耗 ──────────────────────────────────────────────────

        /// <summary>每种设施所需资源</summary>
        public static readonly FacilityCost[] FacilityCosts = new FacilityCost[]
        {
            new FacilityCost { metal = 3, wood = 2 },                                    // Supply
            new FacilityCost { metal = 2, wood = 4 },                                    // Shelter
            new FacilityCost { metal = 2, signal = 2 },                                  // SignalTower
            new FacilityCost { metal = 3, crystal = 1, wood = 2 },                       // Beacon
            new FacilityCost { metal = 2, wood = 3, signal = 1 },                        // Observatory
            new FacilityCost { metal = 4, wood = 3 },                                    // Bridge
        };

        /// <summary>按设施类型获取建造消耗</summary>
        public static FacilityCost GetFacilityCost(FacilityType type)
        {
            return FacilityCosts[(int)type];
        }

        /// <summary>设施中文名与描述</summary>
        public static readonly FacilityMeta[] FacilityMetas = new FacilityMeta[]
        {
            new FacilityMeta { label = "补给仓", desc = "储存沿途收集的物资", icon = "⬢" },       // Supply
            new FacilityMeta { label = "避风棚", desc = "遮风挡雪的简易庇护所", icon = "⌂" },     // Shelter
            new FacilityMeta { label = "信号塔", desc = "发射信号，扩大通讯范围", icon = "⬡" },   // SignalTower
            new FacilityMeta { label = "灯塔",   desc = "强光指引，暴风雪中可见", icon = "◈" },   // Beacon
            new FacilityMeta { label = "观测台", desc = "高处观测地形与天气", icon = "◉" },       // Observatory
            new FacilityMeta { label = "简易桥", desc = "跨越裂隙与沟壑", icon = "═" },           // Bridge
        };

        /// <summary>按设施类型获取元数据</summary>
        public static FacilityMeta GetFacilityMeta(FacilityType type)
        {
            return FacilityMetas[(int)type];
        }

        // ── 资源点 ────────────────────────────────────────────────────────

        /// <summary>资源点类型 → 资源种类映射</summary>
        public static ResourceKind NodeToKind(NodeType type)
        {
            switch (type)
            {
                case NodeType.Wreck:    return ResourceKind.Metal;
                case NodeType.Logpile:  return ResourceKind.Wood;
                case NodeType.FuelDrum: return ResourceKind.Fuel;
                case NodeType.Device:   return ResourceKind.Signal;
                case NodeType.Crystal:  return ResourceKind.Crystal;
                default:                return ResourceKind.Metal;
            }
        }

        /// <summary>资源点生成参数</summary>
        public const float ResourceRouteStart = 460f;
        public const float ResourceRouteEnd = -700f;
        public const float ResourceStep = 38f;
        public const uint ResourceSeed = 0xb00b1e;
        public const float ResourceSpawnThreshold = 0.78f;

        // ── 障碍物 ────────────────────────────────────────────────────────

        /// <summary>各障碍物类型属性</summary>
        public static readonly ObstacleKindProps[] ObstacleKindPropsList = new ObstacleKindProps[]
        {
            new ObstacleKindProps { hp = 1.0f, radius = 2.5f, blocking = false },  // SnowDrift
            new ObstacleKindProps { hp = 4.0f, radius = 1.9f, blocking = true },   // Rockfall
            new ObstacleKindProps { hp = 3.2f, radius = 2.2f, blocking = true },   // FallenLog
            new ObstacleKindProps { hp = 2.2f, radius = 1.8f, blocking = true },   // IceBlock
        };

        /// <summary>按障碍物类型获取属性</summary>
        public static ObstacleKindProps GetObstacleKindProps(ObstacleKind kind)
        {
            return ObstacleKindPropsList[(int)kind];
        }

        /// <summary>障碍物生成参数</summary>
        public const float ObstacleRouteStart = 470f;
        public const float ObstacleRouteEnd = -700f;
        public const float ObstacleClusterStep = 30f;
        public const uint ObstacleSeed = 0x5eed01;

        // ── 成就 ──────────────────────────────────────────────────────────

        /// <summary>成就分类元数据</summary>
        public static readonly AchievementCategoryMeta[] AchievementCategoryMetas = new AchievementCategoryMeta[]
        {
            new AchievementCategoryMeta { label = "旅途", icon = "🧭" },  // Journey
            new AchievementCategoryMeta { label = "营地", icon = "🏕" },  // Camp
            new AchievementCategoryMeta { label = "探索", icon = "🔍" },  // Explore
            new AchievementCategoryMeta { label = "载具", icon = "🚗" },  // Vehicle
            new AchievementCategoryMeta { label = "收集", icon = "📦" },  // Collect
        };

        /// <summary>成就定义列表（29项）</summary>
        public static readonly AchievementDef[] Achievements = new AchievementDef[]
        {
            // ─── 旅途 ───
            new AchievementDef { id = "first_drive",    name = "启程",         desc = "第一次启动车辆",         icon = "🚗", category = AchievementCategory.Journey, hidden = false },
            new AchievementDef { id = "reach_forest",   name = "林间穿行",     desc = "到达森林路段",           icon = "🌲", category = AchievementCategory.Journey, hidden = false },
            new AchievementDef { id = "reach_valley",   name = "裂谷深处",     desc = "到达裂谷路段",           icon = "🏔", category = AchievementCategory.Journey, hidden = false },
            new AchievementDef { id = "reach_mountain", name = "山巅之上",     desc = "到达山路段",             icon = "⛰", category = AchievementCategory.Journey, hidden = false },
            new AchievementDef { id = "reach_summit",   name = "未尽之路的终点", desc = "到达山顶",             icon = "🌅", category = AchievementCategory.Journey, hidden = true },
            new AchievementDef { id = "journey_5",      name = "全线通行",     desc = "解锁第5旅程",           icon = "🗺", category = AchievementCategory.Journey, hidden = false },

            // ─── 营地 ───
            new AchievementDef { id = "first_camp",        name = "首次扎营", desc = "第一次扎营休息",           icon = "🏕", category = AchievementCategory.Camp, hidden = false },
            new AchievementDef { id = "camp_5",            name = "老练旅人", desc = "累计扎营5次",             icon = "⛺", category = AchievementCategory.Camp, hidden = false },
            new AchievementDef { id = "camp_10",           name = "以路为家", desc = "累计扎营10次",            icon = "🏠", category = AchievementCategory.Camp, hidden = false },
            new AchievementDef { id = "campfire_master",   name = "篝火大师", desc = "篝火维护达到90%最佳区间", icon = "🔥", category = AchievementCategory.Camp, hidden = true },
            new AchievementDef { id = "radio_lock",        name = "信号猎人", desc = "锁定第一个电台信号",       icon = "📻", category = AchievementCategory.Camp, hidden = false },
            new AchievementDef { id = "stargazer",         name = "观星者",   desc = "发现第一个星座",           icon = "⭐", category = AchievementCategory.Camp, hidden = false },
            new AchievementDef { id = "all_constellations", name = "天文学家", desc = "发现全部4个星座",         icon = "🌌", category = AchievementCategory.Camp, hidden = true },
            new AchievementDef { id = "first_cook",        name = "野外厨师", desc = "第一次烹饪",               icon = "🍲", category = AchievementCategory.Camp, hidden = false },

            // ─── 探索 ───
            new AchievementDef { id = "first_obstacle", name = "开路先锋",   desc = "清除第一个路障",     icon = "🚧", category = AchievementCategory.Explore, hidden = false },
            new AchievementDef { id = "obstacle_20",   name = "道路清道夫", desc = "清除20个路障",       icon = "🛤", category = AchievementCategory.Explore, hidden = false },
            new AchievementDef { id = "obstacle_50",   name = "无畏开拓者", desc = "清除50个路障",       icon = "⚡", category = AchievementCategory.Explore, hidden = true },
            new AchievementDef { id = "first_resource", name = "拾荒者",    desc = "拾取第一个资源",     icon = "📦", category = AchievementCategory.Explore, hidden = false },
            new AchievementDef { id = "first_build",   name = "建设者",     desc = "建造第一个设施",     icon = "🏗", category = AchievementCategory.Explore, hidden = false },
            new AchievementDef { id = "build_all_types", name = "全能建筑师", desc = "建造全部6种设施",   icon = "🏛", category = AchievementCategory.Explore, hidden = true },
            new AchievementDef { id = "visit_town",    name = "旅人驿站",   desc = "第一次到达小镇",     icon = "🏘", category = AchievementCategory.Explore, hidden = false },

            // ─── 载具 ───
            new AchievementDef { id = "first_repair",    name = "修理工",   desc = "第一次修理部件",       icon = "🔧", category = AchievementCategory.Vehicle, hidden = false },
            new AchievementDef { id = "first_upgrade",   name = "改装师",   desc = "第一次升级部件",       icon = "⬆", category = AchievementCategory.Vehicle, hidden = false },
            new AchievementDef { id = "all_max_level",   name = "完美载具", desc = "所有部件升至最高等级", icon = "💎", category = AchievementCategory.Vehicle, hidden = true },
            new AchievementDef { id = "survive_low_fuel", name = "最后一滴", desc = "油量低于5%仍行驶1分钟", icon = "⛽", category = AchievementCategory.Vehicle, hidden = true },

            // ─── 收集 ───
            new AchievementDef { id = "fragment_10", name = "碎片收集者", desc = "发现10个碎片",     icon = "📜", category = AchievementCategory.Collect, hidden = false },
            new AchievementDef { id = "fragment_30", name = "历史探寻者", desc = "发现30个碎片",     icon = "📖", category = AchievementCategory.Collect, hidden = false },
            new AchievementDef { id = "fragment_all", name = "完整的记忆", desc = "发现全部碎片",     icon = "🏆", category = AchievementCategory.Collect, hidden = true },
            new AchievementDef { id = "resource_full", name = "满载而归", desc = "背包载重达到上限", icon = "🎒", category = AchievementCategory.Collect, hidden = false },
        };

        // ── 小镇 ──────────────────────────────────────────────────────────

        /// <summary>小镇中心坐标</summary>
        public static readonly Vector2 TownCenter = new Vector2(-16f, -183f);

        /// <summary>岔路口坐标</summary>
        public static readonly Vector2 ForkPosition = new Vector2(3.7f, -182.8f);

        /// <summary>小镇检测范围</summary>
        public const float TownDetectRange = 25f;

        /// <summary>店铺交互范围</summary>
        public const float ShopInteractRange = 8f;

        /// <summary>店铺列表（6家）</summary>
        public static readonly ShopDef[] Shops = new ShopDef[]
        {
            new ShopDef
            {
                id = "garage", name = "车库", type = ShopType.Garage,
                desc = "重型维修车间，引擎轰鸣不息",
                position = new Vector3(-4f, 0f, -8f), rotation = 0f,
                size = new Vector3(8f, 5f, 6f),
                roofColor = "#4a5060", wallColor = "#6a7080",
                signText = "车库", lightColor = "#ffe8c0",
                ownerLine = "老机修工擦了擦手上的油：这车跑了不少路吧？让我看看。"
            },
            new ShopDef
            {
                id = "supply", name = "补给站", type = ShopType.Supply,
                desc = "物资堆到天花板，什么都有",
                position = new Vector3(6f, 0f, -4f), rotation = 0.1570796f,
                size = new Vector3(6f, 4f, 5f),
                roofColor = "#4a6a3a", wallColor = "#8a7a60",
                signText = "补给", lightColor = "#a0ff90",
                ownerLine = "补给站老板搓着手：外头风大，进来挑挑？"
            },
            new ShopDef
            {
                id = "trade", name = "交易所", type = ShopType.Trade,
                desc = "以物换物，童叟无欺",
                position = new Vector3(-6f, 0f, 4f), rotation = -0.1f,
                size = new Vector3(6f, 4f, 5f),
                roofColor = "#a08030", wallColor = "#a09080",
                signText = "交易", lightColor = "#ffc840",
                ownerLine = "交易所掌柜推了推眼镜：今天行情不错，换点什么？"
            },
            new ShopDef
            {
                id = "tavern", name = "酒馆", type = ShopType.Tavern,
                desc = "炉火温暖，旅人汇聚之地",
                position = new Vector3(4f, 0f, 6f), rotation = 0.15f,
                size = new Vector3(7f, 4f, 5f),
                roofColor = "#5a3a2a", wallColor = "#7a5a3a",
                signText = "酒馆", lightColor = "#ffc060",
                ownerLine = "酒馆老板擦着杯子：坐吧，外头冷。听到什么新鲜事了吗？"
            },
            new ShopDef
            {
                id = "signal", name = "信号站", type = ShopType.Signal,
                desc = "天线林立，捕捉远方信号",
                position = new Vector3(0f, 0f, 12f), rotation = 0f,
                size = new Vector3(4f, 6f, 4f),
                roofColor = "#3a5a8a", wallColor = "#708090",
                signText = "信号", lightColor = "#60b0ff",
                ownerLine = "信号员盯着屏幕：最近收到些奇怪的频段……"
            },
            new ShopDef
            {
                id = "fuel", name = "加油站", type = ShopType.Fuel,
                desc = "红色雨棚下，燃料管嗡嗡作响",
                position = new Vector3(10f, 0f, 2f), rotation = -0.2f,
                size = new Vector3(5f, 3f, 4f),
                roofColor = "#a03030", wallColor = "#c0c0c0",
                signText = "加油", lightColor = "#ff6060",
                ownerLine = "加油工靠在油泵旁：加满？还是只要够跑的？"
            },
        };

        /// <summary>店铺图标映射</summary>
        public static readonly string[] ShopIcons = new string[]
        {
            "🔧",  // Garage
            "📦",  // Supply
            "⚖️",  // Trade
            "🍺",  // Tavern
            "📡",  // Signal
            "⛽",  // Fuel
        };

        /// <summary>小镇商品列表（15项）</summary>
        public static readonly TownItem[] TownItems = new TownItem[]
        {
            // ── 车库商品 ──
            new TownItem
            {
                id = "engine_upgrade_3", name = "高级发动机升级",
                desc = "发动机升级至 Lv.3，动力与耐久大幅提升",
                type = TownItemType.Upgrade,
                cost = new FacilityCost { metal = 5, signal = 3 },
                effect = "engine_level_3", shopType = ShopType.Garage, unique = true
            },
            new TownItem
            {
                id = "full_repair", name = "全车大修",
                desc = "所有部件恢复至最佳状态",
                type = TownItemType.Repair,
                cost = new FacilityCost { metal = 4, wood = 2 },
                effect = "full_repair", shopType = ShopType.Garage, unique = false
            },
            new TownItem
            {
                id = "body_reinforce", name = "车身加固",
                desc = "车身升级至 Lv.2，更耐撞击",
                type = TownItemType.Upgrade,
                cost = new FacilityCost { metal = 3, wood = 2 },
                effect = "body_level_2", shopType = ShopType.Garage, unique = true
            },

            // ── 补给站商品 ──
            new TownItem
            {
                id = "metal_pack", name = "金属包 ×5",
                desc = "5个金属零件，维修必备",
                type = TownItemType.Resource,
                cost = new FacilityCost { fuel = 2 },
                effect = "metal_5", shopType = ShopType.Supply, unique = false
            },
            new TownItem
            {
                id = "wood_pack", name = "木材包 ×5",
                desc = "5根木材，建造与维修通用",
                type = TownItemType.Resource,
                cost = new FacilityCost { fuel = 2 },
                effect = "wood_5", shopType = ShopType.Supply, unique = false
            },
            new TownItem
            {
                id = "survival_kit", name = "生存包",
                desc = "金属×2 + 木材×2 + 燃料×2",
                type = TownItemType.Resource,
                cost = new FacilityCost { signal = 2 },
                effect = "survival_kit", shopType = ShopType.Supply, unique = false
            },

            // ── 交易所商品 ──
            new TownItem
            {
                id = "fuel_trade", name = "燃料补给",
                desc = "用金属换取3单位燃料",
                type = TownItemType.Resource,
                cost = new FacilityCost { metal = 1 },
                effect = "fuel_3", shopType = ShopType.Trade, unique = false
            },
            new TownItem
            {
                id = "signal_trade", name = "信号件交易",
                desc = "用燃料换取2个信号件",
                type = TownItemType.Resource,
                cost = new FacilityCost { fuel = 2 },
                effect = "signal_2", shopType = ShopType.Trade, unique = false
            },
            new TownItem
            {
                id = "crystal_trade", name = "光源晶交易",
                desc = "用信号件和金属换取1颗光源晶",
                type = TownItemType.Resource,
                cost = new FacilityCost { signal = 2, metal = 2 },
                effect = "crystal_1", shopType = ShopType.Trade, unique = false
            },

            // ── 酒馆商品（打听消息）──
            new TownItem
            {
                id = "traveler_rumor", name = "旅人传闻",
                desc = "花1信号件，打听一段路途传闻",
                type = TownItemType.Info,
                cost = new FacilityCost { signal = 1 },
                effect = "rumor", shopType = ShopType.Tavern, unique = false
            },
            new TownItem
            {
                id = "old_map", name = "旧地图碎片",
                desc = "花2信号件，获得一份旧地图线索",
                type = TownItemType.Info,
                cost = new FacilityCost { signal = 2 },
                effect = "old_map", shopType = ShopType.Tavern, unique = false
            },

            // ── 信号站商品 ──
            new TownItem
            {
                id = "signal_amplifier", name = "信号放大器",
                desc = "电台升级至 Lv.2，信号接收范围扩大",
                type = TownItemType.Upgrade,
                cost = new FacilityCost { metal = 2, signal = 2 },
                effect = "radio_level_2", shopType = ShopType.Signal, unique = true
            },
            new TownItem
            {
                id = "signal_parts", name = "信号件 ×3",
                desc = "3个信号件，用于升级和交易",
                type = TownItemType.Resource,
                cost = new FacilityCost { metal = 2, fuel = 1 },
                effect = "signal_3", shopType = ShopType.Signal, unique = false
            },

            // ── 加油站商品 ──
            new TownItem
            {
                id = "fuel_fill", name = "补满油箱",
                desc = "油箱燃料直接补满",
                type = TownItemType.Resource,
                cost = new FacilityCost { metal = 1 },
                effect = "fuel_fill", shopType = ShopType.Fuel, unique = false
            },
            new TownItem
            {
                id = "tank_upgrade", name = "油箱扩容",
                desc = "油箱升级至 Lv.2，容量增加",
                type = TownItemType.Upgrade,
                cost = new FacilityCost { metal = 3, fuel = 2 },
                effect = "tank_level_2", shopType = ShopType.Fuel, unique = true
            },
        };

        /// <summary>酒馆叙事碎片（10条）</summary>
        public static readonly string[] TavernRumors = new string[]
        {
            "有人说，路的尽头有一座灯塔，但从来没有人走到过那里。",
            "前一批勘测员在裂谷里发现了什么，回来后就不说话了。",
            "信号站最近收到一组重复编码，像是某种求救信号。",
            "有个老司机说，暴风雪最猛的时候，能看到路边的影子在走。",
            "听说有人在冰湖下面看到了光，不是反射——是从深处透上来的。",
            "电台里偶尔能听到音乐，但那种曲子……不属于这个年代。",
            "补给站的老板说，他的货从来不是从正规渠道来的。",
            "有人声称在山脊上看到了另一个自己，穿着一样的衣服，开着一样的车。",
            "车库的老机修工说，他修过的车里，有一辆没有发动机却能跑。",
            "交易所掌柜悄悄说：有些东西不是用来换的——是用来还的。",
        };
    }
}

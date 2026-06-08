// =============================================================================
// GameData.cs — 未尽之路 全局静态数据定义
// 由 TypeScript 数据文件翻译而来，包含所有游戏数据
// =============================================================================

using System;
using UnityEngine;
using WeiJinRoad.Core;

namespace WeiJinRoad.Data
{
    // =========================================================================
    // 枚举定义
    // =========================================================================

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
        public int Metal;
        public int Wood;
        public int Fuel;
        public int Signal;
        public int Crystal;
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
    /// 店铺定义
    /// </summary>
    [Serializable]
    public class ShopDef
    {
        public string Id;
        public string Name;
        public ShopType Type;
        public string Desc;
        public Vector3 Position;
        public float Rotation;
        public Vector3 Size;
        public Color RoofColor;
        public Color WallColor;
        public string SignText;
        public Color LightColor;
        public string OwnerLine;
    }

    /// <summary>
    /// 小镇商品
    /// </summary>
    [Serializable]
    public class TownItem
    {
        public string Id;
        public string Name;
        public string Desc;
        public TownItemType Type;
        public ResourceBag Cost;
        public string Effect;
        public ShopType ShopType;
        public bool Unique;
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
            new FacilityCost { Metal = 3, Wood = 2 },                                    // Supply
            new FacilityCost { Metal = 2, Wood = 4 },                                    // Shelter
            new FacilityCost { Metal = 2, Signal = 2 },                                  // SignalTower
            new FacilityCost { Metal = 3, Crystal = 1, Wood = 2 },                       // Beacon
            new FacilityCost { Metal = 2, Wood = 3, Signal = 1 },                        // Observatory
            new FacilityCost { Metal = 4, Wood = 3 },                                    // Bridge
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
        private static ShopDef[] _shops;
        public static ShopDef[] Shops => _shops ??= CreateShops();

        private static ShopDef[] CreateShops()
        {
            return new ShopDef[]
        {
            new ShopDef
            {
                Id = "garage", Name = "车库", Type = ShopType.Garage,
                Desc = "重型维修车间，引擎轰鸣不息",
                Position = new Vector3(-4f, 0f, -8f), Rotation = 0f,
                Size = new Vector3(8f, 5f, 6f),
                RoofColor = HexColor("#4a5060"), WallColor = HexColor("#6a7080"),
                SignText = "车库", LightColor = HexColor("#ffe8c0"),
                OwnerLine = "老机修工擦了擦手上的油：这车跑了不少路吧？让我看看。"
            },
            new ShopDef
            {
                Id = "supply", Name = "补给站", Type = ShopType.Supply,
                Desc = "物资堆到天花板，什么都有",
                Position = new Vector3(6f, 0f, -4f), Rotation = 0.1570796f,
                Size = new Vector3(6f, 4f, 5f),
                RoofColor = HexColor("#4a6a3a"), WallColor = HexColor("#8a7a60"),
                SignText = "补给", LightColor = HexColor("#a0ff90"),
                OwnerLine = "补给站老板搓着手：外头风大，进来挑挑？"
            },
            new ShopDef
            {
                Id = "trade", Name = "交易所", Type = ShopType.Trade,
                Desc = "以物换物，童叟无欺",
                Position = new Vector3(-6f, 0f, 4f), Rotation = -0.1f,
                Size = new Vector3(6f, 4f, 5f),
                RoofColor = HexColor("#a08030"), WallColor = HexColor("#a09080"),
                SignText = "交易", LightColor = HexColor("#ffc840"),
                OwnerLine = "交易所掌柜推了推眼镜：今天行情不错，换点什么？"
            },
            new ShopDef
            {
                Id = "tavern", Name = "酒馆", Type = ShopType.Tavern,
                Desc = "炉火温暖，旅人汇聚之地",
                Position = new Vector3(4f, 0f, 6f), Rotation = 0.15f,
                Size = new Vector3(7f, 4f, 5f),
                RoofColor = HexColor("#5a3a2a"), WallColor = HexColor("#7a5a3a"),
                SignText = "酒馆", LightColor = HexColor("#ffc060"),
                OwnerLine = "酒馆老板擦着杯子：坐吧，外头冷。听到什么新鲜事了吗？"
            },
            new ShopDef
            {
                Id = "signal", Name = "信号站", Type = ShopType.Signal,
                Desc = "天线林立，捕捉远方信号",
                Position = new Vector3(0f, 0f, 12f), Rotation = 0f,
                Size = new Vector3(4f, 6f, 4f),
                RoofColor = HexColor("#3a5a8a"), WallColor = HexColor("#708090"),
                SignText = "信号", LightColor = HexColor("#60b0ff"),
                OwnerLine = "信号员盯着屏幕：最近收到些奇怪的频段……"
            },
            new ShopDef
            {
                Id = "fuel", Name = "加油站", Type = ShopType.Fuel,
                Desc = "红色雨棚下，燃料管嗡嗡作响",
                Position = new Vector3(10f, 0f, 2f), Rotation = -0.2f,
                Size = new Vector3(5f, 3f, 4f),
                RoofColor = HexColor("#a03030"), WallColor = HexColor("#c0c0c0"),
                SignText = "加油", LightColor = HexColor("#ff6060"),
                OwnerLine = "加油工靠在油泵旁：加满？还是只要够跑的？"
            },
        };
        }

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
        private static TownItem[] _townItems;
        public static TownItem[] TownItems => _townItems ??= CreateTownItems();

        private static TownItem[] CreateTownItems()
        {
            return new TownItem[]
        {
            // ── 车库商品 ──
            new TownItem
            {
                Id = "engine_upgrade_3", Name = "高级发动机升级",
                Desc = "发动机升级至 Lv.3，动力与耐久大幅提升",
                Type = TownItemType.Upgrade,
                Cost = new ResourceBag { Metal = 5, Signal = 3 },
                Effect = "engine_level_3", ShopType = ShopType.Garage, Unique = true
            },
            new TownItem
            {
                Id = "full_repair", Name = "全车大修",
                Desc = "所有部件恢复至最佳状态",
                Type = TownItemType.Repair,
                Cost = new ResourceBag { Metal = 4, Wood = 2 },
                Effect = "full_repair", ShopType = ShopType.Garage, Unique = false
            },
            new TownItem
            {
                Id = "body_reinforce", Name = "车身加固",
                Desc = "车身升级至 Lv.2，更耐撞击",
                Type = TownItemType.Upgrade,
                Cost = new ResourceBag { Metal = 3, Wood = 2 },
                Effect = "body_level_2", ShopType = ShopType.Garage, Unique = true
            },

            // ── 补给站商品 ──
            new TownItem
            {
                Id = "metal_pack", Name = "金属包 ×5",
                Desc = "5个金属零件，维修必备",
                Type = TownItemType.Resource,
                Cost = new ResourceBag { Fuel = 2 },
                Effect = "metal_5", ShopType = ShopType.Supply, Unique = false
            },
            new TownItem
            {
                Id = "wood_pack", Name = "木材包 ×5",
                Desc = "5根木材，建造与维修通用",
                Type = TownItemType.Resource,
                Cost = new ResourceBag { Fuel = 2 },
                Effect = "wood_5", ShopType = ShopType.Supply, Unique = false
            },
            new TownItem
            {
                Id = "survival_kit", Name = "生存包",
                Desc = "金属×2 + 木材×2 + 燃料×2",
                Type = TownItemType.Resource,
                Cost = new ResourceBag { Signal = 2 },
                Effect = "survival_kit", ShopType = ShopType.Supply, Unique = false
            },

            // ── 交易所商品 ──
            new TownItem
            {
                Id = "fuel_trade", Name = "燃料补给",
                Desc = "用金属换取3单位燃料",
                Type = TownItemType.Resource,
                Cost = new ResourceBag { Metal = 1 },
                Effect = "fuel_3", ShopType = ShopType.Trade, Unique = false
            },
            new TownItem
            {
                Id = "signal_trade", Name = "信号件交易",
                Desc = "用燃料换取2个信号件",
                Type = TownItemType.Resource,
                Cost = new ResourceBag { Fuel = 2 },
                Effect = "signal_2", ShopType = ShopType.Trade, Unique = false
            },
            new TownItem
            {
                Id = "crystal_trade", Name = "光源晶交易",
                Desc = "用信号件和金属换取1颗光源晶",
                Type = TownItemType.Resource,
                Cost = new ResourceBag { Signal = 2, Metal = 2 },
                Effect = "crystal_1", ShopType = ShopType.Trade, Unique = false
            },

            // ── 酒馆商品（打听消息）──
            new TownItem
            {
                Id = "traveler_rumor", Name = "旅人传闻",
                Desc = "花1信号件，打听一段路途传闻",
                Type = TownItemType.Info,
                Cost = new ResourceBag { Signal = 1 },
                Effect = "rumor", ShopType = ShopType.Tavern, Unique = false
            },
            new TownItem
            {
                Id = "old_map", Name = "旧地图碎片",
                Desc = "花2信号件，获得一份旧地图线索",
                Type = TownItemType.Info,
                Cost = new ResourceBag { Signal = 2 },
                Effect = "old_map", ShopType = ShopType.Tavern, Unique = false
            },

            // ── 信号站商品 ──
            new TownItem
            {
                Id = "signal_amplifier", Name = "信号放大器",
                Desc = "电台升级至 Lv.2，信号接收范围扩大",
                Type = TownItemType.Upgrade,
                Cost = new ResourceBag { Metal = 2, Signal = 2 },
                Effect = "radio_level_2", ShopType = ShopType.Signal, Unique = true
            },
            new TownItem
            {
                Id = "signal_parts", Name = "信号件 ×3",
                Desc = "3个信号件，用于升级和交易",
                Type = TownItemType.Resource,
                Cost = new ResourceBag { Metal = 2, Fuel = 1 },
                Effect = "signal_3", ShopType = ShopType.Signal, Unique = false
            },

            // ── 加油站商品 ──
            new TownItem
            {
                Id = "fuel_fill", Name = "补满油箱",
                Desc = "油箱燃料直接补满",
                Type = TownItemType.Resource,
                Cost = new ResourceBag { Metal = 1 },
                Effect = "fuel_fill", ShopType = ShopType.Fuel, Unique = false
            },
            new TownItem
            {
                Id = "tank_upgrade", Name = "油箱扩容",
                Desc = "油箱升级至 Lv.2，容量增加",
                Type = TownItemType.Upgrade,
                Cost = new ResourceBag { Metal = 3, Fuel = 2 },
                Effect = "tank_level_2", ShopType = ShopType.Fuel, Unique = true
                },
            };
        }

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

        /// <summary>
        /// 将十六进制颜色字符串转换为 Color
        /// </summary>
        private static Color HexColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c))
                return c;
            return Color.white;
        }
    }
}

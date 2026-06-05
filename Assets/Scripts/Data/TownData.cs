using System;
using UnityEngine;
using WeiJinRoad.Core;

namespace WeiJinRoad.Data
{
    // =================================================================
    // 枚举类型
    // =================================================================

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
    /// 商品类型：升级、资源、信息、维修
    /// </summary>
    public enum TownItemType
    {
        Upgrade,
        Resource,
        Info,
        Repair
    }

    // =================================================================
    // 数据结构
    // =================================================================

    /// <summary>
    /// 店铺定义
    /// </summary>
    [Serializable]
    public class ShopDef
    {
        /// <summary>店铺ID</summary>
        public string Id;
        /// <summary>店铺名称</summary>
        public string Name;
        /// <summary>店铺类型</summary>
        public ShopType Type;
        /// <summary>店铺描述</summary>
        public string Desc;
        /// <summary>相对小镇中心位置</summary>
        public Vector3 Position;
        /// <summary>旋转角度（弧度）</summary>
        public float Rotation;
        /// <summary>尺寸（宽、高、深）</summary>
        public Vector3 Size;
        /// <summary>屋顶颜色</summary>
        public Color RoofColor;
        /// <summary>墙壁颜色</summary>
        public Color WallColor;
        /// <summary>招牌文字</summary>
        public string SignText;
        /// <summary>灯光颜色</summary>
        public Color LightColor;
        /// <summary>店主对话</summary>
        public string OwnerLine;
    }

    /// <summary>
    /// 小镇商品定义
    /// </summary>
    [Serializable]
    public class TownItem
    {
        /// <summary>商品ID</summary>
        public string Id;
        /// <summary>商品名称</summary>
        public string Name;
        /// <summary>商品描述</summary>
        public string Desc;
        /// <summary>商品类型</summary>
        public TownItemType Type;
        /// <summary>购买消耗</summary>
        public ResourceBag Cost;
        /// <summary>效果标识</summary>
        public string Effect;
        /// <summary>所属店铺类型</summary>
        public ShopType ShopType;
        /// <summary>是否唯一（只能购买一次）</summary>
        public bool Unique;
    }

    // =================================================================
    /// <summary>
    /// 霜原驿站 — 小镇静态数据
    ///
    /// 坐标 (3.7, -182.8) 附近，岔路口左转进入
    /// </summary>
    public static class TownData
    {
        // ─── 小镇中心坐标 ───
        /// <summary>小镇中心X坐标</summary>
        public const float TownCenterX = -16f;
        /// <summary>小镇中心Z坐标</summary>
        public const float TownCenterZ = -183f;

        // ─── 岔路口坐标 ───
        /// <summary>岔路口X坐标</summary>
        public const float ForkX = 3.7f;
        /// <summary>岔路口Z坐标</summary>
        public const float ForkZ = -182.8f;

        // ─── 检测范围 ───
        /// <summary>小镇检测范围</summary>
        public const float TownDetectRange = 25f;
        /// <summary>店铺交互范围</summary>
        public const float ShopInteractRange = 8f;

        // ─── 资源元数据 ───
        private static readonly (string Short, Color Color)[] ResourceMeta = new[]
        {
            ("金", new Color(0.75f, 0.80f, 0.90f, 1f)),   // Metal
            ("木", new Color(0.72f, 0.56f, 0.35f, 1f)),   // Wood
            ("燃", new Color(0.90f, 0.65f, 0.15f, 1f)),   // Fuel
            ("信", new Color(0.40f, 0.75f, 0.95f, 1f)),   // Signal
            ("晶", new Color(0.80f, 0.60f, 0.95f, 1f)),   // Crystal
        };

        /// <summary>
        /// 获取资源简称
        /// </summary>
        public static string GetResourceShort(ResourceKind kind) => ResourceMeta[(int)kind].Short;

        /// <summary>
        /// 获取资源颜色
        /// </summary>
        public static Color GetResourceColor(ResourceKind kind) => ResourceMeta[(int)kind].Color;

        // ─── 店铺列表 ───

        private static ShopDef[] _shops;
        /// <summary>店铺定义列表</summary>
        public static ShopDef[] Shops => _shops ??= CreateShops();

        private static ShopDef[] CreateShops()
        {
            return new ShopDef[]
            {
                new ShopDef
                {
                    Id = "garage", Name = "车库", Type = ShopType.Garage,
                    Desc = "重型维修车间，引擎轰鸣不息",
                    Position = new Vector3(-4, 0, -8), Rotation = 0f,
                    Size = new Vector3(8, 5, 6),
                    RoofColor = HexColor("#4a5060"), WallColor = HexColor("#6a7080"),
                    SignText = "车库", LightColor = HexColor("#ffe8c0"),
                    OwnerLine = "老机修工擦了擦手上的油：这车跑了不少路吧？让我看看。",
                },
                new ShopDef
                {
                    Id = "supply", Name = "补给站", Type = ShopType.Supply,
                    Desc = "物资堆到天花板，什么都有",
                    Position = new Vector3(6, 0, -4), Rotation = Mathf.PI * 0.05f,
                    Size = new Vector3(6, 4, 5),
                    RoofColor = HexColor("#4a6a3a"), WallColor = HexColor("#8a7a60"),
                    SignText = "补给", LightColor = HexColor("#a0ff90"),
                    OwnerLine = "补给站老板搓着手：外头风大，进来挑挑？",
                },
                new ShopDef
                {
                    Id = "trade", Name = "交易所", Type = ShopType.Trade,
                    Desc = "以物换物，童叟无欺",
                    Position = new Vector3(-6, 0, 4), Rotation = -0.1f,
                    Size = new Vector3(6, 4, 5),
                    RoofColor = HexColor("#a08030"), WallColor = HexColor("#a09080"),
                    SignText = "交易", LightColor = HexColor("#ffc840"),
                    OwnerLine = "交易所掌柜推了推眼镜：今天行情不错，换点什么？",
                },
                new ShopDef
                {
                    Id = "tavern", Name = "酒馆", Type = ShopType.Tavern,
                    Desc = "炉火温暖，旅人汇聚之地",
                    Position = new Vector3(4, 0, 6), Rotation = 0.15f,
                    Size = new Vector3(7, 4, 5),
                    RoofColor = HexColor("#5a3a2a"), WallColor = HexColor("#7a5a3a"),
                    SignText = "酒馆", LightColor = HexColor("#ffc060"),
                    OwnerLine = "酒馆老板擦着杯子：坐吧，外头冷。听到什么新鲜事了吗？",
                },
                new ShopDef
                {
                    Id = "signal", Name = "信号站", Type = ShopType.Signal,
                    Desc = "天线林立，捕捉远方信号",
                    Position = new Vector3(0, 0, 12), Rotation = 0f,
                    Size = new Vector3(4, 6, 4),
                    RoofColor = HexColor("#3a5a8a"), WallColor = HexColor("#708090"),
                    SignText = "信号", LightColor = HexColor("#60b0ff"),
                    OwnerLine = "信号员盯着屏幕：最近收到些奇怪的频段……",
                },
                new ShopDef
                {
                    Id = "fuel", Name = "加油站", Type = ShopType.Fuel,
                    Desc = "红色雨棚下，燃料管嗡嗡作响",
                    Position = new Vector3(10, 0, 2), Rotation = -0.2f,
                    Size = new Vector3(5, 3, 4),
                    RoofColor = HexColor("#a03030"), WallColor = HexColor("#c0c0c0"),
                    SignText = "加油", LightColor = HexColor("#ff6060"),
                    OwnerLine = "加油工靠在油泵旁：加满？还是只要够跑的？",
                },
            };
        }

        // ─── 商品列表 ───

        private static TownItem[] _townItems;
        /// <summary>商品定义列表</summary>
        public static TownItem[] TownItems => _townItems ??= CreateTownItems();

        private static TownItem[] CreateTownItems()
        {
            return new TownItem[]
            {
                // 车库商品
                new TownItem
                {
                    Id = "engine_upgrade_3", Name = "高级发动机升级",
                    Desc = "发动机升级至 Lv.3，动力与耐久大幅提升",
                    Type = TownItemType.Upgrade,
                    Cost = new ResourceBag { Metal = 5, Signal = 3 },
                    Effect = "engine_level_3", ShopType = ShopType.Garage, Unique = true,
                },
                new TownItem
                {
                    Id = "full_repair", Name = "全车大修",
                    Desc = "所有部件恢复至最佳状态",
                    Type = TownItemType.Repair,
                    Cost = new ResourceBag { Metal = 4, Wood = 2 },
                    Effect = "full_repair", ShopType = ShopType.Garage, Unique = false,
                },
                new TownItem
                {
                    Id = "body_reinforce", Name = "车身加固",
                    Desc = "车身升级至 Lv.2，更耐撞击",
                    Type = TownItemType.Upgrade,
                    Cost = new ResourceBag { Metal = 3, Wood = 2 },
                    Effect = "body_level_2", ShopType = ShopType.Garage, Unique = true,
                },

                // 补给站商品
                new TownItem
                {
                    Id = "metal_pack", Name = "金属包 ×5",
                    Desc = "5个金属零件，维修必备",
                    Type = TownItemType.Resource,
                    Cost = new ResourceBag { Fuel = 2 },
                    Effect = "metal_5", ShopType = ShopType.Supply, Unique = false,
                },
                new TownItem
                {
                    Id = "wood_pack", Name = "木材包 ×5",
                    Desc = "5根木材，建造与维修通用",
                    Type = TownItemType.Resource,
                    Cost = new ResourceBag { Fuel = 2 },
                    Effect = "wood_5", ShopType = ShopType.Supply, Unique = false,
                },
                new TownItem
                {
                    Id = "survival_kit", Name = "生存包",
                    Desc = "金属×2 + 木材×2 + 燃料×2",
                    Type = TownItemType.Resource,
                    Cost = new ResourceBag { Signal = 2 },
                    Effect = "survival_kit", ShopType = ShopType.Supply, Unique = false,
                },

                // 交易所商品
                new TownItem
                {
                    Id = "fuel_trade", Name = "燃料补给",
                    Desc = "用金属换取3单位燃料",
                    Type = TownItemType.Resource,
                    Cost = new ResourceBag { Metal = 1 },
                    Effect = "fuel_3", ShopType = ShopType.Trade, Unique = false,
                },
                new TownItem
                {
                    Id = "signal_trade", Name = "信号件交易",
                    Desc = "用燃料换取2个信号件",
                    Type = TownItemType.Resource,
                    Cost = new ResourceBag { Fuel = 2 },
                    Effect = "signal_2", ShopType = ShopType.Trade, Unique = false,
                },
                new TownItem
                {
                    Id = "crystal_trade", Name = "光源晶交易",
                    Desc = "用信号件和金属换取1颗光源晶",
                    Type = TownItemType.Resource,
                    Cost = new ResourceBag { Signal = 2, Metal = 2 },
                    Effect = "crystal_1", ShopType = ShopType.Trade, Unique = false,
                },

                // 酒馆商品
                new TownItem
                {
                    Id = "traveler_rumor", Name = "旅人传闻",
                    Desc = "花1信号件，打听一段路途传闻",
                    Type = TownItemType.Info,
                    Cost = new ResourceBag { Signal = 1 },
                    Effect = "rumor", ShopType = ShopType.Tavern, Unique = false,
                },
                new TownItem
                {
                    Id = "old_map", Name = "旧地图碎片",
                    Desc = "花2信号件，获得一份旧地图线索",
                    Type = TownItemType.Info,
                    Cost = new ResourceBag { Signal = 2 },
                    Effect = "old_map", ShopType = ShopType.Tavern, Unique = false,
                },

                // 信号站商品
                new TownItem
                {
                    Id = "signal_amplifier", Name = "信号放大器",
                    Desc = "电台升级至 Lv.2，信号接收范围扩大",
                    Type = TownItemType.Upgrade,
                    Cost = new ResourceBag { Metal = 2, Signal = 2 },
                    Effect = "radio_level_2", ShopType = ShopType.Signal, Unique = true,
                },
                new TownItem
                {
                    Id = "signal_parts", Name = "信号件 ×3",
                    Desc = "3个信号件，用于升级和交易",
                    Type = TownItemType.Resource,
                    Cost = new ResourceBag { Metal = 2, Fuel = 1 },
                    Effect = "signal_3", ShopType = ShopType.Signal, Unique = false,
                },

                // 加油站商品
                new TownItem
                {
                    Id = "fuel_fill", Name = "补满油箱",
                    Desc = "油箱燃料直接补满",
                    Type = TownItemType.Resource,
                    Cost = new ResourceBag { Metal = 1 },
                    Effect = "fuel_fill", ShopType = ShopType.Fuel, Unique = false,
                },
                new TownItem
                {
                    Id = "tank_upgrade", Name = "油箱扩容",
                    Desc = "油箱升级至 Lv.2，容量增加",
                    Type = TownItemType.Upgrade,
                    Cost = new ResourceBag { Metal = 3, Fuel = 2 },
                    Effect = "tank_level_2", ShopType = ShopType.Fuel, Unique = true,
                },
            };
        }

        // ─── 酒馆传闻 ───

        private static readonly string[] TavernRumors = new string[]
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
        /// 获取随机酒馆传闻
        /// </summary>
        /// <param name="index">传闻索引，-1为随机</param>
        /// <returns>传闻文本</returns>
        public static string GetRumor(int index = -1)
        {
            if (index < 0 || index >= TavernRumors.Length)
                index = UnityEngine.Random.Range(0, TavernRumors.Length);
            return TavernRumors[index];
        }

        /// <summary>
        /// 获取指定店铺类型的商品列表
        /// </summary>
        public static TownItem[] GetItemsByShopType(ShopType shopType)
        {
            var items = TownItems;
            var result = new System.Collections.Generic.List<TownItem>();
            foreach (var item in items)
            {
                if (item.ShopType == shopType)
                    result.Add(item);
            }
            return result.ToArray();
        }

        // ─── 店铺图标 ───

        private static readonly string[] ShopIcons = new string[]
        {
            "🔧", // Garage
            "📦", // Supply
            "⚖️", // Trade
            "🍺", // Tavern
            "📡", // Signal
            "⛽", // Fuel
        };

        /// <summary>
        /// 获取店铺类型对应的图标emoji
        /// </summary>
        public static string GetShopIcon(ShopType type) => ShopIcons[(int)type];

        // ─── 辅助 ───

        private static Color HexColor(string hex)
        {
            if (ColorUtility.TryParseHtmlString(hex, out var c))
                return c;
            return Color.white;
        }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-bc783dce70414643987f911a63dc0f9f/cwd.txt'; exit "$__tr_native_ec"
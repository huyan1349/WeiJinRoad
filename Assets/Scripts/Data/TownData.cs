// =============================================================================
// TownData.cs — 霜原驿站 小镇静态数据辅助
// =============================================================================

using System;
using UnityEngine;
using WeiJinRoad.Core;

namespace WeiJinRoad.Data
{
    /// <summary>
    /// 霜原驿站 — 小镇静态数据辅助
    ///
    /// 坐标 (3.7, -182.8) 附近，岔路口左转进入
    /// </summary>
    public static class TownData
    {
        // ─── 小镇中心坐标 ───
        public const float TownCenterX = -16f;
        public const float TownCenterZ = -183f;

        // ─── 岔路口坐标 ───
        public const float ForkX = 3.7f;
        public const float ForkZ = -182.8f;

        // ─── 检测范围 ───
        public const float TownDetectRange = 25f;
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

        public static string GetResourceShort(ResourceKind kind) => ResourceMeta[(int)kind].Short;
        public static Color GetResourceColor(ResourceKind kind) => ResourceMeta[(int)kind].Color;

        // ─── 店铺列表（委托给 GameData）───
        public static ShopDef[] Shops => GameData.Shops;

        // ─── 商品列表（委托给 GameData）───
        public static TownItem[] TownItems => GameData.TownItems;

        // ─── 酒馆传闻 ───
        public static string GetRumor(int index = -1)
        {
            var rumors = GameData.TavernRumors;
            if (index < 0 || index >= rumors.Length)
                index = UnityEngine.Random.Range(0, rumors.Length);
            return rumors[index];
        }

        // ─── 按店铺类型筛选商品 ───
        public static TownItem[] GetItemsByShopType(ShopType shopType)
        {
            var items = GameData.TownItems;
            var result = new System.Collections.Generic.List<TownItem>();
            foreach (var item in items)
            {
                if (item.ShopType == shopType)
                    result.Add(item);
            }
            return result.ToArray();
        }

        // ─── 店铺图标 ───
        public static string GetShopIcon(ShopType type) => GameData.ShopIcons[(int)type];
    }
}

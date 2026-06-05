using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;
using WeiJinRoad.Data;

namespace WeiJinRoad.UI
{
    /// <summary>
    /// 小镇购物/酒馆 UI
    ///
    /// 功能：
    /// - 6 个店铺标签：车库、补给站、交易所、酒馆、信号站、加油站
    /// - 每个标签展示可购买商品及价格
    /// - 购买逻辑：检查资源、扣减、应用效果
    /// - 酒馆：显示随机传闻
    /// - UI：Canvas + TextMeshPro，深色半透明背景
    /// - 仅在玩家处于小镇范围内且停车后可见
    /// </summary>
    public class TownUI : MonoBehaviour
    {
        // =================================================================
        // 常量
        // =================================================================

        private const string ZpixFontPath = "Fonts/zpix";
        private const float StopSpeed = 0.3f;
        private const float ToastDuration = 2.5f;

        // ─── 颜色 ───

        private static readonly Color BgColor = new Color(0f, 0f, 0f, 0.70f);
        private static readonly Color PanelBorderColor = new Color(1f, 1f, 1f, 0.12f);
        private static readonly Color TextMainColor = new Color(0.85f, 0.88f, 0.95f, 1f);
        private static readonly Color TextDimColor = new Color(1f, 1f, 1f, 0.35f);
        private static readonly Color TextMutedColor = new Color(1f, 1f, 1f, 0.55f);
        private static readonly Color AccentColor = new Color(0.40f, 0.65f, 0.95f, 1f);
        private static readonly Color SuccessColor = new Color(0.31f, 0.78f, 0.47f, 1f);
        private static readonly Color ErrorColor = new Color(1f, 0.31f, 0.31f, 1f);
        private static readonly Color InfoColor = new Color(0.47f, 0.67f, 1f, 1f);
        private static readonly Color DividerColor = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color DividerColor2 = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color ItemBgColor = new Color(1f, 1f, 1f, 0.03f);
        private static readonly Color ItemBorderColor = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color BtnBuyBgColor = new Color(0.47f, 0.67f, 1f, 0.10f);
        private static readonly Color BtnBuyBorderColor = new Color(0.47f, 0.67f, 1f, 0.30f);
        private static readonly Color BtnDisabledBgColor = new Color(1f, 1f, 1f, 0.02f);
        private static readonly Color BtnDisabledBorderColor = new Color(1f, 1f, 1f, 0.06f);
        private static readonly Color BtnDisabledTextColor = new Color(1f, 1f, 1f, 0.20f);
        private static readonly Color BtnPurchasedBgColor = new Color(0.31f, 0.78f, 0.47f, 0.10f);
        private static readonly Color BtnPurchasedBorderColor = new Color(0.31f, 0.78f, 0.47f, 0.30f);
        private static readonly Color BtnPurchasedTextColor = new Color(0.31f, 0.78f, 0.47f, 0.70f);
        private static readonly Color NearbyDotColor = new Color(0.37f, 0.78f, 1f, 1f);

        // =================================================================
        // 运行时状态
        // =================================================================

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;

        // 顶层容器
        private GameObject _townHintPanel;   // 小镇入口提示
        private GameObject _shopBarPanel;    // 店铺选择栏
        private GameObject _shopDetailPanel; // 店铺详情面板
        private GameObject _toastContainer;  // Toast 容器

        // 店铺详情面板子元素引用
        private TMP_Text _shopDetailTitle;
        private TMP_Text _shopDetailDesc;
        private TMP_Text _shopDetailOwnerLine;
        private Transform _itemListContainer;
        private TMP_Text _inventoryText;

        // 状态
        private ShopDef _activeShop;
        private bool _stopped;
        private readonly List<ToastEntry> _toasts = new List<ToastEntry>();
        private int _toastIdCounter;

        private struct ToastEntry
        {
            public int Id;
            public string Message;
            public ToastType Type;
            public float ExpireTime;
        }

        private enum ToastType { Success, Error, Info }

        // =================================================================
        // Unity 生命周期
        // =================================================================

        private void OnEnable()
        {
            CreateUI();
            GameEvents.OnResourcesChanged += OnResourcesChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnResourcesChanged -= OnResourcesChanged;
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // 轮询车速
            _stopped = Mathf.Abs(gm.VehicleTransient.Speed) < StopSpeed;

            bool inTownAndStopped = gm.TownVisited && _stopped && !string.IsNullOrEmpty(gm.NearbyShop);

            // 小镇入口提示
            if (_townHintPanel != null)
                _townHintPanel.SetActive(gm.TownVisited && _activeShop == null);

            // 店铺选择栏
            if (_shopBarPanel != null)
                _shopBarPanel.SetActive(inTownAndStopped && _activeShop == null);

            // ESC 关闭店铺面板
            if (Input.GetKeyDown(KeyCode.Escape) && _activeShop != null)
                CloseShop();

            // 更新 Toast
            UpdateToasts();
        }

        private void OnResourcesChanged(ResourceBag _)
        {
            if (_activeShop != null && _shopDetailPanel != null && _shopDetailPanel.activeSelf)
                RefreshShopDetail();
        }

        // =================================================================
        // UI 创建
        // =================================================================

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null)
                _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            CreateCanvas();
            CreateTownHint();
            CreateShopBar();
            CreateShopDetailPanel();
            CreateToastContainer();
            Debug.Log("[TownUI] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 130;

            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();
        }

        // ─── 小镇入口提示 ───

        private void CreateTownHint()
        {
            _townHintPanel = CreateUIObject("TownHint", transform);
            var rect = _townHintPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -80f);
            rect.sizeDelta = new Vector2(420f, 44f);

            var bg = _townHintPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.45f);
            bg.raycastTarget = false;

            var hLayout = _townHintPanel.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(20, 20, 10, 10);
            hLayout.spacing = 12f;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            var label1 = CreateUIObject("Label1", _townHintPanel.transform);
            var t1 = label1.AddComponent<TextMeshProUGUI>();
            t1.text = "霜原驿站";
            t1.fontSize = 10;
            t1.color = new Color(1f, 1f, 1f, 0.40f);
            t1.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) t1.font = _fontAsset;

            var divider = CreateUIObject("Divider", _townHintPanel.transform);
            var divRect = divider.AddComponent<RectTransform>();
            divRect.sizeDelta = new Vector2(1f, 12f);
            divider.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.10f);

            var label2 = CreateUIObject("Label2", _townHintPanel.transform);
            var t2 = label2.AddComponent<TextMeshProUGUI>();
            t2.text = "停车后可进入店铺";
            t2.fontSize = 10;
            t2.color = new Color(1f, 1f, 1f, 0.55f);
            t2.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) t2.font = _fontAsset;

            _townHintPanel.SetActive(false);
        }

        // ─── 店铺选择栏 ───

        private void CreateShopBar()
        {
            _shopBarPanel = CreateUIObject("ShopBar", transform);
            var rect = _shopBarPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 24f);
            rect.sizeDelta = new Vector2(680f, 100f);

            var bg = _shopBarPanel.AddComponent<Image>();
            bg.color = new Color(0f, 0f, 0f, 0.55f);

            var hLayout = _shopBarPanel.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(16, 16, 12, 12);
            hLayout.spacing = 8f;
            hLayout.childAlignment = TextAnchor.MiddleCenter;
            hLayout.childControlWidth = false;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = false;
            hLayout.childForceExpandHeight = false;

            // 小镇名称
            var nameLabel = CreateUIObject("TownName", _shopBarPanel.transform);
            var nameRect = nameLabel.AddComponent<RectTransform>();
            nameRect.sizeDelta = new Vector2(80f, 50f);
            var nameText = nameLabel.AddComponent<TextMeshProUGUI>();
            nameText.text = "霜原驿站";
            nameText.fontSize = 10;
            nameText.color = new Color(1f, 1f, 1f, 0.50f);
            nameText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) nameText.font = _fontAsset;

            // 分隔线
            var sep = CreateUIObject("Sep", _shopBarPanel.transform);
            var sepRect = sep.AddComponent<RectTransform>();
            sepRect.sizeDelta = new Vector2(1f, 50f);
            sep.AddComponent<Image>().color = DividerColor2;

            // 店铺按钮
            var shops = TownData.Shops;
            foreach (var shop in shops)
            {
                var btnObj = CreateUIObject($"ShopBtn_{shop.Id}", _shopBarPanel.transform);
                var btnRect = btnObj.AddComponent<RectTransform>();
                btnRect.sizeDelta = new Vector2(64f, 72f);

                var btnBg = btnObj.AddComponent<Image>();
                btnBg.color = ItemBgColor;
                btnBg.raycastTarget = true;

                var btnLayout = btnObj.AddComponent<VerticalLayoutGroup>();
                btnLayout.padding = new RectOffset(4, 4, 6, 4);
                btnLayout.spacing = 4f;
                btnLayout.childAlignment = TextAnchor.MiddleCenter;
                btnLayout.childControlWidth = false;
                btnLayout.childControlHeight = false;
                btnLayout.childForceExpandWidth = false;
                btnLayout.childForceExpandHeight = false;

                // 图标
                var iconObj = CreateUIObject("Icon", btnObj.transform);
                var iconText = iconObj.AddComponent<TextMeshProUGUI>();
                iconText.text = TownData.GetShopIcon(shop.Type);
                iconText.fontSize = 18;
                iconText.alignment = TextAlignmentOptions.Center;
                if (_fontAsset != null) iconText.font = _fontAsset;

                // 名称
                var nameObj = CreateUIObject("Name", btnObj.transform);
                var nameT = nameObj.AddComponent<TextMeshProUGUI>();
                nameT.text = shop.Name;
                nameT.fontSize = 10;
                nameT.color = new Color(1f, 1f, 1f, 0.70f);
                nameT.alignment = TextAlignmentOptions.Center;
                if (_fontAsset != null) nameT.font = _fontAsset;

                // 附近指示点
                var dotObj = CreateUIObject("NearbyDot", btnObj.transform);
                var dotRect = dotObj.AddComponent<RectTransform>();
                dotRect.anchorMin = new Vector2(1f, 1f);
                dotRect.anchorMax = new Vector2(1f, 1f);
                dotRect.pivot = new Vector2(1f, 1f);
                dotRect.anchoredPosition = new Vector2(2f, 2f);
                dotRect.sizeDelta = new Vector2(8f, 8f);
                var dotImg = dotObj.AddComponent<Image>();
                dotImg.color = NearbyDotColor;
                dotObj.SetActive(false);

                // 按钮事件
                var button = btnObj.AddComponent<Button>();
                button.targetGraphic = btnBg;
                var capturedShop = shop;
                button.onClick.AddListener(() => OpenShop(capturedShop));
            }

            _shopBarPanel.SetActive(false);
        }

        // ─── 店铺详情面板 ───

        private void CreateShopDetailPanel()
        {
            _shopDetailPanel = CreateUIObject("ShopDetail", transform);
            var rect = _shopDetailPanel.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(680f, 0f);
            rect.offsetMin = new Vector2(rect.offsetMin.x, 0f);

            var bg = _shopDetailPanel.AddComponent<Image>();
            bg.color = BgColor;

            var vLayout = _shopDetailPanel.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(20, 20, 12, 12);
            vLayout.spacing = 8f;
            vLayout.childAlignment = TextAnchor.LowerCenter;
            vLayout.childControlWidth = true;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true;
            vLayout.childForceExpandHeight = false;

            // 头部
            var headerObj = CreateUIObject("Header", _shopDetailPanel.transform);
            headerObj.AddComponent<LayoutElement>().preferredHeight = 48f;
            var hLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.spacing = 12f;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = false;

            var headerInfo = CreateUIObject("HeaderInfo", headerObj.transform);
            var infoVLayout = headerInfo.AddComponent<VerticalLayoutGroup>();
            infoVLayout.spacing = 2f;
            infoVLayout.childControlWidth = true;
            infoVLayout.childControlHeight = false;
            infoVLayout.childForceExpandWidth = true;
            infoVLayout.childForceExpandHeight = false;

            _shopDetailTitle = CreateUIObject("Title", headerInfo.transform).AddComponent<TextMeshProUGUI>();
            _shopDetailTitle.fontSize = 13;
            _shopDetailTitle.color = TextMainColor;
            _shopDetailTitle.alignment = TextAlignmentOptions.Left;
            if (_fontAsset != null) _shopDetailTitle.font = _fontAsset;

            _shopDetailDesc = CreateUIObject("Desc", headerInfo.transform).AddComponent<TextMeshProUGUI>();
            _shopDetailDesc.fontSize = 10;
            _shopDetailDesc.color = TextDimColor;
            _shopDetailDesc.alignment = TextAlignmentOptions.Left;
            if (_fontAsset != null) _shopDetailDesc.font = _fontAsset;

            // 关闭按钮
            var closeButton = CreateUIObject("CloseBtn", headerObj.transform);
            var closeRect = closeButton.AddComponent<RectTransform>();
            closeRect.sizeDelta = new Vector2(32f, 32f);
            closeButton.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
            var closeText = closeButton.AddComponent<TextMeshProUGUI>();
            closeText.text = "✕";
            closeText.fontSize = 14;
            closeText.color = new Color(1f, 1f, 1f, 0.50f);
            closeText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) closeText.font = _fontAsset;
            var closeBtn = closeButton.AddComponent<Button>();
            closeBtn.onClick.AddListener(CloseShop);

            // 分隔线
            CreateDivider(_shopDetailPanel.transform);

            // 店主对话
            var ownerObj = CreateUIObject("OwnerLine", _shopDetailPanel.transform);
            ownerObj.AddComponent<LayoutElement>().preferredHeight = 36f;
            var ownerVLayout = ownerObj.AddComponent<VerticalLayoutGroup>();
            ownerVLayout.spacing = 2f;
            ownerVLayout.childControlWidth = true;
            ownerVLayout.childControlHeight = false;
            ownerVLayout.childForceExpandWidth = true;
            ownerVLayout.childForceExpandHeight = false;

            var ownerLabel = CreateUIObject("OwnerLabel", ownerObj.transform).AddComponent<TextMeshProUGUI>();
            ownerLabel.text = "店主";
            ownerLabel.fontSize = 10;
            ownerLabel.color = new Color(1f, 1f, 1f, 0.45f);
            ownerLabel.alignment = TextAlignmentOptions.Left;
            if (_fontAsset != null) ownerLabel.font = _fontAsset;

            _shopDetailOwnerLine = CreateUIObject("OwnerText", ownerObj.transform).AddComponent<TextMeshProUGUI>();
            _shopDetailOwnerLine.fontSize = 11;
            _shopDetailOwnerLine.color = new Color(1f, 1f, 1f, 0.60f);
            _shopDetailOwnerLine.fontStyle = FontStyles.Italic;
            _shopDetailOwnerLine.alignment = TextAlignmentOptions.Left;
            _shopDetailOwnerLine.enableWordWrapping = true;
            if (_fontAsset != null) _shopDetailOwnerLine.font = _fontAsset;

            // 分隔线
            CreateDivider(_shopDetailPanel.transform);

            // 商品列表容器
            var listObj = CreateUIObject("ItemList", _shopDetailPanel.transform);
            var listLE = listObj.AddComponent<LayoutElement>();
            listLE.preferredHeight = 240f;
            listLE.flexibleHeight = 0f;
            var listVLayout = listObj.AddComponent<VerticalLayoutGroup>();
            listVLayout.spacing = 6f;
            listVLayout.padding = new RectOffset(4, 4, 4, 4);
            listVLayout.childControlWidth = true;
            listVLayout.childControlHeight = false;
            listVLayout.childForceExpandWidth = true;
            listVLayout.childForceExpandHeight = false;

            // Viewport + Content for ScrollRect
            var viewportObj = CreateUIObject("Viewport", listObj.transform);
            viewportObj.AddComponent<RectTransform>();
            viewportObj.AddComponent<Image>().color = Color.clear;
            viewportObj.AddComponent<Mask>().showMaskGraphic = false;

            var contentObj = CreateUIObject("Content", viewportObj.transform);
            var contentRect = contentObj.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero;
            contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;
            var contentLayout = contentObj.AddComponent<VerticalLayoutGroup>();
            contentLayout.spacing = 6f;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = false;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var listScroll = listObj.AddComponent<ScrollRect>();
            listScroll.viewport = viewportObj.GetComponent<RectTransform>();
            listScroll.content = contentRect;
            listScroll.horizontal = false;
            listScroll.vertical = true;
            _itemListContainer = contentObj.transform;

            // 分隔线
            CreateDivider(_shopDetailPanel.transform);

            // 库存栏
            var invObj = CreateUIObject("Inventory", _shopDetailPanel.transform);
            invObj.AddComponent<LayoutElement>().preferredHeight = 28f;
            var invLayout = invObj.AddComponent<HorizontalLayoutGroup>();
            invLayout.spacing = 12f;
            invLayout.childAlignment = TextAnchor.MiddleLeft;
            invLayout.childControlWidth = false;
            invLayout.childControlHeight = false;
            invLayout.childForceExpandWidth = false;
            invLayout.childForceExpandHeight = false;

            var invLabel = CreateUIObject("InvLabel", invObj.transform).AddComponent<TextMeshProUGUI>();
            invLabel.text = "库存";
            invLabel.fontSize = 9;
            invLabel.color = new Color(1f, 1f, 1f, 0.25f);
            if (_fontAsset != null) invLabel.font = _fontAsset;

            _inventoryText = CreateUIObject("InvValues", invObj.transform).AddComponent<TextMeshProUGUI>();
            _inventoryText.fontSize = 10;
            _inventoryText.color = TextMutedColor;
            _inventoryText.alignment = TextAlignmentOptions.Left;
            if (_fontAsset != null) _inventoryText.font = _fontAsset;

            _shopDetailPanel.SetActive(false);
        }

        // ─── Toast 容器 ───

        private void CreateToastContainer()
        {
            _toastContainer = CreateUIObject("ToastContainer", transform);
            var rect = _toastContainer.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0f);
            rect.anchorMax = new Vector2(0.5f, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(0f, 96f);
            rect.sizeDelta = new Vector2(400f, 200f);

            var vLayout = _toastContainer.AddComponent<VerticalLayoutGroup>();
            vLayout.spacing = 6f;
            vLayout.childAlignment = TextAnchor.LowerCenter;
            vLayout.childControlWidth = false;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = false;
            vLayout.childForceExpandHeight = false;
            vLayout.reverseArrangement = true;
        }

        // =================================================================
        // 店铺操作
        // =================================================================

        /// <summary>
        /// 打开指定店铺
        /// </summary>
        public void OpenShop(ShopDef shop)
        {
            _activeShop = shop;
            if (_shopDetailPanel != null)
            {
                RefreshShopDetail();
                _shopDetailPanel.SetActive(true);
            }
        }

        /// <summary>
        /// 关闭店铺面板
        /// </summary>
        public void CloseShop()
        {
            _activeShop = null;
            if (_shopDetailPanel != null)
                _shopDetailPanel.SetActive(false);
        }

        private void RefreshShopDetail()
        {
            if (_activeShop == null) return;
            var gm = GameManager.Instance;
            if (gm == null) return;

            // 更新头部
            if (_shopDetailTitle != null)
                _shopDetailTitle.text = $"{TownData.GetShopIcon(_activeShop.Type)} {_activeShop.Name}";
            if (_shopDetailDesc != null)
                _shopDetailDesc.text = _activeShop.Desc;
            if (_shopDetailOwnerLine != null)
                _shopDetailOwnerLine.text = $"「{_activeShop.OwnerLine}」";

            // 清空商品列表
            if (_itemListContainer != null)
            {
                for (int i = _itemListContainer.childCount - 1; i >= 0; i--)
                    Destroy(_itemListContainer.GetChild(i).gameObject);
            }

            // 填充商品
            var items = TownData.GetItemsByShopType(_activeShop.Type);
            if (items.Length == 0)
            {
                var emptyObj = CreateUIObject("Empty", _itemListContainer);
                var emptyText = emptyObj.AddComponent<TextMeshProUGUI>();
                emptyText.text = "暂无商品";
                emptyText.fontSize = 11;
                emptyText.color = new Color(1f, 1f, 1f, 0.30f);
                emptyText.alignment = TextAlignmentOptions.Center;
                if (_fontAsset != null) emptyText.font = _fontAsset;
            }
            else
            {
                foreach (var item in items)
                {
                    CreateItemRow(item);
                }
            }

            // 更新库存
            UpdateInventoryDisplay();
        }

        private void CreateItemRow(TownItem item)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            bool purchased = item.Unique && gm.PurchasedItems.Contains(item.Id);
            bool canAfford = !purchased && gm.CanAfford(item.Cost);

            var rowObj = CreateUIObject($"Item_{item.Id}", _itemListContainer);
            var rowLE = rowObj.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 48f;

            var rowBg = rowObj.AddComponent<Image>();
            rowBg.color = purchased
                ? new Color(0.31f, 0.78f, 0.47f, 0.05f)
                : ItemBgColor;

            var hLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(12, 12, 8, 8);
            hLayout.spacing = 10f;
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = true;
            hLayout.childControlHeight = false;
            hLayout.childForceExpandWidth = true;
            hLayout.childForceExpandHeight = false;

            // 名称+描述
            var infoObj = CreateUIObject("Info", rowObj.transform);
            var infoLayout = infoObj.AddComponent<VerticalLayoutGroup>();
            infoLayout.spacing = 2f;
            infoLayout.childControlWidth = true;
            infoLayout.childControlHeight = false;
            infoLayout.childForceExpandWidth = true;
            infoLayout.childForceExpandHeight = false;

            var nameText = CreateUIObject("Name", infoObj.transform).AddComponent<TextMeshProUGUI>();
            nameText.text = item.Name;
            nameText.fontSize = 11;
            nameText.color = purchased ? new Color(1f, 1f, 1f, 0.50f) : new Color(1f, 1f, 1f, 0.80f);
            nameText.alignment = TextAlignmentOptions.Left;
            if (_fontAsset != null) nameText.font = _fontAsset;

            var descText = CreateUIObject("Desc", infoObj.transform).AddComponent<TextMeshProUGUI>();
            descText.text = item.Desc;
            descText.fontSize = 9;
            descText.color = purchased ? new Color(1f, 1f, 1f, 0.20f) : TextDimColor;
            descText.alignment = TextAlignmentOptions.Left;
            if (_fontAsset != null) descText.font = _fontAsset;

            // 价格标签
            var priceObj = CreateUIObject("Price", rowObj.transform);
            var priceLE = priceObj.AddComponent<LayoutElement>();
            priceLE.minWidth = 120f;
            priceLE.preferredWidth = 180f;
            var priceLayout = priceObj.AddComponent<HorizontalLayoutGroup>();
            priceLayout.spacing = 4f;
            priceLayout.childAlignment = TextAnchor.MiddleRight;
            priceLayout.childControlWidth = false;
            priceLayout.childControlHeight = false;
            priceLayout.childForceExpandWidth = false;
            priceLayout.childForceExpandHeight = false;

            var cost = item.Cost;
            if (cost.Metal > 0) CreateResourceTag(priceObj.transform, ResourceKind.Metal, cost.Metal, !canAfford && gm.Resources.Metal < cost.Metal);
            if (cost.Wood > 0) CreateResourceTag(priceObj.transform, ResourceKind.Wood, cost.Wood, !canAfford && gm.Resources.Wood < cost.Wood);
            if (cost.Fuel > 0) CreateResourceTag(priceObj.transform, ResourceKind.Fuel, cost.Fuel, !canAfford && gm.Resources.Fuel < cost.Fuel);
            if (cost.Signal > 0) CreateResourceTag(priceObj.transform, ResourceKind.Signal, cost.Signal, !canAfford && gm.Resources.Signal < cost.Signal);
            if (cost.Crystal > 0) CreateResourceTag(priceObj.transform, ResourceKind.Crystal, cost.Crystal, !canAfford && gm.Resources.Crystal < cost.Crystal);

            // 购买按钮
            var btnObj = CreateUIObject("BuyBtn", rowObj.transform);
            var btnRect = btnObj.AddComponent<RectTransform>();
            btnRect.sizeDelta = new Vector2(56f, 28f);
            var btnImg = btnObj.AddComponent<Image>();
            var btnText = btnObj.AddComponent<TextMeshProUGUI>();

            if (purchased)
            {
                btnImg.color = BtnPurchasedBgColor;
                btnText.text = "已购";
                btnText.color = BtnPurchasedTextColor;
            }
            else if (canAfford)
            {
                btnImg.color = BtnBuyBgColor;
                btnText.text = "购买";
                btnText.color = AccentColor;
            }
            else
            {
                btnImg.color = BtnDisabledBgColor;
                btnText.text = "购买";
                btnText.color = BtnDisabledTextColor;
            }
            btnText.fontSize = 10;
            btnText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) btnText.font = _fontAsset;

            if (canAfford && !purchased)
            {
                var button = btnObj.AddComponent<Button>();
                button.targetGraphic = btnImg;
                var capturedItem = item;
                button.onClick.AddListener(() => HandleBuy(capturedItem));
            }
        }

        private void CreateResourceTag(Transform parent, ResourceKind kind, int amount, bool deficit)
        {
            var tagObj = CreateUIObject($"Tag_{kind}", parent);
            var tagRect = tagObj.AddComponent<RectTransform>();
            tagRect.sizeDelta = new Vector2(50f, 22f);

            var tagBg = tagObj.AddComponent<Image>();
            tagBg.color = deficit
                ? new Color(1f, 0.31f, 0.31f, 0.15f)
                : new Color(1f, 1f, 1f, 0.06f);

            var tagText = tagObj.AddComponent<TextMeshProUGUI>();
            var color = deficit ? ErrorColor : TownData.GetResourceColor(kind);
            tagText.text = $"{TownData.GetResourceShort(kind)} {amount}";
            tagText.fontSize = 10;
            tagText.color = color;
            tagText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) tagText.font = _fontAsset;
        }

        // =================================================================
        // 购买逻辑
        // =================================================================

        private void HandleBuy(TownItem item)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            // 检查是否已购买（唯一商品）
            if (item.Unique && gm.PurchasedItems.Contains(item.Id))
            {
                AddToast("已经购买过了", ToastType.Error);
                return;
            }

            // 检查资源
            if (!gm.CanAfford(item.Cost))
            {
                AddToast("资源不足", ToastType.Error);
                return;
            }

            // 扣减资源
            gm.SpendResources(item.Cost);

            // 应用效果
            ApplyItemEffect(item);

            // 记录购买
            if (item.Unique)
            {
                gm.AddPurchasedItem(item.Id);
            }

            AddToast($"购买成功：{item.Name}", ToastType.Success);

            // 刷新面板
            RefreshShopDetail();
        }

        /// <summary>
        /// 应用商品效果
        /// </summary>
        private void ApplyItemEffect(TownItem item)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            switch (item.Effect)
            {
                // 升级类
                case "engine_level_3":
                    if (gm.GetPartState(PartId.Engine).Level < 3)
                    {
                        gm.SetPartCondition(PartId.Engine, 1f);
                        var parts = gm.VehicleParts;
                        parts[(int)PartId.Engine].Level = 3;
                        gm.VehicleParts = parts;
                        AddToast("发动机已升级至 Lv.3", ToastType.Info);
                    }
                    break;

                case "body_level_2":
                    if (gm.GetPartState(PartId.Body).Level < 2)
                    {
                        var parts = gm.VehicleParts;
                        parts[(int)PartId.Body].Level = 2;
                        gm.VehicleParts = parts;
                        AddToast("车身已加固至 Lv.2", ToastType.Info);
                    }
                    break;

                case "radio_level_2":
                    if (gm.GetPartState(PartId.Radio).Level < 2)
                    {
                        var parts = gm.VehicleParts;
                        parts[(int)PartId.Radio].Level = 2;
                        gm.VehicleParts = parts;
                        AddToast("电台已升级至 Lv.2", ToastType.Info);
                    }
                    break;

                case "tank_level_2":
                    if (gm.GetPartState(PartId.Tank).Level < 2)
                    {
                        var parts = gm.VehicleParts;
                        parts[(int)PartId.Tank].Level = 2;
                        gm.VehicleParts = parts;
                        AddToast("油箱已扩容至 Lv.2", ToastType.Info);
                    }
                    break;

                // 全车大修
                case "full_repair":
                    {
                        var parts = gm.VehicleParts;
                        for (int i = 0; i < parts.Length; i++)
                            parts[i].Condition = 1f;
                        gm.VehicleParts = parts;
                        AddToast("所有部件已恢复至最佳状态", ToastType.Info);
                    }
                    break;

                // 资源类
                case "metal_5":
                    gm.AddResources(new ResourceBag { Metal = 5 });
                    break;
                case "wood_5":
                    gm.AddResources(new ResourceBag { Wood = 5 });
                    break;
                case "survival_kit":
                    gm.AddResources(new ResourceBag { Metal = 2, Wood = 2, Fuel = 2 });
                    break;
                case "fuel_3":
                    gm.AddResources(new ResourceBag { Fuel = 3 });
                    break;
                case "signal_2":
                    gm.AddResources(new ResourceBag { Signal = 2 });
                    break;
                case "crystal_1":
                    gm.AddResources(new ResourceBag { Crystal = 1 });
                    break;
                case "signal_3":
                    gm.AddResources(new ResourceBag { Signal = 3 });
                    break;
                case "fuel_fill":
                    gm.AddResources(new ResourceBag { Fuel = 10 });
                    AddToast("油箱已补满", ToastType.Info);
                    break;

                // 信息类
                case "rumor":
                    AddToast(TownData.GetRumor(), ToastType.Info);
                    break;
                case "old_map":
                    AddToast("你获得了一份旧地图碎片，上面标注着一条隐秘的路径", ToastType.Info);
                    break;

                default:
                    AddToast($"效果：{item.Effect}", ToastType.Info);
                    break;
            }
        }

        // =================================================================
        // Toast 通知
        // =================================================================

        private void AddToast(string message, ToastType type = ToastType.Success)
        {
            int id = ++_toastIdCounter;
            _toasts.Add(new ToastEntry
            {
                Id = id,
                Message = message,
                Type = type,
                ExpireTime = Time.unscaledTime + ToastDuration
            });
            CreateToastUI(id, message, type);
        }

        private void CreateToastUI(int id, string message, ToastType type)
        {
            if (_toastContainer == null) return;

            var toastObj = CreateUIObject($"Toast_{id}", _toastContainer.transform);
            var toastRect = toastObj.AddComponent<RectTransform>();
            toastRect.sizeDelta = new Vector2(360f, 32f);

            Color bgColor, textColor;
            switch (type)
            {
                case ToastType.Success:
                    bgColor = new Color(0.31f, 0.78f, 0.47f, 0.15f);
                    textColor = new Color(0.31f, 0.78f, 0.47f, 0.90f);
                    break;
                case ToastType.Error:
                    bgColor = new Color(1f, 0.31f, 0.31f, 0.15f);
                    textColor = new Color(1f, 0.31f, 0.31f, 0.90f);
                    break;
                default:
                    bgColor = new Color(0.47f, 0.67f, 1f, 0.15f);
                    textColor = new Color(0.47f, 0.67f, 1f, 0.90f);
                    break;
            }

            toastObj.AddComponent<Image>().color = bgColor;
            var toastText = toastObj.AddComponent<TextMeshProUGUI>();
            toastText.text = message;
            toastText.fontSize = 11;
            toastText.color = textColor;
            toastText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) toastText.font = _fontAsset;
        }

        private void UpdateToasts()
        {
            float now = Time.unscaledTime;
            for (int i = _toasts.Count - 1; i >= 0; i--)
            {
                if (now >= _toasts[i].ExpireTime)
                {
                    if (_toastContainer != null)
                    {
                        var child = _toastContainer.transform.Find($"Toast_{_toasts[i].Id}");
                        if (child != null) Destroy(child.gameObject);
                    }
                    _toasts.RemoveAt(i);
                }
            }
        }

        // =================================================================
        // 库存显示
        // =================================================================

        private void UpdateInventoryDisplay()
        {
            var gm = GameManager.Instance;
            if (gm == null || _inventoryText == null) return;

            var res = gm.Resources;
            _inventoryText.text = $"金 {res.Metal}  木 {res.Wood}  燃 {res.Fuel}  信 {res.Signal}  晶 {res.Crystal}";
        }

        // =================================================================
        // 辅助
        // =================================================================

        private void CreateDivider(Transform parent)
        {
            var divObj = CreateUIObject("Divider", parent);
            var divLE = divObj.AddComponent<LayoutElement>();
            divLE.preferredHeight = 1f;
            divLE.flexibleHeight = 0f;
            divObj.AddComponent<Image>().color = DividerColor;
        }

        private GameObject CreateUIObject(string name, Transform parent)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj;
        }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-845c9427346249ca8c9bd5b8240a19cb/cwd.txt'; exit "$__tr_native_ec"
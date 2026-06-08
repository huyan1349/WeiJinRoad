using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.Camp
{
    // ═══════════════════════════════════════════════════════════════
    // CookingGame — 营地烹饪小游戏
    //
    // 翻译自 CookingGame.tsx
    // 核心机制：
    //   - 从库存选择食材放入锅中（4格）
    //   - 食材组合匹配配方则可烹饪
    //   - 烹饪3秒进度条
    //   - 完成后恢复对应部件耐久度
    //   - 消耗营地资源
    //
    // 操作：点击食材添加 / 点击锅中食材移除 / 烹饪按钮
    // ═══════════════════════════════════════════════════════════════

    public class CookingResult
    {
        public bool Success;
        public string RecipeName;
        public string PartRepaired;
        public float ConditionBonus;
    }

    [Serializable]
    public class Recipe
    {
        public string Id;
        public string Name;
        public string Icon;
        public Dictionary<ResourceKind, int> Ingredients = new Dictionary<ResourceKind, int>();
        public string TargetPart;
        public float ConditionBonus;
        public string Description;
    }

    public class SlotContent
    {
        public ResourceKind Kind;
        public int Amount;
    }

    public class CookingGame : MonoBehaviour
    {
        private const int SlotCount = 4;
        private const float CookDuration = 3f;
        private const int RecipePageSize = 2;

        private static readonly Color BgColor          = new Color(0.05f, 0.06f, 0.08f, 0.95f);
        private static readonly Color TextDimColor      = new Color(1f, 1f, 1f, 0.25f);
        private static readonly Color TextNormalColor   = new Color(1f, 1f, 1f, 0.80f);
        private static readonly Color TextFaintColor    = new Color(1f, 1f, 1f, 0.15f);
        private static readonly Color SlotEmptyBg       = new Color(1f, 1f, 1f, 0.03f);
        private static readonly Color SlotFilledBg      = new Color(1f, 1f, 1f, 0.08f);
        private static readonly Color BtnDisabledBg     = new Color(1f, 1f, 1f, 0.03f);
        private static readonly Color BtnDisabledText   = new Color(1f, 1f, 1f, 0.15f);
        private static readonly Color BtnCookBg         = new Color(1f, 0.62f, 0.16f, 0.15f);
        private static readonly Color BtnCookText       = new Color(1f, 0.78f, 0.31f, 0.80f);
        private static readonly Color BtnClearBg        = new Color(1f, 1f, 1f, 0.05f);
        private static readonly Color BtnClearText      = new Color(1f, 1f, 1f, 0.50f);
        private static readonly Color MatchedColor      = new Color(0.31f, 0.86f, 0.55f, 0.60f);
        private static readonly Color ResultBg          = new Color(0.31f, 0.86f, 0.55f, 0.08f);
        private static readonly Color ResultText        = new Color(0.31f, 0.86f, 0.55f, 0.60f);
        private static readonly Color PotBodyColor      = new Color(0.31f, 0.25f, 0.20f, 0.80f);
        private static readonly Color PotInnerColor     = new Color(0.16f, 0.12f, 0.08f, 0.80f);
        private static readonly Color PotHandleColor    = new Color(0.39f, 0.31f, 0.24f, 0.60f);
        private static readonly Color RecipeBg          = new Color(1f, 1f, 1f, 0.05f);
        private static readonly Color RecipeDisabledBg  = new Color(1f, 1f, 1f, 0.02f);

        private const string ZpixFontPath = "Fonts/zpix";

        private static readonly Dictionary<ResourceKind, (string label, string shortName, Color color)> RMeta = new()
        {
            { ResourceKind.Metal,   ("金属", "金", new Color(0.75f, 0.75f, 0.80f, 1f)) },
            { ResourceKind.Wood,    ("木材", "木", new Color(0.65f, 0.45f, 0.25f, 1f)) },
            { ResourceKind.Fuel,    ("燃料", "油", new Color(0.90f, 0.70f, 0.20f, 1f)) },
            { ResourceKind.Signal,  ("信号件", "信", new Color(0.40f, 0.70f, 0.95f, 1f)) },
            { ResourceKind.Crystal, ("光源晶", "晶", new Color(0.60f, 0.85f, 0.95f, 1f)) },
        };

        private static readonly Dictionary<PartId, string> PartLabels = new()
        {
            { PartId.Engine, "发动机" }, { PartId.Tires, "轮胎" }, { PartId.Headlight, "探照灯" },
            { PartId.Tank, "油箱" }, { PartId.Body, "车身" }, { PartId.Radio, "电台" },
        };

        private static readonly Recipe[] Recipes = new Recipe[]
        {
            new Recipe { Id = "hot_soup", Name = "热汤", Icon = "🍲",
                Ingredients = new Dictionary<ResourceKind, int> {{ ResourceKind.Wood, 1 }, { ResourceKind.Metal, 1 }},
                TargetPart = "Engine", ConditionBonus = 0.1f, Description = "恢复发动机 +0.1" },
            new Recipe { Id = "roast_meat", Name = "烤肉", Icon = "🍖",
                Ingredients = new Dictionary<ResourceKind, int> {{ ResourceKind.Wood, 1 }, { ResourceKind.Fuel, 1 }},
                TargetPart = "Tires", ConditionBonus = 0.1f, Description = "恢复轮胎 +0.1" },
            new Recipe { Id = "warm_tea", Name = "暖茶", Icon = "🍵",
                Ingredients = new Dictionary<ResourceKind, int> {{ ResourceKind.Wood, 1 }, { ResourceKind.Crystal, 1 }},
                TargetPart = "Headlight", ConditionBonus = 0.1f, Description = "恢复探照灯 +0.1" },
            new Recipe { Id = "signal_soup", Name = "信号汤", Icon = "📡",
                Ingredients = new Dictionary<ResourceKind, int> {{ ResourceKind.Signal, 1 }, { ResourceKind.Wood, 1 }},
                TargetPart = "Radio", ConditionBonus = 0.1f, Description = "恢复电台 +0.1" },
            new Recipe { Id = "big_stew", Name = "大杂烩", Icon = "🥘",
                Ingredients = new Dictionary<ResourceKind, int> {{ ResourceKind.Metal, 1 }, { ResourceKind.Wood, 2 }},
                TargetPart = "all", ConditionBonus = 0.03f, Description = "所有部件 +0.03" },
        };

        private SlotContent[] _slots = new SlotContent[SlotCount];
        private bool _cooking;
        private float _cookProgress;
        private CookingResult _cookResult;
        private int _recipePage;
        private bool _finished;

        private Canvas _canvas;
        private GameObject _panel;
        private TMP_FontAsset _fontAsset;

        private List<TMP_Text> _ingredientCounts = new List<TMP_Text>();
        private TMP_Text[] _slotLabels = new TMP_Text[SlotCount];
        private Image[] _slotImages = new Image[SlotCount];
        private TMP_Text _matchPreview;
        private Image _cookProgressBar;
        private TMP_Text _cookProgressLabel;
        private GameObject _progressContainer;
        private Button _cookBtn;
        private TMP_Text _cookBtnText;
        private Button _clearBtn;
        private GameObject _resultPanel;
        private TMP_Text _resultNameText;
        private TMP_Text _resultBonusText;
        private List<GameObject> _recipeCards = new List<GameObject>();
        private TMP_Text _pageLabel;

        public bool IsFinished => _finished;

        private void Update()
        {
            if (_cooking)
            {
                _cookProgress += Time.deltaTime / CookDuration;
                if (_cookProgress >= 1f) { _cookProgress = 1f; CompleteCooking(); }
                RefreshCookProgress();
            }
        }

        private void OnDestroy() { GameEvents.OnResourcesChanged -= OnResourcesChanged; }

        public void ShowGame()
        {
            _slots = new SlotContent[SlotCount];
            _cooking = false; _cookProgress = 0f; _cookResult = null;
            _recipePage = 0; _finished = false;
            CreateUI();
            GameEvents.OnResourcesChanged += OnResourcesChanged;
        }

        public void HideGame() { if (_panel != null) _panel.SetActive(false); }

        private void AddIngredient(ResourceKind kind)
        {
            if (_cooking || _finished) return;
            int emptyIdx = -1;
            for (int i = 0; i < SlotCount; i++) if (_slots[i] == null) { emptyIdx = i; break; }
            if (emptyIdx == -1) return;
            var gm = GameManager.Instance;
            if (gm == null || gm.Resources[kind] < 1) return;
            _slots[emptyIdx] = new SlotContent { Kind = kind, Amount = 1 };
            RefreshUI();
        }

        private void RemoveIngredient(int idx)
        {
            if (_cooking || _finished || idx < 0 || idx >= SlotCount || _slots[idx] == null) return;
            _slots[idx] = null;
            RefreshUI();
        }

        private void ClearPot()
        {
            if (_cooking || _finished) return;
            for (int i = 0; i < SlotCount; i++) _slots[i] = null;
            RefreshUI();
        }

        private Recipe CheckRecipe()
        {
            var pot = new Dictionary<ResourceKind, int>();
            int filled = 0;
            foreach (var slot in _slots)
            {
                if (slot == null) continue;
                filled++;
                if (!pot.ContainsKey(slot.Kind)) pot[slot.Kind] = 0;
                pot[slot.Kind] += slot.Amount;
            }
            if (filled == 0) return null;

            foreach (var recipe in Recipes)
            {
                if (recipe.Ingredients.Count != pot.Count) continue;
                bool match = true;
                foreach (var kvp in recipe.Ingredients)
                {
                    if (!pot.ContainsKey(kvp.Key) || Mathf.Abs(pot[kvp.Key] - kvp.Value) > 0.01f)
                    { match = false; break; }
                }
                if (match) return recipe;
            }
            return null;
        }

        private void StartCooking()
        {
            if (_cooking || _finished) return;
            var recipe = CheckRecipe();
            if (recipe == null) return;

            var cost = new ResourceBag();
            foreach (var slot in _slots)
            {
                if (slot == null) continue;
                switch (slot.Kind)
                {
                    case ResourceKind.Metal: cost.Metal += slot.Amount; break;
                    case ResourceKind.Wood: cost.Wood += slot.Amount; break;
                    case ResourceKind.Fuel: cost.Fuel += slot.Amount; break;
                    case ResourceKind.Signal: cost.Signal += slot.Amount; break;
                    case ResourceKind.Crystal: cost.Crystal += slot.Amount; break;
                }
            }
            var gm = GameManager.Instance;
            if (gm == null || !gm.SpendResources(cost)) return;
            _cooking = true; _cookProgress = 0f;
            if (_progressContainer != null) _progressContainer.SetActive(true);
            RefreshUI();
        }

        private void CompleteCooking()
        {
            _cooking = false;
            var recipe = CheckRecipe();
            if (recipe == null) return;

            var gm = GameManager.Instance;
            if (gm != null)
            {
                if (recipe.TargetPart == "all")
                {
                    foreach (PartId pid in Enum.GetValues(typeof(PartId)))
                    {
                        float cur = gm.GetPartState(pid).Condition;
                        gm.SetPartCondition(pid, Mathf.Min(1f, cur + recipe.ConditionBonus));
                    }
                }
                else if (Enum.TryParse<PartId>(recipe.TargetPart, out var pid))
                {
                    float cur = gm.GetPartState(pid).Condition;
                    gm.SetPartCondition(pid, Mathf.Min(1f, cur + recipe.ConditionBonus));
                }
            }

            _cookResult = new CookingResult
            {
                Success = true, RecipeName = recipe.Name,
                PartRepaired = recipe.TargetPart, ConditionBonus = recipe.ConditionBonus
            };

            Invoke(nameof(FinishGame), 2f);
            RefreshUI();
        }

        private void FinishGame() { if (!_finished) _finished = true; }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            EnsureCanvas();

            _panel = CreateUIObject("CookingGamePanel", _canvas.transform);
            var pRect = _panel.AddComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero; pRect.anchorMax = Vector2.one;
            pRect.offsetMin = Vector2.zero; pRect.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = BgColor;

            // 标题
            var tObj = CreateUIObject("Title", _panel.transform);
            var tRect = tObj.AddComponent<RectTransform>();
            AnchorCenter(tRect); tRect.anchoredPosition = new Vector2(0f, 340f); tRect.sizeDelta = new Vector2(200f, 24f);
            var tText = tObj.AddComponent<TextMeshProUGUI>();
            tText.text = "营地烹饪"; tText.fontSize = 12; tText.color = TextDimColor;
            tText.alignment = TextAlignmentOptions.Center; tText.characterSpacing = 4f; SetFont(tText);

            // 三栏
            var cols = CreateUIObject("Columns", _panel.transform);
            var cRect = cols.AddComponent<RectTransform>();
            AnchorCenter(cRect); cRect.anchoredPosition = Vector2.zero; cRect.sizeDelta = new Vector2(700f, 500f);
            var cLayout = cols.AddComponent<HorizontalLayoutGroup>();
            cLayout.spacing = 24f; cLayout.childAlignment = TextAnchor.MiddleCenter;
            cLayout.childControlWidth = true; cLayout.childForceExpandWidth = true;

            CreateIngredientList(cols.transform);
            CreatePotArea(cols.transform);
            CreateRecipeBook(cols.transform);
        }

        private void CreateIngredientList(Transform parent)
        {
            var left = CreateUIObject("Ingredients", parent);
            var lLayout = left.AddComponent<VerticalLayoutGroup>();
            lLayout.padding = new RectOffset(4, 4, 4, 4); lLayout.spacing = 4f;
            lLayout.childAlignment = TextAnchor.UpperCenter;
            lLayout.childControlWidth = true; lLayout.childForceExpandWidth = true;
            var le = left.AddComponent<LayoutElement>(); le.preferredWidth = 160f; le.flexibleWidth = 0f;

            var label = CreateUIObject("IngLabel", left.transform).AddComponent<TextMeshProUGUI>();
            label.text = "库存食材"; label.fontSize = 9; label.color = TextDimColor;
            label.alignment = TextAlignmentOptions.Center; label.characterSpacing = 2f; SetFont(label);
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

            _ingredientCounts.Clear();
            foreach (ResourceKind kind in Enum.GetValues(typeof(ResourceKind)))
            {
                var meta = RMeta[kind];
                var btnObj = CreateUIObject($"Ing_{kind}", left.transform);
                btnObj.AddComponent<Image>().color = new Color(1f,1f,1f,0.05f);
                var btn = btnObj.AddComponent<Button>();
                ResourceKind captured = kind;
                btn.onClick.AddListener(() => AddIngredient(captured));
                btnObj.AddComponent<LayoutElement>().preferredHeight = 32f;

                var hLayout = btnObj.AddComponent<HorizontalLayoutGroup>();
                hLayout.padding = new RectOffset(8, 8, 4, 4); hLayout.spacing = 6f;
                hLayout.childAlignment = TextAnchor.MiddleLeft;
                hLayout.childControlWidth = true; hLayout.childForceExpandWidth = true;

                var dotObj = CreateUIObject("Dot", btnObj.transform);
                dotObj.AddComponent<Image>().color = meta.color;
                var dl = dotObj.AddComponent<LayoutElement>(); dl.preferredWidth = 8f; dl.preferredHeight = 8f;

                var nameObj = CreateUIObject("Name", btnObj.transform);
                var nText = nameObj.AddComponent<TextMeshProUGUI>();
                nText.text = meta.label; nText.fontSize = 10;
                nText.color = new Color(1f,1f,1f,0.60f); nText.alignment = TextAlignmentOptions.MidlineLeft; SetFont(nText);
                nameObj.AddComponent<LayoutElement>().flexibleWidth = 1f;

                var countObj = CreateUIObject("Count", btnObj.transform);
                var cText = countObj.AddComponent<TextMeshProUGUI>();
                cText.fontSize = 9; cText.color = new Color(1f,1f,1f,0.40f);
                cText.alignment = TextAlignmentOptions.MidlineRight; SetFont(cText);
                var cl = countObj.AddComponent<LayoutElement>(); cl.preferredWidth = 30f;
                _ingredientCounts.Add(cText);
            }
        }

        private void CreatePotArea(Transform parent)
        {
            var center = CreateUIObject("Pot", parent);
            var cvLayout = center.AddComponent<VerticalLayoutGroup>();
            cvLayout.spacing = 8f; cvLayout.childAlignment = TextAnchor.MiddleCenter;
            cvLayout.childControlWidth = true; cvLayout.childForceExpandWidth = true;
            var ce = center.AddComponent<LayoutElement>(); ce.preferredWidth = 240f; ce.flexibleWidth = 1f;

            // 锅体
            var potObj = CreateUIObject("PotBody", center.transform);
            potObj.AddComponent<LayoutElement>().preferredHeight = 140f;

            var bodyObj = CreateUIObject("Body", potObj.transform);
            var bRect = bodyObj.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.1f, 0f); bRect.anchorMax = new Vector2(0.9f, 0.6f);
            bRect.offsetMin = Vector2.zero; bRect.offsetMax = Vector2.zero;
            bodyObj.AddComponent<Image>().color = PotBodyColor;

            var innerObj = CreateUIObject("Inner", potObj.transform);
            var iRect = innerObj.AddComponent<RectTransform>();
            iRect.anchorMin = new Vector2(0.15f, 0.15f); iRect.anchorMax = new Vector2(0.85f, 0.55f);
            iRect.offsetMin = Vector2.zero; iRect.offsetMax = Vector2.zero;
            innerObj.AddComponent<Image>().color = PotInnerColor;

            var h1 = CreateUIObject("Handle1", potObj.transform);
            var h1Rect = h1.AddComponent<RectTransform>();
            h1Rect.anchorMin = new Vector2(0f, 0.3f); h1Rect.anchorMax = new Vector2(0.12f, 0.35f);
            h1Rect.offsetMin = Vector2.zero; h1Rect.offsetMax = Vector2.zero;
            h1.AddComponent<Image>().color = PotHandleColor;

            var h2 = CreateUIObject("Handle2", potObj.transform);
            var h2Rect = h2.AddComponent<RectTransform>();
            h2Rect.anchorMin = new Vector2(0.88f, 0.3f); h2Rect.anchorMax = new Vector2(1f, 0.35f);
            h2Rect.offsetMin = Vector2.zero; h2Rect.offsetMax = Vector2.zero;
            h2.AddComponent<Image>().color = PotHandleColor;

            // 格子
            var slotContainer = CreateUIObject("Slots", potObj.transform);
            var sRect = slotContainer.AddComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.2f, 0.55f); sRect.anchorMax = new Vector2(0.8f, 0.85f);
            sRect.offsetMin = Vector2.zero; sRect.offsetMax = Vector2.zero;
            var sLayout = slotContainer.AddComponent<HorizontalLayoutGroup>();
            sLayout.spacing = 6f; sLayout.childAlignment = TextAnchor.MiddleCenter;
            sLayout.childControlWidth = true; sLayout.childForceExpandWidth = true;

            for (int i = 0; i < SlotCount; i++)
            {
                int ci = i;
                var slotObj = CreateUIObject($"Slot_{i}", slotContainer.transform);
                _slotImages[i] = slotObj.AddComponent<Image>();
                _slotImages[i].color = SlotEmptyBg;
                var slotBtn = slotObj.AddComponent<Button>();
                slotBtn.onClick.AddListener(() => RemoveIngredient(ci));
                slotObj.AddComponent<LayoutElement>().preferredHeight = 40f;
                _slotLabels[i] = slotObj.AddComponent<TextMeshProUGUI>();
                _slotLabels[i].fontSize = 8; _slotLabels[i].color = new Color(1f,1f,1f,0.40f);
                _slotLabels[i].alignment = TextAlignmentOptions.Center; SetFont(_slotLabels[i]);
            }

            // 匹配预览
            _matchPreview = CreateUIObject("MatchPreview", center.transform).AddComponent<TextMeshProUGUI>();
            _matchPreview.fontSize = 10; _matchPreview.color = TextFaintColor;
            _matchPreview.alignment = TextAlignmentOptions.Center; SetFont(_matchPreview);
            _matchPreview.gameObject.AddComponent<LayoutElement>().preferredHeight = 24f;

            // 进度条
            _progressContainer = CreateUIObject("ProgressContainer", center.transform);
            var pcLayout = _progressContainer.AddComponent<VerticalLayoutGroup>();
            pcLayout.spacing = 2f; pcLayout.childAlignment = TextAnchor.MiddleCenter;
            pcLayout.childControlWidth = true; pcLayout.childForceExpandWidth = true;

            var progressBar = CreateUIObject("ProgressBar", _progressContainer.transform);
            progressBar.AddComponent<Image>().color = new Color(1f,1f,1f,0.06f);
            progressBar.AddComponent<LayoutElement>().preferredHeight = 8f;

            var progressFill = CreateUIObject("ProgressFill", progressBar.transform);
            var pfRect = progressFill.AddComponent<RectTransform>();
            pfRect.anchorMin = Vector2.zero; pfRect.anchorMax = new Vector2(0f, 1f);
            pfRect.offsetMin = Vector2.zero; pfRect.offsetMax = Vector2.zero;
            _cookProgressBar = progressFill.AddComponent<Image>();
            _cookProgressBar.color = new Color(1f, 0.62f, 0.16f, 0.80f);

            _cookProgressLabel = CreateUIObject("ProgressLabel", _progressContainer.transform).AddComponent<TextMeshProUGUI>();
            _cookProgressLabel.fontSize = 8; _cookProgressLabel.color = new Color(1f,1f,1f,0.20f);
            _cookProgressLabel.alignment = TextAlignmentOptions.Center; SetFont(_cookProgressLabel);
            _cookProgressLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 14f;
            _progressContainer.SetActive(false);

            // 按钮
            var btnRow = CreateUIObject("BtnRow", center.transform);
            var brLayout = btnRow.AddComponent<HorizontalLayoutGroup>();
            brLayout.spacing = 8f; brLayout.childAlignment = TextAnchor.MiddleCenter;
            brLayout.childControlWidth = true; brLayout.childForceExpandWidth = true;

            var cookObj = CreateUIObject("CookBtn", btnRow.transform);
            cookObj.AddComponent<Image>().color = BtnDisabledBg;
            _cookBtn = cookObj.AddComponent<Button>();
            _cookBtn.onClick.AddListener(StartCooking);
            var cbl = cookObj.AddComponent<LayoutElement>(); cbl.preferredHeight = 36f; cbl.flexibleWidth = 2f;
            _cookBtnText = cookObj.AddComponent<TextMeshProUGUI>();
            _cookBtnText.text = "烹饪"; _cookBtnText.fontSize = 12; _cookBtnText.color = BtnDisabledText;
            _cookBtnText.alignment = TextAlignmentOptions.Center; _cookBtnText.characterSpacing = 2f; SetFont(_cookBtnText);

            var clearObj = CreateUIObject("ClearBtn", btnRow.transform);
            clearObj.AddComponent<Image>().color = BtnClearBg;
            _clearBtn = clearObj.AddComponent<Button>();
            _clearBtn.onClick.AddListener(ClearPot);
            clearObj.AddComponent<LayoutElement>().preferredHeight = 36f;
            var clearText = clearObj.AddComponent<TextMeshProUGUI>();
            clearText.text = "清空"; clearText.fontSize = 12; clearText.color = BtnClearText;
            clearText.alignment = TextAlignmentOptions.Center; SetFont(clearText);

            // 结果
            _resultPanel = CreateUIObject("ResultPanel", center.transform);
            _resultPanel.AddComponent<Image>().color = ResultBg;
            var rpLayout = _resultPanel.AddComponent<VerticalLayoutGroup>();
            rpLayout.padding = new RectOffset(12, 12, 8, 8); rpLayout.spacing = 4f;
            rpLayout.childAlignment = TextAnchor.MiddleCenter;
            rpLayout.childControlWidth = true; rpLayout.childForceExpandWidth = true;

            _resultNameText = CreateUIObject("ResultName", _resultPanel.transform).AddComponent<TextMeshProUGUI>();
            _resultNameText.fontSize = 10; _resultNameText.color = ResultText;
            _resultNameText.alignment = TextAlignmentOptions.Center; _resultNameText.characterSpacing = 2f; SetFont(_resultNameText);

            _resultBonusText = CreateUIObject("ResultBonus", _resultPanel.transform).AddComponent<TextMeshProUGUI>();
            _resultBonusText.fontSize = 9; _resultBonusText.color = new Color(1f,1f,1f,0.40f);
            _resultBonusText.alignment = TextAlignmentOptions.Center; SetFont(_resultBonusText);

            _resultPanel.SetActive(false);
        }

        private void CreateRecipeBook(Transform parent)
        {
            var right = CreateUIObject("RecipeBook", parent);
            var rLayout = right.AddComponent<VerticalLayoutGroup>();
            rLayout.padding = new RectOffset(4, 4, 4, 4); rLayout.spacing = 6f;
            rLayout.childAlignment = TextAnchor.UpperCenter;
            rLayout.childControlWidth = true; rLayout.childForceExpandWidth = true;
            var re = right.AddComponent<LayoutElement>(); re.preferredWidth = 180f; re.flexibleWidth = 0f;

            var label = CreateUIObject("RecipeLabel", right.transform).AddComponent<TextMeshProUGUI>();
            label.text = "配方书"; label.fontSize = 9; label.color = TextDimColor;
            label.alignment = TextAlignmentOptions.Center; label.characterSpacing = 2f; SetFont(label);
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 20f;

            var cardContainer = CreateUIObject("RecipeCards", right.transform);
            var ccLayout = cardContainer.AddComponent<VerticalLayoutGroup>();
            ccLayout.spacing = 6f; ccLayout.childAlignment = TextAnchor.UpperCenter;
            ccLayout.childControlWidth = true; ccLayout.childForceExpandWidth = true;

            _recipeCards.Clear();
            for (int i = 0; i < RecipePageSize; i++)
            {
                var card = CreateUIObject($"RecipeCard_{i}", cardContainer.transform);
                card.AddComponent<Image>().color = RecipeBg;
                var cLayout = card.AddComponent<VerticalLayoutGroup>();
                cLayout.padding = new RectOffset(8, 8, 6, 6); cLayout.spacing = 3f;
                cLayout.childAlignment = TextAnchor.UpperLeft;
                cLayout.childControlWidth = true; cLayout.childForceExpandWidth = true;
                card.AddComponent<LayoutElement>().preferredHeight = 80f;

                // 名称行
                var nameRow = CreateUIObject("NameRow", card.transform);
                var nrLayout = nameRow.AddComponent<HorizontalLayoutGroup>();
                nrLayout.spacing = 4f; nrLayout.childAlignment = TextAnchor.MiddleLeft;
                nrLayout.childControlWidth = true; nrLayout.childForceExpandWidth = true;

                var iconText = CreateUIObject("Icon", nameRow.transform).AddComponent<TextMeshProUGUI>();
                iconText.fontSize = 12; iconText.alignment = TextAlignmentOptions.MidlineLeft; SetFont(iconText);
                iconText.gameObject.AddComponent<LayoutElement>().preferredWidth = 20f;

                var nameText = CreateUIObject("RName", nameRow.transform).AddComponent<TextMeshProUGUI>();
                nameText.fontSize = 10; nameText.alignment = TextAlignmentOptions.MidlineLeft; SetFont(nameText);
                nameText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

                // 食材行
                var ingRow = CreateUIObject("IngRow", card.transform);
                var irLayout = ingRow.AddComponent<HorizontalLayoutGroup>();
                irLayout.spacing = 4f; irLayout.childAlignment = TextAnchor.MiddleLeft;
                irLayout.childControlWidth = false;

                var ingText = CreateUIObject("IngText", ingRow.transform).AddComponent<TextMeshProUGUI>();
                ingText.fontSize = 8; ingText.alignment = TextAlignmentOptions.MidlineLeft; SetFont(ingText);
                ingText.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1f;

                // 描述
                var descText = CreateUIObject("Desc", card.transform).AddComponent<TextMeshProUGUI>();
                descText.fontSize = 8; descText.color = new Color(1f,1f,1f,0.30f);
                descText.alignment = TextAlignmentOptions.MidlineLeft; SetFont(descText);

                _recipeCards.Add(card);
            }

            // 翻页
            var pageRow = CreateUIObject("PageRow", right.transform);
            var prLayout = pageRow.AddComponent<HorizontalLayoutGroup>();
            prLayout.spacing = 8f; prLayout.childAlignment = TextAnchor.MiddleCenter;
            prLayout.childControlWidth = false;

            var prevBtn = CreateUIObject("PrevPage", pageRow.transform).AddComponent<Button>();
            var ppText = prevBtn.gameObject.AddComponent<TextMeshProUGUI>();
            ppText.text = "◀"; ppText.fontSize = 9; ppText.color = new Color(1f,1f,1f,0.30f);
            ppText.alignment = TextAlignmentOptions.Center; SetFont(ppText);
            prevBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 24f;
            prevBtn.onClick.AddListener(() => { _recipePage = Mathf.Max(0, _recipePage - 1); RefreshRecipeBook(); });

            _pageLabel = CreateUIObject("PageLabel", pageRow.transform).AddComponent<TextMeshProUGUI>();
            _pageLabel.fontSize = 8; _pageLabel.color = new Color(1f,1f,1f,0.20f);
            _pageLabel.alignment = TextAlignmentOptions.Center; SetFont(_pageLabel);
            _pageLabel.gameObject.AddComponent<LayoutElement>().preferredWidth = 40f;

            var nextBtn = CreateUIObject("NextPage", pageRow.transform).AddComponent<Button>();
            var npText = nextBtn.gameObject.AddComponent<TextMeshProUGUI>();
            npText.text = "▶"; npText.fontSize = 9; npText.color = new Color(1f,1f,1f,0.30f);
            npText.alignment = TextAlignmentOptions.Center; SetFont(npText);
            nextBtn.gameObject.AddComponent<LayoutElement>().preferredWidth = 24f;
            nextBtn.onClick.AddListener(() => {
                int maxPage = Mathf.CeilToInt((float)Recipes.Length / RecipePageSize) - 1;
                _recipePage = Mathf.Min(maxPage, _recipePage + 1); RefreshRecipeBook();
            });

            RefreshRecipeBook();
        }

        private void RefreshUI()
        {
            var gm = GameManager.Instance;

            // 食材数量
            int idx = 0;
            foreach (ResourceKind kind in Enum.GetValues(typeof(ResourceKind)))
            {
                if (idx < _ingredientCounts.Count && gm != null)
                    _ingredientCounts[idx].text = gm.Resources[kind].ToString();
                idx++;
            }

            // 格子
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] != null)
                {
                    _slotImages[i].color = SlotFilledBg;
                    var meta = RMeta[_slots[i].Kind];
                    _slotLabels[i].text = meta.shortName;
                    _slotLabels[i].color = meta.color;
                }
                else
                {
                    _slotImages[i].color = SlotEmptyBg;
                    _slotLabels[i].text = "";
                }
            }

            // 匹配预览
            var matched = CheckRecipe();
            if (_matchPreview != null)
            {
                if (matched != null)
                {
                    _matchPreview.text = $"{matched.Icon} {matched.Name} — {matched.Description}";
                    _matchPreview.color = MatchedColor;
                }
                else
                {
                    bool hasAny = false;
                    foreach (var s in _slots) if (s != null) hasAny = true;
                    _matchPreview.text = hasAny ? "未匹配配方" : "放入食材开始烹饪";
                    _matchPreview.color = TextFaintColor;
                }
            }

            // 按钮
            if (_cookBtn != null)
            {
                bool canCook = matched != null && !_cooking && !_finished;
                _cookBtn.interactable = canCook;
                _cookBtn.GetComponent<Image>().color = canCook ? BtnCookBg : BtnDisabledBg;
                if (_cookBtnText != null) _cookBtnText.color = canCook ? BtnCookText : BtnDisabledText;
            }

            if (_clearBtn != null)
            {
                bool hasAny = false;
                foreach (var s in _slots) if (s != null) hasAny = true;
                _clearBtn.interactable = hasAny && !_cooking && !_finished;
            }

            // 结果
            if (_cookResult != null && _resultPanel != null)
            {
                _resultPanel.SetActive(true);
                if (_resultNameText != null) _resultNameText.text = $"{_cookResult.RecipeName} 完成！";
                if (_resultBonusText != null)
                {
                    if (_cookResult.PartRepaired == "all")
                        _resultBonusText.text = $"所有部件 +{_cookResult.ConditionBonus}";
                    else if (Enum.TryParse<PartId>(_cookResult.PartRepaired, out var pid) && PartLabels.TryGetValue(pid, out var lbl))
                        _resultBonusText.text = $"{lbl} +{_cookResult.ConditionBonus}";
                    else
                        _resultBonusText.text = $"+{_cookResult.ConditionBonus}";
                }
            }

            RefreshRecipeBook();
        }

        private void RefreshCookProgress()
        {
            if (_cookProgressBar != null)
                _cookProgressBar.rectTransform.anchorMax = new Vector2(_cookProgress, 1f);
            if (_cookProgressLabel != null)
                _cookProgressLabel.text = $"烹饪中... {Mathf.RoundToInt(_cookProgress * 100f)}%";
        }

        private void RefreshRecipeBook()
        {
            int maxPage = Mathf.CeilToInt((float)Recipes.Length / RecipePageSize) - 1;
            if (_pageLabel != null) _pageLabel.text = $"{_recipePage + 1}/{maxPage + 1}";

            var gm = GameManager.Instance;
            for (int i = 0; i < RecipePageSize; i++)
            {
                int recipeIdx = _recipePage * RecipePageSize + i;
                if (i >= _recipeCards.Count) break;

                var card = _recipeCards[i];
                var iconText = card.transform.Find("NameRow/Icon")?.GetComponent<TextMeshProUGUI>();
                var nameText = card.transform.Find("NameRow/RName")?.GetComponent<TextMeshProUGUI>();
                var ingText = card.transform.Find("IngRow/IngText")?.GetComponent<TextMeshProUGUI>();
                var descText = card.transform.Find("Desc")?.GetComponent<TextMeshProUGUI>();

                if (recipeIdx < Recipes.Length)
                {
                    var recipe = Recipes[recipeIdx];
                    bool canAfford = true;
                    if (gm != null)
                    {
                        foreach (var kvp in recipe.Ingredients)
                            if (gm.Resources[kvp.Key] < kvp.Value) canAfford = false;
                    }

                    if (iconText != null) iconText.text = recipe.Icon;
                    if (nameText != null) { nameText.text = recipe.Name; nameText.color = canAfford ? new Color(1f,1f,1f,0.60f) : new Color(1f,1f,1f,0.25f); }
                    if (ingText != null)
                    {
                        string ingStr = "";
                        foreach (var kvp in recipe.Ingredients)
                        {
                            var meta = RMeta[kvp.Key];
                            ingStr += $"{meta.shortName}×{kvp.Value} ";
                        }
                        ingText.text = ingStr.Trim();
                    }
                    if (descText != null) descText.text = recipe.Description;
                    card.GetComponent<Image>().color = canAfford ? RecipeBg : RecipeDisabledBg;
                    card.SetActive(true);
                }
                else
                {
                    card.SetActive(false);
                }
            }
        }

        private void OnResourcesChanged(ResourceBag resources) { RefreshUI(); }

        private void EnsureCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null)
            {
                _canvas = gameObject.AddComponent<Canvas>();
                _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 900;
                var scaler = gameObject.GetComponent<CanvasScaler>();
                if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
                if (gameObject.GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
            }
        }

        private GameObject CreateUIObject(string name, Transform parent)
        { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }

        private void AnchorCenter(RectTransform rect)
        { rect.anchorMin = new Vector2(0.5f, 0.5f); rect.anchorMax = new Vector2(0.5f, 0.5f); rect.pivot = new Vector2(0.5f, 0.5f); }

        private void SetFont(TMP_Text tmp) { if (_fontAsset != null) tmp.font = _fontAsset; }
    }
}

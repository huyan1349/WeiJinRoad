using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.Camp
{
    // ═══════════════════════════════════════════════════════════════
    // StarMap — 星图观测小游戏
    //
    // 翻译自 StarMap.tsx
    // 核心机制：
    //   - 交互式夜空显示
    //   - 按顺序点击星点连线识别星座
    //   - 发现星座获得光源晶和叙事碎片
    //   - 60秒倒计时
    //
    // 操作：点击星点连线 / 点击星座名称查看提示
    // ═══════════════════════════════════════════════════════════════

    public class StarMapResult
    {
        public bool Success;
        public int DiscoveredCount;
        public int CrystalReward;
    }

    [Serializable]
    public class Star
    {
        public int Id;
        public float X;
        public float Y;
        public float Size;
        public float Brightness;
    }

    [Serializable]
    public class Constellation
    {
        public string Id;
        public string Name;
        public int[] StarIds;
        public string Hint;
        public bool Discovered;
    }

    public class StarMap : MonoBehaviour
    {
        private const float GameDuration = 60f;
        private const int BackgroundStarCount = 54;

        private static readonly Color SkyMidColor     = new Color(0.04f, 0.06f, 0.16f, 1f);
        private static readonly Color TextDimColor     = new Color(1f, 1f, 1f, 0.30f);
        private static readonly Color TextNormalColor  = new Color(1f, 1f, 1f, 0.80f);
        private static readonly Color StarDefaultColor = new Color(0.78f, 0.86f, 1f, 0.70f);
        private static readonly Color StarBgColor      = new Color(0.71f, 0.78f, 0.94f, 0.25f);
        private static readonly Color StarSelectedColor= new Color(0.73f, 0.92f, 1f, 0.90f);
        private static readonly Color LineColor        = new Color(0.47f, 0.78f, 1f, 0.40f);
        private static readonly Color LineSelColor     = new Color(0.73f, 0.92f, 1f, 0.50f);
        private static readonly Color GreenDim         = new Color(0.31f, 0.86f, 0.55f, 0.40f);
        private static readonly Color BlueDim          = new Color(0.47f, 0.78f, 1f, 0.50f);
        private static readonly Color HintBgColor      = new Color(0f, 0f, 0f, 0.50f);
        private static readonly Color ResultBg         = new Color(0f, 0f, 0f, 0.60f);

        private const string ZpixFontPath = "Fonts/zpix";

        // 星座预设
        private static readonly CData[] CPresets = new CData[]
        {
            new CData { Id = "traveler", Name = "旅人", StarIds = new int[]{0,1,2,3,4,5}, Hint = "一个行走的人形，头朝北方" },
            new CData { Id = "road", Name = "长路", StarIds = new int[]{6,7,8,9,10,11,12}, Hint = "一条蜿蜒的路径，从西到东" },
            new CData { Id = "beacon", Name = "灯塔", StarIds = new int[]{13,14,15,16,17}, Hint = "一座高塔，顶端有光" },
            new CData { Id = "compass", Name = "罗盘", StarIds = new int[]{18,19,20,21,22,23,24,25}, Hint = "四方向对称的圆环" },
        };

        private static readonly Vector2[] CPositions = new Vector2[]
        {
            new Vector2(0.25f,0.80f), new Vector2(0.27f,0.72f), new Vector2(0.23f,0.65f),
            new Vector2(0.25f,0.58f), new Vector2(0.28f,0.52f), new Vector2(0.22f,0.52f),
            new Vector2(0.40f,0.45f), new Vector2(0.45f,0.48f), new Vector2(0.50f,0.46f),
            new Vector2(0.55f,0.50f), new Vector2(0.60f,0.47f), new Vector2(0.65f,0.52f), new Vector2(0.70f,0.50f),
            new Vector2(0.82f,0.35f), new Vector2(0.82f,0.42f), new Vector2(0.82f,0.50f),
            new Vector2(0.82f,0.57f), new Vector2(0.85f,0.62f),
            new Vector2(0.50f,0.85f), new Vector2(0.55f,0.82f), new Vector2(0.58f,0.78f),
            new Vector2(0.55f,0.73f), new Vector2(0.50f,0.70f), new Vector2(0.45f,0.73f),
            new Vector2(0.42f,0.78f), new Vector2(0.45f,0.82f),
        };

        private struct CData { public string Id; public string Name; public int[] StarIds; public string Hint; }

        private List<Star> _stars = new List<Star>();
        private List<Constellation> _constellations = new List<Constellation>();
        private List<int> _selectedStars = new List<int>();
        private string _activeConstellation;
        private float _timeLeft = GameDuration;
        private bool _finished;

        private Canvas _canvas;
        private GameObject _panel;
        private TMP_FontAsset _fontAsset;
        private TMP_Text _timeLeftText;
        private TMP_Text _discoveredCountText;
        private GameObject _resultOverlay;
        private TMP_Text _resultDiscoveredText;
        private TMP_Text _resultCrystalText;
        private TMP_Text _resultFlavorText;

        private Dictionary<int, GameObject> _starObjects = new Dictionary<int, GameObject>();
        private GameObject _lineContainer;
        private List<GameObject> _activeLines = new List<GameObject>();
        private List<Tuple<TMP_Text, Constellation>> _hintButtons = new List<Tuple<TMP_Text, Constellation>>();

        public bool IsFinished => _finished;

        private void Update()
        {
            if (_finished) return;
            _timeLeft -= Time.deltaTime;
            if (_timeLeft <= 0f) { _timeLeft = 0f; FinishGame(); return; }
            if (_timeLeftText != null) _timeLeftText.text = Mathf.CeilToInt(_timeLeft) + "s";
        }

        public void ShowGame()
        {
            _timeLeft = GameDuration; _finished = false;
            _selectedStars.Clear(); _activeConstellation = null;
            GenerateStars(); CreateUI();
        }

        public void HideGame() { if (_panel != null) _panel.SetActive(false); }

        private void GenerateStars()
        {
            _stars.Clear(); _constellations.Clear();
            for (int i = 0; i < CPositions.Length; i++)
                _stars.Add(new Star { Id = i, X = CPositions[i].x, Y = CPositions[i].y,
                    Size = 2f + UnityEngine.Random.value, Brightness = 0.7f + UnityEngine.Random.value * 0.3f });

            for (int i = CPositions.Length; i < CPositions.Length + BackgroundStarCount; i++)
                _stars.Add(new Star { Id = i, X = UnityEngine.Random.value, Y = UnityEngine.Random.value,
                    Size = 0.5f + UnityEngine.Random.value * 1.5f, Brightness = 0.2f + UnityEngine.Random.value * 0.5f });

            foreach (var p in CPresets)
                _constellations.Add(new Constellation { Id = p.Id, Name = p.Name, StarIds = p.StarIds, Hint = p.Hint, Discovered = false });
        }

        private void HandleStarClick(int starId)
        {
            if (_finished || _selectedStars.Contains(starId)) return;
            _selectedStars.Add(starId);

            foreach (var c in _constellations)
            {
                if (c.Discovered) continue;
                bool match = true;
                for (int i = 0; i < _selectedStars.Count; i++)
                {
                    if (_selectedStars[i] != c.StarIds[i]) { match = false; break; }
                }
                if (!match) continue;

                if (_selectedStars.Count == c.StarIds.Length) { DiscoverConstellation(c); return; }
                _activeConstellation = c.Id;
                UpdateStarVisuals(); UpdateLines(); return;
            }

            _selectedStars.Clear(); _activeConstellation = null;
            UpdateStarVisuals(); UpdateLines();
        }

        private void DiscoverConstellation(Constellation c)
        {
            c.Discovered = true;
            var gm = GameManager.Instance;
            if (gm != null)
            {
                gm.AddResources(new ResourceBag { Crystal = 1 });
                gm.DiscoverFragment($"starmap_{c.Id}");
                gm.AddJournalEntry(new JournalEntry
                {
                    Id = $"starmap_{c.Id}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}",
                    FragmentId = $"starmap_{c.Id}", Chapter = "ch1", Title = $"星座: {c.Name}",
                    Content = $"夜空中发现了{c.Name}星座。{c.Hint}", CarrierType = "starmap",
                    Biome = "camp", DiscoveredAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), LocationName = "营地星图"
                });
            }
            _selectedStars.Clear(); _activeConstellation = null;
            UpdateStarVisuals(); UpdateLines(); UpdateHintButtons(); UpdateDiscoveredCount();
        }

        private void SelectConstellationHint(string constellationId)
        {
            var c = _constellations.Find(x => x.Id == constellationId);
            if (c == null || c.Discovered) return;
            _activeConstellation = constellationId;
            _selectedStars.Clear(); _selectedStars.Add(c.StarIds[0]);
            UpdateStarVisuals(); UpdateLines();
        }

        private void FinishGame()
        {
            if (_finished) return; _finished = true;
            int discovered = 0;
            foreach (var c in _constellations) if (c.Discovered) discovered++;
            if (_resultOverlay != null)
            {
                _resultOverlay.SetActive(true);
                if (_resultDiscoveredText != null) _resultDiscoveredText.text = $"发现星座: {discovered}/{_constellations.Count}";
                if (_resultCrystalText != null) _resultCrystalText.text = $"获得光源晶: +{discovered}";
                if (_resultFlavorText != null) { _resultFlavorText.gameObject.SetActive(discovered > 0); _resultFlavorText.text = "星光照亮了旅途的记忆"; }
            }
        }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            EnsureCanvas();

            _panel = CreateUIObject("StarMapPanel", _canvas.transform);
            var pRect = _panel.AddComponent<RectTransform>();
            pRect.anchorMin = Vector2.zero; pRect.anchorMax = Vector2.one;
            pRect.offsetMin = Vector2.zero; pRect.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = SkyMidColor;

            // 星空
            var starField = CreateUIObject("StarField", _panel.transform);
            var sfRect = starField.AddComponent<RectTransform>();
            sfRect.anchorMin = Vector2.zero; sfRect.anchorMax = Vector2.one;
            sfRect.offsetMin = Vector2.zero; sfRect.offsetMax = Vector2.zero;

            _lineContainer = CreateUIObject("LineContainer", starField.transform);
            var lcRect = _lineContainer.AddComponent<RectTransform>();
            lcRect.anchorMin = Vector2.zero; lcRect.anchorMax = Vector2.one;
            lcRect.offsetMin = Vector2.zero; lcRect.offsetMax = Vector2.zero;

            _starObjects.Clear();
            foreach (var star in _stars)
            {
                bool isCStar = star.Id < 26;
                var sObj = CreateUIObject($"Star_{star.Id}", starField.transform);
                var sRect = sObj.AddComponent<RectTransform>();
                sRect.anchorMin = new Vector2(star.X, star.Y);
                sRect.anchorMax = new Vector2(star.X, star.Y);
                sRect.pivot = new Vector2(0.5f, 0.5f);
                float sz = isCStar ? star.Size * 4f : star.Size * 3f;
                sRect.sizeDelta = new Vector2(sz, sz);
                sRect.anchoredPosition = Vector2.zero;
                var sImg = sObj.AddComponent<Image>();
                sImg.color = isCStar
                    ? new Color(StarDefaultColor.r, StarDefaultColor.g, StarDefaultColor.b, star.Brightness)
                    : new Color(StarBgColor.r, StarBgColor.g, StarBgColor.b, star.Brightness * 0.5f);
                sImg.raycastTarget = isCStar;

                if (isCStar)
                {
                    var btn = sObj.AddComponent<Button>();
                    int cid = star.Id;
                    btn.onClick.AddListener(() => HandleStarClick(cid));
                }
                _starObjects[star.Id] = sObj;
            }

            // 标题
            var tObj = CreateUIObject("Title", _panel.transform);
            var tRect = tObj.AddComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.5f, 1f); tRect.anchorMax = new Vector2(0.5f, 1f);
            tRect.pivot = new Vector2(0.5f, 1f); tRect.anchoredPosition = new Vector2(0f, -12f);
            tRect.sizeDelta = new Vector2(200f, 20f);
            var tText = tObj.AddComponent<TextMeshProUGUI>();
            tText.text = "星图观测"; tText.fontSize = 10; tText.color = TextDimColor;
            tText.alignment = TextAlignmentOptions.Center; tText.characterSpacing = 4f; SetFont(tText);

            // 倒计时
            var timeObj = CreateUIObject("TimeLeft", _panel.transform);
            var tlRect = timeObj.AddComponent<RectTransform>();
            tlRect.anchorMin = new Vector2(1f, 1f); tlRect.anchorMax = new Vector2(1f, 1f);
            tlRect.pivot = new Vector2(1f, 1f); tlRect.anchoredPosition = new Vector2(-16f, -14f);
            tlRect.sizeDelta = new Vector2(60f, 16f);
            _timeLeftText = timeObj.AddComponent<TextMeshProUGUI>();
            _timeLeftText.fontSize = 9; _timeLeftText.color = TextDimColor;
            _timeLeftText.alignment = TextAlignmentOptions.MiddleRight; SetFont(_timeLeftText);

            // 底部信息
            CreateBottomInfo();

            // 提示
            var hObj = CreateUIObject("Hint", _panel.transform);
            var hRect = hObj.AddComponent<RectTransform>();
            hRect.anchorMin = new Vector2(0.5f, 0f); hRect.anchorMax = new Vector2(0.5f, 0f);
            hRect.pivot = new Vector2(0.5f, 0f); hRect.anchoredPosition = new Vector2(0f, 4f);
            hRect.sizeDelta = new Vector2(400f, 14f);
            var hText = hObj.AddComponent<TextMeshProUGUI>();
            hText.text = "点击星座名称查看提示 · 按顺序点击星点连线";
            hText.fontSize = 7; hText.color = new Color(1f,1f,1f,0.10f);
            hText.alignment = TextAlignmentOptions.Center; hText.characterSpacing = 2f; SetFont(hText);

            // 结算
            CreateResultOverlay();
        }

        private void CreateBottomInfo()
        {
            var bottom = CreateUIObject("BottomInfo", _panel.transform);
            var bRect = bottom.AddComponent<RectTransform>();
            bRect.anchorMin = new Vector2(0.5f, 0f); bRect.anchorMax = new Vector2(0.5f, 0f);
            bRect.pivot = new Vector2(0.5f, 0f); bRect.anchoredPosition = new Vector2(0f, 20f);
            bRect.sizeDelta = new Vector2(500f, 80f);
            var bLayout = bottom.AddComponent<HorizontalLayoutGroup>();
            bLayout.spacing = 16f; bLayout.childAlignment = TextAnchor.MiddleCenter;
            bLayout.childControlWidth = true; bLayout.childForceExpandWidth = false;

            // 已发现
            var dObj = CreateUIObject("DiscoveredInfo", bottom.transform);
            dObj.AddComponent<Image>().color = HintBgColor;
            var dLayout = dObj.AddComponent<VerticalLayoutGroup>();
            dLayout.padding = new RectOffset(16, 16, 8, 8); dLayout.spacing = 4f;
            dLayout.childAlignment = TextAnchor.MiddleCenter;
            dLayout.childControlWidth = true; dLayout.childForceExpandWidth = true;

            var dLabel = CreateUIObject("DLabel", dObj.transform).AddComponent<TextMeshProUGUI>();
            dLabel.text = "已发现星座"; dLabel.fontSize = 9; dLabel.color = TextDimColor;
            dLabel.alignment = TextAlignmentOptions.Center; dLabel.characterSpacing = 2f; SetFont(dLabel);
            _discoveredCountText = CreateUIObject("DCount", dObj.transform).AddComponent<TextMeshProUGUI>();
            _discoveredCountText.fontSize = 14; _discoveredCountText.color = TextNormalColor;
            _discoveredCountText.alignment = TextAlignmentOptions.Center; SetFont(_discoveredCountText);

            // 提示列表
            var hObj = CreateUIObject("HintList", bottom.transform);
            hObj.AddComponent<Image>().color = HintBgColor;
            var hLayout = hObj.AddComponent<VerticalLayoutGroup>();
            hLayout.padding = new RectOffset(12, 12, 6, 6); hLayout.spacing = 2f;
            hLayout.childAlignment = TextAnchor.UpperLeft;
            hLayout.childControlWidth = true; hLayout.childForceExpandWidth = true;

            var hlLabel = CreateUIObject("HLLabel", hObj.transform).AddComponent<TextMeshProUGUI>();
            hlLabel.text = "星座提示"; hlLabel.fontSize = 9; hlLabel.color = TextDimColor;
            hlLabel.alignment = TextAlignmentOptions.Center; hlLabel.characterSpacing = 2f; SetFont(hlLabel);

            _hintButtons.Clear();
            foreach (var c in _constellations)
            {
                var hBtnObj = CreateUIObject($"Hint_{c.Id}", hObj.transform);
                var btn = hBtnObj.AddComponent<Button>();
                string cid = c.Id;
                btn.onClick.AddListener(() => SelectConstellationHint(cid));
                var hText = hBtnObj.AddComponent<TextMeshProUGUI>();
                hText.fontSize = 9; hText.alignment = TextAlignmentOptions.MiddleLeft; SetFont(hText);
                _hintButtons.Add(Tuple.Create(hText, c));
            }

            UpdateHintButtons(); UpdateDiscoveredCount();
        }

        private void CreateResultOverlay()
        {
            _resultOverlay = CreateUIObject("ResultOverlay", _panel.transform);
            var roRect = _resultOverlay.AddComponent<RectTransform>();
            roRect.anchorMin = Vector2.zero; roRect.anchorMax = Vector2.one;
            roRect.offsetMin = Vector2.zero; roRect.offsetMax = Vector2.zero;
            _resultOverlay.AddComponent<Image>().color = ResultBg;

            var roLayout = _resultOverlay.AddComponent<VerticalLayoutGroup>();
            roLayout.childAlignment = TextAnchor.MiddleCenter;
            roLayout.childControlWidth = true; roLayout.childForceExpandWidth = true; roLayout.spacing = 6f;

            var inner = CreateUIObject("ResultInner", _resultOverlay.transform);
            inner.AddComponent<Image>().color = new Color(1f,1f,1f,0.05f);
            var iLayout = inner.AddComponent<VerticalLayoutGroup>();
            iLayout.padding = new RectOffset(24, 24, 20, 20); iLayout.spacing = 6f;
            iLayout.childAlignment = TextAnchor.MiddleCenter;
            iLayout.childControlWidth = true; iLayout.childForceExpandWidth = true;

            var rLabel = CreateUIObject("RLabel", inner.transform).AddComponent<TextMeshProUGUI>();
            rLabel.text = "观 测 结 束"; rLabel.fontSize = 10; rLabel.color = TextDimColor;
            rLabel.alignment = TextAlignmentOptions.Center; rLabel.characterSpacing = 4f; SetFont(rLabel);

            _resultDiscoveredText = CreateUIObject("RDisc", inner.transform).AddComponent<TextMeshProUGUI>();
            _resultDiscoveredText.fontSize = 12; _resultDiscoveredText.color = new Color(1f,1f,1f,0.60f);
            _resultDiscoveredText.alignment = TextAlignmentOptions.Center; SetFont(_resultDiscoveredText);

            _resultCrystalText = CreateUIObject("RCryst", inner.transform).AddComponent<TextMeshProUGUI>();
            _resultCrystalText.fontSize = 12; _resultCrystalText.color = new Color(1f,1f,1f,0.60f);
            _resultCrystalText.alignment = TextAlignmentOptions.Center; SetFont(_resultCrystalText);

            _resultFlavorText = CreateUIObject("RFlavor", inner.transform).AddComponent<TextMeshProUGUI>();
            _resultFlavorText.fontSize = 9; _resultFlavorText.color = BlueDim;
            _resultFlavorText.alignment = TextAlignmentOptions.Center; _resultFlavorText.characterSpacing = 2f; SetFont(_resultFlavorText);

            _resultOverlay.SetActive(false);
        }

        private void UpdateStarVisuals()
        {
            foreach (var star in _stars)
            {
                if (!_starObjects.TryGetValue(star.Id, out var obj)) continue;
                var img = obj.GetComponent<Image>();
                if (img == null) continue;
                bool sel = _selectedStars.Contains(star.Id);
                bool isCStar = star.Id < 26;
                if (sel) { img.color = StarSelectedColor; obj.transform.localScale = Vector3.one * 1.5f; }
                else if (isCStar) { img.color = new Color(StarDefaultColor.r, StarDefaultColor.g, StarDefaultColor.b, star.Brightness); obj.transform.localScale = Vector3.one; }
            }
        }

        private void UpdateLines()
        {
            foreach (var line in _activeLines) Destroy(line);
            _activeLines.Clear();

            foreach (var c in _constellations)
            {
                if (!c.Discovered) continue;
                for (int i = 0; i < c.StarIds.Length - 1; i++)
                {
                    var s1 = _stars.Find(s => s.Id == c.StarIds[i]);
                    var s2 = _stars.Find(s => s.Id == c.StarIds[i + 1]);
                    if (s1 != null && s2 != null) DrawLine(s1.X, s1.Y, s2.X, s2.Y, LineColor);
                }
            }

            if (_selectedStars.Count > 1)
            {
                for (int i = 0; i < _selectedStars.Count - 1; i++)
                {
                    var s1 = _stars.Find(s => s.Id == _selectedStars[i]);
                    var s2 = _stars.Find(s => s.Id == _selectedStars[i + 1]);
                    if (s1 != null && s2 != null) DrawLine(s1.X, s1.Y, s2.X, s2.Y, LineSelColor);
                }
            }
        }

        private void DrawLine(float x1, float y1, float x2, float y2, Color color)
        {
            if (_lineContainer == null) return;
            var lineObj = CreateUIObject("Line", _lineContainer.transform);
            var lRect = lineObj.AddComponent<RectTransform>();
            lRect.anchorMin = Vector2.zero; lRect.anchorMax = Vector2.one;
            lRect.offsetMin = Vector2.zero; lRect.offsetMax = Vector2.zero;
            lineObj.AddComponent<Image>().color = color;

            Vector2 start = new Vector2(x1, y1);
            Vector2 end = new Vector2(x2, y2);
            Vector2 mid = (start + end) / 2f;
            float distance = Vector2.Distance(start, end);
            float angle = Mathf.Atan2(end.y - start.y, end.x - start.x) * Mathf.Rad2Deg;

            lRect.anchorMin = mid; lRect.anchorMax = mid;
            lRect.pivot = new Vector2(0.5f, 0.5f);
            lRect.sizeDelta = new Vector2(distance * 1920f, 2f);
            lRect.anchoredPosition = Vector2.zero;
            lRect.localEulerAngles = new Vector3(0f, 0f, angle);

            _activeLines.Add(lineObj);
        }

        private void UpdateHintButtons()
        {
            foreach (var tuple in _hintButtons)
            {
                var text = tuple.Item1; var c = tuple.Item2;
                if (c.Discovered) { text.text = $"✓ {c.Name}"; text.color = GreenDim; }
                else if (_activeConstellation == c.Id) { text.text = $"○ {c.Name} — {c.Hint}"; text.color = new Color(1f,1f,1f,0.70f); }
                else { text.text = $"○ {c.Name}"; text.color = new Color(1f,1f,1f,0.30f); }
            }
        }

        private void UpdateDiscoveredCount()
        {
            int d = 0; foreach (var c in _constellations) if (c.Discovered) d++;
            if (_discoveredCountText != null) _discoveredCountText.text = $"{d} / {_constellations.Count}";
        }

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

        private void SetFont(TMP_Text tmp) { if (_fontAsset != null) tmp.font = _fontAsset; }
    }
}

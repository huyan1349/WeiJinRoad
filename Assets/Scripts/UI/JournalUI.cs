using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;

namespace WeiJinRoad.UI
{
    public class JournalUI : MonoBehaviour
    {
        private const string ZpixFontPath = "Fonts/zpix";
        private static readonly string[] ChapterNames = { "ch1", "ch2", "ch3", "ch4", "ch5" };
        private static readonly string[] ChapterLabels = { "第一章", "第二章", "第三章", "第四章", "第五章" };
        private static readonly Color BgColor = new Color(0.06f, 0.07f, 0.10f, 0.92f);
        private static readonly Color PanelColor = new Color(0.10f, 0.11f, 0.15f, 0.90f);
        private static readonly Color TextColor = new Color(0.90f, 0.92f, 0.96f, 1f);
        private static readonly Color AccentColor = new Color(0.40f, 0.65f, 0.95f, 1f);
        private static readonly Color TabActiveColor = new Color(0.25f, 0.30f, 0.40f, 1f);
        private static readonly Color TabInactiveColor = new Color(0.12f, 0.13f, 0.17f, 0.90f);
        private static readonly Color EntryBgColor = new Color(0.12f, 0.13f, 0.17f, 0.80f);
        private static readonly Color CloseBtnColor = new Color(0.70f, 0.25f, 0.25f, 1f);

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;
        private GameObject _panel;
        private Transform _entryContainer;
        private int _currentChapter = 0;

        private void OnEnable()
        {
            CreateUI();
            GameEvents.OnFragmentDiscovered += OnFragmentDiscovered;
            GameEvents.OnJournalOverlayVisibilityChanged += OnJournalVisibilityChanged;
        }

        private void OnDisable()
        {
            GameEvents.OnFragmentDiscovered -= OnFragmentDiscovered;
            GameEvents.OnJournalOverlayVisibilityChanged -= OnJournalVisibilityChanged;
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.J)) ToggleVisibility();
            if (Input.GetKeyDown(KeyCode.Escape) && _panel != null && _panel.activeSelf) Hide();
        }

        private void OnFragmentDiscovered(string fragmentId) { RefreshEntries(); }
        private void OnJournalVisibilityChanged(bool visible) { if (visible) Show(); else Hide(); }

        public void ToggleVisibility()
        {
            if (_panel == null) return;
            if (_panel.activeSelf) Hide(); else Show();
        }

        public void Show()
        {
            if (_panel == null) return;
            RefreshEntries(); _panel.SetActive(true);
            var gm = GameManager.Instance; if (gm != null) gm.SetJournalOverlayVisible(true);
        }

        public void Hide()
        {
            if (_panel == null) return; _panel.SetActive(false);
            var gm = GameManager.Instance; if (gm != null) gm.SetJournalOverlayVisible(false);
        }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            CreateCanvas();
            CreatePanel();
            Debug.Log("[JournalUI] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 500;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CreatePanel()
        {
            _panel = CreateUIObject("JournalPanel", transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero; panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = BgColor;

            var contentArea = CreateUIObject("ContentArea", _panel.transform);
            var contentRect = contentArea.AddComponent<RectTransform>();
            contentRect.anchorMin = new Vector2(0.15f, 0.05f); contentRect.anchorMax = new Vector2(0.85f, 0.95f);
            contentRect.offsetMin = Vector2.zero; contentRect.offsetMax = Vector2.zero;
            contentArea.AddComponent<Image>().color = PanelColor;

            // Header
            var headerObj = CreateUIObject("Header", contentArea.transform);
            var headerRect = headerObj.AddComponent<RectTransform>();
            headerRect.anchorMin = new Vector2(0f, 1f); headerRect.anchorMax = new Vector2(1f, 1f);
            headerRect.pivot = new Vector2(0.5f, 1f);
            headerRect.offsetMin = new Vector2(0f, -50f); headerRect.offsetMax = new Vector2(0f, 0f);
            var headerHLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
            headerHLayout.padding = new RectOffset(15, 15, 10, 10); headerHLayout.spacing = 10f;
            headerHLayout.childAlignment = TextAnchor.MiddleCenter;
            headerHLayout.childControlWidth = true; headerHLayout.childControlHeight = false;
            headerHLayout.childForceExpandWidth = true; headerHLayout.childForceExpandHeight = false;

            var titleObj = CreateUIObject("Title", headerObj.transform);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "日 志  [J 关闭]"; titleText.fontSize = 24; titleText.fontStyle = FontStyles.Bold;
            titleText.color = TextColor; titleText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) titleText.font = _fontAsset;
            titleObj.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var closeBtnObj = CreateUIObject("CloseBtn", headerObj.transform);
            closeBtnObj.AddComponent<Image>().color = CloseBtnColor;
            var closeBtn = closeBtnObj.AddComponent<Button>();
            closeBtn.onClick.AddListener(Hide);
            closeBtnObj.AddComponent<LayoutElement>().preferredWidth = 35f;
            var closeBtnText = closeBtnObj.AddComponent<TextMeshProUGUI>();
            closeBtnText.text = "✕"; closeBtnText.fontSize = 18; closeBtnText.color = TextColor;
            closeBtnText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) closeBtnText.font = _fontAsset;

            // Chapter tabs
            var tabObj = CreateUIObject("ChapterTabs", contentArea.transform);
            var tabRect = tabObj.AddComponent<RectTransform>();
            tabRect.anchorMin = new Vector2(0f, 1f); tabRect.anchorMax = new Vector2(1f, 1f);
            tabRect.pivot = new Vector2(0.5f, 1f);
            tabRect.offsetMin = new Vector2(0f, -90f); tabRect.offsetMax = new Vector2(0f, -55f);
            var tabHLayout = tabObj.AddComponent<HorizontalLayoutGroup>();
            tabHLayout.padding = new RectOffset(10, 10, 5, 5); tabHLayout.spacing = 5f;
            tabHLayout.childAlignment = TextAnchor.MiddleCenter;
            tabHLayout.childControlWidth = true; tabHLayout.childControlHeight = false;
            tabHLayout.childForceExpandWidth = true; tabHLayout.childForceExpandHeight = false;

            for (int i = 0; i < ChapterNames.Length; i++)
            {
                int idx = i;
                var tabBtnObj = CreateUIObject("Tab_" + ChapterNames[i], tabObj.transform);
                tabBtnObj.AddComponent<Image>().color = i == 0 ? TabActiveColor : TabInactiveColor;
                var tabBtn = tabBtnObj.AddComponent<Button>();
                tabBtn.onClick.AddListener(() => SelectChapter(idx));
                tabBtnObj.AddComponent<LayoutElement>().preferredHeight = 28f;
                var tabBtnText = tabBtnObj.AddComponent<TextMeshProUGUI>();
                tabBtnText.text = ChapterLabels[i]; tabBtnText.fontSize = 14;
                tabBtnText.color = i == 0 ? AccentColor : TextColor;
                tabBtnText.alignment = TextAlignmentOptions.Center;
                if (_fontAsset != null) tabBtnText.font = _fontAsset;
            }

            // Entry list
            var listObj = CreateUIObject("EntryList", contentArea.transform);
            var listRect = listObj.AddComponent<RectTransform>();
            listRect.anchorMin = Vector2.zero; listRect.anchorMax = Vector2.one;
            listRect.offsetMin = new Vector2(10f, 10f); listRect.offsetMax = new Vector2(-10f, -95f);
            var listVLayout = listObj.AddComponent<VerticalLayoutGroup>();
            listVLayout.padding = new RectOffset(5, 5, 5, 5); listVLayout.spacing = 8f;
            listVLayout.childAlignment = TextAnchor.UpperCenter;
            listVLayout.childControlWidth = true; listVLayout.childControlHeight = false;
            listVLayout.childForceExpandWidth = true; listVLayout.childForceExpandHeight = false;
            listObj.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _entryContainer = listObj.transform;

            _panel.SetActive(false);
        }

        private void SelectChapter(int index)
        {
            _currentChapter = index;
            RefreshEntries();
            var tabObj = _panel.transform.Find("ContentArea/ChapterTabs");
            if (tabObj == null) return;
            for (int i = 0; i < tabObj.childCount; i++)
            {
                var child = tabObj.GetChild(i);
                var img = child.GetComponent<Image>();
                var txt = child.GetComponent<TextMeshProUGUI>();
                if (img != null) img.color = i == index ? TabActiveColor : TabInactiveColor;
                if (txt != null) txt.color = i == index ? AccentColor : TextColor;
            }
        }

        private void RefreshEntries()
        {
            if (_entryContainer == null) return;
            for (int i = _entryContainer.childCount - 1; i >= 0; i--) Destroy(_entryContainer.GetChild(i).gameObject);
            var gm = GameManager.Instance; if (gm == null) return;
            string chapter = ChapterNames[_currentChapter];
            var entries = gm.Journal.FindAll(e => e.Chapter == chapter);
            if (entries.Count == 0)
            {
                var emptyObj = CreateUIObject("Empty", _entryContainer);
                var emptyText = emptyObj.AddComponent<TextMeshProUGUI>();
                emptyText.text = "暂无记录"; emptyText.fontSize = 16;
                emptyText.color = new Color(0.5f, 0.55f, 0.65f, 1f); emptyText.alignment = TextAlignmentOptions.Center;
                if (_fontAsset != null) emptyText.font = _fontAsset;
                emptyObj.AddComponent<LayoutElement>().preferredHeight = 40f;
                return;
            }
            foreach (var entry in entries)
            {
                var entryObj = CreateUIObject("Entry_" + entry.Id, _entryContainer);
                entryObj.AddComponent<Image>().color = EntryBgColor;
                entryObj.AddComponent<LayoutElement>().preferredHeight = 60f;
                var entryVLayout = entryObj.AddComponent<VerticalLayoutGroup>();
                entryVLayout.padding = new RectOffset(10, 10, 8, 8); entryVLayout.spacing = 4f;
                entryVLayout.childAlignment = TextAnchor.UpperLeft;
                entryVLayout.childControlWidth = true; entryVLayout.childControlHeight = false;
                entryVLayout.childForceExpandWidth = true; entryVLayout.childForceExpandHeight = false;

                var titleObj = CreateUIObject("Title", entryObj.transform);
                var titleText = titleObj.AddComponent<TextMeshProUGUI>();
                titleText.text = entry.Title; titleText.fontSize = 16; titleText.fontStyle = FontStyles.Bold;
                titleText.color = AccentColor; titleText.alignment = TextAlignmentOptions.MidlineLeft;
                if (_fontAsset != null) titleText.font = _fontAsset;
                titleObj.AddComponent<LayoutElement>().preferredHeight = 22f;

                var descObj = CreateUIObject("Desc", entryObj.transform);
                var descText = descObj.AddComponent<TextMeshProUGUI>();
                string content = entry.Content ?? "";
                descText.text = content.Length > 80 ? content.Substring(0, 80) + "…" : content;
                descText.fontSize = 13; descText.color = new Color(0.70f, 0.73f, 0.80f, 1f);
                descText.alignment = TextAlignmentOptions.MidlineLeft; descText.enableWordWrapping = true;
                if (_fontAsset != null) descText.font = _fontAsset;
                descObj.AddComponent<LayoutElement>().preferredHeight = 20f;
            }
        }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-eca6b6a3a15d4baa8377fbd94dc850c9/cwd.txt'; exit "$__tr_native_ec"
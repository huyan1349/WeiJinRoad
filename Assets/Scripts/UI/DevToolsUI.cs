using UnityEngine;
using UnityEngine.UI;
using TMPro;
using WeiJinRoad.Core;
using WeiJinRoad.Vehicle;

namespace WeiJinRoad.UI
{
    public class DevToolsUI : MonoBehaviour
    {
        private const string ZpixFontPath = "Fonts/zpix";
        private static readonly Color BgColor = new Color(0.06f, 0.07f, 0.10f, 0.90f);
        private static readonly Color PanelColor = new Color(0.10f, 0.11f, 0.15f, 0.90f);
        private static readonly Color TextColor = new Color(0.90f, 0.92f, 0.96f, 1f);
        private static readonly Color AccentColor = new Color(0.40f, 0.65f, 0.95f, 1f);
        private static readonly Color BtnColor = new Color(0.15f, 0.17f, 0.22f, 0.90f);
        private static readonly Color BtnHoverColor = new Color(0.25f, 0.28f, 0.35f, 0.95f);
        private static readonly Color CloseBtnColor = new Color(0.70f, 0.25f, 0.25f, 1f);
        private static readonly Color ValueColor = new Color(0.90f, 0.65f, 0.15f, 1f);
        private static readonly Color SectionColor = new Color(0.15f, 0.16f, 0.20f, 0.90f);

        private Canvas _canvas;
        private TMP_FontAsset _fontAsset;
        private GameObject _panel;
        private TMP_Text _fpsText;
        private TMP_Text _posText;
        private TMP_Text _perfText;
        private TMP_Text _frameTimeText;
        private TMP_Text _trianglesText;

        // ─── PerfStatsProbe 字段 ───
        private int _frameCount;
        private float _perfAccumulator;
        private float _perfLastTime;
        private float _perfUpdateInterval = 0.25f; // 250ms

        // ─── GodViewController 字段 ───
        private Camera _godCamera;
        private GameObject _godCameraObj;
        private Vector3 _godCameraEuler;
        private float _godCameraDistance = 200f;
        private Vector3 _godCameraTarget = new Vector3(0f, 0f, -250f);
        private bool _godViewActive;
        private Vector3 _savedCameraPos;
        private Quaternion _savedCameraRot;
        private float _savedNearClip;
        private float _savedFarClip;
        private float _savedFov;
        private Camera _mainCamera;
        private FollowCamera _followCamera;
        private readonly Plane _terrainPlane = new Plane(Vector3.up, 0f);

        private void OnEnable()
        {
            CreateUI();
            GameEvents.OnDevPanelVisibilityChanged += OnDevPanelVisibilityChanged;
        }

        private void OnDisable() { GameEvents.OnDevPanelVisibilityChanged -= OnDevPanelVisibilityChanged; }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F1)) ToggleVisibility();
            if (Input.GetKeyDown(KeyCode.Escape) && _panel != null && _panel.activeSelf) Hide();

            UpdatePerfStatsProbe();
            UpdateGodViewController();

            if (_panel != null && _panel.activeSelf)
            {
                UpdatePerfDisplay();
            }
        }

        private void OnDevPanelVisibilityChanged(bool visible)
        {
            if (visible) Show(); else Hide();
        }

        public void ToggleVisibility()
        {
            if (_panel == null) return;
            if (_panel.activeSelf) Hide(); else Show();
        }

        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
            var gm = GameManager.Instance; if (gm != null) gm.DevPanelVisible = true;
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
            var gm = GameManager.Instance; if (gm != null) gm.DevPanelVisible = false;
        }

        private void CreateUI()
        {
            _fontAsset = Resources.Load<TMP_FontAsset>(ZpixFontPath);
#if UNITY_EDITOR
            if (_fontAsset == null) _fontAsset = UnityEditor.AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/zpix.asset");
#endif
            CreateCanvas();
            CreatePanel();
            Debug.Log("[DevToolsUI] UI 创建完成");
        }

        private void CreateCanvas()
        {
            _canvas = GetComponent<Canvas>();
            if (_canvas == null) _canvas = gameObject.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder = 1500;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;
            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        private void CreatePanel()
        {
            _panel = CreateUIObject("DevToolsPanel", transform);
            var panelRect = _panel.AddComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 0f); panelRect.anchorMax = new Vector2(0.35f, 1f);
            panelRect.offsetMin = Vector2.zero; panelRect.offsetMax = Vector2.zero;
            _panel.AddComponent<Image>().color = BgColor;

            var contentArea = CreateUIObject("ContentArea", _panel.transform);
            var contentRect = contentArea.AddComponent<RectTransform>();
            contentRect.anchorMin = Vector2.zero; contentRect.anchorMax = Vector2.one;
            contentRect.offsetMin = new Vector2(5f, 5f); contentRect.offsetMax = new Vector2(-5f, -5f);
            contentArea.AddComponent<Image>().color = PanelColor;

            var vLayout = contentArea.AddComponent<VerticalLayoutGroup>();
            vLayout.padding = new RectOffset(10, 10, 10, 10); vLayout.spacing = 8f;
            vLayout.childAlignment = TextAnchor.UpperCenter;
            vLayout.childControlWidth = true; vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = true; vLayout.childForceExpandHeight = false;

            // Header
            var headerObj = CreateUIObject("Header", contentArea.transform);
            var headerHLayout = headerObj.AddComponent<HorizontalLayoutGroup>();
            headerHLayout.childControlWidth = true; headerHLayout.childForceExpandWidth = true;
            var titleObj = CreateUIObject("Title", headerObj.transform);
            var titleText = titleObj.AddComponent<TextMeshProUGUI>();
            titleText.text = "开发者工具 [F1]"; titleText.fontSize = 18; titleText.fontStyle = FontStyles.Bold;
            titleText.color = TextColor; titleText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) titleText.font = _fontAsset;
            titleObj.AddComponent<LayoutElement>().flexibleWidth = 1f;
            var closeBtnObj = CreateUIObject("CloseBtn", headerObj.transform);
            closeBtnObj.AddComponent<Image>().color = CloseBtnColor;
            closeBtnObj.AddComponent<Button>().onClick.AddListener(Hide);
            closeBtnObj.AddComponent<LayoutElement>().preferredWidth = 30f;
            var closeBtnText = closeBtnObj.AddComponent<TextMeshProUGUI>();
            closeBtnText.text = "✕"; closeBtnText.fontSize = 16; closeBtnText.color = TextColor;
            closeBtnText.alignment = TextAlignmentOptions.Center;
            if (_fontAsset != null) closeBtnText.font = _fontAsset;

            // Performance section
            CreateSectionHeader(contentArea.transform, "性能");
            _fpsText = CreateInfoRow(contentArea.transform, "FPS", "0");
            _frameTimeText = CreateInfoRow(contentArea.transform, "帧耗时", "0ms");
            _perfText = CreateInfoRow(contentArea.transform, "DrawCalls", "0");
            _trianglesText = CreateInfoRow(contentArea.transform, "三角形", "0");
            _posText = CreateInfoRow(contentArea.transform, "位置", "0, 0");

            // Teleport section
            CreateSectionHeader(contentArea.transform, "传送");
            CreateDevButton(contentArea.transform, "传送至起点", () => RequestTeleport(0f, 270f));
            CreateDevButton(contentArea.transform, "传送至中段", () => RequestTeleport(0f, 135f));
            CreateDevButton(contentArea.transform, "传送至终点", () => RequestTeleport(0f, 0f));

            // Cheats section
            CreateSectionHeader(contentArea.transform, "作弊");
            CreateDevButton(contentArea.transform, "添加资源 +10", OnAddResources);
            CreateDevButton(contentArea.transform, "维修全部", OnRepairAll);
            CreateDevButton(contentArea.transform, "上帝模式", OnToggleGodMode);

            _panel.SetActive(false);
        }

        private void CreateSectionHeader(Transform parent, string title)
        {
            var obj = CreateUIObject("Section_" + title, parent);
            obj.AddComponent<Image>().color = SectionColor;
            obj.AddComponent<LayoutElement>().preferredHeight = 28f;
            var hLayout = obj.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(10, 10, 4, 4);
            hLayout.childAlignment = TextAnchor.MiddleLeft;
            hLayout.childControlWidth = true; hLayout.childForceExpandWidth = true;
            var textObj = CreateUIObject("Label", obj.transform);
            var text = textObj.AddComponent<TextMeshProUGUI>();
            text.text = title; text.fontSize = 14; text.fontStyle = FontStyles.Bold;
            text.color = AccentColor; text.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) text.font = _fontAsset;
        }

        private TMP_Text CreateInfoRow(Transform parent, string label, string initialValue)
        {
            var rowObj = CreateUIObject("InfoRow_" + label, parent);
            var hLayout = rowObj.AddComponent<HorizontalLayoutGroup>();
            hLayout.padding = new RectOffset(10, 10, 2, 2); hLayout.spacing = 8f;
            hLayout.childControlWidth = true; hLayout.childForceExpandWidth = true;
            rowObj.AddComponent<LayoutElement>().preferredHeight = 22f;

            var labelObj = CreateUIObject("Label", rowObj.transform);
            var labelText = labelObj.AddComponent<TextMeshProUGUI>();
            labelText.text = label; labelText.fontSize = 13; labelText.color = TextColor;
            labelText.alignment = TextAlignmentOptions.MidlineLeft;
            if (_fontAsset != null) labelText.font = _fontAsset;
            labelObj.AddComponent<LayoutElement>().flexibleWidth = 1f;

            var valueObj = CreateUIObject("Value", rowObj.transform);
            var valueText = valueObj.AddComponent<TextMeshProUGUI>();
            valueText.text = initialValue; valueText.fontSize = 13; valueText.color = ValueColor;
            valueText.alignment = TextAlignmentOptions.MidlineRight;
            if (_fontAsset != null) valueText.font = _fontAsset;
            valueObj.AddComponent<LayoutElement>().preferredWidth = 100f;

            return valueText;
        }

        private void CreateDevButton(Transform parent, string label, System.Action onClick)
        {
            var btnObj = CreateUIObject("DevBtn_" + label, parent);
            btnObj.AddComponent<Image>().color = BtnColor;
            var btn = btnObj.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick?.Invoke());
            var colors = btn.colors; colors.highlightedColor = BtnHoverColor; btn.colors = colors;
            btnObj.AddComponent<LayoutElement>().preferredHeight = 30f;
            var text = btnObj.AddComponent<TextMeshProUGUI>();
            text.text = label; text.fontSize = 13; text.color = TextColor;
            text.alignment = TextAlignmentOptions.MiddleCenter;
            if (_fontAsset != null) text.font = _fontAsset;
        }

        private void UpdatePerfDisplay()
        {
            var gm = GameManager.Instance;
            var stats = gm != null ? gm.PerfStatsData : null;
            if (_fpsText != null) _fpsText.text = stats != null ? $"{stats.Fps:F0}" : "0";
            if (_frameTimeText != null) _frameTimeText.text = stats != null ? $"{stats.FrameMs:F1}ms" : "0ms";
            if (_perfText != null) _perfText.text = stats != null ? $"{stats.DrawCalls}" : "0";
            if (_trianglesText != null) _trianglesText.text = stats != null ? $"{stats.Triangles:N0}" : "0";
            if (gm != null)
            {
                if (_posText != null) _posText.text = $"{gm.VehicleTransient.Position[0]:F0}, {gm.VehicleTransient.Position[1]:F0}";
            }
        }


        // =================================================================
        // PerfStatsProbe — 性能统计探针（对应 React PerfStatsProbe 组件）
        // =================================================================

        /// <summary>
        /// 每帧累积帧时间和帧数，每250ms更新一次性能统计数据
        /// </summary>
        private void UpdatePerfStatsProbe()
        {
            float now = Time.unscaledTime;
            float dt = now - _perfLastTime;
            _perfLastTime = now;

            _frameCount++;
            _perfAccumulator += dt;

            if (_perfAccumulator >= _perfUpdateInterval)
            {
                float fps = Mathf.Round((_frameCount * 1000f) / (_perfAccumulator * 1000f));
                float frameMs = (_perfAccumulator * 1000f) / _frameCount;

                var gm = GameManager.Instance;
                if (gm != null)
                {
                    var stats = gm.PerfStatsData;
                    stats.Fps = fps;
                    stats.FrameMs = frameMs;
                    stats.DrawCalls = UnityEngine.Profiling.Profiler.supported
                        ? UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() > 0 ? UnityStats.DrawCalls : 0
                        : 0;
                    stats.Triangles = UnityStats.Triangles;
                    stats.Textures = UnityStats.UsedTextures;
                    stats.Geometries = UnityStats.UsedGeometries;
                    gm.SetPerfStats(stats);
                }

                _frameCount = 0;
                _perfAccumulator = 0f;
            }
        }

        // =================================================================
        // GodViewController — 上帝视角控制器（对应 React GodViewController 组件）
        // =================================================================

        /// <summary>
        /// 上帝视角主循环：处理相机切换、WASD+QE移动、鼠标点击传送
        /// </summary>
        private void UpdateGodViewController()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            bool shouldActive = gm.DevGodView;

            // 状态切换
            if (shouldActive && !_godViewActive)
            {
                EnterGodView();
            }
            else if (!shouldActive && _godViewActive)
            {
                ExitGodView();
            }

            if (!_godViewActive) return;

            // WASD + QE 移动
            HandleGodViewMovement();

            // 鼠标右键拖拽旋转
            HandleGodViewRotation();

            // 鼠标左键点击传送
            HandleGodViewTeleport();

            // 应用相机位置
            ApplyGodCameraTransform();
        }

        /// <summary>
        /// 进入上帝视角：保存当前相机状态，切换到俯视相机
        /// </summary>
        private void EnterGodView()
        {
            _mainCamera = Camera.main;
            if (_mainCamera == null) return;

            // 保存当前相机参数
            _savedCameraPos = _mainCamera.transform.position;
            _savedCameraRot = _mainCamera.transform.rotation;
            _savedNearClip = _mainCamera.nearClipPlane;
            _savedFarClip = _mainCamera.farClipPlane;
            _savedFov = _mainCamera.fieldOfView;

            // 禁用跟随相机
            _followCamera = _mainCamera.GetComponent<FollowCamera>();
            if (_followCamera != null) _followCamera.enabled = false;

            // 设置上帝视角相机参数
            _mainCamera.nearClipPlane = 0.1f;
            _mainCamera.farClipPlane = 6000f;
            _mainCamera.fieldOfView = 58f;

            // 初始位置：高空俯视
            var gm = GameManager.Instance;
            float targetX = gm != null ? gm.VehicleTransient.Position[0] : 0f;
            float targetZ = gm != null ? gm.VehicleTransient.Position[1] : -250f;
            _godCameraTarget = new Vector3(targetX, 0f, targetZ);
            _godCameraDistance = 200f;
            _godCameraEuler = new Vector3(55f, 0f, 0f);

            _godViewActive = true;
            gm.CameraFollow = false;

            Debug.Log("[DevToolsUI] 上帝视角已启用");
        }

        /// <summary>
        /// 退出上帝视角：恢复原始相机状态
        /// </summary>
        private void ExitGodView()
        {
            if (_mainCamera != null)
            {
                _mainCamera.nearClipPlane = _savedNearClip;
                _mainCamera.farClipPlane = _savedFarClip;
                _mainCamera.fieldOfView = _savedFov;
                _mainCamera.transform.position = _savedCameraPos;
                _mainCamera.transform.rotation = _savedCameraRot;
            }

            // 恢复跟随相机
            if (_followCamera != null)
            {
                _followCamera.enabled = true;
                _followCamera.SnapToTarget();
            }

            var gm = GameManager.Instance;
            if (gm != null) gm.CameraFollow = true;

            _godViewActive = false;
            Debug.Log("[DevToolsUI] 上帝视角已禁用");
        }

        /// <summary>
        /// 处理上帝视角的 WASD + QE 移动
        /// </summary>
        private void HandleGodViewMovement()
        {
            if (_mainCamera == null) return;

            Vector3 forward = _mainCamera.transform.forward;
            forward.y = 0f;
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            Vector3 move = Vector3.zero;
            if (Input.GetKey(KeyCode.W)) move += forward;
            if (Input.GetKey(KeyCode.S)) move -= forward;
            if (Input.GetKey(KeyCode.D)) move -= right;
            if (Input.GetKey(KeyCode.A)) move += right;
            if (Input.GetKey(KeyCode.E)) _godCameraDistance -= 5f;
            if (Input.GetKey(KeyCode.Q)) _godCameraDistance += 5f;

            _godCameraDistance = Mathf.Clamp(_godCameraDistance, 20f, 1800f);

            if (move.sqrMagnitude > 0f)
            {
                bool fast = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
                float speed = fast ? 520f : 180f;
                _godCameraTarget += move.normalized * speed * Time.deltaTime;
            }
        }

        /// <summary>
        /// 处理上帝视角的鼠标右键拖拽旋转
        /// </summary>
        private void HandleGodViewRotation()
        {
            if (Input.GetMouseButton(1))
            {
                float rotX = Input.GetAxis("Mouse X") * 3f;
                float rotY = -Input.GetAxis("Mouse Y") * 3f;
                _godCameraEuler.y += rotX;
                _godCameraEuler.x = Mathf.Clamp(_godCameraEuler.x + rotY, 5f, 89f);
            }

            // 滚轮缩放
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.001f)
            {
                _godCameraDistance -= scroll * 100f;
                _godCameraDistance = Mathf.Clamp(_godCameraDistance, 20f, 1800f);
            }
        }

        /// <summary>
        /// 处理上帝视角的鼠标左键点击传送（射线投射到地形平面）
        /// </summary>
        private void HandleGodViewTeleport()
        {
            if (!Input.GetMouseButtonDown(0)) return;
            if (Input.GetMouseButton(1)) return; // 右键拖拽时不传送

            // 检查是否点击了UI
            if (UnityEngine.EventSystems.EventSystem.current != null
                && UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;

            Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);
            if (_terrainPlane.Raycast(ray, out float enter))
            {
                Vector3 hitPoint = ray.GetPoint(enter);
                var gm = GameManager.Instance;
                if (gm != null)
                {
                    gm.RequestDevTeleport(hitPoint.x, hitPoint.z);
                    GameEvents.OnDevTeleportRequested?.Invoke(hitPoint.x, hitPoint.z);
                }
            }
        }

        /// <summary>
        /// 根据目标点、距离和角度计算相机位置
        /// </summary>
        private void ApplyGodCameraTransform()
        {
            if (_mainCamera == null) return;

            float radX = _godCameraEuler.x * Mathf.Deg2Rad;
            float radY = _godCameraEuler.y * Mathf.Deg2Rad;

            float offsetX = _godCameraDistance * Mathf.Cos(radX) * Mathf.Sin(radY);
            float offsetY = _godCameraDistance * Mathf.Sin(radX);
            float offsetZ = _godCameraDistance * Mathf.Cos(radX) * Mathf.Cos(radY);

            Vector3 camPos = _godCameraTarget + new Vector3(offsetX, offsetY, offsetZ);
            camPos.y = Mathf.Max(camPos.y, 10f); // 最低高度限制

            _mainCamera.transform.position = camPos;
            _mainCamera.transform.LookAt(_godCameraTarget);
        }

        private void RequestTeleport(float x, float z)
        {
            GameEvents.OnDevTeleportRequested?.Invoke(x, z);
            var gm = GameManager.Instance; if (gm != null) gm.RequestDevTeleport(x, z);
        }

        private void OnAddResources()
        {
            var gm = GameManager.Instance; if (gm == null) return;
            gm.AddResources(new ResourceBag { Metal = 10, Wood = 10, Fuel = 10, Signal = 5, Crystal = 3 });
        }

        private void OnRepairAll()
        {
            var gm = GameManager.Instance; if (gm == null) return;
            foreach (PartId part in System.Enum.GetValues(typeof(PartId)))
                gm.SetPartCondition(part, 1f);
        }

        private void OnToggleGodMode()
        {
            var gm = GameManager.Instance; if (gm == null) return;
            gm.DevGodView = !gm.DevGodView;
        }

        private GameObject CreateUIObject(string name, Transform parent) { var obj = new GameObject(name); obj.transform.SetParent(parent, false); return obj; }
    }
}

using System;
using System.Collections.Generic;
using UnityEngine;
using WeiJinRoad.Core;
using WeiJinRoad.World;

namespace WeiJinRoad.Interaction
{
    // =================================================================
    // 可交互对象类型
    // =================================================================

    /// <summary>
    /// 可交互对象类型枚举
    /// </summary>
    public enum InteractableType
    {
        /// <summary>文字载体</summary>
        TextCarrier,
        /// <summary>人造建筑</summary>
        ArtificialTerrain,
        /// <summary>环境物体</summary>
        EnvObj,
        /// <summary>岩石</summary>
        Rock,
        /// <summary>植被</summary>
        Foliage
    }

    // =================================================================
    // 场景物体定义数据
    // =================================================================

    /// <summary>
    /// 人造建筑定义
    /// </summary>
    [Serializable]
    public class ArtificialTerrainDef
    {
        public string Id;
        public string Name;
        public string Description;
    }

    /// <summary>
    /// 环境物体定义
    /// </summary>
    [Serializable]
    public class EnvironmentObjectDef
    {
        public string Id;
        public string Name;
        public string Description;
    }

    /// <summary>
    /// 文字载体定义
    /// </summary>
    [Serializable]
    public class TextCarrierDef
    {
        public string Id;
        public string CarrierType;
    }

    /// <summary>
    /// 场景物体实例（对应 TS experimentalWorld.sceneryObjects 中的对象）
    /// </summary>
    [Serializable]
    public class SceneryObject
    {
        /// <summary>类型标识</summary>
        public InteractableType Type;
        /// <summary>对象ID</summary>
        public string ObjectId;
        /// <summary>文字内容（仅文字载体）</summary>
        public string Text;
        /// <summary>世界坐标X</summary>
        public float X;
        /// <summary>世界坐标Y</summary>
        public float Y;
        /// <summary>世界坐标Z</summary>
        public float Z;
    }

    // =================================================================
    // 交互结果数据
    // =================================================================

    /// <summary>
    /// 交互结果：包含叙事展示所需的所有信息
    /// </summary>
    [Serializable]
    public class InteractionResult
    {
        /// <summary>碎片ID（仅文字载体有）</summary>
        public string FragmentId;
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

    // =================================================================
    // InteractionSystem — 交互检测与执行系统
    // =================================================================

    /// <summary>
    /// 交互检测系统：检测附近可交互对象、处理交互操作。
    ///
    /// 功能：
    /// - 基于距离检测附近可交互对象（资源点、站点、营地、障碍物、碎片）
    /// - 按类型优先级和距离排序，选出最佳交互目标
    /// - 显示交互提示
    /// - E键交互
    /// - 不同交互类型：拾取资源、建造站点、进入营地、清除障碍、阅读碎片
    /// - 订阅 GameEvents 监听状态变更
    /// </summary>
    public class InteractionSystem : MonoBehaviour
    {
        // =============================================================
        // 常量
        // =============================================================

        /// <summary>类型优先级：数字越小优先级越高</summary>
        private static readonly Dictionary<InteractableType, int> TypePriority = new Dictionary<InteractableType, int>
        {
            { InteractableType.TextCarrier, 0 },
            { InteractableType.ArtificialTerrain, 1 },
            { InteractableType.EnvObj, 2 },
            { InteractableType.Rock, 3 },
            { InteractableType.Foliage, 4 },
        };

        /// <summary>可交互类型集合</summary>
        private static readonly HashSet<InteractableType> InteractableTypes = new HashSet<InteractableType>
        {
            InteractableType.TextCarrier,
            InteractableType.ArtificialTerrain,
            InteractableType.EnvObj,
        };

        // =============================================================
        // 配置
        // =============================================================

        [Header("检测配置")]
        [Tooltip("最大检测距离")]
        public float MaxDetectionDistance = 15f;

        [Tooltip("检测间隔（秒）")]
        public float DetectionInterval = 0.2f;

        [Tooltip("交互按键")]
        public KeyCode InteractKey = KeyCode.E;

        [Header("引用")]
        [Tooltip("场景物体列表（由外部系统填充）")]
        public List<SceneryObject> SceneryObjects = new List<SceneryObject>();

        [Header("定义数据")]
        [Tooltip("人造建筑定义列表")]
        public List<ArtificialTerrainDef> ArtificialTerrains = new List<ArtificialTerrainDef>();
        [Tooltip("环境物体定义列表")]
        public List<EnvironmentObjectDef> EnvironmentObjects = new List<EnvironmentObjectDef>();
        [Tooltip("文字载体定义列表")]
        public List<TextCarrierDef> TextCarriers = new List<TextCarrierDef>();

        // =============================================================
        // 运行时状态
        // =============================================================

        /// <summary>当前最佳交互目标</summary>
        public NearbyInteractable CurrentTarget { get; private set; }

        /// <summary>是否显示交互提示</summary>
        public bool ShowPrompt { get; private set; }

        /// <summary>交互提示文本</summary>
        public string PromptText { get; private set; }

        private float _detectionTimer;
        private GameManager _gameManager;

        // =============================================================
        // Unity 生命周期
        // =============================================================

        private void Awake()
        {
            _gameManager = GameManager.Instance;
        }

        private void OnEnable()
        {
            GameEvents.OnGamePhaseChanged += OnGamePhaseChanged;
            GameEvents.OnObstacleCleared += OnObstacleCleared;
            GameEvents.OnFragmentDiscovered += OnFragmentDiscovered;
            GameEvents.OnStationBuilt += OnStationBuilt;
        }

        private void OnDisable()
        {
            GameEvents.OnGamePhaseChanged -= OnGamePhaseChanged;
            GameEvents.OnObstacleCleared -= OnObstacleCleared;
            GameEvents.OnFragmentDiscovered -= OnFragmentDiscovered;
            GameEvents.OnStationBuilt -= OnStationBuilt;
        }

        private void Update()
        {
            // 定时检测
            _detectionTimer += Time.deltaTime;
            if (_detectionTimer >= DetectionInterval)
            {
                _detectionTimer = 0f;
                DetectNearbyInteractable();
            }

            // E键交互
            if (Input.GetKeyDown(InteractKey) && CurrentTarget != null)
            {
                ExecuteInteraction(CurrentTarget);
            }
        }

        // =============================================================
        // 事件处理
        // =============================================================

        private void OnGamePhaseChanged(int journey)
        {
            _detectionTimer = DetectionInterval;
        }

        private void OnObstacleCleared(string obstacleId)
        {
            _detectionTimer = DetectionInterval;
        }

        private void OnFragmentDiscovered(string fragmentId)
        {
            _detectionTimer = DetectionInterval;
        }

        private void OnStationBuilt(string siteId)
        {
            _detectionTimer = DetectionInterval;
        }

        // =============================================================
        // 检测逻辑
        // =============================================================

        /// <summary>
        /// 检测附近可交互对象，更新 CurrentTarget
        /// </summary>
        private void DetectNearbyInteractable()
        {
            if (_gameManager == null) return;

            float vehicleX = _gameManager.VehicleTransient.Position[0];
            float vehicleZ = _gameManager.VehicleTransient.Position[1];

            NearbyInteractable best = FindNearbyInteractable(vehicleX, vehicleZ, MaxDetectionDistance);

            CurrentTarget = best;
            ShowPrompt = best != null;
            PromptText = best?.Hint ?? string.Empty;

            _gameManager.SetNearbyInteractable(best);
            GameEvents.OnNearbyInteractableChanged?.Invoke(best);
        }

        /// <summary>
        /// 查找附近最佳可交互对象
        ///
        /// 算法：
        /// 1. 遍历所有场景物体，筛选可交互类型
        /// 2. 计算距离，过滤超出范围的
        /// 3. 生成提示文本，过滤无提示的
        /// 4. 按类型优先级排序，同优先级按距离排序
        /// 5. 返回最佳候选
        /// </summary>
        /// <param name="vehicleX">载具X坐标</param>
        /// <param name="vehicleZ">载具Z坐标</param>
        /// <param name="maxDistance">最大检测距离</param>
        /// <returns>最佳可交互对象，无则返回null</returns>
        public NearbyInteractable FindNearbyInteractable(float vehicleX, float vehicleZ, float maxDistance)
        {
            NearbyInteractable bestCandidate = null;
            int bestPriority = int.MaxValue;
            float bestDistance = float.MaxValue;

            for (int i = 0; i < SceneryObjects.Count; i++)
            {
                var obj = SceneryObjects[i];
                if (!InteractableTypes.Contains(obj.Type)) continue;

                float dx = obj.X - vehicleX;
                float dz = obj.Z - vehicleZ;
                float distance = Mathf.Sqrt(dx * dx + dz * dz);

                if (distance > maxDistance) continue;

                int priority = TypePriority.TryGetValue(obj.Type, out var p) ? p : 99;
                string title = GetObjectName(obj);
                string hint = GenerateHint(obj);

                if (string.IsNullOrEmpty(hint)) continue;

                bool isBetter = priority < bestPriority || (priority == bestPriority && distance < bestDistance);
                if (isBetter)
                {
                    bestPriority = priority;
                    bestDistance = distance;
                    bestCandidate = new NearbyInteractable
                    {
                        Id = !string.IsNullOrEmpty(obj.ObjectId) ? obj.ObjectId : $"{obj.Type}_{obj.X}_{obj.Z}",
                        Type = obj.Type.ToString(),
                        Title = title,
                        Hint = hint,
                        Distance = distance,
                        X = obj.X,
                        Y = obj.Y,
                        Z = obj.Z,
                    };
                }
            }

            return bestCandidate;
        }

        // =============================================================
        // 交互执行
        // =============================================================

        /// <summary>
        /// 执行交互操作
        ///
        /// 根据交互对象类型执行不同操作：
        /// - TextCarrier: 发现碎片，添加日志条目
        /// - ArtificialTerrain: 展示建筑描述
        /// - EnvObj: 展示物体描述
        /// </summary>
        /// <param name="interactable">交互目标</param>
        /// <returns>交互结果，失败返回null</returns>
        public InteractionResult InteractWithObject(NearbyInteractable interactable)
        {
            if (interactable == null) return null;

            SceneryObject sceneryObj = null;
            for (int i = 0; i < SceneryObjects.Count; i++)
            {
                var obj = SceneryObjects[i];
                string objId = !string.IsNullOrEmpty(obj.ObjectId) ? obj.ObjectId : $"{obj.Type}_{obj.X}_{obj.Z}";
                if (objId == interactable.Id && obj.Type.ToString() == interactable.Type)
                {
                    sceneryObj = obj;
                    break;
                }
            }

            if (sceneryObj == null) return null;

            float routeZ = TerrainHeight.WorldToRouteZ(sceneryObj.Z);
            string biome = GetBiomeFromRouteZ(routeZ);

            if (sceneryObj.Type == InteractableType.TextCarrier)
                return HandleTextCarrier(sceneryObj, biome);

            if (sceneryObj.Type == InteractableType.ArtificialTerrain)
                return HandleArtificialTerrain(sceneryObj, biome);

            if (sceneryObj.Type == InteractableType.EnvObj)
                return HandleEnvObj(sceneryObj, biome);

            return null;
        }

        /// <summary>
        /// 执行交互并更新游戏状态
        /// </summary>
        /// <param name="interactable">交互目标</param>
        public void ExecuteInteraction(NearbyInteractable interactable)
        {
            var result = InteractWithObject(interactable);
            if (result == null) return;

            _gameManager.SetCurrentNarrativeContent(new NarrativeContent
            {
                Title = result.Title,
                Content = result.Content,
                CarrierType = result.CarrierType,
                Chapter = result.Chapter,
                Biome = result.Biome,
            });
            _gameManager.SetNarrativeOverlayVisible(true);

            if (!string.IsNullOrEmpty(result.FragmentId))
            {
                _gameManager.DiscoverFragment(result.FragmentId);
                _gameManager.AddJournalEntry(new JournalEntry
                {
                    Id = System.Guid.NewGuid().ToString("N").Substring(0, 8),
                    FragmentId = result.FragmentId,
                    Chapter = result.Chapter,
                    Title = result.Title,
                    Content = result.Content,
                    CarrierType = result.CarrierType,
                    Biome = result.Biome,
                    DiscoveredAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                    LocationName = result.Biome,
                });
            }
        }

        // =============================================================
        // 类型处理
        // =============================================================

        private InteractionResult HandleTextCarrier(SceneryObject obj, string biome)
        {
            string fragmentId = !string.IsNullOrEmpty(obj.ObjectId) ? obj.ObjectId : $"frag_{obj.X}_{obj.Z}";
            string carrierType = "未知载体";

            if (!string.IsNullOrEmpty(obj.ObjectId))
            {
                for (int i = 0; i < TextCarriers.Count; i++)
                {
                    if (TextCarriers[i].Id == obj.ObjectId)
                    {
                        carrierType = TextCarriers[i].CarrierType;
                        break;
                    }
                }
            }

            return new InteractionResult
            {
                FragmentId = fragmentId,
                Title = "未命名的文字",
                Content = obj.Text ?? string.Empty,
                CarrierType = carrierType,
                Chapter = "ch1",
                Biome = biome,
            };
        }

        private InteractionResult HandleArtificialTerrain(SceneryObject obj, string biome)
        {
            string name = "未知建筑";
            string description = "一座被遗弃的建筑，沉默地矗立在荒野中。";

            if (!string.IsNullOrEmpty(obj.ObjectId))
            {
                for (int i = 0; i < ArtificialTerrains.Count; i++)
                {
                    if (ArtificialTerrains[i].Id == obj.ObjectId)
                    {
                        name = ArtificialTerrains[i].Name;
                        description = ArtificialTerrains[i].Description;
                        break;
                    }
                }
            }

            return new InteractionResult
            {
                Title = name,
                Content = description,
                CarrierType = "人造建筑",
                Chapter = "ch1",
                Biome = biome,
            };
        }

        private InteractionResult HandleEnvObj(SceneryObject obj, string biome)
        {
            string name = "未知物体";
            string description = "一个散落在路边的物体，被时间和风沙侵蚀。";

            if (!string.IsNullOrEmpty(obj.ObjectId))
            {
                for (int i = 0; i < EnvironmentObjects.Count; i++)
                {
                    if (EnvironmentObjects[i].Id == obj.ObjectId)
                    {
                        name = EnvironmentObjects[i].Name;
                        description = EnvironmentObjects[i].Description;
                        break;
                    }
                }
            }

            return new InteractionResult
            {
                Title = name,
                Content = description,
                CarrierType = "环境物体",
                Chapter = "ch1",
                Biome = biome,
            };
        }

        // =============================================================
        // 辅助方法
        // =============================================================

        /// <summary>
        /// 获取对象显示名称
        /// </summary>
        private string GetObjectName(SceneryObject obj)
        {
            switch (obj.Type)
            {
                case InteractableType.TextCarrier:
                    return "文字载体";

                case InteractableType.ArtificialTerrain:
                    if (!string.IsNullOrEmpty(obj.ObjectId))
                    {
                        for (int i = 0; i < ArtificialTerrains.Count; i++)
                            if (ArtificialTerrains[i].Id == obj.ObjectId)
                                return ArtificialTerrains[i].Name;
                    }
                    return "人造建筑";

                case InteractableType.EnvObj:
                    if (!string.IsNullOrEmpty(obj.ObjectId))
                    {
                        for (int i = 0; i < EnvironmentObjects.Count; i++)
                            if (EnvironmentObjects[i].Id == obj.ObjectId)
                                return EnvironmentObjects[i].Name;
                    }
                    return "环境物体";

                default:
                    return "未知物体";
            }
        }

        /// <summary>
        /// 生成交互提示文本
        /// </summary>
        private string GenerateHint(SceneryObject obj)
        {
            switch (obj.Type)
            {
                case InteractableType.TextCarrier:
                    if (!string.IsNullOrEmpty(obj.Text))
                    {
                        string preview = obj.Text.Length > 20 ? obj.Text.Substring(0, 20) + "…" : obj.Text;
                        return $"发现文字：「{preview}」";
                    }
                    return string.Empty;

                case InteractableType.ArtificialTerrain:
                    return "前方有一处人造建筑，似乎可以探索";

                case InteractableType.EnvObj:
                    return "附近有一个值得注意的物体";

                default:
                    return string.Empty;
            }
        }

        /// <summary>
        /// 根据 routeZ 判断生态区
        /// </summary>
        private static string GetBiomeFromRouteZ(float routeZ)
        {
            if (routeZ > 180f) return "平原";
            if (routeZ > -100f) return "森林";
            if (routeZ > -700f) return "裂谷";
            return "山路";
        }

        // =============================================================
        // 公共 API
        // =============================================================

        /// <summary>
        /// 添加场景物体到检测列表
        /// </summary>
        public void AddSceneryObject(SceneryObject obj)
        {
            SceneryObjects.Add(obj);
        }

        /// <summary>
        /// 清空场景物体列表
        /// </summary>
        public void ClearSceneryObjects()
        {
            SceneryObjects.Clear();
            CurrentTarget = null;
            ShowPrompt = false;
            PromptText = string.Empty;
        }
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-b2caa131ac06491ba525f248dfb7fd54/cwd.txt'; exit "$__tr_native_ec"
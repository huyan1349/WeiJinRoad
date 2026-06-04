using System;
using System.Collections.Generic;
using UnityEngine;
using WeiJinRoad.World;

namespace WeiJinRoad.Core
{
    // =================================================================
    // 路段定义
    // =================================================================

    /// <summary>
    /// 路段定义：旅程门控系统的基本单元
    /// </summary>
    [Serializable]
    public class RouteSegment
    {
        /// <summary>路段ID</summary>
        public string Id;
        /// <summary>路段名称</summary>
        public string Name;
        /// <summary>最小 routeZ（更远的方向）</summary>
        public float RouteZMin;
        /// <summary>最大 routeZ（更近的方向）</summary>
        public float RouteZMax;
        /// <summary>需要第几次旅程才能解锁</summary>
        public int JourneyRequired;
        /// <summary>路段代表色（十六进制）</summary>
        public string Color;
    }

    // =================================================================
    // 旅程状态
    // =================================================================

    /// <summary>
    /// 旅程状态：当前旅程进度和可推进性
    /// </summary>
    [Serializable]
    public class JourneyState
    {
        /// <summary>当前旅程（1~5）</summary>
        public int CurrentJourney;
        /// <summary>已解锁路段列表</summary>
        public List<RouteSegment> UnlockedSegments = new List<RouteSegment>();
        /// <summary>下一解锁条件描述</summary>
        public string NextUnlockCondition;
        /// <summary>是否可推进</summary>
        public bool CanProgress;
    }

    // =================================================================
    // 旅程进度
    // =================================================================

    /// <summary>
    /// 旅程进度条件
    /// </summary>
    [Serializable]
    public class JourneyCondition
    {
        /// <summary>条件标签</summary>
        public string Label;
        /// <summary>当前值</summary>
        public int Current;
        /// <summary>需求值</summary>
        public int Required;
        /// <summary>完成百分比（0~1）</summary>
        public float Percent;
    }

    /// <summary>
    /// 旅程进度详情
    /// </summary>
    [Serializable]
    public class JourneyProgress
    {
        /// <summary>旅程编号</summary>
        public int Journey;
        /// <summary>旅程名称</summary>
        public string JourneyName;
        /// <summary>条件列表</summary>
        public List<JourneyCondition> Conditions = new List<JourneyCondition>();
        /// <summary>总体完成百分比（0~1）</summary>
        public float OverallPercent;
    }

    // =================================================================
    // 路段信息
    // =================================================================

    /// <summary>
    /// 路段详细信息
    /// </summary>
    [Serializable]
    public class SegmentInfo
    {
        public string Id;
        public string Name;
        public float RouteZMin;
        public float RouteZMax;
        public int JourneyRequired;
        public string Color;
        public float ClearedPercent;
        public int CampCount;
        public int TotalObstacles;
        public int ClearedObstacles;
    }

    // =================================================================
    // JourneyManager — 旅程门控系统
    // =================================================================

    /// <summary>
    /// 旅程门控系统
    ///
    /// 控制玩家在不同旅程阶段可到达的路线范围。
    /// 每次旅程解锁新的路段，基于已清除路障、营地数量、碎片发现等条件。
    ///
    /// 路段划分（routeZ 坐标）:
    /// - 平原段: routeZ > 180
    /// - 森林段: 180 >= routeZ > -100
    /// - 裂谷段: -100 >= routeZ > -700
    /// - 山路段: -700 >= routeZ > -1480
    /// - 全线: 所有路段
    ///
    /// 旅程名称: 初行 → 深入 → 穿越 → 攀登 → 全线
    /// </summary>
    public class JourneyManager : MonoBehaviour
    {
        // =============================================================
        // 路段定义（静态数据）
        // =============================================================

        /// <summary>路段定义列表</summary>
        public static readonly RouteSegment[] RouteSegments = new RouteSegment[]
        {
            new RouteSegment { Id = "plains", Name = "平原", RouteZMin = 180, RouteZMax = 470, JourneyRequired = 1, Color = "#5ec7ff" },
            new RouteSegment { Id = "forest", Name = "森林", RouteZMin = -100, RouteZMax = 180, JourneyRequired = 2, Color = "#4ade80" },
            new RouteSegment { Id = "rift", Name = "裂谷", RouteZMin = -700, RouteZMax = -100, JourneyRequired = 3, Color = "#fb923c" },
            new RouteSegment { Id = "mountain", Name = "山路", RouteZMin = -1480, RouteZMax = -700, JourneyRequired = 4, Color = "#f472b6" },
        };

        /// <summary>旅程名称（索引0未使用，1~5对应五次旅程）</summary>
        private static readonly string[] JourneyNames = { "", "初行", "深入", "穿越", "攀登", "全线" };

        // =============================================================
        // 配置
        // =============================================================

        [Header("检测配置")]
        [Tooltip("旅程进度检测间隔（秒）")]
        public float CheckInterval = 2f;

        [Header("障碍物数据")]
        [Tooltip("障碍物定义列表（由外部系统填充）")]
        public List<ObstacleRecord> ObstacleRecords = new List<ObstacleRecord>();

        // =============================================================
        // 运行时状态
        // =============================================================

        private GameManager _gameManager;
        private float _checkTimer;

        /// <summary>路段障碍物数量缓存</summary>
        private Dictionary<string, int> _segmentObstacleCache;

        // =============================================================
        // Unity 生命周期
        // =============================================================

        private void Awake()
        {
            _gameManager = GameManager.Instance;
        }

        private void OnEnable()
        {
            GameEvents.OnObstacleCleared += OnObstacleCleared;
            GameEvents.OnCampBuilt += OnCampBuilt;
            GameEvents.OnFragmentDiscovered += OnFragmentDiscovered;
            GameEvents.OnStationBuilt += OnStationBuilt;
        }

        private void OnDisable()
        {
            GameEvents.OnObstacleCleared -= OnObstacleCleared;
            GameEvents.OnCampBuilt -= OnCampBuilt;
            GameEvents.OnFragmentDiscovered -= OnFragmentDiscovered;
            GameEvents.OnStationBuilt -= OnStationBuilt;
        }

        private void Update()
        {
            _checkTimer += Time.deltaTime;
            if (_checkTimer >= CheckInterval)
            {
                _checkTimer = 0f;
                CheckJourneyProgress();
            }
        }

        // =============================================================
        // 事件处理
        // =============================================================

        private void OnObstacleCleared(string id)
        {
            _segmentObstacleCache = null; // 清除缓存
            CheckJourneyProgress();
        }

        private void OnCampBuilt(int count)
        {
            CheckJourneyProgress();
        }

        private void OnFragmentDiscovered(string id)
        {
            CheckJourneyProgress();
        }

        private void OnStationBuilt(string siteId)
        {
            CheckJourneyProgress();
        }

        // =============================================================
        // 旅程进度检查
        // =============================================================

        /// <summary>
        /// 检查旅程进度，若满足条件自动推进旅程
        /// </summary>
        /// <returns>当前旅程状态</returns>
        public JourneyState CheckJourneyProgress()
        {
            if (_gameManager == null) return new JourneyState { CurrentJourney = 1 };

            int currentJourney = _gameManager.CurrentJourney;

            // 尝试推进旅程
            int newJourney = currentJourney;
            for (int j = currentJourney + 1; j <= 5; j++)
            {
                var (met, _) = CheckJourneyUnlockCondition(j);
                if (met)
                    newJourney = j;
                else
                    break;
            }

            // 旅程推进了，更新状态并触发通知
            if (newJourney > currentJourney)
            {
                _gameManager.CurrentJourney = newJourney;
                _gameManager.SetJourneyNotification(currentJourney, newJourney,
                    $"第{newJourney}旅程 · {GetJourneyName(newJourney)}");
                GameEvents.OnJourneyNotification?.Invoke(new JourneyNotification
                {
                    From = currentJourney,
                    To = newJourney,
                    Message = $"第{newJourney}旅程 · {GetJourneyName(newJourney)}",
                });
            }

            // 计算已解锁路段
            var unlockedSegments = new List<RouteSegment>();
            for (int i = 0; i < RouteSegments.Length; i++)
            {
                if (RouteSegments[i].JourneyRequired <= newJourney)
                    unlockedSegments.Add(RouteSegments[i]);
            }

            // 计算下一解锁条件
            string nextUnlockCondition = null;
            bool canProgress = false;
            if (newJourney < 5)
            {
                var (met, desc) = CheckJourneyUnlockCondition(newJourney + 1);
                nextUnlockCondition = desc;
                canProgress = met;
            }

            return new JourneyState
            {
                CurrentJourney = newJourney,
                UnlockedSegments = unlockedSegments,
                NextUnlockCondition = nextUnlockCondition,
                CanProgress = canProgress,
            };
        }

        // =============================================================
        // 旅程解锁条件
        // =============================================================

        /// <summary>
        /// 检查指定旅程的解锁条件
        /// </summary>
        /// <param name="targetJourney">目标旅程（1~5）</param>
        /// <returns>(是否满足, 条件描述)</returns>
        public (bool met, string description) CheckJourneyUnlockCondition(int targetJourney)
        {
            if (_gameManager == null) return (false, "系统未初始化");

            int campCount = GetTotalCampCount();
            int fragmentCount = _gameManager.DiscoveredFragmentIds.Count;

            switch (targetJourney)
            {
                case 1:
                    return (true, "首次旅程，平原段开放");

                case 2:
                {
                    float plainsCleared = GetSegmentClearedPercent(RouteSegments[0]);
                    float distance = GetTravelDistance();
                    bool met = plainsCleared >= 0.6f && campCount >= 1 && distance > 500f;
                    return (met,
                        $"平原清除{Mathf.RoundToInt(plainsCleared * 100)}%/60% · 营地{campCount}/1 · 行驶{Mathf.RoundToInt(distance)}m/500m");
                }

                case 3:
                {
                    float forestCleared = GetSegmentClearedPercent(RouteSegments[1]);
                    bool met = forestCleared >= 0.5f && campCount >= 2 && fragmentCount >= 5;
                    return (met,
                        $"森林清除{Mathf.RoundToInt(forestCleared * 100)}%/50% · 营地{campCount}/2 · 碎片{fragmentCount}/5");
                }

                case 4:
                {
                    float riftCleared = GetSegmentClearedPercent(RouteSegments[2]);
                    bool hasTower = HasSignalTower();
                    bool met = riftCleared >= 0.4f && campCount >= 3 && hasTower;
                    return (met,
                        $"裂谷清除{Mathf.RoundToInt(riftCleared * 100)}%/40% · 营地{campCount}/3 · 信号塔{(hasTower ? "✓" : "✗")}");
                }

                case 5:
                {
                    float mountainCleared = GetSegmentClearedPercent(RouteSegments[3]);
                    bool reachedSummit = HasReachedSummit();
                    bool met = mountainCleared >= 0.3f && campCount >= 4 && reachedSummit;
                    return (met,
                        $"山路清除{Mathf.RoundToInt(mountainCleared * 100)}%/30% · 营地{campCount}/4 · 山顶{(reachedSummit ? "✓" : "✗")}");
                }

                default:
                    return (false, "未知旅程");
            }
        }

        // =============================================================
        // 路段访问检查
        // =============================================================

        /// <summary>
        /// 检查指定 routeZ 位置是否可达
        /// </summary>
        /// <param name="routeZ">路线空间Z坐标</param>
        /// <returns>是否可达</returns>
        public bool CanAccessRoute(float routeZ)
        {
            if (_gameManager == null) return false;
            int currentJourney = _gameManager.CurrentJourney;

            for (int i = 0; i < RouteSegments.Length; i++)
            {
                if (routeZ >= RouteSegments[i].RouteZMin && routeZ < RouteSegments[i].RouteZMax)
                    return currentJourney >= RouteSegments[i].JourneyRequired;
            }

            // 超出已知路段范围
            if (routeZ >= RouteSegments[0].RouteZMax) return true;
            if (routeZ < RouteSegments[RouteSegments.Length - 1].RouteZMin)
                return currentJourney >= 5;

            return true;
        }

        /// <summary>
        /// 检查世界坐标 Z 是否可达
        /// </summary>
        /// <param name="worldZ">世界空间Z坐标</param>
        /// <returns>是否可达</returns>
        public bool CanAccessWorldZ(float worldZ)
        {
            float routeZ = TerrainHeight.WorldToRouteZ(worldZ);
            return CanAccessRoute(routeZ);
        }

        /// <summary>
        /// 获取当前旅程可达的最大 routeZ 范围
        /// </summary>
        /// <returns>(最小routeZ, 最大routeZ)</returns>
        public (float min, float max) GetAccessibleRouteRange()
        {
            if (_gameManager == null) return (180f, 470f);
            int currentJourney = _gameManager.CurrentJourney;

            float min = float.MaxValue;
            float max = float.MinValue;

            for (int i = 0; i < RouteSegments.Length; i++)
            {
                if (RouteSegments[i].JourneyRequired <= currentJourney)
                {
                    if (RouteSegments[i].RouteZMin < min) min = RouteSegments[i].RouteZMin;
                    if (RouteSegments[i].RouteZMax > max) max = RouteSegments[i].RouteZMax;
                }
            }

            if (min == float.MaxValue) return (180f, 470f);
            return (min, max);
        }

        // =============================================================
        // 旅程进度详情
        // =============================================================

        /// <summary>
        /// 获取旅程进度详情
        /// </summary>
        /// <param name="journey">旅程编号</param>
        /// <returns>进度详情</returns>
        public JourneyProgress GetJourneyProgress(int journey)
        {
            if (_gameManager == null) return new JourneyProgress { Journey = journey, JourneyName = GetJourneyName(journey) };

            int campCount = GetTotalCampCount();
            int fragmentCount = _gameManager.DiscoveredFragmentIds.Count;
            var conditions = new List<JourneyCondition>();

            switch (journey)
            {
                case 1:
                    // 无条件，已解锁
                    break;

                case 2:
                {
                    float plainsCleared = GetSegmentClearedPercent(RouteSegments[0]);
                    float distance = GetTravelDistance();
                    conditions.Add(new JourneyCondition { Label = "平原段清除", Current = Mathf.RoundToInt(plainsCleared * 100), Required = 60, Percent = Mathf.Min(1f, plainsCleared / 0.6f) });
                    conditions.Add(new JourneyCondition { Label = "建造营地", Current = campCount, Required = 1, Percent = Mathf.Min(1f, (float)campCount / 1f) });
                    conditions.Add(new JourneyCondition { Label = "行驶距离", Current = Mathf.RoundToInt(distance), Required = 500, Percent = Mathf.Min(1f, distance / 500f) });
                    break;
                }

                case 3:
                {
                    float forestCleared = GetSegmentClearedPercent(RouteSegments[1]);
                    conditions.Add(new JourneyCondition { Label = "森林段清除", Current = Mathf.RoundToInt(forestCleared * 100), Required = 50, Percent = Mathf.Min(1f, forestCleared / 0.5f) });
                    conditions.Add(new JourneyCondition { Label = "建造营地", Current = campCount, Required = 2, Percent = Mathf.Min(1f, (float)campCount / 2f) });
                    conditions.Add(new JourneyCondition { Label = "发现碎片", Current = fragmentCount, Required = 5, Percent = Mathf.Min(1f, (float)fragmentCount / 5f) });
                    break;
                }

                case 4:
                {
                    float riftCleared = GetSegmentClearedPercent(RouteSegments[2]);
                    bool hasTower = HasSignalTower();
                    conditions.Add(new JourneyCondition { Label = "裂谷段清除", Current = Mathf.RoundToInt(riftCleared * 100), Required = 40, Percent = Mathf.Min(1f, riftCleared / 0.4f) });
                    conditions.Add(new JourneyCondition { Label = "建造营地", Current = campCount, Required = 3, Percent = Mathf.Min(1f, (float)campCount / 3f) });
                    conditions.Add(new JourneyCondition { Label = "信号塔", Current = hasTower ? 1 : 0, Required = 1, Percent = hasTower ? 1f : 0f });
                    break;
                }

                case 5:
                {
                    float mountainCleared = GetSegmentClearedPercent(RouteSegments[3]);
                    bool reachedSummit = HasReachedSummit();
                    conditions.Add(new JourneyCondition { Label = "山路段清除", Current = Mathf.RoundToInt(mountainCleared * 100), Required = 30, Percent = Mathf.Min(1f, mountainCleared / 0.3f) });
                    conditions.Add(new JourneyCondition { Label = "建造营地", Current = campCount, Required = 4, Percent = Mathf.Min(1f, (float)campCount / 4f) });
                    conditions.Add(new JourneyCondition { Label = "到达山顶", Current = reachedSummit ? 1 : 0, Required = 1, Percent = reachedSummit ? 1f : 0f });
                    break;
                }
            }

            float overallPercent = conditions.Count > 0 ? 0f : 1f;
            if (conditions.Count > 0)
            {
                float sum = 0f;
                for (int i = 0; i < conditions.Count; i++) sum += conditions[i].Percent;
                overallPercent = sum / conditions.Count;
            }

            return new JourneyProgress
            {
                Journey = journey,
                JourneyName = GetJourneyName(journey),
                Conditions = conditions,
                OverallPercent = overallPercent,
            };
        }

        // =============================================================
        // 路段信息
        // =============================================================

        /// <summary>
        /// 获取路段详细信息
        /// </summary>
        /// <param name="segmentId">路段ID</param>
        /// <returns>路段信息，未找到返回null</returns>
        public SegmentInfo GetSegmentInfo(string segmentId)
        {
            RouteSegment segment = null;
            for (int i = 0; i < RouteSegments.Length; i++)
            {
                if (RouteSegments[i].Id == segmentId)
                {
                    segment = RouteSegments[i];
                    break;
                }
            }
            if (segment == null) return null;

            int totalObstacles = GetEstimatedObstacleCount(segment);
            int clearedCount = GetSegmentClearedCount(segment);
            float clearedPercent = GetSegmentClearedPercent(segment);
            int campCount = GetSegmentCampCount(segment);

            return new SegmentInfo
            {
                Id = segment.Id,
                Name = segment.Name,
                RouteZMin = segment.RouteZMin,
                RouteZMax = segment.RouteZMax,
                JourneyRequired = segment.JourneyRequired,
                Color = segment.Color,
                ClearedPercent = clearedPercent,
                CampCount = campCount,
                TotalObstacles = totalObstacles,
                ClearedObstacles = clearedCount,
            };
        }

        // =============================================================
        // 营地管理
        // =============================================================

        /// <summary>
        /// 记录一次扎营位置
        /// </summary>
        /// <param name="x">世界坐标X</param>
        /// <param name="z">世界坐标Z</param>
        public void RecordCamp(float x, float z)
        {
            if (_gameManager == null) return;
            _gameManager.AddBuiltCamp(x, z);
        }

        /// <summary>
        /// 获取已建营地数量
        /// </summary>
        public int GetBuiltCampCount()
        {
            if (_gameManager == null) return 0;
            return _gameManager.BuiltCamps.Count;
        }

        /// <summary>
        /// 获取已建营地列表副本
        /// </summary>
        public List<BuiltCamp> GetBuiltCamps()
        {
            if (_gameManager == null) return new List<BuiltCamp>();
            return new List<BuiltCamp>(_gameManager.BuiltCamps);
        }

        // =============================================================
        // 重置
        // =============================================================

        /// <summary>
        /// 重置旅程状态（新游戏时调用）
        /// </summary>
        public void ResetJourneyState()
        {
            if (_gameManager == null) return;
            _gameManager.CurrentJourney = 1;
            _gameManager.ClearJourneyNotification();
            _segmentObstacleCache = null;
        }

        // =============================================================
        // 静态辅助方法
        // =============================================================

        /// <summary>
        /// 获取旅程名称
        /// </summary>
        /// <param name="journey">旅程编号</param>
        /// <returns>旅程名称</returns>
        public static string GetJourneyName(int journey)
        {
            if (journey >= 1 && journey <= 5) return JourneyNames[journey];
            return $"旅程{journey}";
        }

        // =============================================================
        // 内部计算方法
        // =============================================================

        /// <summary>
        /// 计算指定路段中已清除路障的比例
        /// </summary>
        private float GetSegmentClearedPercent(RouteSegment segment)
        {
            if (_gameManager == null) return 0f;

            int totalInSegment = 0;
            int clearedInSegment = 0;
            var clearedIds = new HashSet<string>(_gameManager.ClearedObstacles);

            for (int i = 0; i < ObstacleRecords.Count; i++)
            {
                var o = ObstacleRecords[i];
                float routeZ = TerrainHeight.WorldToRouteZ(o.Z);
                if (routeZ >= segment.RouteZMin && routeZ < segment.RouteZMax)
                {
                    totalInSegment++;
                    if (clearedIds.Contains(o.Id))
                        clearedInSegment++;
                }
            }

            if (totalInSegment == 0) return 1f;
            return Mathf.Min(1f, (float)clearedInSegment / totalInSegment);
        }

        /// <summary>
        /// 获取指定路段已清除障碍物数
        /// </summary>
        private int GetSegmentClearedCount(RouteSegment segment)
        {
            if (_gameManager == null) return 0;
            var clearedIds = new HashSet<string>(_gameManager.ClearedObstacles);
            int count = 0;

            for (int i = 0; i < ObstacleRecords.Count; i++)
            {
                var o = ObstacleRecords[i];
                if (!clearedIds.Contains(o.Id)) continue;
                float routeZ = TerrainHeight.WorldToRouteZ(o.Z);
                if (routeZ >= segment.RouteZMin && routeZ < segment.RouteZMax)
                    count++;
            }

            return count;
        }

        /// <summary>
        /// 获取指定路段的营地数量
        /// </summary>
        private int GetSegmentCampCount(RouteSegment segment)
        {
            if (_gameManager == null) return 0;
            int count = 0;
            for (int i = 0; i < _gameManager.BuiltCamps.Count; i++)
            {
                var c = _gameManager.BuiltCamps[i];
                if (c.RouteZ >= segment.RouteZMin && c.RouteZ < segment.RouteZMax)
                    count++;
            }
            return count;
        }

        /// <summary>
        /// 获取总营地数量
        /// </summary>
        private int GetTotalCampCount()
        {
            if (_gameManager == null) return 0;
            return _gameManager.BuiltCamps.Count;
        }

        /// <summary>
        /// 检查是否有信号塔
        /// </summary>
        private bool HasSignalTower()
        {
            if (_gameManager == null) return false;
            foreach (var kvp in _gameManager.Stations)
            {
                if (kvp.Value.Facilities.Contains(FacilityType.SignalTower))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 检查是否到达山顶区域
        /// </summary>
        private bool HasReachedSummit()
        {
            if (_gameManager == null) return false;
            for (int i = 0; i < _gameManager.BuiltCamps.Count; i++)
            {
                if (_gameManager.BuiltCamps[i].RouteZ < -1400f)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 获取行驶距离（基于已清除的最远障碍物 routeZ）
        /// </summary>
        private float GetTravelDistance()
        {
            if (_gameManager == null) return 0f;
            var clearedIds = new HashSet<string>(_gameManager.ClearedObstacles);
            float minRouteZ = 470f; // 起点

            for (int i = 0; i < ObstacleRecords.Count; i++)
            {
                var o = ObstacleRecords[i];
                if (clearedIds.Contains(o.Id))
                {
                    float routeZ = TerrainHeight.WorldToRouteZ(o.Z);
                    if (routeZ < minRouteZ) minRouteZ = routeZ;
                }
            }

            return 470f - minRouteZ;
        }

        /// <summary>
        /// 预估路段障碍物总数（带缓存）
        /// </summary>
        private int GetEstimatedObstacleCount(RouteSegment segment)
        {
            if (_segmentObstacleCache == null)
            {
                _segmentObstacleCache = new Dictionary<string, int>();
                for (int i = 0; i < RouteSegments.Length; i++)
                {
                    int count = 0;
                    for (int j = 0; j < ObstacleRecords.Count; j++)
                    {
                        float routeZ = TerrainHeight.WorldToRouteZ(ObstacleRecords[j].Z);
                        if (routeZ >= RouteSegments[i].RouteZMin && routeZ < RouteSegments[i].RouteZMax)
                            count++;
                    }
                    _segmentObstacleCache[RouteSegments[i].Id] = count;
                }
            }
            return _segmentObstacleCache.TryGetValue(segment.Id, out var c) ? c : 1;
        }
    }

    // =================================================================
    // 障碍物记录（用于 JourneyManager 的路段统计）
    // =================================================================

    /// <summary>
    /// 障碍物位置记录
    /// </summary>
    [Serializable]
    public class ObstacleRecord
    {
        /// <summary>障碍物ID</summary>
        public string Id;
        /// <summary>世界坐标Z</summary>
        public float Z;
    }
}
CSHARPEOF; __tr_native_ec=$?; pwd -P >| '/var/folders/vy/3_69xc7918q7spv1v294mr7r0000gn/T/agent-toolhost/jobs/job-a48f256a097c498e915d64e183e7908b/cwd.txt'; exit "$__tr_native_ec"
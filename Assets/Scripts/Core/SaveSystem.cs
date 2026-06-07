using System;
using UnityEngine;

namespace WeiJinRoad.Core
{
    /// <summary>
    /// 存档系统 — 负责游戏状态的 JSON 序列化存取
    ///
    /// 使用 Unity 的 JsonUtility 进行序列化，
    /// 存档文件位于 Application.persistentDataPath/save.json。
    /// 支持自动存档、手动存档/读档/删除/检测。
    /// </summary>
    public static class SaveSystem
    {
        // =================================================================
        // 常量
        // =================================================================

        /// <summary>存档文件名</summary>
        private const string SaveFileName = "save.json";

        /// <summary>自动存档间隔（秒）</summary>
        private const float AutoSaveIntervalSeconds = 60f;

        // =================================================================
        // 属性
        // =================================================================

        /// <summary>
        /// 存档文件的完整路径
        /// </summary>
        public static string SavePath => System.IO.Path.Combine(
            Application.persistentDataPath, SaveFileName);

        /// <summary>
        /// 上次自动存档的时间
        /// </summary>
        private static float _lastAutoSaveTime;

        /// <summary>
        /// 是否启用自动存档
        /// </summary>
        public static bool AutoSaveEnabled { get; set; } = true;

        // =================================================================
        // 存档操作
        // =================================================================

        /// <summary>
        /// 保存游戏到文件
        ///
        /// 将 GameManager 当前状态序列化为 JSON 并写入存档文件。
        /// 保存成功后触发 OnGameSaved 事件。
        /// </summary>
        /// <returns>是否保存成功</returns>
        public static bool SaveGame()
        {
            try
            {
                var manager = GameManager.Instance;
                if (manager == null)
                {
                    Debug.LogWarning("[SaveSystem] GameManager 不存在，无法保存");
                    return false;
                }

                var saveData = manager.ToSaveData();
                string json = JsonUtility.ToJson(saveData, true);

                // 确保目录存在
                string dir = System.IO.Path.GetDirectoryName(SavePath);
                if (!System.IO.Directory.Exists(dir))
                {
                    System.IO.Directory.CreateDirectory(dir);
                }

                System.IO.File.WriteAllText(SavePath, json);
                GameEvents.OnGameSaved?.Invoke();
                Debug.Log($"[SaveSystem] 游戏已保存至 {SavePath}");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 保存失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 从文件加载游戏
        ///
        /// 读取存档文件并反序列化为 SaveData，然后恢复 GameManager 状态。
        /// 加载成功后触发 OnGameLoaded 事件。
        /// </summary>
        /// <returns>是否加载成功</returns>
        public static bool LoadGame()
        {
            try
            {
                if (!HasSave())
                {
                    Debug.LogWarning("[SaveSystem] 无存档文件");
                    return false;
                }

                string json = System.IO.File.ReadAllText(SavePath);
                var saveData = JsonUtility.FromJson<SaveData>(json);

                if (saveData == null)
                {
                    Debug.LogError("[SaveSystem] 存档数据反序列化失败");
                    return false;
                }

                var manager = GameManager.Instance;
                if (manager == null)
                {
                    Debug.LogError("[SaveSystem] GameManager 不存在，无法加载");
                    return false;
                }

                manager.FromSaveData(saveData);
                GameEvents.OnGameLoaded?.Invoke();
                Debug.Log("[SaveSystem] 游戏已加载");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 加载失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 删除存档文件
        /// </summary>
        /// <returns>是否删除成功</returns>
        public static bool DeleteSave()
        {
            try
            {
                if (!HasSave()) return true;
                System.IO.File.Delete(SavePath);
                Debug.Log("[SaveSystem] 存档已删除");
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveSystem] 删除存档失败: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// 检测是否存在存档文件
        /// </summary>
        /// <returns>是否存在存档</returns>
        public static bool HasSave()
        {
            return System.IO.File.Exists(SavePath);
        }

        // =================================================================
        // 自动存档
        // =================================================================

        /// <summary>
        /// 自动存档检查（应在每帧或定期调用）
        ///
        /// 当满足以下条件时自动保存：
        /// 1. 自动存档已启用
        /// 2. 距上次自动存档超过间隔时间
        /// </summary>
        public static void AutoSaveCheck()
        {
            if (!AutoSaveEnabled) return;
            if (Time.unscaledTime - _lastAutoSaveTime < AutoSaveIntervalSeconds) return;

            _lastAutoSaveTime = Time.unscaledTime;
            SaveGame();
        }

        /// <summary>
        /// 重置自动存档计时器
        ///
        /// 在重要状态变更后调用，延迟自动存档以避免频繁写入。
        /// </summary>
        public static void ResetAutoSaveTimer()
        {
            _lastAutoSaveTime = Time.unscaledTime;
        }

        /// <summary>
        /// 标记需要存档（在重要状态变更时调用）
        ///
        /// 将下次自动存档时间设为5秒后，避免同一帧多次变更导致频繁存档。
        /// </summary>
        public static void MarkDirty()
        {
            _lastAutoSaveTime = Time.unscaledTime - AutoSaveIntervalSeconds + 5f;
        }

        // =================================================================
        // 存档信息
        // =================================================================

        /// <summary>
        /// 获取存档文件的最后修改时间
        /// </summary>
        /// <returns>最后修改时间，无存档返回null</returns>
        public static DateTime? GetSaveTimestamp()
        {
            if (!HasSave()) return null;
            try
            {
                return System.IO.File.GetLastWriteTime(SavePath);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取存档文件大小（字节）
        /// </summary>
        /// <returns>文件大小，无存档返回0</returns>
        public static long GetSaveFileSize()
        {
            if (!HasSave()) return 0;
            try
            {
                var info = new System.IO.FileInfo(SavePath);
                return info.Length;
            }
            catch
            {
                return 0;
            }
        }
    }
}

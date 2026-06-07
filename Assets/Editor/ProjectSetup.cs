// =============================================================================
// ProjectSetup.cs — 未尽之路 Unity 项目一键配置编辑器脚本
//
// 提供菜单项 Tools/WeiJinRoad/Setup Project，一键完成：
//   1. URP 渲染管线配置
//   2. TMP 字体资源生成
//   3. GLB 模型导入与 Prefab 生成
//   4. 主场景创建（含完整游戏对象层级）
//   5. 核心 Prefab 创建
//   6. 构建设置配置
//
// 命令行调用：
//   -executeMethod WeiJinRoad.Editor.ProjectSetup.SetupAll
// =============================================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace WeiJinRoad.Editor
{
    /// <summary>
    /// 项目一键配置工具，通过菜单或命令行执行完整的项目初始化流程
    /// </summary>
    public static class ProjectSetup
    {
        // =====================================================================
        // 常量与路径
        // =====================================================================

        private const string SettingsDir = "Assets/Settings";
        private const string URPAssetPath = "Assets/Settings/URPAsset.asset";
        private const string UniversalRendererPath = "Assets/Settings/UniversalRenderer.asset";

        private const string FontSourcePath = "Assets/Fonts/zpix.ttf";
        private const string FontAssetPath = "Assets/Fonts/zpix SDF.asset";

        private const string ModelsDir = "Assets/Models";
        private const string PrefabModelsDir = "Assets/Prefabs/Models";

        private const string MainScenePath = "Assets/Scenes/MainScene.unity";
        private const string PrefabsDir = "Assets/Prefabs";

        // =====================================================================
        // 菜单项
        // =====================================================================

        [MenuItem("Tools/WeiJinRoad/Setup Project", false, 100)]
        public static void SetupAllMenuItem()
        {
            SetupAll();
        }

        // =====================================================================
        // SetupAll — 一键执行全部配置
        // =====================================================================

        /// <summary>
        /// 一键执行全部项目配置，可通过命令行调用：
        /// -executeMethod WeiJinRoad.Editor.ProjectSetup.SetupAll
        /// </summary>
        public static void SetupAll()
        {
            Debug.Log("[ProjectSetup] ===== 开始一键项目配置 =====");

            try
            {
                SetupURP();
                SetupTMPFont();
                ImportGLBModels();
                CreateMainScene();
                CreatePrefabs();
                SetupBuildSettings();

                Debug.Log("[ProjectSetup] ===== 全部配置完成！=====");

                // 非命令行模式下弹出完成对话框
                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog(
                        "项目配置完成",
                        "未尽之路项目已一键配置完成！\n\n" +
                        "• URP 渲染管线已设置\n" +
                        "• TMP 字体已生成\n" +
                        "• GLB 模型已导入\n" +
                        "• 主场景已创建\n" +
                        "• 核心 Prefab 已生成\n" +
                        "• 构建设置已配置",
                        "确定"
                    );
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ProjectSetup] 配置过程中发生错误：{ex.Message}\n{ex.StackTrace}");

                if (!Application.isBatchMode)
                {
                    EditorUtility.DisplayDialog(
                        "配置出错",
                        $"项目配置过程中发生错误：\n{ex.Message}",
                        "确定"
                    );
                }
            }
        }

        // =====================================================================
        // 1. SetupURP — 配置 URP 渲染管线
        // =====================================================================

        /// <summary>
        /// 创建 URP 资产并配置渲染管线：
        /// - 创建 Assets/Settings/ 目录
        /// - 创建 UniversalRenderPipelineAsset
        /// - 创建 UniversalRendererData
        /// - 设置 GraphicsSettings.currentRenderPipeline
        /// - 配置 HDR、MSAA 4x、阴影距离 150、主光阴影分辨率 2048
        /// </summary>
        public static void SetupURP()
        {
            Debug.Log("[ProjectSetup] 1/6 — 配置 URP 渲染管线...");

            // 确保目录存在
            EnsureDirectory(SettingsDir);

            // 创建 Universal Renderer Data (Unity 6 / URP 17.x: ForwardRendererData 已重命名为 UniversalRendererData)
            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(UniversalRendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                rendererData.name = "UniversalRenderer";
                AssetDatabase.CreateAsset(rendererData, UniversalRendererPath);
                Debug.Log("[ProjectSetup]   创建 UniversalRendererData: " + UniversalRendererPath);
            }
            else
            {
                Debug.Log("[ProjectSetup]   UniversalRendererData 已存在，跳过创建");
            }

            // 创建 URP Asset
            var urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(URPAssetPath);
            if (urpAsset == null)
            {
                urpAsset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
                urpAsset.name = "URPAsset";
                AssetDatabase.CreateAsset(urpAsset, URPAssetPath);
                Debug.Log("[ProjectSetup]   创建 UniversalRenderPipelineAsset: " + URPAssetPath);
            }
            else
            {
                Debug.Log("[ProjectSetup]   URPAsset 已存在，跳过创建");
            }

            // 分配 Renderer Data 到 Pipeline Asset
            urpAsset.rendererDataList = new ScriptableRendererData[] { rendererData };

            // 设置 UniversalRendererData 的渲染模式为 Forward (Unity 6 / URP 17.x)
            var rendererSO = new SerializedObject(rendererData);
            var renderingModeProp = rendererSO.FindProperty("m_RenderingMode");
            if (renderingModeProp != null)
            {
                // RenderingMode.Forward = 0
                renderingModeProp.intValue = 0;
                rendererSO.ApplyModifiedProperties();
                EditorUtility.SetDirty(rendererData);
            }

            // 通过 SerializedObject 配置 URP 参数
            SerializedObject so = new SerializedObject(urpAsset);

            // HDR 开启
            var hdrProp = so.FindProperty("m_SupportsHDR");
            if (hdrProp != null) hdrProp.boolValue = true;

            // MSAA 4x (URP 14+: m_MSAA renamed to m_MsaaSampleCount)
            var msaaProp = so.FindProperty("m_MsaaSampleCount");
            if (msaaProp != null) msaaProp.intValue = 4;

            // 阴影距离 150
            var shadowDistProp = so.FindProperty("m_ShadowDistance");
            if (shadowDistProp != null) shadowDistProp.floatValue = 150f;

            // 主光阴影分辨率 2048
            var mainLightShadowResProp = so.FindProperty("m_MainLightShadowmapResolution");
            if (mainLightShadowResProp != null) mainLightShadowResProp.intValue = 2048;

            // 主光阴影支持
            var mainLightShadowProp = so.FindProperty("m_MainLightShadowsSupported");
            if (mainLightShadowProp != null) mainLightShadowProp.boolValue = true;

            so.ApplyModifiedProperties();

            // 标记为脏并保存
            EditorUtility.SetDirty(urpAsset);
            AssetDatabase.SaveAssets();

            // 设置 GraphicsSettings
            GraphicsSettings.currentRenderPipeline = urpAsset;
            Debug.Log("[ProjectSetup]   已设置 GraphicsSettings.currentRenderPipeline");

            // 刷新 QualitySettings
            int qualityLevelCount = QualitySettings.names.Length;
            for (int i = 0; i < qualityLevelCount; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
            }
            QualitySettings.SetQualityLevel(QualitySettings.GetQualityLevel(), true);

            Debug.Log("[ProjectSetup]   URP 配置完成 (HDR=On, MSAA=4x, ShadowDist=150, MainLightShadowRes=2048)");
        }

        // =====================================================================
        // 2. SetupTMPFont — 配置 TMP 字体
        // =====================================================================

        /// <summary>
        /// 从 zpix.ttf 创建 TMP 字体资源：
        /// - 查找 Assets/Fonts/zpix.ttf
        /// - 使用 TMP_FontAssetCreator 创建 SDF 字体
        /// - 保存为 Assets/Fonts/zpix SDF.asset
        /// - 设置为 TMP_Settings 默认字体
        /// </summary>
        public static void SetupTMPFont()
        {
            Debug.Log("[ProjectSetup] 2/6 — 配置 TMP 字体...");

            // 检查字体源文件
            var font = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
            if (font == null)
            {
                Debug.LogWarning($"[ProjectSetup]   未找到字体文件: {FontSourcePath}，跳过 TMP 字体配置");
                return;
            }

            // 检查是否已有字体资产
            var existingFontAsset = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(FontAssetPath);
            if (existingFontAsset != null)
            {
                Debug.Log("[ProjectSetup]   TMP 字体资产已存在，跳过创建");
                SetDefaultTMPFont(existingFontAsset);
                return;
            }

            // 创建 TMP 字体资产
            try
            {
                CreateTMPFontAsset(font);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ProjectSetup]   自动创建 TMP 字体失败: {ex.Message}，请手动通过 Window > TextMeshPro > Font Asset Creator 创建");
            }
        }

        /// <summary>
        /// 通过 TMP API 创建字体资产
        /// </summary>
        private static void CreateTMPFontAsset(Font font)
        {
            // 使用 TMP_FontAsset.CreateFontAsset 创建基础字体
            var fontAsset = TMPro.TMP_FontAsset.CreateFontAsset(font);

            if (fontAsset == null)
            {
                Debug.LogWarning("[ProjectSetup]   TMP_FontAsset.CreateFontAsset 返回 null");
                return;
            }

            // 配置字体参数
            fontAsset.name = "zpix SDF";

            // 设置采样点大小
            fontAsset.faceInfo.pointSize = 42;

            // 保存图集纹理信息
            var atlasTexture = fontAsset.atlasTexture;
            if (atlasTexture != null)
            {
                Debug.Log($"[ProjectSetup]   当前图集尺寸: {atlasTexture.width}x{atlasTexture.height}");
            }

            // 保存字体资产
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

            // 保存图集纹理和材质为子资产
            if (atlasTexture != null)
            {
                AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);
            }

            if (fontAsset.material != null)
            {
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            EditorUtility.SetDirty(fontAsset);
            AssetDatabase.SaveAssets();

            Debug.Log($"[ProjectSetup]   TMP 字体资产已创建: {FontAssetPath}");

            // 设置为默认字体
            SetDefaultTMPFont(fontAsset);

            // 提示用户手动生成完整字符集
            Debug.Log("[ProjectSetup]   提示：如需完整 CJK 字符集，请通过 Window > TextMeshPro > Font Asset Creator 手动生成");
            Debug.Log("[ProjectSetup]   推荐设置：Point Size=42, Atlas=2048x2048, Character Set=ASCII + CJK Common");
        }

        /// <summary>
        /// 设置 TMP_Settings 默认字体
        /// </summary>
        private static void SetDefaultTMPFont(TMPro.TMP_FontAsset fontAsset)
        {
            if (fontAsset == null) return;

            var settings = TMPro.TMP_Settings.instance;
            if (settings != null)
            {
                var so = new SerializedObject(settings);
                var defaultFontProp = so.FindProperty("m_defaultFontAsset");
                if (defaultFontProp != null)
                {
                    defaultFontProp.objectReferenceValue = fontAsset;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(settings);
                    AssetDatabase.SaveAssets();
                    Debug.Log("[ProjectSetup]   已设置 TMP_Settings 默认字体");
                }
            }
            else
            {
                Debug.LogWarning("[ProjectSetup]   TMP_Settings 不存在，请先通过 Window > TextMeshPro > Import TMP Essentials 导入");
            }
        }

        // =====================================================================
        // 3. ImportGLBModels — 导入 GLB 模型并创建 Prefab
        // =====================================================================

        /// <summary>
        /// 查找所有 .glb 文件，触发重新导入，并为每个模型创建 Prefab
        /// </summary>
        public static void ImportGLBModels()
        {
            Debug.Log("[ProjectSetup] 3/6 — 导入 GLB 模型...");

            EnsureDirectory(PrefabModelsDir);

            // 收集所有 .glb 文件路径
            var glbPaths = new List<string>();

            // 通过 AssetDatabase 查找
            var allModelGuids = AssetDatabase.FindAssets("glob:*.glb", new[] { ModelsDir });
            foreach (var guid in allModelGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase) && !glbPaths.Contains(path))
                {
                    glbPaths.Add(path);
                }
            }

            // 通过文件系统直接查找（兜底）
            if (Directory.Exists(ModelsDir))
            {
                var files = Directory.GetFiles(ModelsDir, "*.glb", SearchOption.TopDirectoryOnly);
                foreach (var file in files)
                {
                    var assetPath = file.Replace("\\", "/");
                    if (!glbPaths.Contains(assetPath))
                    {
                        glbPaths.Add(assetPath);
                    }
                }
            }

            if (glbPaths.Count == 0)
            {
                Debug.Log("[ProjectSetup]   未找到 .glb 文件，跳过模型导入");
                return;
            }

            Debug.Log($"[ProjectSetup]   找到 {glbPaths.Count} 个 .glb 文件");

            foreach (var glbPath in glbPaths)
            {
                string fileName = Path.GetFileNameWithoutExtension(glbPath);
                string prefabPath = $"{PrefabModelsDir}/{fileName}.prefab";

                Debug.Log($"[ProjectSetup]   处理: {glbPath}");

                // 触发重新导入
                AssetDatabase.ImportAsset(glbPath, ImportAssetOptions.ForceUpdate | ImportAssetOptions.ImportRecursive);

                // 刷新并等待导入完成
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                // 尝试加载导入后的模型
                var modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(glbPath);
                if (modelPrefab == null)
                {
                    // glTFast 可能将模型导入为不同路径，尝试查找同名 Prefab
                    var autoPrefabGuids = AssetDatabase.FindAssets(fileName + " t:Prefab", new[] { ModelsDir });
                    foreach (var pg in autoPrefabGuids)
                    {
                        var pp = AssetDatabase.GUIDToAssetPath(pg);
                        modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(pp);
                        if (modelPrefab != null) break;
                    }
                }

                if (modelPrefab != null)
                {
                    // 检查 Prefab 是否已存在
                    if (!File.Exists(prefabPath))
                    {
                        var prefab = PrefabUtility.SaveAsPrefabAsset(modelPrefab, prefabPath);
                        if (prefab != null)
                        {
                            Debug.Log($"[ProjectSetup]   创建 Prefab: {prefabPath}");
                        }
                        else
                        {
                            Debug.LogWarning($"[ProjectSetup]   无法创建 Prefab: {prefabPath}");
                        }
                    }
                    else
                    {
                        Debug.Log($"[ProjectSetup]   Prefab 已存在: {prefabPath}");
                    }
                }
                else
                {
                    Debug.LogWarning($"[ProjectSetup]   无法加载模型: {glbPath}（可能需要 glTFast 插件）");
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup]   GLB 模型导入完成");
        }

        // =====================================================================
        // 4. CreateMainScene — 创建主场景
        // =====================================================================

        /// <summary>
        /// 创建主场景，包含完整游戏对象层级：
        /// Main Camera, Directional Light, GameManager, RoadSpline, Terrain,
        /// Vehicle (含 Body 子对象), Scenery, RoadObstacles, ResourceNodes,
        /// Stations, CampScene, TownScene, SnowSystem, FogSystem,
        /// AudioManager, InteractionSystem, UI Root (含所有 UI 子对象)
        /// </summary>
        public static void CreateMainScene()
        {
            Debug.Log("[ProjectSetup] 4/6 — 创建主场景...");

            EnsureDirectory("Assets/Scenes");

            // 创建新场景
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.SingleScene);

            // ── Main Camera ──
            var cameraObj = new GameObject("Main Camera");
            cameraObj.tag = "MainCamera";
            var camera = cameraObj.AddComponent<Camera>();
            camera.fieldOfView = 60f;
            camera.allowHDR = true;
            cameraObj.transform.position = new Vector3(20f, 20f, 20f);
            cameraObj.AddComponent<AudioListener>();
            AddMissingScript<WeiJinRoad.Vehicle.FollowCamera>(cameraObj, "FollowCamera");
            Debug.Log("[ProjectSetup]   创建 Main Camera");

            // ── Directional Light ──
            var lightObj = new GameObject("Directional Light");
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            Color lightColor;
            ColorUtility.TryParseHtmlString("#C8D8F0", out lightColor);
            light.color = lightColor;
            light.intensity = 0.8f;
            light.shadows = LightShadows.Soft;
            lightObj.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            AddMissingScript<WeiJinRoad.World.EnvironmentLighting>(lightObj, "EnvironmentLighting");
            Debug.Log("[ProjectSetup]   创建 Directional Light");

            // ── GameManager ──
            var gameManagerObj = new GameObject("GameManager");
            AddMissingScript<WeiJinRoad.Core.GameManager>(gameManagerObj, "GameManager");
            AddMissingScript<WeiJinRoad.Core.SaveSystem>(gameManagerObj, "SaveSystem");
            AddMissingScript<WeiJinRoad.Core.JourneyManager>(gameManagerObj, "JourneyManager");
            AddMissingScript<WeiJinRoad.Core.AchievementSystem>(gameManagerObj, "AchievementSystem");
            Debug.Log("[ProjectSetup]   创建 GameManager");

            // ── RoadSpline ──
            var roadSplineObj = new GameObject("RoadSpline");
            AddMissingScript<WeiJinRoad.World.RoadSpline>(roadSplineObj, "RoadSpline");
            Debug.Log("[ProjectSetup]   创建 RoadSpline");

            // ── Terrain ──
            var terrainObj = new GameObject("Terrain");
            AddMissingScript<WeiJinRoad.World.TerrainGenerator>(terrainObj, "TerrainGenerator");
            Debug.Log("[ProjectSetup]   创建 Terrain");

            // ── Vehicle ──
            var vehicleObj = new GameObject("Vehicle");
            AddMissingScript<WeiJinRoad.Vehicle.VehicleController>(vehicleObj, "VehicleController");
            AddMissingScript<WeiJinRoad.Vehicle.VehicleDamageSystem>(vehicleObj, "VehicleDamageSystem");

            // Vehicle 子对象 Body
            var bodyObj = new GameObject("Body");
            bodyObj.transform.SetParent(vehicleObj.transform, false);
            var bodyMF = bodyObj.AddComponent<MeshFilter>();
            bodyMF.mesh = CreateBoxMesh();
            var bodyMR = bodyObj.AddComponent<MeshRenderer>();
            bodyMR.material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            bodyMR.material.color = new Color(0.4f, 0.35f, 0.3f);
            Debug.Log("[ProjectSetup]   创建 Vehicle (含 Body 子对象)");

            // ── Scenery ──
            var sceneryObj = new GameObject("Scenery");
            AddMissingScript<WeiJinRoad.World.SceneryElements>(sceneryObj, "SceneryElements");
            Debug.Log("[ProjectSetup]   创建 Scenery");

            // ── RoadObstacles ──
            var roadObstaclesObj = new GameObject("RoadObstacles");
            AddMissingScript<WeiJinRoad.World.RoadObstacles>(roadObstaclesObj, "RoadObstacles");
            Debug.Log("[ProjectSetup]   创建 RoadObstacles");

            // ── ResourceNodes ──
            var resourceNodesObj = new GameObject("ResourceNodes");
            AddMissingScript<WeiJinRoad.World.ResourceNodes>(resourceNodesObj, "ResourceNodes");
            Debug.Log("[ProjectSetup]   创建 ResourceNodes");

            // ── Stations ──
            var stationsObj = new GameObject("Stations");
            AddMissingScript<WeiJinRoad.World.Stations>(stationsObj, "Stations");
            Debug.Log("[ProjectSetup]   创建 Stations");

            // ── CampScene ──
            var campSceneObj = new GameObject("CampScene");
            AddMissingScript<WeiJinRoad.World.CampScene>(campSceneObj, "CampScene");
            Debug.Log("[ProjectSetup]   创建 CampScene");

            // ── TownScene ──
            var townSceneObj = new GameObject("TownScene");
            AddMissingScript<WeiJinRoad.World.TownScene>(townSceneObj, "TownScene");
            Debug.Log("[ProjectSetup]   创建 TownScene");

            // ── SnowSystem ──
            var snowSystemObj = new GameObject("SnowSystem");
            AddMissingScript<WeiJinRoad.Effects.SnowSystem>(snowSystemObj, "SnowSystem");
            Debug.Log("[ProjectSetup]   创建 SnowSystem");

            // ── FogSystem ──
            var fogSystemObj = new GameObject("FogSystem");
            AddMissingScript<WeiJinRoad.Effects.FogSystem>(fogSystemObj, "FogSystem");
            Debug.Log("[ProjectSetup]   创建 FogSystem");

            // ── AudioManager ──
            var audioManagerObj = new GameObject("AudioManager");
            AddMissingScript<WeiJinRoad.Audio.AudioManager>(audioManagerObj, "AudioManager");
            Debug.Log("[ProjectSetup]   创建 AudioManager");

            // ── InteractionSystem ──
            var interactionSystemObj = new GameObject("InteractionSystem");
            AddMissingScript<WeiJinRoad.Interaction.InteractionSystem>(interactionSystemObj, "InteractionSystem");
            Debug.Log("[ProjectSetup]   创建 InteractionSystem");

            // ── UI Root ──
            var uiRootObj = new GameObject("UI Root");
            var canvas = uiRootObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            uiRootObj.AddComponent<CanvasScaler>();
            uiRootObj.AddComponent<GraphicRaycaster>();
            Debug.Log("[ProjectSetup]   创建 UI Root (Canvas)");

            // UI 子对象
            CreateUIChild(uiRootObj, "MainMenu", "WeiJinRoad.UI.MainMenuUI");
            CreateUIChild(uiRootObj, "HUD", "WeiJinRoad.UI.HUD");
            CreateUIChild(uiRootObj, "ResourceHUD", "WeiJinRoad.UI.ResourceHUD");
            CreateUIChild(uiRootObj, "MapUI", "WeiJinRoad.UI.MapUI");
            CreateUIChild(uiRootObj, "JournalUI", "WeiJinRoad.UI.JournalUI");
            CreateUIChild(uiRootObj, "CampUI", "WeiJinRoad.UI.CampUI");
            CreateUIChild(uiRootObj, "TownUI", "WeiJinRoad.UI.TownUI");
            CreateUIChild(uiRootObj, "BuildMenu", "WeiJinRoad.UI.BuildMenu");
            CreateUIChild(uiRootObj, "RepairMenu", "WeiJinRoad.UI.RepairMenu");
            CreateUIChild(uiRootObj, "TabMenu", "WeiJinRoad.UI.TabMenu");
            CreateUIChild(uiRootObj, "SettingsPage", "WeiJinRoad.UI.SettingsPage");
            CreateUIChild(uiRootObj, "AchievementToast", "WeiJinRoad.UI.AchievementToast");
            CreateUIChild(uiRootObj, "CinematicIntro", "WeiJinRoad.UI.CinematicIntro");
            CreateUIChild(uiRootObj, "DevTools", "WeiJinRoad.UI.DevToolsUI");
            CreateUIChild(uiRootObj, "MusicPlayer", "WeiJinRoad.UI.MusicPlayerUI");
            CreateUIChild(uiRootObj, "NarrativeOverlay", "WeiJinRoad.UI.NarrativeOverlay");
            CreateUIChild(uiRootObj, "ChapterPrompts", "WeiJinRoad.UI.ChapterPrompts");
            CreateUIChild(uiRootObj, "InteractionPrompt", "WeiJinRoad.UI.InteractionPrompt");

            Debug.Log("[ProjectSetup]   创建全部 UI 子对象");

            // 保存场景
            EditorSceneManager.SaveScene(scene, MainScenePath);
            Debug.Log($"[ProjectSetup]   主场景已保存: {MainScenePath}");
        }

        // =====================================================================
        // 5. CreatePrefabs — 创建核心 Prefab
        // =====================================================================

        /// <summary>
        /// 从场景中的关键游戏对象创建 Prefab：
        /// - Vehicle.prefab
        /// - Terrain.prefab
        /// - GameManager.prefab
        /// - AudioManager.prefab
        /// </summary>
        public static void CreatePrefabs()
        {
            Debug.Log("[ProjectSetup] 5/6 — 创建核心 Prefab...");

            EnsureDirectory(PrefabsDir);

            // 打开主场景
            var scene = EditorSceneManager.OpenScene(MainScenePath);

            // 需要创建 Prefab 的对象名称映射
            var prefabMappings = new Dictionary<string, string>
            {
                { "Vehicle",      $"{PrefabsDir}/Vehicle.prefab" },
                { "Terrain",      $"{PrefabsDir}/Terrain.prefab" },
                { "GameManager",  $"{PrefabsDir}/GameManager.prefab" },
                { "AudioManager", $"{PrefabsDir}/AudioManager.prefab" }
            };

            foreach (var mapping in prefabMappings)
            {
                string objName = mapping.Key;
                string prefabPath = mapping.Value;

                // 在场景中查找对象
                var sceneObj = scene.GetRootGameObjects().FirstOrDefault(go => go.name == objName);
                if (sceneObj == null)
                {
                    Debug.LogWarning($"[ProjectSetup]   场景中未找到对象: {objName}，跳过 Prefab 创建");
                    continue;
                }

                // 检查 Prefab 是否已存在
                if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
                {
                    Debug.Log($"[ProjectSetup]   Prefab 已存在: {prefabPath}");
                    continue;
                }

                // 保存为 Prefab
                var prefab = PrefabUtility.SaveAsPrefabAsset(sceneObj, prefabPath);
                if (prefab != null)
                {
                    Debug.Log($"[ProjectSetup]   创建 Prefab: {prefabPath}");
                }
                else
                {
                    Debug.LogWarning($"[ProjectSetup]   无法创建 Prefab: {prefabPath}");
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[ProjectSetup]   核心 Prefab 创建完成");
        }

        // =====================================================================
        // 6. SetupBuildSettings — 配置构建设置
        // =====================================================================

        /// <summary>
        /// 将 MainScene 添加到 EditorBuildSettings.scenes
        /// </summary>
        public static void SetupBuildSettings()
        {
            Debug.Log("[ProjectSetup] 6/6 — 配置构建设置...");

            // 获取 MainScene 的 GUID
            var sceneGuid = AssetDatabase.AssetPathToGUID(MainScenePath);
            if (string.IsNullOrEmpty(sceneGuid))
            {
                Debug.LogWarning($"[ProjectSetup]   无法获取场景 GUID: {MainScenePath}");
                return;
            }

            // 检查是否已在 Build Settings 中
            var existingScenes = EditorBuildSettings.scenes.ToList();
            bool alreadyAdded = existingScenes.Any(s => s.guid.ToString() == sceneGuid);

            if (alreadyAdded)
            {
                Debug.Log("[ProjectSetup]   MainScene 已在 Build Settings 中");
                return;
            }

            // 添加到 Build Settings（索引 0，即首场景）
            var newScene = new EditorBuildSettingsScene(sceneGuid, true);
            existingScenes.Insert(0, newScene);
            EditorBuildSettings.scenes = existingScenes.ToArray();

            Debug.Log("[ProjectSetup]   MainScene 已添加到 Build Settings (索引 0)");
        }

        // =====================================================================
        // 辅助方法
        // =====================================================================

        /// <summary>
        /// 确保目录存在
        /// </summary>
        private static void EnsureDirectory(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                Debug.Log($"[ProjectSetup]   创建目录: {path}");
            }
        }

        /// <summary>
        /// 尝试为 GameObject 添加脚本组件。
        /// 如果脚本类型不存在（尚未编译），则记录警告。
        /// </summary>
        private static void AddMissingScript<T>(GameObject go, string scriptName) where T : MonoBehaviour
        {
            try
            {
                go.AddComponent<T>();
                Debug.Log($"[ProjectSetup]     添加组件: {typeof(T).Name}");
            }
            catch (Exception)
            {
                Debug.LogWarning($"[ProjectSetup]     脚本 {scriptName} ({typeof(T).FullName}) 未找到，请确认脚本已编译");
            }
        }

        /// <summary>
        /// 创建 UI 子对象并尝试挂载脚本
        /// </summary>
        private static void CreateUIChild(GameObject parent, string name, string scriptTypeName)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent.transform, false);

            // 添加 RectTransform（UI 对象必须有）
            var rectTransform = child.AddComponent<RectTransform>();
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;

            // 尝试通过反射添加脚本
            TryAddScriptByName(child, scriptTypeName);
        }

        /// <summary>
        /// 通过类型全名尝试添加脚本组件
        /// </summary>
        private static void TryAddScriptByName(GameObject go, string typeName)
        {
            // 在所有已加载的程序集中查找类型
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                try
                {
                    var type = assembly.GetType(typeName);
                    if (type != null && typeof(MonoBehaviour).IsAssignableFrom(type))
                    {
                        go.AddComponent(type);
                        return;
                    }
                }
                catch
                {
                    // 忽略程序集加载错误
                }
            }

            Debug.LogWarning($"[ProjectSetup]     脚本 {typeName} 未找到，对象 {go.name} 未挂载脚本");
        }

        /// <summary>
        /// 创建简单的 Box 网格（用于 Vehicle Body 占位）
        /// </summary>
        private static Mesh CreateBoxMesh()
        {
            // 使用 Unity 内置的立方体网格
            var primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var mesh = primitive.GetComponent<MeshFilter>().sharedMesh;
            DestroyImmediate(primitive);

            var boxMesh = new Mesh
            {
                name = "VehicleBodyBox",
                vertices = mesh.vertices,
                triangles = mesh.triangles,
                normals = mesh.normals,
                uv = mesh.uv
            };
            return boxMesh;
        }
    }
}

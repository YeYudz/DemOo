using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Game.HotUpdate;
using Game.HybridCLR;
using GameUI;
using UnityEngine;
using UnityEngine.UI;
using YooAsset;
using YokiFrame;
using YokiFrame.Unity;

namespace Game
{
    public class GameBootstrap : MonoBehaviour
    {
        [Header("UI配置")]
        [Tooltip("UI设计参考分辨率")]
        public Vector2 ReferenceResolution = new Vector2(1920, 1080);

        [Tooltip("宽高匹配权重 (0=宽度优先, 1=高度优先)")]
        [Range(0f, 1f)]
        public float MatchWidthOrHeight = 0.5f;

        /// <summary>
        /// YooAsset 热更新进度事件：(进度值 0~1, 状态文本)
        /// </summary>
        public static event Action<float, string> HotUpdateProgressChanged;

        /// <summary>
        /// YooAsset 热更新完成事件：(是否成功, 错误信息)
        /// </summary>
        public static event Action<bool, string> HotUpdateCompleted;

        /// <summary>
        /// 进入游戏事件：InitPanel 点击进入按钮触发。
        /// 由 GameBootstrap 桥接，HotUpdate 业务入口接管 UI 切换。
        /// </summary>
        public static event Action EnterGameRequested;

        /// <summary>
        /// 供 InitPanel 调用的桥接方法：触发 EnterGameRequested 事件。
        /// </summary>
        public static void TriggerEnterGame()
        {
            EnterGameRequested?.Invoke();
        }

        private void Awake()
        {
            ApplyVideoSettings();
            InitializeYokiFrame();
            ConfigureUIKitSettings();
            ForceCreateUIRoot();
            ApplyCanvasScalerConfig();
            ConfigureUIPanelLoader();
            InitializeSystems();

            // 监听 InitPanel 的进入游戏事件
            EnterGameRequested += OnEnterGameRequested;

            // 直接打开 InitPanel（ResourcesPanelLoaderPool 不依赖 YooAsset）
            OpenInitPanel();
        }

        private void OnDestroy()
        {
            EnterGameRequested -= OnEnterGameRequested;
        }

        private void OnEnterGameRequested()
        {
            LogKit.Log("[GameBootstrap] EnterGameRequested 收到，启动 HotUpdate 业务入口...");
            HybridCLRManager.Instance.StartHotUpdateEntry();
        }

        private void Start()
        {
            // 面板显示后，异步初始化 YooAsset 并做热更新
            StartCoroutine(DelayedHostUpdate());
        }

        private IEnumerator DelayedHostUpdate()
        {
            LogKit.Log("[GameBootstrap] DelayedHostUpdate 启动，等待 3 帧...");

            // 等 3 帧确保界面渲染完成
            yield return null;
            yield return null;
            yield return null;

#if UNITY_EDITOR
            // 编辑器下用模拟进度
            LogKit.Log("[GameBootstrap] 使用编辑器模拟模式");
            yield return SimulateEditorHotUpdate();
#else
            // 打包后初始化 YooAsset（HostPlayMode）并做热更新下载
            yield return InitializeHostPlayMode();
#endif

            LogKit.Log("[GameBootstrap] DelayedHostUpdate 完成");
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器下模拟热更新进度（仅用于 UI 预览，不走真实 YooAsset 流程）。
        /// </summary>
        private IEnumerator SimulateEditorHotUpdate()
        {
            LogKit.Log("[SimulateEditor] 开始模拟编辑器热更新流程");

            string[] steps =
            {
                "正在初始化资源管理器...",
                "资源管理器初始化完成",
                "版本检查完成",
                "正在下载资源...",
                "资源下载完成",
                "加载热更新代码...",
                "资源检查完成\n点击任意位置进入游戏",
            };

            float[] progresses = { 0f, 0.1f, 0.3f, 0.3f, 0.85f, 0.95f, 1f };

            for (int i = 0; i < steps.Length; i++)
            {
                LogKit.Log($"[SimulateEditor] 步骤 {i + 1}/{steps.Length}: {steps[i]}");

                if (i == 5)
                {
                    // 编辑器中直接从 Assets/DataConfig 读取配置。
                    GameDataMgr.Instance.ReloadConfigs();

                    // HybridCLR 代码加载步骤
                    LogKit.Log("[SimulateEditor] 正在加载 HybridCLR 热更新代码...");
                    HotUpdateProgressChanged?.Invoke(progresses[i], steps[i]);
                    yield return HybridCLRManager.Instance.LoadHotUpdateCodeAsync();
                    LogKit.Log("[SimulateEditor] HybridCLR 代码加载完成");
                }
                else
                {
                    HotUpdateProgressChanged?.Invoke(progresses[i], steps[i]);
                    yield return new WaitForSecondsRealtime(0.4f);
                }
            }

            LogKit.Log("[SimulateEditor] 模拟热更新流程完成");
            HotUpdateCompleted?.Invoke(true, null);
        }
#else
        /// <summary>
        /// 打包后使用 HostPlayMode 初始化 YooAsset (3.x API)，执行真正的热更新流程：
        /// 1) 初始化 YooAssets 系统
        /// 2) 创建 DefaultPackage 并以 HostPlayMode 初始化
        /// 3) 请求服务器版本号
        /// 4) 加载资源清单
        /// 5) 对比差异 → 下载差异资源
        /// 6) 加载 HybridCLR 热更新代码
        /// 若任一步骤失败，回退到离线模拟进度。
        /// </summary>
        private IEnumerator InitializeHostPlayMode()
        {
            HotUpdateProgressChanged?.Invoke(0f, "正在初始化资源管理器...");
            LogKit.Log("[HotUpdate] 开始初始化 YooAsset HostPlayMode");

            // 1. 初始化 YooAsset 系统
            YooAssets.Initialize();

            // 2. 创建资源包
            var package = YooAssets.CreatePackage("DefaultPackage");

            // 3. 准备远端资源服务
            var remote = new YooRemoteServices(YooHostSetup.DefaultHostServer, YooHostSetup.FallbackHostServer);

            // 4. 以 HostPlayMode 初始化资源包
            var initOptions = new HostPlayModeOptions
            {
                BuiltinFileSystemParameters = FileSystemParameters.CreateDefaultBuiltinFileSystemParameters(),
                CacheFileSystemParameters = FileSystemParameters.CreateDefaultSandboxFileSystemParameters(remote),
            };
            var initOp = package.InitializePackageAsync(initOptions);
            yield return initOp;

            if (initOp.Status != EOperationStatus.Succeeded)
            {
                LogKit.Warning("[HotUpdate] YooAsset HostPlayMode 初始化失败: " + initOp.Error);
                yield return SimulateOfflineProgress();
                yield break;
            }

            LogKit.Log("[HotUpdate] YooAsset 初始化成功");
            HotUpdateProgressChanged?.Invoke(0.1f, "资源管理器初始化完成");

            // 5. 请求服务器版本号
            var requestVersionOp = package.RequestPackageVersionAsync();
            yield return requestVersionOp;

            if (requestVersionOp.Status != EOperationStatus.Succeeded)
            {
                LogKit.Warning("[HotUpdate] 请求版本失败: " + requestVersionOp.Error);
                yield return SimulateOfflineProgress();
                yield break;
            }

            string version = requestVersionOp.PackageVersion;
            LogKit.Log($"[HotUpdate] 最新版本: {version}");

            // 6. 加载资源清单
            var loadManifestOp = package.LoadPackageManifestAsync(new LoadPackageManifestOptions(version, 60));
            yield return loadManifestOp;

            if (loadManifestOp.Status != EOperationStatus.Succeeded)
            {
                LogKit.Warning("[HotUpdate] 加载清单失败: " + loadManifestOp.Error);
                yield return SimulateOfflineProgress();
                yield break;
            }

            // 7. 创建下载器并下载差异资源
            var downloader = package.CreateResourceDownloader(
                new ResourceDownloaderOptions(8, 3));

            LogKit.Log($"[HotUpdate] 需要下载的文件数: {downloader.TotalDownloadCount}, 总字节数: {downloader.TotalDownloadBytes}");

            if (downloader.TotalDownloadCount > 0)
            {
                long totalBytes = downloader.TotalDownloadBytes;
                HotUpdateProgressChanged?.Invoke(0.1f,
                    $"正在下载资源...({downloader.TotalDownloadCount} 个文件)");

                downloader.DownloadProgressChanged += args =>
                {
                    float progress = totalBytes > 0
                        ? (float)downloader.CurrentDownloadBytes / totalBytes
                        : 0f;
                    float barValue = 0.1f + progress * 0.8f;
                    HotUpdateProgressChanged?.Invoke(barValue,
                        $"下载中...{downloader.CurrentDownloadCount}/{downloader.TotalDownloadCount}");
                };

                downloader.StartDownload();

                while (!downloader.IsDone)
                    yield return null;

                if (downloader.Status != EOperationStatus.Succeeded)
                {
                    LogKit.Error("[HotUpdate] 资源下载失败: " + downloader.Error);
                    HotUpdateProgressChanged?.Invoke(0f, "资源下载失败: " + downloader.Error);
                    HotUpdateCompleted?.Invoke(false, downloader.Error);
                    yield break;
                }

                LogKit.Log("[HotUpdate] 资源下载完成");
                HotUpdateProgressChanged?.Invoke(0.9f, "资源下载完成");
            }
            else
            {
                LogKit.Log("[HotUpdate] 资源已是最新，无需下载");
                HotUpdateProgressChanged?.Invoke(0.9f, "资源已是最新");
            }

            // 8. 切换 UI 加载器为 YooAsset（支持热更新后的资源加载）
            UIKit.SetPanelLoader(new YooAssetPanelLoaderPool());
            LogKit.Log("[HotUpdate] UI 加载器已切换为 YooAssetPanelLoaderPool");

            // 9. 切换 ResKit 资源提供者为 YooAsset（让 Sprite/Texture 等资源也走热更新）
            //    必须在业务资源首次加载前切换，切换后会清除旧的 Resources 缓存
            ResKit.SetProvider(new YooAssetResourceProvider(package));
            LogKit.Log("[HotUpdate] ResKit 资源提供者已切换为 YooAssetResourceProvider");

            // 最新资源下载完成并切换 Provider 后，再读取 DataConfig 配置。
            GameDataMgr.Instance.ReloadConfigs();

            // 10. 加载 HybridCLR 热更新代码（HotUpdate.dll + AOT 元数据）
            //     代码 DLL 通过 YooAsset 资源通道下载，与资源热更新合流
            HotUpdateProgressChanged?.Invoke(0.95f, "加载热更新代码...");
            yield return HybridCLRManager.Instance.LoadHotUpdateCodeAsync();

            // 11. 完成
            HotUpdateProgressChanged?.Invoke(1f, "资源检查完成\n点击任意位置进入游戏");
            HotUpdateCompleted?.Invoke(true, null);
        }

        /// <summary>
        /// 离线模拟进度（未打包资源时的降级方案）。
        /// </summary>
        private IEnumerator SimulateOfflineProgress()
        {
            string[] steps =
            {
                "正在初始化资源管理器...",
                "资源管理器初始化完成（离线模式）",
                "版本检查完成",
                "资源已是最新",
                "加载热更新代码...",
                "资源检查完成\n点击任意位置进入游戏",
            };

            float[] progresses = { 0f, 0.2f, 0.4f, 0.8f, 0.95f, 1f };

            for (int i = 0; i < steps.Length; i++)
            {
                if (i == 4)
                {
                    HotUpdateProgressChanged?.Invoke(progresses[i], steps[i]);
                    yield return HybridCLRManager.Instance.LoadHotUpdateCodeAsync();
                }
                else
                {
                    HotUpdateProgressChanged?.Invoke(progresses[i], steps[i]);
                    yield return new WaitForSecondsRealtime(0.3f);
                }
            }

            HotUpdateCompleted?.Invoke(true, null);
        }
#endif

        /// <summary>
        /// 应用持久化的视频设置（全屏 + 分辨率）。
        /// 在 YokiFrame/UI 初始化之前执行，避免后续 UI 重新计算布局时分辨率已切换好。
        /// 首次启动时 VideoSettingsData 默认为 全屏 + 1920x1080。
        /// </summary>
        private void ApplyVideoSettings()
        {
            var data = GameDataMgr.Instance.videoSettingsData;
            if (data == null)
            {
                LogKit.Warning("[GameBootstrap] videoSettingsData 为空，跳过视频设置应用");
                return;
            }

            try
            {
                Screen.SetResolution(data.width, data.height, data.isFullscreen);
                LogKit.Log($"[GameBootstrap] 视频设置已应用: {data.width}x{data.height}, 全屏={data.isFullscreen}");
            }
            catch (System.Exception e)
            {
                LogKit.Warning($"[GameBootstrap] 应用视频设置失败: {e.Message}");
            }
        }

        private void InitializeYokiFrame()
        {
            if (!YokiFrameKit.IsInitialized)
            {
                // 显式注册 Unity 适配器安装器，防止 IL2CPP managed code stripping 剥离仅通过反射发现的类型。
                // YokiFrameKit.Initialize 内部通过反射扫描 [YokiFrameKitDiscoverableInstaller] 特性来发现安装器，
                // 但 IL2CPP 打包时会剥离未被直接引用的类型，导致 AudioKit/InputKit 等后端未注入，
                // 运行时抛出 "AudioKit backend is not configured" 异常。
                // 此处通过 new 直接引用各 Installer 类型，确保 IL2CPP 保留它们。
                YokiFrameKit.RegisterInstaller(new UnityCoreKitInstaller());
                YokiFrameKit.RegisterInstaller(new UnityAudioKitInstaller());
                YokiFrameKit.RegisterInstaller(new UnityUIKitInstaller());
                YokiFrameKit.RegisterInstaller(new UnityInputKitInstaller());
                YokiFrameKit.RegisterInstaller(new UnityActionKitInstaller());
                YokiFrameKit.RegisterInstaller(new UnitySceneKitInstaller());
                YokiFrameKit.RegisterInstaller(new UnitySaveKitInstaller());

                YokiFrameKit.Initialize(YokiFrameEngine.Unity);
                LogKit.Log("YokiFrame initialized successfully");
            }
        }

        private void ConfigureUIKitSettings()
        {
            var settings = UIKitSettings.Instance;
            settings.ReferenceResolution = ReferenceResolution;
            settings.MatchWidthOrHeight = MatchWidthOrHeight;
            settings.ScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            settings.ScreenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            settings.SortOrder = 100;
            settings.PixelPerfect = false;

            LogKit.Log($"UIKit Settings configured - Resolution: {ReferenceResolution.x}x{ReferenceResolution.y}, Match: {MatchWidthOrHeight}, SortOrder: 100");
        }

        private void ForceCreateUIRoot()
        {
            var root = UIRoot.ExistingInstance;
            if (root != null)
            {
                LogKit.Log("UIRoot already exists: " + root.name);
                return;
            }

            // 先尝试正常创建（通过 Resources.Load UIKit 预制体）
            root = UIRoot.Instance;
            if (root != null)
            {
                LogKit.Log("UIRoot created via prefab: " + root.name);
                return;
            }

            // 回退方案：程序化创建 UIRoot（打包时 Resources.Load 可能找不到 PackageCache 中的预制体）
            LogKit.Warning("UIRoot prefab not found, creating programmatically...");
            root = CreateUIRootProgrammatically();
            if (root != null)
            {
                LogKit.Log("UIRoot created programmatically: " + root.name);

                // 关键：创建后立即访问 UIRoot.Instance 以触发 Initialize()
                // Initialize() 负责：InitializeComponents / InitializeUILevels / InitializeInputModule / InitializeLevelPanels / InitializeFocusSystem
                // 如果不触发 Initialize()，层级节点、输入模块等都不会被正确设置
                root = UIRoot.Instance;
                if (root != null)
                {
                    LogKit.Log("UIRoot initialized via FindAnyObjectByType: " + root.name);
                }
                else
                {
                    LogKit.Error("UIRoot.Instance returned null after programmatic creation!");
                }
            }
            else
            {
                LogKit.Error("Failed to create UIRoot!");
            }
        }

        /// <summary>
        /// 程序化创建 UIRoot，替代 Resources.Load("UIKit")。
        /// 解决 PackageCache 中的 Resources 预制体在打包后不被包含的问题。
        /// </summary>
        private UIRoot CreateUIRootProgrammatically()
        {
            var uiKitObj = new GameObject("UIKit", typeof(RectTransform));
            DontDestroyOnLoad(uiKitObj);

            var canvas = uiKitObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = uiKitObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = ReferenceResolution;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = MatchWidthOrHeight;

            uiKitObj.AddComponent<GraphicRaycaster>();

            // 创建 EventSystem
            var eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.transform.SetParent(uiKitObj.transform, false);
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // 创建 UIRoot 组件
            var root = uiKitObj.AddComponent<UIRoot>();
            root.Canvas = canvas;
            root.CanvasScaler = scaler;
            root.GraphicRaycaster = uiKitObj.GetComponent<GraphicRaycaster>();
            root.EventSystem = eventSystemObj.GetComponent<UnityEngine.EventSystems.EventSystem>();

            // 创建层级节点（名称必须与 UILevel 预定义名称一致，否则 InitializeUILevels() 会重新创建）
            // 预定义层级名称按 Order 升序：AlwayBottom(-200), Bg(-100), Hud(-50), Common(0), Toast(50), Pop(100), Guide(150), AlwayTop(200), CanvasPanel(300)
            string[] levelNames = { "AlwayBottom", "Bg", "Hud", "Common", "Toast", "Pop", "Guide", "AlwayTop", "CanvasPanel" };
            foreach (var levelName in levelNames)
            {
                var levelObj = new GameObject(levelName, typeof(RectTransform));
                levelObj.transform.SetParent(uiKitObj.transform, false);
                var rect = levelObj.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
            }

            return root;
        }

        private void ApplyCanvasScalerConfig()
        {
            var root = UIRoot.ExistingInstance;
            if (root == null) return;

            var scaler = root.CanvasScaler;
            if (scaler != null)
            {
                scaler.referenceResolution = ReferenceResolution;
                scaler.matchWidthOrHeight = MatchWidthOrHeight;
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

                if (root.Canvas != null)
                {
                    root.Canvas.sortingOrder = 100;
                }

                LogKit.Log("CanvasScaler config applied successfully");
            }
            else
            {
                LogKit.Warning("CanvasScaler not found on UIRoot");
            }
        }

        private void ConfigureUIPanelLoader()
        {
            // 使用基于 Resources.Load 的加载器，不依赖 YooAsset
            // UI 预制体位于 Resources/UIPrefabs/ 目录下
            UIKit.SetPanelLoader(new ResourcesPanelLoaderPool());
            LogKit.Log("Panel loader set to ResourcesPanelLoaderPool (Resources/UIPrefabs/)");
        }

        private void InitializeSystems()
        {
            // 各子系统初始化独立 try-catch，确保某个子系统失败不会阻断 InitPanel 显示。
            // Awake 调用顺序：InitializeSystems() → OpenInitPanel()，
            // 若此处抛出未捕获异常，OpenInitPanel() 永远不会执行，导致界面不显示。
            // 注意：BattlePreparationSystem 等业务系统已迁移到 HotUpdate 程序集，
            // 其初始化由 GameEntry.Start() 负责（HotUpdate 代码加载完成后触发）。

            try
            {
                // 初始化音频管理器（强制非懒加载），在 YokiFrameKit.Initialize 之后访问以确保 AudioKit 后端已注入
                var audio = AudioManager.Instance;
                if (audio != null)
                {
                    LogKit.Log("AudioManager initialized - AudioKit settings applied");
                }
                else
                {
                    LogKit.Warning("AudioManager failed to initialize");
                }
            }
            catch (System.Exception e)
            {
                LogKit.Error($"[GameBootstrap] AudioManager initialization failed: {e}");
            }
        }

        private void OpenInitPanel()
        {
            LogKit.Log("Opening InitPanel via UIKit");
            var panel = UIKit.OpenPanel<InitPanel>();
            if (panel == null)
            {
                LogKit.Error("[GameBootstrap] InitPanel failed to open! Check UIRoot initialization and Resources.Load path.");
            }
        }
    }
}
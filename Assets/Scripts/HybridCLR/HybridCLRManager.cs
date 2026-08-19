using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;
using YooAsset;
using YokiFrame;
using Game.HotUpdate;
using HybridCLR;

namespace Game.HybridCLR
{

    /// <summary>
    /// HybridCLR 代码热更新管理器（AOT 主工程）。
    /// 职责：
    /// 1) 通过 YooAsset 下载 HotUpdate.dll.bytes 与 AOT 元数据 .bytes
    /// 2) 调用 RuntimeApi.LoadMetadataForAOTAssembly 补充 AOT 元数据
    /// 3) 加载 HotUpdate.dll 程序集
    /// 4) 反射查找 IHotUpdateEntry 实现并调用 Start()
    /// </summary>
    public sealed class HybridCLRManager
    {
        public static HybridCLRManager Instance { get; } = new HybridCLRManager();

        private Assembly _hotUpdateAssembly;
        public Assembly HotUpdateAssembly => _hotUpdateAssembly;

        public event Action<bool, string> OnLoadCompleted;
        public event Action<float, string> OnLoadProgress;

        private HybridCLRManager() { }

        public IEnumerator LoadHotUpdateCodeAsync()
        {
            OnLoadProgress?.Invoke(0f, "准备加载代码热更新...");

#if UNITY_EDITOR
            yield return LoadInEditor();
#else
            yield return LoadInPlayer();
#endif
        }

        /// <summary>
        /// 用户点击"进入游戏"时调用，反射调用 HotUpdate 业务入口的 Start() 方法。
        /// Editor 和 Player 共用此方法，必须在 #if 块外部。
        /// </summary>
        public void StartHotUpdateEntry()
        {
            if (_hotUpdateAssembly == null)
            {
                LogKit.Error("[HybridCLR] HotUpdate 程序集未加载，无法启动业务入口");
                return;
            }

            var entry = HotUpdateEntryFinder.Find(_hotUpdateAssembly);
            if (entry == null)
            {
                LogKit.Error("[HybridCLR] 未找到 IHotUpdateEntry 实现");
                return;
            }

            try
            {
                LogKit.Log("[HybridCLR] 调用 GameEntry.Start()...");
                entry.Start();
                LogKit.Log("[HybridCLR] GameEntry.Start() 调用成功");
            }
            catch (Exception ex)
            {
                LogKit.Error("[HybridCLR] GameEntry.Start() 异常: " + ex);
            }
        }

#if UNITY_EDITOR
        private IEnumerator LoadInEditor()
        {
            LogKit.Log("[HybridCLR] 编辑器模式：直接加载 HotUpdate 程序集...");
            OnLoadProgress?.Invoke(0.3f, "编辑器模式：直接加载 HotUpdate 程序集...");
            yield return null;

            _hotUpdateAssembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "HotUpdate");

            if (_hotUpdateAssembly == null)
            {
                const string msg = "编辑器模式下未找到 HotUpdate 程序集，请确认已创建 HotUpdate.asmdef 且编译通过。";
                LogKit.Error("[HybridCLR] " + msg);
                OnLoadCompleted?.Invoke(false, msg);
                yield break;
            }

            LogKit.Log($"[HybridCLR] HotUpdate 程序集加载成功: {_hotUpdateAssembly.FullName}");

            var entry = HotUpdateEntryFinder.Find(_hotUpdateAssembly);
            if (entry == null)
            {
                const string msg = "HotUpdate 程序集中未找到 IHotUpdateEntry 实现，请检查 GameEntry 是否正确实现接口。";
                LogKit.Error("[HybridCLR] " + msg);
                OnLoadCompleted?.Invoke(false, msg);
                yield break;
            }

            LogKit.Log("[HybridCLR] IHotUpdateEntry 入口已就绪，等待用户进入游戏...");
            OnLoadProgress?.Invoke(1f, "代码热更新加载完成");
            OnLoadCompleted?.Invoke(true, null);
        }
#else
        private string _lastError;

        private IEnumerator LoadInPlayer()
        {
            var aotMetadataNames = new[]
            {
                "mscorlib.dll",
                "System.dll",
                "Assembly-CSharp.dll",
                "Shared.dll",
            };

            OnLoadProgress?.Invoke(0.1f, "下载 AOT 元数据...");
            foreach (var metaName in aotMetadataNames)
            {
                yield return DownloadAndLoadAotMetadata(metaName);
                if (!string.IsNullOrEmpty(_lastError))
                {
                    // AOT元数据缺失时跳过而非终止（部分DLL可能不需要元数据补充）
                    LogKit.Warning($"[HybridCLR] 跳过缺失的AOT元数据: {metaName} - {_lastError}");
                    _lastError = null;
                }
            }

            if (!string.IsNullOrEmpty(_lastError))
                yield break;

            OnLoadProgress?.Invoke(0.7f, "下载热更新代码...");
            yield return DownloadHotUpdateDll();
            if (!string.IsNullOrEmpty(_lastError))
            {
                OnLoadCompleted?.Invoke(false, _lastError);
                yield break;
            }

            OnLoadProgress?.Invoke(1f, "启动热更新入口...");
            var entry = HotUpdateEntryFinder.Find(_hotUpdateAssembly);
            if (entry == null)
            {
                const string msg = "HotUpdate 程序集中未找到 IHotUpdateEntry 实现，请检查 GameEntry 是否正确实现接口。";
                LogKit.Error("[HybridCLR] " + msg);
                OnLoadCompleted?.Invoke(false, msg);
                yield break;
            }

            LogKit.Log("[HybridCLR] IHotUpdateEntry 入口已就绪，等待用户进入游戏...");
            OnLoadCompleted?.Invoke(true, null);
        }

        private IEnumerator DownloadAndLoadAotMetadata(string assetName)
        {
            _lastError = null;
            var package = YooAssets.GetPackage("DefaultPackage");
            var handle = package.LoadAssetAsync<TextAsset>(assetName);
            yield return handle;

            if (handle.Status != EOperationStatus.Succeeded)
            {
                _lastError = $"下载 AOT 元数据失败: {assetName}, {handle.Error}";
                LogKit.Error("[HybridCLR] " + _lastError);
                yield break;
            }

            var asset = handle.AssetObject as TextAsset;
            if (asset == null || asset.bytes == null || asset.bytes.Length == 0)
            {
                _lastError = $"AOT 元数据为空: {assetName}";
                LogKit.Error("[HybridCLR] " + _lastError);
                yield break;
            }

            try
            {
                var error = RuntimeApi.LoadMetadataForAOTAssembly(asset.bytes, HomologousImageMode.SuperSet);
                if (error != LoadImageErrorCode.OK)
                {
                    _lastError = $"加载 AOT 元数据失败 {assetName}: {error}";
                    LogKit.Warning("[HybridCLR] " + _lastError);
                }
                else
                {
                    LogKit.Log($"[HybridCLR] 已加载 AOT 元数据: {assetName}");
                }
            }
            catch (Exception ex)
            {
                _lastError = $"加载 AOT 元数据异常 {assetName}: {ex.Message}";
                LogKit.Error("[HybridCLR] " + _lastError);
            }
        }

        private IEnumerator DownloadHotUpdateDll()
        {
            _lastError = null;
            var package = YooAssets.GetPackage("DefaultPackage");
            var handle = package.LoadAssetAsync<TextAsset>("HotUpdate.dll");
            yield return handle;

            if (handle.Status != EOperationStatus.Succeeded)
            {
                _lastError = $"下载 HotUpdate.dll 失败: {handle.Error}";
                LogKit.Error("[HybridCLR] " + _lastError);
                yield break;
            }

            var asset = handle.AssetObject as TextAsset;
            if (asset == null || asset.bytes == null || asset.bytes.Length == 0)
            {
                _lastError = "HotUpdate.dll 资源为空";
                LogKit.Error("[HybridCLR] " + _lastError);
                yield break;
            }

            try
            {
                _hotUpdateAssembly = Assembly.Load(asset.bytes);
                LogKit.Log($"[HybridCLR] HotUpdate 程序集加载成功: {_hotUpdateAssembly.FullName}");
            }
            catch (Exception ex)
            {
                _lastError = $"HotUpdate 程序集加载异常: {ex.Message}";
                LogKit.Error("[HybridCLR] " + _lastError);
            }
        }
#endif
    }
}

using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using YooAsset;
using YokiFrame;

namespace GameUI
{
    /// <summary>
    /// 基于 YooAsset 的面板加载池。
    /// 热更新完成后切换为此加载器，UI 预制体从 YooAsset 加载（支持热更新）。
    /// 地址规则：AddressByFileName（如 DecomposePanel.prefab → 地址 "DecomposePanel"）。
    /// </summary>
    public class YooAssetPanelLoaderPool : AbstractPanelLoaderPool
    {
        private const string DEFAULT_PACKAGE_NAME = "DefaultPackage";

        private readonly string _packageName;

        public YooAssetPanelLoaderPool(string packageName = DEFAULT_PACKAGE_NAME)
        {
            _packageName = packageName;
        }

        protected override IPanelLoader CreatePanelLoader() => new YooAssetPanelLoader(this, _packageName);

        /// <summary>
        /// 基于 YooAsset 的面板加载器。
        /// 通过资源地址加载 UI 预制体，支持热更新后加载最新版本。
        /// </summary>
        public class YooAssetPanelLoader : IPanelLoader
        {
            private readonly IPanelLoaderPool _pool;
            private readonly string _packageName;
            private AssetHandle _handle;
            private GameObject _prefab;

            public YooAssetPanelLoader(IPanelLoaderPool pool, string packageName)
            {
                _pool = pool;
                _packageName = packageName;
            }

            public GameObject Load(PanelHandler handler)
            {
                var address = handler.Type.Name;
                var package = YooAssets.GetPackage(_packageName);
                if (package == null)
                {
                    LogKit.Error("[YooAssetPanelLoader] 资源包不存在: " + _packageName);
                    return null;
                }

                _handle = package.LoadAssetSync<GameObject>(address);
                _prefab = _handle?.AssetObject as GameObject;
                if (_prefab == null)
                    LogKit.Error("[YooAssetPanelLoader] 加载失败: " + address);
                return _prefab;
            }

            public async void LoadAsync(PanelHandler handler, Action<GameObject> onLoadComplete)
            {
                try
                {
                    var prefab = await LoadAsync(handler, CancellationToken.None);
                    onLoadComplete?.Invoke(prefab);
                }
                catch (Exception e)
                {
                    LogKit.Exception(e);
                    onLoadComplete?.Invoke(null);
                }
            }

            public Task<GameObject> LoadAsync(PanelHandler handler, CancellationToken cancellationToken = default)
            {
                var address = handler.Type.Name;
                var package = YooAssets.GetPackage(_packageName);
                if (package == null)
                {
                    LogKit.Error("[YooAssetPanelLoader] 资源包不存在: " + _packageName);
                    return Task.FromResult<GameObject>(null);
                }

                _handle = package.LoadAssetAsync<GameObject>(address);
                var tcs = new TaskCompletionSource<GameObject>();

                _handle.Completed += handle =>
                {
                    _prefab = handle.AssetObject as GameObject;
                    if (_prefab == null)
                        LogKit.Error("[YooAssetPanelLoader] 异步加载失败: " + address);
                    tcs.SetResult(_prefab);
                };

                return tcs.Task;
            }

            public void UnLoadAndRecycle()
            {
                _handle?.Release();
                _handle = null;
                _prefab = null;
                _pool.RecycleLoader(this);
            }
        }
    }
}

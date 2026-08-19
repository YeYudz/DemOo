using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using YokiFrame;

namespace GameUI
{
    /// <summary>
    /// 基于 Resources.Load 的面板加载池。
    /// 不依赖 YooAsset/ResKit，直接从 Resources 文件夹加载面板预制体。
    /// 用于在 YooAsset 初始化之前加载核心 UI（如 InitPanel）。
    /// </summary>
    public class ResourcesPanelLoaderPool : AbstractPanelLoaderPool
    {
        /// <summary>
        /// Resources 文件夹下的相对路径前缀（不含 "Resources/" 前缀）。
        /// 面板预制体位于 Resources/UIPrefabs/ 下。
        /// </summary>
        public const string DEFAULT_PATH_PREFIX = "UIPrefabs";

        private readonly string _pathPrefix;

        public ResourcesPanelLoaderPool(string pathPrefix = DEFAULT_PATH_PREFIX)
        {
            _pathPrefix = pathPrefix;
        }

        protected override IPanelLoader CreatePanelLoader() => new ResourcesPanelLoader(this, _pathPrefix);

        /// <summary>
        /// 基于 Resources.Load 的面板加载器。
        /// </summary>
        public class ResourcesPanelLoader : IPanelLoader
        {
            private readonly IPanelLoaderPool _pool;
            private readonly string _pathPrefix;
            private GameObject _prefab;

            public ResourcesPanelLoader(IPanelLoaderPool pool, string pathPrefix)
            {
                _pool = pool;
                _pathPrefix = pathPrefix;
            }

            public GameObject Load(PanelHandler handler)
            {
                var path = BuildPath(handler);
                _prefab = Resources.Load<GameObject>(path);
                if (_prefab == null)
                    LogKit.Error("[ResourcesPanelLoader] 加载失败: Resources/" + path);
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
                var path = BuildPath(handler);
                _prefab = Resources.Load<GameObject>(path);
                if (_prefab == null)
                    LogKit.Error("[ResourcesPanelLoader] 异步加载失败: Resources/" + path);
                return Task.FromResult(_prefab);
            }

            public void UnLoadAndRecycle()
            {
                // Resources.Load 加载的资源由 Unity 自动管理，无需手动释放
                _prefab = null;
                _pool.RecycleLoader(this);
            }

            private string BuildPath(PanelHandler handler)
            {
                return _pathPrefix + "/" + handler.Type.Name;
            }
        }
    }
}

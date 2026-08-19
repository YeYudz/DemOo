using System.Collections.Generic;
using YooAsset;

namespace Game.HotUpdate
{
    /// <summary>
    /// 远端资源地址查询服务（YooAsset 3.x IRemoteService 接口）。
    /// 将文件名拼接到 CDN 主/备地址，返回按优先级排序的候选 URL 列表。
    /// </summary>
    public sealed class YooRemoteServices : IRemoteService
    {
        private readonly string _mainUrl;
        private readonly string _fallbackUrl;

        public YooRemoteServices(string defaultHostServer, string fallbackHostServer)
        {
            _mainUrl = (defaultHostServer ?? string.Empty).TrimEnd('/');
            _fallbackUrl = (fallbackHostServer ?? string.Empty).TrimEnd('/');
        }

        public IReadOnlyList<string> GetRemoteUrls(string fileName)
        {
            var main = $"{_mainUrl}/{fileName}";
            var fallback = $"{_fallbackUrl}/{fileName}";
            return new[] { main, fallback };
        }
    }

    /// <summary>
    /// YooAsset 配置常量（CDN 地址等）。
    /// </summary>
    public static class YooHostSetup
    {
        /// <summary>
        /// 默认 CDN 主服务器地址（PC 平台）。
        /// 指向版本号文件夹本身，YooAsset 会自动拼接 DefaultPackage.version 等文件名。
        /// </summary>
        public const string DefaultHostServer = "http://127.0.0.1:8080";

        /// <summary>
        /// 默认 CDN 备用服务器地址。
        /// </summary>
        public const string FallbackHostServer = "http://127.0.0.1:8080";
    }
}

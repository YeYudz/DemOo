using System;
using System.Linq;
using System.Reflection;

namespace Game.HotUpdate
{
    /// <summary>
    /// 热更新程序集入口接口。
    /// AOT 主工程通过反射查找实现此接口的类型并调用 Start，
    /// 避免 AOT 直接引用 HotUpdate 程序集的类型。
    /// </summary>
    public interface IHotUpdateEntry
    {
        /// <summary>
        /// 热更新程序集入口方法。YooAsset 资源 + HybridCLR 代码 DLL 都加载完成后调用。
        /// </summary>
        void Start();
    }

    /// <summary>
    /// 热更新入口查找器：按约定反射查找实现 IHotUpdateEntry 的类型。
    /// </summary>
    public static class HotUpdateEntryFinder
    {
        public static IHotUpdateEntry Find(Assembly hotUpdateAssembly)
        {
            if (hotUpdateAssembly == null)
                return null;

            Type entryType = null;
            try
            {
                entryType = hotUpdateAssembly
                    .GetTypes()
                    .FirstOrDefault(t =>
                        typeof(IHotUpdateEntry).IsAssignableFrom(t) &&
                        !t.IsAbstract && !t.IsInterface &&
                        t.IsPublic);
            }
            catch (ReflectionTypeLoadException ex)
            {
                entryType = ex.Types?
                    .FirstOrDefault(t => t != null &&
                        typeof(IHotUpdateEntry).IsAssignableFrom(t) &&
                        !t.IsAbstract && !t.IsInterface &&
                        t.IsPublic);
            }

            if (entryType == null)
                return null;

            return (IHotUpdateEntry)Activator.CreateInstance(entryType);
        }
    }
}

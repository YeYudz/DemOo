using Game.HotUpdate;
using GameUI;
using YokiFrame;
using Game.GameLogic.Battle.Core;

namespace Game.GameLogic
{
    /// <summary>
    /// 热更新业务入口。HybridCLR 加载完 HotUpdate.dll 后由 AOT 通过反射调用 Start()。
    /// 放在 HotUpdate 程序集内，可随时替换 DLL 实现代码热更。
    /// </summary>
    public class GameEntry : IHotUpdateEntry
    {
        /// <summary>
        /// 启动热更新业务，完成角色处理器、备战系统和首个登录面板的初始化。
        /// </summary>
        public void Start()
        {
            LogKit.Log("[GameEntry] 热更新业务入口启动");
            LogKit.Log("[GameEntry] V4: 角色处理器注册器模式已启用");

            #region 角色处理器初始化
            // 1) 激活所有角色专属 SkillHandler（通过反射自动发现）
            //    - 新增角色时：只需创建 XxxSkillHandler : ICharacterSkillHandler
            //    - 不需要改 GameEntry，自动注册
            try
            {
                CharacterHandlerRegistry.EnsureAllRegistered();
                LogKit.Log($"[GameEntry] 已注册角色: {string.Join(", ", CharacterHandlerRegistry.AllCharacterIds)}");

                // 演示：Knight 星魂等级 3 的被动
                var knight = CharacterHandlerRegistry.Get("Knight");
                if (knight != null)
                {
                    var effects = knight.GetUnlockedStarEffects(3);
                    LogKit.Log($"[GameEntry] Knight 星魂3 解锁 {effects.Count} 个效果:");
                    foreach (var e in effects)
                        LogKit.Log($"  · {e}");
                }
            }
            catch (System.Exception e)
            {
                LogKit.Error($"[GameEntry] 角色处理器注册失败: {e}");
            }
            #endregion

            #region 备战系统初始化
            // 2) 初始化备战系统（强制非懒加载），确保 BattleStart、遗器卸下、星魂解锁、天赋升级等事件监听器在游戏启动时就已注册
            try
            {
                var battlePrep = BattlePreparationSystem.Instance;
                if (battlePrep != null)
                {
                    LogKit.Log("[GameEntry] BattlePreparationSystem initialized - all event listeners registered");
                }
                else
                {
                    LogKit.Warning("[GameEntry] BattlePreparationSystem failed to initialize");
                }
            }
            catch (System.Exception e)
            {
                LogKit.Error($"[GameEntry] BattlePreparationSystem initialization failed: {e}");
            }
            #endregion

            #region 初始界面
            // 3) 打开登录面板（InitPanel 点击进入后由 GameEntry 接管）
            UIKit.OpenPanel<LoginPanel>();
            #endregion
        }
    }
}

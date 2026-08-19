#region 战斗管理器生命周期

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YokiFrame;
using TurnManagement;
using EnergyManagement;
using CardManagement;
using WaveManagement;
using UnitManagement;
using StateManagement;

namespace Core
{
/// <summary>
/// 战斗模块中的 BattleManagerLifecycle，负责对应的战斗流程处理。
/// </summary>
    public static class BattleManagerLifecycle
    {
        private static bool sIsInitialized = false;

        public static bool IsInitialized => sIsInitialized;

    /// <summary>
    /// 执行 InitializeAllManagers 相关的战斗处理。
    /// </summary>
        public static void InitializeAllManagers()
        {
            if (sIsInitialized)
            {
                LogKit.Warning("[BattleManagerLifecycle] 战斗管理器已初始化，跳过重复初始化");
                return;
            }

            TurnOrderManager.Instance.ClearAll();
            CardDeck.Instance.Clear();
            CardHand.Instance.Clear();
            EnergyManager.Instance.ResetEnergy();
            BattleStateManager.Instance.StartBattle();
            WaveManager.Instance.Reset();
            BattleUnitFactory.Instance.ClearPool();

            sIsInitialized = true;
            LogKit.Log("[BattleManagerLifecycle] 所有战斗管理器已初始化");
        }

    /// <summary>
    /// 执行 DisposeAllManagers 相关的战斗处理。
    /// </summary>
        public static void DisposeAllManagers()
        {
            if (!sIsInitialized)
            {
                LogKit.Warning("[BattleManagerLifecycle] 战斗管理器未初始化，跳过销毁");
                return;
            }

            TurnOrderManager.Dispose();
            CardDeck.Dispose();
            CardHand.Dispose();
            EnergyManager.Dispose();
            BattleStateManager.Dispose();
            WaveManager.Dispose();
            BattleUnitFactory.Dispose();

            sIsInitialized = false;
            LogKit.Log("[BattleManagerLifecycle] 所有战斗管理器已销毁");
        }

    /// <summary>
    /// 执行 VerifyAllManagersInitialized 相关的战斗处理。
    /// </summary>
        public static bool VerifyAllManagersInitialized()
        {
            return TurnOrderManager.Instance.IsInitialized &&
                   CardDeck.Instance.IsInitialized &&
                   CardHand.Instance.IsInitialized &&
                   EnergyManager.Instance.IsInitialized &&
                   BattleStateManager.Instance.IsInitialized &&
                   WaveManager.Instance.IsInitialized &&
                   BattleUnitFactory.Instance.IsInitialized;
        }
    }
}

#endregion

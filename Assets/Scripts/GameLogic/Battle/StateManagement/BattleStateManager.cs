#region 战斗状态管理

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YokiFrame;

namespace StateManagement
{
/// <summary>
/// 战斗模块中的 BattleStateManager，负责对应的战斗流程处理。
/// </summary>
    public class BattleStateManager:Singleton<BattleStateManager>
    {
        private BattleState mCurrentState;
        private bool mIsBattleWin;
        private bool mIsBattleLose;
        private bool mIsBattleActive;
        private bool mIsInitialized = false;

        public BattleState CurrentState => mCurrentState;
        public bool IsBattleWin => mIsBattleWin;
        public bool IsBattleLose => mIsBattleLose;
        public bool IsBattleActive => mIsBattleActive;
        public bool IsInitialized => mIsInitialized;

        public BattleStateManager()
        {
            mIsBattleActive = false;
            mIsBattleWin = false;
            mIsBattleLose = false;
            mCurrentState = BattleState.BattleStart;
        }

    /// <summary>
    /// 执行 StartBattle 相关的战斗处理。
    /// </summary>
        public void StartBattle()
        {
            mIsBattleActive = true;
            mIsBattleWin = false;
            mIsBattleLose = false;
            mIsInitialized = true;
            ChangeState(BattleState.BattleStart);
        }

    /// <summary>
    /// 执行 EndBattle 相关的战斗处理。
    /// </summary>
        public void EndBattle()
        {
            mIsBattleActive = false;
        }

    /// <summary>
    /// 执行 ChangeState 相关的战斗处理。
    /// </summary>
        public void ChangeState(BattleState newState)
        {
            mCurrentState = newState;

            EventKit.Type.Send(new StateEvents.BattleStateChangeEvent
            {
                NewState = newState
            });
        }

    /// <summary>
    /// 执行 CheckBattleEndConditions 相关的战斗处理。
    /// </summary>
        public void CheckBattleEndConditions(int alivePlayerCount, int aliveEnemyCount, bool isLastWave, bool isWaveActive)
        {
            mIsBattleLose = alivePlayerCount <= 0;
            mIsBattleWin = isLastWave && aliveEnemyCount <= 0 && !isWaveActive;

            if (mIsBattleWin)
            {
                EventKit.Type.Send(new StateEvents.BattleWinEvent());
                mIsBattleActive = false;
            }
            else if (mIsBattleLose)
            {
                EventKit.Type.Send(new StateEvents.BattleLoseEvent());
                mIsBattleActive = false;
            }
        }
    }
}

#endregion

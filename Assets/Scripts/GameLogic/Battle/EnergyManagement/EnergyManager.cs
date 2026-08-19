#region 能量管理

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YokiFrame;

namespace EnergyManagement
{
/// <summary>
/// 战斗模块中的 EnergyManager，负责对应的战斗流程处理。
/// </summary>
    public class EnergyManager : Singleton<EnergyManager>
    {

        private const int MAX_ENERGY = 10;

        private int mCurrentEnergy;
        private bool mIsInitialized = false;

        public int CurrentEnergy => mCurrentEnergy;
        public int MaxEnergy => MAX_ENERGY;
        public bool IsInitialized => mIsInitialized;

        public EnergyManager()
        {
            mCurrentEnergy = 0;
        }

    /// <summary>
    /// 执行 OnTurnStart 相关的战斗处理。
    /// </summary>
        public void OnTurnStart()
        {
            GainEnergy(2);
        }

    /// <summary>
    /// 执行 SpendEnergy 相关的战斗处理。
    /// </summary>
        public bool SpendEnergy(int cost)
        {
            if (mCurrentEnergy < cost)
            {
                EventKit.Type.Send(new EnergyEvents.EnergySpendEvent
                {
                    Amount = cost,
                    Success = false
                });
                return false;
            }

            mCurrentEnergy -= cost;

            EventKit.Type.Send(new EnergyEvents.EnergyChangedEvent
            {
                CurrentEnergy = mCurrentEnergy,
                MaxEnergy = MAX_ENERGY,
                ChangeAmount = -cost
            });

            EventKit.Type.Send(new EnergyEvents.EnergySpendEvent
            {
                Amount = cost,
                Success = true
            });

            return true;
        }

    /// <summary>
    /// 执行 GainEnergy 相关的战斗处理。
    /// </summary>
        public void GainEnergy(int amount)
        {
            int oldEnergy = mCurrentEnergy;
            mCurrentEnergy = Mathf.Min(MAX_ENERGY, mCurrentEnergy + amount);
            int actualGain = mCurrentEnergy - oldEnergy;

            EventKit.Type.Send(new EnergyEvents.EnergyChangedEvent
            {
                CurrentEnergy = mCurrentEnergy,
                MaxEnergy = MAX_ENERGY,
                ChangeAmount = actualGain
            });

            EventKit.Type.Send(new EnergyEvents.EnergyGainEvent
            {
                Amount = actualGain
            });
        }

    /// <summary>
    /// 执行 ResetEnergy 相关的战斗处理。
    /// </summary>
        public void ResetEnergy()
        {
            mCurrentEnergy = 0;
            mIsInitialized = true;
        }
    }
}

#endregion

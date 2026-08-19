#region 发牌器行动序

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurnManagement
{
/// <summary>
/// 战斗模块中的 CardDealerTurnOrder，负责对应的战斗流程处理。
/// </summary>
    public class CardDealerTurnOrder : ITurnOrder
    {
        private BattleUnitData mUnit;

        public string UnitId => mUnit.UnitId;
        public BattleUnitType UnitType => mUnit.UnitType;
        public float Speed => mUnit.CurrentSpeed;
        public float CurrentActionValue { get; set; }
        public float BaseActionValue { get; set; }
        public bool IsAlive => mUnit.IsAlive;

        public CardDealerTurnOrder(BattleUnitData unit)
        {
            mUnit = unit;
            CurrentActionValue = 0f;
            BaseActionValue = 0f;
        }
    }
}

#endregion

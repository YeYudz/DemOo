#region 行动序接口

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurnManagement
{
/// <summary>
/// 战斗流程中的接口定义，约束相关对象的行为。
/// </summary>
    public interface ITurnOrder
    {
        string UnitId { get; }
        BattleUnitType UnitType { get; }
        float Speed { get; }
        float CurrentActionValue { get; set; }
        float BaseActionValue { get; set; }
        bool IsAlive { get; }
    }
}

#endregion

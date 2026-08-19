#region 回合事件

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TurnManagement;

namespace TurnEvents
{
/// <summary>
/// 战斗模块中的 UnitActingEvent，负责对应的战斗流程处理。
/// </summary>
    public class UnitActingEvent
    {
        public string UnitId { get; set; }
        public BattleUnitType UnitType { get; set; }
    }

/// <summary>
/// 战斗模块中的 ActionCompleteEvent，负责对应的战斗流程处理。
/// </summary>
    public class ActionCompleteEvent
    {
        public string UnitId { get; set; }
        public BattleUnitType UnitType { get; set; }
    }

/// <summary>
/// 战斗模块中的 TurnOrderUpdatedEvent，负责对应的战斗流程处理。
/// </summary>
    public class TurnOrderUpdatedEvent
    {
        public List<ITurnOrder> Units { get; set; }
    }
}

#endregion

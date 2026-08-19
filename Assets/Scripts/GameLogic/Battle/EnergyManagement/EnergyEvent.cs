#region 能量事件

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace EnergyEvents
{
/// <summary>
/// 战斗模块中的 EnergyChangedEvent，负责对应的战斗流程处理。
/// </summary>
    public class EnergyChangedEvent
    {
        public int CurrentEnergy { get; set; }
        public int MaxEnergy { get; set; }
        public int ChangeAmount { get; set; }
    }

/// <summary>
/// 战斗模块中的 EnergyGainEvent，负责对应的战斗流程处理。
/// </summary>
    public class EnergyGainEvent
    {
        public int Amount { get; set; }
    }

/// <summary>
/// 战斗模块中的 EnergySpendEvent，负责对应的战斗流程处理。
/// </summary>
    public class EnergySpendEvent
    {
        public int Amount { get; set; }
        public bool Success { get; set; }
    }
}

#endregion

#region 单位事件

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnitEvents
{
/// <summary>
/// 战斗模块中的 UnitDamagedEvent，负责对应的战斗流程处理。
/// </summary>
    public class UnitDamagedEvent
    {
        public BattleUnitData Unit { get; set; }
        public int Damage { get; set; }
        public BattleUnitData Attacker { get; set; }
    }

/// <summary>
/// 战斗模块中的 UnitHealedEvent，负责对应的战斗流程处理。
/// </summary>
    public class UnitHealedEvent
    {
        public BattleUnitData Unit { get; set; }
        public int HealAmount { get; set; }
    }

/// <summary>
/// 战斗模块中的 UnitDefeatedEvent，负责对应的战斗流程处理。
/// </summary>
    public class UnitDefeatedEvent
    {
        public BattleUnitData Unit { get; set; }
    }

/// <summary>
/// 战斗模块中的 UnitSpawnedEvent，负责对应的战斗流程处理。
/// </summary>
    public class UnitSpawnedEvent
    {
        public BattleUnitData Unit { get; set; }
    }

/// <summary>
/// 战斗模块中的 UnitShieldEvent，负责对应的战斗流程处理。
/// </summary>
    public class UnitShieldEvent
    {
        public BattleUnitData Unit { get; set; }
        public int ShieldAmount { get; set; }
    }

/// <summary>
/// 战斗模块中的 UnitActionEvent，负责对应的战斗流程处理。
/// </summary>
    public class UnitActionEvent
    {
        public BattleUnitData Unit { get; set; }
        public string ActionType { get; set; }
        public BattleUnitData Target { get; set; }
    }

/// <summary>
/// 战斗模块中的 UnitHealthUpdatedEvent，负责对应的战斗流程处理。
/// </summary>
    public class UnitHealthUpdatedEvent
    {
        public BattleUnitData Unit { get; set; }
    }
}

#endregion

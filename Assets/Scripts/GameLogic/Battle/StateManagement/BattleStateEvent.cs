#region 战斗状态事件

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StateEvents
{
/// <summary>
/// 战斗模块中的 BattleStateChangeEvent，负责对应的战斗流程处理。
/// </summary>
    public class BattleStateChangeEvent
    {
        public BattleState NewState { get; set; }
    }

/// <summary>
/// 战斗模块中的 BattleWinEvent，负责对应的战斗流程处理。
/// </summary>
    public class BattleWinEvent
    {
        // 总经验值
        public int TotalExp { get; set; }
        // 通关奖励物品列表
        public List<AccountItemData> RewardItems { get; set; } = new List<AccountItemData>();
    }

/// <summary>
/// 战斗模块中的 BattleLoseEvent，负责对应的战斗流程处理。
/// </summary>
    public class BattleLoseEvent
    {
    }
}

#endregion

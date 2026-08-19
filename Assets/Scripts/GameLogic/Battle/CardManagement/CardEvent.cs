#region 卡牌事件

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardEvents
{
/// <summary>
/// 战斗模块中的 CardDrawnEvent，负责对应的战斗流程处理。
/// </summary>
    public class CardDrawnEvent
    {
        public SkillCardData Card { get; set; }
        public int HandCount { get; set; }
    }

/// <summary>
/// 战斗模块中的 CardPlayedEvent，负责对应的战斗流程处理。
/// </summary>
    public class CardPlayedEvent
    {
        public SkillCardData Card { get; set; }
        public BattleUnitData Target { get; set; }
    }

/// <summary>
/// 战斗模块中的 CardDiscardedEvent，负责对应的战斗流程处理。
/// </summary>
    public class CardDiscardedEvent
    {
        public SkillCardData Card { get; set; }
    }

/// <summary>
/// 战斗模块中的 HandUpdatedEvent，负责对应的战斗流程处理。
/// </summary>
    public class HandUpdatedEvent
    {
        public List<SkillCardData> Cards { get; set; }
    }
}

#endregion

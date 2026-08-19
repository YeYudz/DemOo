#region 波次事件

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WaveEvents
{
/// <summary>
/// 战斗模块中的 WaveStartEvent，负责对应的战斗流程处理。
/// </summary>
    public class WaveStartEvent
    {
        public int WaveIndex { get; set; }
    }

/// <summary>
/// 战斗模块中的 EnemySpawnEvent，负责对应的战斗流程处理。
/// </summary>
    public class EnemySpawnEvent
    {
        public EnemyData Enemy { get; set; }
        public BattleUnitData Unit { get; set; }
    }

/// <summary>
/// 战斗模块中的 WaveCompleteEvent，负责对应的战斗流程处理。
/// </summary>
    public class WaveCompleteEvent
    {
        public int WaveIndex { get; set; }
    }

/// <summary>
/// 战斗模块中的 AllWavesCompleteEvent，负责对应的战斗流程处理。
/// </summary>
    public class AllWavesCompleteEvent
    {
    }
}

#endregion

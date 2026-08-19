#region 单位对象池

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UnitManagement
{
/// <summary>
/// 战斗模块中的 UnitPool，负责对应的战斗流程处理。
/// </summary>
    public class UnitPool
    {
        private Stack<BattleUnitData> mPool = new Stack<BattleUnitData>();
        private const int MAX_POOL_SIZE = 50;

        public int Count => mPool.Count;

    /// <summary>
    /// 执行 Get 相关的战斗处理。
    /// </summary>
        public BattleUnitData Get()
        {
            if (mPool.Count > 0)
                return mPool.Pop();
            return null;
        }

    /// <summary>
    /// 执行 Return 相关的战斗处理。
    /// </summary>
        public void Return(BattleUnitData unit)
        {
            if (mPool.Count < MAX_POOL_SIZE)
            {
                mPool.Push(unit);
            }
        }

    /// <summary>
    /// 执行 Clear 相关的战斗处理。
    /// </summary>
        public void Clear()
        {
            mPool.Clear();
        }
    }
}

#endregion

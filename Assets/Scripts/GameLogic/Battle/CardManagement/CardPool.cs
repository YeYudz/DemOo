#region 卡牌对象池

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CardManagement
{
/// <summary>
/// 战斗模块中的 CardPool，负责对应的战斗流程处理。
/// </summary>
    public class CardPool
    {
        private Stack<SkillCardData> mPool = new Stack<SkillCardData>();

        public int Count => mPool.Count;

    /// <summary>
    /// 执行 Push 相关的战斗处理。
    /// </summary>
        public void Push(SkillCardData card)
        {
            mPool.Push(card);
        }

    /// <summary>
    /// 执行 Pop 相关的战斗处理。
    /// </summary>
        public SkillCardData Pop()
        {
            if (mPool.Count == 0)
                return null;
            return mPool.Pop();
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

#region 行动序管理

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YokiFrame;
using System.Linq;

namespace TurnManagement
{
    /// <summary>
    /// 行动序管理器（TurnOrderManager）
    ///
    /// 核心概念：
    /// - 行动值（ActionValue, AV）：单位距离下次行动的"剩余路程"
    /// - 基础行动值（BaseActionValue）= 10000 / 速度
    /// - AV越小越接近行动，AV=0表示该单位正在行动
    /// - 速度越快 → BaseAV越小 → 越早行动 → 单位时间内行动次数越多
    ///
    /// 回合流转：
    /// 1. Update() 选取AV最小的单位作为行动者
    /// 2. 行动者执行技能/普攻（由BattleSystem驱动）
    /// 3. CompleteAction() 结算：
    ///    a. 所有单位的AV减少"行动者当前AV"（模拟时间流逝）
    ///    b. 行动者AV重置为BaseAV（开始新一轮跑表）
    ///    c. 应用延迟行动提前（行动期间触发的AdvanceTurn）
    ///
    /// 跨波次规则：
    /// - 保留所有单位的当前AV（不重置）
    /// - 新敌人通过AddUnit加入，AV=BaseAV
    /// - 行动序自然按当前AV重排
    ///
    /// 行动提前（AdvanceTurn）：
    /// - ratio=1.0表示把单位从当前位置直接拉到终点（AV=0）让其立即行动一次
    /// - 行动完成后，该单位下次AV仍为BaseAV（不继承提前量）
    /// - 若提前目标是当前正在行动的单位，延迟到CompleteAction后应用
    ///   （用于Assassin刺杀状态击杀后立即再行动）
    /// </summary>
/// <summary>
/// 战斗模块中的 TurnOrderManager，负责对应的战斗流程处理。
/// </summary>
    public class TurnOrderManager : Singleton<TurnOrderManager>
    {
        // 行动阈值常数：BaseAV = ACTION_VALUE_THRESHOLD / Speed
        private const float ACTION_VALUE_THRESHOLD = 10000f;

        private List<ITurnOrder> mUnits = new List<ITurnOrder>();
        private ITurnOrder mCurrentActor;
        private bool mIsProcessingAction = false;
        private bool mIsInitialized = false;

        // 下次行动值倍率：用于Warrior终结技HP转化等"下次AV变化"效果
        private Dictionary<string, float> mNextActionValueMultipliers = new Dictionary<string, float>();

        // 延迟行动提前：当前行动者在行动期间被提前时，推迟到CompleteAction后应用
        // 用途：Assassin刺杀状态击杀敌方后立即再行动
        private Dictionary<string, float> mPendingAdvances = new Dictionary<string, float>();

        public ITurnOrder CurrentActor => mCurrentActor;
        public bool IsProcessingAction => mIsProcessingAction;
        public int UnitCount => mUnits.Count;
        public bool IsInitialized => mIsInitialized;

        /// <summary>
        /// 添加单位到行动序列。新单位AV=BaseAV。
        /// </summary>
        public void AddUnit(ITurnOrder unit)
        {
            if (!mUnits.Contains(unit))
            {
                unit.BaseActionValue = ACTION_VALUE_THRESHOLD / unit.Speed;
                unit.CurrentActionValue = unit.BaseActionValue;
                mUnits.Add(unit);
                LogKit.Log($"[TurnOrderManager] 添加单位：{unit.UnitId}, Speed:{unit.Speed}, BaseAV:{unit.BaseActionValue:F2}, CurrentAV:{unit.CurrentActionValue:F2}");
                NotifyTurnOrderUpdated();
            }
        }

        /// <summary>
        /// 从行动序列移除单位（死亡等）。
        /// </summary>
        public void RemoveUnit(string unitId)
        {
            var unit = mUnits.FirstOrDefault(u => u.UnitId == unitId);
            if (unit != null)
            {
                mUnits.Remove(unit);
                mPendingAdvances.Remove(unitId);
                mNextActionValueMultipliers.Remove(unitId);
                LogKit.Log($"[TurnOrderManager] 移除单位：{unitId}");
                NotifyTurnOrderUpdated();
            }
        }

        /// <summary>
        /// 每帧调用：选取AV最小的单位作为行动者。
        /// AV相同时，速度快的优先（确保快速单位在并排时先行动）。
        /// </summary>
        public void Update(float deltaTime)
        {
            if (mIsProcessingAction) return;

            var sortedUnits = GetUnitsInOrder();
            if (sortedUnits.Count > 0)
            {
                ITurnOrder nextActor = sortedUnits[0];

                if (mCurrentActor == null || mCurrentActor.UnitId != nextActor.UnitId)
                {
                    mCurrentActor = nextActor;
                    mIsProcessingAction = true;

                    // 星铁规则：行动开始时，所有单位的AV减去行动者的AV（时间流逝）
                    // 行动者AV归零（正在行动），其他单位AV同步减少
                    float consumedAV = mCurrentActor.CurrentActionValue;
                    foreach (var unit in mUnits)
                    {
                        if (unit.UnitId != mCurrentActor.UnitId)
                        {
                            unit.CurrentActionValue = Mathf.Max(0f, unit.CurrentActionValue - consumedAV);
                        }
                    }
                    mCurrentActor.CurrentActionValue = 0f;

                    LogKit.Log($"[TurnOrderManager] ▶ {mCurrentActor.UnitId} 行动，消耗AV:{consumedAV:F2}（速度:{mCurrentActor.Speed}）");

                    EventKit.Type.Send(new TurnEvents.UnitActingEvent
                    {
                        UnitId = mCurrentActor.UnitId,
                        UnitType = mCurrentActor.UnitType
                    });

                    NotifyTurnOrderUpdated();
                }
            }
        }

        /// <summary>
        /// 行动结算：重置行动者AV，应用延迟提前。
        ///
        /// 关键逻辑：
        /// 1. AV减法已在Update()中完成（行动开始时时间流逝）
        /// 2. 行动者AV重置为 BaseAV * multiplier（开始新一轮跑表）
        /// 3. 应用延迟行动提前（减少行动者新AV，使其更快再次行动）
        /// </summary>
    /// <summary>
    /// 执行 CompleteAction 相关的战斗处理。
    /// </summary>
        public void CompleteAction()
        {
            string actingUnitId = mCurrentActor != null ? mCurrentActor.UnitId : null;
            BattleUnitType actingUnitType = mCurrentActor != null ? mCurrentActor.UnitType : BattleUnitType.Player;

            if (mCurrentActor != null)
            {
                // 行动者AV重置为 BaseAV * multiplier
                float multiplier = 1f;
                if (mNextActionValueMultipliers.ContainsKey(mCurrentActor.UnitId))
                {
                    multiplier = mNextActionValueMultipliers[mCurrentActor.UnitId];
                    mNextActionValueMultipliers.Remove(mCurrentActor.UnitId);
                    LogKit.Log($"[TurnOrderManager] {mCurrentActor.UnitId} 应用下次行动值倍率: {multiplier:F2}");
                }
                mCurrentActor.CurrentActionValue = mCurrentActor.BaseActionValue * multiplier;

                // 应用延迟行动提前（Assassin刺杀状态击杀后再行动等）
                if (mPendingAdvances.ContainsKey(mCurrentActor.UnitId))
                {
                    float pendingAdvance = mPendingAdvances[mCurrentActor.UnitId];
                    mPendingAdvances.Remove(mCurrentActor.UnitId);
                    mCurrentActor.CurrentActionValue = Mathf.Max(0f, mCurrentActor.CurrentActionValue - pendingAdvance);
                    LogKit.Log($"[TurnOrderManager] {mCurrentActor.UnitId} 应用延迟行动提前: -{pendingAdvance:F2}, 最终AV={mCurrentActor.CurrentActionValue:F2}");
                }

                LogKit.Log($"[TurnOrderManager] ■ {mCurrentActor.UnitId} 行动完成，新AV:{mCurrentActor.CurrentActionValue:F2}");
            }
            mIsProcessingAction = false;
            mCurrentActor = null;

            EventKit.Type.Send(new TurnEvents.ActionCompleteEvent
            {
                UnitId = actingUnitId,
                UnitType = actingUnitType
            });
            NotifyTurnOrderUpdated();
        }

        /// <summary>
        /// 获取按行动值排序的单位列表。
        /// 排序规则（星穹铁道）：
        /// 1. AV越小越靠前（越快行动）
        /// 2. AV相同时，编队靠左的先动（mUnits索引小的优先）
        ///    我方和敌方同AV时，按编队/敌方站位顺序兜底
        /// </summary>
    /// <summary>
    /// 执行 GetUnitsInOrder 相关的战斗处理。
    /// </summary>
        public List<ITurnOrder> GetUnitsInOrder()
        {
            return mUnits.Where(u => u.IsAlive)
                .OrderBy(u => u.CurrentActionValue)
                .ThenBy(u => mUnits.IndexOf(u))
                .ToList();
        }

        /// <summary>
        /// 获取单位的行动进度百分比（0%=刚行动完，100%=即将行动）。
        /// </summary>
        public float GetProgressPercentage(string unitId)
        {
            var unit = mUnits.FirstOrDefault(u => u.UnitId == unitId);
            if (unit == null) return 0f;
            // 进度 = (BaseAV - CurrentAV) / BaseAV，但CurrentAV可能大于BaseAV（延迟效果）
            // 所以用 1 - CurrentAV/BaseAV 来表示
            float progress = 1f - (unit.CurrentActionValue / unit.BaseActionValue);
            return Mathf.Clamp01(progress) * 100f;
        }

        /// <summary>
        /// 获取单位当前AV值。
        /// </summary>
        public float GetCurrentActionValue(string unitId)
        {
            var unit = mUnits.FirstOrDefault(u => u.UnitId == unitId);
            return unit != null ? unit.CurrentActionValue : 0f;
        }

        /// <summary>
        /// 行动提前（拉条）：按总路程10000的比例，不是按剩余AV的比例。
        ///
        /// 规则（星穹铁道）：
        /// - 拉条X%: CurrentAV -= 10000 * X%（基于总路程，不是剩余距离）
        /// - 例：剩3000 AV的人拉50% = 减5000 → 直接到0
        /// - 例：剩3000 AV的人拉25% = 减2500 → 剩500
        /// - 若目标是当前正在行动的单位，延迟到CompleteAction后应用
        /// </summary>
    /// <summary>
    /// 执行 AdvanceTurn 相关的战斗处理。
    /// </summary>
        public void AdvanceTurn(string unitId, float ratio)
        {
            var unit = mUnits.FirstOrDefault(u => u.UnitId == unitId);
            if (unit == null)
            {
                LogKit.Warning($"[TurnOrderManager] AdvanceTurn找不到单位: {unitId}");
                return;
            }

            // 按总路程10000的比例，不是BaseAV的比例
            float advanceAmount = ACTION_VALUE_THRESHOLD * ratio;

            if (mIsProcessingAction && mCurrentActor != null && unitId == mCurrentActor.UnitId)
            {
                // 当前行动者在行动期间被提前：延迟到CompleteAction后应用
                if (!mPendingAdvances.ContainsKey(unitId))
                    mPendingAdvances[unitId] = 0f;
                mPendingAdvances[unitId] += advanceAmount;
                LogKit.Log($"[TurnOrderManager] {unitId} 延迟行动提前: +{advanceAmount:F2}（待行动完成后应用，当前累积:{mPendingAdvances[unitId]:F2}）");
            }
            else
            {
                // 非当前行动者：立即应用
                float oldAV = unit.CurrentActionValue;
                unit.CurrentActionValue = Mathf.Max(0f, unit.CurrentActionValue - advanceAmount);
                LogKit.Log($"[TurnOrderManager] {unitId} 行动提前: 总路程比例{ratio:P0}, 提前量={advanceAmount:F2}, AV:{oldAV:F2} → {unit.CurrentActionValue:F2}");
            }
            NotifyTurnOrderUpdated();
        }

        /// <summary>
        /// 设置单位下次行动值倍率。
        /// 用于Warrior终结技HP转化等"下次AV变化"效果。
        /// </summary>
        public void SetNextActionValueMultiplier(string unitId, float multiplier)
        {
            if (mNextActionValueMultipliers.ContainsKey(unitId))
            {
                mNextActionValueMultipliers[unitId] = multiplier;
            }
            else
            {
                mNextActionValueMultipliers.Add(unitId, multiplier);
            }
            LogKit.Log($"[TurnOrderManager] 设置 {unitId} 下次行动值倍率: {multiplier:F2}");
        }

        /// <summary>
        /// 行动延迟：将单位向后推。
        /// </summary>
        public void DelayTurn(string unitId, float ratio)
        {
            var unit = mUnits.FirstOrDefault(u => u.UnitId == unitId);
            if (unit != null)
            {
                unit.CurrentActionValue += unit.BaseActionValue * ratio;
                LogKit.Log($"[TurnOrderManager] {unitId} 行动延迟: +{unit.BaseActionValue * ratio:F2}, 新AV={unit.CurrentActionValue:F2}");
                NotifyTurnOrderUpdated();
            }
        }

        /// <summary>
        /// 速度变更：只改BaseAV，不改CurrentAV。
        /// 规则（星穹铁道）：提速/减速改的是速度白字/实战速度 → 改的是基础AV，
        /// 不改"现在在跑道哪"，只改"这次行动完下次多久再来"。
        /// </summary>
        public void SpeedUp(string unitId, float newSpeed)
        {
            var unit = mUnits.FirstOrDefault(u => u.UnitId == unitId);
            if (unit == null) return;

            float oldBaseAV = unit.BaseActionValue;
            float newBaseAV = ACTION_VALUE_THRESHOLD / newSpeed;

            // 只改BaseAV，CurrentAV保持不变
            unit.BaseActionValue = newBaseAV;

            LogKit.Log($"[TurnOrderManager] {unitId} 速度变更: BaseAV {oldBaseAV:F2} → {newBaseAV:F2} (speed={newSpeed}), CurrentAV保持={unit.CurrentActionValue:F2}");
            NotifyTurnOrderUpdated();
        }

        /// <summary>
        /// 获取未来N步行动序列（模拟，用于行动条显示）。
        ///
        /// 模拟规则（星穹铁道）：
        /// 1. 当前行动者置顶（累计AV=0，表示此刻正在行动）
        /// 2. 每次选取AV最小者行动，累计AV += 该单位的行动AV
        /// 3. 所有其他单位AV -= 该单位的行动AV，行动者AV重置为BaseAV
        /// 4. 快速单位可能在10步内出现多次，同一单位两次出现的间隔=BaseAV
        ///
        /// 显示规则：
        /// - 从左到右累计AV递增（时间流逝）
        /// - 同一单位多次出现时，间隔恰好=10000/速度
        /// - 累计AV表示"从现在到该单位行动时已流逝的总时间"
        /// </summary>
        public List<(ITurnOrder unit, float displayAV, bool isCurrent)> GetUpcomingActions(int count)
        {
            var result = new List<(ITurnOrder, float, bool)>();
            var alive = mUnits.Where(u => u.IsAlive).ToList();
            if (alive.Count == 0) return result;

            // 累计行动值（从现在开始累计流逝的时间）
            float cumulativeAV = 0f;

            // 当前行动者置顶（累计AV=0，此刻正在行动）
            if (mIsProcessingAction && mCurrentActor != null && mCurrentActor.IsAlive)
            {
                result.Add((mCurrentActor, 0f, true));
                // 当前行动者完成后，它的AV就是consumedAV（要消耗的时间）
                cumulativeAV = mCurrentActor.CurrentActionValue;
            }

            // 模拟AV字典
            var simAVs = new Dictionary<string, float>();
            float consumedAV = (mIsProcessingAction && mCurrentActor != null) ? mCurrentActor.CurrentActionValue : 0f;

            foreach (var unit in alive)
            {
                if (mIsProcessingAction && mCurrentActor != null && unit.UnitId == mCurrentActor.UnitId)
                {
                    // 当前行动者完成后AV重置为BaseAV
                    simAVs[unit.UnitId] = unit.BaseActionValue;
                }
                else
                {
                    // 其他单位：AV减去当前行动者将消耗的时间
                    simAVs[unit.UnitId] = Mathf.Max(0f, unit.CurrentActionValue - consumedAV);
                }
            }

            // 模拟后续行动
            int remaining = count - result.Count;
            for (int i = 0; i < remaining; i++)
            {
                // 找AV最小的单位（AV相同时编队靠左的先动）
                var nextKey = simAVs
                    .OrderBy(kv => kv.Value)
                    .ThenBy(kv =>
                    {
                        var u = alive.First(a => a.UnitId == kv.Key);
                        return mUnits.IndexOf(u);
                    })
                    .FirstOrDefault();

                if (nextKey.Key == null) break;

                var unit = alive.First(u => u.UnitId == nextKey.Key);
                float actingAV = nextKey.Value;

                // 累计AV += 该单位的行动AV（表示从开始到现在流逝的总时间）
                cumulativeAV += actingAV;
                result.Add((unit, cumulativeAV, false));

                // 模拟行动结算：所有其他单位AV -= actingAV，行动者AV重置为BaseAV
                foreach (var key in simAVs.Keys.ToList())
                {
                    if (key != nextKey.Key)
                        simAVs[key] = Mathf.Max(0f, simAVs[key] - actingAV);
                }
                simAVs[nextKey.Key] = unit.BaseActionValue;
            }

            return result;
        }

        /// <summary>
        /// 清空所有数据（战斗结束时调用）。
        /// </summary>
        public void ClearAll()
        {
            mUnits.Clear();
            mNextActionValueMultipliers.Clear();
            mPendingAdvances.Clear();
            mCurrentActor = null;
            mIsProcessingAction = false;
            mIsInitialized = true;
            LogKit.Log("[TurnOrderManager] 所有数据已清空");
        }

        /// <summary>
        /// 重置所有单位的行动值（仅用于战斗初始或特殊场景）。
        /// 跨波次不应调用此方法——应保留玩家当前行动值。
        /// </summary>
        public void ResetAllActionValues()
        {
            foreach (var unit in mUnits)
            {
                unit.BaseActionValue = ACTION_VALUE_THRESHOLD / unit.Speed;
                unit.CurrentActionValue = unit.BaseActionValue;
            }
            mPendingAdvances.Clear();
            LogKit.Log($"[TurnOrderManager] 所有单位行动值已重置（BaseAV=10000/速度），共{mUnits.Count}个单位");
            NotifyTurnOrderUpdated();
        }

        /// <summary>
        /// 获取所有单位的调试信息（用于日志排查）。
        /// </summary>
        public string GetDebugInfo()
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"[TurnOrderManager] 单位数:{mUnits.Count}, 当前行动者:{mCurrentActor?.UnitId ?? "无"}, 处理中:{mIsProcessingAction}");
            var sorted = GetUnitsInOrder();
            for (int i = 0; i < sorted.Count; i++)
            {
                var u = sorted[i];
                sb.AppendLine($"  [{i}] {u.UnitId} AV={u.CurrentActionValue:F2}/{u.BaseActionValue:F2} Speed={u.Speed}");
            }
            return sb.ToString();
        }

        private void NotifyTurnOrderUpdated()
        {
            EventKit.Type.Send(new TurnEvents.TurnOrderUpdatedEvent
            {
                Units = GetUnitsInOrder()
            });
        }
    }
}

#endregion

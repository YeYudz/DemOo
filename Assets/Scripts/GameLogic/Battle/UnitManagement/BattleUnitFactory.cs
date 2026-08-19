#region 战斗单位工厂

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YokiFrame;

namespace UnitManagement
{
/// <summary>
/// 战斗模块中的 BattleUnitFactory，负责对应的战斗流程处理。
/// </summary>
    public class BattleUnitFactory : Singleton<BattleUnitFactory>
    {

        private UnitPool mUnitPool;
        private bool mIsInitialized = false;

        public bool IsInitialized => mIsInitialized;

        public BattleUnitFactory()
        {
            mUnitPool = new UnitPool();
        }

    /// <summary>
    /// 执行 CreatePlayerUnit 相关的战斗处理。
    /// </summary>
        public BattleUnitData CreatePlayerUnit(string unitId, string name, int maxHp, int speed, int attack, int defense, int level = 1, 
            float critRate = 0.1f, float critDamage = 0.5f, float damageBonus = 0f, float resistancePenetration = 0f)
        {
            var unit = mUnitPool.Get();
            if (unit == null)
            {
                unit = new BattleUnitData(unitId, BattleUnitType.Player, name, maxHp, speed, attack, defense);
            }
            else
            {
                unit.UnitId = unitId;
                unit.UnitType = BattleUnitType.Player;
                unit.Name = name;
                unit.MaxHp = maxHp;
                unit.CurrentHp = maxHp;
                unit.BaseSpeed = speed;
                unit.BaseAttack = attack;
                unit.BaseDefense = defense;
                unit.IsAlive = true;
                unit.Shield = 0;
                unit.ResetBattleEffects();
            }

            unit.Level = level;
            unit.BaseCritRate = critRate;
            unit.BaseCritDamage = critDamage;
            unit.BaseDamageBonus = damageBonus;
            unit.BaseResistancePenetration = resistancePenetration;

            EventKit.Type.Send(new UnitEvents.UnitSpawnedEvent
            {
                Unit = unit
            });

            return unit;
        }

    /// <summary>
    /// 执行 CreateEnemyUnit 相关的战斗处理。
    /// </summary>
        public BattleUnitData CreateEnemyUnit(string unitId, string name, int maxHp, int speed, int attack, int defense, int level = 1, float resistance = 0f)
        {
            var unit = mUnitPool.Get();
            if (unit == null)
            {
                unit = new BattleUnitData(unitId, BattleUnitType.Enemy, name, maxHp, speed, attack, defense);
            }
            else
            {
                unit.UnitId = unitId;
                unit.UnitType = BattleUnitType.Enemy;
                unit.Name = name;
                unit.MaxHp = maxHp;
                unit.CurrentHp = maxHp;
                unit.BaseSpeed = speed;
                unit.BaseAttack = attack;
                unit.BaseDefense = defense;
                unit.IsAlive = true;
                unit.Shield = 0;
                unit.ResetBattleEffects();
            }

            unit.Level = level;
            unit.BaseResistance = resistance;

            EventKit.Type.Send(new UnitEvents.UnitSpawnedEvent
            {
                Unit = unit
            });

            return unit;
        }

    /// <summary>
    /// 执行 CreateCardDealerUnit 相关的战斗处理。
    /// </summary>
        public BattleUnitData CreateCardDealerUnit()
    {
        var unit = mUnitPool.Get();
        if (unit == null)
        {
            unit = new BattleUnitData("CardDealer", BattleUnitType.CardDealer, "发牌器", 9999, 0, 0, 0);
        }
        else
            {
                unit.UnitId = "CardDealer";
                unit.UnitType = BattleUnitType.CardDealer;
                unit.Name = "发牌器";
                unit.MaxHp = 9999;
                unit.CurrentHp = 9999;
                unit.BaseSpeed = 0;
                unit.BaseAttack = 0;
                unit.BaseDefense = 0;
                unit.IsAlive = true;
                unit.Shield = 0;
                unit.ResetBattleEffects();
            }

        EventKit.Type.Send(new UnitEvents.UnitSpawnedEvent
        {
            Unit = unit
        });

        return unit;
    }

    /// <summary>
    /// 执行 DestroyUnit 相关的战斗处理。
    /// </summary>
        public void DestroyUnit(BattleUnitData unit)
        {
            mUnitPool.Return(unit);
        }

    /// <summary>
    /// 执行 ClearPool 相关的战斗处理。
    /// </summary>
        public void ClearPool()
        {
            mUnitPool.Clear();
            mIsInitialized = true;
        }
    }
}

#endregion

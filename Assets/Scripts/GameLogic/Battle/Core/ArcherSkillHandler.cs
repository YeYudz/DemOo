#region 弓箭手技能处理

using System.Collections.Generic;
using UnityEngine;
using YokiFrame;
using EnergyManagement;
using TurnManagement;
using CardManagement;

namespace Game.GameLogic.Battle.Core
{
    /// <summary>
    /// 弓箭手（Archer）专属技能处理器。
    /// 特色：星魂机制围绕技能1能量恢复、目标速度增益、普攻必暴与全队无视防御展开。
    /// 星魂1：使用技能1时50%概率恢复2点能量（1回合冷却）；星魂2：使用技能1时目标速度+30%（1回合）；
    /// 星魂3：普攻必定暴击；星魂4：我方全体造成伤害时无视敌方12%防御力；
    /// 星魂5：技能1伤害提高效果持续时间+1回合。
    /// 由 CharacterHandlerRegistry 自动发现并注册。
    /// </summary>
/// <summary>
/// 战斗模块中的 ArcherSkillHandler，负责对应的战斗流程处理。
/// </summary>
    public class ArcherSkillHandler : ICharacterSkillHandler
    {
        public string CharacterId => "Archer";

        private BattleUnitData GetSelf()
        {
            return BattleSystem.Instance?.GetPlayerUnit("Archer");
        }

        // ============================================================
        // 伤害相关
        // ============================================================
    /// <summary>
    /// 执行 OnBeforeDamage 相关的战斗处理。
    /// </summary>
        public float OnBeforeDamage(string skillType, int starLevel, out float extraDefensePenetration)
        {
            extraDefensePenetration = 0f;

            // 星魂4：Archer在场时，我方全体造成伤害时无视敌方Value%防御力
            if (starLevel >= 4)
            {
                var self = GetSelf();
                extraDefensePenetration = (self != null ? self.GetStarEffectValue(4) : 12f) * 0.01f;
                LogKit.Log($"[Archer] 星魂4触发：我方全体无视敌方{(self != null ? self.GetStarEffectValue(4) : 12)}%防御力");
            }

            return 1.0f;
        }

    /// <summary>
    /// 执行 OnAfterDamage 相关的战斗处理。
    /// </summary>
        public void OnAfterDamage(string skillType, int starLevel, object attacker, object target, int finalDamage)
        {
        }

        // ============================================================
        // 治疗相关
        // ============================================================
    /// <summary>
    /// 执行 OnBeforeHeal 相关的战斗处理。
    /// </summary>
        public float OnBeforeHeal(string skillType, int starLevel)
        {
            return 0f;
        }

        // ============================================================
        // 护盾相关
        // ============================================================
    /// <summary>
    /// 执行 OnShieldAbsorb 相关的战斗处理。
    /// </summary>
        public float OnShieldAbsorb(int starLevel)
        {
            return 0f;
        }

        // ============================================================
        // 星魂被动信息
        // ============================================================
    /// <summary>
    /// 执行 GetUnlockedStarEffects 相关的战斗处理。
    /// </summary>
        public List<string> GetUnlockedStarEffects(int starLevel)
        {
            return CharacterConfigService.GetUnlockedStarEffectDescriptions(CharacterId, starLevel);
        }

        // ============================================================
        // 生命周期
        // ============================================================
    /// <summary>
    /// 执行 OnBattleStart 相关的战斗处理。
    /// </summary>
        public void OnBattleStart(int starLevel)
        {
            LogKit.Log($"Archer战斗开始，星魂{starLevel}");
        }

    /// <summary>
    /// 执行 OnTurnStart 相关的战斗处理。
    /// </summary>
        public void OnTurnStart(int starLevel)
        {
        }

        // ============================================================
        // 技能执行
        // ============================================================
    /// <summary>
    /// 执行 OnSkillExecute 相关的战斗处理。
    /// </summary>
        public void OnSkillExecute(string skillType, int starLevel, object attacker, object target)
        {
            // 星魂1：使用技能1时，Value%概率恢复2点能量（1回合触发冷却）
            if (starLevel >= 1 && skillType == "Skill1")
            {
                var self = GetSelf();
                if (self != null && !self.HasStatusEffect(StatusEffectType.EnergyGainCooldown))
                {
                    float prob = self.GetStarEffectValue(1) * 0.01f;
                    if (Random.value < prob)
                    {
                        EnergyManager.Instance.GainEnergy(2);
                        LogKit.Log("[Archer] 星魂1触发：使用技能1恢复2点能量");
                    }
                    self.AddStatusEffect(StatusEffectType.EnergyGainCooldown, 0, 1, true);
                }
            }

            // 星魂2：使用技能1时，被指定的我方目标速度提升Value%，持续一回合
            if (starLevel >= 2 && skillType == "Skill1")
            {
                var self = GetSelf();
                var targetUnit = target as BattleUnitData;
                if (targetUnit != null && self != null)
                {
                    int speedBonus = Mathf.RoundToInt(self.GetStarEffectValue(2));
                    targetUnit.AddStatusEffect(StatusEffectType.SpeedUp, speedBonus, 1, false);
                    // 同步更新TurnOrderManager的BaseAV，否则行动序不会反映速度变化
                    TurnOrderManager.Instance.SpeedUp(targetUnit.UnitId, targetUnit.CurrentSpeed);
                    LogKit.Log($"[Archer] 星魂2触发：技能1目标{targetUnit.UnitId}速度+{speedBonus}%（1回合），当前速度:{targetUnit.CurrentSpeed}");
                }
            }
        }

    /// <summary>
    /// 执行 OnEnemyKilled 相关的战斗处理。
    /// </summary>
        public void OnEnemyKilled(string skillType, int starLevel, object attacker, object killedEnemy)
        {
        }

        // ============================================================
        // 致命伤害
        // ============================================================
    /// <summary>
    /// 执行 OnLethalDamage 相关的战斗处理。
    /// </summary>
        public bool OnLethalDamage(int starLevel, out int healAmount)
        {
            healAmount = 0;
            return false;
        }

        // ============================================================
        // 技能消耗与暴击
        // ============================================================
    /// <summary>
    /// 执行 GetEffectiveSkillCost 相关的战斗处理。
    /// </summary>
        public int GetEffectiveSkillCost(string skillType, int baseCost, int starLevel)
        {
            return baseCost;
        }

    /// <summary>
    /// 执行 GetExtraCritRate 相关的战斗处理。
    /// </summary>
        public float GetExtraCritRate(string skillType, int starLevel, object target)
        {
            // 星魂3：Archer自身普通攻击造成伤害时必定暴击（Value=100，即100%暴击率）
            if (starLevel >= 3 && skillType == "NormalAttack")
            {
                var self = GetSelf();
                return (self != null ? self.GetStarEffectValue(3) : 100f) * 0.01f;
            }
            return 0f;
        }

        // ============================================================
        // 受击
        // ============================================================
    /// <summary>
    /// 执行 OnBeingAttacked 相关的战斗处理。
    /// </summary>
        public void OnBeingAttacked(int starLevel, object attacker, object target)
        {
        }
    }
}

#endregion

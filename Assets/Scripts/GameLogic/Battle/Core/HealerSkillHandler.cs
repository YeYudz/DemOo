#region 治疗师技能处理

using System.Collections.Generic;
using UnityEngine;
using YokiFrame;
using EnergyManagement;
using TurnManagement;

namespace Game.GameLogic.Battle.Core
{
    /// <summary>
    /// 治疗师（Healer）专属技能处理器。
    /// 特色：治疗加成成长、生存能力提升。
    /// 星魂1：治疗量+10%（由 OnBeforeHeal 直接返回）。
    /// 星魂2：战斗开始时生命上限+15%。
    /// 星魂3：受击恢复1点能量（1回合冷却）。
    /// 星魂4：终结技对生命值≤50%的我方目标治疗量额外+10%。
    /// 星魂5：施放终结技后行动提前100%。
    /// 由 CharacterHandlerRegistry 自动发现并注册。
    /// </summary>
/// <summary>
/// 战斗模块中的 HealerSkillHandler，负责对应的战斗流程处理。
/// </summary>
    public class HealerSkillHandler : ICharacterSkillHandler
    {
        public string CharacterId => "Healer";

        // ============================================================
        // 伤害相关
        // ============================================================
    /// <summary>
    /// 执行 OnBeforeDamage 相关的战斗处理。
    /// </summary>
        public float OnBeforeDamage(string skillType, int starLevel, out float extraDefensePenetration)
        {
            extraDefensePenetration = 0f;
            return 1.0f;
        }

    /// <summary>
    /// 执行 OnAfterDamage 相关的战斗处理。
    /// </summary>
        public void OnAfterDamage(string skillType, int starLevel, object attacker, object target, int finalDamage)
        {
        }

        // ============================================================
        // 治疗相关（Handler 直接控制，不经过通用 StarHealBonus）
        // ============================================================
    /// <summary>
    /// 执行 OnBeforeHeal 相关的战斗处理。
    /// </summary>
        public float OnBeforeHeal(string skillType, int starLevel)
        {
            // 星魂1：治疗量+Value%
            if (starLevel >= 1)
            {
                var self = BattleSystem.Instance?.GetPlayerUnit("Healer");
                return (self != null ? self.GetStarEffectValue(1) : 10f) * 0.01f;
            }
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
        // 星魂被动信息（与 JSON 配置一致）
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
            // 星魂2：生命上限+Value%
            if (starLevel >= 2)
            {
                var self = BattleSystem.Instance.GetPlayerUnit("Healer");
                if (self != null)
                {
                    float hpBonus = self.GetStarEffectValue(2) * 0.01f;
                    self.MaxHp = Mathf.RoundToInt(self.MaxHp * (1f + hpBonus));
                    self.CurrentHp = self.MaxHp;
                    LogKit.Log($"[Healer] 星魂2触发：生命上限提升{self.GetStarEffectValue(2)}%，MaxHp={self.MaxHp}");
                }
            }
            LogKit.Log($"Healer战斗开始，星魂{starLevel}");
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
            if (skillType != "Ultimate") return;

            // 星魂4：对生命值≤50%的我方目标额外治疗+Value%（此处仅记录日志，实际加成由外部应用）
            if (starLevel >= 4)
            {
                var self = BattleSystem.Instance?.GetPlayerUnit("Healer");
                float bonus = self != null ? self.GetStarEffectValue(4) : 10f;
                var allies = BattleSystem.Instance.GetPlayerUnits();
                if (allies != null)
                {
                    foreach (var ally in allies)
                    {
                        if (ally != null && ally.CurrentHpPercent <= 0.5f)
                        {
                            LogKit.Log($"星魂4触发：对{ally.UnitId}额外治疗+{bonus}%");
                        }
                    }
                }
            }

            // 星魂5：施放终结技后行动提前Value%
            if (starLevel >= 5)
            {
                var self = BattleSystem.Instance?.GetPlayerUnit("Healer");
                float advance = self != null ? self.GetStarEffectValue(5) * 0.01f : 1.0f;
                TurnOrderManager.Instance.AdvanceTurn("Healer", advance);
                LogKit.Log($"星魂5触发：行动提前{self?.GetStarEffectValue(5) ?? 100}%");
            }
        }

    /// <summary>
    /// 执行 OnEnemyKilled 相关的战斗处理。
    /// </summary>
        public void OnEnemyKilled(string skillType, int starLevel, object attacker, object killedEnemy)
        {
        }

    /// <summary>
    /// 执行 OnLethalDamage 相关的战斗处理。
    /// </summary>
        public bool OnLethalDamage(int starLevel, out int healAmount)
        {
            healAmount = 0;
            return false;
        }

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
            return 0f;
        }

        // ============================================================
        // 受击相关
        // ============================================================
    /// <summary>
    /// 执行 OnBeingAttacked 相关的战斗处理。
    /// </summary>
        public void OnBeingAttacked(int starLevel, object attacker, object target)
        {
            // 星魂3：自身受到攻击后恢复1点能量（1回合冷却）
            if (starLevel < 3) return;

            var self = target as BattleUnitData;
            if (self == null || self.UnitId != "Healer") return;
            if (self.HasStatusEffect(StatusEffectType.EnergyGainCooldown)) return;

            EnergyManager.Instance.GainEnergy(1);
            self.AddStatusEffect(StatusEffectType.EnergyGainCooldown, 0, 1, true);
            LogKit.Log("星魂3触发：受击恢复1点能量");
        }
    }
}

#endregion

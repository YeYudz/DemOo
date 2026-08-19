#region 战士技能处理

using System.Collections.Generic;
using UnityEngine;
using YokiFrame;
using EnergyManagement;
using TurnManagement;

namespace Game.GameLogic.Battle.Core
{
    /// <summary>
    /// 战士（Warrior）专属技能处理器。
    /// 特色：血量越低伤害越高、生命献祭机制、致命伤害免疫。
    /// </summary>
    public class WarriorSkillHandler : ICharacterSkillHandler
    {
        public string CharacterId => "Warrior";

        private BattleUnitData GetSelf()
        {
            return BattleSystem.Instance?.GetPlayerUnit("Warrior");
        }

    /// <summary>
    /// 执行 OnBeforeDamage 相关的战斗处理。
    /// </summary>
        public float OnBeforeDamage(string skillType, int starLevel, out float extraDefensePenetration)
        {
            extraDefensePenetration = 0f;
            float multiplier = 1.0f;
            var self = GetSelf();
            if (self == null) return multiplier;

            // 星魂1：当前生命值百分比≤50%时，造成的伤害提高Value%
            if (starLevel >= 1 && self.CurrentHpPercent <= 0.5f)
            {
                multiplier += self.GetStarEffectValue(1) * 0.01f;
                LogKit.Log($"[Warrior] 星魂1触发：生命值≤50%，伤害+{self.GetStarEffectValue(1)}%");
            }

            // 星魂5：当前生命值百分比≤50%时，终结技造成的伤害提高Value%
            if (starLevel >= 5 && self.CurrentHpPercent <= 0.5f && skillType == "Ultimate")
            {
                multiplier += self.GetStarEffectValue(5) * 0.01f;
                LogKit.Log($"[Warrior] 星魂5触发：生命值≤50%，终结技伤害+{self.GetStarEffectValue(5)}%");
            }

            return multiplier;
        }

    /// <summary>
    /// 执行 OnAfterDamage 相关的战斗处理。
    /// </summary>
        public void OnAfterDamage(string skillType, int starLevel, object attacker, object target, int finalDamage)
        {
        }

    /// <summary>
    /// 执行 OnBeforeHeal 相关的战斗处理。
    /// </summary>
        public float OnBeforeHeal(string skillType, int starLevel)
        {
            return 0f;
        }

    /// <summary>
    /// 执行 OnShieldAbsorb 相关的战斗处理。
    /// </summary>
        public float OnShieldAbsorb(int starLevel)
        {
            return 0f;
        }

    /// <summary>
    /// 执行 GetUnlockedStarEffects 相关的战斗处理。
    /// </summary>
        public List<string> GetUnlockedStarEffects(int starLevel)
        {
            return CharacterConfigService.GetUnlockedStarEffectDescriptions(CharacterId, starLevel);
        }

    /// <summary>
    /// 执行 OnBattleStart 相关的战斗处理。
    /// </summary>
        public void OnBattleStart(int starLevel)
        {
            var self = GetSelf();
            if (self == null) return;

            // 星魂4：速度+Value%（基础速度*(1+Value%)）
            if (starLevel >= 4)
            {
                float speedBonus = self.GetStarEffectValue(4) * 0.01f;
                self.BaseSpeed = Mathf.RoundToInt(self.BaseSpeed * (1f + speedBonus));
                LogKit.Log($"[Warrior] 星魂4触发：速度提升至 {self.BaseSpeed}");
            }

            LogKit.Log($"[Warrior] 战斗开始，星魂{starLevel}");
        }

    /// <summary>
    /// 执行 OnTurnStart 相关的战斗处理。
    /// </summary>
        public void OnTurnStart(int starLevel)
        {
        }

    /// <summary>
    /// 执行 OnSkillExecute 相关的战斗处理。
    /// </summary>
        public void OnSkillExecute(string skillType, int starLevel, object attacker, object target)
        {
            var self = attacker as BattleUnitData;
            if (self == null) return;

            // 星魂2：使用任意技能卡包括终结技时，提升自身Value暴击率，持续三回合
            if (starLevel >= 2)
            {
                int critBonus = Mathf.RoundToInt(self.GetStarEffectValue(2));
                self.AddStatusEffect(StatusEffectType.CritRateUp, critBonus, 3, true);
                LogKit.Log($"[Warrior] 星魂2触发：暴击率+{critBonus}（3回合）");
            }

            // 终结技：HP转化机制
            if (skillType == "Ultimate")
            {
                float targetHpPercent = 0.5f;
                int targetHp = Mathf.RoundToInt(self.MaxHp * targetHpPercent);
                float currentPercent = self.CurrentHpPercent;
                float change = currentPercent - targetHpPercent;

                if (change > 0)
                {
                    // 当前HP高于50%，降低到50%，攻击力提升变化量
                    self.CurrentHp = targetHp;
                    int atkBonus = Mathf.RoundToInt(change * 100);
                    self.AddStatusEffect(StatusEffectType.AttackUp, atkBonus, 1, true);
                    LogKit.Log($"[Warrior] 终结技HP转化：{currentPercent:P0}→{targetHpPercent:P0}，攻击力+{atkBonus}%");
                }
                else if (change < 0)
                {
                    // 当前HP低于50%，提升到50%，攻击力提升变化量
                    self.CurrentHp = targetHp;
                    int atkBonus = Mathf.RoundToInt(-change * 100);
                    self.AddStatusEffect(StatusEffectType.AttackUp, atkBonus, 1, true);
                    LogKit.Log($"[Warrior] 终结技HP转化：{currentPercent:P0}→{targetHpPercent:P0}，攻击力+{atkBonus}%");
                }
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
            // 星魂3：受到致命攻击时不会死亡，立即回复至生命上限的Value%（仅触发1次）
            if (starLevel >= 3)
            {
                var self = GetSelf();
                if (self != null && !self.DeathSaveUsed)
                {
                    self.DeathSaveUsed = true;
                    float healPercent = self.GetStarEffectValue(3) * 0.01f;
                    healAmount = Mathf.RoundToInt(self.MaxHp * healPercent) - self.CurrentHp;
                    if (healAmount < 0) healAmount = Mathf.RoundToInt(self.MaxHp * healPercent);
                    LogKit.Log($"[Warrior] 星魂3触发：致命伤害免疫，回复至{self.GetStarEffectValue(3)}%生命");
                    return true;
                }
            }
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

    /// <summary>
    /// 执行 OnBeingAttacked 相关的战斗处理。
    /// </summary>
        public void OnBeingAttacked(int starLevel, object attacker, object target)
        {
        }
    }
}

#endregion

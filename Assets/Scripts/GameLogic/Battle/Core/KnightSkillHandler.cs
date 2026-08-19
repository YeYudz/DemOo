#region 骑士技能处理

using System.Collections.Generic;
using UnityEngine;
using YokiFrame;

namespace Game.GameLogic.Battle.Core
{
    /// <summary>
    /// 骑士（Knight）专属技能处理器。
    /// 特色：圣盾庇护（全体护盾）、普攻破甲、终结技惶恐、星魂5依护盾队友提升伤害。
    /// 由 CharacterHandlerRegistry 自动发现并注册。
    /// </summary>
    public class KnightSkillHandler : ICharacterSkillHandler
    {
        public string CharacterId => "Knight";

        // ============================================================
        // 伤害相关
        // ============================================================
    /// <summary>
    /// 执行 OnBeforeDamage 相关的战斗处理。
    /// </summary>
        public float OnBeforeDamage(string skillType, int starLevel, out float extraDefensePenetration)
        {
            extraDefensePenetration = 0f;

            // 星魂5：每有1个队友持有护盾，伤害提高Value%，最高不超过150%
            if (starLevel >= 5)
            {
                int shieldedAllyCount = 0;
                var playerUnits = BattleSystem.Instance?.GetPlayerUnits();
                if (playerUnits != null)
                {
                    foreach (var ally in playerUnits)
                    {
                        if (ally == null) continue;
                        if (ally.UnitId == "Knight") continue;
                        if (ally.Shield > 0) shieldedAllyCount++;
                    }
                }

                var self = BattleSystem.Instance?.GetPlayerUnit("Knight");
                float perAllyBonus = (self != null ? self.GetStarEffectValue(5) : 50f) * 0.01f;
                float damageBonus = Mathf.Min(shieldedAllyCount * perAllyBonus, 1.5f);
                LogKit.Log($"[Knight] 星魂5触发：护盾队友数={shieldedAllyCount}，伤害加成+{damageBonus * 100f:F0}%");
                return 1.0f + damageBonus;
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
        // 护盾相关（护盾逻辑在 OnSkillExecute 中处理）
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
            // 星魂1：战斗开始时为我方全体提供护盾（量等同于技能1的100%）
            if (starLevel >= 1)
            {
                int shieldAmount = CalculateShieldAmount(starLevel);
                var playerUnits = BattleSystem.Instance?.GetPlayerUnits();
                if (playerUnits != null)
                {
                    foreach (var ally in playerUnits)
                    {
                        if (ally == null || !ally.IsAlive) continue;
                        ally.Shield += shieldAmount;
                    }
                }
                LogKit.Log($"[Knight] 星魂1触发：为全体提供护盾{shieldAmount}");
            }

            LogKit.Log($"[Knight] Knight战斗开始，星魂{starLevel}");
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
            switch (skillType)
            {
                case "Skill1":
                {
                    int shield = CalculateShieldAmount(starLevel);
                    var playerUnits = BattleSystem.Instance?.GetPlayerUnits();
                    if (playerUnits != null)
                    {
                        foreach (var ally in playerUnits)
                        {
                            if (ally == null || !ally.IsAlive) continue;
                            ally.Shield += shield;
                        }
                    }
                    LogKit.Log($"[Knight] 圣盾庇护：为全体提供护盾{shield}");
                    break;
                }

                case "NormalAttack":
                {
                    // 星魂3：普攻使目标防御力降低Value%，持续3回合
                    if (starLevel >= 3)
                    {
                        var self = BattleSystem.Instance?.GetPlayerUnit("Knight");
                        int defDown = Mathf.RoundToInt(self != null ? self.GetStarEffectValue(3) : 12f);
                        var targetUnit = target as BattleUnitData;
                        if (targetUnit != null)
                        {
                            targetUnit.AddStatusEffect(StatusEffectType.DefenseDown, defDown, 3, false);
                            LogKit.Log($"[Knight] 星魂3触发：目标防御力-{defDown}%（3回合）");
                        }
                    }
                    break;
                }

                case "Skill2":
                {
                    // 圣光惩戒：100%攻击力伤害+65%概率降低目标攻击力20%（1回合）
                    // 伤害由 BattleSystem 根据 EffectType=Damage 自动结算
                    // AttackDown次要效果及65%概率判定由 BattleSystem.ExecuteSecondaryEffect 处理
                    // 当前无 Knight 专属星魂与 Skill2 联动，留空以备扩展
                    break;
                }

                case "Ultimate":
                {
                    // 终结技：目标陷入惶恐状态，持续3回合
                    var targetUnit = target as BattleUnitData;
                    if (targetUnit != null)
                    {
                        targetUnit.AddStatusEffect(StatusEffectType.Fear, 0, 3, false);

                        // 星魂4：陷入惶恐状态的敌方目标，速度降低Value%
                        if (starLevel >= 4)
                        {
                            var self = BattleSystem.Instance?.GetPlayerUnit("Knight");
                            int speedDown = Mathf.RoundToInt(self != null ? self.GetStarEffectValue(4) : 10f);
                            targetUnit.AddStatusEffect(StatusEffectType.SpeedDown, speedDown, 3, false);
                        }

                        LogKit.Log("[Knight] 终结技：目标陷入惶恐状态");
                    }
                    break;
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

        // ============================================================
        // 辅助方法
        // ============================================================
        private int CalculateShieldAmount(int starLevel)
        {
            var self = BattleSystem.Instance?.GetPlayerUnit("Knight");
            if (self == null) return 120;
            // Use talent values: Value=defense percent, Value2=flat shield
            int defPercent = self.GetTalentValue("Skill1");
            int flatShield = self.GetTalentValue2("Skill1");
            if (defPercent <= 0) defPercent = 12;
            if (flatShield <= 0) flatShield = 120;
            int shield = Mathf.RoundToInt(self.CurrentDefense * defPercent * 0.01f) + flatShield;
            // Star2: shield +100%
            if (starLevel >= 2)
            {
                shield *= 2;
            }
            return shield;
        }
    }
}

#endregion

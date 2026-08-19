#region 法师技能处理

using System.Collections.Generic;
using UnityEngine;
using YokiFrame;
using EnergyManagement;
using TurnManagement;
using CardManagement;

namespace Game.GameLogic.Battle.Core
{
    /// <summary>
    /// 法师（Mage）专属技能处理器。
    /// 特色：星魂机制围绕技能卡能量消耗与能量/生命恢复展开。
    /// 星魂1：技能2能量消耗变为0；星魂2：使用任意技能卡包括终结技时恢复自身5%生命上限生命值；
    /// 星魂3：使用技能2有70%概率恢复1点能量；星魂4：技能1攻击力提升效果变为30%；
    /// 星魂5：使用终结技时恢复与弃置卡牌数量相等的能量。
    /// 由 CharacterHandlerRegistry 自动发现并注册。
    /// </summary>
/// <summary>
/// 战斗模块中的 MageSkillHandler，负责对应的战斗流程处理。
/// </summary>
    public class MageSkillHandler : ICharacterSkillHandler
    {
        public string CharacterId => "Mage";

        private BattleUnitData GetSelf()
        {
            return BattleSystem.Instance?.GetPlayerUnit("Mage");
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
            LogKit.Log($"Mage战斗开始，星魂{starLevel}");
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
            var self = GetSelf();

            // 星魂2：使用任意技能卡包括终结技时，恢复自身Value%生命上限的生命值
            if (starLevel >= 2 && self != null)
            {
                float healPercent = self.GetStarEffectValue(2) * 0.01f;
                int healAmount = Mathf.RoundToInt(self.MaxHp * healPercent);
                self.CurrentHp = Mathf.Min(self.MaxHp, self.CurrentHp + healAmount);
                LogKit.Log($"[Mage] 星魂2触发：恢复自身{healAmount}生命值（{self.GetStarEffectValue(2)}%最大生命）");
            }

            // 星魂3：使用技能2时有Value%概率恢复1点能量
            if (starLevel >= 3 && skillType == "Skill2")
            {
                float prob = self.GetStarEffectValue(3) * 0.01f;
                if (Random.value < prob)
                {
                    EnergyManager.Instance.GainEnergy(1);
                    LogKit.Log("[Mage] 星魂3触发：使用技能2恢复1点能量");
                }
            }

            // 星魂5：使用终结技时，恢复与弃置的卡牌数量相等数量的能量
            if (starLevel >= 5 && skillType == "Ultimate")
            {
                int cardCount = CardHand.Instance.Cards.Count;
                EnergyManager.Instance.GainEnergy(cardCount);
                LogKit.Log($"[Mage] 星魂5触发：使用终结技恢复{cardCount}点能量（弃置卡牌数）");
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
            // 星魂1：使技能2消耗的能量变为0
            if (starLevel >= 1 && skillType == "Skill2")
            {
                return 0;
            }
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

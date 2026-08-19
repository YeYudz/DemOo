using System.Collections.Generic;
using UnityEngine;
using YokiFrame;
using EnergyManagement;
using TurnManagement;

namespace Game.GameLogic.Battle.Core
{
    /// <summary>
    /// 刺客（Assassin）专属技能处理器。
    /// 特色：极高暴击与速度、高攻低防、爆发输出。
    /// 星魂1：对生命值≤80%的敌方目标暴击率+15%（需目标信息，由 BattleSystem 直接处理）。
    /// 星魂2：释放普攻后行动提前20%。
    /// 星魂3：消灭敌方目标时恢复1点能量。
    /// 星魂4：技能1的加速效果可叠加（最多2层）。
    /// 星魂5：神鬼无赦状态下无视目标25%防御力。
    /// 由 CharacterHandlerRegistry 自动发现并注册。
    /// </summary>
    public class AssassinSkillHandler : ICharacterSkillHandler
    {
        public string CharacterId => "Assassin";

        // ============================================================
        // 伤害相关
        // ============================================================
        public float OnBeforeDamage(string skillType, int starLevel, out float extraDefensePenetration)
        {
            extraDefensePenetration = 0f;

            var self = BattleSystem.Instance.GetPlayerUnit("Assassin");
            if (self == null) return 1.0f;

            bool inGodSlayer = self.GodSlayerState > 0;

            // 星魂5：神鬼无赦状态下无视目标Value%防御力
            if (starLevel >= 5 && inGodSlayer)
            {
                extraDefensePenetration = self.GetStarEffectValue(5) * 0.01f;
                LogKit.Log($"星魂5触发：神鬼无赦状态下无视{self.GetStarEffectValue(5)}%防御力");
            }

            // 神鬼无赦状态：伤害+50%
            return 1.0f + (inGodSlayer ? 0.5f : 0f);
        }

        public void OnAfterDamage(string skillType, int starLevel, object attacker, object target, int finalDamage)
        {
            // 星魂2：释放普攻后行动提前Value%
            if (starLevel >= 2 && skillType == "NormalAttack")
            {
                var self = BattleSystem.Instance.GetPlayerUnit("Assassin");
                float advance = self != null ? self.GetStarEffectValue(2) * 0.01f : 0.2f;
                TurnOrderManager.Instance.AdvanceTurn("Assassin", advance);
                LogKit.Log($"星魂2触发：行动提前{self?.GetStarEffectValue(2) ?? 20}%");
            }
        }

        // ============================================================
        // 治疗相关
        // ============================================================
        public float OnBeforeHeal(string skillType, int starLevel)
        {
            return 0f;
        }

        // ============================================================
        // 护盾相关
        // ============================================================
        public float OnShieldAbsorb(int starLevel)
        {
            return 0f;
        }

        // ============================================================
        // 星魂被动信息（与 JSON 配置一致）
        // ============================================================
        public List<string> GetUnlockedStarEffects(int starLevel)
        {
            return CharacterConfigService.GetUnlockedStarEffectDescriptions(CharacterId, starLevel);
        }

        // ============================================================
        // 生命周期
        // ============================================================
        public void OnBattleStart(int starLevel)
        {
            LogKit.Log($"Assassin战斗开始，星魂{starLevel}");
        }

        public void OnTurnStart(int starLevel)
        {
            var self = BattleSystem.Instance.GetPlayerUnit("Assassin");
            if (self == null) return;

            if (self.AssassinationState > 0)
            {
                self.AssassinationState--;
                LogKit.Log($"[Assassin] 刺杀状态剩余回合：{self.AssassinationState}");
            }

            if (self.GodSlayerState > 0)
            {
                self.GodSlayerState--;
                LogKit.Log($"[Assassin] 神鬼无赦状态剩余回合：{self.GodSlayerState}");
            }
        }

        // ============================================================
        // 技能执行
        // ============================================================
        public void OnSkillExecute(string skillType, int starLevel, object attacker, object target)
        {
            var self = BattleSystem.Instance.GetPlayerUnit("Assassin");
            if (self == null) return;

            if (skillType == "Skill2")
            {
                self.AssassinationState = 2;
                LogKit.Log("进入刺杀状态（2回合）");
            }
            else if (skillType == "Ultimate")
            {
                self.GodSlayerState = 1;
                LogKit.Log("进入神鬼无赦状态（1回合）");
            }
            else if (skillType == "Skill1")
            {
                // 技能1：速度+25%，持续2回合
                if (starLevel >= 4)
                {
                    // 星魂4：加速效果可叠加，最多叠加2层（50%）
                    int currentSpeedUp = self.GetStatusEffectValue(StatusEffectType.SpeedUp);
                    int newSpeedUp = Mathf.Min(currentSpeedUp + 25, 50);
                    self.AddStatusEffect(StatusEffectType.SpeedUp, newSpeedUp, 2, true);
                    // 同步更新TurnOrderManager的BaseAV，否则行动序不会反映速度变化
                    TurnOrderManager.Instance.SpeedUp(self.UnitId, self.CurrentSpeed);
                    LogKit.Log($"[Assassin] 星魂4叠加加速：{currentSpeedUp}% -> {newSpeedUp}%（上限50%），当前速度:{self.CurrentSpeed}");
                }
                else
                {
                    self.AddStatusEffect(StatusEffectType.SpeedUp, 25, 2, true);
                    // 同步更新TurnOrderManager的BaseAV
                    TurnOrderManager.Instance.SpeedUp(self.UnitId, self.CurrentSpeed);
                    LogKit.Log($"[Assassin] 速度+25%（2回合），当前速度:{self.CurrentSpeed}");
                }
            }
        }

        public void OnEnemyKilled(string skillType, int starLevel, object attacker, object killedEnemy)
        {
            var self = BattleSystem.Instance.GetPlayerUnit("Assassin");
            if (self == null)
            {
                LogKit.Warning("[Assassin] OnEnemyKilled: 找不到Assassin单位");
                return;
            }

            LogKit.Log($"[Assassin] OnEnemyKilled: skillType={skillType}, starLevel={starLevel}, AssassinationState={self.AssassinationState}, GodSlayerState={self.GodSlayerState}");

            // 星魂3：消灭敌方目标时恢复1点能量
            if (starLevel >= 3)
            {
                EnergyManager.Instance.GainEnergy(1);
                LogKit.Log("[Assassin] 星魂3触发：击杀恢复1点能量");
            }

            // 刺杀状态触发：行动提前100%，进入神鬼无赦
            // GodSlayerState设为2：本回合剩余时间+下回合完整回合（OnTurnStart会先减1）
            // 这样下回合造成伤害时GodSlayerState仍>0，50%伤害加成才生效
            if (self.AssassinationState > 0)
            {
                LogKit.Log($"[Assassin] 刺杀状态触发前：AssassinationState={self.AssassinationState}");
                TurnOrderManager.Instance.AdvanceTurn("Assassin", 1.0f);
                self.GodSlayerState = 2;
                self.AssassinationState = 0;
                LogKit.Log("[Assassin] 刺杀状态触发：行动提前100%（延迟到CompleteAction后应用），进入神鬼无赦（2回合）");
            }
            else
            {
                LogKit.Log("[Assassin] 刺杀状态未触发：AssassinationState=0");
            }
        }

        public bool OnLethalDamage(int starLevel, out int healAmount)
        {
            healAmount = 0;
            return false;
        }

        public int GetEffectiveSkillCost(string skillType, int baseCost, int starLevel)
        {
            return baseCost;
        }

        public float GetExtraCritRate(string skillType, int starLevel, object target)
        {
            // 星魂1：对当前生命值百分比≤80%的敌方目标造成伤害时，暴击率提高Value%
            if (starLevel >= 1 && target is BattleUnitData targetUnit && targetUnit.UnitType == BattleUnitType.Enemy)
            {
                if (targetUnit.CurrentHpPercent <= 0.8f)
                {
                    var self = BattleSystem.Instance.GetPlayerUnit("Assassin");
                    float critBonus = self != null ? self.GetStarEffectValue(1) * 0.01f : 0.15f;
                    LogKit.Log($"[Assassin] 星魂1触发：对生命值≤80%的目标{targetUnit.Name}暴击率+{self?.GetStarEffectValue(1) ?? 15}%");
                    return critBonus;
                }
            }
            return 0f;
        }

        public void OnBeingAttacked(int starLevel, object attacker, object target)
        { 
        }
    }
}

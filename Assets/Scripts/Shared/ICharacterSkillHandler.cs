using System.Collections.Generic;

namespace Game.GameLogic.Battle.Core
{
    public interface ICharacterSkillHandler
    {
        string CharacterId { get; }

        float OnBeforeDamage(string skillType, int starLevel, out float extraDefensePenetration);

        void OnAfterDamage(string skillType, int starLevel, object attacker, object target, int finalDamage);

        float OnBeforeHeal(string skillType, int starLevel);

        float OnShieldAbsorb(int starLevel);

        List<string> GetUnlockedStarEffects(int starLevel);

        void OnBattleStart(int starLevel);

        void OnTurnStart(int starLevel);

        /// <summary>
        /// 技能卡牌执行前回调，用于自定义机制（HP消耗、抽牌概率、能量恢复、状态切换等）。
        /// attacker/target 为 object 类型，实际为 BattleUnitData。
        /// </summary>
        void OnSkillExecute(string skillType, int starLevel, object attacker, object target);

        /// <summary>
        /// 角色击杀敌方单位后回调（Assassin刺杀状态触发等）。
        /// </summary>
        void OnEnemyKilled(string skillType, int starLevel, object attacker, object killedEnemy);

        /// <summary>
        /// 角色即将受到致命伤害时回调，返回true阻止死亡并回复指定血量。
        /// </summary>
        bool OnLethalDamage(int starLevel, out int healAmount);

        /// <summary>
        /// 获取技能实际消耗（星魂减费等）。
        /// </summary>
        int GetEffectiveSkillCost(string skillType, int baseCost, int starLevel);

        /// <summary>
        /// 获取额外暴击率（Archer星魂3普攻必暴、Assassin星魂1对低血量目标暴击+15%等）。
        /// target 为 object 类型，实际为 BattleUnitData，可为 null。
        /// </summary>
        float GetExtraCritRate(string skillType, int starLevel, object target);

        /// <summary>
        /// 角色被攻击后回调（Healer星魂3受击回能等）。
        /// </summary>
        void OnBeingAttacked(int starLevel, object attacker, object target);
    }
}


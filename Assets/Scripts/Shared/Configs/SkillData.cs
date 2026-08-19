using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum SkillEffectType
{
    Damage,
    Heal,
    EnergyGain,
    DrawCard,
    Shield,
    AttackUp,
    AttackDown,
    DefenseUp,
    DefenseDown,
    SpeedUp,
    SpeedDown,
    DamageUp,
    CritRateUp,
    CritDamageUp,
    DefensePenetration,
    DiscardAndDraw,
    ActionAdvance
}

public enum TargetType
{
    Self,
    Ally,
    Enemy,
    AllEnemies,
    AllAllies
}

public class SkillCardData
{
    public string SkillId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Cost { get; set; }
    public SkillEffectType EffectType { get; set; }
    public TargetType TargetType { get; set; }
    public int Value { get; set; }
    public string CharacterId { get; set; }
    public int Duration { get; set; } = 3;
    public int HealPercent { get; set; } = 0;
    public float SelfHpCostPercent { get; set; } = 0f;
    public SkillEffectType SecondaryEffect { get; set; } = (SkillEffectType)(-1);
    public int SecondaryValue { get; set; } = 0;
    public int SecondaryDuration { get; set; } = 3;
    public string SkillType { get; set; } = "Normal";
    public float Probability { get; set; } = 100f;

    public SkillCardData() { }

    public SkillCardData(string skillId, string name, string description, int cost, SkillEffectType effectType, TargetType targetType, int value)
    {
        SkillId = skillId;
        Name = name;
        Description = description;
        Cost = cost;
        EffectType = effectType;
        TargetType = targetType;
        Value = value;
    }

    public SkillCardData(string skillId, string name, string description, int cost, SkillEffectType effectType, TargetType targetType, int value, string characterId)
        : this(skillId, name, description, cost, effectType, targetType, value)
    {
        CharacterId = characterId;
        SkillType = DetermineSkillType(skillId);
    }

    public static string DetermineSkillType(string skillId)
    {
        if (skillId.Contains("NormalAttack"))
            return "NormalAttack";
        else if (skillId.Contains("Ultimate"))
            return "Ultimate";
        else if (skillId.Contains("Skill1"))
            return "Skill1";
        else if (skillId.Contains("Skill2"))
            return "Skill2";
        return "NormalAttack";
    }
}

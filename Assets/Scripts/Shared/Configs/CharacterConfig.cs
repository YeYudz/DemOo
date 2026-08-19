using System.Collections.Generic;

public class PlayerCharacterConfig
{
    public string CharacterId { get; set; }
    public string Name { get; set; }
    public int Rarity { get; set; } = 4;
    public int MaxHp { get; set; }
    public int Speed { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int MaxLevel { get; set; } = 80;
    public int MaxHpAtMaxLevel { get; set; }
    public int AttackAtMaxLevel { get; set; }
    public int DefenseAtMaxLevel { get; set; }
    public int SpeedAtMaxLevel { get; set; }
    public int Level { get; set; }
    public float CritRate { get; set; } = 0.1f;
    public float CritDamage { get; set; } = 0.5f;
    public float DamageBonus { get; set; } = 0f;
    public float ResistancePenetration { get; set; } = 0f;
    public List<string> Skills { get; set; } = new List<string>();
    public float IconColorR { get; set; } = 0.5f;
    public float IconColorG { get; set; } = 0.5f;
    public float IconColorB { get; set; } = 0.5f;
    public List<StarEffectConfig> StarEffects { get; set; } = new List<StarEffectConfig>();
    public List<TalentEffectConfig> TalentEffects { get; set; } = new List<TalentEffectConfig>();
}

public class StarEffectConfig
{
    public int StarLevel { get; set; }
    public string EffectType { get; set; }
    public float Value { get; set; }
    public string Description { get; set; }
    public List<CostItem> UnlockCost { get; set; } = new List<CostItem>();
}

public class TalentEffectConfig
{
    public string SkillType { get; set; }
    public List<TalentLevelEffect> Effects { get; set; } = new List<TalentLevelEffect>();
}

public class TalentLevelEffect
{
    public int Level { get; set; }
    public int Value { get; set; }
    public int Value2 { get; set; }
    public string Description { get; set; }
    public List<CostItem> UpgradeCost { get; set; } = new List<CostItem>();
}

public class CostItem
{
    public string ItemId { get; set; }
    public int Count { get; set; }
}

public class SkillConfig
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
    public float Probability { get; set; } = 100f;
}

public enum CharacterClass
{
    Warrior,
    Mage,
    Archer,
    Healer
}
using System.Collections.Generic;

public enum RelicSlot
{
    Head,
    Hands,
    Body,
    Feet
}

public enum RelicMainStatType
{
    MaxHp,
    Attack,
    CritRate,
    CritDamage,
    MaxHpPercent,
    AttackPercent,
    DefensePercent,
    Speed
}

public static class RelicMainStatTypeExtensions
{
    public static string GetDisplayName(this RelicMainStatType statType)
    {
        switch (statType)
        {
            case RelicMainStatType.MaxHp: return "生命值";
            case RelicMainStatType.Attack: return "攻击力";
            case RelicMainStatType.CritRate: return "暴击率";
            case RelicMainStatType.CritDamage: return "暴击伤害";
            case RelicMainStatType.MaxHpPercent: return "生命值百分比";
            case RelicMainStatType.AttackPercent: return "攻击力百分比";
            case RelicMainStatType.DefensePercent: return "防御力百分比";
            case RelicMainStatType.Speed: return "速度";
            default: return statType.ToString();
        }
    }
}

[System.Serializable]
public class RelicMainStatConfig
{
    public RelicMainStatType StatType { get; set; }
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public bool IsPercent { get; set; }
}

[System.Serializable]
public class RelicSubStatConfig
{
    public RelicMainStatType StatType { get; set; }
    public int MinValue { get; set; }
    public int MaxValue { get; set; }
    public bool IsPercent { get; set; }
}

public class RelicConfig
{
    public string RelicId { get; set; }
    public string Name { get; set; }
    public RelicSlot Slot { get; set; }
    public int Rarity { get; set; }
    public RelicMainStatConfig MainStat { get; set; }
    public List<RelicMainStatConfig> PossibleMainStats { get; set; } = new List<RelicMainStatConfig>();
    public List<RelicSubStatConfig> PossibleSubStats { get; set; } = new List<RelicSubStatConfig>();
    public int SubStatCount { get; set; } = 3;
    public string IconPath { get; set; } = "";
    public string SetName { get; set; } = "";
    public string SetEffect1 { get; set; } = "";
    public string SetEffect2 { get; set; } = "";
}

[System.Serializable]
public class AccountRelicData
{
    public string RelicId;
    public int Level;
    public RelicMainStatType MainStatType;
    public float MainStatValue;
    public List<RelicSubStatData> SubStats = new List<RelicSubStatData>();
    public bool IsPercent;
}

[System.Serializable]
public class RelicSubStatData
{
    public RelicMainStatType StatType;
    public float Value;
    public bool IsPercent;
    public int EnhanceLevel;
}

public class RelicConfigWrapper
{
    public List<RelicConfig> Relics { get; set; } = new List<RelicConfig>();
}
using System.Collections.Generic;

public enum WeaponType
{
    Sword,
    Spear,
    Staff,
    Grimoire,
    Bow,
    Wand
}

[System.Serializable]
public class WeaponEffectData
{
    public string EffectId { get; set; }
    public string EffectName { get; set; }
    public string Description { get; set; }
    public float Value { get; set; }
    public int Duration { get; set; }
}

public class LightConeConfig
{
    public string WeaponId { get; set; }
    public string Name { get; set; }
    public WeaponType WeaponType { get; set; }
    public List<CharacterClass> CompatibleClasses { get; set; } = new List<CharacterClass>();
    public int Rarity { get; set; }
    public int BaseAttack { get; set; }
    public int BaseDefense { get; set; }
    public int BaseMaxHp { get; set; }
    public int MaxLevel { get; set; } = 80;
    public int BaseAttackAtMaxLevel { get; set; }
    public int BaseDefenseAtMaxLevel { get; set; }
    public int BaseMaxHpAtMaxLevel { get; set; }
    public List<WeaponEffectData> Effects { get; set; } = new List<WeaponEffectData>();
    public string IconPath { get; set; } = "";
}

public class LightConeConfigWrapper
{
    public List<LightConeConfig> Weapons { get; set; } = new List<LightConeConfig>();
}
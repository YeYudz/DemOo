#region 角色数据模型

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 战斗模块中的 CharacterData，负责对应的战斗流程处理。
/// </summary>
public class CharacterData
{
    public string CharacterId { get; set; }
    public string Name { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int Speed { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int MaxEnergy { get; set; }
    public int CurrentEnergy { get; set; }
    public List<SkillCardData> Skills { get; set; } = new List<SkillCardData>();

    public CharacterData() { }

    public CharacterData(string characterId, string name, int maxHp, int speed, int attack, int defense, int maxEnergy)
    {
        CharacterId = characterId;
        Name = name;
        MaxHp = maxHp;
        CurrentHp = maxHp;
        Speed = speed;
        Attack = attack;
        Defense = defense;
        MaxEnergy = maxEnergy;
        CurrentEnergy = 0;
        Skills = new List<SkillCardData>();
    }
}

#endregion

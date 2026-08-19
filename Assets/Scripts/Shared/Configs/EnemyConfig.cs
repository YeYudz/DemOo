using System.Collections.Generic;

public class EnemyConfig
{
    public string EnemyId { get; set; }
    public string Name { get; set; }
    public int MaxHp { get; set; }
    public int Speed { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int RewardGold { get; set; }
    public int RewardExp { get; set; }
    public int Level { get; set; } = 1;
    public float Resistance { get; set; } = 0f;
    public List<string> Skills { get; set; } = new List<string>();
    public float IconColorR { get; set; } = 0.5f;
    public float IconColorG { get; set; } = 0.5f;
    public float IconColorB { get; set; } = 0.5f;
}
using System.Collections.Generic;

public class WaveEnemyConfig
{
    public string EnemyId { get; set; }
    public float AttributeMultiplier { get; set; } = 1f;
    public float HpMultiplier { get; set; }
    public float AttackMultiplier { get; set; }
    public float DefenseMultiplier { get; set; }
    public float SpeedMultiplier { get; set; }
}

public class WaveConfig
{
    public int WaveNumber { get; set; }
    public List<string> EnemyIds { get; set; } = new List<string>();
    public List<WaveEnemyConfig> Enemies { get; set; } = new List<WaveEnemyConfig>();
    public float SpawnInterval { get; set; } = 2f;
    public int WaveDelaySeconds { get; set; } = 5;
}

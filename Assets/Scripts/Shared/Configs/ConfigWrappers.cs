using System.Collections.Generic;

public class CharacterConfigWrapper
{
    public List<PlayerCharacterConfig> Characters { get; set; } = new List<PlayerCharacterConfig>();
}

public class EnemyConfigWrapper
{
    public List<EnemyConfig> Enemies { get; set; } = new List<EnemyConfig>();
}

public class SkillConfigWrapper
{
    public List<SkillConfig> Skills { get; set; } = new List<SkillConfig>();
}

public class WaveConfigWrapper
{
    public List<WaveConfig> Waves { get; set; } = new List<WaveConfig>();
}

public class LotteryConfigWrapper
{
    public List<LotteryPoolConfig> Pools { get; set; } = new List<LotteryPoolConfig>();
}
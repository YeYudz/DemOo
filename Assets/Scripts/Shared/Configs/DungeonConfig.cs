using System.Collections.Generic;

public class DungeonConfigWrapper
{
    public List<DungeonConfig> Dungeons { get; set; } = new List<DungeonConfig>();
}

public class DungeonConfig
{
    public string DungeonId { get; set; }
    public string DungeonName { get; set; }
    public int BaseExp { get; set; }
    public int GoldReward { get; set; }
    public List<DropReward> DropRewards { get; set; } = new List<DropReward>();
    public List<WaveConfig> Waves { get; set; } = new List<WaveConfig>();
}

public class DropReward
{
    public string ItemType { get; set; }
    public string ItemId { get; set; }
    public int MinCount { get; set; }
    public int MaxCount { get; set; }
    public float DropRate { get; set; }
}
using System.Collections.Generic;

public class UpgradeMaterialConfig
{
    public string ItemId { get; set; }
    public float BaseCount { get; set; }
    public float CountPerLevel { get; set; }
}

public class UpgradeConfig
{
    public string ItemType { get; set; }
    public int MinRarity { get; set; }
    public int MaxRarity { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public float GoldBase { get; set; }
    public float GoldPerLevel { get; set; }
    public List<UpgradeMaterialConfig> Materials { get; set; } = new List<UpgradeMaterialConfig>();
}

public class UpgradeConfigWrapper
{
    public List<UpgradeConfig> Upgrades { get; set; } = new List<UpgradeConfig>();
}
using System.Collections.Generic;

public class DecomposeMaterialEntry
{
    public string MaterialId { get; set; }
    public int MaterialCount { get; set; }
}

public class DecomposeConfig
{
    public string ItemType { get; set; }
    public int MinRarity { get; set; }
    public int MaxRarity { get; set; }
    public int MinLevel { get; set; }
    public int MaxLevel { get; set; }
    public List<DecomposeMaterialEntry> Materials { get; set; } = new List<DecomposeMaterialEntry>();
}

public class DecomposeConfigWrapper
{
    public List<DecomposeConfig> Decompositions { get; set; } = new List<DecomposeConfig>();
}
using System.Collections.Generic;

public class ConsumableConfig
{
    public string ItemId { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string IconPath { get; set; } = "";
    public string ItemType { get; set; } = "Material";
}

public class ConsumableConfigWrapper
{
    public List<ConsumableConfig> Consumables { get; set; } = new List<ConsumableConfig>();
}
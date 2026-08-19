using System.Collections.Generic;

public class StoreItemConfig
{
    public string Category { get; set; }
    public string ItemType { get; set; }
    public string ItemId { get; set; }
    public int Price { get; set; }
    public string Currency { get; set; }
    public int Stock { get; set; }
    public int Quantity { get; set; } = 1;
    public string IconPath { get; set; }
}

public class StoreConfigWrapper
{
    public List<StoreItemConfig> StoreItems { get; set; } = new List<StoreItemConfig>();
}
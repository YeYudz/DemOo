using System.Collections.Generic;
using YokiFrame;

public class LotteryConfigDatabase
{
    public List<LotteryPoolConfig> Pools { get; set; } = new List<LotteryPoolConfig>();
}

public class LotteryPoolConfig
{
    public string PoolId { get; set; }
    public string PoolName { get; set; }
    public string PoolType { get; set; }
    public int DiamondCost { get; set; } = 160;
    public int TenDrawCost { get; set; } = 1600;
    public int GuaranteedFourStarEvery { get; set; } = 10;
    public int GuaranteedFiveStarEvery { get; set; } = 90;
    public bool HasUpSystem { get; set; } = true;
    public List<string> UpFiveStarIds { get; set; } = new List<string>();
    public List<string> UpFourStarIds { get; set; } = new List<string>();
    public List<string> FiveStarItemIds { get; set; } = new List<string>();
    public List<string> FourStarItemIds { get; set; } = new List<string>();
    public List<string> ThreeStarItemIds { get; set; } = new List<string>();
    public float BaseFiveStarRate { get; set; } = 0.006f;
    public float BaseFourStarRate { get; set; } = 0.051f;
}

public class LotteryDrawItem
{
    public string ItemId { get; set; }
    public string ItemName { get; set; }
    public string ItemType { get; set; }
    public int Rarity { get; set; }
}

public class LotteryPityData
{
    public string PoolId { get; set; }
    public int DrawCountSinceFiveStar { get; set; } = 0;
    public int DrawCountSinceFourStar { get; set; } = 0;
    public int NextFiveStarMustBeUp { get; set; } = 0;
    public int NextFourStarMustBeUp { get; set; } = 0;
    public int TotalFiveStarDraws { get; set; } = 0;
    public int TotalFourStarDraws { get; set; } = 0;
}

public class LotteryHistoryRecord
{
    public string RecordId { get; set; }
    public string ItemId { get; set; }
    public string ItemName { get; set; }
    public string ItemType { get; set; }
    public int Rarity { get; set; }
    public string PoolId { get; set; }
    public long Timestamp { get; set; }
    public bool IsUp { get; set; }
}

public class LotteryDrawResult
{
    public List<LotteryHistoryRecord> Results { get; set; } = new List<LotteryHistoryRecord>();
    public int DiamondCost { get; set; }
    public int DrawCount { get; set; }
}

namespace GameUI
{
    public class LotteryResultPanelData : IUIData
    {
        public LotteryDrawResult DrawResult { get; set; }
    }
}
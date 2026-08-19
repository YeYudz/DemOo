using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

#region 账号数据

/// <summary>
/// 账号基础数据，保存登录信息与副本进度。
/// </summary>
public class AccountData
{
    /// <summary>账号唯一标识。</summary>
    public string AccountId { get; set; }
    /// <summary>账号密码。</summary>
    public string Password { get; set; }
    /// <summary>副本进度。</summary>
    public int DungeonProgress { get; set; } = 0;

    /// <summary>初始化账号数据。</summary>
    public AccountData() { }

    /// <summary>使用账号标识和密码初始化账号数据。</summary>
    public AccountData(string accountId, string password)
    {
        // 设置账号基础信息与初始进度。
        AccountId = accountId;
        Password = password;
        DungeonProgress = 0;
    }
}

#endregion

#region 物品数据

/// <summary>
/// 账号物品数据，保存物品实例及其成长、装备信息。
/// </summary>
public class AccountItemData
{
    /// <summary>物品实例唯一标识。</summary>
    public string InstanceId { get; set; }
    /// <summary>物品类型。</summary>
    public string ItemType { get; set; }
    /// <summary>物品配置标识。</summary>
    public string ItemId { get; set; }
    /// <summary>物品数量。</summary>
    public int Count { get; set; }
    /// <summary>物品等级。</summary>
    public int Level { get; set; }
    /// <summary>物品经验。</summary>
    public int Exp { get; set; }
    /// <summary>物品属性。</summary>
    public Dictionary<string, float> Attributes { get; set; } = new Dictionary<string, float>();
    /// <summary>物品扩展数据。</summary>
    public Dictionary<string, string> ExtraData { get; set; } = new Dictionary<string, string>();
    /// <summary>已装备的遗物。</summary>
    public Dictionary<string, string> EquippedRelics { get; set; } = new Dictionary<string, string>();
    /// <summary>已装备的武器。</summary>
    public string EquippedWeapon { get; set; } = "";
    /// <summary>物品星级。</summary>
    public int StarLevel { get; set; } = 0;
    /// <summary>天赋等级。</summary>
    public Dictionary<string, int> TalentLevels { get; set; } = new Dictionary<string, int>();

    /// <summary>初始化物品数据。</summary>
    public AccountItemData() { }

    /// <summary>使用物品类型、标识和数量初始化物品数据。</summary>
    public AccountItemData(string itemType, string itemId, int count = 1)
    {
        // 初始化物品实例及其默认成长数据。
        InstanceId = Guid.NewGuid().ToString("N");
        ItemType = itemType;
        ItemId = itemId;
        Count = count;
        Level = 1;
        Exp = 0;
        Attributes = new Dictionary<string, float>();
        ExtraData = new Dictionary<string, string>();
        EquippedRelics = new Dictionary<string, string>();
        EquippedWeapon = "";
        StarLevel = 0;
        TalentLevels = new Dictionary<string, int>();
    }

    /// <summary>获取升至下一级所需的经验值。</summary>
    public int ExpToNextLevel => Level * 100;

    /// <summary>获取角色指定天赋的等级。</summary>
    public int GetTalentLevel(string skillType)
    {
        if (TalentLevels.ContainsKey(skillType))
            return TalentLevels[skillType];
        return 1;
    }

    /// <summary>设置角色指定天赋的等级。</summary>
    public void SetTalentLevel(string skillType, int level)
    {
        TalentLevels[skillType] = level;
    }
}

#endregion

#region 数据库集合

/// <summary>
/// 账号数据库，集中保存账号、物品、抽卡和商店数据。
/// </summary>
public class AccountDatabase
{
    /// <summary>账号列表。</summary>
    public List<AccountData> AccountList { get; set; } = new List<AccountData>();
    /// <summary>各账号的物品列表。</summary>
    public Dictionary<string, List<AccountItemData>> AccountInfo { get; set; } = new Dictionary<string, List<AccountItemData>>();
    /// <summary>各账号的抽卡历史记录。</summary>
    public Dictionary<string, List<LotteryHistoryRecord>> LotteryHistoryByAccount { get; set; } = new Dictionary<string, List<LotteryHistoryRecord>>();
    /// <summary>各账号的抽卡保底数据。</summary>
    public Dictionary<string, Dictionary<string, LotteryPityData>> PityDataByAccount { get; set; } = new Dictionary<string, Dictionary<string, LotteryPityData>>();
    /// <summary>各账号的商店购买数据。</summary>
    public Dictionary<string, Dictionary<string, int>> StorePurchaseDataByAccount { get; set; } = new Dictionary<string, Dictionary<string, int>>();

    /// <summary>初始化账号数据库集合。</summary>
    public AccountDatabase()
    {
        // 初始化各类账号数据集合。
        AccountList = new List<AccountData>();
        AccountInfo = new Dictionary<string, List<AccountItemData>>();
        LotteryHistoryByAccount = new Dictionary<string, List<LotteryHistoryRecord>>();
        PityDataByAccount = new Dictionary<string, Dictionary<string, LotteryPityData>>();
        StorePurchaseDataByAccount = new Dictionary<string, Dictionary<string, int>>();
    }
}

#endregion

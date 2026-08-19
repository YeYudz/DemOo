using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YokiFrame;

/// <summary>
/// 分解系统：计算并执行武器、遗器分解后的材料返还。
/// </summary>
public class DecomposeSystem : Singleton<DecomposeSystem>
{
    #region 分解奖励计算

    /// <summary>分解奖励数据，记录材料类型与数量。</summary>
    public struct MaterialReward
    {
        public string MaterialId;
        public int Count;
    }

    public List<MaterialReward> CalculateDecomposeReward(AccountItemData item)
    {
        var rewards = new List<MaterialReward>();

        if (item == null || (item.ItemType != "Weapon" && item.ItemType != "Relic"))
            return rewards;

        int rarity = GetItemRarity(item);
        int level = item.Level;

        var config = GameDataMgr.Instance.decomposeConfigs?.Decompositions
            ?.FirstOrDefault(c =>
                c.ItemType == item.ItemType &&
                rarity >= c.MinRarity && rarity <= c.MaxRarity &&
                level >= c.MinLevel && level <= c.MaxLevel);

        if (config == null)
            return rewards;

        int itemCount = item.Count > 0 ? item.Count : 1;
        foreach (var material in config.Materials)
        {
            rewards.Add(new MaterialReward
            {
                MaterialId = material.MaterialId,
                Count = material.MaterialCount * itemCount
            });
        }

        return rewards;
    }

    public Dictionary<string, int> CalculateBatchReward(List<AccountItemData> items)
    {
        var aggregated = new Dictionary<string, int>();

        foreach (var item in items)
        {
            var rewards = CalculateDecomposeReward(item);
            foreach (var reward in rewards)
            {
                if (aggregated.ContainsKey(reward.MaterialId))
                    aggregated[reward.MaterialId] += reward.Count;
                else
                    aggregated[reward.MaterialId] = reward.Count;
            }
        }

        return aggregated;
    }

    public bool CanDecompose(string accountId, AccountItemData item)
    {
        if (item == null || string.IsNullOrEmpty(item.InstanceId)) return false;
        if (item.ItemType != "Weapon" && item.ItemType != "Relic") return false;

        var accountItem = AccountManager.Instance.GetAccountItemByInstanceId(
            accountId, item.ItemType, item.InstanceId);
        if (accountItem == null) return false;

        bool markedEquipped = accountItem.ExtraData != null &&
            ((accountItem.ExtraData.TryGetValue("IsEquipped", out string isEquipped) && isEquipped == "true") ||
             (accountItem.ExtraData.TryGetValue("EquippedBy", out string equippedBy) && !string.IsNullOrEmpty(equippedBy)));
        if (markedEquipped) return false;

        var characters = AccountManager.Instance.GetItemsByType(accountId, "Character");
        if (item.ItemType == "Weapon")
            return !characters.Any(character => character.EquippedWeapon == item.InstanceId);

        return !characters.Any(character =>
            character.EquippedRelics != null && character.EquippedRelics.Values.Contains(item.InstanceId));
    }

    /// <summary>批量校验并分解物品，发放对应材料。</summary>
    public DecomposeResult DecomposeItems(string accountId, List<AccountItemData> items)
    {
        var result = new DecomposeResult
        {
            Success = false,
            Rewards = new Dictionary<string, int>(),
            DecomposedCount = 0,
            FailedItems = new List<string>()
        };

        if (items == null || items.Count == 0)
            return result;

        var decomposedItems = new List<AccountItemData>();
        foreach (var item in items)
        {
            var accountItem = AccountManager.Instance.GetAccountItemByInstanceId(
                accountId, item.ItemType, item.InstanceId);
            if (accountItem == null)
            {
                result.FailedItems.Add($"{item.ItemId}(不存在)");
                continue;
            }

            if (!CanDecompose(accountId, accountItem))
            {
                result.FailedItems.Add($"{accountItem.ItemId}(已装备)");
                continue;
            }

            bool removed = AccountManager.Instance.RemoveAccountItemByInstanceId(
                accountId, accountItem.ItemType, accountItem.InstanceId);
            if (removed)
            {
                result.DecomposedCount++;
                decomposedItems.Add(accountItem);
            }
            else
            {
                result.FailedItems.Add($"{accountItem.ItemId}(移除失败)");
            }
        }

        var rewards = CalculateBatchReward(decomposedItems);
        foreach (var kvp in rewards)
        {
            if (kvp.Value > 0)
            {
                AccountManager.Instance.AddAccountItem(accountId, "Consumable", kvp.Key, kvp.Value);
                result.Rewards[kvp.Key] = kvp.Value;
            }
        }

        result.Success = result.DecomposedCount > 0;
        return result;
    }

    private int GetItemRarity(AccountItemData item)
    {
        if (item.ItemType == "Weapon")
        {
            var weaponConfig = GameDataMgr.Instance.lightConeConfigs?.Weapons
                ?.FirstOrDefault(w => w.WeaponId == item.ItemId);
            return weaponConfig?.Rarity ?? 0;
        }
        else if (item.ItemType == "Relic")
        {
            var relicConfig = GameDataMgr.Instance.relicConfigs?.Relics
                ?.FirstOrDefault(r => r.RelicId == item.ItemId);
            return relicConfig?.Rarity ?? 0;
        }
        return 0;
    }

    #endregion
}

/// <summary>
/// 分解操作结果，包含成功状态、奖励和失败物品。
/// </summary>
public class DecomposeResult
{
    public bool Success;
    public int DecomposedCount;
    public Dictionary<string, int> Rewards;
    public List<string> FailedItems;
}

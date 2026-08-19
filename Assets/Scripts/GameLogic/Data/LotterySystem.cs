using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YokiFrame;

/// <summary>
/// 抽卡系统：处理卡池配置、概率、保底、记录与奖励发放。
/// </summary>
public class LotterySystem : Singleton<LotterySystem>
{
    #region 卡池与保底数据

    private Dictionary<string, LotteryPityData> mPityDataCache = new Dictionary<string, LotteryPityData>();
    private List<LotteryHistoryRecord> mLotteryHistory = new List<LotteryHistoryRecord>();

    public LotteryPoolConfig GetPoolConfig(string poolId)
    {
        var config = GameDataMgr.Instance.lotteryConfigs;
        if (config == null || config.Pools == null) return null;
        return config.Pools.FirstOrDefault(p => p.PoolId == poolId);
    }

    public LotteryPityData GetPityData(string poolId)
    {
        if (mPityDataCache.TryGetValue(poolId, out var data))
            return data;

        data = new LotteryPityData { PoolId = poolId };
        mPityDataCache[poolId] = data;
        return data;
    }

    private LotteryDrawItem ResolveItemId(string itemId)
    {
        var character = GameDataMgr.Instance.playerCharacterConfigs.Characters
            .FirstOrDefault(c => c.CharacterId == itemId);
        if (character != null)
        {
            return new LotteryDrawItem
            {
                ItemId = character.CharacterId,
                ItemName = character.Name,
                ItemType = "Character",
                Rarity = character.Rarity
            };
        }

        var weapon = GameDataMgr.Instance.lightConeConfigs.Weapons
            .FirstOrDefault(w => w.WeaponId == itemId);
        if (weapon != null)
        {
            return new LotteryDrawItem
            {
                ItemId = weapon.WeaponId,
                ItemName = weapon.Name,
                ItemType = "Weapon",
                Rarity = weapon.Rarity
            };
        }

        return null;
    }

    private List<LotteryDrawItem> ResolveItems(List<string> itemIds)
    {
        var result = new List<LotteryDrawItem>();
        if (itemIds == null) return result;

        foreach (var id in itemIds)
        {
            var item = ResolveItemId(id);
            if (item != null)
            {
                result.Add(item);
            }
        }
        return result;
    }

    /// <summary>执行指定卡池的单抽或十连抽。</summary>
    public LotteryDrawResult PerformLottery(string poolId, int drawCount)
    {
        var poolConfig = GetPoolConfig(poolId);
        if (poolConfig == null)
        {
            LogKit.Log($"[LotterySystem] 卡池 {poolId} 配置不存在");
            return null;
        }

        string accountId = AccountManager.Instance.CurrentAccount?.AccountId;
        if (string.IsNullOrEmpty(accountId))
        {
            LogKit.Log("[LotterySystem] 未登录，无法抽奖");
            return null;
        }

        int diamondCost = drawCount == 1 ? poolConfig.DiamondCost : poolConfig.TenDrawCost;
        int currentDiamond = AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", "Diamond");
        if (currentDiamond < diamondCost)
        {
            LogKit.Log($"[LotterySystem] 钻石不足，需要{diamondCost}，当前{currentDiamond}");
            return null;
        }

        AccountManager.Instance.RemoveAccountItem(accountId, "Consumable", "Diamond", diamondCost);

        var pityData = GetPityData(poolId);
        var results = new LotteryDrawResult
        {
            DiamondCost = diamondCost,
            DrawCount = drawCount
        };

        for (int i = 0; i < drawCount; i++)
        {
            var record = PerformSingleDraw(poolConfig, pityData);
            if (record != null)
            {
                results.Results.Add(record);
                mLotteryHistory.Add(record);
                AddLotteryItemToAccount(accountId, record);
            }
        }

        AccountManager.Instance.SaveDatabase();
        return results;
    }

    private LotteryHistoryRecord PerformSingleDraw(LotteryPoolConfig poolConfig, LotteryPityData pityData)
    {
        pityData.DrawCountSinceFiveStar++;
        pityData.DrawCountSinceFourStar++;

        bool mustBeFiveStar = pityData.DrawCountSinceFiveStar >= poolConfig.GuaranteedFiveStarEvery;
        bool mustBeFourStar = pityData.DrawCountSinceFourStar >= poolConfig.GuaranteedFourStarEvery;
        bool fiveStarHit = false;

        if (mustBeFiveStar)
        {
            fiveStarHit = true;
        }
        else
        {
            float fiveStarRate = poolConfig.BaseFiveStarRate;
            if (pityData.DrawCountSinceFiveStar >= 70)
            {
                fiveStarRate += (pityData.DrawCountSinceFiveStar - 70) * 0.06f;
            }
            fiveStarHit = Random.value < fiveStarRate;
        }

        LotteryDrawItem drawnItem = null;
        bool isUp = false;

        var fiveStarPool = ResolveItems(poolConfig.FiveStarItemIds);
        var fourStarPool = ResolveItems(poolConfig.FourStarItemIds);
        var threeStarPool = ResolveItems(poolConfig.ThreeStarItemIds);

        bool skipFiveStar = false;

        if (fiveStarHit && fiveStarPool.Count > 0)
        {
            pityData.DrawCountSinceFiveStar = 0;
            float rollValue = Random.value;
            bool hit50Percent = rollValue < 0.5f;

            LogKit.Log($"[抽奖] 5星 - 随机值: {rollValue:F4}, 命中50%(非UP): {hit50Percent}, NextFiveStarMustBeUp: {pityData.NextFiveStarMustBeUp}");

            if (poolConfig.HasUpSystem && pityData.NextFiveStarMustBeUp > 0)
            {
                drawnItem = GetRandomDrawItem(poolConfig.UpFiveStarIds, fiveStarPool);
                pityData.NextFiveStarMustBeUp--;
                isUp = true;
                LogKit.Log($"[抽奖] 5星 - 必为UP(保底): {drawnItem?.ItemName}");
            }
            else if (poolConfig.HasUpSystem && hit50Percent && poolConfig.UpFiveStarIds.Count > 0)
            {
                var nonUpItems = fiveStarPool.Where(i => !poolConfig.UpFiveStarIds.Contains(i.ItemId)).ToList();
                LogKit.Log($"[抽奖] 5星 - 命中50%→非UP分支, 非UP池大小: {nonUpItems.Count}");

                if (nonUpItems.Count > 0)
                {
                    drawnItem = nonUpItems[Random.Range(0, nonUpItems.Count)];
                    pityData.NextFiveStarMustBeUp = 1;
                    LogKit.Log($"[抽奖] 5星 - 50%命中→非UP: {drawnItem?.ItemName}, 下次必UP");
                }
                else
                {
                    LogKit.Warning("[抽奖] 5星 - 非UP池为空，回退到UP");
                    drawnItem = GetRandomDrawItem(poolConfig.UpFiveStarIds, fiveStarPool);
                    isUp = true;
                    pityData.NextFiveStarMustBeUp = 0;
                    LogKit.Log($"[抽奖] 5星 - 回退→UP: {drawnItem?.ItemName}");
                }
            }
            else
            {
                if (poolConfig.HasUpSystem && poolConfig.UpFiveStarIds.Count > 0)
                {
                    drawnItem = GetRandomDrawItem(poolConfig.UpFiveStarIds, fiveStarPool);
                    isUp = true;
                    pityData.NextFiveStarMustBeUp = 0;
                    LogKit.Log($"[抽奖] 5星 - 未命中50%→UP: {drawnItem?.ItemName}, 下次继续随机");
                }
                else
                {
                    drawnItem = fiveStarPool[Random.Range(0, fiveStarPool.Count)];
                    LogKit.Log($"[抽奖] 5星 - 无UP系统: {drawnItem?.ItemName}");
                }
            }

            if (!skipFiveStar)
            {
                pityData.TotalFiveStarDraws++;
            }
        }

        if (drawnItem == null)
        {
            bool fourStarHit = false;
            if (mustBeFourStar)
            {
                fourStarHit = true;
            }
            else
            {
                fourStarHit = Random.value < poolConfig.BaseFourStarRate;
            }

            if (fourStarHit && fourStarPool.Count > 0)
            {
                pityData.DrawCountSinceFourStar = 0;
                float fourStarRoll = Random.value;
                bool fourStarHit50 = fourStarRoll < 0.5f;

                LogKit.Log($"[抽奖] 4星 - 随机值: {fourStarRoll:F4}, 命中50%(非UP): {fourStarHit50}, NextFourStarMustBeUp: {pityData.NextFourStarMustBeUp}");

                if (poolConfig.HasUpSystem && pityData.NextFourStarMustBeUp > 0 && poolConfig.UpFourStarIds.Count > 0)
                {
                    drawnItem = GetRandomDrawItem(poolConfig.UpFourStarIds, fourStarPool);
                    pityData.NextFourStarMustBeUp--;
                    isUp = true;
                    LogKit.Log($"[抽奖] 4星 - 必为UP(保底): {drawnItem?.ItemName}");
                }
                else if (poolConfig.HasUpSystem && fourStarHit50 && poolConfig.UpFourStarIds.Count > 0)
                {
                    var nonUpItems = fourStarPool.Where(i => !poolConfig.UpFourStarIds.Contains(i.ItemId)).ToList();
                    LogKit.Log($"[抽奖] 4星 - 命中50%→非UP分支, 非UP池大小: {nonUpItems.Count}");
                    if (nonUpItems.Count > 0)
                    {
                        drawnItem = nonUpItems[Random.Range(0, nonUpItems.Count)];
                        pityData.NextFourStarMustBeUp = 1;
                        LogKit.Log($"[抽奖] 4星 - 50%命中→非UP: {drawnItem?.ItemName}, 下次必UP");
                    }
                    else
                    {
                        LogKit.Warning("[抽奖] 4星 - 非UP池为空，回退到UP");
                        drawnItem = GetRandomDrawItem(poolConfig.UpFourStarIds, fourStarPool);
                        isUp = true;
                        pityData.NextFourStarMustBeUp = 0;
                        LogKit.Log($"[抽奖] 4星 - 回退→UP: {drawnItem?.ItemName}");
                    }
                }
                else
                {
                    if (poolConfig.HasUpSystem && poolConfig.UpFourStarIds.Count > 0)
                    {
                        drawnItem = GetRandomDrawItem(poolConfig.UpFourStarIds, fourStarPool);
                        isUp = true;
                        pityData.NextFourStarMustBeUp = 0;
                        LogKit.Log($"[抽奖] 4星 - 未命中50%→UP: {drawnItem?.ItemName}, 下次继续随机");
                    }
                    else
                    {
                        var nonUpItems = fourStarPool.Where(i => !poolConfig.UpFourStarIds.Contains(i.ItemId)).ToList();
                        if (nonUpItems.Count > 0)
                        {
                            drawnItem = nonUpItems[Random.Range(0, nonUpItems.Count)];
                            LogKit.Log($"[抽奖] 4星 - 无UP系统→非UP: {drawnItem?.ItemName}");
                        }
                        else
                        {
                            drawnItem = fourStarPool[Random.Range(0, fourStarPool.Count)];
                            LogKit.Log($"[抽奖] 4星 - 完整池: {drawnItem?.ItemName}");
                        }
                    }
                }

                pityData.TotalFourStarDraws++;
            }
            else if (threeStarPool.Count > 0)
            {
                drawnItem = threeStarPool[Random.Range(0, threeStarPool.Count)];
            }
        }

        if (drawnItem == null && threeStarPool.Count > 0)
        {
            drawnItem = threeStarPool[Random.Range(0, threeStarPool.Count)];
        }

        return new LotteryHistoryRecord
        {
            RecordId = System.Guid.NewGuid().ToString(),
            ItemId = drawnItem?.ItemId ?? "",
            ItemName = drawnItem?.ItemName ?? "未知物品",
            ItemType = drawnItem?.ItemType ?? "",
            Rarity = drawnItem?.Rarity ?? 3,
            PoolId = poolConfig.PoolId,
            Timestamp = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
            IsUp = isUp
        };
    }

    private LotteryDrawItem GetRandomDrawItem(List<string> itemIds, List<LotteryDrawItem> items)
    {
        if (items == null || items.Count == 0) return null;

        if (itemIds != null && itemIds.Count > 0)
        {
            var filtered = items.Where(i => itemIds.Contains(i.ItemId)).ToList();
            if (filtered.Count > 0)
            {
                return filtered[Random.Range(0, filtered.Count)];
            }
        }

        return items[Random.Range(0, items.Count)];
    }

    public List<LotteryHistoryRecord> GetLotteryHistory(int count = 0)
    {
        if (count <= 0)
            return new List<LotteryHistoryRecord>(mLotteryHistory);
        int skip = Mathf.Max(0, mLotteryHistory.Count - count);
        return mLotteryHistory.Skip(skip).ToList();
    }

    public Dictionary<string, LotteryPityData> GetAllPityData()
    {
        return mPityDataCache;
    }

    public int GetDiamondCount()
    {
        string accountId = AccountManager.Instance.CurrentAccount?.AccountId;
        if (string.IsNullOrEmpty(accountId)) return 0;
        return AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", "Diamond");
    }

    private void AddLotteryItemToAccount(string accountId, LotteryHistoryRecord record)
    {
        if (string.IsNullOrEmpty(record.ItemId)) return;

        if (record.ItemType == "Character")
        {
            var existingChar = AccountManager.Instance.GetAccountItem(accountId, "Character", record.ItemId);
            if (existingChar == null)
            {
                AccountManager.Instance.AddAccountItem(accountId, "Character", record.ItemId, 1);
                var charConfig = GameDataMgr.Instance.playerCharacterConfigs.Characters
                    .FirstOrDefault(c => c.CharacterId == record.ItemId);
                if (charConfig != null)
                {
                    var newChar = AccountManager.Instance.GetAccountItem(accountId, "Character", record.ItemId);
                    if (newChar != null)
                    {
                        newChar.Level = 1;
                        newChar.StarLevel = 0;
                        newChar.Exp = 0;
                        newChar.Attributes["MaxHp"] = charConfig.MaxHp;
                        newChar.Attributes["Attack"] = charConfig.Attack;
                        newChar.Attributes["Defense"] = charConfig.Defense;
                        newChar.Attributes["Speed"] = charConfig.Speed;
                        newChar.Attributes["CritRate"] = charConfig.CritRate;
                        newChar.Attributes["CritDamage"] = charConfig.CritDamage;
                    }
                }
                LogKit.Log($"[LotterySystem] 获得新角色: {record.ItemName}");
            }
            else
            {
                string shardId = record.ItemId + "_Shard";
                AccountManager.Instance.AddAccountItem(accountId, "Consumable", shardId, 1);
                LogKit.Log($"[LotterySystem] 已有角色，获得碎片: {record.ItemName} 碎片 x1");
            }
        }
        else if (record.ItemType == "Weapon")
        {
            AccountManager.Instance.AddAccountItem(accountId, "Weapon", record.ItemId, 1);
            LogKit.Log($"[LotterySystem] 获得武器: {record.ItemName}");
        }

        AccountManager.Instance.SaveDatabase();
    }

    public void LoadLotteryData(string accountId)
    {
        var db = AccountManager.Instance.Database;
        if (db == null) return;

        if (db.LotteryHistoryByAccount.TryGetValue(accountId, out var savedHistory))
        {
            mLotteryHistory = savedHistory ?? new List<LotteryHistoryRecord>();
        }
        else
        {
            mLotteryHistory = new List<LotteryHistoryRecord>();
        }

        mPityDataCache.Clear();
        if (db.PityDataByAccount.TryGetValue(accountId, out var savedPity) && savedPity != null)
        {
            foreach (var kv in savedPity)
            {
                mPityDataCache[kv.Key] = kv.Value;
            }
        }
    }

    public void SaveLotteryData()
    {
        var account = AccountManager.Instance.CurrentAccount;
        if (account == null) return;
        var db = AccountManager.Instance.Database;
        if (db == null) return;
        string accountId = account.AccountId;
        db.LotteryHistoryByAccount[accountId] = new List<LotteryHistoryRecord>(mLotteryHistory);
        db.PityDataByAccount[accountId] = new Dictionary<string, LotteryPityData>(mPityDataCache);
    }

    /// <summary>清理当前账号在内存中的抽卡数据。</summary>
    public void ClearLotteryData()
    {
        mLotteryHistory.Clear();
        mPityDataCache.Clear();
    }

    #endregion
}

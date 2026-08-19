using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YokiFrame;

/// <summary>
/// 商店系统：管理商品库存、购买记录与货币扣除。
/// </summary>
public class StoreSystem : Singleton<StoreSystem>
{
    #region 购买记录

    private Dictionary<string, int> mPurchaseCounts = new Dictionary<string, int>();

    public void LoadPurchaseData(string accountId)
    {
        mPurchaseCounts.Clear();
        var db = AccountManager.Instance.Database;
        if (db != null && db.StorePurchaseDataByAccount.TryGetValue(accountId, out var saved) && saved != null)
        {
            foreach (var kv in saved)
            {
                mPurchaseCounts[kv.Key] = kv.Value;
            }
        }
        LogKit.Log($"[StoreSystem] 加载账号 {accountId} 的购买记录: {mPurchaseCounts.Count} 条");
    }

    public void SavePurchaseData()
    {
        var account = AccountManager.Instance.CurrentAccount;
        if (account == null) return;
        var db = AccountManager.Instance.Database;
        if (db == null) return;
        db.StorePurchaseDataByAccount[account.AccountId] = new Dictionary<string, int>(mPurchaseCounts);
    }

    public void ClearPurchaseData()
    {
        mPurchaseCounts.Clear();
    }

    public List<StoreItemConfig> GetItemsByCategory(string category)
    {
        var configs = GameDataMgr.Instance.storeConfigs?.StoreItems;
        if (configs == null) return new List<StoreItemConfig>();
        return configs.Where(c => c.Category == category).ToList();
    }

    public int GetRemainingStock(StoreItemConfig config)
    {
        if (config == null || config.Stock <= 0) return -1;
        string key = config.ItemId;
        int purchased = mPurchaseCounts.ContainsKey(key) ? mPurchaseCounts[key] : 0;
        return Mathf.Max(0, config.Stock - purchased);
    }

    public int GetMaxPurchaseCount(StoreItemConfig config)
    {
        int remaining = GetRemainingStock(config);
        if (remaining == -1)
        {
            string accountId = AccountManager.Instance.CurrentAccount?.AccountId;
            if (string.IsNullOrEmpty(accountId)) return 0;
            int currency = AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", config.Currency);
            return currency / Mathf.Max(1, config.Price);
        }
        if (remaining == 0) return 0;

        string accId = AccountManager.Instance.CurrentAccount?.AccountId;
        if (string.IsNullOrEmpty(accId)) return 0;
        int cur = AccountManager.Instance.GetAccountItemCount(accId, "Consumable", config.Currency);
        int affordable = cur / Mathf.Max(1, config.Price);
        return Mathf.Min(remaining, affordable);
    }

    public bool CanPurchase(StoreItemConfig config)
    {
        if (config == null) return false;
        int remaining = GetRemainingStock(config);
        if (remaining == 0) return false;
        string accountId = AccountManager.Instance.CurrentAccount?.AccountId;
        if (string.IsNullOrEmpty(accountId)) return false;
        int currencyCount = AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", config.Currency);
        return currencyCount >= config.Price;
    }

    /// <summary>校验库存与货币后购买商品。</summary>
    public PurchaseResult PurchaseItem(StoreItemConfig config, int buyCount = 1)
    {
        var result = new PurchaseResult
        {
            Success = false,
            Message = "",
            PurchasedItem = null
        };

        if (config == null)
        {
            result.Message = "商品不存在";
            return result;
        }

        if (buyCount < 1) buyCount = 1;

        string accountId = AccountManager.Instance.CurrentAccount?.AccountId;
        if (string.IsNullOrEmpty(accountId))
        {
            result.Message = "请先登录";
            return result;
        }

        int remaining = GetRemainingStock(config);
        if (remaining == 0)
        {
            result.Message = "商品已售罄";
            return result;
        }

        if (remaining > 0 && buyCount > remaining)
        {
            result.Message = $"超过限购数量（剩余{remaining}次）";
            return result;
        }

        int totalPrice = config.Price * buyCount;
        int currencyCount = AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", config.Currency);
        if (currencyCount < totalPrice)
        {
            result.Message = "货币不足";
            return result;
        }

        bool deducted = AccountManager.Instance.RemoveAccountItem(accountId, "Consumable", config.Currency, totalPrice);
        if (!deducted)
        {
            result.Message = "扣款失败";
            return result;
        }

        int itemQuantity = (config.Quantity > 0 ? config.Quantity : 1) * buyCount;
        AccountManager.Instance.AddAccountItem(accountId, config.ItemType, config.ItemId, itemQuantity);

        if (!mPurchaseCounts.ContainsKey(config.ItemId))
            mPurchaseCounts[config.ItemId] = 0;
        mPurchaseCounts[config.ItemId] += buyCount;

        SavePurchaseData();
        AccountManager.Instance.SaveDatabase();

        result.Success = true;
        result.Message = $"购买成功: {config.ItemId} x{itemQuantity}";
        result.PurchasedItem = config;

        LogKit.Log($"[StoreSystem] 购买成功: {config.ItemId} x{itemQuantity}, 花费 {totalPrice} {config.Currency}");
        return result;
    }

    /// <summary>清空当前账号的商店购买次数。</summary>
    public void ResetStock()
    {
        mPurchaseCounts.Clear();
        SavePurchaseData();
    }

    #endregion
}

/// <summary>商品购买结果。</summary>
public class PurchaseResult
{
    public bool Success;
    public string Message;
    public StoreItemConfig PurchasedItem;
}

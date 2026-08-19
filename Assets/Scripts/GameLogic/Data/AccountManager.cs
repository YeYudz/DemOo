using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using YokiFrame;

/// <summary>
/// 账号管理器：负责账号登录、存档加载及账号物品增删改查。
/// </summary>
public class AccountManager
{
    #region 单例与存档

    private static AccountManager instance = new AccountManager();
    public static AccountManager Instance => instance;

    private const string DatabaseFileName = "AccountDatabase";

    private AccountDatabase mDatabase;
    private AccountData mCurrentAccount;

    public AccountData CurrentAccount => mCurrentAccount;
    public bool IsLoggedIn => mCurrentAccount != null;
    public AccountDatabase Database => mDatabase;

    private AccountManager()
    {
        LoadDatabase();
    }

    public void LoadDatabase()
    {
        mDatabase = JsonMgr.Instance.LoadData<AccountDatabase>(DatabaseFileName);

        if (mDatabase.AccountList == null)
            mDatabase.AccountList = new List<AccountData>();
        if (mDatabase.AccountInfo == null)
            mDatabase.AccountInfo = new Dictionary<string, List<AccountItemData>>();
        if (mDatabase.StorePurchaseDataByAccount == null)
            mDatabase.StorePurchaseDataByAccount = new Dictionary<string, Dictionary<string, int>>();

        if (mDatabase.AccountList.Count == 0)
        {
            InitializeDefaultAccounts();
            SaveDatabase();
        }
        else
        {
            EnsureDefaultCharacters();
            MigrateOldSaveData();
        }
    }

    private void MigrateOldSaveData()
    {
        bool hasChanges = false;

        foreach (var accountEntry in mDatabase.AccountInfo)
        {
            // 确保所有账号都有必要的消耗品材料
            var requiredMaterials = new[]
            {
                new { ItemId = "Warrior_Shard", DefaultCount = 30 },
                new { ItemId = "Mage_Shard", DefaultCount = 30 },
                new { ItemId = "Healer_Shard", DefaultCount = 30 },
                new { ItemId = "Archer_Shard", DefaultCount = 30 },
                new { ItemId = "TalentBook", DefaultCount = 500 },
                new { ItemId = "SkillGem", DefaultCount = 300 },
                new { ItemId = "UpgradeStone", DefaultCount = 200 },
                new { ItemId = "RefiningAgent", DefaultCount = 200 },
                new { ItemId = "EnchantDust", DefaultCount = 200 },
                new { ItemId = "MagicCrystal", DefaultCount = 200 }
            };

            foreach (var material in requiredMaterials)
            {
                var existingItem = accountEntry.Value
                    .FirstOrDefault(item => item.ItemType == "Consumable" && item.ItemId == material.ItemId);

                if (existingItem == null)
                {
                    accountEntry.Value.Add(new AccountItemData("Consumable", material.ItemId, material.DefaultCount));
                    hasChanges = true;
                    LogKit.Log($"[AccountManager] 补充材料 {material.ItemId} 到账号 {accountEntry.Key}");
                }
                else if (existingItem.Count <= 1)
                {
                    existingItem.Count = material.DefaultCount;
                    hasChanges = true;
                    LogKit.Log($"[AccountManager] 补充材料 {material.ItemId} 数量到 {material.DefaultCount}");
                }
            }

            var stackedEquipment = accountEntry.Value
                .Where(item => (item.ItemType == "Weapon" || item.ItemType == "Relic") && item.Count > 1)
                .ToList();
            foreach (var item in stackedEquipment)
            {
                int additionalCount = item.Count - 1;
                item.Count = 1;
                for (int i = 0; i < additionalCount; i++)
                {
                    accountEntry.Value.Add(CloneEquipmentItem(item));
                }
                hasChanges = true;
            }

            var instanceIds = new HashSet<string>();
            foreach (var item in accountEntry.Value)
            {
                if (string.IsNullOrEmpty(item.InstanceId) || !instanceIds.Add(item.InstanceId))
                {
                    item.InstanceId = Guid.NewGuid().ToString("N");
                    instanceIds.Add(item.InstanceId);
                    hasChanges = true;
                }

                if (item.ItemType == "Character")
                {
                    if (item.Attributes == null)
                    {
                        item.Attributes = new Dictionary<string, float>();
                        hasChanges = true;
                    }

                    if (!item.Attributes.ContainsKey("CritRate"))
                    {
                        item.Attributes["CritRate"] = 0.1f;
                        hasChanges = true;
                    }
                    else if (item.Attributes["CritRate"] > 1f)
                    {
                        item.Attributes["CritRate"] = item.Attributes["CritRate"] / 100f;
                        hasChanges = true;
                    }

                    if (!item.Attributes.ContainsKey("CritDamage"))
                    {
                        item.Attributes["CritDamage"] = 0.5f;
                        hasChanges = true;
                    }
                    else if (item.Attributes["CritDamage"] > 1f)
                    {
                        item.Attributes["CritDamage"] = item.Attributes["CritDamage"] / 100f;
                        hasChanges = true;
                    }
                    if (!item.Attributes.ContainsKey("DamageBonus"))
                    {
                        item.Attributes["DamageBonus"] = 0f;
                        hasChanges = true;
                    }
                    if (!item.Attributes.ContainsKey("ResistancePenetration"))
                    {
                        item.Attributes["ResistancePenetration"] = 0f;
                        hasChanges = true;
                    }

                    if (item.ExtraData == null)
                    {
                        item.ExtraData = new Dictionary<string, string>();
                        hasChanges = true;
                    }

                    if (item.EquippedRelics == null)
                    {
                        item.EquippedRelics = new Dictionary<string, string>();
                        hasChanges = true;
                    }

                    if (item.EquippedWeapon == null)
                    {
                        item.EquippedWeapon = "";
                        hasChanges = true;
                    }
                }

                if (item.ExtraData == null)
                {
                    item.ExtraData = new Dictionary<string, string>();
                    hasChanges = true;
                }

                // 迁移：为遗器生成随机词条数据
                if (item.ItemType == "Relic")
                {
                    bool needsGenerate = false;

                    if (!item.ExtraData.ContainsKey("RelicData"))
                    {
                        needsGenerate = true;
                    }
                    else
                    {
                        string relicDataStr = item.ExtraData["RelicData"];
                        if (string.IsNullOrEmpty(relicDataStr) || relicDataStr == "{}")
                        {
                            needsGenerate = true;
                        }
                    }

                    if (needsGenerate)
                    {
                        var relicConfig = GameDataMgr.Instance.relicConfigs?.Relics?
                            .FirstOrDefault(r => r.RelicId == item.ItemId);

                        if (relicConfig != null)
                        {
                            var relicData = UpgradeSystem.Instance.GenerateRelicData(relicConfig);
                            item.ExtraData["RelicData"] = JsonUtility.ToJson(relicData);
                            item.Level = relicData.Level;
                            hasChanges = true;
                            LogKit.Log($"[AccountManager] 为遗器 {item.ItemId} 生成随机词条");
                        }
                    }
                }
            }

            foreach (var character in accountEntry.Value.Where(item => item.ItemType == "Character"))
            {
                if (!string.IsNullOrEmpty(character.EquippedWeapon) &&
                    GetAccountItemByInstanceId(accountEntry.Key, "Weapon", character.EquippedWeapon) == null)
                {
                    var equippedWeapon = accountEntry.Value.FirstOrDefault(item =>
                        item.ItemType == "Weapon" && item.ItemId == character.EquippedWeapon &&
                        item.ExtraData != null && item.ExtraData.ContainsKey("EquippedBy") &&
                        item.ExtraData["EquippedBy"] == character.ItemId)
                        ?? accountEntry.Value.FirstOrDefault(item => item.ItemType == "Weapon" && item.ItemId == character.EquippedWeapon);
                    if (equippedWeapon != null)
                    {
                        character.EquippedWeapon = equippedWeapon.InstanceId;
                        hasChanges = true;
                    }
                }

                if (character.EquippedRelics == null) continue;
                foreach (var slot in character.EquippedRelics.Keys.ToList())
                {
                    string equippedValue = character.EquippedRelics[slot];
                    if (GetAccountItemByInstanceId(accountEntry.Key, "Relic", equippedValue) != null) continue;

                    var equippedRelic = accountEntry.Value.FirstOrDefault(item =>
                        item.ItemType == "Relic" && item.ItemId == equippedValue &&
                        item.ExtraData != null && item.ExtraData.ContainsKey("EquippedBy") &&
                        item.ExtraData["EquippedBy"] == character.ItemId &&
                        (!item.ExtraData.ContainsKey("EquippedSlot") || item.ExtraData["EquippedSlot"] == slot))
                        ?? accountEntry.Value.FirstOrDefault(item => item.ItemType == "Relic" && item.ItemId == equippedValue);
                    if (equippedRelic != null)
                    {
                        character.EquippedRelics[slot] = equippedRelic.InstanceId;
                        hasChanges = true;
                    }
                }
            }
        }

        if (hasChanges)
        {
            SaveDatabase();
            LogKit.Log("[AccountManager] 旧存档数据迁移完成");
        }
    }

    private void EnsureDefaultCharacters()
    {
        if (!mDatabase.AccountInfo.ContainsKey("admin"))
        {
            mDatabase.AccountInfo["admin"] = new List<AccountItemData>();
        }

        var adminItems = mDatabase.AccountInfo["admin"];

        if (adminItems.FirstOrDefault(item => item.ItemType == "Character" && item.ItemId == "Warrior") == null)
        {
            var warrior = new AccountItemData("Character", "Warrior", 1)
            {
                Level = 10,
                Exp = 500,
                Attributes = {
                    { "MaxHp", 1000 }, { "Speed", 120 }, { "Attack", 150 }, { "Defense", 80 },
                    { "CritRate", 10 }, { "CritDamage", 50 }, { "DamageBonus", 0 }, { "ResistancePenetration", 0 }
                },
                EquippedRelics = new Dictionary<string, string>(),
                EquippedWeapon = ""
            };
            adminItems.Add(warrior);
        }

        if (adminItems.FirstOrDefault(item => item.ItemType == "Character" && item.ItemId == "Mage") == null)
        {
            var mage = new AccountItemData("Character", "Mage", 1)
            {
                Level = 10,
                Exp = 300,
                Attributes = {
                    { "MaxHp", 700 }, { "Speed", 150 }, { "Attack", 200 }, { "Defense", 40 },
                    { "CritRate", 15 }, { "CritDamage", 70 }, { "DamageBonus", 0 }, { "ResistancePenetration", 0 }
                },
                EquippedRelics = new Dictionary<string, string>(),
                EquippedWeapon = ""
            };
            adminItems.Add(mage);
        }

        if (adminItems.FirstOrDefault(item => item.ItemType == "Weapon" && item.ItemId == "IronSword") == null)
        {
            adminItems.Add(new AccountItemData("Weapon", "IronSword", 1) { Level = 1 });
        }

        if (adminItems.FirstOrDefault(item => item.ItemType == "Weapon" && item.ItemId == "WoodenStaff") == null)
        {
            adminItems.Add(new AccountItemData("Weapon", "WoodenStaff", 1) { Level = 1 });
        }

        if (adminItems.FirstOrDefault(item => item.ItemType == "Relic" && item.ItemId == "WarriorHelm") == null)
        {
            adminItems.Add(UpgradeSystem.Instance.CreateRelicItemWithData("WarriorHelm"));
        }

        if (adminItems.FirstOrDefault(item => item.ItemType == "Relic" && item.ItemId == "IronGauntlets") == null)
        {
            adminItems.Add(UpgradeSystem.Instance.CreateRelicItemWithData("IronGauntlets"));
        }

        if (adminItems.FirstOrDefault(item => item.ItemType == "Character" && item.ItemId == "Healer") == null)
        {
            var healer = new AccountItemData("Character", "Healer", 1)
            {
                Level = 8,
                Exp = 200,
                Attributes = {
                    { "MaxHp", 700 }, { "Speed", 90 }, { "Attack", 50 }, { "Defense", 50 },
                    { "CritRate", 0.05f }, { "CritDamage", 0.4f }, { "DamageBonus", 0 }, { "ResistancePenetration", 0 }
                },
                EquippedRelics = new Dictionary<string, string>(),
                EquippedWeapon = ""
            };
            adminItems.Add(healer);
        }

        EnsureConsumableItem(adminItems, "Warrior_Shard", 30);
        EnsureConsumableItem(adminItems, "Mage_Shard", 30);
        EnsureConsumableItem(adminItems, "Healer_Shard", 30);
        EnsureConsumableItem(adminItems, "TalentBook", 500);
        EnsureConsumableItem(adminItems, "SkillGem", 300);
        EnsureConsumableItem(adminItems, "UpgradeStone", 200);
        EnsureConsumableItem(adminItems, "RefiningAgent", 200);
        EnsureConsumableItem(adminItems, "EnchantDust", 200);
        EnsureConsumableItem(adminItems, "MagicCrystal", 200);

        if (!mDatabase.AccountInfo.ContainsKey("test"))
        {
            mDatabase.AccountInfo["test"] = new List<AccountItemData>();
        }

        var testItems = mDatabase.AccountInfo["test"];

        if (testItems.FirstOrDefault(item => item.ItemType == "Character" && item.ItemId == "Archer") == null)
        {
            var archer = new AccountItemData("Character", "Archer", 1)
            {
                Level = 1,
                Exp = 0,
                Attributes = {
                    { "MaxHp", 600 }, { "Speed", 180 }, { "Attack", 180 }, { "Defense", 30 },
                    { "CritRate", 20 }, { "CritDamage", 60 }, { "DamageBonus", 0 }, { "ResistancePenetration", 0 }
                },
                EquippedRelics = new Dictionary<string, string>(),
                EquippedWeapon = ""
            };
            testItems.Add(archer);
        }

        if (testItems.FirstOrDefault(item => item.ItemType == "Weapon" && item.ItemId == "HuntersBow") == null)
        {
            testItems.Add(new AccountItemData("Weapon", "HuntersBow", 1) { Level = 1 });
        }

        EnsureConsumableItem(testItems, "Archer_Shard", 30);
        EnsureConsumableItem(testItems, "TalentBook", 200);
        EnsureConsumableItem(testItems, "SkillGem", 100);
        EnsureConsumableItem(testItems, "UpgradeStone", 100);
        EnsureConsumableItem(testItems, "RefiningAgent", 100);
        EnsureConsumableItem(testItems, "EnchantDust", 100);
        EnsureConsumableItem(testItems, "MagicCrystal", 100);

        SaveDatabase();
    }

    private AccountItemData CloneEquipmentItem(AccountItemData source)
    {
        var clone = new AccountItemData(source.ItemType, source.ItemId, 1)
        {
            Level = source.Level,
            Exp = source.Exp,
            Attributes = source.Attributes != null
                ? new Dictionary<string, float>(source.Attributes)
                : new Dictionary<string, float>(),
            ExtraData = source.ExtraData != null
                ? new Dictionary<string, string>(source.ExtraData)
                : new Dictionary<string, string>(),
            StarLevel = source.StarLevel,
            TalentLevels = source.TalentLevels != null
                ? new Dictionary<string, int>(source.TalentLevels)
                : new Dictionary<string, int>()
        };
        clone.ExtraData.Remove("IsEquipped");
        clone.ExtraData.Remove("EquippedBy");
        clone.ExtraData.Remove("EquippedSlot");
        return clone;
    }

    private void EnsureConsumableItem(List<AccountItemData> items, string itemId, int defaultCount)
    {
        var existing = items.FirstOrDefault(item => item.ItemType == "Consumable" && item.ItemId == itemId);
        if (existing == null)
        {
            items.Add(new AccountItemData("Consumable", itemId, defaultCount));
        }
    }

    public void SaveDatabase()
    {
        JsonMgr.Instance.SaveData(mDatabase, DatabaseFileName);
    }

    private void InitializeDefaultAccounts()
		{
			mDatabase.AccountList.Add(new AccountData("admin", "123456"));
		mDatabase.AccountInfo["admin"] = new List<AccountItemData>
		{
			new AccountItemData("Consumable", "Gold", 100000),
			new AccountItemData("Consumable", "Diamond", 10000),
				new AccountItemData("Character", "Warrior", 1) {
					Level = 10, Exp = 500,
					Attributes = {
						{ "MaxHp", 1000 }, { "Speed", 120 }, { "Attack", 150 }, { "Defense", 80 },
						{ "CritRate", 0.1f }, { "CritDamage", 0.5f }, { "DamageBonus", 0 }, { "ResistancePenetration", 0 }
					},
					EquippedRelics = new Dictionary<string, string>(),
					EquippedWeapon = ""
				},
				new AccountItemData("Character", "Mage", 1) {
					Level = 10, Exp = 300,
					Attributes = {
						{ "MaxHp", 700 }, { "Speed", 150 }, { "Attack", 200 }, { "Defense", 40 },
						{ "CritRate", 0.15f }, { "CritDamage", 0.7f }, { "DamageBonus", 0 }, { "ResistancePenetration", 0 }
					},
					EquippedRelics = new Dictionary<string, string>(),
					EquippedWeapon = ""
				},
				new AccountItemData("Character", "Healer", 1) {
					Level = 8, Exp = 200,
					Attributes = {
						{ "MaxHp", 700 }, { "Speed", 90 }, { "Attack", 50 }, { "Defense", 50 },
						{ "CritRate", 0.05f }, { "CritDamage", 0.4f }, { "DamageBonus", 0 }, { "ResistancePenetration", 0 }
					},
					EquippedRelics = new Dictionary<string, string>(),
					EquippedWeapon = ""
			},
			new AccountItemData("Consumable", "Warrior_Shard", 30),
			new AccountItemData("Consumable", "Mage_Shard", 30),
			new AccountItemData("Consumable", "Healer_Shard", 30),
				new AccountItemData("Consumable", "TalentBook", 500),
				new AccountItemData("Consumable", "SkillGem", 300),
				new AccountItemData("Consumable", "UpgradeStone", 200),
				new AccountItemData("Consumable", "RefiningAgent", 200),
				new AccountItemData("Consumable", "EnchantDust", 200),
				new AccountItemData("Consumable", "MagicCrystal", 200),
				new AccountItemData("Weapon", "IronSword", 1) { Level = 1 },
				new AccountItemData("Weapon", "WoodenStaff", 1) { Level = 1 },
				UpgradeSystem.Instance.CreateRelicItemWithData("WarriorHelm"),
				UpgradeSystem.Instance.CreateRelicItemWithData("IronGauntlets")
			};

			mDatabase.AccountList.Add(new AccountData("test", "123456"));
		mDatabase.AccountInfo["test"] = new List<AccountItemData>
		{
			new AccountItemData("Consumable", "Gold", 1000),
			new AccountItemData("Consumable", "Diamond", 100),
			new AccountItemData("Character", "Archer", 1) {
				Level = 1, Exp = 0,
				Attributes = {
					{ "MaxHp", 600 }, { "Speed", 180 }, { "Attack", 180 }, { "Defense", 30 },
					{ "CritRate", 0.2f }, { "CritDamage", 0.6f }, { "DamageBonus", 0 }, { "ResistancePenetration", 0 }
				},
				EquippedRelics = new Dictionary<string, string>(),
				EquippedWeapon = ""
			},
			new AccountItemData("Consumable", "Archer_Shard", 30),
				new AccountItemData("Consumable", "TalentBook", 200),
				new AccountItemData("Consumable", "SkillGem", 100),
				new AccountItemData("Consumable", "UpgradeStone", 100),
				new AccountItemData("Consumable", "RefiningAgent", 100),
				new AccountItemData("Consumable", "EnchantDust", 100),
				new AccountItemData("Consumable", "MagicCrystal", 100),
				new AccountItemData("Weapon", "HuntersBow", 1) { Level = 1 }
			};
		}

    public bool Register(string accountId, string password)
    {
        if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(password))
            return false;

        if (mDatabase.AccountList.Exists(account => account.AccountId == accountId))
            return false;

        mDatabase.AccountList.Add(new AccountData(accountId, password));
        mDatabase.AccountInfo[accountId] = new List<AccountItemData>
        {
            new AccountItemData("Consumable", "Gold", 32000),
            new AccountItemData("Consumable", "Diamond", 64000),
            new AccountItemData("Character", "Warrior", 1) {
                Level = 1,
                Exp = 0,
                Attributes = {
                    { "MaxHp", 500 }, { "Speed", 100 }, { "Attack", 100 }, { "Defense", 50 },
                    { "CritRate", 0.1f }, { "CritDamage", 0.5f }, { "DamageBonus", 0 }, { "ResistancePenetration", 0 }
                },
                ExtraData = new Dictionary<string, string>(),
                EquippedRelics = new Dictionary<string, string>(),
                EquippedWeapon = ""
            }
        };

        SaveDatabase();
        return true;
    }

    public bool Login(string accountId, string password)
    {
        if (string.IsNullOrEmpty(accountId) || string.IsNullOrEmpty(password))
            return false;

        var account = mDatabase.AccountList.Find(a => a.AccountId == accountId);
        if (account == null || account.Password != password)
            return false;

        mCurrentAccount = account;

        // 登录时加载抽卡记录与保底数据
        LotterySystem.Instance.LoadLotteryData(accountId);

        // 登录时加载商店购买记录
        StoreSystem.Instance.LoadPurchaseData(accountId);

        DungeonManager.Instance.Initialize(account);
        return true;
    }

    /// <summary>保存并清理当前登录账号的数据。</summary>
    public void Logout()
    {
        if (mCurrentAccount != null)
        {
            // 登出时将内存中的抽卡记录与保底数据写回账号数据库
            LotterySystem.Instance.SaveLotteryData();
            StoreSystem.Instance.SavePurchaseData();
            SaveDatabase();
        }

        LotterySystem.Instance.ClearLotteryData();
        StoreSystem.Instance.ClearPurchaseData();
        mCurrentAccount = null;
    }

    public bool AccountExists(string accountId)
    {
        return mDatabase.AccountList.Exists(account => account.AccountId == accountId);
    }

    public List<AccountItemData> GetAccountItems(string accountId)
    {
        return mDatabase.AccountInfo.TryGetValue(accountId, out var items) ? items : new List<AccountItemData>();
    }

    public AccountItemData GetAccountItem(string accountId, string itemType, string itemId)
    {
        if (!mDatabase.AccountInfo.TryGetValue(accountId, out var items))
            return null;

        return items.FirstOrDefault(item => item.ItemType == itemType && item.ItemId == itemId);
    }

    public AccountItemData GetAccountItemByInstanceId(string accountId, string itemType, string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId) || !mDatabase.AccountInfo.TryGetValue(accountId, out var items))
            return null;

        return items.FirstOrDefault(item => item.ItemType == itemType && item.InstanceId == instanceId);
    }

    public int GetAccountItemCount(string accountId, string itemType, string itemId)
    {
        var item = GetAccountItem(accountId, itemType, itemId);
        return item?.Count ?? 0;
    }

    public float GetAttribute(string accountId, string itemType, string itemId, string attributeKey)
    {
        var item = GetAccountItem(accountId, itemType, itemId);
        if (item == null) return 0f;

        return item.Attributes.TryGetValue(attributeKey, out var value) ? value : 0f;
    }

    public void AddAccountItem(string accountId, string itemType, string itemId, int count = 1)
    {
        if (!mDatabase.AccountInfo.ContainsKey(accountId))
            mDatabase.AccountInfo[accountId] = new List<AccountItemData>();

        if (itemType == "Weapon" || itemType == "Relic")
        {
            for (int i = 0; i < count; i++)
            {
                AccountItemData item = itemType == "Relic"
                    ? UpgradeSystem.Instance.CreateRelicItemWithData(itemId)
                    : new AccountItemData(itemType, itemId, 1);
                mDatabase.AccountInfo[accountId].Add(item);
            }
        }
        else
        {
            var existingItem = mDatabase.AccountInfo[accountId]
                .FirstOrDefault(item => item.ItemType == itemType && item.ItemId == itemId);

            if (existingItem != null)
                existingItem.Count += count;
            else
                mDatabase.AccountInfo[accountId].Add(new AccountItemData(itemType, itemId, count));
        }

        SaveDatabase();
    }

    public bool RemoveAccountItemByInstanceId(string accountId, string itemType, string instanceId)
    {
        if (string.IsNullOrEmpty(instanceId) || !mDatabase.AccountInfo.TryGetValue(accountId, out var items))
            return false;

        var item = items.FirstOrDefault(existing =>
            existing.ItemType == itemType && existing.InstanceId == instanceId);
        if (item == null)
            return false;

        items.Remove(item);
        SaveDatabase();
        return true;
    }

    public bool RemoveAccountItem(string accountId, string itemType, string itemId, int count = 1)
    {
        if (!mDatabase.AccountInfo.TryGetValue(accountId, out var items))
            return false;

        var existingItem = items.FirstOrDefault(item => item.ItemType == itemType && item.ItemId == itemId);
        if (existingItem == null)
            return false;

        if (existingItem.Count < count)
            return false;

        existingItem.Count -= count;

        if (existingItem.Count <= 0)
        {
            items.Remove(existingItem);
        }

        SaveDatabase();
        return true;
    }

    public void SetAttribute(string accountId, string itemType, string itemId, string attributeKey, float value)
    {
        if (!mDatabase.AccountInfo.ContainsKey(accountId))
            mDatabase.AccountInfo[accountId] = new List<AccountItemData>();

        var item = mDatabase.AccountInfo[accountId]
            .FirstOrDefault(i => i.ItemType == itemType && i.ItemId == itemId);

        if (item == null)
        {
            item = new AccountItemData(itemType, itemId);
            mDatabase.AccountInfo[accountId].Add(item);
        }

        item.Attributes[attributeKey] = value;
        SaveDatabase();
    }

    public void AddAttribute(string accountId, string itemType, string itemId, string attributeKey, float value)
    {
        var currentValue = GetAttribute(accountId, itemType, itemId, attributeKey);
        SetAttribute(accountId, itemType, itemId, attributeKey, currentValue + value);
    }

    public List<AccountItemData> GetItemsByType(string accountId, string itemType)
    {
        return GetAccountItems(accountId).Where(item => item.ItemType == itemType).ToList();
    }

    /// <summary>获取数据库中的全部账号标识。</summary>
    public List<string> GetAllAccountIds()
    {
        return mDatabase.AccountList.ConvertAll(account => account.AccountId);
    }

    #endregion
}

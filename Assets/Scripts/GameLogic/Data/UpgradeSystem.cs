using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YokiFrame;

/// <summary>
/// 养成系统：处理角色、武器、遗器的升级、装备与属性计算。
/// </summary>
public class UpgradeSystem : Singleton<UpgradeSystem>
{
    #region 角色升级与属性

    /// <summary>为角色增加经验并处理升级。</summary>
    public void AddExp(string accountId, string characterId, int expAmount)
    {
        var charItem = AccountManager.Instance.GetAccountItem(accountId, "Character", characterId);
        if (charItem == null) return;

        var config = GameDataMgr.Instance.playerCharacterConfigs.Characters
            .FirstOrDefault(c => c.CharacterId == characterId);
        if (config == null) return;

        SyncCharacterBaseStats(charItem, config);
        charItem.Exp += expAmount;

        int maxLevel = config.MaxLevel > 0 ? config.MaxLevel : 80;
        while (charItem.Level < maxLevel && charItem.Exp >= charItem.ExpToNextLevel)
        {
            charItem.Exp -= charItem.ExpToNextLevel;
            charItem.Level++;
            SyncCharacterBaseStats(charItem, config);
        }

        UpdateCharacterFinalStats(charItem);
        AccountManager.Instance.SaveDatabase();
    }

    public void CalculateCharacterBaseStats(PlayerCharacterConfig config, int level, out float maxHp, out float attack, out float defense, out float speed)
    {
        maxHp = config != null ? config.MaxHp : 0f;
        attack = config != null ? config.Attack : 0f;
        defense = config != null ? config.Defense : 0f;
        speed = config != null ? config.Speed : 0f;
        if (config == null) return;

        int maxLevel = config.MaxLevel > 0 ? config.MaxLevel : 80;
        if (maxLevel <= 1) return;

        float levelProgress = (Mathf.Clamp(level, 1, maxLevel) - 1f) / (maxLevel - 1f);
        maxHp = Mathf.Lerp(config.MaxHp, config.MaxHpAtMaxLevel > 0 ? config.MaxHpAtMaxLevel : config.MaxHp, levelProgress);
        attack = Mathf.Lerp(config.Attack, config.AttackAtMaxLevel > 0 ? config.AttackAtMaxLevel : config.Attack, levelProgress);
        defense = Mathf.Lerp(config.Defense, config.DefenseAtMaxLevel > 0 ? config.DefenseAtMaxLevel : config.Defense, levelProgress);
        speed = Mathf.Lerp(config.Speed, config.SpeedAtMaxLevel > 0 ? config.SpeedAtMaxLevel : config.Speed, levelProgress);
    }

    private void SyncCharacterBaseStats(AccountItemData charItem, PlayerCharacterConfig config)
    {
        CalculateCharacterBaseStats(config, charItem.Level, out float maxHp, out float attack, out float defense, out float speed);
        charItem.Attributes["MaxHp"] = maxHp;
        charItem.Attributes["Attack"] = attack;
        charItem.Attributes["Defense"] = defense;
        charItem.Attributes["Speed"] = speed;
    }

    public bool UpgradeCharacter(string accountId, string characterId, List<CostItem> upgradeCosts, int goldCost)
    {
        var charItem = AccountManager.Instance.GetAccountItem(accountId, "Character", characterId);
        if (charItem == null) return false;

        var config = GameDataMgr.Instance.playerCharacterConfigs.Characters
            .FirstOrDefault(c => c.CharacterId == characterId);
        if (config == null) return false;

        int maxLevel = config.MaxLevel > 0 ? config.MaxLevel : 80;
        if (charItem.Level >= maxLevel) return false;

        int gold = AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", "Gold");
        if (gold < goldCost)
            return false;

        if (upgradeCosts == null || upgradeCosts.Count == 0) return false;

        foreach (var cost in upgradeCosts)
        {
            var item = AccountManager.Instance.GetAccountItem(accountId, "Consumable", cost.ItemId);
            if (item == null || item.Count < cost.Count)
                return false;
        }

        foreach (var cost in upgradeCosts)
        {
            AccountManager.Instance.RemoveAccountItem(accountId, "Consumable", cost.ItemId, cost.Count);
        }

        AccountManager.Instance.RemoveAccountItem(accountId, "Consumable", "Gold", goldCost);

        int expToAdd = charItem.ExpToNextLevel;
        AddExp(accountId, characterId, expToAdd);

        return true;
    }

    public List<CostItem> GetCharacterUpgradeCost(string characterId)
    {
        var config = GameDataMgr.Instance.playerCharacterConfigs.Characters
            .FirstOrDefault(c => c.CharacterId == characterId);

        var charItem = AccountManager.Instance.GetItemsByType(AccountManager.Instance.CurrentAccount.AccountId, "Character")
            .FirstOrDefault(c => c.ItemId == characterId);
        int currentLevel = charItem != null ? charItem.Level : 1;
        int rarity = config?.Rarity ?? 4;

        return GetUpgradeCostsFromConfig("Character", rarity, currentLevel);
    }

    public int GetCharacterUpgradeGoldCost(string characterId)
    {
        var config = GameDataMgr.Instance.playerCharacterConfigs.Characters
            .FirstOrDefault(c => c.CharacterId == characterId);
        var charItem = AccountManager.Instance.GetItemsByType(AccountManager.Instance.CurrentAccount.AccountId, "Character")
            .FirstOrDefault(c => c.ItemId == characterId);
        int currentLevel = charItem != null ? charItem.Level : 1;
        int rarity = config?.Rarity ?? 4;

        return GetUpgradeGoldFromConfig("Character", rarity, currentLevel);
    }

    public bool EquipRelic(string accountId, string characterId, string relicInstanceId)
    {
        var charItem = AccountManager.Instance.GetAccountItem(accountId, "Character", characterId);
        var relicItem = AccountManager.Instance.GetAccountItemByInstanceId(accountId, "Relic", relicInstanceId);

        if (charItem == null || relicItem == null || relicItem.Count <= 0)
            return false;

        var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
            .FirstOrDefault(r => r.RelicId == relicItem.ItemId);
        if (relicConfig == null)
            return false;

        string slotName = relicConfig.Slot.ToString();

        if (charItem.EquippedRelics == null)
            charItem.EquippedRelics = new Dictionary<string, string>();

        if (charItem.EquippedRelics.ContainsKey(slotName))
        {
            UnequipRelic(accountId, characterId, slotName);
        }

        var accountRelicData = relicItem.ExtraData != null && relicItem.ExtraData.ContainsKey("RelicData")
            ? JsonUtility.FromJson<AccountRelicData>(relicItem.ExtraData["RelicData"])
            : GenerateRelicData(relicConfig);

        FixRelicPercentValues(accountRelicData);

        if (relicItem.ExtraData == null)
            relicItem.ExtraData = new Dictionary<string, string>();
        relicItem.ExtraData["RelicData"] = JsonUtility.ToJson(accountRelicData);

        ApplyRelicStats(charItem, accountRelicData, relicConfig);

        charItem.EquippedRelics[slotName] = relicItem.InstanceId;

        if (charItem.ExtraData == null)
            charItem.ExtraData = new Dictionary<string, string>();
        charItem.ExtraData[$"Relic_{slotName}_Data"] = JsonUtility.ToJson(accountRelicData);

        if (relicItem.ExtraData == null)
            relicItem.ExtraData = new Dictionary<string, string>();
        relicItem.ExtraData["IsEquipped"] = "true";
        relicItem.ExtraData["EquippedBy"] = characterId;
        relicItem.ExtraData["EquippedSlot"] = slotName;

        AccountManager.Instance.SaveDatabase();
        return true;
    }

    public void UnequipRelic(string accountId, string characterId, string slotName)
    {
        var charItem = AccountManager.Instance.GetAccountItem(accountId, "Character", characterId);
        if (charItem == null || charItem.EquippedRelics == null || !charItem.EquippedRelics.ContainsKey(slotName))
            return;

        string relicInstanceId = charItem.EquippedRelics[slotName];
        var relicItem = AccountManager.Instance.GetAccountItemByInstanceId(accountId, "Relic", relicInstanceId);
        if (relicItem == null)
            return;

        var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
            .FirstOrDefault(r => r.RelicId == relicItem.ItemId);
        if (relicConfig == null)
            return;

        var accountRelicData = new AccountRelicData();

        if (charItem.ExtraData != null && charItem.ExtraData.ContainsKey($"Relic_{slotName}_Data"))
        {
            accountRelicData = JsonUtility.FromJson<AccountRelicData>(charItem.ExtraData[$"Relic_{slotName}_Data"]);
        }
        else if (relicItem != null && relicItem.ExtraData != null && relicItem.ExtraData.ContainsKey("RelicData"))
        {
            accountRelicData = JsonUtility.FromJson<AccountRelicData>(relicItem.ExtraData["RelicData"]);
        }

        FixRelicPercentValues(accountRelicData);

        RemoveRelicStats(charItem, accountRelicData, relicConfig);

        if (relicItem != null && relicItem.ExtraData != null)
        {
            relicItem.ExtraData.Remove("IsEquipped");
            relicItem.ExtraData.Remove("EquippedBy");
            relicItem.ExtraData.Remove("EquippedSlot");
        }

        charItem.EquippedRelics.Remove(slotName);
        if (charItem.ExtraData != null)
            charItem.ExtraData.Remove($"Relic_{slotName}_Data");

        AccountManager.Instance.SaveDatabase();
    }

    public bool EnhanceRelic(string accountId, string relicInstanceId, int goldCost)
    {
        var relicItem = AccountManager.Instance.GetAccountItemByInstanceId(accountId, "Relic", relicInstanceId);
        if (relicItem == null)
            return false;

        int gold = AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", "Gold");
        if (gold < goldCost)
            return false;

        var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
            .FirstOrDefault(r => r.RelicId == relicItem.ItemId);
        if (relicConfig == null)
            return false;

        // 保存强化前的旧数据，用于后续移除旧属性
        string oldRelicDataJson = relicItem.ExtraData != null && relicItem.ExtraData.ContainsKey("RelicData")
            ? relicItem.ExtraData["RelicData"] : null;

        var accountRelicData = relicItem.ExtraData != null && relicItem.ExtraData.ContainsKey("RelicData")
            ? JsonUtility.FromJson<AccountRelicData>(relicItem.ExtraData["RelicData"])
            : GenerateRelicData(relicConfig);

        FixRelicPercentValues(accountRelicData);

        if (accountRelicData.Level >= 15)
            return false;

        if (GetRelicMainStatConfig(relicConfig, accountRelicData.MainStatType) == null)
            return false;

        AccountManager.Instance.RemoveAccountItem(accountId, "Consumable", "Gold", goldCost);
        accountRelicData.Level++;

        float levelMultiplier = (float)accountRelicData.Level / 15f;

        var mainStatConfig = GetRelicMainStatConfig(relicConfig, accountRelicData.MainStatType);
        if (mainStatConfig == null)
            return false;

        float mainStatMin = mainStatConfig.MinValue;
        float mainStatMax = mainStatConfig.MaxValue;
        if (mainStatConfig.IsPercent)
        {
            mainStatMin /= 100f;
            mainStatMax /= 100f;
        }

        accountRelicData.IsPercent = mainStatConfig.IsPercent;
        accountRelicData.MainStatValue = mainStatMin + (mainStatMax - mainStatMin) * levelMultiplier;

        if (accountRelicData.Level % 3 == 0)
        {
            if (accountRelicData.SubStats.Count < 3)
            {
                AddNewSubStat(accountRelicData, relicConfig);
            }
            else
            {
                EnhanceRandomSubStat(accountRelicData, relicConfig);
            }
        }

        if (relicItem.ExtraData == null)
            relicItem.ExtraData = new Dictionary<string, string>();
        relicItem.ExtraData["RelicData"] = JsonUtility.ToJson(accountRelicData);
        relicItem.Level = accountRelicData.Level;

        // 如果遗器已装备，需要重新计算角色身上的遗器属性加成
        if (relicItem.ExtraData.ContainsKey("EquippedBy") && relicItem.ExtraData.ContainsKey("EquippedSlot"))
        {
            string equippedBy = relicItem.ExtraData["EquippedBy"];
            string equippedSlot = relicItem.ExtraData["EquippedSlot"];
            var charItem = AccountManager.Instance.GetAccountItem(accountId, "Character", equippedBy);
            if (charItem != null)
            {
                // 移除旧属性
                if (!string.IsNullOrEmpty(oldRelicDataJson))
                {
                    var oldRelicData = JsonUtility.FromJson<AccountRelicData>(oldRelicDataJson);
                    FixRelicPercentValues(oldRelicData);
                    RemoveRelicStats(charItem, oldRelicData, relicConfig);
                }

                // 应用新属性
                ApplyRelicStats(charItem, accountRelicData, relicConfig);
            }
        }

        AccountManager.Instance.SaveDatabase();
        return true;
    }

    private void AddNewSubStat(AccountRelicData accountRelicData, RelicConfig relicConfig)
    {
        var availableSubStats = relicConfig.PossibleSubStats
            .Where(stat => stat.StatType != accountRelicData.MainStatType &&
                           !accountRelicData.SubStats.Any(existing => existing.StatType == stat.StatType))
            .ToList();

        if (availableSubStats.Count == 0)
            return;

        AddSubStat(accountRelicData, availableSubStats[Random.Range(0, availableSubStats.Count)]);
    }

    private void EnhanceRandomSubStat(AccountRelicData accountRelicData, RelicConfig relicConfig)
    {
        if (accountRelicData.SubStats.Count == 0)
            return;

        var subStat = accountRelicData.SubStats[Random.Range(0, accountRelicData.SubStats.Count)];
        subStat.EnhanceLevel++;
        subStat.Value *= 1 + 0.1f * subStat.EnhanceLevel;
    }

    public RelicMainStatConfig GetRelicMainStatConfig(RelicConfig config, RelicMainStatType type)
    {
        var possibleMainStat = config.PossibleMainStats?.FirstOrDefault(stat => stat.StatType == type);
        return possibleMainStat ?? (config.MainStat != null && config.MainStat.StatType == type ? config.MainStat : null);
    }

    public AccountRelicData GenerateRelicData(RelicConfig config)
    {
        var mainStatConfig = config.PossibleMainStats != null && config.PossibleMainStats.Count > 0
            ? config.PossibleMainStats[Random.Range(0, config.PossibleMainStats.Count)]
            : config.MainStat;
        float mainStatValue = mainStatConfig.IsPercent ? mainStatConfig.MinValue / 100f : mainStatConfig.MinValue;

        var data = new AccountRelicData
        {
            RelicId = config.RelicId,
            Level = 1,
            MainStatType = mainStatConfig.StatType,
            IsPercent = mainStatConfig.IsPercent,
            MainStatValue = mainStatValue
        };

        var availableSubStats = config.PossibleSubStats
            .Where(stat => stat.StatType != data.MainStatType)
            .ToList();

        for (int i = 0; i < config.SubStatCount && availableSubStats.Count > 0; i++)
        {
            int randomIndex = Random.Range(0, availableSubStats.Count);
            AddSubStat(data, availableSubStats[randomIndex]);
            availableSubStats.RemoveAt(randomIndex);
        }

        return data;
    }

    private void AddSubStat(AccountRelicData relicData, RelicSubStatConfig config)
    {
        float value = Random.Range(config.MinValue, config.MaxValue);
        if (config.IsPercent)
            value /= 100f;

        relicData.SubStats.Add(new RelicSubStatData
        {
            StatType = config.StatType,
            Value = value,
            IsPercent = config.IsPercent,
            EnhanceLevel = 0
        });
    }

    public AccountItemData CreateRelicItemWithData(string relicId)
    {
        var relicItem = new AccountItemData("Relic", relicId, 1) { Level = 1 };

        var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
            .FirstOrDefault(r => r.RelicId == relicId);

        if (relicConfig != null)
        {
            var relicData = GenerateRelicData(relicConfig);

            relicItem.ExtraData = new Dictionary<string, string>();
            relicItem.ExtraData["RelicData"] = JsonUtility.ToJson(relicData);
        }

        return relicItem;
    }

    private RelicMainStatType GetOpposingStatType(RelicMainStatType statType)
    {
        switch (statType)
        {
            case RelicMainStatType.MaxHp: return RelicMainStatType.MaxHpPercent;
            case RelicMainStatType.MaxHpPercent: return RelicMainStatType.MaxHp;
            case RelicMainStatType.Attack: return RelicMainStatType.AttackPercent;
            case RelicMainStatType.AttackPercent: return RelicMainStatType.Attack;
            case RelicMainStatType.DefensePercent: return RelicMainStatType.MaxHp;
            default: return statType;
        }
    }

    private float GetSubStatRange(RelicMainStatType statType)
    {
        switch (statType)
        {
            case RelicMainStatType.MaxHp: return 50f;
            case RelicMainStatType.Attack: return 15f;
            case RelicMainStatType.Speed: return 10f;
            case RelicMainStatType.CritRate: return 0.05f;
            case RelicMainStatType.CritDamage: return 0.10f;
            case RelicMainStatType.MaxHpPercent: return 0.05f;
            case RelicMainStatType.AttackPercent: return 0.05f;
            case RelicMainStatType.DefensePercent: return 0.05f;
            default: return 10f;
        }
    }

    private bool IsPercentStat(RelicMainStatType statType)
    {
        return statType == RelicMainStatType.MaxHpPercent ||
               statType == RelicMainStatType.AttackPercent ||
               statType == RelicMainStatType.DefensePercent ||
               statType == RelicMainStatType.CritRate ||
               statType == RelicMainStatType.CritDamage;
    }

    private void FixRelicPercentValues(AccountRelicData relicData)
    {
        if (IsPercentStat(relicData.MainStatType) && relicData.MainStatValue > 1f)
        {
            relicData.MainStatValue = relicData.MainStatValue / 100f;
        }

        foreach (var subStat in relicData.SubStats)
        {
            if (IsPercentStat(subStat.StatType) && subStat.Value > 1f)
            {
                subStat.Value = subStat.Value / 100f;
            }
        }
    }

    private void ApplyRelicStats(AccountItemData charItem, AccountRelicData relicData, RelicConfig config)
    {
        AddRelicStat(charItem, relicData.MainStatType, relicData.MainStatValue, relicData.IsPercent);

        foreach (var subStat in relicData.SubStats)
        {
            AddRelicStat(charItem, subStat.StatType, subStat.Value, subStat.IsPercent);
        }

        if (charItem.ExtraData == null)
            charItem.ExtraData = new Dictionary<string, string>();
        charItem.ExtraData[$"Relic_{config.Slot}_Data"] = JsonUtility.ToJson(relicData);

        UpdateCharacterFinalStats(charItem);
    }

    private void RemoveRelicStats(AccountItemData charItem, AccountRelicData relicData, RelicConfig config)
    {
        RemoveRelicStat(charItem, relicData.MainStatType, relicData.MainStatValue, relicData.IsPercent);

        foreach (var subStat in relicData.SubStats)
        {
            RemoveRelicStat(charItem, subStat.StatType, subStat.Value, subStat.IsPercent);
        }

        UpdateCharacterFinalStats(charItem);
    }

    private void AddRelicStat(AccountItemData charItem, RelicMainStatType statType, float value, bool isPercent)
    {
        string statKey = $"Relic_{statType}";

        if (charItem.Attributes.ContainsKey(statKey))
            charItem.Attributes[statKey] += value;
        else
            charItem.Attributes[statKey] = value;
    }

    private void RemoveRelicStat(AccountItemData charItem, RelicMainStatType statType, float value, bool isPercent)
    {
        string statKey = $"Relic_{statType}";

        if (charItem.Attributes.ContainsKey(statKey))
            charItem.Attributes[statKey] -= value;
    }

    public void UpdateCharacterFinalStats(AccountItemData charItem)
    {
        var characterConfig = GameDataMgr.Instance.playerCharacterConfigs.Characters
            .FirstOrDefault(c => c.CharacterId == charItem.ItemId);

        if (characterConfig == null) return;

        CalculateCharacterBaseStats(characterConfig, charItem.Level, out float calculatedMaxHp, out float calculatedAttack, out float calculatedDefense, out float calculatedSpeed);
        float baseMaxHp = calculatedMaxHp;
        float baseAttack = calculatedAttack;
        float baseDefense = calculatedDefense;
        float baseSpeed = calculatedSpeed;
        float baseCritRate = characterConfig.CritRate;
        float baseCritDamage = characterConfig.CritDamage;

        float weaponMaxHp = charItem.Attributes.ContainsKey("Weapon_MaxHp") ? charItem.Attributes["Weapon_MaxHp"] : 0f;
        float weaponAttack = charItem.Attributes.ContainsKey("Weapon_Attack") ? charItem.Attributes["Weapon_Attack"] : 0f;
        float weaponDefense = charItem.Attributes.ContainsKey("Weapon_Defense") ? charItem.Attributes["Weapon_Defense"] : 0f;

        baseMaxHp += weaponMaxHp;
        baseAttack += weaponAttack;
        baseDefense += weaponDefense;

        float relicMaxHpFlat = charItem.Attributes.ContainsKey("Relic_MaxHp") ? charItem.Attributes["Relic_MaxHp"] : 0f;
        float relicMaxHpPercent = charItem.Attributes.ContainsKey("Relic_MaxHpPercent") ? charItem.Attributes["Relic_MaxHpPercent"] : 0f;
        float relicAttackFlat = charItem.Attributes.ContainsKey("Relic_Attack") ? charItem.Attributes["Relic_Attack"] : 0f;
        float relicAttackPercent = charItem.Attributes.ContainsKey("Relic_AttackPercent") ? charItem.Attributes["Relic_AttackPercent"] : 0f;
        float relicDefensePercent = charItem.Attributes.ContainsKey("Relic_DefensePercent") ? charItem.Attributes["Relic_DefensePercent"] : 0f;
        float relicSpeed = charItem.Attributes.ContainsKey("Relic_Speed") ? charItem.Attributes["Relic_Speed"] : 0f;
        float relicCritRate = charItem.Attributes.ContainsKey("Relic_CritRate") ? charItem.Attributes["Relic_CritRate"] : 0f;
        float relicCritDamage = charItem.Attributes.ContainsKey("Relic_CritDamage") ? charItem.Attributes["Relic_CritDamage"] : 0f;

        charItem.Attributes["FinalMaxHp"] = baseMaxHp + Mathf.RoundToInt(baseMaxHp * relicMaxHpPercent) + relicMaxHpFlat;
        charItem.Attributes["FinalAttack"] = baseAttack + Mathf.RoundToInt(baseAttack * relicAttackPercent) + relicAttackFlat;
        charItem.Attributes["FinalDefense"] = baseDefense + Mathf.RoundToInt(baseDefense * relicDefensePercent);
        charItem.Attributes["FinalSpeed"] = baseSpeed + relicSpeed;

        float finalCritRate = baseCritRate + relicCritRate;
        float finalCritDamage = baseCritDamage + relicCritDamage;

        charItem.Attributes["FinalCritRate"] = finalCritRate;
        charItem.Attributes["FinalCritDamage"] = finalCritDamage;

        Dictionary<string, float> tempStats = new Dictionary<string, float>
        {
            ["Attack"] = baseAttack + Mathf.RoundToInt(baseAttack * relicAttackPercent) + relicAttackFlat,
            ["CritRate"] = finalCritRate,
            ["CritDamage"] = finalCritDamage,
            ["DamageBonus"] = charItem.Attributes.ContainsKey("DamageBonus") ? charItem.Attributes["DamageBonus"] : 0f
        };

        ApplyStarBonuses(tempStats, charItem, characterConfig);
        charItem.Attributes["FinalDamageBonus"] = tempStats.ContainsKey("DamageBonus") ? tempStats["DamageBonus"] : 0f;
    }

    public void UpdateCharacterFinalStats(string accountId, string characterId)
    {
        var charItem = AccountManager.Instance.GetAccountItem(accountId, "Character", characterId);
        if (charItem != null)
        {
            UpdateCharacterFinalStats(charItem);
            AccountManager.Instance.SaveDatabase();
        }
    }

    /// <summary>校验适配职业后为角色装备武器实例。</summary>
    public bool EquipWeapon(string accountId, string characterId, string weaponInstanceId)
    {
        var charItem = AccountManager.Instance.GetAccountItem(accountId, "Character", characterId);
        var weaponItem = AccountManager.Instance.GetAccountItemByInstanceId(accountId, "Weapon", weaponInstanceId);

        if (charItem == null || weaponItem == null || weaponItem.Count <= 0)
            return false;

        var weaponConfig = GameDataMgr.Instance.lightConeConfigs.Weapons
            .FirstOrDefault(w => w.WeaponId == weaponItem.ItemId);
        if (weaponConfig == null)
            return false;

        CharacterClass charClass = GetCharacterClass(characterId);
        if (!weaponConfig.CompatibleClasses.Contains(charClass))
            return false;

        if (!string.IsNullOrEmpty(charItem.EquippedWeapon))
        {
            UnequipWeapon(accountId, characterId);
        }

        ApplyWeaponStats(charItem, weaponConfig, weaponItem.Level);

        charItem.EquippedWeapon = weaponItem.InstanceId;

        if (weaponItem.ExtraData == null)
            weaponItem.ExtraData = new Dictionary<string, string>();
        weaponItem.ExtraData["IsEquipped"] = "true";
        weaponItem.ExtraData["EquippedBy"] = characterId;

        UpdateCharacterFinalStats(charItem);

        AccountManager.Instance.SaveDatabase();
        return true;
    }

    public void UnequipWeapon(string accountId, string characterId)
    {
        var charItem = AccountManager.Instance.GetAccountItem(accountId, "Character", characterId);
        if (charItem == null || string.IsNullOrEmpty(charItem.EquippedWeapon))
            return;

        string weaponInstanceId = charItem.EquippedWeapon;
        var weaponItem = AccountManager.Instance.GetAccountItemByInstanceId(accountId, "Weapon", weaponInstanceId);

        var weaponConfig = weaponItem == null ? null : GameDataMgr.Instance.lightConeConfigs.Weapons
            .FirstOrDefault(w => w.WeaponId == weaponItem.ItemId);
        if (weaponConfig != null)
        {
            RemoveWeaponStats(charItem, weaponConfig);
        }
        if (weaponItem != null && weaponItem.ExtraData != null)
        {
            weaponItem.ExtraData.Remove("IsEquipped");
            weaponItem.ExtraData.Remove("EquippedBy");
        }

        charItem.EquippedWeapon = "";

        UpdateCharacterFinalStats(charItem);

        AccountManager.Instance.SaveDatabase();
    }

    public void CalculateWeaponBaseStats(LightConeConfig config, int level, out float maxHp, out float attack, out float defense)
    {
        maxHp = config != null ? config.BaseMaxHp : 0f;
        attack = config != null ? config.BaseAttack : 0f;
        defense = config != null ? config.BaseDefense : 0f;
        if (config == null) return;

        int maxLevel = config.MaxLevel > 1 ? config.MaxLevel : 80;
        float levelProgress = (Mathf.Clamp(level, 1, maxLevel) - 1f) / (maxLevel - 1f);
        maxHp = Mathf.Lerp(config.BaseMaxHp, config.BaseMaxHpAtMaxLevel > 0 || config.BaseMaxHp == 0 ? config.BaseMaxHpAtMaxLevel : config.BaseMaxHp, levelProgress);
        attack = Mathf.Lerp(config.BaseAttack, config.BaseAttackAtMaxLevel > 0 || config.BaseAttack == 0 ? config.BaseAttackAtMaxLevel : config.BaseAttack, levelProgress);
        defense = Mathf.Lerp(config.BaseDefense, config.BaseDefenseAtMaxLevel > 0 || config.BaseDefense == 0 ? config.BaseDefenseAtMaxLevel : config.BaseDefense, levelProgress);
    }

    private void ApplyWeaponStats(AccountItemData charItem, LightConeConfig config, int weaponLevel)
    {
        CalculateWeaponBaseStats(config, weaponLevel, out float maxHp, out float attack, out float defense);
        charItem.Attributes["Weapon_Attack"] = attack;
        charItem.Attributes["Weapon_Defense"] = defense;
        charItem.Attributes["Weapon_MaxHp"] = maxHp;

        foreach (var effect in config.Effects)
        {
            string effectKey = $"WeaponEffect_{effect.EffectId}";
            charItem.Attributes[effectKey] = effect.Value;
        }
    }

    private void RemoveWeaponStats(AccountItemData charItem, LightConeConfig config)
    {
        charItem.Attributes.Remove("Weapon_Attack");
        charItem.Attributes.Remove("Weapon_Defense");
        charItem.Attributes.Remove("Weapon_MaxHp");

        foreach (var effect in config.Effects)
        {
            string effectKey = $"WeaponEffect_{effect.EffectId}";
            charItem.Attributes.Remove(effectKey);
        }
    }

    private CharacterClass GetCharacterClass(string characterId)
    {
        switch (characterId)
        {
            case "Warrior": return CharacterClass.Warrior;
            case "Mage": return CharacterClass.Mage;
            case "Archer": return CharacterClass.Archer;
            case "Healer": return CharacterClass.Healer;
            default: return CharacterClass.Warrior;
        }
    }

    public Dictionary<string, float> CalculateFinalStats(string accountId, string characterId)
    {
        var charItem = AccountManager.Instance.GetAccountItem(accountId, "Character", characterId);
        if (charItem == null)
            return new Dictionary<string, float>();

        var finalStats = new Dictionary<string, float>();

        var config = GameDataMgr.Instance.playerCharacterConfigs.Characters
            .FirstOrDefault(c => c.CharacterId == characterId);

        CalculateCharacterBaseStats(config, charItem.Level, out float calculatedHp, out float calculatedAttack, out float calculatedDefense, out float calculatedSpeed);
        float baseHp = config != null ? calculatedHp : 100f;
        float baseAttack = config != null ? calculatedAttack : 10f;
        float baseDefense = config != null ? calculatedDefense : 0f;
        float baseSpeed = config != null ? calculatedSpeed : 100f;
        float baseCritRate = config != null ? config.CritRate : 0.1f;
        float baseCritDamage = config != null ? config.CritDamage : 0.5f;

        float weaponMaxHp = charItem.Attributes.ContainsKey("Weapon_MaxHp") ? charItem.Attributes["Weapon_MaxHp"] : 0f;
        float weaponAttack = charItem.Attributes.ContainsKey("Weapon_Attack") ? charItem.Attributes["Weapon_Attack"] : 0f;
        float weaponDefense = charItem.Attributes.ContainsKey("Weapon_Defense") ? charItem.Attributes["Weapon_Defense"] : 0f;

        baseHp += weaponMaxHp;
        baseAttack += weaponAttack;
        baseDefense += weaponDefense;

        float relicMaxHpFlat = charItem.Attributes.ContainsKey("Relic_MaxHp") ? charItem.Attributes["Relic_MaxHp"] : 0f;
        float relicMaxHpPercent = charItem.Attributes.ContainsKey("Relic_MaxHpPercent") ? charItem.Attributes["Relic_MaxHpPercent"] : 0f;
        float relicAttackFlat = charItem.Attributes.ContainsKey("Relic_Attack") ? charItem.Attributes["Relic_Attack"] : 0f;
        float relicAttackPercent = charItem.Attributes.ContainsKey("Relic_AttackPercent") ? charItem.Attributes["Relic_AttackPercent"] : 0f;
        float relicDefensePercent = charItem.Attributes.ContainsKey("Relic_DefensePercent") ? charItem.Attributes["Relic_DefensePercent"] : 0f;
        float relicSpeed = charItem.Attributes.ContainsKey("Relic_Speed") ? charItem.Attributes["Relic_Speed"] : 0f;
        float relicCritRate = charItem.Attributes.ContainsKey("Relic_CritRate") ? charItem.Attributes["Relic_CritRate"] : 0f;
        float relicCritDamage = charItem.Attributes.ContainsKey("Relic_CritDamage") ? charItem.Attributes["Relic_CritDamage"] : 0f;

        finalStats["MaxHp"] = baseHp + Mathf.RoundToInt(baseHp * relicMaxHpPercent) + relicMaxHpFlat;
        finalStats["Attack"] = baseAttack + Mathf.RoundToInt(baseAttack * relicAttackPercent) + relicAttackFlat;
        finalStats["Defense"] = baseDefense + Mathf.RoundToInt(baseDefense * relicDefensePercent);
        finalStats["Speed"] = baseSpeed + relicSpeed;

        finalStats["CritRate"] = baseCritRate + relicCritRate;
        finalStats["CritDamage"] = baseCritDamage + relicCritDamage;

        float damageBonus = charItem.Attributes.ContainsKey("DamageBonus") ? charItem.Attributes["DamageBonus"] : 0f;
        finalStats["DamageBonus"] = damageBonus;

        if (charItem.Attributes.ContainsKey("ResistancePenetration"))
            finalStats["ResistancePenetration"] = charItem.Attributes["ResistancePenetration"];

        ApplyStarBonuses(finalStats, charItem, config);

        return finalStats;
    }

    private void ApplyStarBonuses(Dictionary<string, float> finalStats, AccountItemData charItem, PlayerCharacterConfig config)
    {
        if (config == null || config.StarEffects == null)
            return;

        int starLevel = charItem.StarLevel;
        if (starLevel <= 0)
            return;

        foreach (var starEffect in config.StarEffects)
        {
            if (starEffect.StarLevel <= starLevel)
            {
                ApplyStarEffect(finalStats, starEffect);
            }
        }
    }

    private void ApplyStarEffect(Dictionary<string, float> finalStats, StarEffectConfig effect)
    {
        switch (effect.EffectType)
        {
            case "AttackBonus":
                finalStats["Attack"] = finalStats.ContainsKey("Attack") ? finalStats["Attack"] * (1 + effect.Value) : effect.Value * 100;
                break;
            case "DefenseBonus":
                finalStats["Defense"] = finalStats.ContainsKey("Defense") ? finalStats["Defense"] * (1 + effect.Value) : effect.Value * 100;
                break;
            case "MaxHpBonus":
                finalStats["MaxHp"] = finalStats.ContainsKey("MaxHp") ? finalStats["MaxHp"] * (1 + effect.Value) : effect.Value * 100;
                break;
            case "CritRateBonus":
                finalStats["CritRate"] = finalStats.ContainsKey("CritRate") ? finalStats["CritRate"] + effect.Value : effect.Value;
                break;
            case "CritDamageBonus":
                finalStats["CritDamage"] = finalStats.ContainsKey("CritDamage") ? finalStats["CritDamage"] + effect.Value : effect.Value;
                break;
            case "SpeedBonus":
                finalStats["Speed"] = finalStats.ContainsKey("Speed") ? finalStats["Speed"] * (1 + effect.Value) : effect.Value * 100;
                break;
            case "SkillDamageBonus":
                finalStats["DamageBonus"] = finalStats.ContainsKey("DamageBonus") ? finalStats["DamageBonus"] + effect.Value : effect.Value;
                break;
            case "UltimateBonus":
                finalStats["UltimateBonus"] = finalStats.ContainsKey("UltimateBonus") ? finalStats["UltimateBonus"] + effect.Value : effect.Value;
                break;
            case "HealBonus":
                finalStats["HealBonus"] = finalStats.ContainsKey("HealBonus") ? finalStats["HealBonus"] + effect.Value : effect.Value;
                break;
        }
    }

    public bool UnlockStar(string accountId, string characterId, int starLevel)
    {
        var charItem = AccountManager.Instance.GetAccountItem(accountId, "Character", characterId);
        if (charItem == null)
            return false;

        var config = GameDataMgr.Instance.playerCharacterConfigs.Characters
            .FirstOrDefault(c => c.CharacterId == characterId);
        if (config == null)
            return false;

        var starEffect = config.StarEffects.FirstOrDefault(s => s.StarLevel == starLevel);
        if (starEffect == null)
            return false;

        if (starLevel > charItem.StarLevel + 1)
            return false;

        if (starLevel <= charItem.StarLevel)
            return false;

        if (!ConsumeCostItems(accountId, starEffect.UnlockCost))
            return false;

        charItem.StarLevel = starLevel;
        UpdateCharacterFinalStats(accountId, characterId);
        AccountManager.Instance.SaveDatabase();

        LogKit.Log($"[UpgradeSystem] 角色 {characterId} 解锁星魂 {starLevel}");
        return true;
    }

    public bool UpgradeTalent(string accountId, string characterId, string skillType)
    {
        var charItem = AccountManager.Instance.GetAccountItem(accountId, "Character", characterId);
        if (charItem == null)
            return false;

        var config = GameDataMgr.Instance.playerCharacterConfigs.Characters
            .FirstOrDefault(c => c.CharacterId == characterId);
        if (config == null)
            return false;

        var talentConfig = config.TalentEffects.FirstOrDefault(t => t.SkillType == skillType);
        if (talentConfig == null)
            return false;

        int currentLevel = charItem.GetTalentLevel(skillType);
        if (currentLevel >= 10)
            return false;

        if (currentLevel >= talentConfig.Effects.Count)
            return false;

        var nextLevelEffect = talentConfig.Effects.FirstOrDefault(e => e.Level == currentLevel + 1);
        if (nextLevelEffect == null)
            return false;

        int goldCost = GetTalentUpgradeGoldCost(currentLevel);
        int gold = AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", "Gold");
        if (gold < goldCost)
            return false;

        if (!ConsumeCostItems(accountId, nextLevelEffect.UpgradeCost))
            return false;

        AccountManager.Instance.RemoveAccountItem(accountId, "Consumable", "Gold", goldCost);

        charItem.SetTalentLevel(skillType, currentLevel + 1);
        UpdateCharacterFinalStats(accountId, characterId);
        AccountManager.Instance.SaveDatabase();

        LogKit.Log($"[UpgradeSystem] 角色 {characterId} 升级 {skillType} 天赋到等级 {currentLevel + 1}");
        return true;
    }

    public int GetTalentUpgradeGoldCost(int currentLevel)
    {
        return Mathf.RoundToInt(200 * (1 + currentLevel * 0.06f));
    }

    public bool CanUnlockStar(string accountId, string characterId, int starLevel)
    {
        var charItem = AccountManager.Instance.GetAccountItem(accountId, "Character", characterId);
        if (charItem == null)
            return false;

        var config = GameDataMgr.Instance.playerCharacterConfigs.Characters
            .FirstOrDefault(c => c.CharacterId == characterId);
        if (config == null)
            return false;

        var starEffect = config.StarEffects.FirstOrDefault(s => s.StarLevel == starLevel);
        if (starEffect == null)
            return false;

        if (starLevel != charItem.StarLevel + 1)
            return false;

        return HasEnoughItems(accountId, starEffect.UnlockCost);
    }

    public bool CanUpgradeTalent(string accountId, string characterId, string skillType)
    {
        var charItem = AccountManager.Instance.GetAccountItem(accountId, "Character", characterId);
        if (charItem == null)
            return false;

        var config = GameDataMgr.Instance.playerCharacterConfigs.Characters
            .FirstOrDefault(c => c.CharacterId == characterId);
        if (config == null)
            return false;

        var talentConfig = config.TalentEffects.FirstOrDefault(t => t.SkillType == skillType);
        if (talentConfig == null)
            return false;

        int currentLevel = charItem.GetTalentLevel(skillType);
        if (currentLevel >= 10)
            return false;

        if (currentLevel >= talentConfig.Effects.Count)
            return false;

        var nextLevelEffect = talentConfig.Effects.FirstOrDefault(e => e.Level == currentLevel + 1);
        if (nextLevelEffect == null)
            return false;

        int goldCost = GetTalentUpgradeGoldCost(currentLevel);
        int gold = AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", "Gold");
        if (gold < goldCost)
            return false;

        return HasEnoughItems(accountId, nextLevelEffect.UpgradeCost);
    }

    private bool ConsumeCostItems(string accountId, List<CostItem> costs)
    {
        if (costs == null || costs.Count == 0)
            return true;

        if (!HasEnoughItems(accountId, costs))
            return false;

        var db = AccountManager.Instance.Database;
        foreach (var cost in costs)
        {
            var item = AccountManager.Instance.GetAccountItem(accountId, "Consumable", cost.ItemId);
            if (item != null)
            {
                item.Count -= cost.Count;
                if (item.Count <= 0)
                {
                    db.AccountInfo[accountId].Remove(item);
                }
            }
        }
        AccountManager.Instance.SaveDatabase();
        return true;
    }

    private bool HasEnoughItems(string accountId, List<CostItem> costs)
    {
        if (costs == null || costs.Count == 0)
            return true;

        foreach (var cost in costs)
        {
            var item = AccountManager.Instance.GetAccountItem(accountId, "Consumable", cost.ItemId);
            if (item == null || item.Count < cost.Count)
                return false;
        }
        return true;
    }

    public List<CostItem> GetStarUnlockCost(string characterId, int starLevel)
    {
        var config = GameDataMgr.Instance.playerCharacterConfigs.Characters
            .FirstOrDefault(c => c.CharacterId == characterId);
        if (config == null)
            return new List<CostItem>();

        var starEffect = config.StarEffects.FirstOrDefault(s => s.StarLevel == starLevel);
        return starEffect?.UnlockCost ?? new List<CostItem>();
    }

    public List<CostItem> GetTalentUpgradeCost(string characterId, string skillType, int currentLevel)
    {
        var config = GameDataMgr.Instance.playerCharacterConfigs.Characters
            .FirstOrDefault(c => c.CharacterId == characterId);
        if (config == null)
            return new List<CostItem>();

        var talentConfig = config.TalentEffects.FirstOrDefault(t => t.SkillType == skillType);
        if (talentConfig == null)
            return new List<CostItem>();

        var nextLevelEffect = talentConfig.Effects.FirstOrDefault(e => e.Level == currentLevel + 1);
        return nextLevelEffect?.UpgradeCost ?? new List<CostItem>();
    }

    public bool UpgradeWeapon(string accountId, string weaponInstanceId, List<CostItem> upgradeCosts, int goldCost)
    {
        var weaponItem = AccountManager.Instance.GetAccountItemByInstanceId(accountId, "Weapon", weaponInstanceId);
        if (weaponItem == null)
            return false;

        var weaponConfig = GameDataMgr.Instance.lightConeConfigs.Weapons
            .FirstOrDefault(w => w.WeaponId == weaponItem.ItemId);
        if (weaponConfig == null)
            return false;

        int maxLevel = weaponConfig.MaxLevel > 1 ? weaponConfig.MaxLevel : 80;
        if (weaponItem.Level >= maxLevel)
            return false;

        int gold = AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", "Gold");
        if (gold < goldCost)
            return false;

        if (!ConsumeCostItems(accountId, upgradeCosts))
            return false;

        AccountManager.Instance.RemoveAccountItem(accountId, "Consumable", "Gold", goldCost);

        weaponItem.Level++;

        // 如果武器已装备，需要重新计算角色身上的武器属性加成
        if (weaponItem.ExtraData != null && weaponItem.ExtraData.ContainsKey("EquippedBy"))
        {
            string equippedBy = weaponItem.ExtraData["EquippedBy"];
            var charItem = AccountManager.Instance.GetAccountItem(accountId, "Character", equippedBy);
            if (charItem != null)
            {
                RemoveWeaponStats(charItem, weaponConfig);
                ApplyWeaponStats(charItem, weaponConfig, weaponItem.Level);
                UpdateCharacterFinalStats(charItem);
            }
        }

        AccountManager.Instance.SaveDatabase();

        LogKit.Log($"[UpgradeSystem] 武器 {weaponItem.ItemId} ({weaponItem.InstanceId}) 升级到等级 {weaponItem.Level}");
        return true;
    }

    public List<CostItem> GetWeaponUpgradeCost(string weaponInstanceId)
    {
        var weaponItem = AccountManager.Instance.GetAccountItemByInstanceId(
            AccountManager.Instance.CurrentAccount.AccountId, "Weapon", weaponInstanceId);
        var weaponConfig = GameDataMgr.Instance.lightConeConfigs.Weapons
            .FirstOrDefault(w => weaponItem != null && w.WeaponId == weaponItem.ItemId);
        int currentLevel = weaponItem != null ? weaponItem.Level : 1;
        int rarity = weaponConfig?.Rarity ?? 3;

        return GetUpgradeCostsFromConfig("Weapon", rarity, currentLevel);
    }

    public int GetWeaponUpgradeGoldCost(string weaponInstanceId)
    {
        var weaponItem = AccountManager.Instance.GetAccountItemByInstanceId(
            AccountManager.Instance.CurrentAccount.AccountId, "Weapon", weaponInstanceId);
        var weaponConfig = GameDataMgr.Instance.lightConeConfigs.Weapons
            .FirstOrDefault(w => weaponItem != null && w.WeaponId == weaponItem.ItemId);
        int currentLevel = weaponItem != null ? weaponItem.Level : 1;
        int rarity = weaponConfig?.Rarity ?? 3;

        return GetUpgradeGoldFromConfig("Weapon", rarity, currentLevel);
    }

    public List<CostItem> GetRelicUpgradeCost(string relicInstanceId)
    {
        var relicItem = AccountManager.Instance.GetAccountItemByInstanceId(
            AccountManager.Instance.CurrentAccount.AccountId, "Relic", relicInstanceId);
        var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
            .FirstOrDefault(r => relicItem != null && r.RelicId == relicItem.ItemId);
        int currentLevel = 1;

        if (relicItem != null && relicItem.ExtraData != null && relicItem.ExtraData.ContainsKey("RelicData"))
        {
            var relicData = JsonUtility.FromJson<AccountRelicData>(relicItem.ExtraData["RelicData"]);
            currentLevel = relicData != null ? relicData.Level : 1;
        }

        int rarity = relicConfig?.Rarity ?? 3;
        return GetUpgradeCostsFromConfig("Relic", rarity, currentLevel);
    }

    public int GetRelicUpgradeGoldCost(string relicInstanceId)
    {
        var relicItem = AccountManager.Instance.GetAccountItemByInstanceId(
            AccountManager.Instance.CurrentAccount.AccountId, "Relic", relicInstanceId);
        var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
            .FirstOrDefault(r => relicItem != null && r.RelicId == relicItem.ItemId);
        int currentLevel = 1;

        if (relicItem != null && relicItem.ExtraData != null && relicItem.ExtraData.ContainsKey("RelicData"))
        {
            var relicData = JsonUtility.FromJson<AccountRelicData>(relicItem.ExtraData["RelicData"]);
            currentLevel = relicData != null ? relicData.Level : 1;
        }

        int rarity = relicConfig?.Rarity ?? 3;
        return GetUpgradeGoldFromConfig("Relic", rarity, currentLevel);
    }

    private List<CostItem> GetUpgradeCostsFromConfig(string itemType, int rarity, int currentLevel)
    {
        var result = new List<CostItem>();

        var upgradeConfig = GameDataMgr.Instance.upgradeConfigs?.Upgrades
            ?.FirstOrDefault(c =>
                c.ItemType == itemType &&
                rarity >= c.MinRarity && rarity <= c.MaxRarity &&
                currentLevel >= c.MinLevel && currentLevel <= c.MaxLevel);

        if (upgradeConfig == null || upgradeConfig.Materials == null)
            return result;

        foreach (var material in upgradeConfig.Materials)
        {
            int count = Mathf.RoundToInt(material.BaseCount + material.CountPerLevel * currentLevel);
            if (count > 0)
            {
                result.Add(new CostItem { ItemId = material.ItemId, Count = count });
            }
        }

        return result;
    }

    private int GetUpgradeGoldFromConfig(string itemType, int rarity, int currentLevel)
    {
        var upgradeConfig = GameDataMgr.Instance.upgradeConfigs?.Upgrades
            ?.FirstOrDefault(c =>
                c.ItemType == itemType &&
                rarity >= c.MinRarity && rarity <= c.MaxRarity &&
                currentLevel >= c.MinLevel && currentLevel <= c.MaxLevel);

        if (upgradeConfig == null)
            return 0;

        return Mathf.RoundToInt(upgradeConfig.GoldBase + upgradeConfig.GoldPerLevel * currentLevel);
    }

    #endregion
}

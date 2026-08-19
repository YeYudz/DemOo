using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YokiFrame;
using GameUI;
using static GameUI.UIEvents;

/// <summary>
/// 备战系统：接收BattleStartEvent，负责构建战斗单位数据并初始化战斗系统
/// </summary>
public class BattlePreparationSystem : Singleton<BattlePreparationSystem>
{
    #region 初始化与战斗准备

    /// <summary>注册战斗开始及角色养成相关事件。</summary>
    public override void OnSingletonInit()
    {
        EventKit.Type.Register<BattleStartEvent>(OnBattleStart);
        EventKit.Type.Register<UIEvents.StarUnlockRequestedEvent>(OnStarUnlockRequested);
        EventKit.Type.Register<UIEvents.TalentUpgradeRequestedEvent>(OnTalentUpgradeRequested);
        EventKit.Type.Register<UIEvents.WeaponChangeRequestedEvent>(OnWeaponChangeRequested);
        EventKit.Type.Register<UIEvents.RelicChangeRequestedEvent>(OnRelicChangeRequested);
        EventKit.Type.Register<UIEvents.WeaponUpgradeRequestedEvent>(OnWeaponUpgradeRequested);
        EventKit.Type.Register<UIEvents.RelicEnhanceRequestedEvent>(OnRelicEnhanceRequested);
        LogKit.Log("[BattlePreparationSystem] 角色操作事件监听注册完成");
    }

    /// <summary>校验战斗请求，构建队伍并打开战斗界面。</summary>
    private void OnBattleStart(BattleStartEvent evt)
    {
        if (evt == null || string.IsNullOrEmpty(evt.DungeonId))
        {
            LogKit.Warning("[BattlePreparationSystem] 未指定副本ID");
            return;
        }

        if (evt.SelectedCharacterIds == null || evt.SelectedCharacterIds.Count == 0)
        {
            LogKit.Warning("[BattlePreparationSystem] 未选择任何角色");
            return;
        }

        List<BattleUnitData> battleTeam = BuildBattleTeam(evt.SelectedCharacterIds, evt.OwnedCharacters);
        LogKit.Log($"[BattlePreparationSystem] 战斗队伍数量：{battleTeam.Count}");

        List<EnemyWaveData> enemyWaves = BattleSystem.Instance.LoadEnemyWavesFromConfig(evt.DungeonId);
        BattleSystem.Instance.Initialize(battleTeam, enemyWaves, evt.DungeonId);

        UIKit.ClosePanel<TeamPanel>();
        UIKit.OpenPanel<BattlePanel>();
    }

    private List<BattleUnitData> BuildBattleTeam(List<string> selectedCharacterIds, List<AccountItemData> ownedCharacters)
    {
        List<BattleUnitData> battleTeam = new List<BattleUnitData>();
        string accountId = AccountManager.Instance.CurrentAccount.AccountId;

        foreach (var charId in selectedCharacterIds)
        {
            var finalStats = UpgradeSystem.Instance.CalculateFinalStats(accountId, charId);
            var accountChar = ownedCharacters != null ? ownedCharacters.Find(c => c.ItemId == charId) : null;
            var config = GameDataMgr.Instance.playerCharacterConfigs.Characters.FirstOrDefault(c => c.CharacterId == charId);

            if (finalStats.Count > 0)
            {
                BattleUnitData unit = new BattleUnitData(
                    charId,
                    BattleUnitType.Player,
                    config != null ? config.Name : charId,
                    finalStats.ContainsKey("MaxHp") ? Mathf.RoundToInt(finalStats["MaxHp"]) : 100,
                    finalStats.ContainsKey("Speed") ? Mathf.RoundToInt(finalStats["Speed"]) : 100,
                    finalStats.ContainsKey("Attack") ? Mathf.RoundToInt(finalStats["Attack"]) : 10,
                    finalStats.ContainsKey("Defense") ? Mathf.RoundToInt(finalStats["Defense"]) : 0
                );

                unit.Level = accountChar != null ? accountChar.Level : 1;
                unit.StarLevel = accountChar != null ? accountChar.StarLevel : 0;
                unit.BaseCritRate = finalStats.ContainsKey("CritRate") ? finalStats["CritRate"] : 0.1f;
                unit.BaseCritDamage = finalStats.ContainsKey("CritDamage") ? finalStats["CritDamage"] : 0.5f;
                unit.BaseDamageBonus = finalStats.ContainsKey("DamageBonus") ? finalStats["DamageBonus"] : 0f;
                unit.BaseResistancePenetration = finalStats.ContainsKey("ResistancePenetration") ? finalStats["ResistancePenetration"] : 0f;

                // 加载星魂效果：终结技伤害加成
                if (finalStats.ContainsKey("UltimateBonus"))
                {
                    unit.UltimateBonus = finalStats["UltimateBonus"];
                    if (unit.UltimateBonus > 0)
                    {
                        LogKit.Log($"[BattlePreparationSystem] {charId} 加载星魂终结技加成: +{unit.UltimateBonus * 100:F0}%");
                    }
                }
                // 加载星魂效果：治疗加成
                if (finalStats.ContainsKey("HealBonus"))
                {
                    unit.StarHealBonus = finalStats["HealBonus"];
                    if (unit.StarHealBonus > 0)
                    {
                        LogKit.Log($"[BattlePreparationSystem] {charId} 加载星魂治疗加成: +{unit.StarHealBonus * 100:F0}%");
                    }
                }

                if (accountChar != null && config != null && config.TalentEffects != null)
                {
                    foreach (var talentEffect in config.TalentEffects)
                    {
                        int talentLevel = accountChar.GetTalentLevel(talentEffect.SkillType);
                        unit.TalentLevels[talentEffect.SkillType] = talentLevel;
                        var levelEffect = talentEffect.Effects.FirstOrDefault(e => e.Level == talentLevel);
                        if (levelEffect != null)
                        {
                            unit.TalentMultipliers[talentEffect.SkillType] = levelEffect.Value;
                            unit.TalentMultipliers2[talentEffect.SkillType] = levelEffect.Value2;
                        }
                        else
                        {
                            unit.TalentMultipliers[talentEffect.SkillType] = 0f;
                            unit.TalentMultipliers2[talentEffect.SkillType] = 0f;
                        }
                    }
                }

                // 加载星魂效果数值到StarEffectValues
                if (config != null && config.StarEffects != null)
                {
                    foreach (var starEffect in config.StarEffects)
                    {
                        unit.StarEffectValues[starEffect.StarLevel] = starEffect.Value;
                    }
                }

                if (accountChar != null && !string.IsNullOrEmpty(accountChar.EquippedWeapon))
                {
                    var weaponItem = AccountManager.Instance.GetAccountItemByInstanceId(
                        accountId, "Weapon", accountChar.EquippedWeapon);
                    var weaponConfig = GameDataMgr.Instance.lightConeConfigs.Weapons
                        .FirstOrDefault(w => weaponItem != null && w.WeaponId == weaponItem.ItemId);
                    if (weaponConfig != null && weaponConfig.Effects != null)
                    {
                        unit.InitializeWeaponEffects(weaponConfig.Effects);
                        LogKit.Log($"[BattlePreparationSystem] {charId} 加载武器特效: {weaponConfig.Name}");
                    }
                }

                if (accountChar != null && accountChar.EquippedRelics != null && accountChar.EquippedRelics.Count > 0)
                {
                    Dictionary<string, int> setCounts = new Dictionary<string, int>();
                    foreach (var relicKvp in accountChar.EquippedRelics)
                    {
                        var relicItem = AccountManager.Instance.GetAccountItemByInstanceId(
                            accountId, "Relic", relicKvp.Value);
                        var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
                            .FirstOrDefault(r => relicItem != null && r.RelicId == relicItem.ItemId);
                        if (relicConfig != null && !string.IsNullOrEmpty(relicConfig.SetName))
                        {
                            if (!setCounts.ContainsKey(relicConfig.SetName))
                                setCounts[relicConfig.SetName] = 0;
                            setCounts[relicConfig.SetName]++;
                        }
                    }
                    if (setCounts.Count > 0)
                    {
                        unit.InitializeRelicSetEffects(setCounts);
                        LogKit.Log($"[BattlePreparationSystem] {charId} 加载遗器套装特效: {string.Join(", ", setCounts.Select(s => $"{s.Key}x{s.Value}"))}");
                    }
                }

                battleTeam.Add(unit);
                LogKit.Log($"[BattlePreparationSystem] 添加角色(最终属性)：{charId}, HP:{unit.MaxHp}, Speed:{unit.BaseSpeed}, Attack:{unit.BaseAttack}, Level:{unit.Level}");
            }
        }

        return battleTeam;
    }

    private void OnStarUnlockRequested(UIEvents.StarUnlockRequestedEvent evt)
    {
        if (evt == null || string.IsNullOrEmpty(evt.CharacterId) || evt.StarLevel < 1 || evt.StarLevel > 5)
            return;

        if (!AccountManager.Instance.IsLoggedIn) return;
        string accountId = AccountManager.Instance.CurrentAccount.AccountId;

        LogKit.Log($"[BattlePreparationSystem] 处理星魂解锁请求 - 角色:{evt.CharacterId}, 星级:{evt.StarLevel}");

        bool success = UpgradeSystem.Instance.UnlockStar(accountId, evt.CharacterId, evt.StarLevel);

        if (success)
        {
            EventKit.Type.Send(new UIEvents.CharacterDataChangedEvent
            {
                CharacterId = evt.CharacterId
            });
        }

        EventKit.Type.Send(new UIEvents.StarUnlockResultEvent
        {
            CharacterId = evt.CharacterId,
            StarLevel = evt.StarLevel,
            Success = success
        });
    }

    private void OnTalentUpgradeRequested(UIEvents.TalentUpgradeRequestedEvent evt)
    {
        if (evt == null || string.IsNullOrEmpty(evt.CharacterId) || string.IsNullOrEmpty(evt.SkillType))
            return;

        if (!AccountManager.Instance.IsLoggedIn) return;
        string accountId = AccountManager.Instance.CurrentAccount.AccountId;

        LogKit.Log($"[BattlePreparationSystem] 处理天赋升级请求 - 角色:{evt.CharacterId}, 技能:{evt.SkillType}");

        bool success = UpgradeSystem.Instance.UpgradeTalent(accountId, evt.CharacterId, evt.SkillType);
        int newLevel = 0;
        if (success)
        {
            var charItem = AccountManager.Instance.GetAccountItem(accountId, "Character", evt.CharacterId);
            if (charItem != null)
                newLevel = charItem.GetTalentLevel(evt.SkillType);

            EventKit.Type.Send(new UIEvents.CharacterDataChangedEvent
            {
                CharacterId = evt.CharacterId
            });
        }

        EventKit.Type.Send(new UIEvents.TalentUpgradeResultEvent
        {
            CharacterId = evt.CharacterId,
            SkillType = evt.SkillType,
            Success = success,
            NewLevel = newLevel
        });
    }

    private void OnWeaponChangeRequested(UIEvents.WeaponChangeRequestedEvent evt)
    {
        if (evt == null || string.IsNullOrEmpty(evt.TargetCharacterId) || string.IsNullOrEmpty(evt.WeaponInstanceId))
            return;

        if (!AccountManager.Instance.IsLoggedIn) return;
        string accountId = AccountManager.Instance.CurrentAccount.AccountId;

        LogKit.Log($"[BattlePreparationSystem] 处理武器变更请求 - 目标角色:{evt.TargetCharacterId}, 武器:{evt.WeaponId}");

        var weaponItem = AccountManager.Instance.GetAccountItemByInstanceId(accountId, "Weapon", evt.WeaponInstanceId);
        if (weaponItem == null)
        {
            SendWeaponChangeResult(evt.TargetCharacterId, evt.WeaponId, false, "", evt.WeaponInstanceId);
            return;
        }

        // 判断装备状态
        bool isEquipped = weaponItem.ExtraData != null &&
            weaponItem.ExtraData.ContainsKey("IsEquipped") &&
            weaponItem.ExtraData["IsEquipped"] == "true";
        string equippedBy = isEquipped && weaponItem.ExtraData.ContainsKey("EquippedBy")
            ? weaponItem.ExtraData["EquippedBy"]
            : "";
        bool isEquippedBySelf = isEquipped && equippedBy == evt.TargetCharacterId;
        bool isEquippedByOther = isEquipped && !string.IsNullOrEmpty(equippedBy) && equippedBy != evt.TargetCharacterId;

        string changeType = "";

        if (isEquippedBySelf)
        {
            // 卸下：从当前角色卸下
            UpgradeSystem.Instance.UnequipWeapon(accountId, evt.TargetCharacterId);
            changeType = "Unequip";
            LogKit.Log($"[BattlePreparationSystem] 武器卸下完成 - 角色:{evt.TargetCharacterId}, 武器:{evt.WeaponId}");
        }
        else if (isEquippedByOther)
        {
            // 替换：先从原装备者卸下，再装备到当前角色
            UpgradeSystem.Instance.UnequipWeapon(accountId, equippedBy);
            LogKit.Log($"[BattlePreparationSystem] 武器替换(卸) - 原角色:{equippedBy}, 武器:{evt.WeaponId}");

            bool equipSuccess = UpgradeSystem.Instance.EquipWeapon(accountId, evt.TargetCharacterId, evt.WeaponInstanceId);
            if (equipSuccess)
            {
                changeType = "Replace";
                LogKit.Log($"[BattlePreparationSystem] 武器替换(装) - 新角色:{evt.TargetCharacterId}, 武器:{evt.WeaponId}");
            }
            else
            {
                SendWeaponChangeResult(evt.TargetCharacterId, evt.WeaponId, false, changeType, evt.WeaponInstanceId);
                return;
            }
        }
        else
        {
            // 装备：直接装备到当前角色
            bool equipSuccess = UpgradeSystem.Instance.EquipWeapon(accountId, evt.TargetCharacterId, evt.WeaponInstanceId);
            if (equipSuccess)
            {
                changeType = "Equip";
                LogKit.Log($"[BattlePreparationSystem] 武器装备完成 - 角色:{evt.TargetCharacterId}, 武器:{evt.WeaponId}");
            }
            else
            {
                SendWeaponChangeResult(evt.TargetCharacterId, evt.WeaponId, false, changeType, evt.WeaponInstanceId);
                return;
            }
        }

        // 发送武器装备事件，通知CharacterPanel刷新武器显示（此事件由BattlePreparationSystem统一发送，sonWeaponPanel不再重复发送）
        EventKit.Type.Send(new UIEvents.WeaponEquippedEvent
        {
            CharacterId = evt.TargetCharacterId,
            WeaponId = evt.WeaponId
        });

        SendWeaponChangeResult(evt.TargetCharacterId, evt.WeaponId, true, changeType, evt.WeaponInstanceId);
    }

    private void SendWeaponChangeResult(string targetCharacterId, string weaponId, bool success, string changeType, string weaponInstanceId = "")
    {
        EventKit.Type.Send(new UIEvents.WeaponChangeResultEvent
        {
            TargetCharacterId = targetCharacterId,
            WeaponId = weaponId,
            WeaponInstanceId = weaponInstanceId,
            Success = success,
            ChangeType = changeType
        });
    }

    private void OnRelicChangeRequested(UIEvents.RelicChangeRequestedEvent evt)
    {
        if (evt == null || string.IsNullOrEmpty(evt.TargetCharacterId) || string.IsNullOrEmpty(evt.RelicInstanceId))
            return;

        if (!AccountManager.Instance.IsLoggedIn) return;
        string accountId = AccountManager.Instance.CurrentAccount.AccountId;

        LogKit.Log($"[BattlePreparationSystem] 处理遗器变更请求 - 目标角色:{evt.TargetCharacterId}, 遗器:{evt.RelicId}, 部位:{evt.RelicSlot}");

        var relicItem = AccountManager.Instance.GetAccountItemByInstanceId(accountId, "Relic", evt.RelicInstanceId);
        if (relicItem == null)
        {
            SendRelicChangeResult(evt.TargetCharacterId, evt.RelicId, evt.RelicSlot, false, "", evt.RelicInstanceId);
            return;
        }

        // 判断装备状态
        bool isEquipped = relicItem.ExtraData != null &&
            relicItem.ExtraData.ContainsKey("IsEquipped") &&
            relicItem.ExtraData["IsEquipped"] == "true";
        string equippedBy = isEquipped && relicItem.ExtraData.ContainsKey("EquippedBy")
            ? relicItem.ExtraData["EquippedBy"]
            : "";
        bool isEquippedBySelf = isEquipped && equippedBy == evt.TargetCharacterId;
        bool isEquippedByOther = isEquipped && !string.IsNullOrEmpty(equippedBy) && equippedBy != evt.TargetCharacterId;

        string normalizedSlot = NormalizeRelicSlot(evt.RelicSlot);
        string changeType = "";

        if (isEquippedBySelf)
        {
            // 卸下：从当前角色卸下
            UpgradeSystem.Instance.UnequipRelic(accountId, evt.TargetCharacterId, normalizedSlot);
            changeType = "Unequip";
            LogKit.Log($"[BattlePreparationSystem] 遗器卸下完成 - 角色:{evt.TargetCharacterId}, 部位:{normalizedSlot}, 遗器:{evt.RelicId}");
        }
        else if (isEquippedByOther)
        {
            // 替换：先从原装备者卸下，再装备到当前角色
            string originalSlot = relicItem.ExtraData.ContainsKey("EquippedSlot")
                ? relicItem.ExtraData["EquippedSlot"]
                : normalizedSlot;

            UpgradeSystem.Instance.UnequipRelic(accountId, equippedBy, originalSlot);
            LogKit.Log($"[BattlePreparationSystem] 遗器替换(卸) - 原角色:{equippedBy}, 部位:{originalSlot}, 遗器:{evt.RelicId}");

            bool equipSuccess = UpgradeSystem.Instance.EquipRelic(accountId, evt.TargetCharacterId, evt.RelicInstanceId);
            if (equipSuccess)
            {
                changeType = "Replace";
                LogKit.Log($"[BattlePreparationSystem] 遗器替换(装) - 新角色:{evt.TargetCharacterId}, 遗器:{evt.RelicId}");
            }
            else
            {
                SendRelicChangeResult(evt.TargetCharacterId, evt.RelicId, evt.RelicSlot, false, changeType, evt.RelicInstanceId);
                return;
            }
        }
        else
        {
            // 装备：直接装备到当前角色
            bool equipSuccess = UpgradeSystem.Instance.EquipRelic(accountId, evt.TargetCharacterId, evt.RelicInstanceId);
            if (equipSuccess)
            {
                changeType = "Equip";
                LogKit.Log($"[BattlePreparationSystem] 遗器装备完成 - 角色:{evt.TargetCharacterId}, 部位:{normalizedSlot}, 遗器:{evt.RelicId}");
            }
            else
            {
                SendRelicChangeResult(evt.TargetCharacterId, evt.RelicId, evt.RelicSlot, false, changeType, evt.RelicInstanceId);
                return;
            }
        }

        // 发送遗器装备事件，通知CharacterPanel刷新遗器属性显示（统一由BattlePreparationSystem发送，sonRelicPanel不再重复发送）
        EventKit.Type.Send(new UIEvents.RelicEquippedEvent
        {
            CharacterId = evt.TargetCharacterId
        });

        SendRelicChangeResult(evt.TargetCharacterId, evt.RelicId, evt.RelicSlot, true, changeType, evt.RelicInstanceId);
    }

    private string NormalizeRelicSlot(string slot)
    {
        switch (slot)
        {
            case "Hand": return "Hands";
            case "Hands": return "Hands";
            default: return slot;
        }
    }

    private void SendRelicChangeResult(string targetCharacterId, string relicId, string relicSlot, bool success, string changeType, string relicInstanceId = "")
    {
        EventKit.Type.Send(new UIEvents.RelicChangeResultEvent
        {
            TargetCharacterId = targetCharacterId,
            RelicId = relicId,
            RelicInstanceId = relicInstanceId,
            RelicSlot = relicSlot,
            Success = success,
            ChangeType = changeType
        });
    }

    private void OnWeaponUpgradeRequested(UIEvents.WeaponUpgradeRequestedEvent evt)
    {
        if (evt == null || string.IsNullOrEmpty(evt.WeaponInstanceId))
            return;

        if (!AccountManager.Instance.IsLoggedIn) return;
        string accountId = AccountManager.Instance.CurrentAccount.AccountId;

        LogKit.Log($"[BattlePreparationSystem] 处理武器升级请求 - 武器:{evt.WeaponId}, 目标角色:{evt.TargetCharacterId}");

        var upgradeCosts = UpgradeSystem.Instance.GetWeaponUpgradeCost(evt.WeaponInstanceId);
        int goldCost = UpgradeSystem.Instance.GetWeaponUpgradeGoldCost(evt.WeaponInstanceId);

        bool success = UpgradeSystem.Instance.UpgradeWeapon(accountId, evt.WeaponInstanceId, upgradeCosts, goldCost);

        if (success)
        {
            LogKit.Log($"[BattlePreparationSystem] 武器升级成功 - 武器:{evt.WeaponId}");

            if (!string.IsNullOrEmpty(evt.TargetCharacterId))
            {
                UpgradeSystem.Instance.UpdateCharacterFinalStats(accountId, evt.TargetCharacterId);
                EventKit.Type.Send(new UIEvents.CharacterDataChangedEvent
                {
                    CharacterId = evt.TargetCharacterId
                });
            }
        }
        else
        {
            LogKit.Log($"[BattlePreparationSystem] 武器升级失败 - 武器:{evt.WeaponId}");
        }

        EventKit.Type.Send(new UIEvents.WeaponUpgradeResultEvent
        {
            WeaponId = evt.WeaponId,
            WeaponInstanceId = evt.WeaponInstanceId,
            TargetCharacterId = evt.TargetCharacterId,
            Success = success
        });
    }

    private void OnRelicEnhanceRequested(UIEvents.RelicEnhanceRequestedEvent evt)
    {
        if (evt == null || string.IsNullOrEmpty(evt.RelicInstanceId))
            return;

        if (!AccountManager.Instance.IsLoggedIn) return;
        string accountId = AccountManager.Instance.CurrentAccount.AccountId;

        LogKit.Log($"[BattlePreparationSystem] 处理遗器强化请求 - 遗器:{evt.RelicId}, 目标角色:{evt.TargetCharacterId}");

        var upgradeCosts = UpgradeSystem.Instance.GetRelicUpgradeCost(evt.RelicInstanceId);
        int goldCost = UpgradeSystem.Instance.GetRelicUpgradeGoldCost(evt.RelicInstanceId);

        // 校验材料
        if (upgradeCosts != null)
        {
            foreach (var cost in upgradeCosts)
            {
                int owned = AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", cost.ItemId);
                if (owned < cost.Count)
                {
                    LogKit.Log($"[BattlePreparationSystem] 遗器强化失败 - 材料 {cost.ItemId} 不足 ({owned}/{cost.Count})");
                    EventKit.Type.Send(new UIEvents.RelicEnhanceResultEvent
                    {
                        RelicId = evt.RelicId,
                        RelicInstanceId = evt.RelicInstanceId,
                        TargetCharacterId = evt.TargetCharacterId,
                        Success = false
                    });
                    return;
                }
            }

        }

        // 调用强化（EnhanceRelic内部处理金币扣除和强化逻辑）
        bool success = UpgradeSystem.Instance.EnhanceRelic(accountId, evt.RelicInstanceId, goldCost);

        if (success)
        {
            if (upgradeCosts != null)
            {
                foreach (var cost in upgradeCosts)
                {
                    AccountManager.Instance.RemoveAccountItem(accountId, "Consumable", cost.ItemId, cost.Count);
                }
            }

            LogKit.Log($"[BattlePreparationSystem] 遗器强化成功 - 遗器:{evt.RelicId}");

            if (!string.IsNullOrEmpty(evt.TargetCharacterId))
            {
                UpgradeSystem.Instance.UpdateCharacterFinalStats(accountId, evt.TargetCharacterId);
                EventKit.Type.Send(new UIEvents.RelicEquippedEvent
                {
                    CharacterId = evt.TargetCharacterId
                });
            }
        }
        else
        {
            LogKit.Log($"[BattlePreparationSystem] 遗器强化失败 - 遗器:{evt.RelicId}");
        }

        EventKit.Type.Send(new UIEvents.RelicEnhanceResultEvent
        {
            RelicId = evt.RelicId,
            RelicInstanceId = evt.RelicInstanceId,
            TargetCharacterId = evt.TargetCharacterId,
            Success = success
        });
    }

    #endregion
}

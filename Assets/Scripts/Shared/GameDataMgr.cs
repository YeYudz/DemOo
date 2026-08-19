using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YokiFrame;

public class GameDataMgr 
{
    private static GameDataMgr instance = new GameDataMgr();
    public static GameDataMgr Instance => instance;

    public MusicData musicData;
    public VideoSettingsData videoSettingsData;
    public CharacterConfigWrapper playerCharacterConfigs;
    public EnemyConfigWrapper enemyConfigs;
    public SkillConfigWrapper skillConfigs;
    public WaveConfigWrapper waveConfigs;
    public DungeonConfigWrapper dungeonConfigs;
    public RelicConfigWrapper relicConfigs;
    public LightConeConfigWrapper lightConeConfigs;
    public ConsumableConfigWrapper consumableConfigs;
    public LotteryConfigWrapper lotteryConfigs;
    public DecomposeConfigWrapper decomposeConfigs;
    public UpgradeConfigWrapper upgradeConfigs;
    public StoreConfigWrapper storeConfigs;

    private GameDataMgr()
    {
        // 玩家设置需要在 YooAsset 初始化前应用，始终从可写目录读取。
        musicData = JsonMgr.Instance.LoadData<MusicData>("MusicData");
        videoSettingsData = JsonMgr.Instance.LoadData<VideoSettingsData>("VideoSettings");
    }

    public void ReloadConfigs()
    {
        playerCharacterConfigs = JsonMgr.Instance.LoadConfig<CharacterConfigWrapper>("PlayerCharactersConfig");
        enemyConfigs = JsonMgr.Instance.LoadConfig<EnemyConfigWrapper>("EnemiesConfig");
        skillConfigs = JsonMgr.Instance.LoadConfig<SkillConfigWrapper>("SkillsConfig");
        waveConfigs = JsonMgr.Instance.LoadConfig<WaveConfigWrapper>("WavesConfig");
        dungeonConfigs = JsonMgr.Instance.LoadConfig<DungeonConfigWrapper>("DungeonsConfig");
        relicConfigs = JsonMgr.Instance.LoadConfig<RelicConfigWrapper>("RelicsConfig");
        lightConeConfigs = JsonMgr.Instance.LoadConfig<LightConeConfigWrapper>("LightConeConfig");
        consumableConfigs = JsonMgr.Instance.LoadConfig<ConsumableConfigWrapper>("ConsumableConfig");
        lotteryConfigs = JsonMgr.Instance.LoadConfig<LotteryConfigWrapper>("LotteryConfig");
        decomposeConfigs = JsonMgr.Instance.LoadConfig<DecomposeConfigWrapper>("DecomposeConfig");
        upgradeConfigs = JsonMgr.Instance.LoadConfig<UpgradeConfigWrapper>("UpgradeConfig");
        storeConfigs = JsonMgr.Instance.LoadConfig<StoreConfigWrapper>("StoreConfig");

        AutoPopulateLotteryItemIds();

        LogKit.Log($"[GameDataMgr] YooAsset 配置加载完成 - 角色:{playerCharacterConfigs.Characters.Count}, 敌人:{enemyConfigs.Enemies.Count}, 技能:{skillConfigs.Skills.Count}, 波次:{waveConfigs.Waves.Count}, 遗器:{relicConfigs.Relics.Count}, 武器:{lightConeConfigs.Weapons.Count}, 消耗品:{consumableConfigs.Consumables.Count}, 卡池:{lotteryConfigs.Pools.Count}, 商店:{storeConfigs?.StoreItems?.Count ?? 0}");

        foreach (var pool in lotteryConfigs.Pools)
        {
            LogKit.Log($"[GameDataMgr] 卡池 {pool.PoolId}: 5星[{string.Join(",", pool.FiveStarItemIds)}], UP5星[{string.Join(",", pool.UpFiveStarIds)}], 4星[{string.Join(",", pool.FourStarItemIds)}], UP4星[{string.Join(",", pool.UpFourStarIds)}], 3星[{string.Join(",", pool.ThreeStarItemIds)}]");
        }
    }

    

    public void SaveData<T>(T data, string key)
    {
        try
        {
            JsonMgr.Instance.SaveData(data, key);
        }
        catch (System.Exception e)
        {
            LogKit.Warning($"[GameDataMgr] 保存数据失败 {key}: {e.Message}");
        }
    }

    public void SaveMusicData()
    {
        SaveData(musicData, "MusicData");
    }

    public void SaveVideoSettingsData()
    {
        SaveData(videoSettingsData, "VideoSettings");
    }

    private void AutoPopulateLotteryItemIds()
    {
        if (lotteryConfigs?.Pools == null) return;

        foreach (var pool in lotteryConfigs.Pools)
        {
            if (pool.FiveStarItemIds == null || pool.FiveStarItemIds.Count == 0)
            {
                pool.FiveStarItemIds = playerCharacterConfigs.Characters
                    .Where(c => c.Rarity == 5)
                    .Select(c => c.CharacterId)
                    .Concat(lightConeConfigs.Weapons.Where(w => w.Rarity == 5).Select(w => w.WeaponId))
                    .ToList();
            }

            if (pool.FourStarItemIds == null || pool.FourStarItemIds.Count == 0)
            {
                pool.FourStarItemIds = playerCharacterConfigs.Characters
                    .Where(c => c.Rarity == 4)
                    .Select(c => c.CharacterId)
                    .Concat(lightConeConfigs.Weapons.Where(w => w.Rarity == 4).Select(w => w.WeaponId))
                    .ToList();
            }

            if (pool.ThreeStarItemIds == null || pool.ThreeStarItemIds.Count == 0)
            {
                pool.ThreeStarItemIds = lightConeConfigs.Weapons
                    .Where(w => w.Rarity == 3)
                    .Select(w => w.WeaponId)
                    .ToList();
            }
        }
    }
}

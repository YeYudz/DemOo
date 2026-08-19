using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 角色配置查询服务，提供星魂效果描述读取。
/// </summary>
public static class CharacterConfigService
{
    #region 星魂效果查询

    /// <summary>获取角色当前已解锁的星魂效果描述。</summary>
    public static List<string> GetUnlockedStarEffectDescriptions(string characterId, int starLevel)
    {
        if (starLevel <= 0 || GameDataMgr.Instance?.playerCharacterConfigs?.Characters == null)
            return new List<string>();

        var characterConfig = GameDataMgr.Instance.playerCharacterConfigs.Characters
            .FirstOrDefault(config => config.CharacterId == characterId);
        if (characterConfig?.StarEffects == null)
            return new List<string>();

        int unlockedCount = Math.Min(starLevel, characterConfig.StarEffects.Count);
        var descriptions = new List<string>(unlockedCount);
        for (int i = 0; i < unlockedCount; i++)
        {
            string description = characterConfig.StarEffects[i]?.Description;
            if (!string.IsNullOrEmpty(description))
                descriptions.Add(description);
        }

        return descriptions;
    }

    #endregion
}

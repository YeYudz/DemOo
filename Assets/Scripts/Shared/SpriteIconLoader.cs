using UnityEngine;
using UnityEngine.UI;
using YokiFrame;

namespace GameUI
{
    /// <summary>
    /// 图片资源加载器，统一管理角色、武器、遗器、敌人、技能、星魂、天赋等图标的加载
    /// </summary>
    public static class SpriteIconLoader
    {
        private const string CHARACTER_PATH = "Sprites/Character/";
        private const string ENEMY_PATH = "Sprites/Enemy/";
        private const string WEAPON_PATH = "Sprites/Weapon/";
        private const string RELIC_PATH = "Sprites/Relic/";
        private const string SKILL_PATH = "Sprites/Skill/";
        private const string STARCONE_PATH = "Sprites/StarCone/";
        private const string CONSUMABLE_PATH = "Sprites/Consumable/";
        private const string DEALER_PATH = "Sprites/Dealer/";
        private const string EXP_PATH = "Sprites/Exp/";
        private const string OTHER_PATH = "Sprites/Other/";

        /// <summary>
        /// 从指定路径加载Sprite，支持多种回退方式。
        /// 加载策略：
        ///   1. ResKit.Load(itemId) — 打包后走 YooAsset（地址=文件名，如 "Archer"）
        ///   2. Resources.Load(basePath+itemId) — 编辑器或 YooAsset 未命中时走 Resources 完整路径兜底
        ///   3. Resources.Load<Texture2D> + 转 Sprite — 支持 jpg 等非 Sprite 格式
        /// </summary>
        private static Sprite LoadSprite(string basePath, string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            // 方式1: 使用 ResKit 加载（YooAsset 地址 = 文件名，不带路径前缀）
            //   打包后: YooAssetResourceProvider 通过 "Archer" 命中最新 Bundle
            //   编辑器: UnityResourceProvider 通过 "Archer" 找不到 → 返回 null → 走方式2
            //   特殊处理: Other 组使用 AddressByGroupAndFileName，地址为 "Sprites_{fileName}"
            string yooAddress = ResolveYooAssetAddress(basePath, itemId);
            var sprite = ResKit.Load<Sprite>(yooAddress);
            if (sprite != null) return sprite;

            // 方式2: 使用 Unity Resources.Load 加载 Sprite（完整路径兜底）
            string fullPath = basePath + itemId;
            sprite = Resources.Load<Sprite>(fullPath);
            if (sprite != null) return sprite;

            // 方式3: 尝试加载 Texture2D 并转换为 Sprite (支持 jpg 等格式)
            var texture = Resources.Load<Texture2D>(fullPath);
            if (texture != null)
            {
                RenderTexture rt = RenderTexture.GetTemporary(
                    texture.width, texture.height, 0, RenderTextureFormat.ARGB32);
                RenderTexture previous = RenderTexture.active;
                Graphics.Blit(texture, rt);
                RenderTexture.active = rt;

                Texture2D readableTexture = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);
                readableTexture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
                readableTexture.Apply();

                RenderTexture.active = previous;
                RenderTexture.ReleaseTemporary(rt);

                sprite = Sprite.Create(readableTexture, new Rect(0, 0, readableTexture.width, readableTexture.height), new Vector2(0.5f, 0.5f));
                if (sprite != null) return sprite;

                Object.Destroy(readableTexture);
            }

            return null;
        }

        /// <summary>
        /// 根据 basePath 解析 YooAsset 地址。
        /// Sprites/Character、Sprites/Skill 等目录使用 AddressByFileName，地址 = 文件名。
        /// Sprites/Other 目录使用 AddressByGroupAndFileName，地址 = "Sprites_文件名"。
        /// </summary>
        private static string ResolveYooAssetAddress(string basePath, string itemId)
        {
            // Other 组使用 AddressByGroupAndFileName 规则，组名为 "Sprites"
            if (basePath == OTHER_PATH)
                return $"Sprites_{itemId}";
            // 其余目录均使用 AddressByFileName，地址 = 文件名
            return itemId;
        }

        /// <summary>
        /// 加载角色头像
        /// </summary>
        public static Sprite LoadCharacterIcon(string characterId)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            var sprite = LoadSprite(CHARACTER_PATH, characterId);
            if (sprite == null)
                LogKit.Warning($"[SpriteIconLoader] 角色图片未找到: {CHARACTER_PATH}{characterId}");
            return sprite;
        }

        /// <summary>
        /// 加载敌人图标
        /// </summary>
        public static Sprite LoadEnemyIcon(string enemyId)
        {
            if (string.IsNullOrEmpty(enemyId)) return null;
            var sprite = LoadSprite(ENEMY_PATH, enemyId);
            if (sprite == null)
                LogKit.Warning($"[SpriteIconLoader] 敌人图片未找到: {ENEMY_PATH}{enemyId}");
            return sprite;
        }

        /// <summary>
        /// 加载武器图标
        /// </summary>
        public static Sprite LoadWeaponIcon(string weaponId)
        {
            if (string.IsNullOrEmpty(weaponId)) return null;
            var sprite = LoadSprite(WEAPON_PATH, weaponId);
            if (sprite == null)
                LogKit.Warning($"[SpriteIconLoader] 武器图片未找到: {WEAPON_PATH}{weaponId}");
            return sprite;
        }

        /// <summary>
        /// 加载遗器图标
        /// </summary>
        public static Sprite LoadRelicIcon(string relicId)
        {
            if (string.IsNullOrEmpty(relicId)) return null;
            var sprite = LoadSprite(RELIC_PATH, relicId);
            if (sprite == null)
                LogKit.Warning($"[SpriteIconLoader] 遗器图片未找到: {RELIC_PATH}{relicId}");
            return sprite;
        }

        /// <summary>
        /// 加载技能卡牌图标 (SkillId: Warrior_NormalAttack, Mage_Ultimate, ...)
        /// </summary>
        public static Sprite LoadSkillIcon(string skillId)
        {
            if (string.IsNullOrEmpty(skillId)) return null;
            var sprite = LoadSprite(SKILL_PATH, skillId);
            if (sprite == null)
            {
                string yooAddress = ResolveYooAssetAddress(SKILL_PATH, skillId);
                LogKit.Warning(
                    $"[SpriteIconLoader] 技能图标加载失败：YooAsset地址={yooAddress}，" +
                    $"Resources路径={SKILL_PATH}{skillId}。ResKit与Resources回退均未命中，请确认 " +
                    "Sprites/Skill Collector 已构建并随当前 Windows 版本完整发布。"
                );
            }
            return sprite;
        }

        /// <summary>
        /// 加载星魂图标 (格式: {CharacterId}_Star{Level}, 如 Warrior_Star1)
        /// </summary>
        public static Sprite LoadStarConeIcon(string characterId, int starLevel)
        {
            if (string.IsNullOrEmpty(characterId)) return null;
            string starId = $"{characterId}_Star{starLevel}";
            var sprite = LoadSprite(STARCONE_PATH, starId);
            if (sprite == null)
                LogKit.Warning($"[SpriteIconLoader] 星魂图片未找到: {STARCONE_PATH}{starId}");
            return sprite;
        }

        /// <summary>
        /// 加载星魂图标 (直接使用完整ID)
        /// </summary>
        public static Sprite LoadStarConeIcon(string starId)
        {
            if (string.IsNullOrEmpty(starId)) return null;
            var sprite = LoadSprite(STARCONE_PATH, starId);
            if (sprite == null)
                LogKit.Warning($"[SpriteIconLoader] 星魂图片未找到: {STARCONE_PATH}{starId}");
            return sprite;
        }

        /// <summary>
        /// 加载经验图标
        /// </summary>
        public static Sprite LoadExpIcon(string expId)
        {
            if (string.IsNullOrEmpty(expId)) return null;
            var sprite = LoadSprite(EXP_PATH, expId);
            if (sprite == null)
                LogKit.Warning($"[SpriteIconLoader] 经验图片未找到: {EXP_PATH}{expId}");
            return sprite;
        }

        /// <summary>
        /// 加载其它杂项图标（如 TeamAdd 编队加号占位图）
        /// </summary>
        public static Sprite LoadOtherIcon(string iconId)
        {
            if (string.IsNullOrEmpty(iconId)) return null;
            var sprite = LoadSprite(OTHER_PATH, iconId);
            if (sprite == null)
                LogKit.Warning($"[SpriteIconLoader] 其它图片未找到: {OTHER_PATH}{iconId}");
            return sprite;
        }

        /// <summary>
        /// 加载消耗品图标 (格式: {CharacterId}_{SkillType}_Consumable, 如 Warrior_NormalAttack_Consumable)
        /// </summary>
        public static Sprite LoadConsumableIcon(string consumableId)
        {
            if (string.IsNullOrEmpty(consumableId)) return null;
            var sprite = LoadSprite(CONSUMABLE_PATH, consumableId);
            if (sprite == null)
                LogKit.Warning($"[SpriteIconLoader] 消耗品图片未找到: {CONSUMABLE_PATH}{consumableId}");
            return sprite;
        }

        /// <summary>
        /// 加载发牌器图标
        /// </summary>
        public static Sprite LoadDealerIcon(string dealerId = "CardDealer")
        {
            var sprite = LoadSprite(DEALER_PATH, dealerId);
            if (sprite == null)
                LogKit.Warning($"[SpriteIconLoader] 发牌器图片未找到: {DEALER_PATH}{dealerId}");
            return sprite;
        }

        /// <summary>
        /// 根据类型和ID加载对应图标
        /// </summary>
        public static Sprite LoadIcon(string itemType, string itemId)
        {
            switch (itemType)
            {
                case "Character":
                    return LoadCharacterIcon(itemId);
                case "Enemy":
                    return LoadEnemyIcon(itemId);
                case "Weapon":
                    return LoadWeaponIcon(itemId);
                case "Relic":
                    return LoadRelicIcon(itemId);
                case "Skill":
                    return LoadSkillIcon(itemId);
                case "StarCone":
                    return LoadStarConeIcon(itemId);
                case "Consumable":
                    return LoadConsumableIcon(itemId);
                case "Dealer":
                    return LoadDealerIcon(itemId);
                case "Exp":
                    return LoadExpIcon(itemId);
                case "Other":
                    return LoadOtherIcon(itemId);
                default:
                    LogKit.Warning($"[SpriteIconLoader] 未知物品类型: {itemType}");
                    return null;
            }
        }

        /// <summary>
        /// 安全设置图片，加载失败时清除旧sprite
        /// </summary>
        public static void SetIcon(Image image, string itemType, string itemId)
        {
            if (image == null) return;
            var sprite = LoadIcon(itemType, itemId);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
            }
        }

        /// <summary>
        /// 安全设置角色图片
        /// </summary>
        public static void SetCharacterIcon(Image image, string characterId)
        {
            if (image == null) return;
            var sprite = LoadCharacterIcon(characterId);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
            }
        }

        /// <summary>
        /// 安全设置武器图片
        /// </summary>
        public static void SetWeaponIcon(Image image, string weaponId)
        {
            if (image == null) return;
            var sprite = LoadWeaponIcon(weaponId);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
            }
        }

        /// <summary>
        /// 安全设置遗器图片
        /// </summary>
        public static void SetRelicIcon(Image image, string relicId)
        {
            if (image == null) return;
            var sprite = LoadRelicIcon(relicId);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
            }
        }

        /// <summary>
        /// 安全设置敌人图片
        /// </summary>
        public static void SetEnemyIcon(Image image, string enemyId)
        {
            if (image == null) return;
            var sprite = LoadEnemyIcon(enemyId);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
            }
        }

        /// <summary>
        /// 安全设置技能卡牌图片
        /// </summary>
        public static void SetSkillIcon(Image image, string skillId)
        {
            if (image == null) return;
            var sprite = LoadSkillIcon(skillId);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
            }
        }

        /// <summary>
        /// 安全设置星魂图片
        /// </summary>
        public static void SetStarConeIcon(Image image, string starId)
        {
            if (image == null) return;
            var sprite = LoadStarConeIcon(starId);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
            }
        }

        /// <summary>
        /// 安全设置消耗品图片
        /// </summary>
        public static void SetConsumableIcon(Image image, string consumableId)
        {
            if (image == null) return;
            var sprite = LoadConsumableIcon(consumableId);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
            }
        }

        /// <summary>
        /// 安全设置发牌器图片
        /// </summary>
        public static void SetDealerIcon(Image image, string dealerId = "CardDealer")
        {
            if (image == null) return;
            var sprite = LoadDealerIcon(dealerId);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
            }
        }

        /// <summary>
        /// 安全设置其它杂项图片（如 TeamAdd 编队加号占位图）
        /// </summary>
        public static void SetOtherIcon(Image image, string iconId)
        {
            if (image == null) return;
            var sprite = LoadOtherIcon(iconId);
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
            }
            else
            {
                image.sprite = null;
            }
        }
    }
}

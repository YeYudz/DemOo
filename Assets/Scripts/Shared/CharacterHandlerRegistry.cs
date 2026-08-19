using System;
using System.Collections.Generic;
using YokiFrame;

namespace Game.GameLogic.Battle.Core
{
    public static class CharacterHandlerRegistry
    {
        private static readonly Dictionary<string, ICharacterSkillHandler> _handlers =
            new Dictionary<string, ICharacterSkillHandler>();

        private static bool _initialized = false;

        public static void Register(ICharacterSkillHandler handler)
        {
            if (handler == null || string.IsNullOrEmpty(handler.CharacterId)) return;
            _handlers[handler.CharacterId] = handler;
            LogKit.Log($"[CharacterRegistry] 注册角色处理器: {handler.CharacterId}");
        }

        public static void EnsureAllRegistered()
        {
            if (_initialized) return;
            _initialized = true;

            foreach (var handler in FindAllHandlers())
            {
                Register(handler);
            }

            LogKit.Log($"[CharacterRegistry] 初始化完成，共注册 {_handlers.Count} 个角色处理器");
        }

        public static ICharacterSkillHandler Get(string characterId)
        {
            return string.IsNullOrEmpty(characterId) ? null :
                _handlers.TryGetValue(characterId, out var h) ? h : null;
        }

        public static IEnumerable<string> AllCharacterIds => _handlers.Keys;

        // 保留旧热更新程序集依赖的 API，避免 Shared 与已发布 HotUpdate.dll 版本不一致时崩溃。
        public static List<string> GetUnlockedStarEffectDescriptions(string characterId, int starLevel)
        {
            var descriptions = new List<string>();
            if (string.IsNullOrEmpty(characterId) || starLevel <= 0)
                return descriptions;

            var characters = GameDataMgr.Instance?.playerCharacterConfigs?.Characters;
            if (characters == null)
                return descriptions;

            for (int i = 0; i < characters.Count; i++)
            {
                var character = characters[i];
                if (character == null || character.CharacterId != characterId || character.StarEffects == null)
                    continue;

                int unlockedCount = Math.Min(starLevel, character.StarEffects.Count);
                for (int j = 0; j < unlockedCount; j++)
                {
                    string description = character.StarEffects[j]?.Description;
                    if (!string.IsNullOrEmpty(description))
                        descriptions.Add(description);
                }

                break;
            }

            return descriptions;
        }

        private static IEnumerable<ICharacterSkillHandler> FindAllHandlers()
        {
            var result = new List<ICharacterSkillHandler>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var asm in assemblies)
            {
                try
                {
                    string asmName = asm.GetName().Name;
                    if (asmName.StartsWith("Unity") || asmName.StartsWith("System"))
                        continue;

                    foreach (var type in asm.GetTypes())
                    {
                        if (!typeof(ICharacterSkillHandler).IsAssignableFrom(type)) continue;
                        if (type.IsInterface || type.IsAbstract) continue;

                        try
                        {
                            var instance = (ICharacterSkillHandler)Activator.CreateInstance(type);
                            result.Add(instance);
                        }
                        catch (Exception e)
                        {
                            LogKit.Warning($"[CharacterRegistry] 激活 {type.Name} 失败: {e.Message}");
                        }
                    }
                }
                catch { }
            }

            return result;
        }
    }
}

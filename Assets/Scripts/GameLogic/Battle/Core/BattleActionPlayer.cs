using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using YokiFrame;
using GameUI;
using Game;
using UnitEvents;
using CardManagement;

/// <summary>
/// 战斗表现层播放器：负责技能视觉效果播放，通过协程实现异步等待。
/// 逻辑层发送 SkillVisualRequestEvent → 表现层播放动画 → 命中时发送 SkillVisualHitEvent → 完成后发送 SkillVisualCompleteEvent
/// </summary>
public class BattleActionPlayer : MonoBehaviour
{
    private static BattleActionPlayer sInstance;

    /// <summary>
    /// 单位视图注册表：UnitId → RectTransform，用于获取头像位置
    /// </summary>
    private Dictionary<string, RectTransform> mUnitViews = new Dictionary<string, RectTransform>();

    /// <summary>
    /// 跟踪当前活跃的HandCardFlyIn容器，战斗结束时统一销毁
    /// </summary>
    private List<GameObject> mHandCardFlyInContainers = new List<GameObject>();
    private List<GameObject> mAttachedBattleEffects = new List<GameObject>();
    private Transform mEffectRoot;

    /// <summary>
    /// 敌方/我方面板引用，用于字典查找失败时直接遍历面板子物体
    /// </summary>
    private Transform mEnemyUnitsPanel;
    private Transform mPlayerUnitsPanel;

    /// <summary>
    /// 手牌面板引用，用于手牌操作技能（Mage_Skill2/Mage_Ultimate）的卡牌飞入表现
    /// </summary>
    private Transform mHandCardsPanel;

    private Coroutine mCurrentCoroutine;
    private Coroutine mDealerEffectCoroutine;
    private Coroutine mAvatarGlowCoroutine;
    private Image mGlowingAvatar;
    private Material mAvatarGlowMaterial;
    private Material mAvatarGlowMaterialTemplate;

    public static BattleActionPlayer Instance => sInstance;

    public static BattleActionPlayer Create()
    {
        if (sInstance != null) return sInstance;

        GameObject obj = new GameObject("__BattleActionPlayer__");
        sInstance = obj.AddComponent<BattleActionPlayer>();
        DontDestroyOnLoad(obj);
        return sInstance;
    }

    private void Awake()
    {
        EventKit.Type.Register<UIEvents.SkillVisualRequestEvent>(OnSkillVisualRequest);
        EventKit.Type.Register<UnitDamagedEvent>(OnUnitDamaged);
        EventKit.Type.Register<UnitHealedEvent>(OnUnitHealed);
        LogKit.Log("[BattleActionPlayer] 初始化完成，已注册技能表现与伤害/治疗音效事件监听");
    }

    private void OnDestroy()
    {
        EventKit.Type.UnRegister<UIEvents.SkillVisualRequestEvent>(OnSkillVisualRequest);
        EventKit.Type.UnRegister<UnitDamagedEvent>(OnUnitDamaged);
        EventKit.Type.UnRegister<UnitHealedEvent>(OnUnitHealed);
        StopAllCoroutines();
        ClearAvatarSkillGlow();
        ClearBattleEffects();

        // 销毁所有未完成的卡牌飞入容器（防止战斗结束后HandCardFlyIn残留）
        foreach (var container in mHandCardFlyInContainers)
        {
            if (container != null)
                Destroy(container);
        }
        mHandCardFlyInContainers.Clear();

        // 兜底清理：Canvas上可能有因协程异常或对象卸载残留的HandCardFlyIn
        CleanupOrphanedHandCardFlyIn();

        mUnitViews.Clear();
        mHandCardsPanel = null;
        mEnemyUnitsPanel = null;
        mPlayerUnitsPanel = null;
        mDealerEffectCoroutine = null;
        sInstance = null;
    }

    /// <summary>
    /// 兜底清理：遍历所有Canvas查找并销毁名称为HandCardFlyIn的孤儿对象。
    /// </summary>
    private void CleanupOrphanedHandCardFlyIn()
    {
        try
        {
            Canvas[] allCanvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in allCanvases)
            {
                if (canvas == null) continue;
                for (int i = canvas.transform.childCount - 1; i >= 0; i--)
                {
                    Transform child = canvas.transform.GetChild(i);
                    if (child != null && child.name.StartsWith("HandCardFlyIn"))
                    {
                        Destroy(child.gameObject);
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            LogKit.Warning($"[BattleActionPlayer] CleanupOrphanedHandCardFlyIn异常: {e.Message}");
        }
    }

    /// <summary>
    /// 单位受到伤害时播放命中音效。
    /// </summary>
    private void OnUnitDamaged(UnitDamagedEvent evt)
    {
        AudioManager.Instance.PlaySFX(AudioManager.SFX_Hit);
    }

    /// <summary>
    /// 单位被治疗时播放治疗音效。
    /// </summary>
    private void OnUnitHealed(UnitHealedEvent evt)
    {
        AudioManager.Instance.PlaySFX(AudioManager.SFX_Heal);
    }

    public static void DestroySelf()
    {
        if (sInstance != null)
        {
            Destroy(sInstance.gameObject);
        }
    }

    #region 单位视图注册

    public void RegisterUnitView(string unitId, RectTransform rect)
    {
        if (!string.IsNullOrEmpty(unitId) && rect != null)
        {
            mUnitViews[unitId] = rect;
        }
    }

    public void UnregisterUnitView(string unitId)
    {
        if (!string.IsNullOrEmpty(unitId))
        {
            mUnitViews.Remove(unitId);
        }
    }

    public void ClearUnitViews()
    {
        mUnitViews.Clear();
    }

    /// <summary>
    /// 注册敌方/我方面板引用，供字典查找失败时直接遍历面板子物体
    /// </summary>
    public void RegisterUnitPanels(Transform enemyPanel, Transform playerPanel)
    {
        mEnemyUnitsPanel = enemyPanel;
        mPlayerUnitsPanel = playerPanel;
    }

    /// <summary>
    /// 注册手牌面板引用，供手牌操作技能（Mage_Skill2/Mage_Ultimate）的卡牌飞入表现使用
    /// </summary>
    public void RegisterHandCardsPanel(Transform handPanel)
    {
        mHandCardsPanel = handPanel;
    }

    /// <summary>
    /// 获取单位世界坐标位置。
    /// 直接扫描敌方/我方面板的直接子物体，不依赖字典、不递归，避免过期条目和嵌套匹配问题。
    /// </summary>
    private Vector3 GetUnitWorldPosition(string unitId)
    {
        // 1. 搜索敌方面板直接子物体
        Vector3 pos = FindDirectChildPosition(mEnemyUnitsPanel, unitId);
        if (pos != Vector3.zero) return pos;

        // 2. 搜索我方面板直接子物体
        pos = FindDirectChildPosition(mPlayerUnitsPanel, unitId);
        if (pos != Vector3.zero) return pos;

        LogKit.Warning($"[BattleActionPlayer] 无法获取单位 {unitId} 的位置（面板直接子物体未找到）");
        return Vector3.zero;
    }

    /// <summary>
    /// 在指定面板的直接子物体中按名称查找位置（不递归）
    /// </summary>
    private Vector3 FindDirectChildPosition(Transform panel, string unitId)
    {
        RectTransform rect = FindDirectChildTransform(panel, unitId);
        return rect != null ? rect.position : Vector3.zero;
    }

    /// <summary>
    /// 在指定面板的直接子物体中按名称查找Transform（不递归）
    /// 用于需要获取Transform引用的场景（如近战动画需要移动头像）
    /// </summary>
    private RectTransform FindDirectChildTransform(Transform panel, string unitId)
    {
        if (panel == null || string.IsNullOrEmpty(unitId)) return null;

        int count = panel.childCount;
        for (int i = 0; i < count; i++)
        {
            Transform child = panel.GetChild(i);
            if (child == null) continue;
            if (child.name == unitId && child.gameObject.activeInHierarchy)
            {
                return child as RectTransform;
            }
        }
        return null;
    }

    /// <summary>
    /// 收集角色单位容器中所有可移动的子物体（不移动整个unit容器）
    /// 包括：IconBg(头像)、HpBarBg(血条底)、HpBar(血条)、HpText(血条文本)
    /// </summary>
    private Image FindUnitAvatar(string unitId)
    {
        RectTransform unitRoot = FindDirectChildTransform(mEnemyUnitsPanel, unitId)
            ?? FindDirectChildTransform(mPlayerUnitsPanel, unitId);
        return unitRoot?.Find("IconBg/Icon")?.GetComponent<Image>();
    }

    private void PlayAvatarSkillGlow(string unitId, string skillId)
    {
        ClearAvatarSkillGlow();

        var avatar = FindUnitAvatar(unitId);
        if (avatar == null)
            return;

        if (mAvatarGlowMaterialTemplate == null)
        {
            mAvatarGlowMaterialTemplate = ResKit.Load<Material>("UI_AvatarSkillGlow");
#if UNITY_EDITOR
            mAvatarGlowMaterialTemplate ??= AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/UIEffects/UI_AvatarSkillGlow.mat");
#endif
        }
        if (mAvatarGlowMaterialTemplate == null)
        {
            LogKit.Error("[BattleActionPlayer] 未找到 UI_AvatarSkillGlow；请确认 YooAsset 已构建 UIEffects Collector，或编辑器资源路径存在。");
            return;
        }

        mGlowingAvatar = avatar;
        mAvatarGlowMaterial = new Material(mAvatarGlowMaterialTemplate);
        mAvatarGlowMaterial.SetColor("_GlowColor", GetSkillGlowColor(skillId));
        mAvatarGlowMaterial.SetFloat("_SkillGlow", 0f);
        avatar.material = mAvatarGlowMaterial;
        mAvatarGlowCoroutine = StartCoroutine(AnimateAvatarSkillGlow());
    }

    private Color GetSkillGlowColor(string skillId)
    {
        if (skillId.Contains("Ultimate")) return new Color(1f, 0.78f, 0.2f);
        if (skillId.Contains("Mage") || skillId.Contains("Healer")) return new Color(0.35f, 0.8f, 1f);
        if (skillId.Contains("Archer")) return new Color(0.45f, 1f, 0.55f);
        return new Color(1f, 0.42f, 0.2f);
    }

    private IEnumerator AnimateAvatarSkillGlow()
    {
        const float fadeInDuration = 0.12f;
        const float holdDuration = 0.18f;
        const float fadeOutDuration = 0.25f;

        for (float elapsed = 0f; elapsed < fadeInDuration; elapsed += Time.deltaTime)
        {
            if (mAvatarGlowMaterial == null) yield break;
            mAvatarGlowMaterial.SetFloat("_SkillGlow", elapsed / fadeInDuration);
            yield return null;
        }

        if (mAvatarGlowMaterial != null)
            mAvatarGlowMaterial.SetFloat("_SkillGlow", 1f);
        yield return new WaitForSeconds(holdDuration);

        for (float elapsed = 0f; elapsed < fadeOutDuration; elapsed += Time.deltaTime)
        {
            if (mAvatarGlowMaterial == null) yield break;
            mAvatarGlowMaterial.SetFloat("_SkillGlow", 1f - elapsed / fadeOutDuration);
            yield return null;
        }

        if (mGlowingAvatar != null)
            mGlowingAvatar.material = null;
        if (mAvatarGlowMaterial != null)
            Destroy(mAvatarGlowMaterial);
        mAvatarGlowCoroutine = null;
        mGlowingAvatar = null;
        mAvatarGlowMaterial = null;
    }

    private void ClearAvatarSkillGlow()
    {
        if (mAvatarGlowCoroutine != null)
        {
            StopCoroutine(mAvatarGlowCoroutine);
            mAvatarGlowCoroutine = null;
        }

        if (mGlowingAvatar != null)
            mGlowingAvatar.material = null;
        if (mAvatarGlowMaterial != null)
            Destroy(mAvatarGlowMaterial);

        mGlowingAvatar = null;
        mAvatarGlowMaterial = null;
    }

    private List<(RectTransform rect, Vector3 originalPos)> CollectMovableChildren(Transform unitContainer)
    {
        var result = new List<(RectTransform, Vector3)>();
        if (unitContainer == null) return result;

        Transform icon = unitContainer.Find("IconBg/Icon");
        RectTransform iconRect = icon as RectTransform;
        if (iconRect != null)
        {
            result.Add((iconRect, iconRect.position));
        }

        return result;
    }

    /// <summary>
    /// 计算所有可移动子物体的中心位置（用IconBg代表中心）
    /// </summary>
    private Vector3 GetChildrenCenter(List<(RectTransform rect, Vector3 originalPos)> children)
    {
        // 优先用IconBg作为参考中心（角色视觉的核心位置）
        foreach (var item in children)
        {
            if (item.rect != null && item.rect.name == "IconBg")
            {
                return item.rect.position;
            }
        }
        // 没有IconBg则取平均中心
        if (children.Count == 0) return Vector3.zero;
        Vector3 sum = Vector3.zero;
        foreach (var item in children) sum += item.rect.position;
        return sum / children.Count;
    }

    /// <summary>
    /// 移动所有可移动子物体到目标位置（基于IconBg中心对齐）
    /// </summary>
    private void MoveChildrenToTarget(List<(RectTransform rect, Vector3 originalPos)> children, Vector3 centerTarget, Vector3 centerOriginal)
    {
        Vector3 offset = centerTarget - centerOriginal;
        foreach (var item in children)
        {
            if (item.rect == null) continue;
            item.rect.position = item.originalPos + offset;
        }
    }

    /// <summary>
    /// 获取战斗特效根节点。退出战斗时会统一销毁该节点及其所有子物体。
    /// </summary>
    private Transform GetEffectParent()
    {
        if (mEffectRoot != null)
            return mEffectRoot;

        Canvas canvas = FindObjectOfType<Canvas>();
        GameObject root = new GameObject("__BattleEffects__");
        mEffectRoot = root.transform;
        mEffectRoot.SetParent(canvas != null ? canvas.transform : transform, false);
        return mEffectRoot;
    }

    private void ClearBattleEffects()
    {
        foreach (var effect in mAttachedBattleEffects)
        {
            if (effect != null)
                Destroy(effect);
        }
        mAttachedBattleEffects.Clear();

        if (mEffectRoot != null)
            Destroy(mEffectRoot.gameObject);
        mEffectRoot = null;
    }

    /// <summary>
    /// 剥离技能卡牌的数字索引后缀
    /// 例: "Warrior_Skill1_1" → "Warrior_Skill1", "Warrior_Skill2_2" → "Warrior_Skill2"
    /// 不影响: "Warrior_Skill1" → "Warrior_Skill1", "Warrior_Ultimate" → "Warrior_Ultimate"
    /// </summary>
    private string StripCardIndexSuffix(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return skillId;
        int lastUnderscore = skillId.LastIndexOf('_');
        if (lastUnderscore > 0 && int.TryParse(skillId.Substring(lastUnderscore + 1), out int index))
        {
            return skillId.Substring(0, lastUnderscore);
        }
        return skillId;
    }

    #endregion

    #region 表现事件处理

    private void OnSkillVisualRequest(UIEvents.SkillVisualRequestEvent evt)
    {
        if (mCurrentCoroutine != null)
            StopCoroutine(mCurrentCoroutine);

        mCurrentCoroutine = StartCoroutine(PlaySkillVisual(evt));
    }

    /// <summary>
    /// 按技能类型播放释放音效：
    /// - 近战攻击（剑/匕首等）统一音效 SFX_AttackMelee
    /// - 远程箭矢 SFX_AttackArrow
    /// - 远程法球 SFX_AttackMagic
    /// - 增益/治疗技能 SFX_Buff
    /// 普通攻击与技能攻击在同类型下音效统一。
    /// 敌人技能同样按此分类。
    /// </summary>
    private void PlaySkillCastSfx(string baseSkillId)
    {
        string sfx;
        switch (baseSkillId)
        {
            // 远程法球
            case "Mage_NormalAttack":
            case "Healer_NormalAttack":
                sfx = AudioManager.SFX_AttackMagic;
                break;
            // 敌人远程攻击（法球/投掷/龙息等）→ 统一法球音效
            case "Goblin_Throw":
            case "Slime_Attack":
            case "DarkMage_Fireball":
            case "DarkMage_Curse":
            case "Dragon_Breath":
            case "Dragon_Roar":
                sfx = AudioManager.SFX_AttackMagic;
                break;
            // 友方增益/治疗/护盾 + 手牌操作
            case "Mage_Skill1":
            case "Archer_Skill1":
            case "Archer_Ultimate":   // 激励号令：全队增益
            case "Healer_Skill1":
            case "Healer_Skill2":
            case "Healer_Ultimate":
            case "Knight_Skill1":     // 圣盾庇护：全体护盾
            case "Knight_Skill2":     // 圣盾守护：单体护盾
            case "Mage_Skill2":
            case "Mage_Ultimate":
                sfx = AudioManager.SFX_Buff;
                break;
            // 远程箭矢
            case "Archer_NormalAttack":
            case "Archer_Skill2":     // 穿甲箭：远程伤害
                sfx = AudioManager.SFX_AttackArrow;
                break;
            // 近战（默认）：战士/枪兵/刺客/骑士 + 敌人近战(Goblin_Stab/Skeleton_SwordSlash/Dragon_TailWhip)
            default:
                sfx = AudioManager.SFX_AttackMelee;
                break;
        }
        AudioManager.Instance.PlaySFX(sfx);
    }

    private IEnumerator PlaySkillVisual(UIEvents.SkillVisualRequestEvent evt)
    {
        // 剥离卡牌索引后缀（如 "Warrior_Skill1_1" → "Warrior_Skill1"），确保switch正确匹配
        string baseSkillId = StripCardIndexSuffix(evt.SkillId);

        // 播放技能/攻击释放音效（按类型分类）
        PlaySkillCastSfx(baseSkillId);
        PlayAvatarSkillGlow(evt.AttackerUnitId, baseSkillId);

        // 判断攻击者阵营，决定面板方向
        bool isEnemyAttacker = IsEnemyAttacker(evt.AttackerUnitId);

        // 自身定位技能（如治疗、增益）不播放攻击表现，直接执行效果
        bool isSelfTarget = evt.TargetUnitIds.Count > 0 && evt.TargetUnitIds[0] == evt.AttackerUnitId;

        LogKit.Log($"[BattleActionPlayer] 播放技能表现: {evt.SkillId}(剥离后:{baseSkillId}), 攻击者:{evt.AttackerUnitId}, 目标数:{evt.TargetUnitIds.Count}, 自身定位:{isSelfTarget}, 敌方攻击:{isEnemyAttacker}");

        // === 敌人技能视觉表现分支 ===
        if (isEnemyAttacker)
        {
            EnemySkillVisualType enemyVisualType = DetermineEnemySkillVisualType(baseSkillId);

            // 自身定位的增益技能：逐个快速命中，不播放动画
            if (isSelfTarget)
            {
                for (int _ti = 0; _ti < evt.TargetUnitIds.Count; _ti++)
                {
                    EventKit.Type.Send(new UIEvents.SkillVisualHitEvent { TargetUnitId = evt.TargetUnitIds[_ti], TargetUnitIndex = _ti < evt.TargetUnitIndices.Count ? evt.TargetUnitIndices[_ti] : -1 });
                    yield return new WaitForSeconds(0.1f);
                }
                EventKit.Type.Send(new UIEvents.SkillVisualCompleteEvent());
                mCurrentCoroutine = null;
                yield break;
            }

            // 敌人攻击：攻击者面板=敌方面板，目标面板=我方面板
            // 敌人增益：攻击者面板=敌方面板，目标面板=敌方面板（友军）
            Transform enemyAttackerPanel = mEnemyUnitsPanel;
            Transform enemyTargetPanel = (enemyVisualType == EnemySkillVisualType.Buff)
                ? mEnemyUnitsPanel   // 增益目标为友军（敌方）
                : mPlayerUnitsPanel; // 攻击目标为玩家

            switch (enemyVisualType)
            {
                case EnemySkillVisualType.Melee:
                    yield return PlayEnemyMeleeAttack(evt, enemyTargetPanel);
                    break;
                case EnemySkillVisualType.Ranged:
                    yield return PlayProjectileAttack(evt, enemyAttackerPanel, enemyTargetPanel);
                    break;
                case EnemySkillVisualType.Buff:
                    yield return PlayAllyOrbAttack(evt, enemyAttackerPanel, enemyTargetPanel);
                    break;
            }

            EventKit.Type.Send(new UIEvents.SkillVisualCompleteEvent());
            mCurrentCoroutine = null;
            yield break;
        }

        // === 以下为玩家技能的现有逻辑 ===

        // 自身定位技能：根据技能类型决定表现方式
        // - Archer_Skill2 穿甲箭：远程弹道（switch中处理）
        // - Mage_Skill2/Mage_Ultimate：手牌操作类，卡牌飞入手牌区域（switch中处理）
        // - Knight_Skill1/Archer_Ultimate/Healer_Ultimate：全队增益/治疗/护盾，需走AllyOrb表现（switch中处理）
        if (isSelfTarget && baseSkillId != "Archer_Skill2"
            && baseSkillId != "Mage_Skill2" && baseSkillId != "Mage_Ultimate"
            && baseSkillId != "Knight_Skill1" && baseSkillId != "Archer_Ultimate"
            && baseSkillId != "Healer_Ultimate")
        {
            for (int _ti = 0; _ti < evt.TargetUnitIds.Count; _ti++)
            {
                EventKit.Type.Send(new UIEvents.SkillVisualHitEvent { TargetUnitId = evt.TargetUnitIds[_ti], TargetUnitIndex = _ti < evt.TargetUnitIndices.Count ? evt.TargetUnitIndices[_ti] : -1 });
                yield return new WaitForSeconds(0.1f);
            }
            EventKit.Type.Send(new UIEvents.SkillVisualCompleteEvent());
            mCurrentCoroutine = null;
            yield break;
        }

        // 根据技能ID选择不同的表现效果（使用剥离后缀的SkillId匹配）
        switch (baseSkillId)
        {
            // 弹道攻击（远程）
            case "Archer_NormalAttack":
            case "Archer_Skill2":    // 穿甲箭：远程伤害+减防
            case "Mage_NormalAttack":
            case "Healer_NormalAttack":
                yield return PlayProjectileAttack(evt, mPlayerUnitsPanel, mEnemyUnitsPanel);
                break;
            // 近战攻击（战士所有攻击 + 枪兵普攻 + 刺客所有攻击 + 骑士普攻/终结技）
            case "Warrior_NormalAttack":
            case "Warrior_Skill1":
            case "Warrior_Skill2":
            case "Warrior_Ultimate":
            case "Spearman_NormalAttack":
            case "Assassin_NormalAttack":
            case "Assassin_Skill1":
            case "Assassin_Skill2":
            case "Assassin_Ultimate":
            case "Knight_NormalAttack":
            case "Knight_Skill2":
            case "Knight_Ultimate":
                yield return PlayMeleeAttack(evt, mPlayerUnitsPanel, mEnemyUnitsPanel);
                break;
            // 友方增益/治疗/护盾法球（抛物线运动至友方目标）
            case "Mage_Skill1":      // 奥术增幅：友方攻击力提升
            case "Archer_Skill1":    // 急行军：友方行动提前+伤害提升
            case "Archer_Ultimate":  // 激励号令：全体友方攻击力+暴击伤害提升
            case "Healer_Skill1":    // 治疗术：友方治疗
            case "Healer_Ultimate":  // 神圣治愈：全体友方治疗
            case "Knight_Skill1":    // 圣盾庇护：全体友方护盾
                yield return PlayAllyOrbAttack(evt, mPlayerUnitsPanel, mPlayerUnitsPanel);
                break;
            // 敌方debuff法球（从玩家方飞向敌方）
            case "Healer_Skill2":    // 虚弱诅咒：敌方伤害降低
                yield return PlayAllyOrbAttack(evt, mPlayerUnitsPanel, mEnemyUnitsPanel);
                break;
            // 自身增益法球（环绕施法者旋转）
            // Assassin_Skill2 为自身刺杀状态，TargetType=Self，走isSelfTarget快速命中
            // 手牌操作技能（卡牌从屏幕外飞入手牌区域）
            case "Mage_Skill2":      // 奥术洞察：抽取2张手牌
            case "Mage_Ultimate":    // 奥术重构：弃置所有手牌并抽取等量
                yield return PlayHandCardEffect(evt);
                break;
            default:
                // 未定义表现的技能：逐个快速命中，不播放动画
                for (int _ti = 0; _ti < evt.TargetUnitIds.Count; _ti++)
                {
                    EventKit.Type.Send(new UIEvents.SkillVisualHitEvent { TargetUnitId = evt.TargetUnitIds[_ti], TargetUnitIndex = _ti < evt.TargetUnitIndices.Count ? evt.TargetUnitIndices[_ti] : -1 });
                    yield return new WaitForSeconds(0.1f);
                }
                break;
        }

        // 表现完成，通知逻辑层继续
        EventKit.Type.Send(new UIEvents.SkillVisualCompleteEvent());
        mCurrentCoroutine = null;
    }

    /// <summary>
    /// 加载技能表现资源（Sprite），支持多格式兜底（对齐 SpriteIconLoader 策略）。
    /// 资源路径: Resources/Battle/Effects/{SkillId}
    /// 例如: Archer_Ultimate → Battle/Effects/Archer_Ultimate
    /// 注意: 自动剥离卡牌索引后缀（如 Warrior_Skill1_1 → Warrior_Skill1）
    /// YooAsset 地址: Effects_{baseSkillId}（Battle 组使用 AddressByFolderAndFileName）
    /// </summary>
    private Sprite LoadSkillEffectSprite(string skillId)
    {
        if (string.IsNullOrEmpty(skillId)) return null;

        // 剥离卡牌索引后缀，确保资源路径正确
        string baseSkillId = StripCardIndexSuffix(skillId);
        string fullPath = $"Battle/Effects/{baseSkillId}";

        // 方式1: 使用 ResKit 加载 Sprite（YooAsset 地址 = "Effects_{baseSkillId}"）
        string yooAddress = $"Effects_{baseSkillId}";
        var sprite = ResKit.Load<Sprite>(yooAddress);
        if (sprite != null) return sprite;

        // 方式2: 使用 Unity Resources.Load 加载 Sprite（完整路径兜底）
        sprite = Resources.Load<Sprite>(fullPath);
        if (sprite != null) return sprite;

        // 方式3: 尝试加载 Texture2D 并转换为 Sprite (支持 jpg 等非 Sprite 格式)
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
            if (sprite != null)
            {
                LogKit.Log($"[BattleActionPlayer] 技能图片通过 Texture2D 兜底加载成功: {fullPath}");
                return sprite;
            }

            Object.Destroy(readableTexture);
        }

        LogKit.Warning($"[BattleActionPlayer] 技能图片加载失败: Resources/{fullPath} (尝试了 ResKit/Resources.Sprite/Resources.Texture2D)");
        return null;
    }

    #endregion

    #region 通用弹道攻击：发射飞行物（箭矢/法球/治疗球等）

    /// <summary>
    /// 通用弹道攻击表现：发射飞行物飞向目标，适用于所有有飞行物的攻击表现。
    /// 包括但不限于：弓箭手普攻(箭矢)、法师普攻(法球)、医疗师普攻(治疗球)、弓箭手终结技(箭雨)、敌人远程攻击等。
    ///
    /// 目标定位策略：
    /// - TargetUnitIndices有值：按索引精确定位（普攻单目标，解决同名敌人UnitId重复问题）
    /// - TargetUnitIndices为空：扫描目标面板所有可见单位（终结技/AoE多目标，确保弹道数=可见目标数）
    ///
    /// attackerPanel：攻击者所在面板（玩家=mPlayerUnitsPanel，敌人=mEnemyUnitsPanel）
    /// targetPanel：目标所在面板（与攻击者对面）
    ///
    /// 图片加载：根据SkillId从Resources/Battle/Effects/{SkillId}加载飞行物图片
    /// </summary>
    private IEnumerator PlayProjectileAttack(UIEvents.SkillVisualRequestEvent evt, Transform attackerPanel, Transform targetPanel)
    {
        LogKit.Log($"[BattleActionPlayer] {evt.SkillId} 开始: 攻击者={evt.AttackerUnitId}, 目标索引数={evt.TargetUnitIndices.Count}, 目标ID数={evt.TargetUnitIds.Count}");

        Vector3 attackerPos = GetUnitWorldPosition(evt.AttackerUnitId);
        if (attackerPos == Vector3.zero)
        {
            attackerPos = new Vector3(0, -200, 0);
            LogKit.Warning("[BattleActionPlayer] 无法获取攻击者位置，使用默认位置");
        }

        // 收集目标位置列表
        List<(string unitId, Vector3 position, int listIndex)> targets = new List<(string, Vector3, int)>();

        if (evt.TargetUnitIndices.Count > 0)
        {
            // 按索引精确定位（普攻单目标）
            for (int i = 0; i < evt.TargetUnitIndices.Count; i++)
            {
                int idx = evt.TargetUnitIndices[i];
                string id = i < evt.TargetUnitIds.Count ? evt.TargetUnitIds[i] : "";
                Vector3 pos = Vector3.zero;

                if (targetPanel != null && idx >= 0 && idx < targetPanel.childCount)
                {
                    RectTransform rect = targetPanel.GetChild(idx) as RectTransform;
                    if (rect != null) pos = rect.position;
                }

                // 索引失败时回退到按UnitId查找
                if (pos == Vector3.zero)
                    pos = FindDirectChildPosition(targetPanel, id);
                if (pos == Vector3.zero)
                {
                    pos = attackerPos + new Vector3(0, -150, 0);
                    LogKit.Warning($"[BattleActionPlayer] 目标 {id}(索引:{idx}) 位置获取失败，使用兜底位置");
                }

                targets.Add((id, pos, idx));
            }
        }
        else
        {
            // 无索引：扫描目标面板所有可见单位（终结技/AoE多目标）
            var visibleUnits = CollectVisibleUnits(targetPanel);
            foreach (var vu in visibleUnits)
                targets.Add((vu.unitId, vu.position, -1));
        }

        LogKit.Log($"[BattleActionPlayer] {evt.SkillId} 目标收集完成: 共 {targets.Count} 个, ID=[{string.Join(",", targets.ConvertAll(t => t.unitId))}]");

        Sprite projectileSprite = LoadSkillEffectSprite(evt.SkillId);

        Transform effectParent = GetEffectParent();
        GameObject container = new GameObject($"{evt.SkillId}_Projectiles");
        container.transform.SetParent(effectParent, false);

        Vector3 startOffset = new Vector3(0, 60, 0);
        List<Coroutine> flights = new List<Coroutine>();

        // 敌人投掷物在此基础上再放大2倍（玩家/盟友默认1倍）
        float projectileScale = (attackerPanel == mEnemyUnitsPanel) ? 2f : 1f;

        foreach (var target in targets)
        {
            GameObject projectile = CreateArrow(container.transform, attackerPos + startOffset, projectileSprite, projectileScale);
            flights.Add(StartCoroutine(FlyArrowToTarget(projectile, target.unitId, target.position, 0.5f, target.listIndex)));
        }

        LogKit.Log($"[BattleActionPlayer] {evt.SkillId} 弹道创建完成: 共 {targets.Count} 个飞行物");

        foreach (var cor in flights)
        {
            yield return cor;
        }

        yield return new WaitForSeconds(0.3f);

        Destroy(container);
    }

    #endregion

    #region 通用友方法球增益：抛物线运动至友方目标

    /// <summary>
    /// 通用友方增益/治疗法球表现：从攻击者位置发射法球，抛物线运动落到友方目标头像上。
    /// 适用于所有指定友方目标的技能（Mage_Skill1、Archer_Skill1、Healer_Skill1、Healer_Skill2、Healer_Ultimate、敌人增益）。
    ///
    /// 表现阶段：
    /// 1. 收集所有友方目标位置（单目标按索引精确定位，全体目标扫描友方面板）
    /// 2. 为每个目标创建一个法球飞行物
    /// 3. 法球沿抛物线轨迹飞行至目标（飞行时长0.6s）
    /// 4. 所有法球到达后触发命中结算
    /// 5. 短暂停顿后清理飞行物
    ///
    /// attackerPanel：攻击者所在面板（玩家=mPlayerUnitsPanel，敌人=mEnemyUnitsPanel）
    /// targetPanel：友方目标所在面板（与攻击者同阵营）
    ///
    /// 安全机制：每个yield点检查RectTransform有效性，丢失时发送剩余命中事件避免逻辑层卡死
    /// </summary>
    private IEnumerator PlayAllyOrbAttack(UIEvents.SkillVisualRequestEvent evt, Transform attackerPanel, Transform targetPanel)
    {
        // === 1. 获取攻击者位置 ===
        Vector3 attackerPos = GetUnitWorldPosition(evt.AttackerUnitId);
        if (attackerPos == Vector3.zero)
        {
            LogKit.Warning($"[BattleActionPlayer] 友方法球：无法找到攻击者 {evt.AttackerUnitId}，跳过表现");
            for (int _ti = 0; _ti < evt.TargetUnitIds.Count; _ti++)
            {
                EventKit.Type.Send(new UIEvents.SkillVisualHitEvent { TargetUnitId = evt.TargetUnitIds[_ti], TargetUnitIndex = _ti < evt.TargetUnitIndices.Count ? evt.TargetUnitIndices[_ti] : -1 });
            }
            yield break;
        }

        // === 2. 收集友方目标位置 ===
        List<(string unitId, Vector3 position, int listIndex)> targets = new List<(string unitId, Vector3 position, int listIndex)>();

        // 友方目标需要从目标面板查找（玩家增益→我方面板，敌人增益→敌方面板）
        if (evt.TargetUnitIndices.Count > 0 && targetPanel != null)
        {
            for (int i = 0; i < evt.TargetUnitIndices.Count; i++)
            {
                int idx = evt.TargetUnitIndices[i];
                string id = i < evt.TargetUnitIds.Count ? evt.TargetUnitIds[i] : "";
                Vector3 pos = Vector3.zero;

                if (idx >= 0 && idx < targetPanel.childCount)
                {
                    RectTransform rect = targetPanel.GetChild(idx) as RectTransform;
                    if (rect != null) pos = rect.position;
                }
                if (pos == Vector3.zero)
                    pos = FindDirectChildPosition(targetPanel, id);
                if (pos == Vector3.zero)
                    pos = attackerPos + new Vector3(0, 150, 0);

                targets.Add((id, pos, idx));
            }
        }
        else if (evt.TargetUnitIds.Count > 0)
        {
            // 单目标友方技能（如 Ally 定位）通过 TargetUnitIds 查找
            for (int i = 0; i < evt.TargetUnitIds.Count; i++)
            {
                string targetId = evt.TargetUnitIds[i];
                int idx = i < evt.TargetUnitIndices.Count ? evt.TargetUnitIndices[i] : -1;
                Vector3 pos = FindDirectChildPosition(targetPanel, targetId);
                if (pos == Vector3.zero)
                    pos = attackerPos + new Vector3(0, 150, 0);
                targets.Add((targetId, pos, idx));
            }
        }
        else
        {
            // 无明确目标索引：扫描目标面板所有可见友方（如 AllAllies）
            var visibleUnits = CollectVisibleUnits(targetPanel);
            foreach (var vu in visibleUnits)
                targets.Add((vu.unitId, vu.position, -1));
        }

        if (targets.Count == 0)
        {
            LogKit.Warning("[BattleActionPlayer] 友方法球：无目标，跳过表现");
            yield break;
        }

        // === 3. 加载法球Sprite ===
        Sprite orbSprite = LoadSkillEffectSprite(evt.SkillId);

        // === 4. 创建容器和法球 ===
        GameObject container = new GameObject($"AllyOrb_{evt.SkillId}");
        container.transform.SetParent(GetEffectParent(), false);

        List<Coroutine> flights = new List<Coroutine>();
        Vector3 startOffset = new Vector3(0, 30, 0);

        foreach (var target in targets)
        {
            GameObject orb = CreateArrow(container.transform, attackerPos + startOffset, orbSprite, 1f);
            flights.Add(StartCoroutine(FlyOrbToAllyParabolic(orb, target.unitId, target.position, 0.6f, target.listIndex)));
        }

        LogKit.Log($"[BattleActionPlayer] {evt.SkillId} 友方法球创建完成: 共 {targets.Count} 个法球");

        // === 5. 等待所有法球到达 ===
        foreach (var cor in flights)
        {
            yield return cor;
        }

        // === 6. 短暂停顿后清理 ===
        yield return new WaitForSeconds(0.3f);

        Destroy(container);

        LogKit.Log($"[BattleActionPlayer] {evt.SkillId} 友方法球表现完成");
    }

    /// <summary>
    /// 法球沿抛物线轨迹飞行至友方目标。
    /// 抛物线高度：根据起点终点距离动态计算，最高点在两者中点上方
    /// </summary>
    private IEnumerator FlyOrbToAllyParabolic(GameObject orb, string targetUnitId, Vector3 targetPos, float duration, int targetIndex = -1)
    {
        if (orb == null)
        {
            EventKit.Type.Send(new UIEvents.SkillVisualHitEvent { TargetUnitId = targetUnitId, TargetUnitIndex = targetIndex });
            yield break;
        }

        Vector3 startPos = orb.transform.position;
        Vector3 midPos = (startPos + targetPos) * 0.5f;

        // 抛物线高度：距离的25%，最小50
        float arcHeight = Mathf.Max(50f, Vector3.Distance(startPos, targetPos) * 0.25f);
        midPos.y += arcHeight;

        float elapsed = 0f;
        while (elapsed < duration && orb != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            // 二次贝塞尔曲线：B(t) = (1-t)²·P0 + 2(1-t)t·P1 + t²·P2
            float oneMinusT = 1f - t;
            Vector3 pos = oneMinusT * oneMinusT * startPos
                        + 2f * oneMinusT * t * midPos
                        + t * t * targetPos;

            orb.transform.position = pos;
            yield return null;
        }

        // === 命中结算 ===
        if (orb != null) orb.SetActive(false);
        EventKit.Type.Send(new UIEvents.SkillVisualHitEvent { TargetUnitId = targetUnitId, TargetUnitIndex = targetIndex });
    }

    /// <summary>
    /// 扫描我方面板，收集所有可见友方单位的UnitId和位置
    /// 用于 AllAllies 类型技能（如 Healer_Ultimate 神圣治愈）
    /// </summary>
    private List<(string unitId, Vector3 position)> CollectVisibleAllies()
    {
        var result = new List<(string unitId, Vector3 position)>();

        if (mPlayerUnitsPanel == null)
        {
            LogKit.Warning("[BattleActionPlayer] mPlayerUnitsPanel为null，无法收集友方位置");
            return result;
        }

        int count = mPlayerUnitsPanel.childCount;
        for (int i = 0; i < count; i++)
        {
            Transform child = mPlayerUnitsPanel.GetChild(i);
            if (child == null || !child.gameObject.activeInHierarchy) continue;

            RectTransform rect = child as RectTransform;
            if (rect != null)
            {
                result.Add((child.name, rect.position));
            }
        }

        LogKit.Log($"[BattleActionPlayer] 收集到 {result.Count} 个可见友方单位");
        return result;
    }

    #endregion

    #region 通用自身增益法球：环绕施法者旋转

    /// <summary>
    /// 通用自身增益法球表现：法球从施法者头顶位置开始，顺时针绕施法者头像高速旋转3圈，触发增益效果。
    /// 适用于所有自身定位的增益技能（如 Archer_Skill2 致命射击）。
    ///
    /// 表现阶段：
    /// 1. 获取施法者位置
    /// 2. 在施法者头顶生成法球
    /// 3. 法球顺时针绕施法者头像旋转3圈（旋转半径50px，时长0.9s，每圈0.3s）
    /// 4. 旋转结束后触发命中结算
    /// 5. 短暂停顿后清理
    ///
    /// 安全机制：每帧检查RectTransform有效性，丢失时发送命中事件避免逻辑层卡死
    /// </summary>
    private IEnumerator PlaySelfBuffOrbitAttack(UIEvents.SkillVisualRequestEvent evt)
    {
        // === 1. 获取施法者位置 ===
        Vector3 casterPos = GetUnitWorldPosition(evt.AttackerUnitId);
        if (casterPos == Vector3.zero)
        {
            LogKit.Warning($"[BattleActionPlayer] 自身增益：无法找到施法者 {evt.AttackerUnitId}，跳过表现");
            for (int _ti = 0; _ti < evt.TargetUnitIds.Count; _ti++)
            {
                EventKit.Type.Send(new UIEvents.SkillVisualHitEvent { TargetUnitId = evt.TargetUnitIds[_ti], TargetUnitIndex = _ti < evt.TargetUnitIndices.Count ? evt.TargetUnitIndices[_ti] : -1 });
            }
            yield break;
        }

        // === 2. 加载法球Sprite ===
        Sprite orbSprite = LoadSkillEffectSprite(evt.SkillId);

        // === 3. 创建容器和法球 ===
        GameObject container = new GameObject($"SelfOrbit_{evt.SkillId}");
        container.transform.SetParent(GetEffectParent(), false);

        // 法球初始位置：施法者头顶正上方50px（旋转半径）
        float orbitRadius = 50f;
        Vector3 startPos = casterPos + new Vector3(0, orbitRadius, 0);
        GameObject orb = CreateArrow(container.transform, startPos, orbSprite, 1f);

        LogKit.Log($"[BattleActionPlayer] {evt.SkillId} 自身增益开始: 施法者={evt.AttackerUnitId}, 旋转3圈");

        // === 4. 顺时针旋转3圈 ===
        float totalDuration = 0.9f;      // 总时长0.9s
        float angularSpeed = 360f * 3f / totalDuration;  // 每秒度数：3圈/0.9s = 1200°/s
        float elapsed = 0f;

        while (elapsed < totalDuration)
        {
            if (orb == null)
            {
                LogKit.Warning("[BattleActionPlayer] 法球丢失(旋转阶段)");
                EventKit.Type.Send(new UIEvents.SkillVisualHitEvent { TargetUnitId = evt.AttackerUnitId });
                yield break;
            }

            elapsed += Time.deltaTime;
            // 顺时针角度（Unity中Z轴负方向旋转为顺时针）
            float currentAngle = -angularSpeed * elapsed * Mathf.Deg2Rad;

            // 法球位置：施法者中心 + 旋转半径 * (sin(angle), cos(angle))
            // 初始 angle=0 时位置在正上方(0, radius)，顺时针旋转
            float offsetX = Mathf.Sin(currentAngle) * orbitRadius;
            float offsetY = Mathf.Cos(currentAngle) * orbitRadius;
            orb.transform.position = casterPos + new Vector3(offsetX, offsetY, 0);

            yield return null;
        }

        // === 5. 触发命中结算 ===
        if (orb != null) orb.SetActive(false);
        for (int _ti = 0; _ti < evt.TargetUnitIds.Count; _ti++)
        {
            EventKit.Type.Send(new UIEvents.SkillVisualHitEvent { TargetUnitId = evt.TargetUnitIds[_ti], TargetUnitIndex = _ti < evt.TargetUnitIndices.Count ? evt.TargetUnitIndices[_ti] : -1 });
        }

        LogKit.Log($"[BattleActionPlayer] {evt.SkillId} 增益效果触发");

        // === 6. 短暂停顿后清理 ===
        yield return new WaitForSeconds(0.3f);
        Destroy(container);

        LogKit.Log($"[BattleActionPlayer] {evt.SkillId} 自身增益表现完成");
    }

    #endregion

    /// <summary>
    /// 直接扫描敌方面板，收集当前在场的所有可见敌人的UnitId和位置
    /// 不依赖数据层状态，确保弹道数始终等于面板上可见的敌人数
    /// </summary>
    private List<(string unitId, Vector3 position)> CollectVisibleEnemies()
    {
        var result = new List<(string unitId, Vector3 position)>();

        if (mEnemyUnitsPanel == null)
        {
            LogKit.Warning("[BattleActionPlayer] mEnemyUnitsPanel为null，无法收集敌人位置");
            return result;
        }

        foreach (Transform child in mEnemyUnitsPanel)
        {
            if (child == null) continue;
            if (!child.gameObject.activeInHierarchy) continue;

            string unitId = child.name;
            if (string.IsNullOrEmpty(unitId)) continue;

            RectTransform rect = child as RectTransform;
            if (rect == null) continue;

            result.Add((unitId, rect.position));
        }

        return result;
    }

    #region 敌人技能视觉表现辅助

    /// <summary>
    /// 敌人技能视觉表现类型
    /// </summary>
    private enum EnemySkillVisualType
    {
        Melee,   // 近战攻击（套用玩家近战表现）
        Ranged,  // 远程攻击（套用玩家远程弹道表现）
        Buff     // 增益技能（套用玩家增益法球表现）
    }

    /// <summary>
    /// 判断攻击者是否为敌人（通过在敌方面板查找）
    /// </summary>
    private bool IsEnemyAttacker(string attackerUnitId)
    {
        if (string.IsNullOrEmpty(attackerUnitId)) return false;
        return FindDirectChildTransform(mEnemyUnitsPanel, attackerUnitId) != null;
    }

    /// <summary>
    /// 根据攻击者所在面板，返回对面面板（目标所在面板）
    /// </summary>
    private Transform GetOppositePanel(Transform attackerPanel)
    {
        return attackerPanel == mEnemyUnitsPanel ? mPlayerUnitsPanel : mEnemyUnitsPanel;
    }

    /// <summary>
    /// 根据技能ID判断敌人技能的视觉表现类型。
    /// 通过SkillId中"怪物名_技能名"格式判断具体的远程/近战/增益。
    /// - Goblin_Stab/Skeleton_SwordSlash/Dragon_TailWhip → 近战
    /// - Goblin_Throw/Slime_Attack/DarkMage_Fireball/DarkMage_Curse/Dragon_Breath/Dragon_Roar → 远程
    /// - 其他敌人对自身/友军的增益技能 → 增益
    /// </summary>
    private EnemySkillVisualType DetermineEnemySkillVisualType(string skillId)
    {
        switch (skillId)
        {
            // 近战攻击
            case "Goblin_Stab":         // 哥布林刺杀
            case "Skeleton_SwordSlash":  // 骷髅剑斩
            case "Dragon_TailWhip":     // 巨龙甩尾
                return EnemySkillVisualType.Melee;

            // 远程攻击
            case "Goblin_Throw":        // 哥布林投掷
            case "Slime_Attack":        // 史莱姆撞击
            case "DarkMage_Fireball":   // 黑暗法师暗火球
            case "DarkMage_Curse":      // 黑暗法师诅咒
            case "Dragon_Breath":       // 巨龙龙息
            case "Dragon_Roar":         // 巨龙龙吼
                return EnemySkillVisualType.Ranged;

            // 默认近战
            default:
                return EnemySkillVisualType.Melee;
        }
    }

    /// <summary>
    /// 通用收集指定面板上所有可见单位的位置
    /// </summary>
    private List<(string unitId, Vector3 position)> CollectVisibleUnits(Transform panel)
    {
        var result = new List<(string unitId, Vector3 position)>();
        if (panel == null)
        {
            LogKit.Warning($"[BattleActionPlayer] 面板为null，无法收集可见单位");
            return result;
        }

        foreach (Transform child in panel)
        {
            if (child == null) continue;
            if (!child.gameObject.activeInHierarchy) continue;

            string unitId = child.name;
            if (string.IsNullOrEmpty(unitId)) continue;

            RectTransform rect = child as RectTransform;
            if (rect == null) continue;

            result.Add((unitId, rect.position));
        }

        return result;
    }

    #endregion

    #region 敌人近战攻击：独立移动图片+武器挥动

    /// <summary>
    /// 敌人近战攻击表现：创建一张被 Resources/Sprites/Enemy/{EnemyId} 赋值的独立图片，
    /// 从攻击者位置移动至目标70%距离，在图片旁生成武器并挥动，挥动完成后才造成伤害，然后返回原位并销毁。
    ///
    /// 表现阶段：
    /// 1. 获取攻击者位置，加载敌人头像Sprite（Resources/Sprites/Enemy/{EnemyId}）
    /// 2. 在攻击者位置创建独立移动图片（不依赖敌人单位容器的子物体结构）
    /// 3. 收集目标位置，计算目标中心
    /// 4. 移动图片至距离目标中心70%的位置（0.3s）
    /// 5. 停顿0.2s，在图片旁生成武器
    /// 6. 停顿0.2s，挥舞武器劈砍（0.3s）
    /// 7. 劈砍完毕后发送命中事件（造成伤害）
    /// 8. 停顿0.2s，移动图片返回原位
    /// 9. 销毁图片和武器
    /// </summary>
    private IEnumerator PlayEnemyMeleeAttack(UIEvents.SkillVisualRequestEvent evt, Transform targetPanel)
    {
        // === 1. 获取攻击者位置 ===
        Vector3 attackerPos = GetUnitWorldPosition(evt.AttackerUnitId);
        if (attackerPos == Vector3.zero)
        {
            LogKit.Warning($"[BattleActionPlayer] 敌人近战：无法找到攻击者 {evt.AttackerUnitId}，跳过表现");
            for (int _ti = 0; _ti < evt.TargetUnitIds.Count; _ti++)
            {
                EventKit.Type.Send(new UIEvents.SkillVisualHitEvent { TargetUnitId = evt.TargetUnitIds[_ti], TargetUnitIndex = _ti < evt.TargetUnitIndices.Count ? evt.TargetUnitIndices[_ti] : -1 });
            }
            yield break;
        }

        // === 2. 加载敌人头像Sprite ===
        Sprite enemySprite = SpriteIconLoader.LoadEnemyIcon(evt.AttackerUnitId);

        // === 3. 收集目标位置 ===
        List<(string unitId, Vector3 position, int listIndex)> targets = new List<(string, Vector3, int)>();

        if (evt.TargetUnitIndices.Count > 0)
        {
            for (int i = 0; i < evt.TargetUnitIndices.Count; i++)
            {
                int idx = evt.TargetUnitIndices[i];
                string id = i < evt.TargetUnitIds.Count ? evt.TargetUnitIds[i] : "";
                Vector3 pos = Vector3.zero;

                if (targetPanel != null && idx >= 0 && idx < targetPanel.childCount)
                {
                    RectTransform rect = targetPanel.GetChild(idx) as RectTransform;
                    if (rect != null) pos = rect.position;
                }
                if (pos == Vector3.zero)
                    pos = FindDirectChildPosition(targetPanel, id);
                if (pos == Vector3.zero)
                    pos = attackerPos + new Vector3(0, -150, 0);

                targets.Add((id, pos, idx));
            }
        }
        else
        {
            var visibleUnits = CollectVisibleUnits(targetPanel);
            foreach (var vu in visibleUnits)
                targets.Add((vu.unitId, vu.position, -1));
        }

        if (targets.Count == 0)
        {
            LogKit.Warning("[BattleActionPlayer] 敌人近战：无目标，跳过表现");
            yield break;
        }

        // === 4. 创建独立移动图片容器 ===
        Transform effectParent = GetEffectParent();
        GameObject moveImage = new GameObject($"EnemyMelee_{evt.AttackerUnitId}");
        moveImage.transform.SetParent(effectParent, false);
        moveImage.transform.position = attackerPos;

        RectTransform imgRect = moveImage.AddComponent<RectTransform>();
        // 图片尺寸80x80（与头像IconBg一致）
        float imgSize = 80f;
        imgRect.sizeDelta = new Vector2(imgSize, imgSize);

        Image img = moveImage.AddComponent<Image>();
        img.raycastTarget = false;

        if (enemySprite != null)
        {
            img.sprite = enemySprite;
            img.color = Color.white;
            // 保持图片宽高比
            float scale = Mathf.Min(imgSize / enemySprite.rect.width, imgSize / enemySprite.rect.height);
            imgRect.sizeDelta = new Vector2(enemySprite.rect.width * scale, enemySprite.rect.height * scale);
        }
        else
        {
            // 兜底：使用敌人配置颜色
            var enemyConfig = GameDataMgr.Instance.enemyConfigs?.Enemies?
                .FirstOrDefault(e => e.EnemyId == evt.AttackerUnitId);
            if (enemyConfig != null)
                img.color = new Color(enemyConfig.IconColorR, enemyConfig.IconColorG, enemyConfig.IconColorB, 1f);
            else
                img.color = Color.gray;
        }

        LogKit.Log($"[BattleActionPlayer] {evt.SkillId} 敌人近战开始: 攻击者={evt.AttackerUnitId}, 目标数={targets.Count}, Sprite={(enemySprite != null ? enemySprite.name : "无")}");

        Vector3 startPos = attackerPos;

        // === 5. 计算目标中心和移动位置（70%距离）===
        Vector3 targetCenter = Vector3.zero;
        foreach (var t in targets) targetCenter += t.position;
        targetCenter /= targets.Count;

        Vector3 movePos = Vector3.Lerp(startPos, targetCenter, 0.7f);

        // === 6. 第一阶段：移动图片至70%位置（0.3s）===
        float moveDuration = 0.3f;
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            if (moveImage == null) { LogKit.Warning("[BattleActionPlayer] 移动图片丢失(移动阶段)"); yield break; }
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            moveImage.transform.position = Vector3.Lerp(startPos, movePos, t);
            yield return null;
        }

        if (moveImage != null) moveImage.transform.position = movePos;

        // 停顿0.2s
        yield return new WaitForSeconds(0.2f);
        if (moveImage == null) { LogKit.Warning("[BattleActionPlayer] 移动图片丢失(移动后停顿)"); yield break; }

        // === 7. 第二阶段：在图片旁生成武器 ===
        Sprite weaponSprite = LoadSkillEffectSprite(evt.SkillId);
        GameObject weaponObj = CreateWeaponOnImage(moveImage.transform, targetCenter, weaponSprite);

        // 停顿0.2s（玩家能看见武器在抬起位置）
        yield return new WaitForSeconds(0.2f);
        if (moveImage == null) { if (weaponObj != null) Destroy(weaponObj); LogKit.Warning("[BattleActionPlayer] 移动图片丢失(武器生成后停顿)"); yield break; }

        // === 8. 第三阶段：挥舞武器劈砍（0.3s）===
        float swingDuration = 0.3f;
        elapsed = 0f;
        // 资源图片锋刃朝向左下角(225°)，旋转+45°使锋刃朝向正下方(270°)
        float baseRotation = 45f;
        float startSwing = -30f;
        float endSwing = 30f;

        while (elapsed < swingDuration)
        {
            if (moveImage == null || weaponObj == null) { if (weaponObj != null) Destroy(weaponObj); LogKit.Warning("[BattleActionPlayer] 丢失(挥舞阶段)"); yield break; }
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / swingDuration);
            float easedT = t * t;
            float currentSwing = Mathf.Lerp(startSwing, endSwing, easedT);
            weaponObj.transform.localRotation = Quaternion.Euler(0, 0, baseRotation + currentSwing);
            yield return null;
        }

        // === 9. 第四阶段：伤害判定结算（挥动完成后才造成伤害）===
        foreach (var target in targets)
        {
            EventKit.Type.Send(new UIEvents.SkillVisualHitEvent { TargetUnitId = target.unitId, TargetUnitIndex = target.listIndex });
        }

        LogKit.Log($"[BattleActionPlayer] {evt.SkillId} 敌人近战伤害结算完成，目标数={targets.Count}");

        // 停顿0.2s
        yield return new WaitForSeconds(0.2f);
        if (moveImage == null) { if (weaponObj != null) Destroy(weaponObj); yield break; }

        // === 10. 第五阶段：移动图片返回原位 ===
        Vector3 currentPos = moveImage.transform.position;
        elapsed = 0f;
        while (elapsed < moveDuration)
        {
            if (moveImage == null) { LogKit.Warning("[BattleActionPlayer] 移动图片丢失(回位阶段)"); yield break; }
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            moveImage.transform.position = Vector3.Lerp(currentPos, startPos, t);
            yield return null;
        }

        // === 11. 清理 ===
        if (weaponObj != null) Destroy(weaponObj);
        if (moveImage != null) Destroy(moveImage);

        LogKit.Log($"[BattleActionPlayer] {evt.SkillId} 敌人近战表现完成");
    }

    /// <summary>
    /// 在移动图片旁创建武器GameObject，作为移动图片的子物体。
    /// </summary>
    private GameObject CreateWeaponOnImage(Transform parent, Vector3 targetCenter, Sprite sprite)
    {
        GameObject weapon = new GameObject("EnemyMeleeWeapon");
        weapon.transform.SetParent(parent, false);

        RectTransform rect = weapon.AddComponent<RectTransform>();
        // 武器位置：图片正下方（玩家目标在下方，朝目标方向挥砍）
        rect.localPosition = new Vector3(0, -50, 0);

        float maxDimension = 160f;

        Image img = weapon.AddComponent<Image>();
        img.raycastTarget = false;

        if (sprite != null)
        {
            img.sprite = sprite;
            img.color = Color.white;
            float scale = Mathf.Min(maxDimension / sprite.rect.width, maxDimension / sprite.rect.height);
            rect.sizeDelta = new Vector2(sprite.rect.width * scale, sprite.rect.height * scale);
        }
        else
        {
            // 无图片兜底：灰色长条形（模拟武器形状，竖直方向）
            img.color = new Color(0.8f, 0.8f, 0.8f);
            rect.sizeDelta = new Vector2(maxDimension * 0.2f, maxDimension);
        }

        // 资源图片锋刃朝向左下角(225°)，旋转+45°使锋刃朝向正下方(270°)
        weapon.transform.localRotation = Quaternion.Euler(0, 0, 45f);

        return weapon;
    }

    #endregion

    #region 通用近战攻击：头像移动+武器劈砍

    /// <summary>
    /// 通用近战攻击表现：角色头像+血条移动至目标70%距离，生成武器劈砍后返回。
    /// 适用于所有近战攻击表现（普攻、近战技能、敌人近战等）。
    ///
    /// 表现阶段：
    /// 1. 可移动子物体（IconBg+HpBar+HpText）移动至距离目标70%位置（多目标时移至目标中心70%）
    /// 2. 停顿0.2s，在头像旁生成武器（剑锋朝向敌人）
    /// 3. 停顿0.2s，确保玩家能看见武器在抬起位置
    /// 4. 挥舞武器劈砍
    /// 5. 劈砍完毕后伤害判定结算
    /// 6. 停顿0.2s
    /// 7. 可移动子物体回到原位
    ///
    /// attackerPanel：攻击者所在面板（玩家=mPlayerUnitsPanel，敌人=mEnemyUnitsPanel）
    /// targetPanel：目标所在面板（与攻击者对面）
    ///
    /// 安全机制：每个yield点检查子物体引用是否仍存在，丢失时立即还原原始位置并优雅退出
    /// </summary>
    private IEnumerator PlayMeleeAttack(UIEvents.SkillVisualRequestEvent evt, Transform attackerPanel, Transform targetPanel)
    {
        // === 0. 缓存攻击者unit容器及可移动子物体（只移动IconBg+血条，不动整个unit）===
        RectTransform attackerContainer = FindDirectChildTransform(attackerPanel, evt.AttackerUnitId);
        if (attackerContainer == null)
        {
            LogKit.Warning($"[BattleActionPlayer] 近战表现：无法找到攻击者 {evt.AttackerUnitId}，直接触发命中结算");
            for (int i = 0; i < evt.TargetUnitIds.Count; i++)
            {
                EventKit.Type.Send(new UIEvents.SkillVisualHitEvent
                {
                    TargetUnitId = evt.TargetUnitIds[i],
                    TargetUnitIndex = i < evt.TargetUnitIndices.Count ? evt.TargetUnitIndices[i] : -1
                });
            }
            yield break;
        }

        // 收集可移动子物体及其原始位置
        var movableChildren = CollectMovableChildren(attackerContainer);
        if (movableChildren.Count == 0)
        {
            LogKit.Warning($"[BattleActionPlayer] 近战表现：无可移动子物体，直接触发命中结算");
            for (int i = 0; i < evt.TargetUnitIds.Count; i++)
            {
                EventKit.Type.Send(new UIEvents.SkillVisualHitEvent
                {
                    TargetUnitId = evt.TargetUnitIds[i],
                    TargetUnitIndex = i < evt.TargetUnitIndices.Count ? evt.TargetUnitIndices[i] : -1
                });
            }
            yield break;
        }

        Vector3 originalCenter = GetChildrenCenter(movableChildren);

        // 缓存所有可移动子物体的原始localScale（攻击时放大2倍，结束时还原）
        List<Vector3> originalScales = new List<Vector3>();
        foreach (var item in movableChildren)
        {
            originalScales.Add(item.rect != null ? item.rect.localScale : Vector3.one);
        }

        bool hasSentHitEvents = false;

        // 所有可移动子物体是否仍然有效
        bool ChildrenValid()
        {
            foreach (var item in movableChildren)
                if (item.rect == null) return false;
            return attackerContainer != null;
        }

        void SendHitEvents()
        {
            if (hasSentHitEvents) return;

            hasSentHitEvents = true;
            for (int i = 0; i < evt.TargetUnitIds.Count; i++)
            {
                EventKit.Type.Send(new UIEvents.SkillVisualHitEvent
                {
                    TargetUnitId = evt.TargetUnitIds[i],
                    TargetUnitIndex = i < evt.TargetUnitIndices.Count ? evt.TargetUnitIndices[i] : -1
                });
            }
        }

        // 还原所有子物体到原始位置和缩放（异常退出时调用）
        void RestorePositions()
        {
            for (int i = 0; i < movableChildren.Count; i++)
            {
                if (movableChildren[i].rect != null)
                {
                    movableChildren[i].rect.position = movableChildren[i].originalPos;
                    movableChildren[i].rect.localScale = originalScales[i];
                }
            }
        }

        // 设置所有可移动子物体的缩放
        void SetChildrenScale(float scaleMultiplier)
        {
            for (int i = 0; i < movableChildren.Count; i++)
            {
                if (movableChildren[i].rect != null)
                {
                    movableChildren[i].rect.localScale = originalScales[i] * scaleMultiplier;
                }
            }
        }

        // === 1. 收集目标位置 ===
        List<(string unitId, Vector3 position, int listIndex)> targets = new List<(string, Vector3, int)>();

        if (evt.TargetUnitIndices.Count > 0)
        {
            for (int i = 0; i < evt.TargetUnitIndices.Count; i++)
            {
                int idx = evt.TargetUnitIndices[i];
                string id = i < evt.TargetUnitIds.Count ? evt.TargetUnitIds[i] : "";
                Vector3 pos = Vector3.zero;

                if (targetPanel != null && idx >= 0 && idx < targetPanel.childCount)
                {
                    RectTransform rect = targetPanel.GetChild(idx) as RectTransform;
                    if (rect != null) pos = rect.position;
                }
                if (pos == Vector3.zero)
                    pos = FindDirectChildPosition(targetPanel, id);
                if (pos == Vector3.zero)
                    pos = originalCenter + new Vector3(0, 150, 0);

                targets.Add((id, pos, idx));
            }
        }
        else
        {
            var visibleUnits = CollectVisibleUnits(targetPanel);
            foreach (var vu in visibleUnits)
                targets.Add((vu.unitId, vu.position, -1));
        }

        if (targets.Count == 0)
        {
            LogKit.Warning("[BattleActionPlayer] 近战表现：无目标，直接触发命中结算");
            SendHitEvents();
            yield break;
        }

        // === 2. 计算目标中心和移动位置 ===
        Vector3 targetCenter = Vector3.zero;
        foreach (var t in targets) targetCenter += t.position;
        targetCenter /= targets.Count;

        Vector3 moveCenter = Vector3.Lerp(originalCenter, targetCenter, 0.7f);

        LogKit.Log($"[BattleActionPlayer] {evt.SkillId} 近战开始: 攻击者={evt.AttackerUnitId}, 目标数={targets.Count}, 移动至70%距离, 放大2倍");

        // === 3. 第一阶段：放大并移动头像至70%位置 ===
        SetChildrenScale(2f);
        float moveDuration = 0.3f;
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            if (!ChildrenValid()) { RestorePositions(); LogKit.Warning("[BattleActionPlayer] 子物体丢失(移动阶段)，降级为直接命中结算"); SendHitEvents(); yield break; }
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            Vector3 currentCenter = Vector3.Lerp(originalCenter, moveCenter, t);
            MoveChildrenToTarget(movableChildren, currentCenter, originalCenter);
            yield return null;
        }

        // 停顿0.2s
        yield return new WaitForSeconds(0.2f);
        if (!ChildrenValid()) { RestorePositions(); LogKit.Warning("[BattleActionPlayer] 子物体丢失(移动后停顿)，降级为直接命中结算"); SendHitEvents(); yield break; }

        // === 4. 第二阶段：生成武器（挂在头像背景下） ===
        Sprite weaponSprite = LoadSkillEffectSprite(evt.SkillId);
        RectTransform weaponParent = movableChildren[0].rect;
        GameObject weaponObj = CreateWeapon(weaponParent, targetCenter, weaponSprite);

        // 停顿0.2s（玩家能看见武器在抬起位置）
        yield return new WaitForSeconds(0.2f);
        if (!ChildrenValid()) { if (weaponObj != null) Destroy(weaponObj); RestorePositions(); LogKit.Warning("[BattleActionPlayer] 子物体丢失(武器生成后停顿)，降级为直接命中结算"); SendHitEvents(); yield break; }

        // === 5. 第三阶段：挥舞武器劈砍 ===
        // 武器初始剑锋朝正上方(90°)，CreateWeapon已旋转-135°作为基础角度
        // 挥舞：从抬起(-30°)劈砍到(+30°)，相对正上方左右摆动60°
        float swingDuration = 0.3f;
        elapsed = 0f;

        // 基础旋转角度（CreateWeapon设置的-135°），挥舞在此基础上叠加
        float baseRotation = -135f;
        // 挥舞起始角度：剑锋朝右上方（抬起准备劈砍）
        float startSwing = -30f;
        // 挥舞结束角度：剑锋朝左上方（完成劈砍）
        float endSwing = 30f;

        while (elapsed < swingDuration)
        {
            if (!ChildrenValid() || weaponObj == null) { if (weaponObj != null) Destroy(weaponObj); RestorePositions(); LogKit.Warning("[BattleActionPlayer] Transform丢失(挥舞阶段)，降级为直接命中结算"); SendHitEvents(); yield break; }
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / swingDuration);
            float easedT = t * t;
            float currentSwing = Mathf.Lerp(startSwing, endSwing, easedT);
            weaponObj.transform.localRotation = Quaternion.Euler(0, 0, baseRotation + currentSwing);
            yield return null;
        }

        // === 6. 第四阶段：伤害判定结算 ===
        SendHitEvents();

        LogKit.Log($"[BattleActionPlayer] {evt.SkillId} 伤害结算完成，目标数={targets.Count}");

        // 停顿0.2s
        yield return new WaitForSeconds(0.2f);
        if (!ChildrenValid()) { if (weaponObj != null) Destroy(weaponObj); RestorePositions(); LogKit.Warning("[BattleActionPlayer] 子物体丢失(结算后停顿)"); yield break; }

        // === 7. 第五阶段：可移动子物体回到原位 ===
        Vector3 currentCenterNow = GetChildrenCenter(movableChildren);
        elapsed = 0f;
        while (elapsed < moveDuration)
        {
            if (!ChildrenValid()) { RestorePositions(); LogKit.Warning("[BattleActionPlayer] 子物体丢失(回位阶段)"); yield break; }
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            Vector3 currentCenter = Vector3.Lerp(currentCenterNow, originalCenter, t);
            MoveChildrenToTarget(movableChildren, currentCenter, originalCenter);
            yield return null;
        }

        // 强制还原到精确原始位置和缩放（浮点累积误差修正）
        RestorePositions();

        // === 8. 清理 ===
        if (weaponObj != null) Destroy(weaponObj);

        LogKit.Log($"[BattleActionPlayer] {evt.SkillId} 近战表现完成");
    }

    /// <summary>
    /// 创建武器GameObject，作为攻击者头像的子物体
    /// 武器图片默认剑锋朝左下角(225°)，通过旋转-135°使剑锋朝向正上方(90°)
    /// </summary>
    private GameObject CreateWeapon(Transform parent, Vector3 targetCenter, Sprite sprite)
    {
        GameObject weapon = new GameObject("MeleeWeapon");
        weapon.transform.SetParent(parent, false);
        mAttachedBattleEffects.Add(weapon);

        RectTransform rect = weapon.AddComponent<RectTransform>();
        // 武器位置：攻击者头像正上方
        rect.localPosition = new Vector3(0, 50, 0);

        float maxDimension = 160f;  // 武器尺寸为原80的2.5倍

        Image img = weapon.AddComponent<Image>();
        img.raycastTarget = false;

        if (sprite != null)
        {
            img.sprite = sprite;
            img.color = Color.white;
            float scale = Mathf.Min(maxDimension / sprite.rect.width, maxDimension / sprite.rect.height);
            rect.sizeDelta = new Vector2(sprite.rect.width * scale, sprite.rect.height * scale);
        }
        else
        {
            // 无图片兜底：灰色长条形（模拟武器形状，竖直方向）
            img.color = new Color(0.8f, 0.8f, 0.8f);
            rect.sizeDelta = new Vector2(maxDimension * 0.2f, maxDimension);
        }

        // 图片剑锋朝左下角(225°)，旋转-135°使剑锋朝正上方(90°)
        // 注：挥舞逻辑会基于此基础角度进行±swing的旋转
        weapon.transform.localRotation = Quaternion.Euler(0, 0, -135f);

        return weapon;
    }

    #endregion

    /// <summary>
    /// 创建箭矢GameObject，优先使用传入的技能Sprite，无Sprite则使用纯色兜底
    /// 基础尺寸为原来3倍（原30x30 → 90x90），sizeMultiplier再做额外缩放（敌人投掷物传2倍 → 180x180）
    /// </summary>
    private GameObject CreateArrow(Transform parent, Vector3 startPos, Sprite sprite, float sizeMultiplier = 1f)
    {
        GameObject arrow = new GameObject("Arrow");
        arrow.transform.SetParent(parent, false);
        arrow.transform.position = startPos;

        RectTransform rect = arrow.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120 * sizeMultiplier, 18 * sizeMultiplier);  // 原40x6的3倍 × 额外缩放

        Image img = arrow.AddComponent<Image>();
        img.raycastTarget = false;

        if (sprite != null)
        {
            img.sprite = sprite;
            img.color = Color.white;
            // 基础90x90 × 额外缩放，保持宽高比（敌人: 90×2=180, 玩家: 90×1=90）
            float maxDimension = 90f * sizeMultiplier;
            float spriteWidth = sprite.rect.width;
            float spriteHeight = sprite.rect.height;
            float scaleFactor = Mathf.Min(maxDimension / spriteWidth, maxDimension / spriteHeight);
            rect.sizeDelta = new Vector2(spriteWidth * scaleFactor, spriteHeight * scaleFactor);
            LogKit.Log($"[BattleActionPlayer] 箭矢Sprite已赋值: {sprite.name}, 原始尺寸: {spriteWidth}x{spriteHeight}, 缩放后: {rect.sizeDelta}, 乘数: {sizeMultiplier}");
        }
        else
        {
            LogKit.Warning("[BattleActionPlayer] 箭矢Sprite为null，使用纯色兜底（橙黄色长条）");
            img.color = new Color(1f, 0.85f, 0.2f);
        }

        return arrow;
    }

    /// <summary>
    /// 箭矢飞向目标并命中
    /// </summary>
    private IEnumerator FlyArrowToTarget(GameObject arrow, string targetUnitId, Vector3 targetPos, float duration, int targetIndex = -1)
    {
        Vector3 startPos = arrow.transform.position;
        float elapsed = 0f;

        // 计算箭矢朝向角度
        // 箭矢图片默认朝左下角(225°方向)，需加135°偏移使其与目标方向对齐
        Vector3 direction = (targetPos - startPos);
        if (direction.sqrMagnitude > 0.01f)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg + 135f;
            arrow.transform.rotation = Quaternion.Euler(0, 0, angle);
        }

        // 飞行
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            // 使用缓动曲线让箭矢有加速感
            t = 1f - Mathf.Pow(1f - t, 2f);
            arrow.transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        // 箭矢到达目标，发送命中事件（触发伤害结算）
        EventKit.Type.Send(new UIEvents.SkillVisualHitEvent { TargetUnitId = targetUnitId, TargetUnitIndex = targetIndex });

        // 播放命中特效
        StartCoroutine(PlayHitEffect(arrow.transform.position));

        // 销毁箭矢
        Destroy(arrow);
    }

    /// <summary>
    /// 命中特效：橙色闪光扩散并淡出
    /// </summary>
    private IEnumerator PlayHitEffect(Vector3 pos)
    {
        Transform effectParent = GetEffectParent();

        GameObject hit = new GameObject("HitEffect");
        hit.transform.SetParent(effectParent, false);
        hit.transform.position = pos;

        RectTransform rect = hit.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(50, 50);

        Image img = hit.AddComponent<Image>();
        img.color = new Color(1f, 0.5f, 0f, 0.8f);
        img.raycastTarget = false;

        float duration = 0.25f;
        float elapsed = 0f;
        Vector3 startScale = Vector3.one;
        Vector3 endScale = Vector3.one * 1.8f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            img.color = new Color(1f, 0.5f, 0f, 0.8f * (1f - t));
            rect.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        Destroy(hit);
    }

    #region 手牌操作技能：卡牌飞入手牌区域

    /// <summary>
    /// 手牌操作技能表现：卡牌从屏幕外随机位置飞入手牌区域。
    /// 适用于 Mage_Skill2(奥术洞察：抽2张) 和 Mage_Ultimate(奥术重构：弃置所有并抽等量)。
    ///
    /// 表现阶段：
    /// 1. 确定飞入卡牌数量（Mage_Skill2固定2张，Mage_Ultimate为当前手牌数）
    /// 2. 播放卡牌飞入动画（FlyCardsToHandArea）
    /// 3. 发送命中事件（逻辑层执行实际抽牌/弃置抽牌）
    /// </summary>
    private IEnumerator PlayHandCardEffect(UIEvents.SkillVisualRequestEvent evt)
    {
        string baseSkillId = StripCardIndexSuffix(evt.SkillId);

        // === 1. 确定飞入卡牌数量 ===
        int cardCount;
        if (baseSkillId == "Mage_Skill2")
        {
            cardCount = 2;  // 奥术洞察：抽取2张
        }
        else  // Mage_Ultimate：弃置所有手牌并抽取等量
        {
            cardCount = CardHand.Instance != null ? CardHand.Instance.Count : 0;
            if (cardCount <= 0)
            {
                // 无手牌时直接执行效果，跳过飞入表现
                LogKit.Log($"[BattleActionPlayer] {evt.SkillId} 当前无手牌，跳过飞入表现");
                for (int _ti = 0; _ti < evt.TargetUnitIds.Count; _ti++)
                {
                    EventKit.Type.Send(new UIEvents.SkillVisualHitEvent { TargetUnitId = evt.TargetUnitIds[_ti], TargetUnitIndex = _ti < evt.TargetUnitIndices.Count ? evt.TargetUnitIndices[_ti] : -1 });
                }
                yield break;
            }
        }

        // === 2. 播放卡牌飞入动画 ===
        if (mHandCardsPanel == null)
        {
            LogKit.Warning($"[BattleActionPlayer] 手牌面板未注册，跳过 {evt.SkillId} 飞入表现");
            for (int _ti = 0; _ti < evt.TargetUnitIds.Count; _ti++)
            {
                EventKit.Type.Send(new UIEvents.SkillVisualHitEvent { TargetUnitId = evt.TargetUnitIds[_ti], TargetUnitIndex = _ti < evt.TargetUnitIndices.Count ? evt.TargetUnitIndices[_ti] : -1 });
            }
            yield break;
        }

        yield return FlyCardsToHandArea(cardCount);

        // === 3. 发送命中事件（逻辑层执行实际抽牌/弃置抽牌） ===
        for (int _ti = 0; _ti < evt.TargetUnitIds.Count; _ti++)
        {
            EventKit.Type.Send(new UIEvents.SkillVisualHitEvent { TargetUnitId = evt.TargetUnitIds[_ti], TargetUnitIndex = _ti < evt.TargetUnitIndices.Count ? evt.TargetUnitIndices[_ti] : -1 });
        }

        LogKit.Log($"[BattleActionPlayer] {evt.SkillId} 手牌飞入完成，已触发抽牌效果");
    }

    /// <summary>
    /// 发牌器抽牌表现：卡牌从屏幕外飞入手牌区域，动画完成后回调通知逻辑层实际抽牌。
    /// 供 BattleSystem.OnCardDealerAction 调用。
    /// </summary>
    /// <param name="cardCount">飞入卡牌数量</param>
    /// <param name="onComplete">动画完成回调（逻辑层在此执行实际抽牌）</param>
    public void PlayDealerDrawCardEffect(int cardCount, System.Action onComplete)
    {
        if (cardCount <= 0)
        {
            onComplete?.Invoke();
            return;
        }

        if (mHandCardsPanel == null)
        {
            LogKit.Warning("[BattleActionPlayer] 手牌面板未注册，跳过发牌器飞入表现");
            onComplete?.Invoke();
            return;
        }

        if (mDealerEffectCoroutine != null)
            StopCoroutine(mDealerEffectCoroutine);
        mDealerEffectCoroutine = StartCoroutine(PlayDealerDrawCardCoroutine(cardCount, onComplete));
    }

    private IEnumerator PlayDealerDrawCardCoroutine(int cardCount, System.Action onComplete)
    {
        LogKit.Log($"[BattleActionPlayer] 发牌器手牌飞入开始: {cardCount}张卡牌");

        yield return FlyCardsToHandArea(cardCount);

        LogKit.Log("[BattleActionPlayer] 发牌器手牌飞入完成");

        mDealerEffectCoroutine = null;
        onComplete?.Invoke();
    }

    /// <summary>
    /// 通用卡牌飞入手牌区域动画：在屏幕外随机位置生成卡牌，依次飞入手牌面板均匀分布位置。
    /// 使用 FlyCard 图片资源（Resources/Sprites/Dealer/FlyCard）。
    /// 不含命中事件/回调逻辑，由调用方负责后续结算。
    /// </summary>
    private IEnumerator FlyCardsToHandArea(int cardCount)
    {
        RectTransform handPanelRect = mHandCardsPanel as RectTransform;
        if (handPanelRect == null)
        {
            LogKit.Warning("[BattleActionPlayer] 手牌面板非RectTransform，跳过飞入动画");
            yield break;
        }

        // 获取手牌面板的世界坐标四角，计算世界空间中心与宽度
        Vector3[] corners = new Vector3[4];
        handPanelRect.GetWorldCorners(corners);
        Vector3 handCenter = (corners[0] + corners[2]) * 0.5f;
        float handWorldWidth = corners[2].x - corners[0].x;

        // 创建容器（加入跟踪列表，战斗结束时OnDestroy统一销毁）
        Transform effectParent = GetEffectParent();
        GameObject container = new GameObject("HandCardFlyIn");
        container.transform.SetParent(effectParent, false);
        mHandCardFlyInContainers.Add(container);

        Sprite cardSprite = LoadFlyCardSprite();
        List<Coroutine> flights = new List<Coroutine>();

        // 计算每张卡牌的目标位置（在手牌面板内均匀分布）
        float cardSpacing = Mathf.Min(80f, handWorldWidth / (cardCount + 1));
        float startX = handCenter.x - (cardCount - 1) * cardSpacing * 0.5f;

        for (int i = 0; i < cardCount; i++)
        {
            Vector3 startPos = GetRandomOffscreenPosition();
            Vector3 targetPos = new Vector3(startX + i * cardSpacing, handCenter.y, 0);
            float startRotation = Random.Range(-45f, 45f);

            GameObject cardObj = CreateFlyingCard(container.transform, startPos, cardSprite);

            // 错开0.1s出发，飞行时长0.5s
            flights.Add(StartCoroutine(FlyCardToHand(cardObj, targetPos, startRotation, 0.5f, i * 0.1f)));
        }

        LogKit.Log($"[BattleActionPlayer] 卡牌飞入开始: {cardCount}张");

        // 等待所有卡牌到达
        foreach (var cor in flights)
        {
            yield return cor;
        }

        // 清理临时卡牌对象
        yield return new WaitForSeconds(0.15f);
        if (container != null)
        {
            mHandCardFlyInContainers.Remove(container);
            Destroy(container);
        }
    }

    /// <summary>
    /// 加载飞入卡牌专用图片：Resources/Sprites/Dealer/FlyCard
    /// YooAsset 地址: FlyCard（Sprites/Dealer 组使用 AddressByFileName）
    /// 支持 ResKit/Resources.Load&lt;Sprite&gt;/Texture2D 兜底
    /// </summary>
    private Sprite LoadFlyCardSprite()
    {
        string fullPath = "Sprites/Dealer/FlyCard";

        // 方式1: ResKit（YooAsset 地址 = "FlyCard"）
        var sprite = ResKit.Load<Sprite>("FlyCard");
        if (sprite != null) return sprite;

        // 方式2: Resources.Load<Sprite>（完整路径兜底）
        sprite = Resources.Load<Sprite>(fullPath);
        if (sprite != null) return sprite;

        // 方式3: Texture2D 兜底（支持 jpg 等非 Sprite 格式）
        var texture = Resources.Load<Texture2D>(fullPath);
        if (texture != null)
        {
            sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            if (sprite != null)
            {
                LogKit.Log($"[BattleActionPlayer] FlyCard图片通过 Texture2D 兜底加载成功: Resources/{fullPath}");
                return sprite;
            }
        }

        LogKit.Warning($"[BattleActionPlayer] FlyCard图片加载失败: Resources/{fullPath}，使用法师紫色兜底");
        return null;
    }

    /// <summary>
    /// 获取屏幕外随机位置（从上/下/左/右四个方向随机选取）
    /// </summary>
    private Vector3 GetRandomOffscreenPosition()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null) return new Vector3(0, 1000, 0);

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null) return new Vector3(0, 1000, 0);

        float halfWidth = canvasRect.rect.width * 0.5f;
        float halfHeight = canvasRect.rect.height * 0.5f;
        float margin = 100f;  // 屏幕外偏移量

        int edge = Random.Range(0, 4);
        Vector3 localPos;
        switch (edge)
        {
            case 0:  // 上方
                localPos = new Vector3(Random.Range(-halfWidth, halfWidth), halfHeight + margin, 0);
                break;
            case 1:  // 下方
                localPos = new Vector3(Random.Range(-halfWidth, halfWidth), -halfHeight - margin, 0);
                break;
            case 2:  // 左侧
                localPos = new Vector3(-halfWidth - margin, Random.Range(-halfHeight, halfHeight), 0);
                break;
            default: // 右侧
                localPos = new Vector3(halfWidth + margin, Random.Range(-halfHeight, halfHeight), 0);
                break;
        }

        return canvasRect.TransformPoint(localPos);
    }

    /// <summary>
    /// 创建飞行的卡牌视觉对象（矩形卡牌，优先使用技能Sprite，无Sprite则使用法师紫色兜底）
    /// </summary>
    private GameObject CreateFlyingCard(Transform parent, Vector3 startPos, Sprite sprite)
    {
        GameObject card = new GameObject("FlyingCard");
        card.transform.SetParent(parent, false);
        card.transform.position = startPos;

        RectTransform rect = card.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(120, 168);  // 卡牌尺寸(2x)

        Image img = card.AddComponent<Image>();
        img.raycastTarget = false;

        if (sprite != null)
        {
            img.sprite = sprite;
            img.color = Color.white;
        }
        else
        {
            // 兜底：法师紫色卡牌
            img.color = new Color(0.4f, 0.2f, 0.8f, 1f);
        }

        return card;
    }

    /// <summary>
    /// 卡牌飞向手牌区域目标位置。
    /// 带延迟出发、ease-out缓动（先快后慢）、旋转回正（从随机角度旋转到0°）。
    /// </summary>
    private IEnumerator FlyCardToHand(GameObject card, Vector3 targetPos, float startRotation, float duration, float delay)
    {
        if (card == null) yield break;

        // 延迟出发（错开效果）
        if (delay > 0f)
        {
            yield return new WaitForSeconds(delay);
            if (card == null) yield break;
        }

        Vector3 startPos = card.transform.position;
        float elapsed = 0f;

        while (elapsed < duration && card != null)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            // ease-out 缓动：1 - (1-t)^3
            float easedT = 1f - Mathf.Pow(1f - t, 3f);

            card.transform.position = Vector3.Lerp(startPos, targetPos, easedT);
            // 旋转回正
            card.transform.rotation = Quaternion.Euler(0, 0, Mathf.Lerp(startRotation, 0f, easedT));
            yield return null;
        }

        if (card != null)
        {
            card.transform.position = targetPos;
            card.transform.rotation = Quaternion.identity;
        }
    }

    #endregion
}
#region 战斗数据模型

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TurnManagement;

/// <summary>
/// 战斗流程中的枚举定义，描述相关状态或类型。
/// </summary>
public enum BattleUnitType
{
    Player,
    Enemy,
    CardDealer
}

/// <summary>
/// 战斗流程中的枚举定义，描述相关状态或类型。
/// </summary>
public enum BattleState
{
    BattleStart,
    WaitingForAction,
    PlayerActing,
    EnemyActing,
    CardDealerActing,
    WaveTransition,
    BattleWin,
    BattleLose
}

/// <summary>
/// 战斗流程中的枚举定义，描述相关状态或类型。
/// </summary>
public enum StatusEffectType
{
    AttackUp,
    AttackDown,
    DefenseUp,
    DefenseDown,
    SpeedUp,
    SpeedDown,
    Shield,
    DamageUp,
    CritRateUp,
    CritDamageUp,
    DefensePenetration,
    Fear,
    EnergyGainCooldown
}

/// <summary>
/// 战斗模块中的 StatusEffect，负责对应的战斗流程处理。
/// </summary>
public class StatusEffect
{
    public StatusEffectType Type { get; set; }
    public int Value { get; set; }
    public int Duration { get; set; }

    public StatusEffect(StatusEffectType type, int value, int duration)
    {
        Type = type;
        Value = value;
        Duration = duration;
    }
}

/// <summary>
/// 战斗模块中的 BattleUnitData，负责对应的战斗流程处理。
/// </summary>
public class BattleUnitData
{
    public string UnitId { get; set; }
    public BattleUnitType UnitType { get; set; }
    public string Name { get; set; }
    public int MaxHp { get; set; }
    public int CurrentHp { get; set; }
    public int BaseSpeed { get; set; }
    public int BaseAttack { get; set; }
    public int BaseDefense { get; set; }
    public bool IsAlive { get; set; }
    
    public int Shield { get; set; }
    private List<StatusEffect> mStatusEffects = new List<StatusEffect>();

    public int Level { get; set; } = 1;
    public float BaseCritRate { get; set; } = 0.1f;
    public float BaseCritDamage { get; set; } = 0.5f;
    public float BaseDamageBonus { get; set; } = 0f;
    public float BaseResistance { get; set; } = 0f;
    public float BaseResistancePenetration { get; set; } = 0f;
    public float BaseDefenseReduction { get; set; } = 0f;
    public float BaseIgnoreDefense { get; set; } = 0f;
    public Dictionary<string, int> TalentLevels { get; set; } = new Dictionary<string, int>();
    public Dictionary<string, float> TalentMultipliers { get; set; } = new Dictionary<string, float>();
    public Dictionary<string, float> TalentMultipliers2 { get; set; } = new Dictionary<string, float>();
    public Dictionary<int, float> StarEffectValues { get; set; } = new Dictionary<int, float>();

    // Weapon effect modifiers
    public float NormalAttackMultiplier { get; set; } = 1.0f;
    public float SkillDamageMultiplier { get; set; } = 1.0f;
    public float HealMultiplier { get; set; } = 1.0f;
    public float ExtraCritDamageChance { get; set; } = 0f;
    public float ExtraCritDamageValue { get; set; } = 0f;
    public float ExtraAttackChance { get; set; } = 0f;
    public float DotDamagePercent { get; set; } = 0f;
    public int DotDuration { get; set; } = 0;
    public float HealOverTimePercent { get; set; } = 0f;
    public float AttackUpAfterSkillPercent { get; set; } = 0f;
    public int AttackUpAfterSkillDuration { get; set; } = 0;
    public float DefenseUpAfterHealPercent { get; set; } = 0f;
    public int DefenseUpAfterHealDuration { get; set; } = 0;

    // Star effect modifiers (from character star unlocks)
    public int StarLevel { get; set; } = 0;
    public float UltimateBonus { get; set; } = 0f;
    public float StarHealBonus { get; set; } = 0f;

    // Character talent values: maps skill type to talent value (percentage, e.g. 100=100%)
    public Dictionary<string, int> TalentValues { get; set; } = new Dictionary<string, int>();

    public int CurrentSpeed => CalculateEffectiveSpeed();
    public int CurrentAttack => CalculateEffectiveAttack();
    public int CurrentDefense => CalculateEffectiveDefense();
    public float CurrentCritRate => CalculateEffectiveCritRate();
    public float CurrentCritDamage => CalculateEffectiveCritDamage();
    public float CurrentDamageBonus => CalculateEffectiveDamageBonus();
    public float CurrentResistance => CalculateEffectiveResistance();
    public float CurrentResistancePenetration => CalculateEffectiveResistancePenetration();
    public float CurrentDefenseReduction => CalculateEffectiveDefenseReduction();
    public float CurrentIgnoreDefense => CalculateEffectiveIgnoreDefense();

    // Character state tracking
    public bool DeathSaveUsed { get; set; } = false;
    public int AssassinationState { get; set; } = 0;
    public int GodSlayerState { get; set; } = 0;
    public float CurrentHpPercent => MaxHp > 0 ? (float)CurrentHp / MaxHp : 0f;

    public BattleUnitData() { }

    public BattleUnitData(string unitId, BattleUnitType unitType, string name, int maxHp, int speed, int attack, int defense)
    {
        UnitId = unitId;
        UnitType = unitType;
        Name = name;
        MaxHp = maxHp;
        CurrentHp = maxHp;
        BaseSpeed = speed;
        BaseAttack = attack;
        BaseDefense = defense;
        IsAlive = true;
        Shield = 0;
        Level = 1;
    }

    /// <summary>
    /// 执行 AddStatusEffect 相关的战斗处理。
    /// </summary>
    public void AddStatusEffect(StatusEffectType type, int value, int duration, bool isSelfTarget = false)
    {
        int effectiveDuration = isSelfTarget ? duration + 1 : duration;
        
        var existing = mStatusEffects.FirstOrDefault(e => e.Type == type);
        if (existing != null)
        {
            existing.Value = value;
            existing.Duration = effectiveDuration;
        }
        else
        {
            mStatusEffects.Add(new StatusEffect(type, value, effectiveDuration));
        }
    }

    /// <summary>
    /// 执行 RemoveStatusEffect 相关的战斗处理。
    /// </summary>
    public void RemoveStatusEffect(StatusEffectType type)
    {
        mStatusEffects.RemoveAll(e => e.Type == type);
    }

    private List<DotEffect> mDotEffects = new List<DotEffect>();

/// <summary>
/// 战斗模块中的 DotEffect，负责对应的战斗流程处理。
/// </summary>
    public class DotEffect
    {
        public string SourceId { get; set; }
        public float DamagePercent { get; set; }
        public int Duration { get; set; }
        public float SourceBaseAttack { get; set; }
    }

    /// <summary>
    /// 执行 ApplyDot 相关的战斗处理。
    /// </summary>
    public void ApplyDot(string sourceId, float damagePercent, int duration, float sourceBaseAttack = 0f)
    {
        mDotEffects.Add(new DotEffect
        {
            SourceId = sourceId,
            DamagePercent = damagePercent,
            Duration = duration,
            SourceBaseAttack = sourceBaseAttack
        });
    }

    /// <summary>
    /// 执行 ResetBattleEffects 相关的战斗处理。
    /// </summary>
    public void ResetBattleEffects()
    {
        NormalAttackMultiplier = 1.0f;
        SkillDamageMultiplier = 1.0f;
        HealMultiplier = 1.0f;
        ExtraCritDamageChance = 0f;
        ExtraCritDamageValue = 0f;
        ExtraAttackChance = 0f;
        DotDamagePercent = 0f;
        DotDuration = 0;
        HealOverTimePercent = 0f;
        AttackUpAfterSkillPercent = 0f;
        AttackUpAfterSkillDuration = 0;
        DefenseUpAfterHealPercent = 0f;
        DefenseUpAfterHealDuration = 0;
        UltimateBonus = 0f;
        StarHealBonus = 0f;
        DeathSaveUsed = false;
        AssassinationState = 0;
        GodSlayerState = 0;
        mStatusEffects.Clear();
        mDotEffects.Clear();
    }

    /// <summary>
    /// 执行 UpdateStatusEffects 相关的战斗处理。
    /// </summary>
    public void UpdateStatusEffects()
    {
        bool speedChanged = false;
        mStatusEffects.RemoveAll(e => {
            e.Duration--;
            if (e.Duration <= 0)
            {
                if (e.Type == StatusEffectType.SpeedUp || e.Type == StatusEffectType.SpeedDown)
                {
                    speedChanged = true;
                }
                return true;
            }
            return false;
        });
        
        if (speedChanged && TurnOrderManager.Instance.IsInitialized)
        {
            TurnOrderManager.Instance.SpeedUp(UnitId, CurrentSpeed);
        }
    }

    /// <summary>
    /// 执行 ProcessDotDamage 相关的战斗处理。
    /// </summary>
    public void ProcessDotDamage()
    {
        if (mDotEffects.Count == 0) return;

        float totalDotDamage = 0f;
        foreach (var dot in mDotEffects)
        {
            totalDotDamage += dot.SourceBaseAttack * dot.DamagePercent;
        }

        if (totalDotDamage > 0)
        {
            int dotDamage = Mathf.CeilToInt(totalDotDamage);
            CurrentHp -= dotDamage;
            if (CurrentHp <= 0)
            {
                CurrentHp = 0;
                IsAlive = false;
            }
        }

        mDotEffects.RemoveAll(d =>
        {
            d.Duration--;
            return d.Duration <= 0;
        });
    }

    /// <summary>
    /// 执行 ProcessHealOverTime 相关的战斗处理。
    /// </summary>
    public void ProcessHealOverTime()
    {
        if (HealOverTimePercent <= 0 || !IsAlive) return;

        int healAmount = Mathf.RoundToInt(MaxHp * HealOverTimePercent);
        CurrentHp = Mathf.Min(MaxHp, CurrentHp + healAmount);
    }

    private int CalculateEffectiveSpeed()
    {
        float speed = BaseSpeed;
        foreach (var effect in mStatusEffects)
        {
            switch (effect.Type)
            {
                case StatusEffectType.SpeedUp:
                    speed += BaseSpeed * effect.Value / 100f;
                    break;
                case StatusEffectType.SpeedDown:
                    speed -= BaseSpeed * effect.Value / 100f;
                    break;
            }
        }
        return Mathf.Max(1, Mathf.RoundToInt(speed));
    }

    private int CalculateEffectiveAttack()
    {
        float attack = BaseAttack;
        foreach (var effect in mStatusEffects)
        {
            switch (effect.Type)
            {
                case StatusEffectType.AttackUp:
                    attack += BaseAttack * effect.Value / 100f;
                    break;
                case StatusEffectType.AttackDown:
                    attack -= BaseAttack * effect.Value / 100f;
                    break;
            }
        }
        return Mathf.Max(1, Mathf.RoundToInt(attack));
    }

    private int CalculateEffectiveDefense()
    {
        float defense = BaseDefense;
        foreach (var effect in mStatusEffects)
        {
            switch (effect.Type)
            {
                case StatusEffectType.DefenseUp:
                    defense += BaseDefense * effect.Value / 100f;
                    break;
                case StatusEffectType.DefenseDown:
                    defense -= BaseDefense * effect.Value / 100f;
                    break;
            }
        }
        return Mathf.Max(0, Mathf.RoundToInt(defense));
    }

    private float CalculateEffectiveCritRate()
    {
        float critRate = BaseCritRate;
        foreach (var effect in mStatusEffects)
        {
            switch (effect.Type)
            {
                case StatusEffectType.CritRateUp:
                    critRate += effect.Value * 0.01f;
                    break;
            }
        }
        return Mathf.Clamp01(critRate);
    }

    private float CalculateEffectiveCritDamage()
    {
        float critDamage = BaseCritDamage;
        foreach (var effect in mStatusEffects)
        {
            switch (effect.Type)
            {
                case StatusEffectType.CritDamageUp:
                    critDamage += effect.Value * 0.01f;
                    break;
            }
        }
        return Mathf.Max(0f, critDamage);
    }

    private float CalculateEffectiveDamageBonus()
    {
        float bonus = BaseDamageBonus;
        foreach (var effect in mStatusEffects)
        {
            switch (effect.Type)
            {
                case StatusEffectType.DamageUp:
                    bonus += effect.Value * 0.01f;
                    break;
            }
        }
        return bonus;
    }

    private float CalculateEffectiveResistance()
    {
        return BaseResistance;
    }

    private float CalculateEffectiveResistancePenetration()
    {
        return BaseResistancePenetration;
    }

    private float CalculateEffectiveDefenseReduction()
    {
        float reduction = BaseDefenseReduction;
        foreach (var effect in mStatusEffects)
        {
            switch (effect.Type)
            {
                case StatusEffectType.DefenseDown:
                    reduction += effect.Value * 0.01f;
                    break;
            }
        }
        return reduction;
    }

    private float CalculateEffectiveIgnoreDefense()
    {
        return BaseIgnoreDefense;
    }

    /// <summary>
    /// 执行 HasStatusEffect 相关的战斗处理。
    /// </summary>
    public bool HasStatusEffect(StatusEffectType type)
    {
        return mStatusEffects.Any(e => e.Type == type);
    }

    /// <summary>
    /// 执行 GetStatusEffectValue 相关的战斗处理。
    /// </summary>
    public int GetStatusEffectValue(StatusEffectType type)
    {
        var effect = mStatusEffects.FirstOrDefault(e => e.Type == type);
        return effect != null ? effect.Value : 0;
    }

    /// <summary>
    /// 执行 GetTalentMultiplier 相关的战斗处理。
    /// </summary>
    public float GetTalentMultiplier(string skillType)
    {
        if (TalentMultipliers != null && TalentMultipliers.ContainsKey(skillType))
            return TalentMultipliers[skillType] / 100f;
        return 1.0f;
    }

    /// <summary>
    /// 执行 GetTalentValue 相关的战斗处理。
    /// </summary>
    public int GetTalentValue(string skillType)
    {
        if (TalentMultipliers != null && TalentMultipliers.ContainsKey(skillType))
            return (int)TalentMultipliers[skillType];
        return 0;
    }

    /// <summary>
    /// 执行 GetTalentValue2 相关的战斗处理。
    /// </summary>
    public int GetTalentValue2(string skillType)
    {
        if (TalentMultipliers2 != null && TalentMultipliers2.ContainsKey(skillType))
            return (int)TalentMultipliers2[skillType];
        return 0;
    }

    /// <summary>
    /// 执行 GetStarEffectValue 相关的战斗处理。
    /// </summary>
    public float GetStarEffectValue(int starLevel)
    {
        if (StarEffectValues != null && StarEffectValues.ContainsKey(starLevel))
            return StarEffectValues[starLevel];
        return 0f;
    }

    /// <summary>
    /// 执行 GetTalentLevel 相关的战斗处理。
    /// </summary>
    public int GetTalentLevel(string skillType)
    {
        if (TalentLevels != null && TalentLevels.ContainsKey(skillType))
            return TalentLevels[skillType];
        return 1;
    }

    /// <summary>
    /// 执行 InitializeWeaponEffects 相关的战斗处理。
    /// </summary>
    public void InitializeWeaponEffects(List<WeaponEffectData> effects)
    {
        if (effects == null) return;

        foreach (var effect in effects)
        {
            switch (effect.EffectId)
            {
                case "IronSlash":
                case "DarkShadow":
                case "Pierce":
                    NormalAttackMultiplier = effect.Value > NormalAttackMultiplier ? effect.Value : NormalAttackMultiplier;
                    break;
                case "MagicBoost":
                case "FireMastery":
                    SkillDamageMultiplier += effect.Value;
                    break;
                case "FlameStrike":
                    AttackUpAfterSkillPercent = effect.Value;
                    AttackUpAfterSkillDuration = effect.Duration > 0 ? effect.Duration : 2;
                    break;
                case "Burn":
                    DotDamagePercent = effect.Value;
                    DotDuration = effect.Duration > 0 ? effect.Duration : 2;
                    break;
                case "ArcaneKnowledge":
                    BaseCritRate += effect.Value;
                    break;
                case "AncientWisdom":
                    BaseCritDamage += effect.Value;
                    break;
                case "ManaFlow":
                    HealOverTimePercent = effect.Value;
                    break;
                case "Precision":
                    BaseCritRate += effect.Value;
                    break;
                case "WindRush":
                    BaseSpeed += Mathf.RoundToInt(BaseSpeed * effect.Value);
                    break;
                case "MultiShot":
                    ExtraAttackChance = effect.Value;
                    break;
                case "ShadowStrike":
                    ExtraCritDamageChance = 0.3f;
                    ExtraCritDamageValue = effect.Value;
                    break;
                case "HealBoost":
                case "DivineHeal":
                    HealMultiplier += effect.Value;
                    break;
                case "Blessing":
                    DefenseUpAfterHealPercent = effect.Value;
                    DefenseUpAfterHealDuration = effect.Duration > 0 ? effect.Duration : 2;
                    break;
            }
        }
    }

    /// <summary>
    /// 执行 InitializeRelicSetEffects 相关的战斗处理。
    /// </summary>
    public void InitializeRelicSetEffects(Dictionary<string, int> setCounts)
    {
        if (setCounts == null) return;

        foreach (var kvp in setCounts)
        {
            string setName = kvp.Key;
            int count = kvp.Value;

            if (count >= 2)
            {
                ApplySetEffect1(setName);
            }

            if (count >= 4)
            {
                ApplySetEffect2(setName);
            }
        }
    }

    private void ApplySetEffect1(string setName)
    {
        switch (setName)
        {
            case "战神套装":
                BaseAttack = Mathf.RoundToInt(BaseAttack * 1.05f);
                BaseDefense = Mathf.RoundToInt(BaseDefense * 1.05f);
                MaxHp = Mathf.RoundToInt(MaxHp * 1.05f);
                break;
            case "奥术套装":
                SkillDamageMultiplier += 0.05f;
                break;
            case "猎手套装":
                BaseSpeed += Mathf.RoundToInt(BaseSpeed * 0.08f);
                break;
            case "圣愈套装":
                HealMultiplier += 0.10f;
                break;
        }
    }

    private void ApplySetEffect2(string setName)
    {
        switch (setName)
        {
            case "战神套装":
                SkillDamageMultiplier += 0.10f;
                break;
            case "奥术套装":
                BaseCritDamage += 0.15f;
                break;
            case "猎手套装":
                BaseCritRate += 0.10f;
                break;
            case "圣愈套装":
                MaxHp = Mathf.RoundToInt(MaxHp * 1.05f);
                break;
        }
    }
}

/// <summary>
/// 战斗模块中的 EnemyData，负责对应的战斗流程处理。
/// </summary>
public class EnemyData
{
    public string EnemyId { get; set; }
    public string Name { get; set; }
    public int MaxHp { get; set; }
    public int Speed { get; set; }
    public int Attack { get; set; }
    public int Defense { get; set; }
    public int RewardGold { get; set; }
    public int RewardExp { get; set; }
    public int Level { get; set; } = 1;
    public float Resistance { get; set; } = 0f;

    public EnemyData() { }

    public EnemyData(string enemyId, string name, int maxHp, int speed, int attack, int defense)
    {
        EnemyId = enemyId;
        Name = name;
        MaxHp = maxHp;
        Speed = speed;
        Attack = attack;
        Defense = defense;
    }

    public EnemyData(string enemyId, string name, int maxHp, int speed, int attack, int defense, int rewardGold, int rewardExp)
        : this(enemyId, name, maxHp, speed, attack, defense)
    {
        RewardGold = rewardGold;
        RewardExp = rewardExp;
    }

    public EnemyData(string enemyId, string name, int maxHp, int speed, int attack, int defense, int rewardGold, int rewardExp, int level, float resistance)
        : this(enemyId, name, maxHp, speed, attack, defense, rewardGold, rewardExp)
    {
        Level = level;
        Resistance = resistance;
    }
}

/// <summary>
/// 战斗模块中的 EnemyWaveData，负责对应的战斗流程处理。
/// </summary>
public class EnemyWaveData
{
    public int WaveNumber { get; set; }
    public List<EnemyData> EnemyList { get; set; } = new List<EnemyData>();
    public float SpawnInterval { get; set; } = 2f;
    public int WaveDelaySeconds { get; set; } = 5;

    public EnemyWaveData() { }

    public EnemyWaveData(int waveNumber, List<EnemyData> enemyList, float spawnInterval = 2f)
    {
        WaveNumber = waveNumber;
        EnemyList = enemyList;
        SpawnInterval = spawnInterval;
    }

    public EnemyWaveData(int waveNumber, float spawnInterval, int waveDelaySeconds)
    {
        WaveNumber = waveNumber;
        SpawnInterval = spawnInterval;
        WaveDelaySeconds = waveDelaySeconds;
        EnemyList = new List<EnemyData>();
    }
}

#endregion

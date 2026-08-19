#region 战斗系统

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YokiFrame;
using System.Linq;
using TurnManagement;
using EnergyManagement;
using CardManagement;
using WaveManagement;
using UnitManagement;
using StateManagement;
using Core;
using GameUI;
using static GameUI.UIEvents;
using Game.GameLogic.Battle.Core;

/// <summary>
/// 攻击表现类型标识：决定走哪种视觉表现流程
/// </summary>
public enum AttackVisualType
{
    None,       // 未知类型 — 无表现，立即执行
    Melee,      // 近战 — 头像移动+武器劈砍（战士、枪兵）
    Arrow,      // 弓箭手 — 箭矢弹道
    MagicOrb,   // 法师 — 法球弹道
    HealingOrb  // 医疗师 — 治疗球弹道
}

/// <summary>
/// 战斗模块中的 BattleSystem，负责对应的战斗流程处理。
/// </summary>
public class BattleSystem:Singleton<BattleSystem>
{
    #region 战斗状态与初始化

    private List<BattleUnitData> mPlayerUnits = new List<BattleUnitData>();
    private List<BattleUnitData> mEnemyUnits = new List<BattleUnitData>();
    private BattleUnitData mCardDealerUnit;

    private bool mIsActionComplete = false;
    private bool mIsWaitingForVisual = false;
    private SkillCardData mPendingCard;
    private BattleUnitData mPendingAttacker;
    private BattleUnitData mPendingNormalAttackTarget;
    // 敌人攻击待处理数据（复用 mIsWaitingForVisual 机制，走视觉表现流程）
    private bool mIsEnemyAttacking = false;
    private BattleUnitData mPendingEnemyAttacker;
    private BattleUnitData mPendingEnemyTarget;
    private string mCurrentDungeonId = "";

    public bool IsActionComplete => mIsActionComplete;
    public bool IsBattleActive => BattleStateManager.Instance.IsBattleActive;
    public string CurrentDungeonId => mCurrentDungeonId;

    /// <summary>
    /// 执行 Initialize 相关的战斗处理。
    /// </summary>
    public void Initialize(List<BattleUnitData> playerTeam, List<EnemyWaveData> enemyWaves, string dungeonId = "")
        {
            BattleFSM.Instance.SetupStates();

            BattleManagerLifecycle.InitializeAllManagers();

            // 双重保障：清空上一场战斗残留的敌人/玩家数据，确保每次进入副本都是全新状态
            mEnemyUnits.Clear();
            mPlayerUnits.Clear();

            mPlayerUnits = new List<BattleUnitData>(playerTeam);
            mCurrentDungeonId = dungeonId;
            DungeonManager.Instance.SetCurrentDungeon(dungeonId);
            WaveManager.Instance.Initialize(enemyWaves);

            SetupCardDealer();
            SetupTurnOrderUnits();
            InitializeCardDeck();
            SetupCardHandExcessHandler();

            RegisterEvents();

            LogKit.Log($"[BattleSystem] 战斗初始化完成: 玩家数={mPlayerUnits.Count}, 敌人列表已清空");
        }

    /// <summary>
    /// 执行 LoadEnemyWavesFromConfig 相关的战斗处理。
    /// </summary>
        public List<EnemyWaveData> LoadEnemyWavesFromConfig(string dungeonId)
        {
            List<EnemyWaveData> waves = new List<EnemyWaveData>();

            DungeonConfig dungeonConfig = GameDataMgr.Instance.dungeonConfigs?.Dungeons?.FirstOrDefault(d => d.DungeonId == dungeonId);

            if (dungeonConfig != null && dungeonConfig.Waves != null)
            {
                LogKit.Log($"[BattleSystem] 使用副本配置: {dungeonConfig.DungeonName}");
                foreach (var waveConfig in dungeonConfig.Waves)
                {
                    EnemyWaveData wave = new EnemyWaveData(waveConfig.WaveNumber, waveConfig.SpawnInterval, waveConfig.WaveDelaySeconds);

                    foreach (var enemyEntry in GetWaveEnemies(waveConfig))
                    {
                        var enemyConfig = GameDataMgr.Instance.enemyConfigs.Enemies.FirstOrDefault(e => e.EnemyId == enemyEntry.EnemyId);
                        if (enemyConfig != null)
                            AddEnemyToWave(wave, enemyConfig, enemyEntry);
                    }

                    waves.Add(wave);
                }
            }
            else
            {
                LogKit.Log($"[BattleSystem] 使用默认波次配置");
                foreach (var waveConfig in GameDataMgr.Instance.waveConfigs.Waves)
                {
                    EnemyWaveData wave = new EnemyWaveData(waveConfig.WaveNumber, waveConfig.SpawnInterval, waveConfig.WaveDelaySeconds);

                    foreach (var enemyEntry in GetWaveEnemies(waveConfig))
                    {
                        var enemyConfig = GameDataMgr.Instance.enemyConfigs.Enemies.FirstOrDefault(e => e.EnemyId == enemyEntry.EnemyId);
                        if (enemyConfig != null)
                            AddEnemyToWave(wave, enemyConfig, enemyEntry);
                    }

                    waves.Add(wave);
                }
            }

            return waves;
        }

        #endregion

        #region 波次配置

        private List<WaveEnemyConfig> GetWaveEnemies(WaveConfig waveConfig)
        {
            if (waveConfig.Enemies != null && waveConfig.Enemies.Count > 0)
                return waveConfig.Enemies;

            return waveConfig.EnemyIds.Select(enemyId => new WaveEnemyConfig { EnemyId = enemyId }).ToList();
        }

        private void AddEnemyToWave(EnemyWaveData wave, EnemyConfig config, WaveEnemyConfig enemyEntry)
        {
            float attributeMultiplier = enemyEntry.AttributeMultiplier > 0f ? enemyEntry.AttributeMultiplier : 1f;
            float hpMultiplier = enemyEntry.HpMultiplier > 0f ? enemyEntry.HpMultiplier : attributeMultiplier;
            float attackMultiplier = enemyEntry.AttackMultiplier > 0f ? enemyEntry.AttackMultiplier : attributeMultiplier;
            float defenseMultiplier = enemyEntry.DefenseMultiplier > 0f ? enemyEntry.DefenseMultiplier : attributeMultiplier;
            float speedMultiplier = enemyEntry.SpeedMultiplier > 0f ? enemyEntry.SpeedMultiplier : attributeMultiplier;

            wave.EnemyList.Add(new EnemyData(
                config.EnemyId, config.Name,
                Mathf.RoundToInt(config.MaxHp * hpMultiplier),
                Mathf.Max(1, Mathf.RoundToInt(config.Speed * speedMultiplier)),
                Mathf.RoundToInt(config.Attack * attackMultiplier),
                Mathf.RoundToInt(config.Defense * defenseMultiplier),
                config.RewardGold, config.RewardExp, config.Level, config.Resistance));
        }

        private void SetupCardDealer()
        {
            mCardDealerUnit = BattleUnitFactory.Instance.CreateCardDealerUnit();
            UpdateCardDealerSpeed();
        }

        private void UpdateCardDealerSpeed()
        {
            if (mCardDealerUnit == null) return;

            List<int> speeds = mPlayerUnits.Where(u => u.IsAlive)
                .Select(u => u.CurrentSpeed)
                .OrderByDescending(s => s)
                .ToList();

            float speed = 80f;

            if (speeds.Count >= 1) speed += speeds[0] * 0.2f;
            if (speeds.Count >= 2) speed += speeds[1] * 0.1f;
            if (speeds.Count >= 3) speed += speeds[2] * 0.05f;
            if (speeds.Count >= 4) speed += speeds[3] * 0.01f;

            int newSpeed = Mathf.RoundToInt(speed);
            mCardDealerUnit.BaseSpeed = newSpeed;

            TurnOrderManager.Instance.SpeedUp("CardDealer", newSpeed);

            LogKit.Log($"[BattleSystem] 发牌器速度更新为：{mCardDealerUnit.CurrentSpeed}");
        }

        private void SetupCardHandExcessHandler()
        {
            CardHand.Instance.OnExcessCard += (card) =>
            {
                CardDeck.Instance.Discard(card);
                LogKit.Log($"[BattleSystem] 手牌已满，卡牌 {card.Name} 加入弃牌堆");
            };
        }

    private void SetupTurnOrderUnits()
    {
        foreach (var unit in mPlayerUnits)
        {
            TurnOrderManager.Instance.AddUnit(new BattleUnitTurnOrder(unit));
        }

        TurnOrderManager.Instance.AddUnit(new CardDealerTurnOrder(mCardDealerUnit));
    }

    private void InitializeCardDeck()
        {
            List<SkillCardData> allSkills = new List<SkillCardData>();

            foreach (var unit in mPlayerUnits)
            {
                allSkills.AddRange(GenerateCharacterSkills(unit));
            }

            CardDeck.Instance.Initialize(allSkills);
            UpdateAliveCharactersInDeck();
            CardHand.Instance.AddCards(CardDeck.Instance.Draw(4));
            LogKit.Log($"[BattleSystem] 初始手牌：{CardHand.Instance.Count}张");
        }

        private void UpdateAliveCharactersInDeck()
        {
            List<string> aliveIds = mPlayerUnits.Where(u => u.IsAlive)
                .Select(u => u.UnitId)
                .ToList();
            CardDeck.Instance.SetAliveCharacters(aliveIds);
        }

    private List<SkillCardData> GenerateCharacterSkills(BattleUnitData unit)
    {
        List<SkillCardData> skills = new List<SkillCardData>();

        var skillConfigs = GameDataMgr.Instance.skillConfigs.Skills;
        var characterSkillIds = GameDataMgr.Instance.playerCharacterConfigs.Characters
            .FirstOrDefault(c => c.CharacterId == unit.UnitId)?.Skills;

        if (characterSkillIds != null)
        {
            foreach (var skillId in characterSkillIds)
            {
                if (skillId.EndsWith("NormalAttack"))
                    continue;

                var config = skillConfigs.FirstOrDefault(s => s.SkillId == skillId);
                if (config != null)
                {
                    int count = skillId.EndsWith("Ultimate") ? 1 : 2;

                    for (int i = 0; i < count; i++)
                    {
                        SkillCardData card = new SkillCardData(
                            config.SkillId + (count > 1 ? $"_{i + 1}" : ""),
                            config.Name,
                            config.Description,
                            config.Cost,
                            config.EffectType,
                            config.TargetType,
                            config.Value,
                            unit.UnitId
                        );
                        card.Duration = config.Duration;
                    card.HealPercent = config.HealPercent;
                    card.SelfHpCostPercent = config.SelfHpCostPercent;
                    card.SecondaryEffect = config.SecondaryEffect;
                    card.SecondaryValue = config.SecondaryValue;
                    card.SecondaryDuration = config.SecondaryDuration;
                    card.Probability = config.Probability;
                    skills.Add(card);
                    }
                }
            }
        }

        return skills;
    }

    private void RegisterEvents()
    {
        EventKit.Type.Register<TurnEvents.UnitActingEvent>(OnUnitActing);
        EventKit.Type.Register<TurnEvents.ActionCompleteEvent>(OnActionComplete);
        EventKit.Type.Register<WaveEvents.WaveStartEvent>(OnWaveStart);
        EventKit.Type.Register<WaveEvents.EnemySpawnEvent>(OnEnemySpawn);
        EventKit.Type.Register<WaveEvents.WaveCompleteEvent>(OnWaveComplete);
        EventKit.Type.Register<WaveEvents.AllWavesCompleteEvent>(OnAllWavesComplete);
        EventKit.Type.Register<UnitEvents.UnitDefeatedEvent>(OnUnitDefeated);
        EventKit.Type.Register<StateEvents.BattleWinEvent>(OnBattleWin);

        EventKit.Type.Register<BattlePanelOpenedEvent>(OnBattlePanelOpened);
        EventKit.Type.Register<BattlePanelClosedEvent>(OnBattlePanelClosed);
        EventKit.Type.Register<CardPlayRequestedEvent>(OnCardPlayRequested);
        EventKit.Type.Register<NormalAttackRequestedEvent>(OnNormalAttackRequested);
        EventKit.Type.Register<ManualDiscardRequestedEvent>(OnManualDiscardRequested);
        EventKit.Type.Register<BattleEndRequestedEvent>(OnBattleEndRequested);
        EventKit.Type.Register<BattleUIRefreshRequestedEvent>(OnBattleUIRefreshRequested);

        // 表现层事件
        EventKit.Type.Register<SkillVisualHitEvent>(OnSkillVisualHit);
        EventKit.Type.Register<SkillVisualCompleteEvent>(OnSkillVisualComplete);
    }

    private void UnregisterEvents()
    {
        EventKit.Type.UnRegister<TurnEvents.UnitActingEvent>(OnUnitActing);
        EventKit.Type.UnRegister<TurnEvents.ActionCompleteEvent>(OnActionComplete);
        EventKit.Type.UnRegister<WaveEvents.WaveStartEvent>(OnWaveStart);
        EventKit.Type.UnRegister<WaveEvents.EnemySpawnEvent>(OnEnemySpawn);
        EventKit.Type.UnRegister<WaveEvents.WaveCompleteEvent>(OnWaveComplete);
        EventKit.Type.UnRegister<WaveEvents.AllWavesCompleteEvent>(OnAllWavesComplete);
        EventKit.Type.UnRegister<UnitEvents.UnitDefeatedEvent>(OnUnitDefeated);
        EventKit.Type.UnRegister<StateEvents.BattleWinEvent>(OnBattleWin);

        EventKit.Type.UnRegister<BattlePanelOpenedEvent>(OnBattlePanelOpened);
        EventKit.Type.UnRegister<BattlePanelClosedEvent>(OnBattlePanelClosed);
        EventKit.Type.UnRegister<CardPlayRequestedEvent>(OnCardPlayRequested);
        EventKit.Type.UnRegister<NormalAttackRequestedEvent>(OnNormalAttackRequested);
        EventKit.Type.UnRegister<ManualDiscardRequestedEvent>(OnManualDiscardRequested);
        EventKit.Type.UnRegister<BattleEndRequestedEvent>(OnBattleEndRequested);
        EventKit.Type.UnRegister<BattleUIRefreshRequestedEvent>(OnBattleUIRefreshRequested);

        // 表现层事件
        EventKit.Type.UnRegister<SkillVisualHitEvent>(OnSkillVisualHit);
        EventKit.Type.UnRegister<SkillVisualCompleteEvent>(OnSkillVisualComplete);

        CardHand.Instance.ClearExcessCardHandler();
    }

    /// <summary>
    /// 执行 StartBattle 相关的战斗处理。
    /// </summary>
    public void StartBattle()
    {
        BattleStateManager.Instance.StartBattle();
        BattleFSM.Instance.Start();
        BattleFSM.Instance.Change(BattleState.BattleStart);
    }

    /// <summary>
    /// 执行 Update 相关的战斗处理。
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!IsBattleActive) return;

        // 表现层播放期间暂停回合推进
        if (mIsWaitingForVisual) return;

        TurnOrderManager.Instance.Update(deltaTime);
        WaveManager.Instance.Update(deltaTime);

        BattleFSM.Instance.Update();
        BattleFSM.Instance.CustomUpdate();
    }

    /// <summary>
    /// 执行 ChangeState 相关的战斗处理。
    /// </summary>
    public void ChangeState(BattleState state)
    {
        BattleStateManager.Instance.ChangeState(state);
        BattleFSM.Instance.Change(state);
    }

    /// <summary>
    /// 执行 EndBattle 相关的战斗处理。
    /// </summary>
    public void EndBattle()
    {
        BattleStateManager.Instance.EndBattle();
        UnregisterEvents();

        // 双重保障：清空战斗数据，确保下次进入副本时敌人状态不受上一次影响
        mEnemyUnits.Clear();
        mPlayerUnits.Clear();
        mPendingCard = null;
        mPendingAttacker = null;
        mPendingNormalAttackTarget = null;
        mIsWaitingForVisual = false;
        mIsActionComplete = false;

        BattleUnitFactory.Instance.ClearPool();

        BattleManagerLifecycle.DisposeAllManagers();

        LogKit.Log("[BattleSystem] 战斗结束，所有战斗数据已清空");
    }

    private void OnBattlePanelOpened(BattlePanelOpenedEvent evt)
    {
        BattleUpdateDriver.Create().StartDriving();
        StartBattle();
        SendBattleUIData();
    }

    private void OnBattlePanelClosed(BattlePanelClosedEvent evt)
    {
        EndBattle();
    }

    private void OnBattleEndRequested(BattleEndRequestedEvent evt)
    {
        EndBattle();
    }

    private void OnBattleUIRefreshRequested(BattleUIRefreshRequestedEvent evt)
    {
        SendBattleUIData();
    }

    private void SendBattleUIData()
    {
        EventKit.Type.Send(new BattleUIDataEvent
        {
            PlayerUnits = mPlayerUnits,
            EnemyUnits = mEnemyUnits,
            HandCards = CardHand.Instance.Cards,
            CurrentEnergy = EnergyManager.Instance.CurrentEnergy,
            MaxEnergy = EnergyManager.Instance.MaxEnergy,
            CurrentActorId = TurnOrderManager.Instance.CurrentActor?.UnitId ?? "",
            CurrentActorType = TurnOrderManager.Instance.CurrentActor?.UnitType ?? BattleUnitType.Player,
            TurnOrderUnits = TurnOrderManager.Instance.GetUnitsInOrder(),
            ManualDiscardCost = GetManualDiscardCost()
        });
    }

    private void OnCardPlayRequested(CardPlayRequestedEvent evt)
    {
        if (evt.Card != null && evt.Target != null)
        {
            PlayCard(evt.Card, evt.Target);
        }
    }

    private void OnNormalAttackRequested(NormalAttackRequestedEvent evt)
    {
        if (evt.Attacker != null && evt.Target != null)
        {
            ExecuteNormalAttack(evt.Attacker, evt.Target);
        }
    }

    private void OnManualDiscardRequested(ManualDiscardRequestedEvent evt)
    {
        ManualDiscard();
    }

    #endregion

    #region 卡牌处理

    /// <summary>
    /// 消耗能量并请求播放技能卡牌的表现效果。
    /// </summary>
    public bool PlayCard(SkillCardData card, BattleUnitData target)
    {
        // 先确定攻击者，用于查询角色Handler
        var actor = TurnOrderManager.Instance.CurrentActor;
        BattleUnitData attacker = actor != null ? mPlayerUnits.FirstOrDefault(u => u.UnitId == actor.UnitId) : null;

        // 星魂减费：查询角色专属Handler获取实际能量消耗（Mage星魂1：技能2消耗变0）
        var handler = CharacterHandlerRegistry.Get(attacker?.UnitId ?? "");
        int starLevel = attacker?.StarLevel ?? 0;
        int effectiveCost = handler != null
            ? handler.GetEffectiveSkillCost(card.SkillType, card.Cost, starLevel)
            : card.Cost;

        LogKit.Log($"[BattleSystem] PlayCard: skillId={card.SkillId}, skillType={card.SkillType}, 原Cost={card.Cost}, effectiveCost={effectiveCost}, starLevel={starLevel}, handler={(handler != null ? handler.CharacterId : "无")}");

        if (!EnergyManager.Instance.SpendEnergy(effectiveCost))
        {
            LogKit.Log($"[BattleSystem] 能量不足，需要{effectiveCost}点能量（原{card.Cost}）");
            return false;
        }

        // 自身HP消耗（Warrior技能1：消耗自身生命上限30%的生命值，保留至少1点生命）
        if (card.SelfHpCostPercent > 0 && attacker != null && attacker.IsAlive)
        {
            int hpCost = Mathf.RoundToInt(attacker.MaxHp * card.SelfHpCostPercent);
            int actualHpCost = Mathf.Min(hpCost, Mathf.Max(0, attacker.CurrentHp - 1));
            if (actualHpCost > 0)
            {
                attacker.CurrentHp -= actualHpCost;
                LogKit.Log($"[BattleSystem] {attacker.Name} 技能HP消耗: {actualHpCost}（{card.SelfHpCostPercent * 100f:F0}%最大生命）");
            }
        }

        // 移除手牌、弃牌（不依赖表现层，可立即执行）
        CardHand.Instance.RemoveCard(card);
        CardDeck.Instance.Discard(card);

        // 触发技能执行回调：HP转化/状态切换/能量恢复/护盾预置等
        // 在表现层播放前执行，确保后续伤害结算时状态已就位（Assassin神鬼无赦、Warrior终结技HP转化等）
        if (handler != null && attacker != null)
        {
            handler.OnSkillExecute(card.SkillType, attacker.StarLevel, attacker, target);
        }

        List<string> targetIds = new List<string>();
        List<int> targetIndices = new List<int>();
        if (card.TargetType == TargetType.AllEnemies)
        {
            var aliveEnemies = mEnemyUnits.Where(e => e.IsAlive).ToList();
            foreach (var e in aliveEnemies)
            {
                targetIds.Add(e.UnitId);
                targetIndices.Add(mEnemyUnits.IndexOf(e));
            }
            LogKit.Log($"[BattleSystem] AllEnemies目标构建: mEnemyUnits总数={mEnemyUnits.Count}, 存活数={aliveEnemies.Count}, 目标ID=[{string.Join(",", targetIds)}]");
        }
        else if (card.TargetType == TargetType.AllAllies)
        {
            targetIds = mPlayerUnits.Where(e => e.IsAlive).Select(e => e.UnitId).ToList();
        }
        else if (target != null)
        {
            targetIds.Add(target.UnitId);
            // 单目标技能：记录目标在列表中的精确索引（近战/友方表现需要）
            if (target.UnitType == BattleUnitType.Enemy)
            {
                targetIndices.Add(mEnemyUnits.IndexOf(target));
            }
            else if (target.UnitType == BattleUnitType.Player)
            {
                targetIndices.Add(mPlayerUnits.IndexOf(target));
            }
        }

        // 存储待处理技能数据，等待表现层回调
        mPendingCard = card;
        mPendingAttacker = attacker;
        mIsWaitingForVisual = true;

        LogKit.Log($"[BattleSystem] 技能 {card.SkillId} 表现请求发送，目标数:{targetIds.Count}，等待表现层播放");

        // 发送表现请求（表现层播放动画，命中时回调 OnSkillVisualHit，完成时回调 OnSkillVisualComplete）
        EventKit.Type.Send(new SkillVisualRequestEvent
        {
            SkillId = card.SkillId,
            AttackerUnitId = attacker?.UnitId ?? "",
            TargetUnitIds = targetIds,
            TargetUnitIndices = targetIndices
        });

        return true;
    }

    /// <summary>
    /// 表现层命中回调：对单个目标执行技能效果（伤害结算/治疗等）
    /// </summary>
    private void OnSkillVisualHit(SkillVisualHitEvent evt)
    {
        if (!mIsWaitingForVisual) return;

        // 敌人攻击命中：直接对玩家目标造成伤害
        if (mIsEnemyAttacking && mPendingEnemyTarget != null)
        {
            if (mPendingEnemyTarget.UnitId == evt.TargetUnitId && mPendingEnemyTarget.IsAlive)
            {
                DealDamage(mPendingEnemyAttacker, mPendingEnemyTarget, 1.0f);
                LogKit.Log($"[BattleSystem] 敌人攻击表现命中 {mPendingEnemyTarget.UnitId}，已执行伤害");
            }
            return;
        }

        // 普攻命中：mPendingCard为空但mPendingNormalAttackTarget不为空
        if (mPendingCard == null && mPendingNormalAttackTarget != null)
        {
            if (mPendingNormalAttackTarget.UnitId == evt.TargetUnitId && mPendingNormalAttackTarget.IsAlive)
            {
                ExecuteNormalAttackInternal(mPendingAttacker, mPendingNormalAttackTarget);
                LogKit.Log($"[BattleSystem] 普攻表现层命中 {mPendingNormalAttackTarget.UnitId}，已执行伤害");
            }
            return;
        }

        // 技能卡牌命中
        if (mPendingCard == null) return;

        BattleUnitData target = null;

        // 优先使用索引定位目标（解决同名敌人UnitId重复导致FirstOrDefault总返回索引0的问题）
        if (evt.TargetUnitIndex >= 0)
        {
            var targetType = mPendingCard.TargetType;
            if (targetType == TargetType.Enemy || targetType == TargetType.AllEnemies)
            {
                if (evt.TargetUnitIndex < mEnemyUnits.Count)
                    target = mEnemyUnits[evt.TargetUnitIndex];
            }
            else if (targetType == TargetType.Ally || targetType == TargetType.AllAllies || targetType == TargetType.Self)
            {
                if (evt.TargetUnitIndex < mPlayerUnits.Count)
                    target = mPlayerUnits[evt.TargetUnitIndex];
            }
        }

        // 索引无效时回退到UnitId查找
        if (target == null)
        {
            target = mEnemyUnits.FirstOrDefault(u => u.UnitId == evt.TargetUnitId)
                  ?? mPlayerUnits.FirstOrDefault(u => u.UnitId == evt.TargetUnitId);
        }

        if (target == null || !target.IsAlive) return;

        // AllAllies 目标类型不执行二级效果（二级效果在完成回调中统一执行）
        bool executeSecondary = mPendingCard.TargetType != TargetType.AllAllies;
        ExecuteSkillEffectOnTarget(mPendingCard, mPendingAttacker, target, executeSecondary);

        LogKit.Log($"[BattleSystem] 表现层命中 {target.UnitId}，已执行技能效果");
    }

    /// <summary>
    /// 表现完成后结算后续效果，并结束当前行动。
    /// </summary>
    private void OnSkillVisualComplete(SkillVisualCompleteEvent evt)
    {
        if (!mIsWaitingForVisual) return;

        // 敌人攻击表现完成
        if (mIsEnemyAttacking)
        {
            mIsWaitingForVisual = false;
            mIsEnemyAttacking = false;
            mPendingEnemyAttacker = null;
            mPendingEnemyTarget = null;

            TurnOrderManager.Instance.CompleteAction();
            mIsActionComplete = true;
            CheckBattleEndConditions();

            LogKit.Log("[BattleSystem] 敌人攻击表现完成，回合行动结束");
            return;
        }

        // 普攻表现完成
        if (mPendingCard == null && mPendingNormalAttackTarget != null)
        {
            mIsWaitingForVisual = false;

            TurnOrderManager.Instance.CompleteAction();
            mIsActionComplete = true;
            CheckBattleEndConditions();

            LogKit.Log("[BattleSystem] 普攻表现完成，回合行动结束");

            mPendingNormalAttackTarget = null;
            mPendingAttacker = null;
            return;
        }

        // 技能卡牌表现完成
        if (mPendingCard == null) return;

        mIsWaitingForVisual = false;

        // AllAllies 的二级效果在所有目标处理完后统一执行，作用于全体我方（Archer终结技全体暴击伤害+10%等）
        if (mPendingCard.TargetType == TargetType.AllAllies && mPendingCard.SecondaryEffect != (SkillEffectType)(-1))
        {
            foreach (var ally in mPlayerUnits.Where(u => u.IsAlive))
            {
                ExecuteSecondaryEffect(mPendingCard, mPendingAttacker, ally);
            }
        }

        EventKit.Type.Send(new CardEvents.CardPlayedEvent
        {
            Card = mPendingCard,
            Target = null
        });

        TurnOrderManager.Instance.CompleteAction();
        mIsActionComplete = true;

        CheckBattleEndConditions();

        LogKit.Log($"[BattleSystem] 技能 {mPendingCard.SkillId} 表现完成，回合行动结束");

        mPendingCard = null;
        mPendingAttacker = null;
    }

    #endregion

    #region 技能与伤害

    private void ExecuteSkillEffect(SkillCardData card, BattleUnitData target)
    {
        var actor = TurnOrderManager.Instance.CurrentActor;
        BattleUnitData attacker = actor != null ? mPlayerUnits.FirstOrDefault(u => u.UnitId == actor.UnitId) : null;

        if (card.TargetType == TargetType.AllEnemies)
        {
            List<BattleUnitData> enemies = mEnemyUnits.Where(e => e.IsAlive).ToList();
            foreach (var enemy in enemies)
            {
                if (enemy.IsAlive)
                {
                    ExecuteSkillEffectOnTarget(card, attacker, enemy, true);
                }
            }
            return;
        }

        if (card.TargetType == TargetType.AllAllies)
        {
            List<BattleUnitData> allies = mPlayerUnits.Where(e => e.IsAlive).ToList();
            foreach (var ally in allies)
            {
                if (ally.IsAlive)
                {
                    ExecuteSkillEffectOnTarget(card, attacker, ally, false);
                }
            }
            if (card.SecondaryEffect != (SkillEffectType)(-1))
            {
                ExecuteSecondaryEffect(card, attacker, attacker);
            }
            return;
        }

        ExecuteSkillEffectOnTarget(card, attacker, target);
    }

    private void ExecuteSkillEffectOnTarget(SkillCardData card, BattleUnitData attacker, BattleUnitData target, bool executeSecondary = true)
    {
        if (target == null || !target.IsAlive) return;

        bool isSelfTarget = attacker != null && target.UnitId == attacker.UnitId;

        // 攻击技能的自身增益次要效果在造成伤害前施加（Warrior技能1：先提升攻击力再造成伤害，使伤害享受攻击力加成）
        // ExecuteSecondaryEffect内部会判断增益效果作用于攻击者自身
        bool isPreDamageBuff = executeSecondary
            && card.SecondaryEffect != (SkillEffectType)(-1)
            && card.EffectType == SkillEffectType.Damage
            && attacker != null
            && target.UnitType == BattleUnitType.Enemy
            && (card.SecondaryEffect == SkillEffectType.AttackUp
                || card.SecondaryEffect == SkillEffectType.DefenseUp
                || card.SecondaryEffect == SkillEffectType.SpeedUp
                || card.SecondaryEffect == SkillEffectType.CritRateUp
                || card.SecondaryEffect == SkillEffectType.CritDamageUp
                || card.SecondaryEffect == SkillEffectType.DamageUp);

        if (isPreDamageBuff)
        {
            ExecuteSecondaryEffect(card, attacker, attacker);
        }

        switch (card.EffectType)
        {
            case SkillEffectType.Damage:
                if (attacker != null)
                {
                    int talentValue = attacker.GetTalentValue(card.SkillType);
                    int effectiveValue = talentValue > 0 ? talentValue : card.Value;
                    float skillMultiplier = attacker.SkillDamageMultiplier;
                    float extraCritChance = attacker.ExtraCritDamageChance;
                    float extraCritValue = attacker.ExtraCritDamageValue;

                    // 角色专属技能处理器（Knight神圣伤害等）
                    float handlerMultiplier = 1.0f;
                    float extraDefensePenetration = 0f;
                    float extraCritRate = 0f;
                    var handler = CharacterHandlerRegistry.Get(attacker.UnitId);
                    if (handler != null)
                    {
                        // 有专属处理器时，完全由处理器决定伤害倍率，跳过通用星魂加成
                        handlerMultiplier = handler.OnBeforeDamage(card.SkillType, attacker.StarLevel, out extraDefensePenetration);
                        // Handler额外暴击率（Archer星魂3普攻必暴、Assassin星魂1低血量目标暴击+15%等）
                        extraCritRate = handler.GetExtraCritRate(card.SkillType, attacker.StarLevel, target);
                    }
                    else
                    {
                        // 无专属处理器时，使用通用星魂加成
                        if (card.SkillType == "Ultimate" && attacker.UltimateBonus > 0)
                        {
                            handlerMultiplier += attacker.UltimateBonus;
                        }
                    }

                    float totalMultiplier = effectiveValue * 0.01f * skillMultiplier * handlerMultiplier;
                    DealDamage(attacker, target, totalMultiplier, extraCritChance, extraCritValue, extraDefensePenetration, card.SkillType, extraCritRate);

                    // 伤害后Hook（Assassin星魂2普攻后行动提前等）
                    int finalDamage = Mathf.CeilToInt(attacker.CurrentAttack * totalMultiplier);
                    handler?.OnAfterDamage(card.SkillType, attacker.StarLevel, attacker, target, finalDamage);
                }
                break;
            case SkillEffectType.Heal:
                int healTalentVal = attacker != null ? attacker.GetTalentValue(card.SkillType) : 0;
                int healTalentVal2 = attacker != null ? attacker.GetTalentValue2(card.SkillType) : 0;
                int effectiveHealPercent = healTalentVal > 0 ? healTalentVal : card.HealPercent;
                int effectiveFlatHeal = healTalentVal2 > 0 ? healTalentVal2 : card.Value;
                int healAmount = effectiveFlatHeal;
                if (effectiveHealPercent > 0 && attacker != null)
                {
                    healAmount += Mathf.RoundToInt(attacker.MaxHp * effectiveHealPercent * 0.01f);
                }
                if (attacker != null)
                {
                    healAmount = Mathf.RoundToInt(healAmount * attacker.HealMultiplier);
                    // 角色专属治疗加成（Healer星魂1：治疗量+10%、Knight圣光回复等）
                    var healHandler = CharacterHandlerRegistry.Get(attacker.UnitId);
                    if (healHandler != null)
                    {
                        // 有专属处理器时，完全由处理器决定治疗加成
                        float healBonus = healHandler.OnBeforeHeal(card.SkillType, attacker.StarLevel);
                        if (healBonus > 0)
                        {
                            healAmount = Mathf.RoundToInt(healAmount * (1f + healBonus));
                        }
                    }
                    else if (attacker.StarHealBonus > 0)
                    {
                        // 无专属处理器时，使用通用星魂治疗加成
                        healAmount = Mathf.RoundToInt(healAmount * (1f + attacker.StarHealBonus));
                    }

                    // Healer星魂4：终结技对生命值≤50%的我方目标治疗量额外+10%
                    if (attacker.UnitId == "Healer" && attacker.StarLevel >= 4
                        && card.SkillType == "Ultimate" && target.CurrentHpPercent <= 0.5f)
                    {
                        healAmount = Mathf.RoundToInt(healAmount * 1.10f);
                        LogKit.Log($"[BattleSystem] Healer星魂4触发：对{target.Name}额外治疗+10%（生命值≤50%）");
                    }
                }
                HealUnit(target, healAmount);

                if (attacker != null && attacker.DefenseUpAfterHealPercent > 0)
                {
                    target.AddStatusEffect(StatusEffectType.DefenseUp,
                        Mathf.RoundToInt(attacker.DefenseUpAfterHealPercent * 100),
                        attacker.DefenseUpAfterHealDuration,
                        target.UnitId == attacker.UnitId);
                    LogKit.Log($"[BattleSystem] {attacker.Name} 治疗触发防御提升效果");
                }
                break;
            case SkillEffectType.EnergyGain:
                EnergyManager.Instance.GainEnergy(card.Value);
                break;
            case SkillEffectType.DrawCard:
                {
                    // 概率检测（Mage技能2：50%概率抽2张牌等）
                    // Talent Value overrides Probability, Value2 unused (draw count stays as card.Value)
                    int drawTalentVal = attacker != null ? attacker.GetTalentValue(card.SkillType) : 0;
                    float effectiveProb = drawTalentVal > 0 ? drawTalentVal : card.Probability;
                    if (effectiveProb >= 100f || Random.value < effectiveProb * 0.01f)
                    {
                        CardHand.Instance.AddCards(CardDeck.Instance.Draw(card.Value));
                        LogKit.Log($"[BattleSystem] 抽牌成功：{card.Value}张（概率{effectiveProb:F0}%）");
                    }
                    else
                    {
                        LogKit.Log($"[BattleSystem] 抽牌失败：概率{effectiveProb:F0}%未命中");
                    }
                }
                break;
            case SkillEffectType.Shield:
                {
                    // Shield supports dual values: Value=defense percent, Value2=flat shield amount
                    // For Knight_Skill1: Value=12(def%), Value2=120(flat)
                    int shieldTalentVal = attacker != null ? attacker.GetTalentValue(card.SkillType) : 0;
                    int shieldTalentVal2 = attacker != null ? attacker.GetTalentValue2(card.SkillType) : 0;
                    int defPercent = shieldTalentVal > 0 ? shieldTalentVal : card.HealPercent;
                    int flatShield = shieldTalentVal2 > 0 ? shieldTalentVal2 : card.Value;
                    int shieldAmount = flatShield;
                    if (defPercent > 0 && attacker != null)
                    {
                        shieldAmount += Mathf.RoundToInt(attacker.CurrentDefense * defPercent * 0.01f);
                    }
                    // 角色专属护盾加成（Knight圣盾被动等）
                    if (attacker != null)
                    {
                        var shieldHandler = CharacterHandlerRegistry.Get(attacker.UnitId);
                        if (shieldHandler != null)
                        {
                            float shieldBonus = shieldHandler.OnShieldAbsorb(attacker.StarLevel);
                            if (shieldBonus > 0)
                            {
                                shieldAmount = Mathf.RoundToInt(shieldAmount * (1f + shieldBonus));
                            }
                        }
                    }
                    AddShield(target, shieldAmount);
                }
                break;
            case SkillEffectType.AttackUp:
                int atkUpVal = (attacker != null && attacker.GetTalentValue(card.SkillType) > 0) ? attacker.GetTalentValue(card.SkillType) : card.Value;
                target.AddStatusEffect(StatusEffectType.AttackUp, atkUpVal, card.Duration, isSelfTarget);
                break;
            case SkillEffectType.AttackDown:
                int atkDownVal = (attacker != null && attacker.GetTalentValue(card.SkillType) > 0) ? attacker.GetTalentValue(card.SkillType) : card.Value;
                target.AddStatusEffect(StatusEffectType.AttackDown, atkDownVal, card.Duration, isSelfTarget);
                break;
            case SkillEffectType.DefenseUp:
                int defUpVal = (attacker != null && attacker.GetTalentValue(card.SkillType) > 0) ? attacker.GetTalentValue(card.SkillType) : card.Value;
                target.AddStatusEffect(StatusEffectType.DefenseUp, defUpVal, card.Duration, isSelfTarget);
                break;
            case SkillEffectType.DefenseDown:
                int defDownVal = (attacker != null && attacker.GetTalentValue(card.SkillType) > 0) ? attacker.GetTalentValue(card.SkillType) : card.Value;
                target.AddStatusEffect(StatusEffectType.DefenseDown, defDownVal, card.Duration, isSelfTarget);
                break;
            case SkillEffectType.SpeedUp:
                int spdUpVal = (attacker != null && attacker.GetTalentValue(card.SkillType) > 0) ? attacker.GetTalentValue(card.SkillType) : card.Value;
                target.AddStatusEffect(StatusEffectType.SpeedUp, spdUpVal, card.Duration, isSelfTarget);
                TurnOrderManager.Instance.SpeedUp(target.UnitId, target.CurrentSpeed);
                break;
            case SkillEffectType.SpeedDown:
                int spdDownVal = (attacker != null && attacker.GetTalentValue(card.SkillType) > 0) ? attacker.GetTalentValue(card.SkillType) : card.Value;
                target.AddStatusEffect(StatusEffectType.SpeedDown, spdDownVal, card.Duration, isSelfTarget);
                TurnOrderManager.Instance.SpeedUp(target.UnitId, target.CurrentSpeed);
                break;
            case SkillEffectType.DamageUp:
                int dmgUpVal = (attacker != null && attacker.GetTalentValue(card.SkillType) > 0) ? attacker.GetTalentValue(card.SkillType) : card.Value;
                target.AddStatusEffect(StatusEffectType.DamageUp, dmgUpVal, card.Duration, isSelfTarget);
                break;
            case SkillEffectType.CritRateUp:
                int critRateVal = (attacker != null && attacker.GetTalentValue(card.SkillType) > 0) ? attacker.GetTalentValue(card.SkillType) : card.Value;
                target.AddStatusEffect(StatusEffectType.CritRateUp, critRateVal, card.Duration, isSelfTarget);
                break;
            case SkillEffectType.CritDamageUp:
                int critDmgVal = (attacker != null && attacker.GetTalentValue(card.SkillType) > 0) ? attacker.GetTalentValue(card.SkillType) : card.Value;
                target.AddStatusEffect(StatusEffectType.CritDamageUp, critDmgVal, card.Duration, isSelfTarget);
                break;
            case SkillEffectType.DefensePenetration:
                int defPenVal = (attacker != null && attacker.GetTalentValue(card.SkillType) > 0) ? attacker.GetTalentValue(card.SkillType) : card.Value;
                target.AddStatusEffect(StatusEffectType.DefensePenetration, defPenVal, card.Duration, isSelfTarget);
                break;
            case SkillEffectType.DiscardAndDraw:
                ExecuteDiscardAndDraw();
                break;
            case SkillEffectType.ActionAdvance:
                // 行动提前：将目标单位行动值减少 BaseAV * ratio（ratio为百分比，如24表示24%）
                // 规则：提前量 = 10000/速度 * 百分比，只由速度决定，与当前剩余路程无关
                int actionAdvanceVal = (attacker != null && attacker.GetTalentValue(card.SkillType) > 0) ? attacker.GetTalentValue(card.SkillType) : card.Value;
                TurnOrderManager.Instance.AdvanceTurn(target.UnitId, actionAdvanceVal * 0.01f);
                LogKit.Log($"[BattleSystem] {card.SkillId} 对 {target.UnitId} 施加行动提前 {actionAdvanceVal}%");
                break;
        }

        if (attacker != null)
        {
            if (attacker.AttackUpAfterSkillPercent > 0)
            {
                attacker.AddStatusEffect(StatusEffectType.AttackUp,
                    Mathf.RoundToInt(attacker.AttackUpAfterSkillPercent * 100),
                    attacker.AttackUpAfterSkillDuration, true);
                LogKit.Log($"[BattleSystem] {attacker.Name} 释放技能触发攻击提升效果 +{attacker.AttackUpAfterSkillPercent * 100:F0}%");
            }

            if (attacker.DotDamagePercent > 0 && attacker.DotDuration > 0 && card.EffectType == SkillEffectType.Damage)
            {
                target.ApplyDot(attacker.UnitId, attacker.DotDamagePercent, attacker.DotDuration, attacker.BaseAttack);
                LogKit.Log($"[BattleSystem] {attacker.Name} 技能灼烧 {target.Name}，每回合造成{attacker.DotDamagePercent * 100:F0}%攻击力伤害，持续{attacker.DotDuration}回合");
            }
        }

        // 增益次要效果已在伤害前施加（isPreDamageBuff），此处仅处理未施加的次要效果（减益等）
        if (executeSecondary && card.SecondaryEffect != (SkillEffectType)(-1) && !isPreDamageBuff)
            {
                ExecuteSecondaryEffect(card, attacker, target);
            }
        }

    private void ExecuteSecondaryEffect(SkillCardData card, BattleUnitData attacker, BattleUnitData target)
    {
        // 概率检测（Knight技能2圣光惩戒：65%概率降低敌方攻击力）
        // Probability默认100（必定触发），仅当0<Probability<100时进行概率判定
        if (card.Probability > 0f && card.Probability < 100f)
        {
            if (Random.value >= card.Probability * 0.01f)
            {
                LogKit.Log($"[BattleSystem] 次要效果概率未命中：{card.Probability}%（随机={Random.value * 100f:F0}%）");
                return;
            }
            LogKit.Log($"[BattleSystem] 次要效果概率命中：{card.Probability}%（随机={Random.value * 100f:F0}%）");
        }

        // Talent Value2 overrides SecondaryValue if > 0
        int secondaryVal = (attacker != null && attacker.GetTalentValue2(card.SkillType) > 0)
            ? attacker.GetTalentValue2(card.SkillType)
            : card.SecondaryValue;

        if (card.SecondaryEffect == (SkillEffectType)99)
        {
            if (attacker != null)
            {
                float multiplier = 1f - secondaryVal * 0.01f;
                TurnOrderManager.Instance.SetNextActionValueMultiplier(attacker.UnitId, multiplier);
                LogKit.Log($"[BattleSystem] {attacker.UnitId} 设置下次行动值倍率: {multiplier}");
            }
            return;
        }

        // 确定次要效果的目标：
        // - 增益效果(AttackUp/DefenseUp/SpeedUp/CritRateUp/CritDamageUp/DamageUp)作用于敌方伤害技能时，应施加于攻击者自身
        // - 减益效果(AttackDown/DefenseDown/SpeedDown)施加于目标
        // - 增益效果作用于友方目标技能时，施加于目标（如Archer_Skill1的DamageUp给友方）
        bool isEnemyDamageSkill = card.EffectType == SkillEffectType.Damage && target != null && attacker != null
            && target.UnitType == BattleUnitType.Enemy;
        bool isBuffEffect = card.SecondaryEffect == SkillEffectType.AttackUp
            || card.SecondaryEffect == SkillEffectType.DefenseUp
            || card.SecondaryEffect == SkillEffectType.SpeedUp
            || card.SecondaryEffect == SkillEffectType.CritRateUp
            || card.SecondaryEffect == SkillEffectType.CritDamageUp
            || card.SecondaryEffect == SkillEffectType.DamageUp;

        BattleUnitData secondaryTarget = (isEnemyDamageSkill && isBuffEffect && attacker != null) ? attacker : target;
        if (secondaryTarget == null) return;

        bool isSelfTarget = attacker != null && secondaryTarget.UnitId == attacker.UnitId;

        switch (card.SecondaryEffect)
        {
            case SkillEffectType.AttackUp:
                secondaryTarget.AddStatusEffect(StatusEffectType.AttackUp, secondaryVal, card.SecondaryDuration, isSelfTarget);
                break;
            case SkillEffectType.AttackDown:
                secondaryTarget.AddStatusEffect(StatusEffectType.AttackDown, secondaryVal, card.SecondaryDuration, isSelfTarget);
                break;
            case SkillEffectType.DefenseUp:
                secondaryTarget.AddStatusEffect(StatusEffectType.DefenseUp, secondaryVal, card.SecondaryDuration, isSelfTarget);
                break;
            case SkillEffectType.DefenseDown:
                secondaryTarget.AddStatusEffect(StatusEffectType.DefenseDown, secondaryVal, card.SecondaryDuration, isSelfTarget);
                break;
            case SkillEffectType.SpeedUp:
                secondaryTarget.AddStatusEffect(StatusEffectType.SpeedUp, secondaryVal, card.SecondaryDuration, isSelfTarget);
                break;
            case SkillEffectType.SpeedDown:
                secondaryTarget.AddStatusEffect(StatusEffectType.SpeedDown, secondaryVal, card.SecondaryDuration, isSelfTarget);
                break;
            case SkillEffectType.CritRateUp:
                secondaryTarget.AddStatusEffect(StatusEffectType.CritRateUp, secondaryVal, card.SecondaryDuration, isSelfTarget);
                break;
            case SkillEffectType.CritDamageUp:
                secondaryTarget.AddStatusEffect(StatusEffectType.CritDamageUp, secondaryVal, card.SecondaryDuration, isSelfTarget);
                break;
            case SkillEffectType.DamageUp:
                secondaryTarget.AddStatusEffect(StatusEffectType.DamageUp, secondaryVal, card.SecondaryDuration, isSelfTarget);
                break;
            case SkillEffectType.ActionAdvance:
                // 行动提前：次要效果也支持，如Archer_Ultimate的24%提前
                TurnOrderManager.Instance.AdvanceTurn(secondaryTarget.UnitId, card.SecondaryValue * 0.01f);
                LogKit.Log($"[BattleSystem] {card.SkillId} 次要效果: {secondaryTarget.UnitId} 行动提前 {card.SecondaryValue}%");
                break;
        }
    }

    private void ExecuteDiscardAndDraw()
    {
        List<SkillCardData> handCards = CardHand.Instance.Cards;
        int discardCount = handCards.Count;
        
        CardDeck.Instance.Discard(handCards);
        CardHand.Instance.Clear();
        
        List<SkillCardData> drawnCards = CardDeck.Instance.Draw(discardCount);
        CardHand.Instance.AddCards(drawnCards);
        
        LogKit.Log($"[BattleSystem] 弃置{discardCount}张手牌，抽取{drawnCards.Count}张新手牌");
    }

    private void AddShield(BattleUnitData target, int amount)
    {
        if (!target.IsAlive)
            return;
        target.Shield += amount;
        EventKit.Type.Send(new UnitEvents.UnitShieldEvent
        {
            Unit = target,
            ShieldAmount = amount
        });
    }

    private int CalculateDamage(BattleUnitData attacker, BattleUnitData target, float skillMultiplier, float extraCritChance = 0f, float extraCritValue = 0f, float extraDefensePenetration = 0f, float extraCritRate = 0f)
    {
        float attack = attacker.CurrentAttack;
        float multiplier = skillMultiplier;
        float damageBonus = 1f + attacker.CurrentDamageBonus;

        // 暴击率：基础暴击率 + Handler额外暴击率（Archer星魂3普攻必暴等），并叠加惶恐状态对暴击伤害的加成
        float totalCritRate = Mathf.Clamp01(attacker.CurrentCritRate + extraCritRate);
        bool isCrit = Random.value < totalCritRate;

        float critDamage = attacker.CurrentCritDamage;
        // 惶恐状态：我方对惶恐敌方目标造成伤害时，暴击伤害+15%（Knight终结技）
        if (isCrit && target.HasStatusEffect(StatusEffectType.Fear) && attacker.UnitType == BattleUnitType.Player)
        {
            critDamage += 0.15f;
            LogKit.Log($"[BattleSystem] {target.Name} 处于惶恐状态，{attacker.Name} 暴击伤害+15%");
        }
        float critMultiplier = isCrit ? (1f + critDamage) : 1f;

        if (isCrit && extraCritChance > 0 && Random.value < extraCritChance)
        {
            critMultiplier += extraCritValue;
            LogKit.Log($"[BattleSystem] {attacker.Name} 触发额外暴击伤害 +{extraCritValue * 100:F0}%");
        }

        float attackerLevelFactor = attacker.Level * 10f + 200f;
        float targetLevelFactor = target.Level * 10f + 200f;
        float totalIgnoreDefense = Mathf.Min(1f, attacker.CurrentDefenseReduction + attacker.CurrentIgnoreDefense + extraDefensePenetration);
        float defenseFactor = attackerLevelFactor / (targetLevelFactor * (1f - totalIgnoreDefense) + attackerLevelFactor);

        float resistance = Mathf.Max(-1f, target.CurrentResistance - attacker.CurrentResistancePenetration);
        float resistanceFactor = 1f - resistance;

        float rawDamage = attack * multiplier * damageBonus * critMultiplier * defenseFactor * resistanceFactor;

        return Mathf.CeilToInt(rawDamage);
    }

    private void DealDamage(BattleUnitData attacker, BattleUnitData target, float skillMultiplier, float extraCritChance = 0f, float extraCritValue = 0f, float extraDefensePenetration = 0f, string skillType = "NormalAttack", float extraCritRate = 0f)
    {
        if (!target.IsAlive)
            return;

        int rawDamage = CalculateDamage(attacker, target, skillMultiplier, extraCritChance, extraCritValue, extraDefensePenetration, extraCritRate);
        int remainingDamage = rawDamage;

        if (target.Shield > 0)
        {
            if (target.Shield >= remainingDamage)
            {
                target.Shield -= remainingDamage;
                remainingDamage = 0;
            }
            else
            {
                remainingDamage -= target.Shield;
                target.Shield = 0;
            }
        }

        int actualDamage = Mathf.Max(1, remainingDamage);
        target.CurrentHp -= actualDamage;

        EventKit.Type.Send(new UnitEvents.UnitDamagedEvent
        {
            Unit = target,
            Damage = actualDamage,
            Attacker = attacker
        });

        EventKit.Type.Send(new UnitEvents.UnitHealthUpdatedEvent
        {
            Unit = target
        });

        // 受击回调：目标角色Handler处理受击后效果（Healer星魂3受击回能）
        var targetHandler = CharacterHandlerRegistry.Get(target.UnitId);
        if (targetHandler != null && target.UnitType == BattleUnitType.Player)
        {
            targetHandler.OnBeingAttacked(target.StarLevel, attacker, target);
        }

        if (target.CurrentHp <= 0)
        {
            // 致命伤害免疫：Warrior星魂3受到致命攻击时不会死亡，立即回复至生命上限的25%
            if (targetHandler != null && target.UnitType == BattleUnitType.Player
                && targetHandler.OnLethalDamage(target.StarLevel, out int healAmount) && healAmount > 0)
            {
                target.CurrentHp = Mathf.Max(1, target.CurrentHp + healAmount);
                if (target.CurrentHp > target.MaxHp) target.CurrentHp = target.MaxHp;
                LogKit.Log($"[BattleSystem] {target.Name} 致命伤害免疫，回复{healAmount}生命值");
                EventKit.Type.Send(new UnitEvents.UnitHealedEvent
                {
                    Unit = target,
                    HealAmount = healAmount
                });
                EventKit.Type.Send(new UnitEvents.UnitHealthUpdatedEvent { Unit = target });
                return;
            }

            target.CurrentHp = 0;
            target.IsAlive = false;

            EventKit.Type.Send(new UnitEvents.UnitDefeatedEvent
            {
                Unit = target
            });

            // 击杀回调：攻击者角色Handler处理击杀后效果（Assassin刺杀状态触发行动提前、星魂3回能）
            if (attacker != null && target.UnitType == BattleUnitType.Enemy && attacker.UnitType == BattleUnitType.Player)
            {
                var attackerHandler = CharacterHandlerRegistry.Get(attacker.UnitId);
                if (attackerHandler != null)
                {
                    attackerHandler.OnEnemyKilled(skillType, attacker.StarLevel, attacker, target);
                    LogKit.Log($"[BattleSystem] {attacker.Name} 击杀 {target.Name}，触发击杀回调");
                }
            }
        }
    }

    private void HealUnit(BattleUnitData target, int healAmount)
    {
        if (!target.IsAlive)
            return;

        target.CurrentHp = Mathf.Min(target.MaxHp, target.CurrentHp + healAmount);

        EventKit.Type.Send(new UnitEvents.UnitHealedEvent
        {
            Unit = target,
            HealAmount = healAmount
        });

        EventKit.Type.Send(new UnitEvents.UnitHealthUpdatedEvent
        {
            Unit = target
        });
    }

    private void CheckBattleEndConditions()
    {
        int alivePlayerCount = mPlayerUnits.Count(u => u.IsAlive);
        int aliveEnemyCount = mEnemyUnits.Count(u => u.IsAlive);

        BattleStateManager.Instance.CheckBattleEndConditions(
            alivePlayerCount,
            aliveEnemyCount,
            WaveManager.Instance.IsLastWave,
            WaveManager.Instance.IsWaveActive
        );
    }

    #endregion

    #region 回合推进

    /// <summary>
    /// 请求波次管理器开始下一波敌人。
    /// </summary>
    public void StartNextWave()
    {
        WaveManager.Instance.StartNextWave();
    }

    /// <summary>
    /// 在手牌未满时从牌堆抽取一张卡牌。
    /// </summary>
    public void DrawCard()
    {
        if (!CardHand.Instance.IsFull)
        {
            var card = CardDeck.Instance.Draw();
            if (card != null)
                CardHand.Instance.AddCard(card);
        }
    }

    /// <summary>
    /// 处理战斗开始：恢复回合资源、启动首波并触发角色回调。
    /// </summary>
    public void OnBattleStart()
    {
        EnergyManager.Instance.OnTurnStart();
        WaveManager.Instance.StartNextWave();

        // 调用所有玩家角色的战斗开始回调（Warrior星魂4速度+12%、Healer星魂2生命上限+15%、Knight星魂1全体护盾等）
        foreach (var unit in mPlayerUnits)
        {
            if (unit == null || !unit.IsAlive) continue;
            var handler = CharacterHandlerRegistry.Get(unit.UnitId);
            handler?.OnBattleStart(unit.StarLevel);
        }
    }

    #endregion

    #region 胜负处理

    private void OnBattleWin(StateEvents.BattleWinEvent evt)
    {
        if (!AccountManager.Instance.IsLoggedIn)
            return;

        string accountId = AccountManager.Instance.CurrentAccount.AccountId;

        DungeonConfig dungeonConfig = null;
        if (!string.IsNullOrEmpty(mCurrentDungeonId))
        {
            dungeonConfig = GameDataMgr.Instance.dungeonConfigs?.Dungeons?.FirstOrDefault(d => d.DungeonId == mCurrentDungeonId);
        }

        if (dungeonConfig == null && GameDataMgr.Instance.dungeonConfigs?.Dungeons?.Count > 0)
        {
            dungeonConfig = GameDataMgr.Instance.dungeonConfigs.Dungeons[0];
        }

        int totalExp = 0;
        foreach (var unit in mEnemyUnits)
        {
            totalExp += unit.Level * 10;
        }

        if (dungeonConfig != null)
        {
            totalExp += dungeonConfig.BaseExp;
        }

        List<BattleUnitData> alivePlayers = mPlayerUnits.Where(u => u.IsAlive).ToList();
        if (alivePlayers.Count > 0)
        {
            int expPerCharacter = totalExp / alivePlayers.Count;
            foreach (var player in alivePlayers)
            {
                UpgradeSystem.Instance.AddExp(accountId, player.UnitId, expPerCharacter);
                LogKit.Log($"[BattleSystem] {player.Name} 获得 {expPerCharacter} 经验");
            }
        }

        // 收集奖励物品
        List<AccountItemData> rewardItems = new List<AccountItemData>();

        if (dungeonConfig != null && dungeonConfig.GoldReward > 0)
        {
            AccountManager.Instance.AddAccountItem(accountId, "Consumable", "Gold", dungeonConfig.GoldReward);
            rewardItems.Add(new AccountItemData("Consumable", "Gold", dungeonConfig.GoldReward));
            LogKit.Log($"[BattleSystem] 获得 {dungeonConfig.GoldReward} 金币");
        }

        if (dungeonConfig != null && dungeonConfig.DropRewards != null)
        {
            foreach (var drop in dungeonConfig.DropRewards)
            {
                if (Random.value <= drop.DropRate)
                {
                    int count = Random.Range(drop.MinCount, drop.MaxCount + 1);
                    AccountManager.Instance.AddAccountItem(accountId, drop.ItemType, drop.ItemId, count);
                    rewardItems.Add(new AccountItemData(drop.ItemType, drop.ItemId, count));
                    LogKit.Log($"[BattleSystem] 获得物品: {drop.ItemType} - {drop.ItemId} x{count}");
                }
            }
        }

        // 将奖励物品传递给事件，供结算面板使用
        evt.TotalExp = totalExp;
        evt.RewardItems = rewardItems;

        LogKit.Log($"[BattleSystem] 战斗胜利！总经验奖励：{totalExp}, 奖励物品数：{rewardItems.Count}");
    }

    /// <summary>
    /// 开始玩家单位行动并触发角色回合开始回调。
    /// </summary>
    public void OnPlayerUnitAction()
    {
        mIsActionComplete = false;
        var actor = TurnOrderManager.Instance.CurrentActor;
        if (actor != null)
        {
            // 调用角色回合开始回调（Assassin刺杀/神鬼无赦状态回合数递减等）
            var playerUnit = mPlayerUnits.FirstOrDefault(u => u.UnitId == actor.UnitId);
            if (playerUnit != null && playerUnit.IsAlive)
            {
                var handler = CharacterHandlerRegistry.Get(playerUnit.UnitId);
                handler?.OnTurnStart(playerUnit.StarLevel);
            }
            LogKit.Log($"[BattleSystem] {actor.UnitId} 的回合开始，请选择技能或普通攻击");
        }
    }

    /// <summary>
    /// 立即执行指定攻击者的普通攻击并完成当前行动。
    /// </summary>
    public void ExecuteNormalAttack(BattleUnitData attacker)
    {
        ExecuteNormalAttackInternal(attacker);
        TurnOrderManager.Instance.CompleteAction();
        mIsActionComplete = true;
    }

    /// <summary>
    /// 根据角色UnitId前缀判断攻击表现类型，用于决定走哪种视觉流程
    /// </summary>
    public static AttackVisualType GetAttackVisualType(string unitId)
    {
        if (string.IsNullOrEmpty(unitId)) return AttackVisualType.None;
        if (unitId.StartsWith("Archer")) return AttackVisualType.Arrow;
        if (unitId.StartsWith("Mage")) return AttackVisualType.MagicOrb;
        if (unitId.StartsWith("Healer")) return AttackVisualType.HealingOrb;
        if (unitId.StartsWith("Warrior")) return AttackVisualType.Melee;
        if (unitId.StartsWith("Spearman")) return AttackVisualType.Melee;
        if (unitId.StartsWith("Assassin")) return AttackVisualType.Melee;
        if (unitId.StartsWith("Knight")) return AttackVisualType.Melee;
        return AttackVisualType.None;
    }

    /// <summary>
    /// 执行 ExecuteNormalAttack 相关的战斗处理。
    /// </summary>
    public void ExecuteNormalAttack(BattleUnitData attacker, BattleUnitData target)
    {
        // 有表现效果的攻击走视觉流程（近战+远程）
        AttackVisualType visualType = GetAttackVisualType(attacker?.UnitId);
        if (visualType != AttackVisualType.None && attacker != null && target != null)
        {
            // 普攻SkillId固定模式：角色名_NormalAttack
            string skillId = attacker.UnitId + "_NormalAttack";
            mPendingAttacker = attacker;
            mPendingNormalAttackTarget = target;
            mIsWaitingForVisual = true;

            List<string> targetIds = new List<string> { target.UnitId };
            int targetIndex = mEnemyUnits.IndexOf(target);
            List<int> targetIndices = new List<int> { targetIndex };

            EventKit.Type.Send(new SkillVisualRequestEvent
            {
                SkillId = skillId,
                AttackerUnitId = attacker.UnitId,
                TargetUnitIds = targetIds,
                TargetUnitIndices = targetIndices
            });

            LogKit.Log($"[BattleSystem] 普攻表现请求发送: {attacker.UnitId}({visualType}), SkillId={skillId}, 目标:{target.UnitId}(索引:{targetIndex})");
            return;
        }

        // 无表现类型：立即执行
        ExecuteNormalAttackInternal(attacker, target);
        TurnOrderManager.Instance.CompleteAction();
        mIsActionComplete = true;
    }

    /// <summary>
    /// 开始敌方单位行动，选择目标并等待攻击表现完成。
    /// </summary>
    public void OnEnemyUnitAction()
    {
        mIsActionComplete = false;
        var actor = TurnOrderManager.Instance.CurrentActor;
        if (actor != null)
        {
            ProcessEnemyAction(actor.UnitId);
            // 不立即 CompleteAction，等待 SkillVisualCompleteEvent 回调
        }
        else
        {
            TurnOrderManager.Instance.CompleteAction();
            mIsActionComplete = true;
        }
    }

    /// <summary>
    /// 执行发牌器行动，根据存活玩家数补充手牌。
    /// </summary>
    public void OnCardDealerAction()
    {
        mIsActionComplete = false;
        int alivePlayerCount = mPlayerUnits.Count(u => u.IsAlive);

        // 通过表现层播放卡牌飞入动画，完成后实际抽牌
        if (BattleActionPlayer.Instance != null)
        {
            BattleActionPlayer.Instance.PlayDealerDrawCardEffect(alivePlayerCount, () =>
            {
                DrawMultipleCards(alivePlayerCount);
                LogKit.Log($"[BattleSystem] 发牌器行动，补充{alivePlayerCount}张手牌");
                TurnOrderManager.Instance.CompleteAction();
                mIsActionComplete = true;
            });
        }
        else
        {
            // 表现层不存在时直接抽牌
            DrawMultipleCards(alivePlayerCount);
            LogKit.Log($"[BattleSystem] 发牌器行动，补充{alivePlayerCount}张手牌");
            TurnOrderManager.Instance.CompleteAction();
            mIsActionComplete = true;
        }
    }

    /// <summary>
    /// 执行 ManualDiscard 相关的战斗处理。
    /// </summary>
    public bool ManualDiscard()
    {
        int alivePlayerCount = mPlayerUnits.Count(u => u.IsAlive);
        int cost = alivePlayerCount * 2 - 1;

        if (!EnergyManager.Instance.SpendEnergy(cost))
        {
            LogKit.Log($"[BattleSystem] 手动弃牌失败，能量不足。需要{cost}点能量");
            return false;
        }

        List<SkillCardData> handCards = CardHand.Instance.Cards;
        int discardCount = handCards.Count;

        CardDeck.Instance.Discard(handCards);
        CardHand.Instance.Clear();

        LogKit.Log($"[BattleSystem] 手动弃牌，弃置{discardCount}张手牌");

        int neededCount = alivePlayerCount;
        
        if (CardDeck.Instance.Count >= neededCount)
        {
            CardHand.Instance.AddCards(CardDeck.Instance.Draw(neededCount));
        }
        else
        {
            if (CardDeck.Instance.DiscardCount > 0)
            {
                UpdateAliveCharactersInDeck();
            }
            CardHand.Instance.AddCards(CardDeck.Instance.Draw(neededCount));
        }

        LogKit.Log($"[BattleSystem] 手动弃牌后，手牌总数：{CardHand.Instance.Count}");
        return true;
    }

    /// <summary>
    /// 执行 GetManualDiscardCost 相关的战斗处理。
    /// </summary>
    public int GetManualDiscardCost()
    {
        int alivePlayerCount = mPlayerUnits.Count(u => u.IsAlive);
        return alivePlayerCount * 2 - 1;
    }

    /// <summary>
    /// 执行 OnPlayerTurnStart 相关的战斗处理。
    /// </summary>
    public void OnPlayerTurnStart()
    {
        EnergyManager.Instance.OnTurnStart();
    }

    /// <summary>
    /// 处理敌方回合开始事件。
    /// </summary>
    public void OnEnemyTurnStart()
    {
    }

    /// <summary>
    /// 处理波次切换并保留玩家当前行动值。
    /// </summary>
    public void OnWaveTransition()
    {
        // 跨波次：保留玩家角色当前行动值，不重置
        // 新敌人由AddUnit自动设置BaseAV
        // 行动序列会根据当前AV自然重排
        LogKit.Log("[BattleSystem] 波次过渡：保留玩家行动值，等待新敌人加入");
    }

    private void DrawMultipleCards(int count)
    {
        List<SkillCardData> drawnCards = CardDeck.Instance.Draw(count);
        CardHand.Instance.AddCards(drawnCards);
        LogKit.Log($"[BattleSystem] 实际补充手牌：{drawnCards.Count}张，手牌总数：{CardHand.Instance.Count}");
    }

    private void ExecuteNormalAttackInternal(BattleUnitData attacker)
    {
        var targets = mEnemyUnits.Where(e => e.IsAlive).ToList();
        if (targets.Count == 0)
            return;

        var target = targets.First();
        float weaponMultiplier = attacker.NormalAttackMultiplier;
        int baseValue = Mathf.RoundToInt(weaponMultiplier * 100);
        int talentVal = attacker.GetTalentValue("NormalAttack");
        int effectiveValue = talentVal > 0 ? talentVal : baseValue;
        float extraCritChance = attacker.ExtraCritDamageChance;
        float extraCritValue = attacker.ExtraCritDamageValue;

        // 角色专属Handler调用：OnSkillExecute前置（Knight星魂3普攻破甲）、OnBeforeDamage、GetExtraCritRate
        var handler = CharacterHandlerRegistry.Get(attacker.UnitId);
        float handlerMultiplier = 1.0f;
        float extraDefensePenetration = 0f;
        float extraCritRate = 0f;
        if (handler != null)
        {
            handler.OnSkillExecute("NormalAttack", attacker.StarLevel, attacker, target);
            handlerMultiplier = handler.OnBeforeDamage("NormalAttack", attacker.StarLevel, out extraDefensePenetration);
            extraCritRate = handler.GetExtraCritRate("NormalAttack", attacker.StarLevel, target);
        }

        float totalMultiplier = effectiveValue * 0.01f * handlerMultiplier;
        DealDamage(attacker, target, totalMultiplier, extraCritChance, extraCritValue, extraDefensePenetration, "NormalAttack", extraCritRate);

        // 伤害后Hook（Assassin星魂2普攻后行动提前20%）
        if (handler != null)
        {
            int finalDamage = Mathf.CeilToInt(attacker.CurrentAttack * totalMultiplier);
            handler.OnAfterDamage("NormalAttack", attacker.StarLevel, attacker, target, finalDamage);
        }

        EnergyManager.Instance.GainEnergy(1);

        if (attacker.ExtraAttackChance > 0 && targets.Count > 1)
        {
            if (Random.value < attacker.ExtraAttackChance)
            {
                var extraTarget = targets[1];
                DealDamage(attacker, extraTarget, effectiveValue * 0.01f * 0.5f, extraCritChance, extraCritValue, 0f, "NormalAttack", extraCritRate);
                LogKit.Log($"[BattleSystem] {attacker.Name} 触发多重射击，额外攻击 {extraTarget.Name}");
            }
        }

        LogKit.Log($"[BattleSystem] {attacker.Name} 普通攻击 {target.Name}，武器倍率:{weaponMultiplier:F2}x，天赋值:{effectiveValue}");
    }

    private void ExecuteNormalAttackInternal(BattleUnitData attacker, BattleUnitData target)
    {
        if (target == null || !target.IsAlive)
            return;

        float weaponMultiplier = attacker.NormalAttackMultiplier;
        int baseValue = Mathf.RoundToInt(weaponMultiplier * 100);
        int talentVal = attacker.GetTalentValue("NormalAttack");
        int effectiveValue = talentVal > 0 ? talentVal : baseValue;
        float extraCritChance = attacker.ExtraCritDamageChance;
        float extraCritValue = attacker.ExtraCritDamageValue;

        // 角色专属Handler调用：OnSkillExecute前置（Knight星魂3普攻破甲）、OnBeforeDamage、GetExtraCritRate
        var handler = CharacterHandlerRegistry.Get(attacker.UnitId);
        float handlerMultiplier = 1.0f;
        float extraDefensePenetration = 0f;
        float extraCritRate = 0f;
        if (handler != null)
        {
            handler.OnSkillExecute("NormalAttack", attacker.StarLevel, attacker, target);
            handlerMultiplier = handler.OnBeforeDamage("NormalAttack", attacker.StarLevel, out extraDefensePenetration);
            extraCritRate = handler.GetExtraCritRate("NormalAttack", attacker.StarLevel, target);
        }

        float totalMultiplier = effectiveValue * 0.01f * handlerMultiplier;
        DealDamage(attacker, target, totalMultiplier, extraCritChance, extraCritValue, extraDefensePenetration, "NormalAttack", extraCritRate);

        // 伤害后Hook（Assassin星魂2普攻后行动提前20%）
        if (handler != null)
        {
            int finalDamage = Mathf.CeilToInt(attacker.CurrentAttack * totalMultiplier);
            handler.OnAfterDamage("NormalAttack", attacker.StarLevel, attacker, target, finalDamage);
        }

        EnergyManager.Instance.GainEnergy(1);

        if (attacker.ExtraAttackChance > 0)
        {
            var otherTargets = mEnemyUnits.Where(e => e.IsAlive && e.UnitId != target.UnitId).ToList();
            if (otherTargets.Count > 0 && Random.value < attacker.ExtraAttackChance)
            {
                var extraTarget = otherTargets.First();
                DealDamage(attacker, extraTarget, effectiveValue * 0.01f * 0.5f, extraCritChance, extraCritValue, 0f, "NormalAttack", extraCritRate);
                LogKit.Log($"[BattleSystem] {attacker.Name} 触发多重射击，额外攻击 {extraTarget.Name}");
            }
        }

        LogKit.Log($"[BattleSystem] {attacker.Name} 普通攻击 {target.Name}，武器倍率:{weaponMultiplier:F2}x，天赋值:{effectiveValue}");
    }

    private void ProcessEnemyAction(string unitId)
    {
        var enemy = mEnemyUnits.FirstOrDefault(u => u.UnitId == unitId);
        if (enemy == null || !enemy.IsAlive)
            return;

        var targets = mPlayerUnits.Where(p => p.IsAlive).ToList();
        if (targets.Count == 0)
            return;

        // 随机选一个存活玩家目标
        int randomIndex = Random.Range(0, targets.Count);
        var target = targets[randomIndex];

        // 从敌人配置的技能列表中随机选一个技能
        string skillId = PickEnemySkillId(enemy);

        // 存储待处理数据，走视觉表现流程（与玩家攻击机制一致）
        mIsEnemyAttacking = true;
        mPendingEnemyAttacker = enemy;
        mPendingEnemyTarget = target;
        mIsWaitingForVisual = true;

        List<string> targetIds = new List<string> { target.UnitId };
        List<int> targetIndices = new List<int> { mPlayerUnits.IndexOf(target) };

        // 发送视觉表现请求（表现层播放动画，命中时回调 OnSkillVisualHit，完成时回调 OnSkillVisualComplete）
        EventKit.Type.Send(new SkillVisualRequestEvent
        {
            SkillId = skillId,
            AttackerUnitId = enemy.UnitId,
            TargetUnitIds = targetIds,
            TargetUnitIndices = targetIndices
        });

        LogKit.Log($"[BattleSystem] 敌人 {enemy.Name} 攻击 {target.Name}，技能:{skillId}，等待表现层播放");
    }

    /// <summary>
    /// 从敌人配置的技能列表中随机选一个技能ID。
    /// 若配置无技能则使用敌人UnitId+"_Attack"作为兜底。
    /// </summary>
    private string PickEnemySkillId(BattleUnitData enemy)
    {
        var enemyConfig = GameDataMgr.Instance.enemyConfigs.Enemies.FirstOrDefault(e => e.EnemyId == enemy.UnitId);
        if (enemyConfig != null && enemyConfig.Skills != null && enemyConfig.Skills.Count > 0)
        {
            int idx = Random.Range(0, enemyConfig.Skills.Count);
            return enemyConfig.Skills[idx];
        }
        return enemy.UnitId + "_Attack";
    }

    private void OnUnitActing(TurnEvents.UnitActingEvent evt)
    {
        switch (evt.UnitType)
        {
            case BattleUnitType.CardDealer:
                ChangeState(BattleState.CardDealerActing);
                break;
            case BattleUnitType.Player:
                ChangeState(BattleState.PlayerActing);
                break;
            case BattleUnitType.Enemy:
                ChangeState(BattleState.EnemyActing);
                break;
        }
    }

    private void OnActionComplete(TurnEvents.ActionCompleteEvent evt)
    {
        UpdateUnitStatusEffects(evt.UnitId);

        if (!BattleStateManager.Instance.IsBattleWin && !BattleStateManager.Instance.IsBattleLose)
        {
            ChangeState(BattleState.WaitingForAction);
        }
    }

    private void UpdateUnitStatusEffects(string actingUnitId = null)
    {
        if (string.IsNullOrEmpty(actingUnitId))
        {
            foreach (var unit in mPlayerUnits)
            {
                if (unit.IsAlive)
                {
                    unit.UpdateStatusEffects();
                    ProcessUnitTurnEffects(unit);
                }
            }

            foreach (var unit in mEnemyUnits)
            {
                if (unit.IsAlive)
                {
                    unit.UpdateStatusEffects();
                    ProcessUnitTurnEffects(unit);
                }
            }

            LogKit.Log("[BattleSystem] 所有单位状态效果回合更新");
        }
        else
        {
            var playerUnit = mPlayerUnits.FirstOrDefault(u => u.UnitId == actingUnitId);
            if (playerUnit != null && playerUnit.IsAlive)
            {
                playerUnit.UpdateStatusEffects();
                ProcessUnitTurnEffects(playerUnit);
                LogKit.Log($"[BattleSystem] {playerUnit.UnitId} 状态效果回合更新");
            }

            var enemyUnit = mEnemyUnits.FirstOrDefault(u => u.UnitId == actingUnitId);
            if (enemyUnit != null && enemyUnit.IsAlive)
            {
                enemyUnit.UpdateStatusEffects();
                ProcessUnitTurnEffects(enemyUnit);
                LogKit.Log($"[BattleSystem] {enemyUnit.UnitId} 状态效果回合更新");
            }
        }
    }

    private void ProcessUnitTurnEffects(BattleUnitData unit)
    {
        if (!unit.IsAlive) return;

        bool wasAlive = unit.IsAlive;
        unit.ProcessDotDamage();
        if (wasAlive && !unit.IsAlive)
        {
            EventKit.Type.Send(new UnitEvents.UnitDefeatedEvent
            {
                Unit = unit
            });
        }

        unit.ProcessHealOverTime();
    }

    private void OnWaveStart(WaveEvents.WaveStartEvent evt)
    {
    }

    private void OnEnemySpawn(WaveEvents.EnemySpawnEvent evt)
    {
        var enemyUnit = BattleUnitFactory.Instance.CreateEnemyUnit(
            evt.Enemy.EnemyId,
            evt.Enemy.Name,
            evt.Enemy.MaxHp,
            evt.Enemy.Speed,
            evt.Enemy.Attack,
            evt.Enemy.Defense,
            evt.Enemy.Level,
            evt.Enemy.Resistance
        );
        mEnemyUnits.Add(enemyUnit);
        TurnOrderManager.Instance.AddUnit(new BattleUnitTurnOrder(enemyUnit));
    }

    private void OnWaveComplete(WaveEvents.WaveCompleteEvent evt)
        {
            if (!WaveManager.Instance.IsLastWave)
            {
                ChangeState(BattleState.WaveTransition);
            }
            else
            {
                CheckBattleEndConditions();
            }
        }

        private void OnAllWavesComplete(WaveEvents.AllWavesCompleteEvent evt)
        {
            CheckBattleEndConditions();
        }

    private void OnUnitDefeated(UnitEvents.UnitDefeatedEvent evt)
    {
        if (evt.Unit.UnitType == BattleUnitType.Enemy)
        {
            WaveManager.Instance.OnEnemyDefeated(evt.Unit.UnitId);
            mEnemyUnits.Remove(evt.Unit);
            TurnOrderManager.Instance.RemoveUnit(evt.Unit.UnitId);
            BattleUnitFactory.Instance.DestroyUnit(evt.Unit);
        }
        else if (evt.Unit.UnitType == BattleUnitType.Player)
        {
            mPlayerUnits.Remove(evt.Unit);
            TurnOrderManager.Instance.RemoveUnit(evt.Unit.UnitId);
            BattleUnitFactory.Instance.DestroyUnit(evt.Unit);

            UpdateAliveCharactersInDeck();
            CardDeck.Instance.RemoveDeadCharacterCards(evt.Unit.UnitId);
            UpdateCardDealerSpeed();
        }

        CheckBattleEndConditions();
    }

    #endregion

    #region 查询接口

    /// <summary>
    /// 执行 GetPlayerHand 相关的战斗处理。
    /// </summary>
    public List<SkillCardData> GetPlayerHand()
    {
        return CardHand.Instance.Cards;
    }

    /// <summary>
    /// 执行 GetCurrentEnergy 相关的战斗处理。
    /// </summary>
    public int GetCurrentEnergy()
    {
        return EnergyManager.Instance.CurrentEnergy;
    }

    /// <summary>
    /// 返回当前能量上限。
    /// </summary>
    public int GetMaxEnergy()
    {
        return EnergyManager.Instance.MaxEnergy;
    }

    /// <summary>
    /// 返回当前玩家单位列表。
    /// </summary>
    public List<BattleUnitData> GetPlayerUnits()
    {
        return mPlayerUnits;
    }

    /// <summary>
    /// 返回当前敌方单位列表。
    /// </summary>
    public List<BattleUnitData> GetEnemyUnits()
    {
        return mEnemyUnits;
    }

    /// <summary>
    /// 返回当前回合顺序中的单位。
    /// </summary>
    public List<ITurnOrder> GetTurnOrder()
    {
        return TurnOrderManager.Instance.GetUnitsInOrder();
    }

    /// <summary>
    /// 执行 GetCurrentActorId 相关的战斗处理。
    /// </summary>
    public string GetCurrentActorId()
    {
        return TurnOrderManager.Instance.CurrentActor?.UnitId ?? "";
    }

    /// <summary>
    /// 返回当前行动单位的类型。
    /// </summary>
    public BattleUnitType GetCurrentActorType()
    {
        return TurnOrderManager.Instance.CurrentActor?.UnitType ?? BattleUnitType.Player;
    }

    /// <summary>
    /// 返回当前行动的战斗单位。
    /// </summary>
    public BattleUnitData GetCurrentActor()
    {
        return TurnOrderManager.Instance.CurrentActor as BattleUnitData;
    }

    /// <summary>
    /// 按 ID 查找玩家单位。
    /// </summary>
    public BattleUnitData GetPlayerUnit(string unitId)
    {
        return mPlayerUnits.FirstOrDefault(u => u.UnitId == unitId);
    }

    /// <summary>
    /// 按 ID 查找玩家、敌人或发牌器单位。
    /// </summary>
    public BattleUnitData GetUnitData(string unitId)
    {
        var playerUnit = mPlayerUnits.FirstOrDefault(u => u.UnitId == unitId);
        if (playerUnit != null) return playerUnit;

        var enemyUnit = mEnemyUnits.FirstOrDefault(u => u.UnitId == unitId);
        if (enemyUnit != null) return enemyUnit;

        if (mCardDealerUnit != null && mCardDealerUnit.UnitId == unitId)
            return mCardDealerUnit;

        return null;
    }

    /// <summary>
    /// 返回回合顺序中的全部单位。
    /// </summary>
    public List<ITurnOrder> GetAllUnitsInTurnOrder()
    {
        return TurnOrderManager.Instance.GetUnitsInOrder();
    }
}

    #endregion
#endregion
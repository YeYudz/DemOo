#region 战斗状态机

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YokiFrame;

/// <summary>
/// 战斗模块中的 BattleFSM，负责对应的战斗流程处理。
/// </summary>
public class BattleFSM:Singleton<BattleFSM>
{
    private BattleSystem mOwner;
    private Dictionary<BattleState, BattleStateBase> mStates = new Dictionary<BattleState, BattleStateBase>();
    private BattleStateBase mCurrentState;
    private BattleStateBase mPreviousState;

    public BattleFSM()
    {
        mOwner = BattleSystem.Instance;
    }

    /// <summary>
    /// 执行 SetupStates 相关的战斗处理。
    /// </summary>
    public void SetupStates()
    {
        mStates[BattleState.BattleStart] = new BattleStartState(this);
        mStates[BattleState.WaitingForAction] = new WaitingForActionState(this);
        mStates[BattleState.PlayerActing] = new PlayerActingState(this);
        mStates[BattleState.EnemyActing] = new EnemyActingState(this);
        mStates[BattleState.CardDealerActing] = new CardDealerActingState(this);
        mStates[BattleState.WaveTransition] = new WaveTransitionState(this);
        mStates[BattleState.BattleWin] = new BattleWinState(this);
        mStates[BattleState.BattleLose] = new BattleLoseState(this);
    }

    /// <summary>
    /// 执行 Start 相关的战斗处理。
    /// </summary>
    public void Start()
    {
        if (mStates.ContainsKey(BattleState.BattleStart))
        {
            mCurrentState = mStates[BattleState.BattleStart];
            mCurrentState.Enter();
        }
    }

    /// <summary>
    /// 执行 Change 相关的战斗处理。
    /// </summary>
    public void Change(BattleState state)
    {
        if (!mStates.ContainsKey(state))
            return;

        if (mCurrentState != null)
        {
            mCurrentState.Exit();
        }

        mPreviousState = mCurrentState;
        mCurrentState = mStates[state];
        mCurrentState.Enter();
    }

    /// <summary>
    /// 执行 Update 相关的战斗处理。
    /// </summary>
    public void Update()
    {
        if (mCurrentState != null)
        {
            mCurrentState.Update();
        }
    }

    /// <summary>
    /// 执行 CustomUpdate 相关的战斗处理。
    /// </summary>
    public void CustomUpdate()
    {
        if (mCurrentState != null)
        {
            mCurrentState.CustomUpdate();
        }
    }

    public BattleSystem Owner
    {
        get { return mOwner; }
    }

    public BattleStateBase PreviousState
    {
        get { return mPreviousState; }
    }
}

/// <summary>
/// 战斗模块中的 BattleStateBase，负责对应的战斗流程处理。
/// </summary>
public abstract class BattleStateBase
{
    protected BattleFSM mFSM;

    public BattleStateBase(BattleFSM fsm)
    {
        mFSM = fsm;
    }

    /// <summary>
    /// 执行 Enter 相关的战斗处理。
    /// </summary>
    public abstract void Enter();
    public abstract void Exit();
    public abstract void Update();
    /// <summary>
    /// 执行 CustomUpdate 相关的战斗处理。
    /// </summary>
    public abstract void CustomUpdate();
}

/// <summary>
/// 战斗模块中的 BattleStartState，负责对应的战斗流程处理。
/// </summary>
public class BattleStartState : BattleStateBase
{
    public BattleStartState(BattleFSM fsm) : base(fsm) { }

    /// <summary>
    /// 执行 Enter 相关的战斗处理。
    /// </summary>
    public override void Enter()
    {
        mFSM.Owner.OnBattleStart();
    }

    /// <summary>
    /// 执行 Exit 相关的战斗处理。
    /// </summary>
    public override void Exit() { }

    public override void Update() { }

    /// <summary>
    /// 执行 CustomUpdate 相关的战斗处理。
    /// </summary>
    public override void CustomUpdate()
    {
        mFSM.Owner.ChangeState(BattleState.WaitingForAction);
    }
}

/// <summary>
/// 战斗模块中的 WaitingForActionState，负责对应的战斗流程处理。
/// </summary>
public class WaitingForActionState : BattleStateBase
{
    public WaitingForActionState(BattleFSM fsm) : base(fsm) { }

    /// <summary>
    /// 执行 Enter 相关的战斗处理。
    /// </summary>
    public override void Enter() { }

    public override void Exit() { }

    /// <summary>
    /// 执行 Update 相关的战斗处理。
    /// </summary>
    public override void Update() { }

    public override void CustomUpdate() { }
}

/// <summary>
/// 战斗模块中的 PlayerActingState，负责对应的战斗流程处理。
/// </summary>
public class PlayerActingState : BattleStateBase
{
    public PlayerActingState(BattleFSM fsm) : base(fsm) { }

    /// <summary>
    /// 执行 Enter 相关的战斗处理。
    /// </summary>
    public override void Enter()
    {
        mFSM.Owner.OnPlayerUnitAction();
    }

    /// <summary>
    /// 执行 Exit 相关的战斗处理。
    /// </summary>
    public override void Exit() { }

    public override void Update() { }

    /// <summary>
    /// 执行 CustomUpdate 相关的战斗处理。
    /// </summary>
    public override void CustomUpdate() { }
}

/// <summary>
/// 战斗模块中的 EnemyActingState，负责对应的战斗流程处理。
/// </summary>
public class EnemyActingState : BattleStateBase
{
    public EnemyActingState(BattleFSM fsm) : base(fsm) { }

    /// <summary>
    /// 执行 Enter 相关的战斗处理。
    /// </summary>
    public override void Enter()
    {
        mFSM.Owner.OnEnemyUnitAction();
    }

    /// <summary>
    /// 执行 Exit 相关的战斗处理。
    /// </summary>
    public override void Exit() { }

    public override void Update() { }

    /// <summary>
    /// 执行 CustomUpdate 相关的战斗处理。
    /// </summary>
    public override void CustomUpdate() { }
}

/// <summary>
/// 战斗模块中的 CardDealerActingState，负责对应的战斗流程处理。
/// </summary>
public class CardDealerActingState : BattleStateBase
{
    public CardDealerActingState(BattleFSM fsm) : base(fsm) { }

    /// <summary>
    /// 执行 Enter 相关的战斗处理。
    /// </summary>
    public override void Enter()
    {
        mFSM.Owner.OnCardDealerAction();
    }

    /// <summary>
    /// 执行 Exit 相关的战斗处理。
    /// </summary>
    public override void Exit() { }

    public override void Update() { }

    /// <summary>
    /// 执行 CustomUpdate 相关的战斗处理。
    /// </summary>
    public override void CustomUpdate() { }
}

/// <summary>
/// 战斗模块中的 WaveTransitionState，负责对应的战斗流程处理。
/// </summary>
public class WaveTransitionState : BattleStateBase
{
    public WaveTransitionState(BattleFSM fsm) : base(fsm) { }

    /// <summary>
    /// 执行 Enter 相关的战斗处理。
    /// </summary>
    public override void Enter()
    {
        mFSM.Owner.OnWaveTransition();
    }

    /// <summary>
    /// 执行 Exit 相关的战斗处理。
    /// </summary>
    public override void Exit() { }

    public override void Update() { }

    /// <summary>
    /// 执行 CustomUpdate 相关的战斗处理。
    /// </summary>
    public override void CustomUpdate()
    {
        mFSM.Owner.StartNextWave();
        mFSM.Owner.ChangeState(BattleState.WaitingForAction);
    }
}

/// <summary>
/// 战斗模块中的 BattleWinState，负责对应的战斗流程处理。
/// </summary>
public class BattleWinState : BattleStateBase
{
    public BattleWinState(BattleFSM fsm) : base(fsm) { }

    /// <summary>
    /// 执行 Enter 相关的战斗处理。
    /// </summary>
    public override void Enter()
    {
        YokiFrame.EventKit.Type.Send(new StateEvents.BattleWinEvent());
    }

    /// <summary>
    /// 执行 Exit 相关的战斗处理。
    /// </summary>
    public override void Exit() { }

    public override void Update() { }

    /// <summary>
    /// 执行 CustomUpdate 相关的战斗处理。
    /// </summary>
    public override void CustomUpdate() { }
}

/// <summary>
/// 战斗模块中的 BattleLoseState，负责对应的战斗流程处理。
/// </summary>
public class BattleLoseState : BattleStateBase
{
    public BattleLoseState(BattleFSM fsm) : base(fsm) { }

    /// <summary>
    /// 执行 Enter 相关的战斗处理。
    /// </summary>
    public override void Enter()
    {
        YokiFrame.EventKit.Type.Send(new StateEvents.BattleLoseEvent());
    }

    /// <summary>
    /// 执行 Exit 相关的战斗处理。
    /// </summary>
    public override void Exit() { }

    public override void Update() { }

    /// <summary>
    /// 执行 CustomUpdate 相关的战斗处理。
    /// </summary>
    public override void CustomUpdate() { }
}

#endregion

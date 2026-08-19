using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YokiFrame;

/// <summary>
/// 副本管理器：跟踪当前副本并在胜利后解锁后续进度。
/// </summary>
public class DungeonManager : Singleton<DungeonManager>
{
    #region 副本进度

    private string mCurrentDungeonId = "";
    private AccountData mCurrentAccount = null;

    public void Initialize(AccountData account)
    {
        mCurrentAccount = account;
        EventKit.Type.Register<StateEvents.BattleWinEvent>(OnBattleWin);
        EventKit.Type.Register<StateEvents.BattleLoseEvent>(OnBattleLose);
    }

    public void SetCurrentDungeon(string dungeonId)
    {
        mCurrentDungeonId = dungeonId;
    }

    private void OnBattleWin(StateEvents.BattleWinEvent evt)
    {
        if (string.IsNullOrEmpty(mCurrentDungeonId))
            return;

        if (mCurrentAccount == null)
            return;

        UnlockNextDungeon();
    }

    private void OnBattleLose(StateEvents.BattleLoseEvent evt)
    {
        return;
    }

    private void UnlockNextDungeon()
    {
        int currentDungeonIndex = GetDungeonIndex(mCurrentDungeonId);
        
        if (currentDungeonIndex < 0)
            return;

        if (currentDungeonIndex >= mCurrentAccount.DungeonProgress)
        {
            mCurrentAccount.DungeonProgress = currentDungeonIndex + 1;
        }
    }

    private int GetDungeonIndex(string dungeonId)
    {
        if (dungeonId.StartsWith("Dungeon_"))
        {
            string numberPart = dungeonId.Replace("Dungeon_", "");
            if (int.TryParse(numberPart, out int index))
            {
                return index - 1;
            }
        }
        return -1;
    }

    /// <summary>判断指定副本是否已解锁。</summary>
    public bool IsDungeonUnlocked(string dungeonId)
    {
        if (mCurrentAccount == null)
            return false;

        int dungeonIndex = GetDungeonIndex(dungeonId);
        if (dungeonIndex < 0)
            return false;

        return dungeonIndex <= mCurrentAccount.DungeonProgress;
    }

    /// <summary>获取当前账号的副本进度。</summary>
    public int GetCurrentProgress()
    {
        return mCurrentAccount?.DungeonProgress ?? 0;
    }

    #endregion
}

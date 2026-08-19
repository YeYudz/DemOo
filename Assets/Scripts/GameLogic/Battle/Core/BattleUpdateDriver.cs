#region 战斗更新驱动

using UnityEngine;
using YokiFrame;
using static GameUI.UIEvents;

/// <summary>
/// 战斗更新驱动器：将战斗循环从UI面板中剥离，由独立的MonoBehaviour驱动BattleSystem.Update()。
/// 在BattlePanelOpenedEvent时创建并启动，在BattlePanelClosedEvent时停止并销毁。
/// </summary>
public class BattleUpdateDriver : MonoBehaviour
{
    private bool mIsDriving = false;

    private static BattleUpdateDriver sInstance;

    /// <summary>
    /// 执行 Create 相关的战斗处理。
    /// </summary>
    public static BattleUpdateDriver Create()
    {
        if (sInstance != null)
            return sInstance;

        GameObject driverObj = new GameObject("__BattleUpdateDriver__");
        sInstance = driverObj.AddComponent<BattleUpdateDriver>();
        DontDestroyOnLoad(driverObj);
        return sInstance;
    }

    private void Awake()
    {
        EventKit.Type.Register<BattlePanelClosedEvent>(OnBattlePanelClosed);
    }

    private void OnDestroy()
    {
        EventKit.Type.UnRegister<BattlePanelClosedEvent>(OnBattlePanelClosed);
    }

    /// <summary>
    /// 执行 StartDriving 相关的战斗处理。
    /// </summary>
    public void StartDriving()
    {
        mIsDriving = true;
    }

    private void OnBattlePanelClosed(BattlePanelClosedEvent evt)
    {
        mIsDriving = false;
        DestroySelf();
    }

    private void Update()
    {
        if (mIsDriving && BattleSystem.Instance.IsBattleActive)
        {
            BattleSystem.Instance.Update(Time.deltaTime);
        }
    }

    private void DestroySelf()
    {
        sInstance = null;
        Destroy(gameObject);
    }
}

#endregion

using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using YokiFrame.Unity;
using System.Collections.Generic;
using Game;

namespace GameUI
{
	public sealed class MainMenuPanelData : IUIData
	{
	}

	/// <summary>
	/// 主菜单面板：显示账号资源并提供主要功能入口。
	/// </summary>
	public partial class MainMenuPanel : UIPanel
	{
		#region 生命周期与菜单操作
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as MainMenuPanelData ?? new MainMenuPanelData();
			// 确保主菜单始终位于最底层（AlwayBottom），不会被后打开的面板遮挡
			if (UIRoot.Instance != null)
			{
				UIRoot.Instance.SetPanelLevel(this, UILevel.AlwayBottom);
			}
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as MainMenuPanelData ?? mData;
			UpdateAccountInfo();
		}

		protected override void OnShow()
		{
			LogKit.Log("[MainMenuPanel] 主菜单已打开");
			UpdateAccountInfo();

			// 播放主菜单背景音乐
			AudioManager.Instance.PlayBGM(AudioManager.BGM_MainMenu);

			if (mStartGameButton != null)
			{
				mStartGameButton.onClick.RemoveAllListeners();
				mStartGameButton.onClick.AddListener(OnStartGameButtonClicked);
			}

			if (mSettingsButton != null)
			{
				mSettingsButton.onClick.RemoveAllListeners();
				mSettingsButton.onClick.AddListener(OnSettingsButtonClicked);
			}

			if (mQuitButton != null)
			{
				mQuitButton.onClick.RemoveAllListeners();
				mQuitButton.onClick.AddListener(OnQuitButtonClicked);
			}

			if (mLogoutButton != null)
			{
				mLogoutButton.onClick.RemoveAllListeners();
				mLogoutButton.onClick.AddListener(OnLogoutButtonClicked);
			}

			if (mCharacterPanelButton != null)
			{
				mCharacterPanelButton.onClick.RemoveAllListeners();
				mCharacterPanelButton.onClick.AddListener(OnCharacterPanelButtonClicked);
			}

			if (mInventoryPanelButton != null)
			{
				mInventoryPanelButton.onClick.RemoveAllListeners();
				mInventoryPanelButton.onClick.AddListener(OnInventoryPanelButtonClicked);
			}

			if (mLotteryButton != null)
			{
				mLotteryButton.onClick.RemoveAllListeners();
				mLotteryButton.onClick.AddListener(OnLotteryButtonClicked);
			}

			if (mDecomposeButton != null)
			{
				mDecomposeButton.onClick.RemoveAllListeners();
				mDecomposeButton.onClick.AddListener(OnDecomposeButtonClicked);
			}

			if (mStoreButton != null)
			{
				mStoreButton.onClick.RemoveAllListeners();
				mStoreButton.onClick.AddListener(OnStoreButtonClicked);
			}
		}

		protected override void OnHide()
		{
			if (mStartGameButton != null)
				mStartGameButton.onClick.RemoveAllListeners();
			if (mSettingsButton != null)
				mSettingsButton.onClick.RemoveAllListeners();
			if (mQuitButton != null)
				mQuitButton.onClick.RemoveAllListeners();
			if (mLogoutButton != null)
				mLogoutButton.onClick.RemoveAllListeners();
			if (mCharacterPanelButton != null)
				mCharacterPanelButton.onClick.RemoveAllListeners();
			if (mInventoryPanelButton != null)
				mInventoryPanelButton.onClick.RemoveAllListeners();
			if (mLotteryButton != null)
				mLotteryButton.onClick.RemoveAllListeners();
			if (mDecomposeButton != null)
				mDecomposeButton.onClick.RemoveAllListeners();
			if (mStoreButton != null)
				mStoreButton.onClick.RemoveAllListeners();
		}

		protected override void OnClose()
		{
		}

		private void UpdateAccountInfo()
		{
			if (mAccountInfoText != null && AccountManager.Instance.IsLoggedIn)
			{
				string accountId = AccountManager.Instance.CurrentAccount.AccountId;
				int gold = AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", "Gold");
			int diamond = AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", "Diamond");

				mAccountInfoText.text = $"账号: {accountId}\n金币: {gold}\n钻石: {diamond}";
			}
		}

		private void OnStartGameButtonClicked()
		{
			LogKit.Log("[MainMenuPanel] 进入副本选择");
			UIKit.ClosePanel<MainMenuPanel>();
			UIKit.OpenPanel<DungeonSelectPanel>();
		}

		private void OnSettingsButtonClicked()
		{
			LogKit.Log("[MainMenuPanel] 打开设置");
			UIKit.OpenPanel<SettingPanel>();
		}

		private void OnQuitButtonClicked()
		{
			LogKit.Log("[MainMenuPanel] 退出游戏");
			AccountManager.Instance.SaveDatabase();
			Application.Quit();
		}

		private void OnLogoutButtonClicked()
		{
			LogKit.Log("[MainMenuPanel] 退出登录");
			AccountManager.Instance.Logout();
			UIKit.ClosePanel<MainMenuPanel>();
			UIKit.OpenPanel<LoginPanel>();
		}

		private void OnCharacterPanelButtonClicked()
		{
			LogKit.Log("[MainMenuPanel] 打开角色面板");
			UIKit.OpenPanel<CharacterPanel>(data: new CharacterPanelData
			{
				OpenerPanelType = "MainMenuPanel"
			});
		}

		private void OnInventoryPanelButtonClicked()
		{
			LogKit.Log("[MainMenuPanel] 打开背包面板");
			UIKit.ClosePanel<MainMenuPanel>();
			UIKit.OpenPanel<InventoryPanel>();
		}

		private void OnLotteryButtonClicked()
		{
			LogKit.Log("[MainMenuPanel] 打开抽卡面板");
			UIKit.OpenPanel<LotteryPanel>();
		}

		private void OnDecomposeButtonClicked()
		{
			LogKit.Log("[MainMenuPanel] 打开分解面板");
			UIKit.OpenPanel<DecomposePanel>();
		}

		private void OnStoreButtonClicked()
		{
			LogKit.Log("[MainMenuPanel] 打开商店面板");
			UIKit.OpenPanel<StorePanel>();
		}
		#endregion
	}
}

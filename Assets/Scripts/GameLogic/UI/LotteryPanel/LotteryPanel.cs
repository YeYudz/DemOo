using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using System.Collections.Generic;
using System.Linq;

namespace GameUI
{
	public enum LotteryPoolType
	{
		LimitedCharacter,
		LimitedWeapon,
		Permanent
	}

	public sealed class LotteryPanelData : IUIData
	{
		#region 生命周期与数据的读取
		public LotteryPoolType DefaultPoolType { get; set; } = LotteryPoolType.LimitedCharacter;
	}

	public partial class LotteryPanel : UIPanel
	{
		private LotteryPoolType mCurrentPoolType;
		private LotteryPoolConfig mCurrentPoolConfig;
		private LotteryPityData mCurrentPityData;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as LotteryPanelData ?? new LotteryPanelData();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as LotteryPanelData ?? mData;
			InitializeToggles();
			RegisterButtonEvents();
			LoadPoolConfig();
			UpdatePoolDisplay();
			UpdatePityInfo();
		}

		protected override void OnShow()
		{
			SetPanelSortingOrder();
		}

		protected override void OnHide()
		{
		}

		protected override void OnClose()
		{
		}

		private void SetPanelSortingOrder()
		{
			Canvas canvas = GetComponent<Canvas>();
			if (canvas != null && UIManager.Instance != null)
			{
				canvas.sortingOrder = UIManager.Instance.AllocateSortingOrder();
			}
		}

		private void InitializeToggles()
		{
			mCurrentPoolType = mData.DefaultPoolType;

			if (mTogCharacter != null)
			{
				mTogCharacter.onValueChanged.RemoveAllListeners();
				mTogCharacter.onValueChanged.AddListener((isOn) =>
				{
					if (isOn) OnPoolSelected(LotteryPoolType.LimitedCharacter);
				});
				mTogCharacter.isOn = (mCurrentPoolType == LotteryPoolType.LimitedCharacter);
			}

			if (mTogWeapon != null)
			{
				mTogWeapon.onValueChanged.RemoveAllListeners();
				mTogWeapon.onValueChanged.AddListener((isOn) =>
				{
					if (isOn) OnPoolSelected(LotteryPoolType.LimitedWeapon);
				});
				mTogWeapon.isOn = (mCurrentPoolType == LotteryPoolType.LimitedWeapon);
			}

			if (mTogPermanent != null)
			{
				mTogPermanent.onValueChanged.RemoveAllListeners();
				mTogPermanent.onValueChanged.AddListener((isOn) =>
				{
					if (isOn) OnPoolSelected(LotteryPoolType.Permanent);
				});
				mTogPermanent.isOn = (mCurrentPoolType == LotteryPoolType.Permanent);
			}
		}

		private void OnPoolSelected(LotteryPoolType poolType)
		{
			if (mCurrentPoolType == poolType && mCurrentPoolConfig != null) return;

			mCurrentPoolType = poolType;
			LoadPoolConfig();
			UpdatePoolDisplay();
			UpdatePityInfo();
		}

		private void LoadPoolConfig()
		{
			string poolId = GetPoolId(mCurrentPoolType);
			mCurrentPoolConfig = LotterySystem.Instance.GetPoolConfig(poolId);
			mCurrentPityData = LotterySystem.Instance.GetPityData(poolId);
		}

		private string GetPoolId(LotteryPoolType type)
		{
			switch (type)
			{
				case LotteryPoolType.LimitedCharacter: return "limited_character";
				case LotteryPoolType.LimitedWeapon: return "limited_weapon";
				case LotteryPoolType.Permanent: return "permanent";
				default: return "permanent";
			}
		}

		private void UpdatePoolDisplay()
		{
			if (mImgItem == null) return;

			Sprite sprite = null;

			switch (mCurrentPoolType)
			{
				case LotteryPoolType.LimitedCharacter:
					// 限定角色池：显示UP角色的立绘图
					if (mCurrentPoolConfig != null && mCurrentPoolConfig.UpFiveStarIds.Count > 0)
					{
						sprite = SpriteIconLoader.LoadCharacterIcon(mCurrentPoolConfig.UpFiveStarIds[0]);
					}
					break;
				case LotteryPoolType.LimitedWeapon:
					// 限定武器池：显示UP武器的图片
					if (mCurrentPoolConfig != null && mCurrentPoolConfig.UpFiveStarIds.Count > 0)
					{
						sprite = SpriteIconLoader.LoadWeaponIcon(mCurrentPoolConfig.UpFiveStarIds[0]);
					}
					break;
				case LotteryPoolType.Permanent:
					// 常驻池：显示通用卡池图片（YooAsset 地址: "permanent"，AddressByFileName）
					sprite = ResKit.Load<Sprite>("permanent");
					if (sprite == null) sprite = Resources.Load<Sprite>("Sprites/Lottery/permanent");
					break;
			}

			if (sprite != null)
			{
				mImgItem.sprite = sprite;
				mImgItem.color = Color.white;
			}
			else
			{
				// 资源未加载时，回退为纯色占位
				switch (mCurrentPoolType)
				{
					case LotteryPoolType.LimitedCharacter:
						mImgItem.color = new Color(0.8f, 0.6f, 0.2f);
						break;
					case LotteryPoolType.LimitedWeapon:
						mImgItem.color = new Color(0.8f, 0.4f, 0.4f);
						break;
					case LotteryPoolType.Permanent:
						mImgItem.color = new Color(0.5f, 0.5f, 0.8f);
						break;
				}
			}
		}

		private void UpdatePityInfo()
	{
		int diamonCount = LotterySystem.Instance.GetDiamondCount();

		// 显示钻石数量
		if (mTextDiamondCount != null)
		{
			mTextDiamondCount.text = diamonCount.ToString();
		}

		// 加载钻石图标
		if (mImgDiamond != null)
		{
			SpriteIconLoader.SetConsumableIcon(mImgDiamond, "Diamond");
			if (mImgDiamond.sprite != null)
			{
				mImgDiamond.color = Color.white;
			}
		}

		if (mCurrentPityData == null || mCurrentPoolConfig == null) return;

		int fiveStarPity = mCurrentPoolConfig.GuaranteedFiveStarEvery - mCurrentPityData.DrawCountSinceFiveStar;
		int fourStarPity = mCurrentPoolConfig.GuaranteedFourStarEvery - mCurrentPityData.DrawCountSinceFourStar;

		LogKit.Log($"[LotteryPanel] 卡池: {mCurrentPoolConfig.PoolName}");
		LogKit.Log($"  钻石: {diamonCount}");
		LogKit.Log($"  距离5星保底: {fiveStarPity}抽");
		LogKit.Log($"  距离4星保底: {fourStarPity}抽");
		if (mCurrentPoolConfig.HasUpSystem && mCurrentPityData.NextFiveStarMustBeUp > 0)
		{
			LogKit.Log($"  下次5星必为UP角色");
		}
	}

		private void RegisterButtonEvents()
		{
			if (mBtnClose != null)
			{
				mBtnClose.onClick.RemoveAllListeners();
				mBtnClose.onClick.AddListener(OnBtnCloseClicked);
			}

			if (mBtnRule != null)
			{
				mBtnRule.onClick.RemoveAllListeners();
				mBtnRule.onClick.AddListener(OnBtnRuleClicked);
			}

			if (mBtnHistory != null)
			{
				mBtnHistory.onClick.RemoveAllListeners();
				mBtnHistory.onClick.AddListener(OnBtnHistoryClicked);
			}

			if (mBtnSingle != null)
			{
				mBtnSingle.onClick.RemoveAllListeners();
				mBtnSingle.onClick.AddListener(OnBtnSingleClicked);
			}

			if (mBtnTen != null)
			{
				mBtnTen.onClick.RemoveAllListeners();
				mBtnTen.onClick.AddListener(OnBtnTenClicked);
			}
		}

		private void OnBtnCloseClicked()
		{
			UIKit.ClosePanel<LotteryPanel>();
		}

		private void OnBtnRuleClicked()
		{
			string poolId = GetPoolId(mCurrentPoolType);
			UIKit.OpenPanel<LotteryRulePanel>(UILevel.Pop, data: new LotteryRulePanelData { PoolId = poolId });
		}

		private void OnBtnHistoryClicked()
		{
			string poolId = GetPoolId(mCurrentPoolType);
			UIKit.OpenPanel<LotteryHistoryPanel>(UILevel.Pop, data: new LotteryHistoryPanelData { PoolId = poolId });
		}

		private void OnBtnSingleClicked()
		{
			ExecuteLottery(1);
		}

		private void OnBtnTenClicked()
		{
			ExecuteLottery(10);
		}

		/// <summary>
		/// 校验资源并执行指定次数的抽卡。
		/// </summary>
		private void ExecuteLottery(int count)
		{
			if (mCurrentPoolConfig == null)
			{
				LogKit.Log("[LotteryPanel] 卡池配置未加载");
				return;
			}

			if (!AccountManager.Instance.IsLoggedIn)
			{
				LogKit.Log("[LotteryPanel] 请先登录");
				return;
			}

			int totalCost = count == 1 ? mCurrentPoolConfig.DiamondCost : mCurrentPoolConfig.TenDrawCost;
			int currentDiamond = LotterySystem.Instance.GetDiamondCount();

			if (currentDiamond < totalCost)
			{
				LogKit.Log($"[LotteryPanel] 钻石不足，需要{totalCost}钻石，当前{currentDiamond}钻石");
				return;
			}

			string poolId = mCurrentPoolConfig.PoolId;
			var result = LotterySystem.Instance.PerformLottery(poolId, count);

			if (result != null && result.Results.Count > 0)
			{
				LogKit.Log($"[LotteryPanel] {count}连抽完成，消耗{totalCost}钻石");
				foreach (var record in result.Results)
				{
					string upTag = record.IsUp ? "[UP] " : "";
					LogKit.Log($"  {upTag}{record.ItemName} ({record.Rarity}星)");
				}

				LoadPoolConfig();
				UpdatePityInfo();

				var resultData = new LotteryResultPanelData
				{
					DrawResult = result
				};
				UIKit.OpenPanel<LotteryResultPanel>(UILevel.Pop, data: resultData);
			}
			else
			{
				LogKit.Log("[LotteryPanel] 抽奖失败");
			}
		}
		#endregion
	}
}

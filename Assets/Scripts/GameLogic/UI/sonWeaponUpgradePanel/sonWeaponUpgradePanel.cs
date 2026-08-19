using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using System.Collections.Generic;
using System.Linq;

namespace GameUI
{
	public sealed class sonWeaponUpgradePanelData : IUIData
	{
		public string WeaponId { get; set; }
		public string WeaponInstanceId { get; set; }
		public string TargetCharacterId { get; set; }
		public string OpenerPanelType { get; set; }
		public object OpenerData { get; set; }
	}

	/// <summary>
	/// 武器升级面板：展示升级消耗并执行升级。
	/// </summary>
	public partial class sonWeaponUpgradePanel : UIPanel
	{
		#region 生命周期与升级操作
		private LightConeConfig mWeaponConfig;
		private AccountItemData mWeaponItem;
		private Font mDefaultFont;
		private List<CostItem> mUpgradeCosts = new List<CostItem>();
		private int mGoldCost = 0;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as sonWeaponUpgradePanelData ?? new sonWeaponUpgradePanelData();
			LoadDefaultFont();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as sonWeaponUpgradePanelData ?? mData;
			LoadWeaponData();
			GenerateUpgradeCosts();
			RefreshWeaponInfo();
			RefreshGoldCost();
			RefreshMaterialButtons();
			UpdateUpgradeButtonState();
			RegisterButtonEvents();
			RegisterEventListeners();
		}

		protected override void OnShow()
		{
			SetPanelSortingOrder();
		}

		private void SetPanelSortingOrder()
		{
			Canvas canvas = GetComponent<Canvas>();
			if (canvas != null && UIManager.Instance != null)
			{
				canvas.sortingOrder = UIManager.Instance.AllocateSortingOrder();
			}
		}

		protected override void OnHide()
		{
		}

		protected override void OnClose()
		{
			UnregisterEventListeners();
			mWeaponConfig = null;
			mWeaponItem = null;
			mUpgradeCosts.Clear();
		}

		private void LoadDefaultFont()
		{
			try
			{
				mDefaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			}
			catch
			{
				mDefaultFont = null;
			}

			if (mDefaultFont == null)
			{
				Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
				if (fonts.Length > 0)
					mDefaultFont = fonts[0];
			}
		}

		private void LoadWeaponData()
		{
			mWeaponItem = null;
			mWeaponConfig = null;

			if (AccountManager.Instance.IsLoggedIn)
			{
				string accountId = AccountManager.Instance.CurrentAccount.AccountId;
				mWeaponItem = AccountManager.Instance.GetAccountItemByInstanceId(
					accountId, "Weapon", mData.WeaponInstanceId);
			}

			mWeaponConfig = GameDataMgr.Instance.lightConeConfigs.Weapons
				.FirstOrDefault(w => mWeaponItem != null && w.WeaponId == mWeaponItem.ItemId);
		}

		private void GenerateUpgradeCosts()
		{
			mUpgradeCosts.Clear();

			if (mWeaponConfig == null || mWeaponItem == null) return;

			var costs = UpgradeSystem.Instance.GetWeaponUpgradeCost(mData.WeaponInstanceId);
			if (costs != null)
			{
				mUpgradeCosts.AddRange(costs);
			}

			mGoldCost = UpgradeSystem.Instance.GetWeaponUpgradeGoldCost(mData.WeaponInstanceId);
		}

		private int GetCurrentGold()
		{
			if (!AccountManager.Instance.IsLoggedIn) return 0;
			string accountId = AccountManager.Instance.CurrentAccount.AccountId;
			return AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", "Gold");
		}

		private void RefreshGoldCost()
		{
			if (mTexGoldCost != null)
			{
				int currentGold = GetCurrentGold();
				bool hasEnough = currentGold >= mGoldCost;
				mTexGoldCost.text = $"所需金币\n{currentGold}/{mGoldCost}";
				mTexGoldCost.color = hasEnough ? new Color(0.2384f, 0.8490f, 0.2042f) : new Color(0.8509f, 0.3627f, 0.2039f);
			}
		}

		private void RefreshWeaponInfo()
		{
			if (mImgWeapon != null)
			{
				if (mWeaponConfig != null && mWeaponItem != null)
				{
					SpriteIconLoader.SetWeaponIcon(mImgWeapon, mWeaponItem.ItemId);
					if (mImgWeapon.sprite == null)
					{
						mImgWeapon.color = GetWeaponRarityColor(mWeaponConfig.Rarity);
					}
				}
				else
				{
					mImgWeapon.color = Color.clear;
				}
			}

			if (mTexDetail != null)
			{
				if (mWeaponConfig != null && mWeaponItem != null)
				{
					int weaponLevel = mWeaponItem.Level;
					int maxLevel = mWeaponConfig.MaxLevel > 1 ? mWeaponConfig.MaxLevel : 80;
					bool isMaxLevel = weaponLevel >= maxLevel;

					UpgradeSystem.Instance.CalculateWeaponBaseStats(mWeaponConfig, weaponLevel, out float currentMaxHp, out float currentAttack, out float currentDefense);
					int currentAttackValue = Mathf.RoundToInt(currentAttack);
					int currentDefenseValue = Mathf.RoundToInt(currentDefense);
					int currentMaxHpValue = Mathf.RoundToInt(currentMaxHp);

					mTexDetail.text = $"武器名称: {mWeaponConfig.Name}\n" +
									 $"稀有度: {mWeaponConfig.Rarity}星\n" +
									 $"等级: Lv.{weaponLevel} / Lv.{maxLevel}\n" +
									 $"攻击力: {currentAttackValue}\n" +
									 $"防御力: {currentDefenseValue}\n" +
									 $"生命值: {currentMaxHpValue}\n\n";

					if (!isMaxLevel)
					{
						UpgradeSystem.Instance.CalculateWeaponBaseStats(mWeaponConfig, weaponLevel + 1, out float nextMaxHp, out float nextAttack, out float nextDefense);

						mTexDetail.text += $"升级后属性:\n" +
											 $"  攻击力 {currentAttackValue} → {Mathf.RoundToInt(nextAttack)}\n" +
											 $"  防御力 {currentDefenseValue} → {Mathf.RoundToInt(nextDefense)}\n" +
											 $"  生命值 {currentMaxHpValue} → {Mathf.RoundToInt(nextMaxHp)}";
					}
					else
					{
						mTexDetail.text += "已达到最高等级";
					}
				}
				else
				{
					mTexDetail.text = "武器数据未找到";
				}
			}
		}

		private Color GetWeaponRarityColor(int rarity)
		{
			switch (rarity)
			{
				case 3: return new Color(0.8f, 0.8f, 0.8f);
				case 4: return new Color(0.5f, 0.5f, 1f);
				case 5: return new Color(1f, 0.8f, 0.2f);
				default: return Color.gray;
			}
		}

		private void RefreshMaterialButtons()
		{
			Button[] materialButtons = { mBtnMaterial1, mBtnMaterial2, mBtnMaterial3, mBtnMaterial4 };
			Color[] materialColors = {
				new Color(0.8f, 0.6f, 0.4f),
				new Color(0.4f, 0.6f, 0.8f),
				new Color(0.6f, 0.4f, 0.8f),
				new Color(0.8f, 0.8f, 0.4f)
			};

			for (int i = 0; i < materialButtons.Length; i++)
			{
				var btn = materialButtons[i];
				if (btn == null) continue;

				if (i < mUpgradeCosts.Count)
				{
					var cost = mUpgradeCosts[i];
					btn.gameObject.SetActive(true);

					Image btnImage = btn.GetComponent<Image>();
					if (btnImage != null)
					{
						SpriteIconLoader.SetConsumableIcon(btnImage, cost.ItemId);
						if (btnImage.sprite != null)
							btnImage.color = Color.white;
						else
							btnImage.color = materialColors[i % materialColors.Length];
					}

					string accountId = AccountManager.Instance.IsLoggedIn ? AccountManager.Instance.CurrentAccount.AccountId : "";
					int ownedCount = 0;
					if (AccountManager.Instance.IsLoggedIn)
					{
						var item = AccountManager.Instance.GetAccountItem(accountId, "Consumable", cost.ItemId);
						if (item != null) ownedCount = item.Count;
					}

					string itemName = GetItemDisplayName(cost.ItemId);
					Text btnText = btn.GetComponentInChildren<Text>(true);
					if (btnText != null)
					{
						// 文字移至右下角，青色高亮
						btnText.text = $"{itemName}\n{ownedCount}/{cost.Count}";
						btnText.color = new Color(0.0368f, 0.8679f, 0.8679f);
						btnText.alignment = TextAnchor.LowerRight;
						btnText.fontStyle = FontStyle.Bold;
					}
				}
				else
				{
					btn.gameObject.SetActive(false);
				}
			}
		}

		private string GetItemDisplayName(string itemId)
		{
			var consumableConfig = GameDataMgr.Instance.consumableConfigs?.Consumables
				.FirstOrDefault(c => c.ItemId == itemId);
			if (consumableConfig != null)
				return consumableConfig.Name;

			return itemId;
		}

		private void UpdateUpgradeButtonState()
		{
			if (mBtnUpgrade == null) return;

			bool isMaxLevel = mWeaponItem != null && mWeaponItem.Level >= (mWeaponConfig != null && mWeaponConfig.MaxLevel > 1 ? mWeaponConfig.MaxLevel : 80);
			bool hasEnoughGold = GetCurrentGold() >= mGoldCost;
			bool allMaterialsEnough = CheckAllMaterialsEnough();

			mBtnUpgrade.interactable = !isMaxLevel && hasEnoughGold && allMaterialsEnough;

			Image upgradeImage = mBtnUpgrade.GetComponent<Image>();
			if (upgradeImage != null)
			{
				if (isMaxLevel)
				{
					upgradeImage.color = new Color(0.5f, 0.5f, 0.5f);
				}
				else if (!allMaterialsEnough)
				{
					upgradeImage.color = new Color(0.3f, 0.3f, 0.3f);
				}
				else
				{
					upgradeImage.color = hasEnoughGold
						? new Color(0.2f, 0.6f, 0.2f)
						: new Color(0.3f, 0.3f, 0.3f);
				}
			}

			Text upgradeText = mBtnUpgrade.GetComponentInChildren<Text>();
			if (upgradeText != null)
			{
				if (isMaxLevel)
				{
					upgradeText.text = "已满级";
					upgradeText.color = Color.gray;
				}
				else if (!allMaterialsEnough)
				{
					upgradeText.text = "材料不足";
					upgradeText.color = Color.gray;
				}
				else
				{
					upgradeText.text = hasEnoughGold ? "升级" : "金币不足";
					upgradeText.color = hasEnoughGold ? Color.white : Color.gray;
				}
			}
		}

		private bool CheckAllMaterialsEnough()
		{
			if (!AccountManager.Instance.IsLoggedIn) return false;

			string accountId = AccountManager.Instance.CurrentAccount.AccountId;
			foreach (var cost in mUpgradeCosts)
			{
				var item = AccountManager.Instance.GetAccountItem(accountId, "Consumable", cost.ItemId);
				int ownedCount = item != null ? item.Count : 0;
				if (ownedCount < cost.Count)
					return false;
			}
			return true;
		}

		private void RegisterButtonEvents()
		{
			if (mBtnUpgrade != null)
			{
				mBtnUpgrade.onClick.RemoveAllListeners();
				mBtnUpgrade.onClick.AddListener(OnBtnUpgradeClicked);
			}

			if (mBtnBack != null)
			{
				mBtnBack.onClick.RemoveAllListeners();
				mBtnBack.onClick.AddListener(OnBtnBackClicked);
			}
		}

		private void RegisterEventListeners()
		{
			EventKit.Type.Register<UIEvents.WeaponUpgradeResultEvent>(OnWeaponUpgradeResult);
		}

		private void UnregisterEventListeners()
		{
			EventKit.Type.UnRegister<UIEvents.WeaponUpgradeResultEvent>(OnWeaponUpgradeResult);
		}

		private void OnWeaponUpgradeResult(UIEvents.WeaponUpgradeResultEvent e)
		{
			sonWeaponUpgradePanelData data = mData as sonWeaponUpgradePanelData;
			if (data == null || data.WeaponInstanceId != e.WeaponInstanceId)
				return;

			LogKit.Log($"[sonWeaponUpgradePanel] 武器升级结果: Success:{e.Success}, 武器:{e.WeaponId}");

			if (e.Success && AccountManager.Instance.IsLoggedIn)
			{
				string accountId = AccountManager.Instance.CurrentAccount.AccountId;
				mWeaponItem = AccountManager.Instance.GetAccountItemByInstanceId(
					accountId, "Weapon", data.WeaponInstanceId);
			}

			// 刷新所有UI显示
			GenerateUpgradeCosts();
			RefreshWeaponInfo();
			RefreshGoldCost();
			RefreshMaterialButtons();
			UpdateUpgradeButtonState();
		}

		private void OnBtnUpgradeClicked()
		{
			if (!mBtnUpgrade.interactable) return;
			if (mWeaponConfig == null || mWeaponItem == null) return;
			if (!AccountManager.Instance.IsLoggedIn) return;
			if (mWeaponItem.Level >= (mWeaponConfig.MaxLevel > 1 ? mWeaponConfig.MaxLevel : 80)) return;

			LogKit.Log($"[sonWeaponUpgradePanel] 请求升级武器: {mWeaponConfig.WeaponId}");

			// 发送武器升级请求事件，由BattlePreparationSystem执行完整业务（材料+金币扣除、升级、属性刷新）
			EventKit.Type.Send(new UIEvents.WeaponUpgradeRequestedEvent
			{
				WeaponId = mData.WeaponId,
				WeaponInstanceId = mData.WeaponInstanceId,
				TargetCharacterId = mData.TargetCharacterId
			});
		}

		private void OnBtnBackClicked()
		{
			UIKit.ClosePanel<sonWeaponUpgradePanel>();
		}
		#endregion
	}
}

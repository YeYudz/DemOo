using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using System.Collections.Generic;
using System.Linq;

namespace GameUI
{
	public sealed class sonCharacterUpgradePanelData : IUIData
	{
		public string CharacterId { get; set; }
		public string OpenerPanelType { get; set; }
		public object OpenerData { get; set; }
	}

	/// <summary>
	/// 角色升级面板：展示角色升级材料并执行升级。
	/// </summary>
	public partial class sonCharacterUpgradePanel : UIPanel
	{
		#region 生命周期与升级操作
		private PlayerCharacterConfig mCharacterConfig;
		private AccountItemData mCharacterItem;
		private Font mDefaultFont;
		private List<CostItem> mUpgradeCosts = new List<CostItem>();
		private int mGoldCost = 0;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as sonCharacterUpgradePanelData ?? new sonCharacterUpgradePanelData();
			LoadDefaultFont();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as sonCharacterUpgradePanelData ?? mData;
			LoadCharacterData();
			GenerateUpgradeCosts();
			RefreshCharacterInfo();
			RefreshGoldCost();
			RefreshMaterialButtons();
			UpdateUpgradeButtonState();
			RegisterButtonEvents();
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
			mCharacterConfig = null;
			mCharacterItem = null;
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

		private void LoadCharacterData()
		{
			mCharacterConfig = GameDataMgr.Instance.playerCharacterConfigs.Characters
				.FirstOrDefault(c => c.CharacterId == mData.CharacterId);

			if (AccountManager.Instance.IsLoggedIn)
			{
				string accountId = AccountManager.Instance.CurrentAccount.AccountId;
				mCharacterItem = AccountManager.Instance.GetAccountItem(accountId, "Character", mData.CharacterId);
			}
		}

		private int GetCharacterMaxLevel()
		{
			return mCharacterConfig != null && mCharacterConfig.MaxLevel > 0 ? mCharacterConfig.MaxLevel : 80;
		}

		private void GenerateUpgradeCosts()
		{
			mUpgradeCosts.Clear();

			if (mCharacterConfig == null || mCharacterItem == null) return;

			var costs = UpgradeSystem.Instance.GetCharacterUpgradeCost(mData.CharacterId);
			if (costs != null)
			{
				mUpgradeCosts.AddRange(costs);
			}

			mGoldCost = UpgradeSystem.Instance.GetCharacterUpgradeGoldCost(mData.CharacterId);
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

					// 设置材料图片，无图片时用颜色区分
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

		private void RefreshCharacterInfo()
		{
			if (mImgCharacter != null && mCharacterConfig != null)
			{
				SpriteIconLoader.SetCharacterIcon(mImgCharacter, mCharacterConfig.CharacterId);
				if (mImgCharacter.sprite == null)
				{
					mImgCharacter.color = GetCharacterColor(mCharacterConfig.CharacterId);
				}
			}

			if (mTexDetail != null)
			{
				if (mCharacterConfig != null && mCharacterItem != null)
				{
					int currentLevel = mCharacterItem.Level;
					int maxLevel = GetCharacterMaxLevel();
					int exp = mCharacterItem.Exp;
					int expToNext = mCharacterItem.ExpToNextLevel;

					var finalStats = UpgradeSystem.Instance.CalculateFinalStats(
						AccountManager.Instance.CurrentAccount.AccountId, mCharacterItem.ItemId);

					float hp = finalStats.ContainsKey("MaxHp") ? finalStats["MaxHp"] : 0f;
					float attack = finalStats.ContainsKey("Attack") ? finalStats["Attack"] : 0f;
					float defense = finalStats.ContainsKey("Defense") ? finalStats["Defense"] : 0f;
					float speed = finalStats.ContainsKey("Speed") ? finalStats["Speed"] : 0f;

					bool isMaxLevel = currentLevel >= maxLevel;

					mTexDetail.text = $"角色名称: {mCharacterConfig.Name}\n" +
									 $"等级: Lv.{currentLevel} / Lv.{maxLevel}\n" +
									 $"经验: {exp}/{expToNext}\n\n" +
									 $"当前属性:\n" +
									 $"  攻击力: {Mathf.RoundToInt(attack)}\n" +
									 $"  生命值: {Mathf.RoundToInt(hp)}\n" +
									 $"  防御力: {Mathf.RoundToInt(defense)}\n" +
									 $"  速度: {Mathf.RoundToInt(speed)}";

					if (isMaxLevel)
					{
						mTexDetail.text += "\n\n已达到最高等级";
					}
				}
				else
				{
					mTexDetail.text = "角色数据未找到";
				}
			}
		}

		private Color GetCharacterColor(string characterId)
		{
			switch (characterId)
			{
				case "Warrior": return new Color(1f, 0.3f, 0.3f);
				case "Mage": return new Color(0.3f, 0.3f, 1f);
				case "Archer": return new Color(0.3f, 1f, 0.3f);
				case "Healer": return new Color(1f, 1f, 0.3f);
				default: return Color.gray;
			}
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
				// "所需金币" 金黄色突出，数量达标情况绿色/红色
				mTexGoldCost.color = hasEnough ? new Color(0.2384f, 0.8490f, 0.2042f) : new Color(0.8509f, 0.3627f, 0.2039f);
			}
		}

		private void UpdateUpgradeButtonState()
		{
			if (mBtnUpgrade == null) return;

			bool isMaxLevel = mCharacterItem != null &&
				mCharacterItem.Level >= GetCharacterMaxLevel();

			if (isMaxLevel)
			{
				mBtnUpgrade.interactable = false;
				Text btnText = mBtnUpgrade.GetComponentInChildren<Text>();
				if (btnText != null)
				{
					btnText.text = "已满级";
					btnText.color = Color.gray;
				}
				Image btnImage = mBtnUpgrade.GetComponent<Image>();
				if (btnImage != null)
				{
					btnImage.color = new Color(0.5f, 0.5f, 0.5f);
				}
				return;
			}

			bool hasEnoughGold = GetCurrentGold() >= mGoldCost;
			bool allMaterialsEnough = CheckAllMaterialsEnough();
			mBtnUpgrade.interactable = hasEnoughGold && allMaterialsEnough;

			Image upgradeImage = mBtnUpgrade.GetComponent<Image>();
			if (upgradeImage != null)
			{
				if (!allMaterialsEnough)
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
				if (!allMaterialsEnough)
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

		private void OnBtnUpgradeClicked()
		{
			if (!mBtnUpgrade.interactable) return;
			if (mCharacterItem == null || mCharacterConfig == null) return;
			if (!AccountManager.Instance.IsLoggedIn) return;

			string accountId = AccountManager.Instance.CurrentAccount.AccountId;

			if (mCharacterItem.Level >= GetCharacterMaxLevel()) return;

			foreach (var cost in mUpgradeCosts)
			{
				var item = AccountManager.Instance.GetAccountItem(accountId, "Consumable", cost.ItemId);
				int ownedCount = item != null ? item.Count : 0;
				if (ownedCount < cost.Count)
				{
					LogKit.Log($"[sonCharacterUpgradePanel] 材料 {GetItemDisplayName(cost.ItemId)} 不足");
					return;
				}
			}

			LogKit.Log($"[sonCharacterUpgradePanel] 升级角色: {mData.CharacterId}");

			bool success = UpgradeSystem.Instance.UpgradeCharacter(accountId, mData.CharacterId, mUpgradeCosts, mGoldCost);

			if (success)
			{
				LogKit.Log($"[sonCharacterUpgradePanel] 角色升级成功");

				mCharacterItem = AccountManager.Instance.GetAccountItem(accountId, "Character", mData.CharacterId);

				EventKit.Type.Send(new UIEvents.CharacterDataChangedEvent
				{
					CharacterId = mData.CharacterId
				});

				GenerateUpgradeCosts();
				RefreshCharacterInfo();
				RefreshGoldCost();
				RefreshMaterialButtons();
				UpdateUpgradeButtonState();
			}
			else
			{
				LogKit.Log($"[sonCharacterUpgradePanel] 角色升级失败");
			}
		}

		private void OnBtnBackClicked()
		{
			UIKit.ClosePanel<sonCharacterUpgradePanel>();
		}
		#endregion
	}
}

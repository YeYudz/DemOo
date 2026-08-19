using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using System.Collections.Generic;
using System.Linq;

namespace GameUI
{
	public sealed class sonRelicUpgradePanelData : IUIData
	{
		public string RelicId { get; set; }
		public string RelicInstanceId { get; set; }
		public string TargetCharacterId { get; set; }
		public string TargetRelicSlot { get; set; }
		public string OpenerPanelType { get; set; }
		public object OpenerData { get; set; }
	}

	/// <summary>
	/// 遗器升级面板：展示升级材料并执行升级。
	/// </summary>
	public partial class sonRelicUpgradePanel : UIPanel
	{
		#region 生命周期与升级操作
		private RelicConfig mRelicConfig;
		private AccountRelicData mRelicData;
		private AccountItemData mRelicItem;
		private List<UpgradeMaterial> mUpgradeMaterials = new List<UpgradeMaterial>();

		private class UpgradeMaterial
		{
			public string MaterialId { get; set; }
			public string Name { get; set; }
			public int RequiredCount { get; set; }
			public int OwnedCount { get; set; }
			public Color Color { get; set; }
		}

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as sonRelicUpgradePanelData ?? new sonRelicUpgradePanelData();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as sonRelicUpgradePanelData ?? mData;
			LoadRelicData();
			GenerateUpgradeMaterials();
			RefreshRelicInfo();
			RefreshGoldCost();
			EnsureMaterialButtonsExist();
			RefreshMaterialButtons();
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
			mRelicConfig = null;
			mRelicData = null;
			mRelicItem = null;
			mUpgradeMaterials.Clear();
		}

		private void EnsureMaterialButtonsExist()
		{
			Button[] buttons = { mBtnMaterial1, mBtnMaterial2, mBtnMaterial3, mBtnMaterial4 };
			string[] btnNames = { "BtnMaterial1", "BtnMaterial2", "BtnMaterial3", "BtnMaterial4" };

			// 查找或创建材料按钮容器
			Transform materialContainer = transform.Find("MaterialContainer");
			if (materialContainer == null)
			{
				GameObject containerObj = new GameObject("MaterialContainer");
				containerObj.transform.SetParent(transform, false);
				RectTransform containerRect = containerObj.AddComponent<RectTransform>();
				containerRect.sizeDelta = new Vector2(350, 80);
				containerRect.anchoredPosition = new Vector2(0, -200);
				containerRect.anchorMin = new Vector2(0.5f, 0.5f);
				containerRect.anchorMax = new Vector2(0.5f, 0.5f);
				containerRect.pivot = new Vector2(0.5f, 0.5f);
				materialContainer = containerObj.transform;
			}

			for (int i = 0; i < 4; i++)
			{
				if (buttons[i] == null)
				{
					GameObject btnObj = new GameObject(btnNames[i]);
					btnObj.transform.SetParent(materialContainer, false);

					RectTransform rect = btnObj.AddComponent<RectTransform>();
					rect.sizeDelta = new Vector2(75, 75);
					rect.anchoredPosition = new Vector2(-120 + i * 80, 0);
					rect.anchorMin = new Vector2(0.5f, 0.5f);
					rect.anchorMax = new Vector2(0.5f, 0.5f);
					rect.pivot = new Vector2(0.5f, 0.5f);

					Image img = btnObj.AddComponent<Image>();
					img.color = new Color(0.9f, 0.9f, 0.9f);

					Button btn = btnObj.AddComponent<Button>();
					btn.targetGraphic = img;

					CreateMaterialText(btnObj.transform);

					if (i == 0) mBtnMaterial1 = btn;
					else if (i == 1) mBtnMaterial2 = btn;
					else if (i == 2) mBtnMaterial3 = btn;
					else if (i == 3) mBtnMaterial4 = btn;
				}
				else
				{
					// 确保已存在的按钮也有Text子组件
					EnsureTextChildExists(buttons[i].transform);
				}
			}
		}

		private void CreateMaterialText(Transform parent)
		{
			GameObject textObj = new GameObject("Text");
			textObj.transform.SetParent(parent, false);
			RectTransform textRect = textObj.AddComponent<RectTransform>();
			textRect.anchorMin = new Vector2(1, 0);
			textRect.anchorMax = new Vector2(1, 0);
			textRect.pivot = new Vector2(1, 0);
			textRect.sizeDelta = new Vector2(70, 30);
			textRect.anchoredPosition = new Vector2(-3, 3);

			Text text = textObj.AddComponent<Text>();
			text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			text.fontSize = 14;
			text.alignment = TextAnchor.LowerRight;
			text.color = new Color(0.0368f, 0.8679f, 0.8679f);
			text.fontStyle = FontStyle.Bold;
			text.text = "";
			text.horizontalOverflow = HorizontalWrapMode.Wrap;
			text.verticalOverflow = VerticalWrapMode.Overflow;
		}

		private void EnsureTextChildExists(Transform btnTransform)
		{
			Text existingText = btnTransform.GetComponentInChildren<Text>(true);
			if (existingText == null)
			{
				CreateMaterialText(btnTransform);
			}
		}

		private void LoadRelicData()
		{
			mRelicItem = null;
			mRelicConfig = null;
			mRelicData = null;

			if (!AccountManager.Instance.IsLoggedIn) return;

			string accountId = AccountManager.Instance.CurrentAccount.AccountId;
			mRelicItem = AccountManager.Instance.GetAccountItemByInstanceId(
				accountId, "Relic", mData.RelicInstanceId);
			mRelicConfig = GameDataMgr.Instance.relicConfigs.Relics
				.FirstOrDefault(r => mRelicItem != null && r.RelicId == mRelicItem.ItemId);

			if (mRelicItem != null && mRelicItem.ExtraData != null && mRelicItem.ExtraData.ContainsKey("RelicData"))
			{
				mRelicData = JsonUtility.FromJson<AccountRelicData>(mRelicItem.ExtraData["RelicData"]);
			}
			else if (mRelicConfig != null)
			{
				mRelicData = UpgradeSystem.Instance.GenerateRelicData(mRelicConfig);
			}
		}

		private void GenerateUpgradeMaterials()
		{
			mUpgradeMaterials.Clear();

			if (mRelicData == null) return;

			if (mRelicData.Level >= 15) return;

			var costs = UpgradeSystem.Instance.GetRelicUpgradeCost(mData.RelicInstanceId);
			if (costs == null || costs.Count == 0) return;

			Color[] materialColors = {
				new Color(0.8f, 0.6f, 0.4f),
				new Color(0.4f, 0.6f, 0.8f),
				new Color(0.6f, 0.4f, 0.8f),
				new Color(0.8f, 0.8f, 0.4f)
			};

			for (int i = 0; i < costs.Count; i++)
			{
				var cost = costs[i];
				mUpgradeMaterials.Add(new UpgradeMaterial
				{
					MaterialId = cost.ItemId,
					Name = GetItemDisplayName(cost.ItemId),
					RequiredCount = cost.Count,
					OwnedCount = GetMaterialCountFromInventory(cost.ItemId),
					Color = materialColors[i % materialColors.Length]
				});
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

		private int GetMaterialCountFromInventory(string materialId)
		{
			if (!AccountManager.Instance.IsLoggedIn) return 0;

			try
			{
				string accountId = AccountManager.Instance.CurrentAccount.AccountId;
				var item = AccountManager.Instance.GetAccountItem(accountId, "Consumable", materialId);
				return item != null ? item.Count : 0;
			}
			catch
			{
				return 0;
			}
		}

		private int CalculateGoldCost()
	{
		if (mRelicData == null || mRelicConfig == null) return 0;
		return UpgradeSystem.Instance.GetRelicUpgradeGoldCost(mData.RelicInstanceId);
	}

		private int GetCurrentGold()
		{
			if (!AccountManager.Instance.IsLoggedIn) return 0;
			string accountId = AccountManager.Instance.CurrentAccount.AccountId;
			return AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", "Gold");
		}

		private void RefreshRelicInfo()
		{
			if (mImgRelic != null)
			{
				if (mRelicConfig != null && mRelicItem != null)
				{
					SpriteIconLoader.SetRelicIcon(mImgRelic, mRelicItem.ItemId);
					if (mImgRelic.sprite == null)
					{
						mImgRelic.color = GetRelicRarityColor(mRelicConfig.Rarity);
					}
				}
				else
				{
					mImgRelic.color = Color.clear;
				}
			}

			if (mTexDetail != null)
			{
				if (mRelicConfig != null && mRelicData != null)
					{
						float mainDisplayValue = mRelicData.IsPercent ? mRelicData.MainStatValue * 100 : mRelicData.MainStatValue;
						string mainValueStr = mRelicData.IsPercent ? $"{Mathf.Round(mainDisplayValue * 10) / 10}%" : $"{Mathf.RoundToInt(mainDisplayValue)}";

						mTexDetail.text = $"遗器名称: {mRelicConfig.Name}\n" +
										 $"稀有度: {mRelicConfig.Rarity}星\n" +
										 $"部位: {mRelicConfig.Slot}\n" +
										 $"当前等级: Lv.{mRelicData.Level}\n\n" +
										 $"主词条: {mRelicData.MainStatType.GetDisplayName()} +{mainValueStr}\n\n";

						if (mRelicData.SubStats != null && mRelicData.SubStats.Count > 0)
						{
							mTexDetail.text += "副词条:\n";
							foreach (var subStat in mRelicData.SubStats)
							{
								float subDisplayValue = subStat.IsPercent ? subStat.Value * 100 : subStat.Value;
								string subValueStr = subStat.IsPercent ? $"+{Mathf.Round(subDisplayValue * 10) / 10}%" : $"+{Mathf.RoundToInt(subDisplayValue)}";
								mTexDetail.text += $"  - {subStat.StatType.GetDisplayName()} {subValueStr} (强化+{subStat.EnhanceLevel})\n";
							}
						}

						if (mRelicData.Level < 15)
						{
							float nextLevelMultiplier = (float)(mRelicData.Level + 1) / 15f;

							var mainStatConfig = UpgradeSystem.Instance.GetRelicMainStatConfig(mRelicConfig, mRelicData.MainStatType);
							if (mainStatConfig != null)
							{
								float minVal = mainStatConfig.MinValue;
								float maxVal = mainStatConfig.MaxValue;
								if (mainStatConfig.IsPercent)
								{
									minVal /= 100f;
									maxVal /= 100f;
								}

								float nextMainStatValue = minVal + (maxVal - minVal) * nextLevelMultiplier;
								float nextDisplayValue = mainStatConfig.IsPercent ? nextMainStatValue * 100 : nextMainStatValue;
								string nextValueStr = mainStatConfig.IsPercent ? $"{Mathf.Round(nextDisplayValue * 10) / 10}%" : $"{Mathf.RoundToInt(nextDisplayValue)}";
								mTexDetail.text += $"\n强化后预计:\n" +
													 $"主词条: {mRelicData.MainStatType.GetDisplayName()} +{nextValueStr}";
							}
						}
						else
						{
							mTexDetail.text += "\n已达最高等级";
						}
					}
				else
				{
					mTexDetail.text = "遗器数据未找到";
				}
			}
		}

		private Color GetRelicRarityColor(int rarity)
		{
			switch (rarity)
			{
				case 3: return new Color(0.8f, 0.8f, 0.8f);
				case 4: return new Color(0.5f, 0.5f, 1f);
				case 5: return new Color(1f, 0.8f, 0.2f);
				default: return Color.gray;
			}
		}

		private void RefreshGoldCost()
		{
			if (mTexGoldCost != null)
			{
				int goldCost = CalculateGoldCost();
				int currentGold = GetCurrentGold();
				bool hasEnough = currentGold >= goldCost;
				mTexGoldCost.text = $"所需金币\n{currentGold}/{goldCost}";
				mTexGoldCost.color = hasEnough ? new Color(0.2384f, 0.8490f, 0.2042f) : new Color(0.8509f, 0.3627f, 0.2039f);
			}

			UpdateUpgradeButtonState();
		}

		private void RefreshMaterialButtons()
		{
			Button[] materialButtons = { mBtnMaterial1, mBtnMaterial2, mBtnMaterial3, mBtnMaterial4 };

			for (int i = 0; i < 4; i++)
			{
				if (materialButtons[i] != null && i < mUpgradeMaterials.Count)
				{
					var material = mUpgradeMaterials[i];

					Image image = materialButtons[i].GetComponent<Image>();
					if (image != null)
					{
						SpriteIconLoader.SetConsumableIcon(image, material.MaterialId);
						if (image.sprite != null)
							image.color = Color.white;
						else
							image.color = material.Color;
					}

					Text text = materialButtons[i].GetComponentInChildren<Text>(true);
					if (text != null)
					{
						text.text = $"{material.Name}\n{material.OwnedCount}/{material.RequiredCount}";
						text.color = new Color(0.0368f, 0.8679f, 0.8679f);
						text.alignment = TextAnchor.LowerRight;
						text.fontStyle = FontStyle.Bold;
					}
				}
				else if (materialButtons[i] != null)
				{
					materialButtons[i].gameObject.SetActive(false);
				}
			}

			UpdateUpgradeButtonState();
		}

		private void UpdateUpgradeButtonState()
		{
			if (mBtnUpgrade == null) return;

			bool isMaxLevel = mRelicData != null && mRelicData.Level >= 15;
			bool hasEnoughGold = GetCurrentGold() >= CalculateGoldCost();
			bool allMaterialsEnough = mUpgradeMaterials.All(m => m.OwnedCount >= m.RequiredCount);

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
					upgradeText.text = hasEnoughGold ? "强化" : "金币不足";
					upgradeText.color = hasEnoughGold ? Color.white : Color.gray;
				}
			}
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
			EventKit.Type.Register<UIEvents.RelicEnhanceResultEvent>(OnRelicEnhanceResult);
		}

		private void UnregisterEventListeners()
		{
			EventKit.Type.UnRegister<UIEvents.RelicEnhanceResultEvent>(OnRelicEnhanceResult);
		}

		private void OnRelicEnhanceResult(UIEvents.RelicEnhanceResultEvent e)
		{
			sonRelicUpgradePanelData data = mData as sonRelicUpgradePanelData;
			if (data == null || data.RelicInstanceId != e.RelicInstanceId)
				return;

			LogKit.Log($"[sonRelicUpgradePanel] 遗器强化结果: Success:{e.Success}, 遗器:{e.RelicId}");

			// 重新加载遗器数据并刷新所有UI显示
			LoadRelicData();
			GenerateUpgradeMaterials();
			RefreshRelicInfo();
			RefreshGoldCost();
			RefreshMaterialButtons();
		}

		private void OnBtnUpgradeClicked()
		{
			if (!mBtnUpgrade.interactable) return;
			if (mRelicData == null || mRelicConfig == null) return;
			if (!AccountManager.Instance.IsLoggedIn) return;

			LogKit.Log($"[sonRelicUpgradePanel] 请求强化遗器: {mRelicConfig.RelicId}");

			// 发送遗器强化请求事件，由BattlePreparationSystem执行完整业务（材料校验+扣除、金币扣除、强化、属性刷新）
			EventKit.Type.Send(new UIEvents.RelicEnhanceRequestedEvent
			{
				RelicId = mData.RelicId,
				RelicInstanceId = mData.RelicInstanceId,
				TargetCharacterId = mData.TargetCharacterId
			});
		}

		private void OnBtnBackClicked()
		{
			UIKit.ClosePanel<sonRelicUpgradePanel>();
		}
		#endregion
	}
}

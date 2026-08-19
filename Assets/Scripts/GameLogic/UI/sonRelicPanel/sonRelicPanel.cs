using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using System.Collections.Generic;
using System.Linq;

namespace GameUI
{
	public sealed class sonRelicPanelData : IUIData
	{
		public string TargetCharacterId { get; set; }
		public string TargetRelicSlot { get; set; }
		public string OpenerPanelType { get; set; }
		public object OpenerData { get; set; }
	}

	/// <summary>
	/// 遗器选择面板：按部位查看并装备遗器。
	/// </summary>
	public partial class sonRelicPanel : UIPanel
	{
		#region 生命周期与遗器操作
		private List<AccountItemData> mRelics = new List<AccountItemData>();
		private AccountItemData mSelectedRelic;
		private AccountItemData mTargetCharacter;
		private string mCurrentSlot = "Head";
		private bool mIsInitializing = false;

		protected override void OnInit(IUIData uiData = null)
		{
			// 保存传递过来的数据，供OnOpen使用
			mData = uiData as sonRelicPanelData ?? new sonRelicPanelData();
			
			// 在OnInit中就更新mCurrentSlot，因为OnOpen的参数可能为null
			if (mData != null && !string.IsNullOrEmpty(mData.TargetRelicSlot))
			{
				mCurrentSlot = mData.TargetRelicSlot;
			}
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			// 优先使用OnOpen传递的数据，如果有的话
			sonRelicPanelData newData = uiData as sonRelicPanelData;
			if (newData != null)
			{
				mData = newData;
				if (!string.IsNullOrEmpty(newData.TargetRelicSlot))
				{
					mCurrentSlot = newData.TargetRelicSlot;
				}
			}
			// 否则使用OnInit中保存的数据（确保第一次打开时也能正确设置）
			else if (mData != null && !string.IsNullOrEmpty(mData.TargetRelicSlot))
			{
				mCurrentSlot = mData.TargetRelicSlot;
			}
			
			LoadTargetCharacter();
			LoadRelics();
			SelectEquippedRelic();
			RefreshRelicList();
			RefreshRelicDetail();
			RegisterButtonEvents();
			RegisterEventListeners();
			
			// 设置初始化标志，防止SetToggleSelection触发的onValueChanged事件覆盖mCurrentSlot
			mIsInitializing = true;
			RegisterToggleEvents();
			SetToggleSelection();
			mIsInitializing = false;
		}

		private string NormalizeSlotName(string slot)
		{
			switch (slot)
			{
				case "Hand": return "Hands";
				case "Hands": return "Hands";
				default: return slot;
			}
		}

		private void SelectEquippedRelic()
		{
			string normalizedSlot = NormalizeSlotName(mCurrentSlot);
			if (mTargetCharacter != null && mTargetCharacter.EquippedRelics != null && mTargetCharacter.EquippedRelics.ContainsKey(normalizedSlot))
			{
				string equippedRelicInstanceId = mTargetCharacter.EquippedRelics[normalizedSlot];
				mSelectedRelic = mRelics.FirstOrDefault(r => r.InstanceId == equippedRelicInstanceId);
			}
		}

		protected override void OnShow()
		{
			SetPanelSortingOrder();

			// 从子面板（如强化面板）返回时刷新遗器数据
			LoadTargetCharacter();
			LoadRelics();
			SelectEquippedRelic();
			RefreshRelicList();
			RefreshRelicDetail();
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
			mSelectedRelic = null;
			mTargetCharacter = null;
			mRelics.Clear();
		}

		private void LoadRelics()
		{
			mRelics.Clear();
			
			if (!AccountManager.Instance.IsLoggedIn) return;
			
			string accountId = AccountManager.Instance.CurrentAccount.AccountId;
			var allRelics = AccountManager.Instance.GetItemsByType(accountId, "Relic");
			
			RelicSlot slot = StringToRelicSlot(mCurrentSlot);
			mRelics = allRelics
				.Where(r => r.Count > 0)
				.Where(r => 
				{
					var config = GameDataMgr.Instance.relicConfigs.Relics.FirstOrDefault(rc => rc.RelicId == r.ItemId);
					return config != null && config.Slot == slot;
				})
				.ToList();

			// 已装备优先，其次按星级、等级降序，最后按配置索引降序聚合同 ID 个体。
			var relicConfigs = GameDataMgr.Instance.relicConfigs.Relics;
			mRelics = mRelics
				.OrderByDescending(IsItemEquipped)
				.ThenByDescending(item => relicConfigs
					.FirstOrDefault(config => config.RelicId == item.ItemId)?.Rarity ?? 0)
				.ThenByDescending(item => item.Level)
				.ThenByDescending(item => relicConfigs.FindIndex(config => config.RelicId == item.ItemId))
				.ThenBy(item => item.InstanceId)
				.ToList();
		}

		private bool IsItemEquipped(AccountItemData item)
		{
			return item.ExtraData != null && 
				item.ExtraData.ContainsKey("IsEquipped") && 
				item.ExtraData["IsEquipped"] == "true";
		}

		private RelicSlot StringToRelicSlot(string slotName)
		{
			switch (slotName)
			{
				case "Head": return RelicSlot.Head;
				case "Hand":
				case "Hands": return RelicSlot.Hands;
				case "Body": return RelicSlot.Body;
				case "Feet": return RelicSlot.Feet;
				default: return RelicSlot.Head;
			}
		}

		private void LoadTargetCharacter()
		{
			if (!AccountManager.Instance.IsLoggedIn) return;

			string accountId = AccountManager.Instance.CurrentAccount.AccountId;
			var characters = AccountManager.Instance.GetItemsByType(accountId, "Character");
			mTargetCharacter = characters.FirstOrDefault(c => c.ItemId == mData.TargetCharacterId);
		}

		private void RefreshRelicList()
		{
			if (mScrollView == null) return;

			Transform content = mScrollView.content;
			if (content == null) return;

			foreach (Transform child in content)
			{
				Object.Destroy(child.gameObject);
			}

			GridLayoutGroup layout = content.GetComponent<GridLayoutGroup>();
			if (layout == null)
			{
				layout = content.gameObject.AddComponent<GridLayoutGroup>();
			}

			const int columns = 10;
			const float gap = 10f;
			RectTransform viewportRt = mScrollView.viewport != null
				? mScrollView.viewport
				: mScrollView.GetComponent<RectTransform>();
			float containerWidth = viewportRt.rect.width;
			float cellSize = Mathf.Max(1f, (containerWidth - gap * (columns + 1)) / columns);

			layout.cellSize = new Vector2(cellSize, cellSize);
			layout.spacing = new Vector2(gap, gap);
			layout.padding = new RectOffset((int)gap, (int)gap, (int)gap, (int)gap);
			layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
			layout.startAxis = GridLayoutGroup.Axis.Horizontal;
			layout.childAlignment = TextAnchor.UpperLeft;
			layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			layout.constraintCount = columns;

			RectTransform contentRt = content.GetComponent<RectTransform>();
			if (contentRt != null)
			{
				contentRt.anchorMin = new Vector2(0f, 1f);
				contentRt.anchorMax = new Vector2(1f, 1f);
				contentRt.pivot = new Vector2(0.5f, 1f);
				contentRt.sizeDelta = new Vector2(0f, contentRt.sizeDelta.y);
			}

			foreach (var relic in mRelics)
			{
				CreateRelicItem(relic, content);
			}
		}

		private void CreateRelicItem(AccountItemData relic, Transform parent)
		{
			var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
				.FirstOrDefault(r => r.RelicId == relic.ItemId);
			
			// 只显示等级
			string displayText = $"Lv.{relic.Level}";

			GameObject item = new GameObject(relic.ItemId);
			item.transform.SetParent(parent, false);

			RectTransform rect = item.GetComponent<RectTransform>();
			if (rect == null) rect = item.AddComponent<RectTransform>();

			Image image = item.AddComponent<Image>();
			SpriteIconLoader.SetRelicIcon(image, relic.ItemId);
			if (image.sprite == null)
			{
				image.color = relicConfig != null ? GetRelicRarityColor(relicConfig.Rarity) : Color.white;
			}

			// 检查是否已装备
			bool isEquipped = relic.ExtraData != null && 
				relic.ExtraData.ContainsKey("IsEquipped") && 
				relic.ExtraData["IsEquipped"] == "true";
			
			if (isEquipped)
			{
				image.color = new Color(image.color.r * 0.5f, image.color.g * 0.5f, image.color.b * 0.5f, 0.7f);
			}

			// 在图片上方显示装备者名字
			if (isEquipped && relic.ExtraData != null && relic.ExtraData.ContainsKey("EquippedBy"))
			{
				string equippedBy = relic.ExtraData["EquippedBy"];
				string characterName = GetCharacterName(equippedBy);

				GameObject equipNameObj = new GameObject("EquipName");
				equipNameObj.transform.SetParent(item.transform, false);

				Text equipNameText = equipNameObj.AddComponent<Text>();
				equipNameText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
				equipNameText.text = characterName;
				equipNameText.fontSize = 10;
				equipNameText.alignment = TextAnchor.MiddleCenter;
				equipNameText.color = new Color(1f, 0.8f, 0f); // 金色

				RectTransform equipNameRect = equipNameObj.GetComponent<RectTransform>();
				equipNameRect.sizeDelta = new Vector2(100, 20);
				equipNameRect.localPosition = new Vector3(0, 40, 0);
			}

			GameObject textObj = new GameObject("Text");
			textObj.transform.SetParent(item.transform, false);

			Text text = textObj.AddComponent<Text>();
			text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			text.text = displayText;
			text.fontSize = 16;
			text.alignment = TextAnchor.LowerRight;
			text.color = isEquipped ? Color.gray : Color.white;
			text.fontStyle = FontStyle.Bold;

			RectTransform textRect = textObj.GetComponent<RectTransform>();
			textRect.anchorMin = new Vector2(1, 0);
			textRect.anchorMax = new Vector2(1, 0);
			textRect.pivot = new Vector2(1, 0);
			textRect.sizeDelta = new Vector2(60, 20);
			textRect.anchoredPosition = new Vector2(-5, 5);

			Button button = item.AddComponent<Button>();
			button.onClick.AddListener(() => OnRelicClicked(relic));
		}

		private string GetCharacterName(string characterId)
		{
			string accountId = AccountManager.Instance.CurrentAccount.AccountId;
			var characters = AccountManager.Instance.GetItemsByType(accountId, "Character");
			var character = characters.FirstOrDefault(c => c.ItemId == characterId);
			if (character != null)
			{
				var charConfig = GameDataMgr.Instance.playerCharacterConfigs.Characters
					.FirstOrDefault(ch => ch.CharacterId == character.ItemId);
				return charConfig != null ? charConfig.Name : characterId;
			}
			return characterId;
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

		private void OnRelicClicked(AccountItemData relic)
		{
			mSelectedRelic = relic;
			RefreshRelicDetail();
		}

		private void RefreshRelicDetail()
		{
			if (mTexDetail != null)
			{
				if (mSelectedRelic != null)
				{
					var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
						.FirstOrDefault(r => r.RelicId == mSelectedRelic.ItemId);

					if (relicConfig != null)
					{
						var relicData = GetRelicData(mSelectedRelic, relicConfig);

						// 主词条值显示：百分比属性乘以100，保留1位小数
						float mainDisplayValue = relicData.IsPercent ? relicData.MainStatValue * 100 : relicData.MainStatValue;
						string mainValueStr = relicData.IsPercent ? $"{Mathf.Round(mainDisplayValue * 10) / 10}%" : $"{Mathf.RoundToInt(mainDisplayValue)}";

						mTexDetail.text = $"遗器名称: {relicConfig.Name}\n" +
										 $"稀有度: {relicConfig.Rarity}星\n" +
										 $"部位: {relicConfig.Slot}\n" +
										 $"等级: Lv.{relicData.Level}\n\n" +
										 $"主词条: {relicData.MainStatType.GetDisplayName()} +{mainValueStr}\n\n";

						if (relicData.SubStats != null && relicData.SubStats.Count > 0)
						{
							mTexDetail.text += "副词条:\n";
							foreach (var subStat in relicData.SubStats)
							{
								float subDisplayValue = subStat.IsPercent ? subStat.Value * 100 : subStat.Value;
								string subValueStr = subStat.IsPercent ? $"+{Mathf.Round(subDisplayValue * 10) / 10}%" : $"+{Mathf.RoundToInt(subDisplayValue)}";
								mTexDetail.text += $"  - {subStat.StatType.GetDisplayName()} {subValueStr}\n";
							}
						}
					}
					else
					{
						mTexDetail.text = $"遗器ID: {mSelectedRelic.ItemId}\n等级: Lv.{mSelectedRelic.Level}";
					}
				}
				else if (mTargetCharacter != null && mTargetCharacter.EquippedRelics != null && mTargetCharacter.EquippedRelics.ContainsKey(NormalizeSlotName(mCurrentSlot)))
				{
					var equippedRelicItem = AccountManager.Instance.GetAccountItemByInstanceId(
						AccountManager.Instance.CurrentAccount.AccountId,
						"Relic",
						mTargetCharacter.EquippedRelics[NormalizeSlotName(mCurrentSlot)]);
					var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
						.FirstOrDefault(r => equippedRelicItem != null && r.RelicId == equippedRelicItem.ItemId);

					if (relicConfig != null)
					{
						mTexDetail.text = $"当前装备: {relicConfig.Name}\n" +
										 $"稀有度: {relicConfig.Rarity}星\n" +
										 $"部位: {relicConfig.Slot}";
					}
				}
				else
				{
					mTexDetail.text = "";
				}
			}

			UpdateChangeButtonState();
			RefreshSetEffect();
		}

		private void RefreshSetEffect()
		{
			if (mTexSetEffect == null || mTargetCharacter == null)
			{
				if (mTexSetEffect != null)
					mTexSetEffect.text = "";
				return;
			}

			Dictionary<string, int> setCounts = new Dictionary<string, int>();
			Dictionary<string, RelicConfig> setConfigs = new Dictionary<string, RelicConfig>();

			// 统计已装备的遗器套装
			if (mTargetCharacter.EquippedRelics != null)
			{
				foreach (var kvp in mTargetCharacter.EquippedRelics)
				{
					var relicItem = AccountManager.Instance.GetAccountItemByInstanceId(
						AccountManager.Instance.CurrentAccount.AccountId, "Relic", kvp.Value);
					var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
						.FirstOrDefault(r => relicItem != null && r.RelicId == relicItem.ItemId);

					if (relicConfig == null || string.IsNullOrEmpty(relicConfig.SetName))
						continue;

					string setName = relicConfig.SetName;
					if (!setCounts.ContainsKey(setName))
					{
						setCounts[setName] = 0;
						setConfigs[setName] = relicConfig;
					}
					setCounts[setName]++;
				}
			}

			// 如果选中了遗器，预览装备后的套装效果
			bool hasPreview = false;
			if (mSelectedRelic != null)
			{
				var selectedConfig = GameDataMgr.Instance.relicConfigs.Relics
					.FirstOrDefault(r => r.RelicId == mSelectedRelic.ItemId);

				if (selectedConfig != null && !string.IsNullOrEmpty(selectedConfig.SetName))
				{
					string setName = selectedConfig.SetName;
					if (!setCounts.ContainsKey(setName))
					{
						setCounts[setName] = 0;
						setConfigs[setName] = selectedConfig;
					}
					
					// 检查是否已经装备了该遗器
					bool alreadyEquipped = mTargetCharacter.EquippedRelics != null &&
						mTargetCharacter.EquippedRelics.ContainsValue(mSelectedRelic.InstanceId);
					
					if (!alreadyEquipped)
					{
						setCounts[setName]++;
						hasPreview = true;
					}
				}
			}

			string result = "";
			foreach (var kvp in setCounts)
			{
				string setName = kvp.Key;
				int count = kvp.Value;
				var config = setConfigs[setName];

				result += $"{setName}: {count}/4\n";

				if (count >= 1 && !string.IsNullOrEmpty(config.SetEffect1))
				{
					bool isActive = !hasPreview || (count >= 1);
					result += isActive ? $"  [已激活] {config.SetEffect1}\n" : $"  [未激活] {config.SetEffect1}\n";
				}

				if (count >= 3 && !string.IsNullOrEmpty(config.SetEffect2))
				{
					bool isActive = !hasPreview || (count >= 3);
					result += isActive ? $"  [已激活] {config.SetEffect2}\n" : $"  [未激活] {config.SetEffect2}\n";
				}
			}

			if (result == "")
			{
				mTexSetEffect.text = "套装效果: 无";
			}
			else
			{
				mTexSetEffect.text = (hasPreview ? "[预览] " : "") + result.Trim();
			}
		}

		private AccountRelicData GetRelicData(AccountItemData relicItem, RelicConfig config)
		{
			if (relicItem.ExtraData != null && relicItem.ExtraData.ContainsKey("RelicData"))
			{
				return JsonUtility.FromJson<AccountRelicData>(relicItem.ExtraData["RelicData"]);
			}
			return UpgradeSystem.Instance.GenerateRelicData(config);
		}

		private void UpdateChangeButtonState()
		{
			if (mBtnChange == null) return;

			mBtnChange.interactable = mSelectedRelic != null;

			Image changeImage = mBtnChange.GetComponent<Image>();
			Text changeText = mBtnChange.GetComponentInChildren<Text>();

			if (mSelectedRelic == null || mTargetCharacter == null)
			{
				if (changeText != null)
				{
					changeText.text = "装备";
					changeText.color = Color.white;
				}
				if (changeImage != null)
				{
					changeImage.color = new Color(0.2f, 0.6f, 0.2f); // 绿色 - 装备
				}
				return;
			}

			// 判断装备状态
			bool isEquippedBySelf = mSelectedRelic.ExtraData != null && 
				mSelectedRelic.ExtraData.ContainsKey("IsEquipped") && 
				mSelectedRelic.ExtraData["IsEquipped"] == "true" &&
				mSelectedRelic.ExtraData.ContainsKey("EquippedBy") &&
				mSelectedRelic.ExtraData["EquippedBy"] == mTargetCharacter.ItemId;

			bool isEquippedByOther = mSelectedRelic.ExtraData != null && 
				mSelectedRelic.ExtraData.ContainsKey("IsEquipped") && 
				mSelectedRelic.ExtraData["IsEquipped"] == "true" &&
				mSelectedRelic.ExtraData.ContainsKey("EquippedBy") &&
				mSelectedRelic.ExtraData["EquippedBy"] != mTargetCharacter.ItemId;

			if (changeImage != null)
			{
				if (isEquippedBySelf)
					changeImage.color = new Color(0.6f, 0.2f, 0.2f); // 红色 - 卸下
				else if (isEquippedByOther)
					changeImage.color = new Color(0.6f, 0.4f, 0.2f); // 橙色 - 替换
				else
					changeImage.color = new Color(0.2f, 0.6f, 0.2f); // 绿色 - 装备
			}

			if (changeText != null)
			{
				if (isEquippedBySelf)
					changeText.text = "卸下";
				else if (isEquippedByOther)
					changeText.text = "替换";
				else
					changeText.text = "装备";
				changeText.color = Color.white;
			}
		}

		private void RegisterButtonEvents()
		{
			if (mBtnChange != null)
			{
				mBtnChange.onClick.RemoveAllListeners();
				mBtnChange.onClick.AddListener(OnBtnChangeClicked);
			}

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
			EventKit.Type.Register<UIEvents.RelicChangeResultEvent>(OnRelicChangeResult);
			EventKit.Type.Register<UIEvents.RelicEnhanceResultEvent>(OnRelicEnhanceResult);
		}

		private void UnregisterEventListeners()
		{
			EventKit.Type.UnRegister<UIEvents.RelicChangeResultEvent>(OnRelicChangeResult);
			EventKit.Type.UnRegister<UIEvents.RelicEnhanceResultEvent>(OnRelicEnhanceResult);
		}

		private void OnRelicChangeResult(UIEvents.RelicChangeResultEvent e)
		{
			sonRelicPanelData data = mData as sonRelicPanelData;
			if (data == null || data.TargetCharacterId != e.TargetCharacterId)
				return;

			LogKit.Log($"[sonRelicPanel] 遗器变更结果: {e.ChangeType}, Success:{e.Success}, 遗器:{e.RelicId}");

			// 重新加载数据并刷新列表
			LoadRelics();
			RefreshRelicList();
			RefreshRelicDetail();
			RefreshSetEffect();
			UpdateChangeButtonState();
		}

		private void OnRelicEnhanceResult(UIEvents.RelicEnhanceResultEvent e)
		{
			if (!e.Success || mSelectedRelic == null || mSelectedRelic.InstanceId != e.RelicInstanceId)
				return;

			LoadTargetCharacter();
			LoadRelics();
			mSelectedRelic = mRelics.FirstOrDefault(relic => relic.InstanceId == e.RelicInstanceId);
			RefreshRelicList();
			RefreshRelicDetail();
			RefreshSetEffect();
		}

		private void RegisterToggleEvents()
		{
			if (mToggleHead != null)
			{
				mToggleHead.onValueChanged.RemoveAllListeners();
				mToggleHead.onValueChanged.AddListener((isOn) => { if (isOn) OnSlotChanged("Head"); });
			}

			if (mToggleHands != null)
			{
				mToggleHands.onValueChanged.RemoveAllListeners();
				mToggleHands.onValueChanged.AddListener((isOn) => { if (isOn) OnSlotChanged("Hand"); });
			}

			if (mToggleBody != null)
			{
				mToggleBody.onValueChanged.RemoveAllListeners();
				mToggleBody.onValueChanged.AddListener((isOn) => { if (isOn) OnSlotChanged("Body"); });
			}

			if (mToggleFeet != null)
			{
				mToggleFeet.onValueChanged.RemoveAllListeners();
				mToggleFeet.onValueChanged.AddListener((isOn) => { if (isOn) OnSlotChanged("Feet"); });
			}
		}

		private void SetToggleSelection()
		{
			if (mToggleHead != null) mToggleHead.isOn = (mCurrentSlot == "Head");
			if (mToggleHands != null) mToggleHands.isOn = (mCurrentSlot == "Hand");
			if (mToggleBody != null) mToggleBody.isOn = (mCurrentSlot == "Body");
			if (mToggleFeet != null) mToggleFeet.isOn = (mCurrentSlot == "Feet");
		}

		private void OnSlotChanged(string slot)
		{
			// 初始化期间忽略toggle事件，防止覆盖mCurrentSlot
			if (mIsInitializing) return;
			
			mCurrentSlot = slot;
			mSelectedRelic = null;
			LoadRelics();
			SelectEquippedRelic();
			RefreshRelicList();
			RefreshRelicDetail();
		}

		private void OnBtnChangeClicked()
		{
			if (mSelectedRelic == null || mTargetCharacter == null) return;
			if (!AccountManager.Instance.IsLoggedIn) return;

			LogKit.Log($"[sonRelicPanel] 请求遗器变更 - 角色:{mTargetCharacter.ItemId}, 遗器:{mSelectedRelic.ItemId}, 部位:{mCurrentSlot}");

			// 发送遗器变更请求事件，由BattlePreparationSystem执行业务逻辑（装备/卸下/替换）
			EventKit.Type.Send(new UIEvents.RelicChangeRequestedEvent
			{
				TargetCharacterId = mTargetCharacter.ItemId,
				RelicId = mSelectedRelic.ItemId,
				RelicInstanceId = mSelectedRelic.InstanceId,
				RelicSlot = mCurrentSlot
			});
		}

		private void OnBtnUpgradeClicked()
		{
			AccountItemData relicItem = mSelectedRelic;
			if (relicItem == null && mTargetCharacter != null &&
				mTargetCharacter.EquippedRelics != null &&
				mTargetCharacter.EquippedRelics.ContainsKey(NormalizeSlotName(mCurrentSlot)) &&
				AccountManager.Instance.IsLoggedIn)
			{
				relicItem = AccountManager.Instance.GetAccountItemByInstanceId(
					AccountManager.Instance.CurrentAccount.AccountId,
					"Relic",
					mTargetCharacter.EquippedRelics[NormalizeSlotName(mCurrentSlot)]);
			}

			if (relicItem != null)
			{
				UIKit.OpenPanel<sonRelicUpgradePanel>(UILevel.Pop, data: new sonRelicUpgradePanelData
				{
					RelicId = relicItem.ItemId,
					RelicInstanceId = relicItem.InstanceId,
					TargetCharacterId = mData.TargetCharacterId,
					TargetRelicSlot = mCurrentSlot,
					OpenerPanelType = "sonRelicPanel",
					OpenerData = new sonRelicPanelData 
					{ 
						TargetCharacterId = mData.TargetCharacterId,
						TargetRelicSlot = mCurrentSlot
					}
				});
			}
		}

		private void OnBtnBackClicked()
		{
			UIKit.ClosePanel<sonRelicPanel>();
		}
		#endregion
	}
}

using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using System.Collections.Generic;
using System.Linq;

namespace GameUI
{
	public sealed class sonWeaponPanelData : IUIData
	{
		public string TargetCharacterId { get; set; }
		public string OpenerPanelType { get; set; }
		public object OpenerData { get; set; }
	}

	/// <summary>
	/// 武器选择面板：查看并装备角色武器。
	/// </summary>
	public partial class sonWeaponPanel : UIPanel
	{
		#region 生命周期与武器操作
		private List<AccountItemData> mWeapons = new List<AccountItemData>();
		private AccountItemData mSelectedWeapon;
		private AccountItemData mTargetCharacter;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as sonWeaponPanelData ?? new sonWeaponPanelData();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as sonWeaponPanelData ?? mData;
			LoadTargetCharacter();
			LoadWeapons();
			SelectEquippedWeapon();
			RefreshWeaponList();
			RefreshWeaponDetail();
			RegisterButtonEvents();
			RegisterEventListeners();
		}

		private void SelectEquippedWeapon()
		{
			if (mTargetCharacter != null && !string.IsNullOrEmpty(mTargetCharacter.EquippedWeapon))
			{
				mSelectedWeapon = mWeapons.FirstOrDefault(w => w.InstanceId == mTargetCharacter.EquippedWeapon);
			}
		}

		protected override void OnShow()
		{
			SetPanelSortingOrder();

			// 从子面板（如升级面板）返回时刷新武器数据
			LoadWeapons();
			SelectEquippedWeapon();
			RefreshWeaponList();
			RefreshWeaponDetail();
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
			mSelectedWeapon = null;
			mTargetCharacter = null;
			mWeapons.Clear();
		}

		private void LoadWeapons()
		{
			mWeapons.Clear();
			
			if (!AccountManager.Instance.IsLoggedIn) return;
			
			string accountId = AccountManager.Instance.CurrentAccount.AccountId;
			var allWeapons = AccountManager.Instance.GetItemsByType(accountId, "Weapon");
			
			if (mTargetCharacter == null)
			{
				mWeapons = allWeapons.ToList();
			}
			else
			{
				CharacterClass requiredClass = GetCharacterClass(mTargetCharacter.ItemId);
				mWeapons = allWeapons
					.Where(w => 
					{
						var weaponConfig = GameDataMgr.Instance.lightConeConfigs.Weapons
							.FirstOrDefault(wc => wc.WeaponId == w.ItemId);
						return weaponConfig != null && weaponConfig.CompatibleClasses.Contains(requiredClass);
					})
					.ToList();
			}

			// 已装备优先，其次按星级、等级降序，最后按配置索引降序聚合同 ID 个体。
			var weaponConfigs = GameDataMgr.Instance.lightConeConfigs.Weapons;
			mWeapons = mWeapons
				.OrderByDescending(IsItemEquipped)
				.ThenByDescending(item => weaponConfigs
					.FirstOrDefault(config => config.WeaponId == item.ItemId)?.Rarity ?? 0)
				.ThenByDescending(item => item.Level)
				.ThenByDescending(item => weaponConfigs.FindIndex(config => config.WeaponId == item.ItemId))
				.ThenBy(item => item.InstanceId)
				.ToList();
		}

		private bool IsItemEquipped(AccountItemData item)
		{
			return item.ExtraData != null && 
				item.ExtraData.ContainsKey("IsEquipped") && 
				item.ExtraData["IsEquipped"] == "true";
		}

		private CharacterClass GetCharacterClass(string characterId)
		{
			switch (characterId)
			{
				case "Warrior": return CharacterClass.Warrior;
				case "Archer": return CharacterClass.Archer;
				case "Mage": return CharacterClass.Mage;
				case "Healer": return CharacterClass.Healer;
				default: return CharacterClass.Warrior;
			}
		}

		private void LoadTargetCharacter()
		{
			if (!AccountManager.Instance.IsLoggedIn) return;

			string accountId = AccountManager.Instance.CurrentAccount.AccountId;
			var characters = AccountManager.Instance.GetItemsByType(accountId, "Character");
			mTargetCharacter = characters.FirstOrDefault(c => c.ItemId == mData.TargetCharacterId);
		}

		private void RefreshWeaponList()
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

			foreach (var weaponItem in mWeapons)
			{
				CreateWeaponItem(weaponItem, content);
			}
		}

		private void CreateWeaponItem(AccountItemData weaponItem, Transform parent)
		{
			var weaponConfig = GameDataMgr.Instance.lightConeConfigs.Weapons
				.FirstOrDefault(w => w.WeaponId == weaponItem.ItemId);
			
			// 只显示等级
			string displayText = $"Lv.{weaponItem.Level}";

			GameObject item = new GameObject(weaponItem.ItemId);
			item.transform.SetParent(parent, false);

			RectTransform rect = item.GetComponent<RectTransform>();
			if (rect == null) rect = item.AddComponent<RectTransform>();

			Image image = item.AddComponent<Image>();
			SpriteIconLoader.SetWeaponIcon(image, weaponItem.ItemId);
			if (image.sprite == null)
			{
				image.color = weaponConfig != null ? GetWeaponRarityColor(weaponConfig.Rarity) : Color.white;
			}

			// 检查是否已装备
			bool isEquipped = weaponItem.ExtraData != null && 
				weaponItem.ExtraData.ContainsKey("IsEquipped") && 
				weaponItem.ExtraData["IsEquipped"] == "true";
			
			if (isEquipped)
			{
				if (image.sprite != null)
				{
					image.color = new Color(1f, 1f, 1f, 0.5f);
				}
				else
				{
					image.color = new Color(image.color.r * 0.5f, image.color.g * 0.5f, image.color.b * 0.5f, 0.7f);
				}
			}

			// 在图片上方显示装备者名字
			if (isEquipped && weaponItem.ExtraData != null && weaponItem.ExtraData.ContainsKey("EquippedBy"))
			{
				string equippedBy = weaponItem.ExtraData["EquippedBy"];
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
			button.onClick.AddListener(() => OnWeaponClicked(weaponItem));
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

		private void OnWeaponClicked(AccountItemData weaponItem)
		{
			mSelectedWeapon = weaponItem;
			RefreshWeaponDetail();
		}

		private void RefreshWeaponDetail()
		{
			if (mTexDetail != null)
			{
				if (mSelectedWeapon != null)
				{
					var weaponConfig = GameDataMgr.Instance.lightConeConfigs.Weapons
						.FirstOrDefault(w => w.WeaponId == mSelectedWeapon.ItemId);

					if (weaponConfig != null)
					{
						int weaponLevel = mSelectedWeapon.Level;
						UpgradeSystem.Instance.CalculateWeaponBaseStats(weaponConfig, weaponLevel, out float currentMaxHp, out float currentAttack, out float currentDefense);

						mTexDetail.text = $"武器名称: {weaponConfig.Name}\n" +
										 $"稀有度: {weaponConfig.Rarity}星\n" +
										 $"等级: Lv.{weaponLevel}\n" +
										 $"攻击力: {Mathf.RoundToInt(currentAttack)}\n" +
										 $"防御力: {Mathf.RoundToInt(currentDefense)}\n" +
										 $"生命值: {Mathf.RoundToInt(currentMaxHp)}\n\n";

						if (weaponConfig.Effects != null && weaponConfig.Effects.Count > 0)
						{
							mTexDetail.text += "武器效果:\n";
							foreach (var effect in weaponConfig.Effects)
							{
								mTexDetail.text += $"  - {effect.EffectName}: {effect.Description}\n";
							}
						}
					}
					else
					{
						mTexDetail.text = $"武器ID: {mSelectedWeapon.ItemId}\n等级: Lv.{mSelectedWeapon.Level}";
					}
				}
				else if (mTargetCharacter != null && !string.IsNullOrEmpty(mTargetCharacter.EquippedWeapon))
				{
					var equippedWeaponItem = AccountManager.Instance.GetAccountItemByInstanceId(
						AccountManager.Instance.CurrentAccount.AccountId, "Weapon", mTargetCharacter.EquippedWeapon);
					var equippedWeapon = GameDataMgr.Instance.lightConeConfigs.Weapons
						.FirstOrDefault(w => equippedWeaponItem != null && w.WeaponId == equippedWeaponItem.ItemId);
					int weaponLevel = equippedWeaponItem != null ? equippedWeaponItem.Level : 1;

					if (equippedWeapon != null)
					{
						UpgradeSystem.Instance.CalculateWeaponBaseStats(equippedWeapon, weaponLevel, out float currentMaxHp, out float currentAttack, out float currentDefense);

						mTexDetail.text = $"当前装备: {equippedWeapon.Name}\n" +
										 $"稀有度: {equippedWeapon.Rarity}星\n" +
										 $"等级: Lv.{weaponLevel}\n" +
										 $"攻击力: {Mathf.RoundToInt(currentAttack)}\n" +
										 $"防御力: {Mathf.RoundToInt(currentDefense)}\n" +
										 $"生命值: {Mathf.RoundToInt(currentMaxHp)}";
					}
				}
				else
				{
					mTexDetail.text = "";
				}
			}

			UpdateChangeButtonState();
		}

		private void UpdateChangeButtonState()
		{
			if (mBtnChange == null) return;

			mBtnChange.interactable = mSelectedWeapon != null;

			Image changeImage = mBtnChange.GetComponent<Image>();
			Text changeText = mBtnChange.GetComponentInChildren<Text>();

			if (mSelectedWeapon == null || mTargetCharacter == null)
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
			bool isEquippedBySelf = mSelectedWeapon.ExtraData != null && 
				mSelectedWeapon.ExtraData.ContainsKey("IsEquipped") && 
				mSelectedWeapon.ExtraData["IsEquipped"] == "true" &&
				mSelectedWeapon.ExtraData.ContainsKey("EquippedBy") &&
				mSelectedWeapon.ExtraData["EquippedBy"] == mTargetCharacter.ItemId;

			bool isEquippedByOther = mSelectedWeapon.ExtraData != null && 
				mSelectedWeapon.ExtraData.ContainsKey("IsEquipped") && 
				mSelectedWeapon.ExtraData["IsEquipped"] == "true" &&
				mSelectedWeapon.ExtraData.ContainsKey("EquippedBy") &&
				mSelectedWeapon.ExtraData["EquippedBy"] != mTargetCharacter.ItemId;

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
			EventKit.Type.Register<UIEvents.WeaponChangeResultEvent>(OnWeaponChangeResult);
			EventKit.Type.Register<UIEvents.WeaponUpgradeResultEvent>(OnWeaponUpgradeResult);
		}

		private void UnregisterEventListeners()
		{
			EventKit.Type.UnRegister<UIEvents.WeaponChangeResultEvent>(OnWeaponChangeResult);
			EventKit.Type.UnRegister<UIEvents.WeaponUpgradeResultEvent>(OnWeaponUpgradeResult);
		}

		private void OnWeaponChangeResult(UIEvents.WeaponChangeResultEvent e)
		{
			sonWeaponPanelData data = mData as sonWeaponPanelData;
			if (data == null || data.TargetCharacterId != e.TargetCharacterId)
				return;

			LogKit.Log($"[sonWeaponPanel] 武器变更结果: {e.ChangeType}, Success:{e.Success}, 武器:{e.WeaponId}");

			// 重新加载数据并刷新列表
			LoadWeapons();
			RefreshWeaponList();
			RefreshWeaponDetail();
			UpdateChangeButtonState();
		}

		private void OnWeaponUpgradeResult(UIEvents.WeaponUpgradeResultEvent e)
		{
			if (!e.Success || mSelectedWeapon == null || mSelectedWeapon.InstanceId != e.WeaponInstanceId)
				return;

			LoadTargetCharacter();
			LoadWeapons();
			mSelectedWeapon = mWeapons.FirstOrDefault(weapon => weapon.InstanceId == e.WeaponInstanceId);
			RefreshWeaponList();
			RefreshWeaponDetail();
		}

		private void OnBtnChangeClicked()
		{
			if (mSelectedWeapon == null || mTargetCharacter == null) return;
			if (!AccountManager.Instance.IsLoggedIn) return;

			LogKit.Log($"[sonWeaponPanel] 请求武器变更 - 角色:{mTargetCharacter.ItemId}, 武器:{mSelectedWeapon.ItemId}");

			// 发送武器变更请求事件，由BattlePreparationSystem执行业务逻辑（装备/卸下/替换）
			EventKit.Type.Send(new UIEvents.WeaponChangeRequestedEvent
			{
				TargetCharacterId = mTargetCharacter.ItemId,
				WeaponId = mSelectedWeapon.ItemId,
				WeaponInstanceId = mSelectedWeapon.InstanceId
			});
		}

		private void OnBtnUpgradeClicked()
		{
			AccountItemData weaponItem = mSelectedWeapon;
			if (weaponItem == null && mTargetCharacter != null &&
				!string.IsNullOrEmpty(mTargetCharacter.EquippedWeapon) &&
				AccountManager.Instance.IsLoggedIn)
			{
				weaponItem = AccountManager.Instance.GetAccountItemByInstanceId(
					AccountManager.Instance.CurrentAccount.AccountId,
					"Weapon",
					mTargetCharacter.EquippedWeapon);
			}

			if (weaponItem != null)
			{
				UIKit.OpenPanel<sonWeaponUpgradePanel>(UILevel.Pop, data: new sonWeaponUpgradePanelData
				{
					WeaponId = weaponItem.ItemId,
					WeaponInstanceId = weaponItem.InstanceId,
					TargetCharacterId = mData.TargetCharacterId,
					OpenerPanelType = "sonWeaponPanel",
					OpenerData = new sonWeaponPanelData { TargetCharacterId = mData.TargetCharacterId }
				});
			}
		}

		private void OnBtnBackClicked()
		{
			UIKit.ClosePanel<sonWeaponPanel>();
		}
		#endregion
	}
}

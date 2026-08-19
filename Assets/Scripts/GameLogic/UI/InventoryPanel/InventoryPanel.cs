using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using System.Collections.Generic;
using System.Linq;

namespace GameUI
{
	public sealed class InventoryPanelData : IUIData
	{
	}

	/// <summary>
	/// 背包面板：筛选物品并查看详情。
	/// </summary>
	public partial class InventoryPanel : UIPanel
	{
		#region 生命周期与物品筛选
		private List<AccountItemData> mAllItems = new List<AccountItemData>();
		private List<AccountItemData> mFilteredItems = new List<AccountItemData>();
		private string mCurrentCategory = "All";
		private Font mDefaultFont;

		private List<GameObject> mActiveGrids = new List<GameObject>();
		private Stack<GameObject> mPooledGrids = new Stack<GameObject>();

		// 筛选状态
		private HashSet<int> mSelectedStars = new HashSet<int>();
		private HashSet<int> mSelectedWeaponTypes = new HashSet<int>();
		private HashSet<int> mSelectedRelicSlots = new HashSet<int>();
		private bool mIsScreenPanelOpen = false;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as InventoryPanelData ?? new InventoryPanelData();
			LoadDefaultFont();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as InventoryPanelData ?? mData;
			LoadItems();
			mCurrentCategory = "All";
			mSelectedStars.Clear();
			mSelectedWeaponTypes.Clear();
			mSelectedRelicSlots.Clear();
			mIsScreenPanelOpen = false;
			if (mToggleAll != null)
				mToggleAll.isOn = true;
			RegisterToggleEvents();
			RegisterFilterEvents();
			UpdateScreenPanelVisibility();
			FilterItems();
			RefreshItemGrid();
			ClearItemDetail();
		}

		protected override void OnShow()
		{
			SetPanelSortingOrder();

			if (mBackButton != null)
			{
				mBackButton.onClick.RemoveAllListeners();
				mBackButton.onClick.AddListener(OnBackButtonClicked);
			}

			if (mBtnScreen != null)
			{
				mBtnScreen.onClick.RemoveAllListeners();
				mBtnScreen.onClick.AddListener(OnBtnScreenClicked);
			}

			if (mBtnCloseScreen != null)
			{
				mBtnCloseScreen.onClick.RemoveAllListeners();
				mBtnCloseScreen.onClick.AddListener(OnBtnCloseScreenClicked);
			}

			UpdateScreenPanelVisibility();
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
			if (mBackButton != null)
				mBackButton.onClick.RemoveAllListeners();
			if (mBtnScreen != null)
				mBtnScreen.onClick.RemoveAllListeners();
			if (mBtnCloseScreen != null)
				mBtnCloseScreen.onClick.RemoveAllListeners();
		}

		protected override void OnClose()
		{
			foreach (var grid in mActiveGrids)
			{
				Object.Destroy(grid);
			}
			mActiveGrids.Clear();

			foreach (var grid in mPooledGrids)
			{
				Object.Destroy(grid);
			}
			mPooledGrids.Clear();
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

		private void LoadItems()
		{
			if (!AccountManager.Instance.IsLoggedIn)
				return;

			string accountId = AccountManager.Instance.CurrentAccount.AccountId;
			var weaponConfigs = GameDataMgr.Instance.lightConeConfigs.Weapons;
			var relicConfigs = GameDataMgr.Instance.relicConfigs.Relics;
			mAllItems = AccountManager.Instance.GetAccountItems(accountId)
				.Where(item => item.ItemType != "Character")
				.OrderBy(item => item.ItemType == "Weapon" || item.ItemType == "Relic" ? 0 : 1)
				.ThenByDescending(item => IsItemEquipped(item))
				.ThenByDescending(item => item.ItemType == "Weapon"
					? weaponConfigs.FirstOrDefault(config => config.WeaponId == item.ItemId)?.Rarity ?? 0
					: item.ItemType == "Relic"
						? relicConfigs.FirstOrDefault(config => config.RelicId == item.ItemId)?.Rarity ?? 0
						: 0)
				.ThenByDescending(item => item.ItemType == "Weapon" || item.ItemType == "Relic" ? item.Level : 0)
				.ThenByDescending(item => item.ItemType == "Weapon"
					? weaponConfigs.FindIndex(config => config.WeaponId == item.ItemId)
					: item.ItemType == "Relic"
						? relicConfigs.FindIndex(config => config.RelicId == item.ItemId)
						: 0)
				.ThenBy(item => item.ItemType == "Weapon" || item.ItemType == "Relic" ? item.InstanceId : "")
				.ToList();
		}

		private bool IsItemEquipped(AccountItemData item)
		{
			return item.ExtraData != null &&
				item.ExtraData.ContainsKey("IsEquipped") &&
				item.ExtraData["IsEquipped"] == "true";
		}

		private void RegisterToggleEvents()
		{
			BindToggle(mToggleAll, "All");
			BindToggle(mToggleWeapon, "Weapon");
			BindToggle(mToggleRelic, "Relic");
			BindToggle(mToggleConsume, "Consumable");
			BindToggle(mToggleOther, "Other");
		}

		private void BindToggle(Toggle toggle, string category)
		{
			if (toggle == null) return;
			toggle.onValueChanged.RemoveAllListeners();
			toggle.onValueChanged.AddListener((bool isOn) =>
			{
				if (isOn)
				{
					OnCategoryChanged(category);
				}
			});
		}

		private void OnCategoryChanged(string category)
		{
			if (mCurrentCategory == category) return;

			mCurrentCategory = category;
			mIsScreenPanelOpen = false;
			UpdateScreenPanelVisibility();
			FilterItems();
			RefreshItemGrid();
			ClearItemDetail();
		}

		private void FilterItems()
		{
			mFilteredItems.Clear();

			IEnumerable<AccountItemData> query = mAllItems;

			if (mCurrentCategory == "All")
			{
				// 不过滤类型
			}
			else if (mCurrentCategory == "Other")
			{
				query = query.Where(item =>
					item.ItemType != "Weapon" &&
					item.ItemType != "Relic" &&
					item.ItemType != "Consumable");
			}
			else
			{
				query = query.Where(item => item.ItemType == mCurrentCategory);
			}

			// 星级筛选
			if (mCurrentCategory == "Weapon" || mCurrentCategory == "Relic")
			{
				if (mSelectedStars.Count > 0)
				{
					query = query.Where(item =>
						mSelectedStars.Contains(GetItemRarity(item)));
				}

				// 武器类型筛选
				if (mCurrentCategory == "Weapon" && mSelectedWeaponTypes.Count > 0)
				{
					query = query.Where(item =>
						mSelectedWeaponTypes.Contains(GetItemWeaponType(item)));
				}

				// 遗器部位筛选
				if (mCurrentCategory == "Relic" && mSelectedRelicSlots.Count > 0)
				{
					query = query.Where(item =>
						mSelectedRelicSlots.Contains(GetItemRelicSlot(item)));
				}
			}

			mFilteredItems.AddRange(query);
		}

		private int GetItemRarity(AccountItemData item)
		{
			if (item.ItemType == "Weapon")
			{
				var weaponConfig = GameDataMgr.Instance.lightConeConfigs?.Weapons
					?.FirstOrDefault(w => w.WeaponId == item.ItemId);
				return weaponConfig?.Rarity ?? 0;
			}
			else if (item.ItemType == "Relic")
			{
				var relicConfig = GameDataMgr.Instance.relicConfigs?.Relics
					?.FirstOrDefault(r => r.RelicId == item.ItemId);
				return relicConfig?.Rarity ?? 0;
			}
			return 0;
		}

		private int GetItemWeaponType(AccountItemData item)
		{
			var weaponConfig = GameDataMgr.Instance.lightConeConfigs?.Weapons
				?.FirstOrDefault(w => w.WeaponId == item.ItemId);
			if (weaponConfig != null)
				return (int)weaponConfig.WeaponType;
			return -1;
		}

		private int GetItemRelicSlot(AccountItemData item)
		{
			var relicConfig = GameDataMgr.Instance.relicConfigs?.Relics
				?.FirstOrDefault(r => r.RelicId == item.ItemId);
			if (relicConfig != null)
				return (int)relicConfig.Slot;
			return -1;
		}

		private int CalculateTotalSlots()
		{
			int count = 0;
			foreach (var item in mFilteredItems)
			{
				if (item.ItemType == "Weapon" || item.ItemType == "Relic")
				{
					count += item.Count;
				}
				else
				{
					count++;
				}
			}
			return count;
		}

		private void RefreshItemGrid()
		{
			if (mItemGrid == null)
				return;

			Transform content = mItemGrid.Find("Viewport/Content");
			if (content == null)
				content = mItemGrid;

			GridLayoutGroup layout = content.GetComponent<GridLayoutGroup>();
			if (layout == null)
				layout = content.gameObject.AddComponent<GridLayoutGroup>();

			RectTransform contentRt = content.GetComponent<RectTransform>();
			float containerWidth = contentRt.rect.width;
			int maxColumns = 6;
			float paddingX = 5f;
			float paddingY = 5f;

			float totalSpacing = paddingX * (maxColumns + 1);
			float cellWidth = (containerWidth - totalSpacing) / maxColumns;
			float cellHeight = cellWidth;

			layout.cellSize = new Vector2(cellWidth, cellHeight);
			layout.spacing = new Vector2(paddingX, paddingY);
			layout.padding = new RectOffset((int)paddingX, (int)paddingX, (int)paddingY, (int)paddingY);
			layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
			layout.startAxis = GridLayoutGroup.Axis.Horizontal;
			layout.childAlignment = TextAnchor.UpperLeft;
			layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			layout.constraintCount = maxColumns;

			ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
			if (fitter == null)
				fitter = content.gameObject.AddComponent<ContentSizeFitter>();
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

			ScrollRect scrollRect = mItemGrid.GetComponent<ScrollRect>();
			if (scrollRect != null)
			{
				scrollRect.horizontal = false;
				scrollRect.vertical = true;
				scrollRect.movementType = ScrollRect.MovementType.Clamped;
				scrollRect.inertia = false;
			}

			int totalSlots = CalculateTotalSlots();

			if (mActiveGrids.Count > totalSlots)
			{
				int excessCount = mActiveGrids.Count - totalSlots;
				for (int i = mActiveGrids.Count - 1; i >= totalSlots; i--)
				{
					GameObject grid = mActiveGrids[i];
					grid.SetActive(false);
					mPooledGrids.Push(grid);
				}
				mActiveGrids.RemoveRange(totalSlots, excessCount);
			}

			int slotIndex = 0;
			foreach (var item in mFilteredItems)
			{
				int itemCount = (item.ItemType == "Weapon" || item.ItemType == "Relic") ? item.Count : 1;
				for (int i = 0; i < itemCount; i++)
				{
					GameObject grid;
					if (slotIndex < mActiveGrids.Count)
					{
						grid = mActiveGrids[slotIndex];
					}
					else
					{
						if (mPooledGrids.Count > 0)
						{
							grid = mPooledGrids.Pop();
							grid.transform.SetParent(content, false);
						}
						else
						{
							grid = CreateGridObject(content);
						}
						mActiveGrids.Add(grid);
					}

					grid.SetActive(true);
					grid.name = i > 0 ? $"{item.ItemId}_{i + 1}" : item.ItemId;
					UpdateGridContent(grid, item, i + 1);
					slotIndex++;
				}
			}

			LogKit.Log($"[InventoryPanel] 当前分类: {mCurrentCategory}, 物品数量: {mFilteredItems.Count}, 格子数量: {mActiveGrids.Count}, 池化数量: {mPooledGrids.Count}");
		}

		private GameObject CreateGridObject(Transform parent)
		{
			GameObject gridObj = new GameObject("ItemGrid");
			gridObj.transform.SetParent(parent, false);

			RectTransform rect = gridObj.AddComponent<RectTransform>();
			rect.sizeDelta = new Vector2(90, 90);

			Image image = gridObj.AddComponent<Image>();

			Button button = gridObj.AddComponent<Button>();

			GameObject textObj = new GameObject("Text");
			textObj.transform.SetParent(gridObj.transform, false);
			RectTransform textRect = textObj.AddComponent<RectTransform>();
			textRect.anchorMin = new Vector2(1, 0);
			textRect.anchorMax = new Vector2(1, 0);
			textRect.pivot = new Vector2(1, 0);
			textRect.sizeDelta = new Vector2(60, 20);
			textRect.anchoredPosition = new Vector2(-5, 5);

			Text text = textObj.AddComponent<Text>();
			text.font = mDefaultFont;
			text.alignment = TextAnchor.LowerRight;
			text.color = Color.white;
			text.fontSize = 17;
			text.fontStyle = FontStyle.Bold;

			return gridObj;
		}

		private void UpdateGridContent(GameObject gridObj, AccountItemData item, int instanceIndex = 0)
		{
			Image image = gridObj.GetComponent<Image>();
			if (image != null)
			{
				if (!string.IsNullOrEmpty(item.ItemType) && !string.IsNullOrEmpty(item.ItemId))
				{
					SpriteIconLoader.SetIcon(image, item.ItemType, item.ItemId);
				}
				if (image.sprite == null)
				{
					image.color = GetItemTypeColor(item);
				}
			}

			Text text = gridObj.GetComponentInChildren<Text>();
			if (text != null)
			{
				// 武器/遗器只显示等级，消耗品只显示数量
				string displayText = "";
				if (item.ItemType == "Weapon" || item.ItemType == "Relic")
				{
					displayText = $"Lv.{item.Level}";
				}
				else
				{
					displayText = $"x{item.Count}";
				}
				text.text = displayText;
				text.color = Color.white;
				text.alignment = TextAnchor.LowerRight;
				text.fontStyle = FontStyle.Bold;
			}

			Button button = gridObj.GetComponent<Button>();
			if (button != null)
			{
				button.onClick.RemoveAllListeners();
				button.onClick.AddListener(() => OnItemClicked(item));
			}
		}

		private string GetItemDisplayName(AccountItemData item)
		{
			if (item.ItemType == "Consumable")
			{
				var consumableConfig = GameDataMgr.Instance.consumableConfigs?.Consumables
					.FirstOrDefault(c => c.ItemId == item.ItemId);
				if (consumableConfig != null)
					return consumableConfig.Name;
			}
			else if (item.ItemType == "Relic")
			{
				var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
					.FirstOrDefault(r => r.RelicId == item.ItemId);
				if (relicConfig != null)
					return relicConfig.Name;
			}
			else if (item.ItemType == "Weapon")
			{
				var weaponConfig = GameDataMgr.Instance.lightConeConfigs.Weapons
					.FirstOrDefault(w => w.WeaponId == item.ItemId);
				if (weaponConfig != null)
					return weaponConfig.Name;
			}
			return item.ItemId;
		}

		private string GetConsumableSubType(AccountItemData item)
		{
			if (item.ItemType != "Consumable") return null;
			var consumableConfig = GameDataMgr.Instance.consumableConfigs?.Consumables
				.FirstOrDefault(c => c.ItemId == item.ItemId);
			return consumableConfig?.ItemType;
		}

		private Color GetItemTypeColor(AccountItemData item)
		{
			switch (item.ItemType)
			{
				case "Character": return Color.blue;
				case "Consumable":
					return GetConsumableSubType(item) == "Currency" ? Color.yellow : Color.green;
				case "Relic": return new Color(0.6f, 0.2f, 0.8f);
				case "Weapon": return new Color(0.8f, 0.6f, 0.2f);
				default: return Color.gray;
			}
		}

		private string GetItemTypeName(AccountItemData item)
		{
			switch (item.ItemType)
			{
				case "Character": return "角色";
				case "Consumable":
					return GetConsumableSubType(item) == "Currency" ? "货币" : "消耗品";
				case "Relic": return "遗器";
				case "Weapon": return "武器";
				default: return "其他";
			}
		}

		private void OnItemClicked(AccountItemData item)
		{
			if (mItemDetailText == null)
				return;

			string itemName = GetItemDisplayName(item);
			string typeName = GetItemTypeName(item);

			string details = $"物品名称: {itemName}\n" +
							$"类型: {typeName}\n";

			if (item.ItemType != "Weapon" && item.ItemType != "Relic")
			{
				details += $"数量: {item.Count}\n";
			}

			if (item.ItemType == "Weapon" || item.ItemType == "Relic")
			{
				details += $"等级: {item.Level}";
			}

			if (item.ItemType == "Relic")
			{
				var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
					.FirstOrDefault(r => r.RelicId == item.ItemId);
				if (relicConfig != null)
				{
					details += $"\n部位: {relicConfig.Slot}\n" +
							   $"稀有度: {relicConfig.Rarity}星\n" +
							   $"主词条: {relicConfig.MainStat.StatType.GetDisplayName()} ({relicConfig.MainStat.MinValue}-{relicConfig.MainStat.MaxValue})";
				}
			}
			else if (item.ItemType == "Weapon")
			{
				var weaponConfig = GameDataMgr.Instance.lightConeConfigs.Weapons
					.FirstOrDefault(w => w.WeaponId == item.ItemId);
				if (weaponConfig != null)
				{
					int maxLevel = weaponConfig.MaxLevel > 1 ? weaponConfig.MaxLevel : 80;
					int maxAttack = weaponConfig.BaseAttackAtMaxLevel > 0 || weaponConfig.BaseAttack == 0
						? weaponConfig.BaseAttackAtMaxLevel : weaponConfig.BaseAttack;
					int maxDefense = weaponConfig.BaseDefenseAtMaxLevel > 0 || weaponConfig.BaseDefense == 0
						? weaponConfig.BaseDefenseAtMaxLevel : weaponConfig.BaseDefense;
					int maxHp = weaponConfig.BaseMaxHpAtMaxLevel > 0 || weaponConfig.BaseMaxHp == 0
						? weaponConfig.BaseMaxHpAtMaxLevel : weaponConfig.BaseMaxHp;

					details += $"\n武器类型: {weaponConfig.WeaponType}\n" +
							   $"稀有度: {weaponConfig.Rarity}星\n" +
							   $"Lv.1 攻击力: {weaponConfig.BaseAttack}\n" +
							   $"Lv.1 防御力: {weaponConfig.BaseDefense}\n" +
							   $"Lv.1 生命值: {weaponConfig.BaseMaxHp}\n" +
							   $"Lv.{maxLevel} 攻击力: {maxAttack}\n" +
							   $"Lv.{maxLevel} 防御力: {maxDefense}\n" +
							   $"Lv.{maxLevel} 生命值: {maxHp}";

					if (weaponConfig.Effects != null && weaponConfig.Effects.Count > 0)
					{
						details += "\n效果:";
						foreach (var effect in weaponConfig.Effects)
						{
							details += $"\n  - {effect.EffectName}: {effect.Description}";
						}
					}
				}
			}
			else if (item.Attributes != null && item.Attributes.Count > 0)
			{
				details += "\n属性:";
				foreach (var attr in item.Attributes)
				{
					details += $"\n  - {attr.Key}: {attr.Value}";
				}
			}

			mItemDetailText.text = details;
		}

		private void ClearItemDetail()
		{
			if (mItemDetailText != null)
			{
				mItemDetailText.text = "请选择物品查看详情";
			}
		}

		private void OnBackButtonClicked()
		{
			LogKit.Log("[InventoryPanel] 返回主菜单");
			UIKit.ClosePanel<InventoryPanel>();
			UIKit.OpenPanel<MainMenuPanel>(UILevel.AlwayBottom);
		}

		private void OnBtnScreenClicked()
		{
			mIsScreenPanelOpen = true;
			UpdateScreenPanelVisibility();
		}

		private void OnBtnCloseScreenClicked()
		{
			mIsScreenPanelOpen = false;
			UpdateScreenPanelVisibility();
		}

		private void UpdateScreenPanelVisibility()
		{
			bool showScreenBtn = (mCurrentCategory == "Weapon" || mCurrentCategory == "Relic");
			if (mBtnScreen != null)
			{
				mBtnScreen.gameObject.SetActive(showScreenBtn);
			}
			if (mPanelScreen != null)
			{
				mPanelScreen.gameObject.SetActive(mIsScreenPanelOpen && showScreenBtn);
				UpdateFilterToggleVisibility();
			}
		}

		private void UpdateFilterToggleVisibility()
		{
			bool isWeapon = mCurrentCategory == "Weapon";
			bool isRelic = mCurrentCategory == "Relic";

			// 星级筛选始终在武器/遗器标签下显示
			SetToggleVisible(mTogStar3, true);
			SetToggleVisible(mTogStar4, true);
			SetToggleVisible(mTogStar5, true);

			// 武器类型筛选仅在武器标签下显示
			SetToggleVisible(mTogWeaponSword, isWeapon);
			SetToggleVisible(mTogWeaponSpear, isWeapon);
			SetToggleVisible(mTogWeaponStaff, isWeapon);
			SetToggleVisible(mTogWeaponGrimoire, isWeapon);
			SetToggleVisible(mTogWeaponBow, isWeapon);
			SetToggleVisible(mTogWeaponWand, isWeapon);

			// 遗器部位筛选仅在遗器标签下显示
			SetToggleVisible(mTogRelicHead, isRelic);
			SetToggleVisible(mTogRelicHands, isRelic);
			SetToggleVisible(mTogRelicBody, isRelic);
			SetToggleVisible(mTogRelicFeet, isRelic);
		}

		private void SetToggleVisible(Toggle toggle, bool visible)
		{
			if (toggle != null)
			{
				toggle.gameObject.SetActive(visible);
			}
		}

		private void RegisterFilterEvents()
		{
			// 星级筛选
			BindFilterToggle(mTogStar3, 3, null, null);
			BindFilterToggle(mTogStar4, 4, null, null);
			BindFilterToggle(mTogStar5, 5, null, null);

			// 武器类型筛选
			BindFilterToggle(mTogWeaponSword, null, (int)WeaponType.Sword, null);
			BindFilterToggle(mTogWeaponSpear, null, (int)WeaponType.Spear, null);
			BindFilterToggle(mTogWeaponStaff, null, (int)WeaponType.Staff, null);
			BindFilterToggle(mTogWeaponGrimoire, null, (int)WeaponType.Grimoire, null);
			BindFilterToggle(mTogWeaponBow, null, (int)WeaponType.Bow, null);
			BindFilterToggle(mTogWeaponWand, null, (int)WeaponType.Wand, null);

			// 遗器部位筛选
			BindFilterToggle(mTogRelicHead, null, null, (int)RelicSlot.Head);
			BindFilterToggle(mTogRelicHands, null, null, (int)RelicSlot.Hands);
			BindFilterToggle(mTogRelicBody, null, null, (int)RelicSlot.Body);
			BindFilterToggle(mTogRelicFeet, null, null, (int)RelicSlot.Feet);
		}

		private void BindFilterToggle(Toggle toggle, int? star, int? weaponType, int? relicSlot)
		{
			if (toggle == null) return;
			toggle.onValueChanged.RemoveAllListeners();
			toggle.isOn = false;
			toggle.onValueChanged.AddListener((bool isOn) =>
			{
				if (isOn)
				{
					if (star.HasValue) mSelectedStars.Add(star.Value);
					if (weaponType.HasValue) mSelectedWeaponTypes.Add(weaponType.Value);
					if (relicSlot.HasValue) mSelectedRelicSlots.Add(relicSlot.Value);
				}
				else
				{
					if (star.HasValue) mSelectedStars.Remove(star.Value);
					if (weaponType.HasValue) mSelectedWeaponTypes.Remove(weaponType.Value);
					if (relicSlot.HasValue) mSelectedRelicSlots.Remove(relicSlot.Value);
				}
				FilterItems();
				RefreshItemGrid();
				ClearItemDetail();
			});
		}
	}
#endregion
}

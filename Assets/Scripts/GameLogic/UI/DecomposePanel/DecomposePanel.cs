using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using System.Collections.Generic;
using System.Linq;

namespace GameUI
{
    /// <summary>
    /// 分解面板数据。
    /// </summary>
    public sealed class DecomposePanelData : IUIData
    {
    }

    /// <summary>
    /// 分解面板：筛选并分解未装备的武器和遗器。
    /// </summary>
    public partial class DecomposePanel : UIPanel
    {
        #region 生命周期与事件

        private List<AccountItemData> mAllItems = new List<AccountItemData>();
        private List<AccountItemData> mFilteredItems = new List<AccountItemData>();
        private string mCurrentCategory = "Weapon";
        private Font mDefaultFont;

        private List<GameObject> mActiveGrids = new List<GameObject>();
        private Stack<GameObject> mPooledGrids = new Stack<GameObject>();

        private HashSet<int> mSelectedStars = new HashSet<int>();
        private HashSet<int> mSelectedWeaponTypes = new HashSet<int>();
        private HashSet<int> mSelectedRelicSlots = new HashSet<int>();
        private bool mIsScreenPanelOpen = false;

        private HashSet<string> mSelectedKeys = new HashSet<string>();

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as DecomposePanelData ?? new DecomposePanelData();
            LoadDefaultFont();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            mData = uiData as DecomposePanelData ?? mData;
            LoadItems();
            mCurrentCategory = "Weapon";
            mSelectedStars.Clear();
            mSelectedWeaponTypes.Clear();
            mSelectedRelicSlots.Clear();
            mSelectedKeys.Clear();
            mIsScreenPanelOpen = false;
            if (mToggleWeapon != null)
                mToggleWeapon.isOn = true;
            RegisterToggleEvents();
            RegisterFilterEvents();
            UpdateScreenPanelVisibility();
            FilterItems();
            RefreshItemGrid();
            UpdateSelectedState();
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

            if (mBtnSelectAll != null)
            {
                mBtnSelectAll.onClick.RemoveAllListeners();
                mBtnSelectAll.onClick.AddListener(OnSelectAllClicked);
            }

            if (mBtnReset != null)
            {
                mBtnReset.onClick.RemoveAllListeners();
                mBtnReset.onClick.AddListener(OnResetClicked);
            }

            if (mBtnDecompose != null)
            {
                mBtnDecompose.onClick.RemoveAllListeners();
                mBtnDecompose.onClick.AddListener(OnDecomposeClicked);
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

        /// <summary>
        /// 隐藏面板时移除按钮监听。
        /// </summary>
        protected override void OnHide()
        {
            if (mBackButton != null)
                mBackButton.onClick.RemoveAllListeners();
            if (mBtnScreen != null)
                mBtnScreen.onClick.RemoveAllListeners();
            if (mBtnCloseScreen != null)
                mBtnCloseScreen.onClick.RemoveAllListeners();
            if (mBtnSelectAll != null)
                mBtnSelectAll.onClick.RemoveAllListeners();
            if (mBtnReset != null)
                mBtnReset.onClick.RemoveAllListeners();
            if (mBtnDecompose != null)
                mBtnDecompose.onClick.RemoveAllListeners();
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
        #endregion

        #region 物品筛选与分解

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

            if (mDefaultFont == null)
            {
                mDefaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
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
                .Where(item => item.ItemType == "Weapon" || item.ItemType == "Relic")
                .OrderByDescending(IsItemEquipped)
                .ThenByDescending(item => item.ItemType == "Weapon"
                    ? weaponConfigs.FirstOrDefault(config => config.WeaponId == item.ItemId)?.Rarity ?? 0
                    : relicConfigs.FirstOrDefault(config => config.RelicId == item.ItemId)?.Rarity ?? 0)
                .ThenByDescending(item => item.Level)
                .ThenByDescending(item => item.ItemType == "Weapon"
                    ? weaponConfigs.FindIndex(config => config.WeaponId == item.ItemId)
                    : relicConfigs.FindIndex(config => config.RelicId == item.ItemId))
                .ThenBy(item => item.InstanceId)
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
            BindToggle(mToggleWeapon, "Weapon");
            BindToggle(mToggleRelic, "Relic");
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
            mSelectedKeys.Clear();
            UpdateScreenPanelVisibility();
            FilterItems();
            RefreshItemGrid();
            UpdateSelectedState();
        }

        private void FilterItems()
        {
            mFilteredItems.Clear();

            IEnumerable<AccountItemData> query = mAllItems;

            query = query.Where(item => item.ItemType == mCurrentCategory);

            if (mSelectedStars.Count > 0)
            {
                query = query.Where(item =>
                    mSelectedStars.Contains(GetItemRarity(item)));
            }

            if (mCurrentCategory == "Weapon" && mSelectedWeaponTypes.Count > 0)
            {
                query = query.Where(item =>
                    mSelectedWeaponTypes.Contains(GetItemWeaponType(item)));
            }

            if (mCurrentCategory == "Relic" && mSelectedRelicSlots.Count > 0)
            {
                query = query.Where(item =>
                    mSelectedRelicSlots.Contains(GetItemRelicSlot(item)));
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

        private string GetGridKey(int allItemsIndex, int instanceIndex)
        {
            return $"{allItemsIndex}_{instanceIndex}";
        }

        private List<AccountItemData> GetSelectedItemsForDecompose()
        {
            var itemCounts = new Dictionary<int, int>();
            foreach (var key in mSelectedKeys)
            {
                var parts = key.Split('_');
                if (parts.Length != 2) continue;
                if (!int.TryParse(parts[0], out int allItemsIndex)) continue;
                if (itemCounts.ContainsKey(allItemsIndex))
                    itemCounts[allItemsIndex]++;
                else
                    itemCounts[allItemsIndex] = 1;
            }

            var result = new List<AccountItemData>();
            foreach (var kvp in itemCounts)
            {
                if (kvp.Key < 0 || kvp.Key >= mAllItems.Count) continue;
                var original = mAllItems[kvp.Key];
                result.Add(new AccountItemData
                {
                    InstanceId = original.InstanceId,
                    ItemType = original.ItemType,
                    ItemId = original.ItemId,
                    Count = kvp.Value,
                    Level = original.Level,
                    Exp = original.Exp,
                    StarLevel = original.StarLevel
                });
            }
            return result;
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
                int allItemsIndex = mAllItems.IndexOf(item);
                int itemCount = item.Count > 0 ? item.Count : 1;
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
                    string gridKey = GetGridKey(allItemsIndex, i);
                    grid.name = $"{item.ItemId}_{i}";
                    UpdateGridContent(grid, item, gridKey);
                    slotIndex++;
                }
            }

            LogKit.Log($"[DecomposePanel] 当前分类: {mCurrentCategory}, 物品数量: {mFilteredItems.Count}, 格子数量: {mActiveGrids.Count}");
        }

        private int CalculateTotalSlots()
        {
            int count = 0;
            foreach (var item in mFilteredItems)
            {
                count += item.Count > 0 ? item.Count : 1;
            }
            return count;
        }

        private GameObject CreateGridObject(Transform parent)
        {
            GameObject gridObj = new GameObject("ItemGrid");
            gridObj.transform.SetParent(parent, false);

            RectTransform rect = gridObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(90, 90);

            Image image = gridObj.AddComponent<Image>();

            Button button = gridObj.AddComponent<Button>();

            GameObject equippedOverlayObj = new GameObject("EquippedOverlay");
            equippedOverlayObj.transform.SetParent(gridObj.transform, false);
            RectTransform equippedOverlayRect = equippedOverlayObj.AddComponent<RectTransform>();
            equippedOverlayRect.anchorMin = Vector2.zero;
            equippedOverlayRect.anchorMax = Vector2.one;
            equippedOverlayRect.offsetMin = Vector2.zero;
            equippedOverlayRect.offsetMax = Vector2.zero;
            Image equippedOverlay = equippedOverlayObj.AddComponent<Image>();
            equippedOverlay.color = new Color(1f, 0.72f, 0.05f, 0.5f);
            equippedOverlay.raycastTarget = false;
            equippedOverlayObj.SetActive(false);

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(gridObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(1, 0);
            textRect.anchorMax = new Vector2(1, 0);
            textRect.pivot = new Vector2(1, 0);
            textRect.sizeDelta = new Vector2(60, 20);
            textRect.anchoredPosition = new Vector2(-5, 5);

            Text text = textObj.AddComponent<Text>();
            if (mDefaultFont != null) text.font = mDefaultFont;
            text.alignment = TextAnchor.LowerRight;
            text.color = Color.white;
            text.fontSize = 17;
            text.fontStyle = FontStyle.Bold;

            GameObject checkObj = new GameObject("Check");
            checkObj.transform.SetParent(gridObj.transform, false);
            RectTransform checkRect = checkObj.AddComponent<RectTransform>();
            checkRect.anchorMin = new Vector2(0, 1);
            checkRect.anchorMax = new Vector2(0, 1);
            checkRect.pivot = new Vector2(0, 1);
            checkRect.sizeDelta = new Vector2(20, 20);
            checkRect.anchoredPosition = new Vector2(5, -5);

            Image checkImage = checkObj.AddComponent<Image>();
            checkImage.color = new Color(0, 0, 0, 0.5f);

            GameObject checkTextObj = new GameObject("CheckText");
            checkTextObj.transform.SetParent(checkObj.transform, false);
            RectTransform checkTextRect = checkTextObj.AddComponent<RectTransform>();
            checkTextRect.anchorMin = new Vector2(0, 0);
            checkTextRect.anchorMax = new Vector2(1, 1);
            checkTextRect.offsetMin = Vector2.zero;
            checkTextRect.offsetMax = Vector2.zero;

            Text checkText = checkTextObj.AddComponent<Text>();
            if (mDefaultFont != null) checkText.font = mDefaultFont;
            checkText.alignment = TextAnchor.MiddleCenter;
            checkText.color = Color.white;
            checkText.fontSize = 14;
            checkText.text = "";

            return gridObj;
        }

        private void UpdateGridContent(GameObject gridObj, AccountItemData item, string gridKey)
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
                text.text = $"Lv.{item.Level}";
                text.color = Color.white;
                text.alignment = TextAnchor.LowerRight;
                text.fontStyle = FontStyle.Bold;
            }

            Transform checkTransform = gridObj.transform.Find("Check");
            Text checkText = checkTransform.Find("CheckText")?.GetComponent<Text>();
            if (checkTransform != null)
            {
                Image checkImage = checkTransform.GetComponent<Image>();
                bool isSelected = mSelectedKeys.Contains(gridKey);
                if (checkImage != null)
                {
                    checkImage.color = isSelected ? new Color(0.2f, 0.8f, 0.2f, 0.8f) : new Color(0, 0, 0, 0.5f);
                }
                if (checkText != null)
                {
                    checkText.text = isSelected ? "✓" : "";
                }
            }

            string accountId = AccountManager.Instance.CurrentAccount.AccountId;
            bool canDecompose = DecomposeSystem.Instance.CanDecompose(accountId, item);
            Transform equippedOverlay = gridObj.transform.Find("EquippedOverlay");
            if (equippedOverlay != null)
            {
                equippedOverlay.gameObject.SetActive(!canDecompose);
            }
            if(!canDecompose)
            {
                checkText.text = "锁";
            }

            Button button = gridObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.interactable = canDecompose;
                if (canDecompose)
                {
                    button.onClick.AddListener(() => OnGridItemClicked(item, gridKey));
                }
            }
        }

        private void OnGridItemClicked(AccountItemData item, string gridKey)
        {
            if (mSelectedKeys.Contains(gridKey))
            {
                mSelectedKeys.Remove(gridKey);
            }
            else
            {
                string accountId = AccountManager.Instance.CurrentAccount.AccountId;
                if (!DecomposeSystem.Instance.CanDecompose(accountId, item))
                {
                    LogKit.Warning($"[DecomposePanel] 物品 {item.ItemId} 已装备，无法分解");
                    return;
                }
                mSelectedKeys.Add(gridKey);
            }

            RefreshItemGrid();
            UpdateSelectedState();
        }

        private void OnSelectAllClicked()
        {
            mSelectedKeys.Clear();
            string accountId = AccountManager.Instance.CurrentAccount.AccountId;
            foreach (var item in mFilteredItems)
            {
                if (DecomposeSystem.Instance.CanDecompose(accountId, item))
                {
                    int allItemsIndex = mAllItems.IndexOf(item);
                    int itemCount = item.Count > 0 ? item.Count : 1;
                    for (int i = 0; i < itemCount; i++)
                    {
                        mSelectedKeys.Add(GetGridKey(allItemsIndex, i));
                    }
                }
            }
            RefreshItemGrid();
            UpdateSelectedState();
        }

        private void OnResetClicked()
        {
            mSelectedKeys.Clear();
            mSelectedStars.Clear();
            mSelectedWeaponTypes.Clear();
            mSelectedRelicSlots.Clear();
            mIsScreenPanelOpen = false;

            if (mTogStar3 != null) mTogStar3.isOn = false;
            if (mTogStar4 != null) mTogStar4.isOn = false;
            if (mTogStar5 != null) mTogStar5.isOn = false;
            if (mTogWeaponSword != null) mTogWeaponSword.isOn = false;
            if (mTogWeaponSpear != null) mTogWeaponSpear.isOn = false;
            if (mTogWeaponStaff != null) mTogWeaponStaff.isOn = false;
            if (mTogWeaponGrimoire != null) mTogWeaponGrimoire.isOn = false;
            if (mTogWeaponBow != null) mTogWeaponBow.isOn = false;
            if (mTogWeaponWand != null) mTogWeaponWand.isOn = false;
            if (mTogRelicHead != null) mTogRelicHead.isOn = false;
            if (mTogRelicHands != null) mTogRelicHands.isOn = false;
            if (mTogRelicBody != null) mTogRelicBody.isOn = false;
            if (mTogRelicFeet != null) mTogRelicFeet.isOn = false;

            UpdateScreenPanelVisibility();
            FilterItems();
            RefreshItemGrid();
            UpdateSelectedState();
        }

        private void OnDecomposeClicked()
        {
            if (mSelectedKeys.Count == 0)
            {
                LogKit.Warning("[DecomposePanel] 未选择任何物品");
                return;
            }

            string accountId = AccountManager.Instance.CurrentAccount.AccountId;
            var itemsToDecompose = GetSelectedItemsForDecompose();

            if (itemsToDecompose.Count == 0)
            {
                LogKit.Warning("[DecomposePanel] 没有可分解的物品");
                return;
            }

            var result = DecomposeSystem.Instance.DecomposeItems(accountId, itemsToDecompose);

            if (result.Success)
            {
                string rewardText = "分解成功！\n获得材料：";
                foreach (var kvp in result.Rewards)
                {
                    var config = GameDataMgr.Instance.consumableConfigs?.Consumables
                        ?.FirstOrDefault(c => c.ItemId == kvp.Key);
                    string name = config?.Name ?? kvp.Key;
                    rewardText += $"\n  {name} x{kvp.Value}";
                }
                if (result.FailedItems.Count > 0)
                {
                    rewardText += $"\n\n以下物品分解失败：{string.Join(", ", result.FailedItems)}";
                }
                LogKit.Log($"[DecomposePanel] {rewardText}");

                LoadItems();
                mSelectedKeys.Clear();
                FilterItems();
                RefreshItemGrid();
                UpdateSelectedState();
            }
            else
            {
                LogKit.Warning("[DecomposePanel] 分解失败");
            }
        }

        private void UpdateSelectedState()
        {
            int totalCount = mSelectedKeys.Count;

            if (mSelectedCountText != null)
            {
                mSelectedCountText.text = $"已选择: {totalCount} 件";
            }

            if (totalCount > 0)
            {
                if (mRewardPreviewText != null)
                {
                    mRewardPreviewText.text = "奖励预览如下";
                }

                var selectedItems = GetSelectedItemsForDecompose();
                var rewards = DecomposeSystem.Instance.CalculateBatchReward(selectedItems);
                UpdateRewardSlots(rewards);
            }
            else
            {
                if (mRewardPreviewText != null)
                {
                    mRewardPreviewText.text = "当前没有选中要分解的物品";
                }

                UpdateRewardSlots(new Dictionary<string, int>());
            }
        }

        private void UpdateRewardSlots(Dictionary<string, int> rewards)
        {
            GameObject[] slots = { mRewardSlot1, mRewardSlot2, mRewardSlot3, mRewardSlot4 };

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                slots[i].SetActive(false);
            }

            if (rewards == null || rewards.Count == 0)
                return;

            int index = 0;
            foreach (var kvp in rewards)
            {
                if (index >= slots.Length) break;

                var slot = slots[index];
                if (slot == null) continue;

                slot.SetActive(true);

                Image slotImage = slot.GetComponent<Image>();
                if (slotImage != null)
                {
                    SpriteIconLoader.SetConsumableIcon(slotImage, kvp.Key);
                    if (slotImage.sprite == null)
                    {
                        var config = GameDataMgr.Instance.consumableConfigs?.Consumables
                            ?.FirstOrDefault(c => c.ItemId == kvp.Key);
                        if (config != null && !string.IsNullOrEmpty(config.IconPath))
                        {
                            // 使用 SpriteIconLoader 统一加载（支持 YooAsset 热更新）
                            var loadedSprite = SpriteIconLoader.LoadIcon(config.ItemType, config.ItemId);
                            if (loadedSprite != null)
                                slotImage.sprite = loadedSprite;
                        }

                        if (slotImage.sprite == null)
                        {
                            slotImage.color = GetRewardColor(kvp.Key);
                        }
                        else
                        {
                            slotImage.color = Color.white;
                        }
                    }
                    else
                    {
                        slotImage.color = Color.white;
                    }
                }

                Text countText = slot.GetComponentInChildren<Text>(true);
                if (countText != null)
                {
                    countText.text = $"x{kvp.Value}";
                }

                index++;
            }
        }

        private Color GetRewardColor(string itemId)
        {
            switch (itemId)
            {
                case "UpgradeStone": return new Color(0.7f, 0.7f, 0.9f);
                case "RefiningAgent": return new Color(0.9f, 0.7f, 0.5f);
                case "EnchantDust": return new Color(0.8f, 0.6f, 0.8f);
                case "MagicCrystal": return new Color(0.5f, 0.8f, 1f);
                case "IronOre": return new Color(0.6f, 0.6f, 0.6f);
                case "StarDust": return new Color(1f, 0.8f, 0.4f);
                case "SkillGem": return new Color(0.4f, 0.9f, 0.6f);
                case "Diamond": return new Color(0.4f, 0.8f, 1f);
                case "RelicShard": return new Color(0.7f, 0.5f, 0.9f);
                default: return new Color(0.5f, 0.5f, 0.5f);
            }
        }

        private void OnBackButtonClicked()
        {
            UIKit.ClosePanel<DecomposePanel>();
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
            if (mBtnScreen != null)
            {
                mBtnScreen.gameObject.SetActive(true);
            }
            if (mPanelScreen != null)
            {
                mPanelScreen.gameObject.SetActive(mIsScreenPanelOpen);
                UpdateFilterToggleVisibility();
            }
        }

        private void UpdateFilterToggleVisibility()
        {
            bool isWeapon = mCurrentCategory == "Weapon";
            bool isRelic = mCurrentCategory == "Relic";

            SetToggleVisible(mTogStar3, true);
            SetToggleVisible(mTogStar4, true);
            SetToggleVisible(mTogStar5, true);

            SetToggleVisible(mTogWeaponSword, isWeapon);
            SetToggleVisible(mTogWeaponSpear, isWeapon);
            SetToggleVisible(mTogWeaponStaff, isWeapon);
            SetToggleVisible(mTogWeaponGrimoire, isWeapon);
            SetToggleVisible(mTogWeaponBow, isWeapon);
            SetToggleVisible(mTogWeaponWand, isWeapon);

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
            BindFilterToggle(mTogStar3, 3, null, null);
            BindFilterToggle(mTogStar4, 4, null, null);
            BindFilterToggle(mTogStar5, 5, null, null);

            BindFilterToggle(mTogWeaponSword, null, (int)WeaponType.Sword, null);
            BindFilterToggle(mTogWeaponSpear, null, (int)WeaponType.Spear, null);
            BindFilterToggle(mTogWeaponStaff, null, (int)WeaponType.Staff, null);
            BindFilterToggle(mTogWeaponGrimoire, null, (int)WeaponType.Grimoire, null);
            BindFilterToggle(mTogWeaponBow, null, (int)WeaponType.Bow, null);
            BindFilterToggle(mTogWeaponWand, null, (int)WeaponType.Wand, null);

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
                UpdateSelectedState();
            });
        }

        private Color GetItemTypeColor(AccountItemData item)
        {
            switch (item.ItemType)
            {
                case "Relic": return new Color(0.6f, 0.2f, 0.8f);
                case "Weapon": return new Color(0.8f, 0.6f, 0.2f);
                default: return Color.gray;
            }
        }
#endregion
    }
}

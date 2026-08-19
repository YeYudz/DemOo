using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using System.Collections.Generic;
using System.Linq;

namespace GameUI
{
    /// <summary>
    /// 商店面板的数据容器。
    /// </summary>
    public sealed class StorePanelData : IUIData
    {
    }

    /// <summary>
    /// 商店面板，负责商品分类展示和购买交互。
    /// </summary>
    public partial class StorePanel : UIPanel
    {
        #region 状态数据

        private List<StoreItemConfig> mCurrentItems = new List<StoreItemConfig>();
        private string mCurrentCategory = "Weapon";
        private Font mDefaultFont;
        private List<GameObject> mActiveGrids = new List<GameObject>();
        private Stack<GameObject> mPooledGrids = new Stack<GameObject>();

        private StoreItemConfig mPendingPurchaseItem;
        private int mMaxBuyCount;

        #endregion

        #region 生命周期

        /// <summary>
        /// 初始化商店数据和默认字体。
        /// </summary>
        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as StorePanelData ?? new StorePanelData();
            LoadDefaultFont();
        }

        /// <summary>
        /// 打开面板并刷新默认分类、货币和商品。
        /// </summary>
        protected override void OnOpen(IUIData uiData = null)
        {
            mData = uiData as StorePanelData ?? mData;
            mCurrentCategory = "Weapon";
            UpdateCurrencyDisplay();
            LoadItems();
            RefreshItemGrid();
            CloseSurePanel();
        }

        /// <summary>
        /// 显示面板并绑定控件事件。
        /// </summary>
        protected override void OnShow()
        {
            if (mBtnClose != null)
            {
                mBtnClose.onClick.RemoveAllListeners();
                mBtnClose.onClick.AddListener(OnCloseButtonClicked);
            }

            BindToggle(mTogWeapon, "Weapon");
            BindToggle(mTogRelic, "Relic");
            BindToggle(mTogOther, "Other");

            if (mBtnBuy != null)
            {
                mBtnBuy.onClick.RemoveAllListeners();
                mBtnBuy.onClick.AddListener(OnBuyButtonClicked);
            }
            if (mBtnCancel != null)
            {
                mBtnCancel.onClick.RemoveAllListeners();
                mBtnCancel.onClick.AddListener(OnCancelButtonClicked);
            }
            if (mBtnConfirm != null)
            {
                mBtnConfirm.onClick.RemoveAllListeners();
                mBtnConfirm.onClick.AddListener(OnConfirmButtonClicked);
            }
            if (mSlider != null)
            {
                mSlider.onValueChanged.RemoveAllListeners();
                mSlider.onValueChanged.AddListener(OnSliderValueChanged);
            }

            RefreshItemGrid();
        }

        /// <summary>
        /// 隐藏面板并解除控件事件绑定。
        /// </summary>
        protected override void OnHide()
        {
            if (mBtnClose != null)
                mBtnClose.onClick.RemoveAllListeners();
            if (mTogWeapon != null)
                mTogWeapon.onValueChanged.RemoveAllListeners();
            if (mTogRelic != null)
                mTogRelic.onValueChanged.RemoveAllListeners();
            if (mTogOther != null)
                mTogOther.onValueChanged.RemoveAllListeners();
            if (mBtnBuy != null)
                mBtnBuy.onClick.RemoveAllListeners();
            if (mBtnCancel != null)
                mBtnCancel.onClick.RemoveAllListeners();
            if (mBtnConfirm != null)
                mBtnConfirm.onClick.RemoveAllListeners();
            if (mSlider != null)
                mSlider.onValueChanged.RemoveAllListeners();
        }

        /// <summary>
        /// 关闭面板并清理活动及缓存的商品网格。
        /// </summary>
        protected override void OnClose()
        {
            foreach (var grid in mActiveGrids)
                Object.Destroy(grid);
            mActiveGrids.Clear();
            foreach (var grid in mPooledGrids)
                Object.Destroy(grid);
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
                if (fonts.Length > 0) mDefaultFont = fonts[0];
            }
            if (mDefaultFont == null)
                mDefaultFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        /// <summary>
        /// 将分类切换控件绑定到指定商品分类。
        /// </summary>
        private void BindToggle(Toggle toggle, string category)
        {
            if (toggle == null) return;
            toggle.onValueChanged.RemoveAllListeners();
            toggle.onValueChanged.AddListener((bool isOn) =>
            {
                if (isOn)
                {
                    mCurrentCategory = category;
                    LoadItems();
                    RefreshItemGrid();
                    CloseSurePanel();
                }
            });
        }

        /// <summary>
        /// 响应关闭按钮并关闭商店面板。
        /// </summary>
        private void OnCloseButtonClicked()
        {
            UIKit.ClosePanel<StorePanel>();
        }

        /// <summary>
        /// 按当前分类加载商品数据。
        /// </summary>
        private void LoadItems()
        {
            mCurrentItems = StoreSystem.Instance.GetItemsByCategory(mCurrentCategory);
        }

        /// <summary>
        /// 刷新金币、钻石数量及对应图标。
        /// </summary>
        private void UpdateCurrencyDisplay()
        {
            if (!AccountManager.Instance.IsLoggedIn) return;
            string accountId = AccountManager.Instance.CurrentAccount.AccountId;
            int gold = AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", "Gold");
            int diamond = AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", "Diamond");
            if (mGoldCount != null) mGoldCount.text = gold.ToString();
            if (mDiamondCount != null) mDiamondCount.text = diamond.ToString();

            if (mImgGold != null && mImgGold.sprite == null)
            {
                Sprite goldSprite = SpriteIconLoader.LoadConsumableIcon("Gold");
                if (goldSprite != null) mImgGold.sprite = goldSprite;
            }
            if (mImgDiamond != null && mImgDiamond.sprite == null)
            {
                Sprite diamondSprite = SpriteIconLoader.LoadConsumableIcon("Diamond");
                if (diamondSprite != null) mImgDiamond.sprite = diamondSprite;
            }
        }

        #endregion

        #region 商品网格

        /// <summary>
        /// 刷新商品网格并复用已有网格对象。
        /// </summary>
        private void RefreshItemGrid()
        {
            if (mListItem == null) return;

            Transform content = mListItem.Find("Viewport/Content");
            if (content == null) content = mListItem;

            GridLayoutGroup layout = content.GetComponent<GridLayoutGroup>();
            if (layout == null) layout = content.gameObject.AddComponent<GridLayoutGroup>();

            RectTransform contentRt = content.GetComponent<RectTransform>();
            float containerWidth = contentRt.rect.width;
            if (containerWidth <= 0) containerWidth = 800f;

            int maxColumns = 4;
            float spacing = 5f;

            float totalSpacing = spacing * (maxColumns + 1);
            float cellWidth = (containerWidth - totalSpacing) / maxColumns;
            float cellHeight = cellWidth * 1.2f;

            layout.cellSize = new Vector2(cellWidth, cellHeight);
            layout.spacing = new Vector2(spacing, spacing);
            layout.padding = new RectOffset((int)spacing, (int)spacing, (int)spacing, (int)spacing);
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = maxColumns;

            ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
            if (fitter == null) fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            int totalSlots = mCurrentItems.Count;

            if (mActiveGrids.Count > totalSlots)
            {
                int excess = mActiveGrids.Count - totalSlots;
                for (int i = mActiveGrids.Count - 1; i >= totalSlots; i--)
                {
                    GameObject grid = mActiveGrids[i];
                    grid.SetActive(false);
                    mPooledGrids.Push(grid);
                }
                mActiveGrids.RemoveRange(totalSlots, excess);
            }

            for (int i = 0; i < mCurrentItems.Count; i++)
            {
                GameObject grid;
                if (i < mActiveGrids.Count)
                {
                    grid = mActiveGrids[i];
                }
                else
                {
                    if (mPooledGrids.Count > 0)
                    {
                        // 优先取出缓存网格，减少运行时对象分配。
                        grid = mPooledGrids.Pop();
                        grid.transform.SetParent(content, false);
                    }
                    else
                    {
                        grid = CreateStoreGridObject(content);
                    }
                    mActiveGrids.Add(grid);
                }
                grid.SetActive(true);
                UpdateGridContent(grid, mCurrentItems[i]);
            }
        }

        private GameObject CreateStoreGridObject(Transform parent)
        {
            GameObject gridObj = new GameObject("StoreItem");
            gridObj.transform.SetParent(parent, false);

            RectTransform rect = gridObj.AddComponent<RectTransform>();

            Image bgImage = gridObj.AddComponent<Image>();
            ColorBlock colors = new ColorBlock();
            colors.normalColor = new Color(0.95f, 0.95f, 0.95f);
            colors.highlightedColor = new Color(1f, 1f, 0.9f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.selectedColor = new Color(1f, 1f, 0.9f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.1f;

            Button button = gridObj.AddComponent<Button>();
            button.colors = colors;
            button.targetGraphic = bgImage;

            GameObject priceObj = new GameObject("PriceSection");
            priceObj.transform.SetParent(gridObj.transform, false);
            RectTransform priceRect = priceObj.AddComponent<RectTransform>();
            priceRect.anchorMin = new Vector2(0, 0);
            priceRect.anchorMax = new Vector2(1, 1f / 6f);
            priceRect.offsetMin = Vector2.zero;
            priceRect.offsetMax = Vector2.zero;

            Image priceBg = priceObj.AddComponent<Image>();
            priceBg.color = new Color(0.2f, 0.2f, 0.2f, 0.9f);

            GameObject priceTextObj = new GameObject("PriceText");
            priceTextObj.transform.SetParent(priceObj.transform, false);
            RectTransform priceTextRect = priceTextObj.AddComponent<RectTransform>();
            priceTextRect.anchorMin = Vector2.zero;
            priceTextRect.anchorMax = Vector2.one;
            priceTextRect.offsetMin = Vector2.zero;
            priceTextRect.offsetMax = Vector2.zero;

            Text priceText = priceTextObj.AddComponent<Text>();
            priceText.font = mDefaultFont;
            priceText.alignment = TextAnchor.MiddleCenter;
            priceText.color = Color.white;
            priceText.fontSize = 18;
            priceText.fontStyle = FontStyle.Bold;

            GameObject iconObj = new GameObject("IconSection");
            iconObj.transform.SetParent(gridObj.transform, false);
            RectTransform iconRect = iconObj.AddComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0, 1f / 6f);
            iconRect.anchorMax = new Vector2(1, 1);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Image iconImage = iconObj.AddComponent<Image>();
            iconImage.preserveAspect = true;

            GameObject qtyLabelObj = new GameObject("QuantityLabel");
            qtyLabelObj.transform.SetParent(gridObj.transform, false);
            RectTransform qtyRect = qtyLabelObj.AddComponent<RectTransform>();
            qtyRect.anchorMin = new Vector2(0.65f, 0);
            qtyRect.anchorMax = new Vector2(1, 0.15f);
            qtyRect.offsetMin = Vector2.zero;
            qtyRect.offsetMax = Vector2.zero;

            Text qtyText = qtyLabelObj.AddComponent<Text>();
            qtyText.font = mDefaultFont;
            qtyText.alignment = TextAnchor.MiddleRight;
            qtyText.color = new Color(1f, 0.6f, 0f);
            qtyText.fontSize = 14;
            qtyText.fontStyle = FontStyle.Bold;
            qtyText.horizontalOverflow = HorizontalWrapMode.Overflow;
            qtyLabelObj.SetActive(false);

            return gridObj;
        }

        private void UpdateGridContent(GameObject gridObj, StoreItemConfig config)
        {
            gridObj.name = config.ItemId;

            bool canPurchase = StoreSystem.Instance.CanPurchase(config);

            Image bgImage = gridObj.GetComponent<Image>();
            if (bgImage != null)
            {
                bgImage.color = canPurchase
                    ? new Color(0.95f, 0.95f, 0.95f)
                    : new Color(0.7f, 0.7f, 0.7f);
            }

            Transform priceSection = gridObj.transform.Find("PriceSection");
            if (priceSection != null)
            {
                Text priceText = priceSection.GetComponentInChildren<Text>();
                if (priceText != null)
                {
                    string currencyName = config.Currency == "Diamond" ? "钻石" : "金币";
                    int remaining = StoreSystem.Instance.GetRemainingStock(config);
                    string stockText = remaining > 0 ? $"(剩{remaining})" : (remaining == -1 ? "" : "(售罄)");

                    priceText.text = $"{config.Price} {currencyName} {stockText}";
                    priceText.color = canPurchase
                        ? Color.white
                        : new Color(0.8f, 0.8f, 0.8f);
                }
            }

            Transform qtyLabel = gridObj.transform.Find("QuantityLabel");
            if (qtyLabel != null)
            {
                int qty = config.Quantity > 0 ? config.Quantity : 1;
                if (qty > 1)
                {
                    qtyLabel.gameObject.SetActive(true);
                    Text qtyText = qtyLabel.GetComponent<Text>();
                    if (qtyText != null)
                    {
                        qtyText.text = $"x{qty}";
                        qtyText.color = canPurchase ? new Color(1f, 0.6f, 0f) : new Color(0.8f, 0.8f, 0.8f);
                    }
                }
                else
                {
                    qtyLabel.gameObject.SetActive(false);
                }
            }

            Transform iconSection = gridObj.transform.Find("IconSection");
            if (iconSection != null)
            {
                Image iconImage = iconSection.GetComponent<Image>();
                if (iconImage != null)
                {
                    if (!string.IsNullOrEmpty(config.IconPath))
                    {
                        // 使用 SpriteIconLoader 统一加载（支持 YooAsset 热更新）
                        Sprite sprite = SpriteIconLoader.LoadIcon(config.ItemType, config.ItemId);
                        if (sprite != null)
                        {
                            iconImage.sprite = sprite;
                            iconImage.color = Color.white;
                        }
                        else
                        {
                            SetItemIcon(iconImage, config);
                        }
                    }
                    else
                    {
                        SetItemIcon(iconImage, config);
                    }
                }
            }

            Button button = gridObj.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => OnStoreItemClicked(config));
            }
        }

        private void SetItemIcon(Image iconImage, StoreItemConfig config)
        {
            if (config.ItemType == "Weapon")
            {
                SpriteIconLoader.SetIcon(iconImage, "Weapon", config.ItemId);
            }
            else if (config.ItemType == "Relic")
            {
                SpriteIconLoader.SetIcon(iconImage, "Relic", config.ItemId);
            }
            else if (config.ItemType == "Consumable")
            {
                SpriteIconLoader.SetIcon(iconImage, "Consumable", config.ItemId);
            }

            if (iconImage.sprite == null)
            {
                iconImage.color = GetCategoryColor(config.Category);
            }
            else
            {
                iconImage.color = Color.white;
            }
        }

        /// <summary>
        /// 获取商品分类对应的占位颜色。
        /// </summary>
        private Color GetCategoryColor(string category)
        {
            switch (category)
            {
                case "Weapon": return new Color(0.8f, 0.6f, 0.2f);
                case "Relic": return new Color(0.6f, 0.2f, 0.8f);
                case "Other": return new Color(0.3f, 0.6f, 0.9f);
                default: return Color.gray;
            }
        }

        #endregion

        #region 购买确认

        /// <summary>
        /// 检查商品是否可购买并打开购买确认面板。
        /// </summary>
        private void OnStoreItemClicked(StoreItemConfig config)
        {
            if (!StoreSystem.Instance.CanPurchase(config))
            {
                int remaining = StoreSystem.Instance.GetRemainingStock(config);
                if (remaining == 0)
                    LogKit.Warning($"[StorePanel] {config.ItemId} 已售罄");
                else
                    LogKit.Warning($"[StorePanel] 货币不足，需要 {config.Price} {config.Currency}");
                return;
            }

            mPendingPurchaseItem = config;
            mMaxBuyCount = StoreSystem.Instance.GetMaxPurchaseCount(config);

            if (mMaxBuyCount <= 0)
            {
                LogKit.Warning($"[StorePanel] 无法购买 {config.ItemId}");
                return;
            }

            OpenSurePanel();
        }

        /// <summary>
        /// 打开购买确认区域并初始化购买数量控件。
        /// </summary>
        private void OpenSurePanel()
        {
            if (mSurePanel == null || mPendingPurchaseItem == null) return;

            mSurePanel.SetActive(true);

            bool showSlider = mMaxBuyCount > 1;
            bool showTextSure = !showSlider;

            if (mTextSure != null)
                mTextSure.gameObject.SetActive(showTextSure);
            if (mSlider != null)
                mSlider.gameObject.SetActive(showSlider);
            if (mTextCount != null)
                mTextCount.gameObject.SetActive(showSlider);

            if (mTextResult != null)
                mTextResult.gameObject.SetActive(false);

            if (mBtnBuy != null)
                mBtnBuy.gameObject.SetActive(true);
            if (mBtnCancel != null)
                mBtnCancel.gameObject.SetActive(true);
            if (mBtnConfirm != null)
                mBtnConfirm.gameObject.SetActive(false);

            if (showTextSure && mTextSure != null)
            {
                mTextSure.text = "是否购买该商品？";
            }

            if (showSlider)
            {
                if (mSlider != null)
                {
                    mSlider.minValue = 1;
                    mSlider.maxValue = mMaxBuyCount;
                    mSlider.wholeNumbers = true;
                    mSlider.value = 1;
                }
                UpdateSliderText();
            }
        }

        /// <summary>
        /// 更新购买次数、获得数量和总价文本。
        /// </summary>
        private void UpdateSliderText()
        {
            if (mSlider == null || mTextCount == null || mPendingPurchaseItem == null) return;

            int buyCount = Mathf.RoundToInt(mSlider.value);
            int qty = mPendingPurchaseItem.Quantity > 0 ? mPendingPurchaseItem.Quantity : 1;
            int totalItems = qty * buyCount;
            int totalPrice = mPendingPurchaseItem.Price * buyCount;

            mTextCount.text = $"购买次数: {buyCount}/{mMaxBuyCount}\n获得: x{totalItems}\n总价: {totalPrice} {mPendingPurchaseItem.Currency}";
        }

        private void OnSliderValueChanged(float value)
        {
            UpdateSliderText();
        }

        private void OnBuyButtonClicked()
        {
            if (mPendingPurchaseItem == null) return;

            int buyCount = 1;
            if (mSlider != null && mSlider.gameObject.activeSelf)
            {
                buyCount = Mathf.RoundToInt(mSlider.value);
            }

            var result = StoreSystem.Instance.PurchaseItem(mPendingPurchaseItem, buyCount);

            if (mTextSure != null)
                mTextSure.gameObject.SetActive(false);
            if (mSlider != null)
                mSlider.gameObject.SetActive(false);
            if (mTextCount != null)
                mTextCount.gameObject.SetActive(false);

            if (mBtnBuy != null)
                mBtnBuy.gameObject.SetActive(false);
            if (mBtnCancel != null)
                mBtnCancel.gameObject.SetActive(false);
            if (mBtnConfirm != null)
                mBtnConfirm.gameObject.SetActive(true);

            if (mTextResult != null)
            {
                mTextResult.gameObject.SetActive(true);
                mTextResult.text = result.Success ? "购买成功" : "购买失败";
                mTextResult.color = result.Success ? Color.white : Color.red;
            }

            if (result.Success)
            {
                LogKit.Log($"[StorePanel] {result.Message}");
            }
            else
            {
                LogKit.Warning($"[StorePanel] 购买失败: {result.Message}");
            }

            UpdateCurrencyDisplay();
            RefreshItemGrid();
        }

        private void OnCancelButtonClicked()
        {
            CloseSurePanel();
        }

        private void OnConfirmButtonClicked()
        {
            CloseSurePanel();
        }

        private void CloseSurePanel()
        {
            if (mSurePanel != null)
                mSurePanel.SetActive(false);
            mPendingPurchaseItem = null;
        }

        #endregion
    }
}

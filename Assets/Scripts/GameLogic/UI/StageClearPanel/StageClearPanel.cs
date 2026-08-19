using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YokiFrame;

namespace GameUI
{
    public sealed class StageClearPanelData : IUIData
    {
        // 是否通关
        public bool IsSuccess;
        // 总经验值
        public int TotalExp;
        // 奖励物品列表（仅通关时有值）
        public List<AccountItemData> RewardItems;
    }

    /// <summary>
    /// 副本结算面板：显示通关结果和奖励。
    /// </summary>
    public partial class StageClearPanel : UIPanel
    {
        #region 生命周期与奖励展示
        private StageClearPanelData mData;
        private List<GameObject> mActiveRewardGrids = new List<GameObject>();
        private Stack<GameObject> mPooledGrids = new Stack<GameObject>();
        private const int MAX_COLUMNS = 6;
        private const float CELL_SPACING = 5f;

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as StageClearPanelData ?? new StageClearPanelData();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            mData = uiData as StageClearPanelData ?? mData;
            EnsureGridLayout();
            RefreshUI();
        }

        protected override void OnShow()
        {
            if (mBtnClose != null)
            {
                mBtnClose.onClick.RemoveAllListeners();
                mBtnClose.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        protected override void OnHide()
        {
            if (mBtnClose != null)
                mBtnClose.onClick.RemoveAllListeners();
            ClearRewardGrids();
        }

        protected override void OnClose()
        {
        }

        private void EnsureGridLayout()
        {
            if (mListContent == null)
                return;

            GridLayoutGroup layout = mListContent.GetComponent<GridLayoutGroup>();
            if (layout == null)
                layout = mListContent.gameObject.AddComponent<GridLayoutGroup>();

            RectTransform contentRt = mListContent as RectTransform;
            float containerWidth = contentRt.rect.width;
            if (containerWidth <= 0)
                containerWidth = 600f;

            // 公式: cellWidth * 6 + CELL_SPACING * 7 = containerWidth
            float cellWidth = (containerWidth - CELL_SPACING * (MAX_COLUMNS + 1)) / MAX_COLUMNS;
            float cellHeight = cellWidth;

            layout.cellSize = new Vector2(cellWidth, cellHeight);
            layout.spacing = new Vector2(CELL_SPACING, CELL_SPACING);
            layout.padding = new RectOffset((int)CELL_SPACING, (int)CELL_SPACING, (int)CELL_SPACING, (int)CELL_SPACING);
            layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
            layout.startAxis = GridLayoutGroup.Axis.Horizontal;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = MAX_COLUMNS;

            ContentSizeFitter fitter = mListContent.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = mListContent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        /// <summary>
        /// 根据结算数据刷新 UI。
        /// </summary>
        private void RefreshUI()
        {
            ClearRewardGrids();

            if (mData == null)
                return;

            if (mData.IsSuccess)
            {
                if (mTexTips != null)
                {
                    mTexTips.text = "通关成功";
                    mTexTips.color = Color.white;
                }
                if (mTexSuggestion != null)
                    mTexSuggestion.gameObject.SetActive(false);
                if (mListContent != null)
                    mListContent.gameObject.SetActive(true);

                GenerateRewardItems();
            }
            else
            {
                if (mTexTips != null)
                {
                    mTexTips.text = "通关失败";
                    mTexTips.color = Color.white;
                }
                if (mTexSuggestion != null)
                {
                    mTexSuggestion.text = "请养成角色后再试";
                    mTexSuggestion.color = Color.red;
                    mTexSuggestion.gameObject.SetActive(true);
                }
                if (mListContent != null)
                    mListContent.gameObject.SetActive(false);
            }
        }

        private void GenerateRewardItems()
        {
            var allItems = new List<AccountItemData>();

            // 先添加经验奖励
            if (mData.TotalExp > 0)
            {
                allItems.Add(new AccountItemData("Exp", "Exp", mData.TotalExp));
            }

            // 再添加其他奖励物品
            if (mData.RewardItems != null)
            {
                allItems.AddRange(mData.RewardItems);
            }

            foreach (var item in allItems)
            {
                GameObject grid = GetOrCreateGrid();
                UpdateGridContent(grid, item);
                mActiveRewardGrids.Add(grid);
            }
        }

        private GameObject GetOrCreateGrid()
        {
            GameObject gridObj;
            if (mPooledGrids.Count > 0)
            {
                gridObj = mPooledGrids.Pop();
            }
            else
            {
                gridObj = CreateGridObject(mListContent.transform);
            }
            gridObj.SetActive(true);
            return gridObj;
        }

        private void ClearRewardGrids()
        {
            foreach (var grid in mActiveRewardGrids)
            {
                if (grid != null)
                {
                    grid.SetActive(false);
                    mPooledGrids.Push(grid);
                }
            }
            mActiveRewardGrids.Clear();
        }

        private GameObject CreateGridObject(Transform parent)
        {
            GameObject gridObj = new GameObject("RewardGrid");
            gridObj.transform.SetParent(parent, false);

            RectTransform rect = gridObj.AddComponent<RectTransform>();
            rect.sizeDelta = new Vector2(90, 90);

            Image image = gridObj.AddComponent<Image>();
            Button button = gridObj.AddComponent<Button>();

            // 数量文本：右上角，金黄色加粗
            GameObject textObj = new GameObject("CountText");
            textObj.transform.SetParent(gridObj.transform, false);
            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(1, 1);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.pivot = new Vector2(1, 1);
            textRect.sizeDelta = new Vector2(60, 20);
            textRect.anchoredPosition = new Vector2(-3, -3);

            Text text = textObj.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.UpperRight;
            text.color = new Color(1f, 0.84f, 0f);
            text.fontSize = 14;
            text.fontStyle = FontStyle.Bold;

            return gridObj;
        }

        private void UpdateGridContent(GameObject gridObj, AccountItemData item)
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
            }
        }

        private Color GetItemTypeColor(AccountItemData item)
        {
            if (item == null || string.IsNullOrEmpty(item.ItemType))
                return Color.gray;

            switch (item.ItemType)
            {
                case "Weapon":
                    return new Color(1f, 0.75f, 0f);
                case "Relic":
                    return new Color(0.75f, 0f, 1f);
                case "Exp":
                    return new Color(1f, 0.84f, 0f);
                default:
                    return Color.gray;
            }
        }

        /// <summary>
        /// 关闭结算面板并返回主菜单。
        /// </summary>
        private void OnCloseButtonClicked()
        {
            UIKit.ClosePanel<StageClearPanel>();
            UIKit.OpenPanel<MainMenuPanel>(UILevel.AlwayBottom);
        }
        #endregion
    }
}

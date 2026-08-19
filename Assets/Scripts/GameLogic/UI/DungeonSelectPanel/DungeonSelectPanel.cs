using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using System.Collections.Generic;
using System.Linq;

namespace GameUI
{
    public sealed class DungeonSelectPanelData : IUIData
    {
    }

    /// <summary>
    /// 副本选择面板：浏览已解锁副本并查看奖励。
    /// </summary>
    public partial class DungeonSelectPanel : UIPanel
    {
        #region 生命周期与副本选择
        private List<DungeonConfig> mDungeons = new List<DungeonConfig>();
        private Font mDefaultFont;
        private string mSelectedDungeonId = "";

        protected override void OnInit(IUIData uiData = null)
        {
            mData = uiData as DungeonSelectPanelData ?? new DungeonSelectPanelData();
            LoadDefaultFont();
        }

        protected override void OnOpen(IUIData uiData = null)
        {
            mData = uiData as DungeonSelectPanelData ?? mData;
            
             mSelectedDungeonId = "";
            LoadDungeons();
            RefreshUI();
        }

        protected override void OnShow()
        {
            if (mBackButton != null)
            {
                mBackButton.onClick.RemoveAllListeners();
                mBackButton.onClick.AddListener(OnBackButtonClicked);
            }
        }

        protected override void OnHide()
        {
            if (mBackButton != null)
                mBackButton.onClick.RemoveAllListeners();
        }

        protected override void OnClose()
        {
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

        private void LoadDungeons()
        {
            mDungeons.Clear();
            if (GameDataMgr.Instance.dungeonConfigs != null)
            {
                mDungeons.AddRange(GameDataMgr.Instance.dungeonConfigs.Dungeons);
            }
        }

        private void RefreshUI()
        {
            if (mDungeons.Count == 0)
            {
                if (mStatusText != null)
                {
                    mStatusText.text = "暂无副本数据";
                }
                return;
            }

            if (mStatusText != null)
            {
                mStatusText.text = "";
            }

            ClearDungeonList();
            SetupGridLayout();

            foreach (var dungeon in mDungeons)
            {
                CreateDungeonButton(dungeon);
            }

            if (!string.IsNullOrEmpty(mSelectedDungeonId))
            {
                UpdateSelectedDungeonInfo();
            }
        }

        private void SetupGridLayout()
        {
            if (mDungeonListPanel == null)
                return;

            GridLayoutGroup gridLayout = mDungeonListPanel.GetComponent<GridLayoutGroup>();
            if (gridLayout == null)
            {
                gridLayout = mDungeonListPanel.gameObject.AddComponent<GridLayoutGroup>();
            }

            gridLayout.cellSize = new Vector2(130, 130);
            gridLayout.spacing = new Vector2(10, 10);
            gridLayout.padding = new RectOffset(5, 0, 30, 30);
            gridLayout.childAlignment = TextAnchor.UpperLeft;
            gridLayout.constraint = GridLayoutGroup.Constraint.Flexible;
        }

        private void ClearDungeonList()
        {
            if (mDungeonListPanel != null)
            {
                for (int i = mDungeonListPanel.childCount - 1; i >= 0; i--)
                {
                    Destroy(mDungeonListPanel.GetChild(i).gameObject);
                }
            }
        }

        private void CreateDungeonButton(DungeonConfig dungeon)
        {
            if (mDungeonListPanel == null)
                return;

            bool isUnlocked = DungeonManager.Instance.IsDungeonUnlocked(dungeon.DungeonId);

            GameObject container = new GameObject($"DungeonBtn_{dungeon.DungeonId}");
            container.transform.SetParent(mDungeonListPanel, false);
            container.AddComponent<RectTransform>();

            RectTransform containerRt = container.GetComponent<RectTransform>();
            containerRt.sizeDelta = new Vector2(130, 130);

            Image bgImage = container.AddComponent<Image>();
            bgImage.color = dungeon.DungeonId == mSelectedDungeonId ? new Color(0.7f, 0.9f, 0.7f) : 
                           isUnlocked ? new Color(0.7f, 0.7f, 0.7f) : new Color(0.5f, 0.5f, 0.5f);

            Image borderImage = new GameObject("Border").AddComponent<Image>();
            borderImage.transform.SetParent(container.transform, false);
            RectTransform borderRt = borderImage.GetComponent<RectTransform>();
            borderRt.sizeDelta = new Vector2(130, 130);
            borderRt.anchoredPosition = Vector2.zero;
            borderImage.color = dungeon.DungeonId == mSelectedDungeonId ? new Color(0.5f, 0.8f, 0.5f) : Color.clear;

            Text text = new GameObject("Text").AddComponent<Text>();
            text.transform.SetParent(container.transform, false);
            text.text = isUnlocked ? dungeon.DungeonName : "Locked";
            text.font = mDefaultFont;
            text.fontSize = 30;
            text.color = isUnlocked ? Color.black : Color.gray;
            text.alignment = TextAnchor.MiddleCenter;
            RectTransform textRt = text.GetComponent<RectTransform>();
            textRt.sizeDelta = new Vector2(130, 130);

            Button button = container.AddComponent<Button>();
            button.interactable = isUnlocked;
            button.onClick.AddListener(() => OnDungeonSelected(dungeon.DungeonId));
        }

        private void OnDungeonSelected(string dungeonId)
        {
            if (!DungeonManager.Instance.IsDungeonUnlocked(dungeonId))
                return;

            mSelectedDungeonId = dungeonId;
            RefreshUI();
        }

        private void UpdateSelectedDungeonInfo()
        {
            DungeonConfig selectedDungeon = mDungeons.FirstOrDefault(d => d.DungeonId == mSelectedDungeonId);
            
            if (selectedDungeon != null && mStatusText != null)
            {
                string dropInfo = "";
                if (selectedDungeon.DropRewards != null && selectedDungeon.DropRewards.Count > 0)
                {
                    dropInfo = "\n掉落奖励:";
                    foreach (var drop in selectedDungeon.DropRewards)
                    {
                        dropInfo += $"\n  - {GetItemDisplayName(drop.ItemType, drop.ItemId)} (概率{drop.DropRate * 100}%)";
                    }
                }

                mStatusText.text = $"副本名称: {selectedDungeon.DungeonName}\n" +
                    $"经验奖励: {selectedDungeon.BaseExp}\n" +
                    $"金币奖励: {selectedDungeon.GoldReward}\n" +
                    $"波次数: {selectedDungeon.Waves.Count}" +
                    dropInfo;
            }

            if (mStartButton != null)
            {
                mStartButton.onClick.RemoveAllListeners();
                mStartButton.onClick.AddListener(() => OnStartDungeon(mSelectedDungeonId));
            }
        }

        private string GetItemDisplayName(string itemType, string itemId)
        {
            switch (itemType)
            {
                case "Consumable":
                    return GameDataMgr.Instance.consumableConfigs?.Consumables
                        .FirstOrDefault(c => c.ItemId == itemId)?.Name ?? itemId;
                case "Relic":
                    return GameDataMgr.Instance.relicConfigs?.Relics
                        .FirstOrDefault(r => r.RelicId == itemId)?.Name ?? itemId;
                case "Weapon":
                    return GameDataMgr.Instance.lightConeConfigs?.Weapons
                        .FirstOrDefault(w => w.WeaponId == itemId)?.Name ?? itemId;
                default:
                    return itemId;
            }
        }

        private void OnStartDungeon(string dungeonId)
        {
            if (string.IsNullOrEmpty(dungeonId))
            {
                LogKit.Warning("[DungeonSelectPanel] 未选择副本");
                return;
            }

            LogKit.Log($"[DungeonSelectPanel] 选择副本: {dungeonId}，进入编队");
            UIKit.ClosePanel<DungeonSelectPanel>();
            UIKit.OpenPanel<TeamPanel>(UILevel.Pop, data: new TeamPanelData { DungeonId = dungeonId });
        }

        private void OnBackButtonClicked()
        {
            LogKit.Log("[DungeonSelectPanel] 返回主菜单");
            UIKit.ClosePanel<DungeonSelectPanel>();
            UIKit.OpenPanel<MainMenuPanel>(UILevel.AlwayBottom);
        }
    }
#endregion
}

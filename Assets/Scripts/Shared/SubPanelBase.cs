using UnityEngine;
using YokiFrame;

namespace GameUI
{
    public class SubPanelBase : UIPanel
    {
        protected PanelStateData mPanelState = new PanelStateData();

        protected override void OnShow()
        {
            SetPanelSortingOrder();
        }

        protected void SetPanelSortingOrder()
        {
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null && UIManager.Instance != null)
            {
                canvas.sortingOrder = UIManager.Instance.AllocateSortingOrder();
            }
        }

        protected void SavePanelState(string characterId, string currentTab, string currentRelicSlot = null, int currentStarLevel = 1)
        {
            mPanelState.SelectedCharacterId = characterId;
            mPanelState.CurrentTab = currentTab;
            mPanelState.CurrentRelicSlot = currentRelicSlot;
            mPanelState.CurrentStarLevel = currentStarLevel;
        }

        protected void LoadPanelState(PanelStateData state)
        {
            if (state != null)
            {
                mPanelState = state;
            }
        }

        protected PanelStateData GetPanelState()
        {
            return mPanelState;
        }
    }
}

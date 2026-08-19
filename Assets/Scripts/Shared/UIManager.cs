using UnityEngine;
using YokiFrame;
using YokiFrame.Unity;

namespace GameUI
{
    public sealed class UIManager : MonoSingleton<UIManager>
    {
        private int mPanelDepthCounter = 10;

        public override void OnSingletonInit()
        {
            LogKit.Log("UIManager initialized");
        }

        public int AllocateSortingOrder()
        {
            return mPanelDepthCounter++;
        }

        public T OpenPanel<T>(IUIData data = null) where T : UIPanel
        {
            return UIKit.OpenPanel<T>(data: data);
        }

        public void ClosePanel<T>() where T : UIPanel
        {
            UIKit.ClosePanel<T>();
        }

        public T GetPanel<T>() where T : UIPanel
        {
            return UIKit.GetPanel<T>();
        }

        public void CloseAllPanels()
        {
            UIKit.CloseAllPanel();
        }
    }
}

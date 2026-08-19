using UnityEngine;
using YokiFrame;

namespace GameUI
{
	public sealed class LoadingPanelData : IUIData
	{
	}

	public partial class LoadingPanel : UIPanel
	{
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as LoadingPanelData ?? new LoadingPanelData();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as LoadingPanelData ?? mData;
		}

		protected override void OnShow()
		{
		}

		protected override void OnHide()
		{
		}

		protected override void OnClose()
		{
		}
	}
}

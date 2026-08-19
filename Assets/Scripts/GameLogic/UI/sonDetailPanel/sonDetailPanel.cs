using UnityEngine;
using YokiFrame;

namespace GameUI
{
	/// <summary>
	/// 详情子面板数据。
	/// </summary>
	public sealed class sonDetailPanelData : IUIData
	{
	}

	/// <summary>
	/// 详情子面板。
	/// </summary>
	public partial class sonDetailPanel : UIPanel
	{
		#region 生命周期
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as sonDetailPanelData ?? new sonDetailPanelData();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as sonDetailPanelData ?? mData;
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
		#endregion
	}
}

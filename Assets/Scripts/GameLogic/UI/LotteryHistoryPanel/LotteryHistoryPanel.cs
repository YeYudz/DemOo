using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameUI
{
	/// <summary>
	/// 抽卡历史面板数据。
	/// </summary>
	public sealed class LotteryHistoryPanelData : IUIData
	{
		public string PoolId { get; set; }
	}

	/// <summary>
	/// 抽卡历史面板：分页显示指定卡池的抽卡记录。
	/// </summary>
	public partial class LotteryHistoryPanel : UIPanel
	{
		#region 生命周期与历史记录
		private const int RECORDS_PER_PAGE = 10;
		private int mCurrentPage = 0;
		private List<LotteryHistoryRecord> mHistoryRecords = new List<LotteryHistoryRecord>();

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as LotteryHistoryPanelData ?? new LotteryHistoryPanelData();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as LotteryHistoryPanelData ?? mData;
			LoadHistoryData();
			RegisterButtonEvents();
			RefreshDisplay();
		}

		protected override void OnShow()
		{
			SetPanelSortingOrder();
		}

		protected override void OnHide()
		{
		}

		protected override void OnClose()
		{
		}

		private void SetPanelSortingOrder()
		{
			Canvas canvas = GetComponent<Canvas>();
			if (canvas != null && UIManager.Instance != null)
			{
				canvas.sortingOrder = UIManager.Instance.AllocateSortingOrder();
			}
		}

		private void LoadHistoryData()
		{
			mHistoryRecords.Clear();
			if (AccountManager.Instance != null && AccountManager.Instance.IsLoggedIn)
			{
				var allRecords = LotterySystem.Instance.GetLotteryHistory();
				if (allRecords != null)
				{
					string poolId = mData.PoolId;
					if (string.IsNullOrEmpty(poolId))
					{
						poolId = "limited_character";
					}

					// 只显示当前卡池的记录，最新的在前
					mHistoryRecords = allRecords
						.Where(r => r.PoolId == poolId)
						.AsEnumerable()
						.Reverse()
						.ToList();
				}
			}
			mCurrentPage = 0;
		}

		private void RegisterButtonEvents()
		{
			if (mBtnClose != null)
			{
				mBtnClose.onClick.RemoveAllListeners();
				mBtnClose.onClick.AddListener(OnBtnCloseClicked);
			}

			if (mBtnLeft != null)
			{
				mBtnLeft.onClick.RemoveAllListeners();
				mBtnLeft.onClick.AddListener(OnBtnLeftClicked);
			}

			if (mBtnRight != null)
			{
				mBtnRight.onClick.RemoveAllListeners();
				mBtnRight.onClick.AddListener(OnBtnRightClicked);
			}
		}

		/// <summary>
		/// 关闭抽卡历史面板。
		/// </summary>
		private void OnBtnCloseClicked()
		{
			UIKit.ClosePanel<LotteryHistoryPanel>();
		}

		private void OnBtnLeftClicked()
		{
			if (mCurrentPage > 0)
			{
				mCurrentPage--;
				RefreshDisplay();
			}
		}

		private void OnBtnRightClicked()
		{
			int totalPages = GetTotalPages();
			if (mCurrentPage < totalPages - 1)
			{
				mCurrentPage++;
				RefreshDisplay();
			}
		}

		private int GetTotalPages()
		{
			if (mHistoryRecords == null || mHistoryRecords.Count == 0)
				return 0;
			return Mathf.CeilToInt(mHistoryRecords.Count / (float)RECORDS_PER_PAGE);
		}

		private void RefreshDisplay()
		{
			if (mTexHistory == null) return;

			if (mHistoryRecords == null || mHistoryRecords.Count == 0)
			{
				mTexHistory.text = "暂无抽卡记录";
				UpdateNavigationButtons();
				return;
			}

			StringBuilder sb = new StringBuilder();

			int startIndex = mCurrentPage * RECORDS_PER_PAGE;
			int count = Mathf.Min(RECORDS_PER_PAGE, mHistoryRecords.Count - startIndex);

			for (int i = 0; i < count; i++)
			{
				var record = mHistoryRecords[startIndex + i];
				string rarityStars = new string('★', record.Rarity);
				System.DateTimeOffset utcTime = System.DateTimeOffset.FromUnixTimeSeconds(record.Timestamp);
				System.DateTime beijingTime = utcTime.ToOffset(System.TimeSpan.FromHours(8)).DateTime;
				string timestamp = beijingTime.ToString("MM-dd HH:mm");
				// 根据星级设置颜色：3星纯黑，4星紫色，5星金色
				string colorTag = GetRarityColorTag(record.Rarity);
				// [UP] 标签移至行尾
				string upTag = record.IsUp ? " [UP]" : "";
				sb.AppendLine($"{timestamp}  <color={colorTag}>{record.ItemName} ({rarityStars})</color>{upTag}");
			}

			mTexHistory.text = sb.ToString();
			UpdateNavigationButtons();
		}

		private string GetRarityColorTag(int rarity)
		{
			switch (rarity)
			{
				case 5: return "#FAE135"; // 金色 (250, 225, 53)
				case 4: return "#AB3FEA"; // 紫色 (171, 63, 234)
				default: return "#000000"; // 3星及以下纯黑
			}
		}

		private void UpdateNavigationButtons()
	{
		int totalPages = GetTotalPages();

		if (mBtnLeft != null)
		{
			mBtnLeft.interactable = mCurrentPage > 0;
		}

		if (mBtnRight != null)
		{
			mBtnRight.interactable = totalPages > 0 && mCurrentPage < totalPages - 1;
		}

		if (mTextPage != null)
		{
			if (totalPages > 0)
			{
				mTextPage.text = (mCurrentPage + 1) + "/" + totalPages;
			}
			else
			{
				mTextPage.text = "0/0";
			}
		}
	}
		#endregion
	}
}

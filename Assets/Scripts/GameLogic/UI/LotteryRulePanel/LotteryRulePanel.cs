using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using System.Collections.Generic;
using System.Text;

namespace GameUI
{
	public sealed class LotteryRulePanelData : IUIData
	{
		public string PoolId { get; set; }
	}

	public class LotteryRuleSection
	{
		public string Title { get; set; }
		public List<string> Lines { get; set; } = new List<string>();
	}

	public class LotteryRuleEntry
	{
		public string PoolId { get; set; }
		public string PoolName { get; set; }
		public List<LotteryRuleSection> Sections { get; set; } = new List<LotteryRuleSection>();
	}

	public class LotteryRuleDatabase
	{
		public List<LotteryRuleEntry> Rules { get; set; } = new List<LotteryRuleEntry>();
	}

	/// <summary>
	/// 抽卡规则面板：显示当前卡池的概率与保底规则。
	/// </summary>
	public partial class LotteryRulePanel : UIPanel
	{
		#region 生命周期与规则展示
		private LotteryRuleDatabase mRuleDatabase;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as LotteryRulePanelData ?? new LotteryRulePanelData();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as LotteryRulePanelData ?? mData;
			LoadRuleDatabase();
			RegisterButtonEvents();
			RefreshRuleDisplay();
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

		private void RegisterButtonEvents()
		{
			if (mBtnClose != null)
			{
				mBtnClose.onClick.RemoveAllListeners();
				mBtnClose.onClick.AddListener(OnBtnCloseClicked);
			}
		}

		private void OnBtnCloseClicked()
		{
			UIKit.ClosePanel<LotteryRulePanel>();
		}

		private void LoadRuleDatabase()
		{
			mRuleDatabase = JsonMgr.Instance.LoadConfig<LotteryRuleDatabase>("LotteryRules");
			if (mRuleDatabase == null || mRuleDatabase.Rules == null || mRuleDatabase.Rules.Count == 0)
			{
				Debug.LogError("[LotteryRulePanel] 规则配置加载失败或为空，请检查 YooAsset DataConfig/LotteryRules.json");
			}
		}

		private void RefreshRuleDisplay()
		{
			if (mTexRule == null) return;

			StringBuilder sb = new StringBuilder();

			if (mRuleDatabase == null || mRuleDatabase.Rules == null || mRuleDatabase.Rules.Count == 0)
			{
				sb.AppendLine("规则配置加载失败，请检查 StreamingAssets/LotteryRules.json");
				mTexRule.text = sb.ToString();
				return;
			}

			string poolId = mData.PoolId;
			if (string.IsNullOrEmpty(poolId))
			{
				poolId = "limited_character";
			}

			var ruleEntry = mRuleDatabase.Rules.Find(r => r.PoolId == poolId);
			if (ruleEntry == null)
			{
				sb.AppendLine("【抽卡规则】");
				sb.AppendLine();
				sb.AppendLine("当前卡池的规则配置未找到");
				sb.AppendLine();
				sb.AppendLine("可用卡池：");
				foreach (var rule in mRuleDatabase.Rules)
				{
					sb.AppendLine("  - " + rule.PoolName);
				}
				mTexRule.text = sb.ToString();
				return;
			}

			sb.AppendLine("【抽卡规则】");
			sb.AppendLine();
			sb.AppendLine("卡池名称：" + ruleEntry.PoolName);
			sb.AppendLine();

			foreach (var section in ruleEntry.Sections)
			{
				if (!string.IsNullOrEmpty(section.Title))
				{
					sb.AppendLine(section.Title);
				}

				if (section.Lines != null)
				{
					foreach (var line in section.Lines)
					{
						sb.AppendLine(line);
					}
				}

				sb.AppendLine();
			}

			mTexRule.text = sb.ToString();
		}
		#endregion
	}
}

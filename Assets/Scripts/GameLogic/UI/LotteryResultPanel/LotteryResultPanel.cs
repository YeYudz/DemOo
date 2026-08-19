using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YokiFrame;

namespace GameUI
{
	/// <summary>
	/// 抽卡结果面板：展示单抽或十连抽结果。
	/// </summary>
	public partial class LotteryResultPanel : UIPanel
	{
		#region 生命周期与结果展示
		private List<GameObject> mCreatedItems = new List<GameObject>();
		private Coroutine mShowCoroutine;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as LotteryResultPanelData ?? new LotteryResultPanelData();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as LotteryResultPanelData ?? mData;
			RegisterButtonEvents();
			ClearContent();
		}

		protected override void OnShow()
		{
			SetPanelSortingOrder();
			StartShowingResults();
		}

		protected override void OnHide()
		{
		}

		protected override void OnClose()
		{
			if (mShowCoroutine != null)
			{
				StopCoroutine(mShowCoroutine);
				mShowCoroutine = null;
			}
			ClearContent();
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

		/// <summary>
		/// 关闭抽卡结果面板。
		/// </summary>
		private void OnBtnCloseClicked()
		{
			UIKit.ClosePanel<LotteryResultPanel>();
		}

		private void ClearContent()
		{
			foreach (var item in mCreatedItems)
			{
				if (item != null)
				{
					Destroy(item);
				}
			}
			mCreatedItems.Clear();
		}

		private void StartShowingResults()
		{
			if (mData == null || mData.DrawResult == null || mData.DrawResult.Results == null)
			{
				LogKit.Log("[LotteryResultPanel] 没有抽奖结果");
				return;
			}

			var results = mData.DrawResult.Results;
			int count = results.Count;

			if (count <= 0) return;

			if (count == 1)
			{
				ShowSingleResult(results[0]);
			}
			else
			{
				mShowCoroutine = StartCoroutine(ShowMultipleResults(results));
			}
		}

		private void ShowSingleResult(LotteryHistoryRecord record)
		{
			if (mContentContainer == null || mResultItemTemplate == null) return;

			var containerRect = mContentContainer as RectTransform;
			if (containerRect == null) return;

			float containerHeight = containerRect.rect.height;
			float itemSize = containerHeight * 0.8f;

			var itemObj = CreateResultItem(record);
			var itemRect = itemObj.GetComponent<RectTransform>();

			itemRect.anchorMin = new Vector2(0.5f, 0.5f);
			itemRect.anchorMax = new Vector2(0.5f, 0.5f);
			itemRect.pivot = new Vector2(0.5f, 0.5f);
			itemRect.sizeDelta = new Vector2(itemSize, itemSize);
			itemRect.anchoredPosition = Vector2.zero;

			mCreatedItems.Add(itemObj);
		}

		private IEnumerator ShowMultipleResults(List<LotteryHistoryRecord> results)
		{
			if (mContentContainer == null || mResultItemTemplate == null) yield break;

			var containerRect = mContentContainer as RectTransform;
			if (containerRect == null) yield break;

			int count = results.Count;
			float containerWidth = containerRect.rect.width;
			float containerHeight = containerRect.rect.height;

			float itemHeight = containerHeight * 0.8f;
			float itemWidth = itemHeight;

			float spacing = (containerWidth - itemWidth * count) / (count + 1);
			if (spacing < 5f)
			{
				spacing = 5f;
				itemWidth = (containerWidth - spacing * (count + 1)) / count;
			}

			for (int i = 0; i < count; i++)
			{
				var record = results[i];
				var itemObj = CreateResultItem(record);
				var itemRect = itemObj.GetComponent<RectTransform>();

				itemRect.anchorMin = new Vector2(0, 0.5f);
				itemRect.anchorMax = new Vector2(0, 0.5f);
				itemRect.pivot = new Vector2(0.5f, 0.5f);
				itemRect.sizeDelta = new Vector2(itemWidth, itemHeight);

				float targetX = spacing + itemWidth / 2 + i * (itemWidth + spacing);
				float startX = containerWidth + itemWidth;

				itemRect.anchoredPosition = new Vector2(startX, 0);

				mCreatedItems.Add(itemObj);

				yield return StartCoroutine(AnimateItemIn(itemRect, startX, targetX, 0.35f));
			}

			mShowCoroutine = null;
		}

		private IEnumerator AnimateItemIn(RectTransform itemRect, float fromX, float toX, float duration)
		{
			float elapsed = 0f;
			Vector2 startPos = new Vector2(fromX, 0);
			Vector2 endPos = new Vector2(toX, 0);

			while (elapsed < duration)
			{
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				t = 1f - Mathf.Pow(1f - t, 3f);

				itemRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
				yield return null;
			}

			itemRect.anchoredPosition = endPos;
		}

		private GameObject CreateResultItem(LotteryHistoryRecord record)
		{
			var itemObj = Instantiate(mResultItemTemplate, mContentContainer);
			itemObj.SetActive(true);

			var image = itemObj.GetComponent<Image>();
			if (image != null)
			{
				if (!string.IsNullOrEmpty(record.ItemType) && !string.IsNullOrEmpty(record.ItemId))
				{
					SpriteIconLoader.SetIcon(image, record.ItemType, record.ItemId);
				}
				if (image.sprite == null)
				{
					image.color = GetRarityColor(record.Rarity);
				}
			}

			// 金黄色 (250, 225, 53)，青色 (54, 117, 250)
			Color starColor = new Color32(250, 225, 53, 255);
			Color nameColor = new Color32(54, 117, 250, 255);

			var texts = itemObj.GetComponentsInChildren<Text>(true);
			foreach (var text in texts)
			{
				if (text.name.Contains("textName") || text.name.Contains("Name") || text.name.Contains("name"))
				{
					text.text = record.ItemName;
					text.color = nameColor;
				}
				else if (text.name.Contains("textStar") || text.name.Contains("Star") || text.name.Contains("star") || text.name.Contains("Rarity"))
				{
					text.text = record.Rarity + "星";
					text.color = starColor;
				}
			}

			return itemObj;
		}

		private Color GetRarityColor(int rarity)
		{
			switch (rarity)
			{
				case 5: return new Color(1f, 0.84f, 0f);
				case 4: return new Color(0.6f, 0.6f, 1f);
				default: return new Color(0.7f, 0.7f, 0.7f);
			}
		}
		#endregion
	}
}

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YokiFrame;

namespace GameUI
{
	public sealed class InitPanelData : IUIData
	{
	}

	public partial class InitPanel : UIPanel
	{
		private bool mUpdateChecked = false;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as InitPanelData ?? new InitPanelData();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as InitPanelData ?? mData;

			FixLayout();

			if (mStatusText != null)
				mStatusText.text = "正在初始化...";

			if (mProgressSlider != null)
			{
				mProgressSlider.value = 0;
				mProgressSlider.interactable = false;
			}

			if (mEnterButton != null)
				mEnterButton.interactable = false;

			// 订阅 GameBootstrap 的热更新事件
			Game.GameBootstrap.HotUpdateProgressChanged += OnHotUpdateProgress;
			Game.GameBootstrap.HotUpdateCompleted += OnHotUpdateCompleted;
		}

		/// <summary>
		/// 修复自动生成的预制体布局问题：
		/// - Panel 背景尺寸为 0 → 全屏半透明黑色遮罩
		/// - Slider 按标准 Unity 结构修复（Handle 宽度左右各缩进一半，防止 Handle 越界）
		/// - Fill 由 Slider 组件内部根据 value 驱动宽度（不写死全拉伸）
		/// - 全屏点击按钮置于最上层，接收点击进入登录
		/// </summary>
		private void FixLayout()
		{
			// 修复根节点：确保铺满父容器
			var rootRect = transform as RectTransform;
			if (rootRect != null)
			{
				rootRect.anchorMin = Vector2.zero;
				rootRect.anchorMax = Vector2.one;
				rootRect.offsetMin = Vector2.zero;
				rootRect.offsetMax = Vector2.zero;
				rootRect.localScale = Vector3.one;
			}

			// 修复 Panel 背景（第一个子对象）：全屏半透明黑色遮罩
			var panelObj = transform.Find("Panel");
			if (panelObj != null)
			{
				var panelRect = panelObj.GetComponent<RectTransform>();
				var panelImg = panelObj.GetComponent<Image>();
				if (panelRect != null)
				{
					panelRect.anchorMin = Vector2.zero;
					panelRect.anchorMax = Vector2.one;
					panelRect.offsetMin = Vector2.zero;
					panelRect.offsetMax = Vector2.zero;
				}
				if (panelImg != null)
				{
					panelImg.color = new Color(0, 0, 0, 0.75f);
					panelImg.raycastTarget = true;
				}
			}

			// 修复 Slider 相关节点（按 Unity 默认 Slider 结构标准设置）
			var sliderObj = transform.Find("sliInit");
			if (sliderObj != null)
			{
				var sliderRect = sliderObj.GetComponent<RectTransform>();
				const float sliderWidth = 800f;
				const float sliderHeight = 40f;
				const float handleWidth = 20f;
				float halfHandle = handleWidth * 0.5f;

				if (sliderRect != null)
				{
					sliderRect.anchorMin = new Vector2(0.5f, 0.5f);
					sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
					sliderRect.pivot = new Vector2(0.5f, 0.5f);
					sliderRect.sizeDelta = new Vector2(sliderWidth, sliderHeight);
					sliderRect.anchoredPosition = new Vector2(0, 0);
				}

				// 修复 Background（Slider 底部轨道）：垂直方向居中 50% 高度
				var background = sliderObj.Find("Background");
				if (background != null)
				{
					var bgRect = background.GetComponent<RectTransform>();
					var bgImg = background.GetComponent<Image>();
					if (bgRect != null)
					{
						bgRect.anchorMin = new Vector2(0f, 0.25f);
						bgRect.anchorMax = new Vector2(1f, 0.75f);
						bgRect.offsetMin = Vector2.zero;
						bgRect.offsetMax = Vector2.zero;
					}
					if (bgImg != null)
					{
						bgImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
					}
				}

				// 修复 Fill Area：垂直居中，左右缩进 halfHandle 留出 Handle 空间
				var fillArea = sliderObj.Find("Fill Area");
				if (fillArea != null)
				{
					var fillAreaRect = fillArea.GetComponent<RectTransform>();
					if (fillAreaRect != null)
					{
						fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
						fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
						fillAreaRect.offsetMin = new Vector2(halfHandle, 0f);
						fillAreaRect.offsetMax = new Vector2(-halfHandle, 0f);
					}
				}

				// 修复 Fill（进度填充）：左锚定，初始宽度 0，由 Slider 组件根据 value 驱动
				var fill = sliderObj.Find("Fill Area/Fill");
				if (fill != null)
				{
					var fillRect = fill.GetComponent<RectTransform>();
					var fillImg = fill.GetComponent<Image>();
					if (fillRect != null)
					{
						fillRect.anchorMin = new Vector2(0f, 0f);
						fillRect.anchorMax = new Vector2(0f, 1f);
						fillRect.pivot = new Vector2(0f, 0.5f);
						fillRect.anchoredPosition = Vector2.zero;
						fillRect.sizeDelta = new Vector2(0f, 0f);
					}
					if (fillImg != null)
					{
						fillImg.color = new Color(0.2f, 0.8f, 0.2f, 1f);
					}
				}

				// 修复 Handle Slide Area：水平全拉伸，左右缩进 halfHandle
				var handleSlideArea = sliderObj.Find("Fill Area/Handle Slide Area");
				if (handleSlideArea != null)
				{
					var areaRect = handleSlideArea.GetComponent<RectTransform>();
					if (areaRect != null)
					{
						areaRect.anchorMin = Vector2.zero;
						areaRect.anchorMax = Vector2.one;
						areaRect.offsetMin = new Vector2(halfHandle, 0f);
						areaRect.offsetMax = new Vector2(-halfHandle, 0f);
					}
				}

				// 修复 Handle（进度条滑块）
				var handle = sliderObj.Find("Fill Area/Handle Slide Area/Handle");
				if (handle != null)
				{
					var handleRect = handle.GetComponent<RectTransform>();
					var handleImg = handle.GetComponent<Image>();
					if (handleRect != null)
					{
						handleRect.anchorMin = new Vector2(0.5f, 0.5f);
						handleRect.anchorMax = new Vector2(0.5f, 0.5f);
						handleRect.pivot = new Vector2(0.5f, 0.5f);
						handleRect.sizeDelta = new Vector2(handleWidth, sliderHeight);
					}
					if (handleImg != null)
					{
						handleImg.color = new Color(0.2f, 0.8f, 0.2f, 1f);
					}
				}
			}

			// 修复状态文本：Slider 下方居中显示
			var statusObj = transform.Find("texInit");
			if (statusObj != null)
			{
				var statusRect = statusObj.GetComponent<RectTransform>();
				var statusText = statusObj.GetComponent<Text>();
				if (statusRect != null)
				{
					statusRect.anchorMin = new Vector2(0.5f, 0.5f);
					statusRect.anchorMax = new Vector2(0.5f, 0.5f);
					statusRect.pivot = new Vector2(0.5f, 0.5f);
					statusRect.sizeDelta = new Vector2(800f, 60f);
					statusRect.anchoredPosition = new Vector2(0, -60);
				}
				if (statusText != null)
				{
					statusText.alignment = TextAnchor.MiddleCenter;
					statusText.color = Color.white;
					statusText.fontSize = 28;
				}
			}

			// 修复按钮：全屏透明点击区域，置于最上层（接收点击），
			// 资源检查完成前保持 interactable=false，由 OnOpen / OnShow / OnHotUpdateCompleted 控制可用性
			var btnObj = transform.Find("btnInit");
			if (btnObj != null)
			{
				var btnRect = btnObj.GetComponent<RectTransform>();
				if (btnRect != null)
				{
					btnRect.anchorMin = Vector2.zero;
					btnRect.anchorMax = Vector2.one;
					btnRect.offsetMin = Vector2.zero;
					btnRect.offsetMax = Vector2.zero;
					btnRect.SetAsLastSibling();
				}
			}
		}

		protected override void OnShow()
		{
			if (mEnterButton != null)
			{
				mEnterButton.onClick.RemoveAllListeners();
				mEnterButton.onClick.AddListener(OnEnterButtonClicked);
				mEnterButton.interactable = mUpdateChecked;
			}
		}

		protected override void OnHide()
		{
			if (mEnterButton != null)
			{
				mEnterButton.onClick.RemoveAllListeners();
			}
		}

		protected override void OnClose()
		{
			// 取消订阅，防止内存泄漏
			Game.GameBootstrap.HotUpdateProgressChanged -= OnHotUpdateProgress;
			Game.GameBootstrap.HotUpdateCompleted -= OnHotUpdateCompleted;
		}

		/// <summary>
		/// 热更新进度回调：由 GameBootstrap 驱动
		/// </summary>
		private void OnHotUpdateProgress(float progress, string status)
		{
			UpdateProgress(progress, status);
		}

		/// <summary>
		/// 热更新完成回调
		/// </summary>
		private void OnHotUpdateCompleted(bool success, string error)
		{
			if (success)
			{
				mUpdateChecked = true;
				if (mEnterButton != null)
					mEnterButton.interactable = true;
			}
			else
			{
				LogKit.Error("[InitPanel] 热更新失败: " + error);
				UpdateProgress(0f, "资源更新失败: " + error);
			}
		}

		/// <summary>
		/// 更新进度条与状态文本（Unity UI 必须在主线程调用）。
		/// 同时根据进度值控制按钮可用性：只有 Slider.value >= 1 且热更新成功时才可用。
		/// </summary>
		private void UpdateProgress(float value, string status)
		{
			if (mProgressSlider != null)
				mProgressSlider.value = value;
			if (mStatusText != null)
				mStatusText.text = status;

			// 进度不满 1 或资源检查未通过，按钮保持不可用
			if (mEnterButton != null)
			{
				bool isReady = value >= 1f && mUpdateChecked;
				if (mEnterButton.interactable != isReady)
					mEnterButton.interactable = isReady;
			}
		}

		private void OnEnterButtonClicked()
		{
			if (!mUpdateChecked) return;

			LogKit.Log("[InitPanel] 进入游戏");
			UIKit.ClosePanel<InitPanel>();
			Game.GameBootstrap.TriggerEnterGame();
		}
	}
}
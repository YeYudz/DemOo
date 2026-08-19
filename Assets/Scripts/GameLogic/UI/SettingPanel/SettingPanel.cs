using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using Game;
using System.Collections.Generic;

namespace GameUI
{
	public sealed class SettingPanelData : IUIData
	{
	}

	/// <summary>
	/// 设置面板：分为"视频"/"音量"两个页签。
	/// - 视频页签：全屏切换 + 分辨率选择
	/// - 音量页签：音乐与音效 Slider
	/// </summary>
	public partial class SettingPanel : UIPanel
	{
		#region 生命周期与设置操作
		/// <summary>
		/// 分辨率数据：宽高
		/// </summary>
		private struct ResolutionOption
		{
			public int Width;
			public int Height;
			public string Label;
			public ResolutionOption(int w, int h, string lbl) { Width = w; Height = h; Label = lbl; }
		}

		private List<ResolutionOption> mResolutionOptions;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as SettingPanelData ?? new SettingPanelData();
			InitResolutionOptions();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as SettingPanelData ?? mData;
		}

		protected override void OnShow()
		{
			// ============= 页签 Toggle =============
			if (mVideoTabToggle != null)
			{
				mVideoTabToggle.onValueChanged.RemoveAllListeners();
				mVideoTabToggle.onValueChanged.AddListener(OnVideoTabChanged);
			}
			if (mAudioTabToggle != null)
			{
				mAudioTabToggle.onValueChanged.RemoveAllListeners();
				mAudioTabToggle.onValueChanged.AddListener(OnAudioTabChanged);
			}
			// 默认选中"音量"页签（保持原有行为，因为首次打开就是音量设置）
			if (mAudioTabToggle != null && mAudioTabToggle.isOn)
			{
				SwitchToAudioTab();
			}
			else if (mVideoTabToggle != null && mVideoTabToggle.isOn)
			{
				SwitchToVideoTab();
			}
			else
			{
				// 两个 toggle 都没选中，默认选音频
				SwitchToAudioTab();
				if (mAudioTabToggle != null) mAudioTabToggle.isOn = true;
			}

			// ============= 音量页签 =============
			if (mMusicVolumeSlider != null)
			{
				mMusicVolumeSlider.minValue = 0f;
				mMusicVolumeSlider.maxValue = 1f;
				mMusicVolumeSlider.value = AudioManager.Instance.MusicVolume;
				mMusicVolumeSlider.onValueChanged.RemoveAllListeners();
				mMusicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
			}

			if (mSFXVolumeSlider != null)
			{
				mSFXVolumeSlider.minValue = 0f;
				mSFXVolumeSlider.maxValue = 1f;
				mSFXVolumeSlider.value = AudioManager.Instance.SFXVolume;
				mSFXVolumeSlider.onValueChanged.RemoveAllListeners();
				mSFXVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
			}

			UpdateMusicValueText();
			UpdateSFXValueText();

			// ============= 视频页签：全屏 + 分辨率 =============
		if (mFullscreenLabel != null)
		{
			mFullscreenLabel.text = "全屏";
		}
		if (mFullscreenToggle != null)
		{
			// 从持久化数据读取全屏状态（不依赖 Screen.fullScreen，编辑器模式下也可正确显示）
			var vData = GameDataMgr.Instance.videoSettingsData;
			mFullscreenToggle.isOn = vData != null ? vData.isFullscreen : Screen.fullScreen;
			mFullscreenToggle.onValueChanged.RemoveAllListeners();
			mFullscreenToggle.onValueChanged.AddListener(OnFullscreenChanged);
		}

		if (mResolutionLabel != null)
		{
			mResolutionLabel.text = "分辨率";
		}
		InitResolutionDropdown();

			// ============= 关闭按钮 =============
			if (mCloseButton != null)
			{
				mCloseButton.onClick.RemoveAllListeners();
				mCloseButton.onClick.AddListener(OnCloseButtonClicked);
			}
		}

		protected override void OnHide()
		{
			if (mVideoTabToggle != null)
				mVideoTabToggle.onValueChanged.RemoveAllListeners();
			if (mAudioTabToggle != null)
				mAudioTabToggle.onValueChanged.RemoveAllListeners();
			if (mMusicVolumeSlider != null)
				mMusicVolumeSlider.onValueChanged.RemoveListener(OnMusicVolumeChanged);
			if (mSFXVolumeSlider != null)
				mSFXVolumeSlider.onValueChanged.RemoveListener(OnSFXVolumeChanged);
			if (mFullscreenToggle != null)
				mFullscreenToggle.onValueChanged.RemoveAllListeners();
			if (mResolutionDropdown != null)
				mResolutionDropdown.onValueChanged.RemoveAllListeners();
			if (mCloseButton != null)
				mCloseButton.onClick.RemoveAllListeners();
		}

		protected override void OnClose()
		{
		}
		#endregion

		#region 页签切换

		private void OnVideoTabChanged(bool isOn)
		{
			if (isOn)
			{
				SwitchToVideoTab();
				// 互斥：视频页签打开时，音频页签关闭
				if (mAudioTabToggle != null && mAudioTabToggle.isOn)
					mAudioTabToggle.isOn = false;
			}
			else
			{
				// 如果视频关闭且音频也关闭，默认回到音频
				if (mAudioTabToggle != null && !mAudioTabToggle.isOn)
				{
					mAudioTabToggle.isOn = true;
					SwitchToAudioTab();
				}
			}
		}

		private void OnAudioTabChanged(bool isOn)
		{
			if (isOn)
			{
				SwitchToAudioTab();
				// 互斥：音频页签打开时，视频页签关闭
				if (mVideoTabToggle != null && mVideoTabToggle.isOn)
					mVideoTabToggle.isOn = false;
			}
			else
			{
				// 如果音频关闭且视频也关闭，默认回到视频
				if (mVideoTabToggle != null && !mVideoTabToggle.isOn)
				{
					mVideoTabToggle.isOn = true;
					SwitchToVideoTab();
				}
			}
		}

		private void SwitchToVideoTab()
		{
			if (mVideoContentPanel != null)
				mVideoContentPanel.SetActive(true);
			if (mAudioContentPanel != null)
				mAudioContentPanel.SetActive(false);
		}

		private void SwitchToAudioTab()
		{
			if (mVideoContentPanel != null)
				mVideoContentPanel.SetActive(false);
			if (mAudioContentPanel != null)
				mAudioContentPanel.SetActive(true);
		}

		#endregion

		#region 音量设置

		private void OnMusicVolumeChanged(float value)
		{
			AudioManager.Instance.SetMusicVolume(value);
			UpdateMusicValueText();
		}

		private void OnSFXVolumeChanged(float value)
		{
			AudioManager.Instance.SetSFXVolume(value);
			UpdateSFXValueText();
			AudioManager.Instance.PlayButtonClick();
		}

		private void UpdateMusicValueText()
		{
			if (mMusicValueText != null)
			{
				mMusicValueText.text = Mathf.RoundToInt(AudioManager.Instance.MusicVolume * 100) + "%";
			}
		}

		private void UpdateSFXValueText()
		{
			if (mSFXValueText != null)
			{
				mSFXValueText.text = Mathf.RoundToInt(AudioManager.Instance.SFXVolume * 100) + "%";
			}
		}

		#endregion

		#region 视频设置

		/// <summary>
		/// 初始化可选分辨率列表：提供常见分辨率 + 系统支持的全部分辨率
		/// </summary>
		private void InitResolutionOptions()
		{
			mResolutionOptions = new List<ResolutionOption>
			{
				new ResolutionOption(1280, 720, "1280 x 720"),
				new ResolutionOption(1366, 768, "1366 x 768"),
				new ResolutionOption(1600, 900, "1600 x 900"),
				new ResolutionOption(1920, 1080, "1920 x 1080"),
				new ResolutionOption(2560, 1440, "2560 x 1440"),
				new ResolutionOption(3840, 2160, "3840 x 2160")
			};

			// 加入系统支持的额外分辨率（去重、按宽度升序）
			HashSet<string> added = new HashSet<string>();
			foreach (var o in mResolutionOptions)
				added.Add(o.Label);

			try
			{
				foreach (var res in Screen.resolutions)
				{
					string key = $"{res.width} x {res.height}";
					if (!added.Contains(key))
					{
						added.Add(key);
						mResolutionOptions.Add(new ResolutionOption(res.width, res.height, key));
					}
				}
			}
			catch { /* 部分平台可能没有Screen.resolutions，忽略 */ }

			mResolutionOptions.Sort((a, b) => a.Width.CompareTo(b.Width));
		}

		/// <summary>
	/// 填充分辨率下拉框，选中持久化的分辨率（无持久化时使用 Screen 当前分辨率）
	/// </summary>
	private void InitResolutionDropdown()
	{
		if (mResolutionDropdown == null) return;

		mResolutionDropdown.onValueChanged.RemoveAllListeners();
		mResolutionDropdown.options.Clear();

		// 优先从持久化数据读取分辨率，编辑器模式下也能正确显示
		var vData = GameDataMgr.Instance.videoSettingsData;
		int currentW = vData != null ? vData.width : Screen.width;
		int currentH = vData != null ? vData.height : Screen.height;

		int currentIdx = 0;
		bool matched = false;

		for (int i = 0; i < mResolutionOptions.Count; i++)
		{
			var opt = mResolutionOptions[i];
			mResolutionDropdown.options.Add(new Dropdown.OptionData(opt.Label));
			if (opt.Width == currentW && opt.Height == currentH)
			{
				currentIdx = i;
				matched = true;
			}
		}

		// 兜底：持久化的分辨率不在预设列表中，作为"自定义"追加
		if (!matched && mResolutionOptions.Count > 0)
		{
			string custom = $"{currentW} x {currentH}";
			mResolutionOptions.Add(new ResolutionOption(currentW, currentH, custom));
			mResolutionDropdown.options.Add(new Dropdown.OptionData(custom));
			currentIdx = mResolutionOptions.Count - 1;
		}

		mResolutionDropdown.value = currentIdx;
		mResolutionDropdown.RefreshShownValue();
		mResolutionDropdown.onValueChanged.AddListener(OnResolutionChanged);
	}

	private void OnFullscreenChanged(bool isFullscreen)
	{
		Screen.fullScreen = isFullscreen;

		// 持久化
		var data = GameDataMgr.Instance.videoSettingsData;
		if (data != null)
		{
			data.isFullscreen = isFullscreen;
			GameDataMgr.Instance.SaveVideoSettingsData();
		}

		LogKit.Log($"[SettingPanel] 全屏模式切换: {isFullscreen}");
	}

	private void OnResolutionChanged(int index)
	{
		if (index < 0 || index >= mResolutionOptions.Count) return;

		var opt = mResolutionOptions[index];
		Screen.SetResolution(opt.Width, opt.Height, Screen.fullScreen);

		// 持久化
		var data = GameDataMgr.Instance.videoSettingsData;
		if (data != null)
		{
			data.width = opt.Width;
			data.height = opt.Height;
			GameDataMgr.Instance.SaveVideoSettingsData();
		}

		LogKit.Log($"[SettingPanel] 分辨率切换: {opt.Label}");
	}

		#endregion

		private void OnCloseButtonClicked()
		{
			UIKit.ClosePanel<SettingPanel>();
		}
	}
}

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using YokiFrame.Unity;

namespace Game
{
    /// <summary>
    /// 音频管理器，基于 YokiFrame.AudioKit 封装背景音乐与音效播放及音量/静音设置。
    /// 音频资源需放置在 Resources/Audio/Music 与 Resources/Audio/Sfx 目录下（不包含文件扩展名）。
    /// 设置通过 GameDataMgr.musicData 持久化（MusicData.json）。
    /// </summary>
    public sealed class AudioManager : MonoSingleton<AudioManager>
    {
        // 背景音乐路径（对应 Resources/Audio/Music/{名称}）
        public const string BGM_Login = "Audio/Music/Login";
        public const string BGM_MainMenu = "Audio/Music/MainMenu";
        public const string BGM_Battle = "Audio/Music/Battle";

        // 音效路径（对应 Resources/Audio/Sfx/{名称}）
        public const string SFX_ButtonClick = "Audio/Sfx/ButtonClick";
        public const string SFX_LoginSuccess = "Audio/Sfx/LoginSuccess";
        public const string SFX_BattleStart = "Audio/Sfx/BattleStart";
        // 攻击音效按类型分类：近战(剑/匕首统一)、远程箭矢、远程法球、增益技能
        public const string SFX_AttackMelee = "Audio/Sfx/AttackMelee";
        public const string SFX_AttackArrow = "Audio/Sfx/AttackArrow";
        public const string SFX_AttackMagic = "Audio/Sfx/AttackMagic";
        public const string SFX_Buff = "Audio/Sfx/Buff";
        public const string SFX_Hit = "Audio/Sfx/Hit";
        public const string SFX_Heal = "Audio/Sfx/Heal";

        // BGM 淡入淡出时长（秒），消除切换时“从无到有”的卡顿感
        private const float BGM_FADE_DURATION = 0.5f;

        private int mCurrentBgmVoiceId;
        private string mCurrentBgmPath;

        // 全局按钮点击音效绑定器
        private readonly List<Button> mButtonBuffer = new List<Button>();
        private readonly Dictionary<string, bool> mAudioResourceAvailability = new Dictionary<string, bool>();
        private float mNextButtonScan;
        private const float BUTTON_SCAN_INTERVAL = 0.3f;

        public override void OnSingletonInit()
        {
            ApplySettings();
            PreloadBGM();
            LogKit.Log("[AudioManager] initialized, backend: " + AudioKit.BackendName);
        }

        /// <summary>
        /// 读取 GameDataMgr.musicData 并应用到 AudioKit 总线音量与静音状态。
        /// 需在 YokiFrameKit.Initialize 之后调用，确保音频后端已注入。
        /// </summary>
        public void ApplySettings()
        {
            var data = GameDataMgr.Instance.musicData;
            if (data == null)
            {
                LogKit.Warning("[AudioManager] MusicData 为空，跳过设置应用");
                return;
            }

            AudioKit.SetBusVolume(AudioBus.Music, data.musicValue);
            AudioKit.SetBusVolume(AudioBus.Sfx, data.soundValue);
            AudioKit.MuteBus(AudioBus.Music, !data.musicIsOpen);
            AudioKit.MuteBus(AudioBus.Sfx, !data.soundIsOpen);

            LogKit.Log($"[AudioManager] 设置已应用 - 音乐:{(data.musicIsOpen ? "开" : "关")}({data.musicValue}), 音效:{(data.soundIsOpen ? "开" : "关")}({data.soundValue})");
        }

        /// <summary>
        /// 预加载所有 BGM 资源，避免切换时同步加载造成的卡顿。
        /// </summary>
        private void PreloadBGM()
        {
            AudioKit.Preload(BGM_Login);
            AudioKit.Preload(BGM_MainMenu);
            AudioKit.Preload(BGM_Battle);
        }

        #region 背景音乐

        /// <summary>
        /// 播放背景音乐（Music 总线，默认循环，带淡入）。会先淡出当前正在播放的 BGM。
        /// </summary>
        public void PlayBGM(string path, bool loop = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                LogKit.Warning("[AudioManager] PlayBGM 路径为空");
                return;
            }

            // 同一路径已在播放则不重复播放
            if (mCurrentBgmPath == path && mCurrentBgmVoiceId > 0)
                return;

            // 淡出旧的 BGM（不阻塞，与新 BGM 淡入并行形成交叉淡化）
            StopBGM();

            mCurrentBgmPath = path;
            var options = AudioPlayOptions.Default;
            options.Bus = AudioBus.Music;
            options.Loop = loop;
            options.Volume = 1f;
            options.FadeInDuration = BGM_FADE_DURATION;
            options.FadeOutDuration = BGM_FADE_DURATION;
            mCurrentBgmVoiceId = AudioKit.Play(path, options);
            LogKit.Log($"[AudioManager] 播放 BGM: {path}, voiceId={mCurrentBgmVoiceId}");
        }

        /// <summary>
        /// 淡出并停止当前背景音乐。
        /// </summary>
        public void StopBGM()
        {
            if (mCurrentBgmVoiceId > 0)
            {
                AudioKit.StopWithFade(mCurrentBgmVoiceId, BGM_FADE_DURATION);
                mCurrentBgmVoiceId = 0;
            }
            mCurrentBgmPath = null;
        }

        /// <summary>
        /// 当前正在播放的 BGM 路径；未播放时为 null。
        /// </summary>
        public string CurrentBGM => mCurrentBgmPath;

        #endregion

        #region 音效

        /// <summary>
        /// 播放音效（Sfx 总线，不循环）。
        /// </summary>
        public void PlaySFX(string path)
        {
            if (string.IsNullOrEmpty(path))
                return;

            if (!HasAudioResource(path))
                return;

            AudioKit.PlaySfx(path);
        }

        /// <summary>
        /// 播放通用按钮点击音效。
        /// </summary>
        public void PlayButtonClick()
        {
            PlaySFX(SFX_ButtonClick);
        }

        private bool HasAudioResource(string path)
        {
            if (mAudioResourceAvailability.TryGetValue(path, out bool exists))
                return exists;

            exists = Resources.Load<AudioClip>(path) != null;
            mAudioResourceAvailability[path] = exists;
            return exists;
        }

        #endregion

        #region 全局按钮点击音效绑定

        private void Update()
        {
            // 驱动 AudioKit 后端：推进 BGM 淡入淡出、清理已结束的音效 voice。
            // Unity 默认安装器（UnityAudioKitInstaller.Tick）不会自动调用 AudioKit.Update，
            // 必须由宿主每帧驱动，否则带 FadeIn 的 BGM 会停留在初始 0 音量（表现为 BGM 不响/被音效覆盖）。
            AudioKit.Update(Time.unscaledDeltaTime);

            if (Time.unscaledTime < mNextButtonScan)
                return;
            mNextButtonScan = Time.unscaledTime + BUTTON_SCAN_INTERVAL;
            RefreshButtonSounds();
        }

        /// <summary>
        /// 扫描 UIRoot 下所有 Button，自动绑定点击音效。
        /// 采用“先移除再添加”策略：每次移除旧的 PlayButtonClick 监听并重新添加，
        /// 可自动恢复被面板 OnShow 中 RemoveAllListeners 清除的监听，且不会重复累积。
        /// </summary>
        private void RefreshButtonSounds()
        {
            var root = UIRoot.ExistingInstance;
            if (root == null)
                return;

            mButtonBuffer.Clear();
            root.GetComponentsInChildren(true, mButtonBuffer);

            for (int i = 0; i < mButtonBuffer.Count; i++)
            {
                var btn = mButtonBuffer[i];
                if (btn == null)
                    continue;
                btn.onClick.RemoveListener(PlayButtonClick);
                btn.onClick.AddListener(PlayButtonClick);
            }
        }

        #endregion

        #region 音量与静音设置（持久化）

        /// <summary>
        /// 设置背景音乐音量并持久化。
        /// </summary>
        public void SetMusicVolume(float volume)
        {
            var data = GameDataMgr.Instance.musicData;
            if (data == null) return;

            data.musicValue = Mathf.Clamp01(volume);
            AudioKit.SetBusVolume(AudioBus.Music, data.musicValue);
            GameDataMgr.Instance.SaveMusicData();
        }

        /// <summary>
        /// 设置音效音量并持久化。
        /// </summary>
        public void SetSFXVolume(float volume)
        {
            var data = GameDataMgr.Instance.musicData;
            if (data == null) return;

            data.soundValue = Mathf.Clamp01(volume);
            AudioKit.SetBusVolume(AudioBus.Sfx, data.soundValue);
            GameDataMgr.Instance.SaveMusicData();
        }

        /// <summary>
        /// 开关背景音乐并持久化。
        /// </summary>
        public void ToggleMusic(bool on)
        {
            var data = GameDataMgr.Instance.musicData;
            if (data == null) return;

            data.musicIsOpen = on;
            AudioKit.MuteBus(AudioBus.Music, !on);
            GameDataMgr.Instance.SaveMusicData();
        }

        /// <summary>
        /// 开关音效并持久化。
        /// </summary>
        public void ToggleSFX(bool on)
        {
            var data = GameDataMgr.Instance.musicData;
            if (data == null) return;

            data.soundIsOpen = on;
            AudioKit.MuteBus(AudioBus.Sfx, !on);
            GameDataMgr.Instance.SaveMusicData();
        }

        public bool IsMusicOn => GameDataMgr.Instance.musicData?.musicIsOpen ?? true;
        public bool IsSFXOn => GameDataMgr.Instance.musicData?.soundIsOpen ?? true;
        public float MusicVolume => GameDataMgr.Instance.musicData?.musicValue ?? 1f;
        public float SFXVolume => GameDataMgr.Instance.musicData?.soundValue ?? 1f;

        #endregion
    }
}

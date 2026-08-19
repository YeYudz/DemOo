using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using Game;

namespace GameUI
{
	public sealed class LoginPanelData : IUIData
	{
		public string Username { get; set; }
		public string Password { get; set; }
		public bool IsFromRegisterSuccess { get; set; }
	}

	/// <summary>
	/// 登录面板：校验账号并进入主菜单。
	/// </summary>
	public partial class LoginPanel : UIPanel
	{
		#region 生命周期与登录操作
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as LoginPanelData ?? new LoginPanelData();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as LoginPanelData ?? mData;
			
			if (mUsernameInput != null)
				mUsernameInput.text = mData.Username;
			if (mPasswordInput != null)
				mPasswordInput.text = mData.Password;

			if (mErrorMessageText != null)
			{
				mErrorMessageText.gameObject.SetActive(true);
				if (mData.IsFromRegisterSuccess)
				{
					mErrorMessageText.text = "注册成功，请登录";
				}
				else
				{
					mErrorMessageText.text = "DemOo";
				}
			}
		}

		protected override void OnShow()
		{
			// 播放登录背景音乐
			AudioManager.Instance.PlayBGM(AudioManager.BGM_Login);

			if (mLoginButton != null)
			{
				mLoginButton.onClick.RemoveAllListeners();
				mLoginButton.onClick.AddListener(OnLoginButtonClicked);
			}

			if (mRegisterButton != null)
			{
				mRegisterButton.onClick.RemoveAllListeners();
				mRegisterButton.onClick.AddListener(OnRegisterButtonClicked);
			}
		}

		protected override void OnHide()
		{
			if (mLoginButton != null)
				mLoginButton.onClick.RemoveAllListeners();
			if (mRegisterButton != null)
				mRegisterButton.onClick.RemoveAllListeners();

			mData.IsFromRegisterSuccess = false;
		}

		protected override void OnClose()
		{
		}

		private async void OnLoginButtonClicked()
		{
			string username = mUsernameInput?.text ?? string.Empty;
			string password = mPasswordInput?.text ?? string.Empty;

			HideErrorMessage();

			if (string.IsNullOrEmpty(username))
			{
				ShowErrorMessage("请输入用户名");
				return;
			}

			if (string.IsNullOrEmpty(password))
			{
				ShowErrorMessage("请输入密码");
				return;
			}

			if (mLoginButton != null)
				mLoginButton.interactable = false;

			await System.Threading.Tasks.Task.Delay(500);

			if (!AccountManager.Instance.AccountExists(username))
			{
				ShowErrorMessage($"账号 {username} 不存在");
				if (mLoginButton != null)
					mLoginButton.interactable = true;
				return;
			}

			bool success = AccountManager.Instance.Login(username, password);

			if (success)
			{
				var account = AccountManager.Instance.CurrentAccount;
				LogKit.Log($"[LoginPanel] 登录成功！欢迎回来，{account.AccountId}！");
				AudioManager.Instance.PlaySFX(AudioManager.SFX_LoginSuccess);
				UIKit.ClosePanel<LoginPanel>();
				UIKit.OpenPanel<MainMenuPanel>(UILevel.AlwayBottom);
			}
			else
			{
				ShowErrorMessage("密码错误");
			}

			if (mLoginButton != null)
				mLoginButton.interactable = true;
		}

		private void ShowErrorMessage(string message)
		{
			if (mErrorMessageText != null)
			{
				mErrorMessageText.text = message;
				mErrorMessageText.gameObject.SetActive(true);
			}
			LogKit.Warning($"[LoginPanel] {message}");
		}

		private void HideErrorMessage()
		{
			if (mErrorMessageText != null)
			{
				mErrorMessageText.gameObject.SetActive(false);
			}
		}

		private void OnRegisterButtonClicked()
		{
			LogKit.Log("[LoginPanel] 跳转注册界面");
			UIKit.ClosePanel<LoginPanel>();
			UIKit.OpenPanel<RegisterPanel>();
		}
	}
#endregion
}

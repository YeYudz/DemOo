using UnityEngine;
using UnityEngine.UI;
using YokiFrame;

namespace GameUI
{
	public sealed class RegisterPanelData : IUIData
	{
		public string Username { get; set; }
		public string Password { get; set; }
	}

	/// <summary>
	/// 注册面板：校验并创建新账号。
	/// </summary>
	public partial class RegisterPanel : UIPanel
	{
		#region 生命周期与注册操作
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as RegisterPanelData ?? new RegisterPanelData();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as RegisterPanelData ?? mData;
			
			if (mUsernameInput != null)
				mUsernameInput.text = mData.Username;
			if (mPasswordInput != null)
				mPasswordInput.text = mData.Password;

			if (mErrorMessageText != null)
			{
				mErrorMessageText.gameObject.SetActive(true);
				mErrorMessageText.text = "注册您的账号";
			}
		}

		protected override void OnShow()
		{
			if (mRegisterButton != null)
			{
				mRegisterButton.onClick.RemoveAllListeners();
				mRegisterButton.onClick.AddListener(OnRegisterButtonClicked);
			}

			if (mBackButton != null)
			{
				mBackButton.onClick.RemoveAllListeners();
				mBackButton.onClick.AddListener(OnBackButtonClicked);
			}
		}

		protected override void OnHide()
		{
			if (mRegisterButton != null)
				mRegisterButton.onClick.RemoveAllListeners();
			if (mBackButton != null)
				mBackButton.onClick.RemoveAllListeners();
				
			mErrorMessageText.text = "注册您的账号";
		}

		protected override void OnClose()
		{
		}

		private async void OnRegisterButtonClicked()
		{
			string username = mUsernameInput?.text ?? string.Empty;
			string password = mPasswordInput?.text ?? string.Empty;

			HideErrorMessage();

			if (string.IsNullOrEmpty(username))
			{
				ShowErrorMessage("请输入用户名");
				return;
			}

			if (username.Length < 3)
			{
				ShowErrorMessage("用户名至少需要3个字符");
				return;
			}

			if (AccountManager.Instance.AccountExists(username))
			{
				ShowErrorMessage("用户名已被注册");
				return;
			}

			if (string.IsNullOrEmpty(password))
			{
				ShowErrorMessage("请输入密码");
				return;
			}

			if (password.Length < 6)
			{
				ShowErrorMessage("密码至少需要6个字符");
				return;
			}

			if (mRegisterButton != null)
				mRegisterButton.interactable = false;

			await System.Threading.Tasks.Task.Delay(500);

			bool success = AccountManager.Instance.Register(username, password);

			if (success)
			{
				LogKit.Log("[RegisterPanel] 注册成功！请登录");
				UIKit.ClosePanel<RegisterPanel>();
				UIKit.OpenPanel<LoginPanel>(data: new LoginPanelData { Username = username, IsFromRegisterSuccess = true });
			}
			else
			{
				ShowErrorMessage("注册失败");
			}

			if (mRegisterButton != null)
				mRegisterButton.interactable = true;
		}

		private void ShowErrorMessage(string message)
		{
			if (mErrorMessageText != null)
			{
				mErrorMessageText.text = message;
				mErrorMessageText.gameObject.SetActive(true);
			}
			LogKit.Warning($"[RegisterPanel] {message}");
		}

		private void HideErrorMessage()
		{
			if (mErrorMessageText != null)
			{
				mErrorMessageText.gameObject.SetActive(false);
			}
		}

		private void OnBackButtonClicked()
		{
			LogKit.Log("[RegisterPanel] 返回登录界面");
			UIKit.ClosePanel<RegisterPanel>();
			UIKit.OpenPanel<LoginPanel>();
		}
		#endregion
	}
}

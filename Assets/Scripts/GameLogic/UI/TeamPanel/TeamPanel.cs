using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using System.Collections.Generic;
using System.Linq;
using static GameUI.UIEvents;

namespace GameUI
{
	/// <summary>
	/// 队伍面板的初始化与打开数据。
	/// </summary>
	public sealed class TeamPanelData : IUIData
	{
		public List<string> SelectedCharacterIds { get; set; } = new List<string>();
		public string DungeonId { get; set; }
	}

	/// <summary>
	/// 队伍编成面板，负责角色选择与队伍确认。
	/// </summary>
	public partial class TeamPanel : UIPanel
	{
		private List<AccountItemData> mOwnedCharacters = new List<AccountItemData>();
		private string[] mSelectedCharacters = new string[MAX_TEAM_SIZE];
		private const int MAX_TEAM_SIZE = 4;
		private Font mDefaultFont;

		#region 生命周期

		/// <summary>
		/// 初始化队伍面板数据与默认字体。
		/// </summary>
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as TeamPanelData ?? new TeamPanelData();
			LoadDefaultFont();
		}

		/// <summary>
		/// 打开面板并恢复已选择的角色。
		/// </summary>
		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as TeamPanelData ?? mData;
			mSelectedCharacters = new string[MAX_TEAM_SIZE];
			for (int i = 0; i < mData.SelectedCharacterIds.Count && i < MAX_TEAM_SIZE; i++)
			{
				mSelectedCharacters[i] = mData.SelectedCharacterIds[i];
			}
			LoadOwnedCharacters();
			RefreshUI();
		}

		/// <summary>
		/// 显示面板并绑定按钮事件。
		/// </summary>
		protected override void OnShow()
		{
			if (mConfirmButton != null)
			{
				mConfirmButton.onClick.RemoveAllListeners();
				mConfirmButton.onClick.AddListener(OnConfirmButtonClicked);
			}

			if (mBackButton != null)
			{
				mBackButton.onClick.RemoveAllListeners();
				mBackButton.onClick.AddListener(OnBackButtonClicked);
			}
		}

		/// <summary>
		/// 隐藏面板并移除按钮事件。
		/// </summary>
		protected override void OnHide()
		{
			if (mConfirmButton != null)
				mConfirmButton.onClick.RemoveAllListeners();
			if (mBackButton != null)
				mBackButton.onClick.RemoveAllListeners();
		}

		/// <summary>
		/// 关闭队伍面板。
		/// </summary>
		protected override void OnClose()
		{
		}

		#endregion

		#region 角色加载

		/// <summary>
		/// 加载运行时使用的默认字体。
		/// </summary>
		private void LoadDefaultFont()
		{
			try
			{
				mDefaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
			}
			catch
			{
				mDefaultFont = null;
			}

			if (mDefaultFont == null)
			{
				Font[] fonts = Resources.FindObjectsOfTypeAll<Font>();
				if (fonts.Length > 0)
					mDefaultFont = fonts[0];
			}
		}

		/// <summary>
		/// 加载当前账号拥有的角色及其基础属性。
		/// </summary>
		private void LoadOwnedCharacters()
		{
			mOwnedCharacters.Clear();

			if (!AccountManager.Instance.IsLoggedIn)
				return;

			string accountId = AccountManager.Instance.CurrentAccount.AccountId;
			// 读取账号拥有的角色，并补齐配置中的基础属性。
			List<AccountItemData> accountCharacters = AccountManager.Instance.GetItemsByType(accountId, "Character");

			foreach (var accountChar in accountCharacters)
			{
				var config = GameDataMgr.Instance.playerCharacterConfigs.Characters.FirstOrDefault(c => c.CharacterId == accountChar.ItemId);
				if (config != null)
				{
					if (!accountChar.Attributes.ContainsKey("MaxHp"))
						accountChar.Attributes["MaxHp"] = config.MaxHp;
					if (!accountChar.Attributes.ContainsKey("Speed"))
						accountChar.Attributes["Speed"] = config.Speed;
					if (!accountChar.Attributes.ContainsKey("Attack"))
						accountChar.Attributes["Attack"] = config.Attack;
					if (!accountChar.Attributes.ContainsKey("Defense"))
						accountChar.Attributes["Defense"] = config.Defense;
					if (!accountChar.Attributes.ContainsKey("CritRate"))
						accountChar.Attributes["CritRate"] = config.CritRate;
					if (!accountChar.Attributes.ContainsKey("CritDamage"))
						accountChar.Attributes["CritDamage"] = config.CritDamage;
					if (!accountChar.Attributes.ContainsKey("DamageBonus"))
						accountChar.Attributes["DamageBonus"] = config.DamageBonus;
					if (!accountChar.Attributes.ContainsKey("ResistancePenetration"))
						accountChar.Attributes["ResistancePenetration"] = config.ResistancePenetration;
				}
				mOwnedCharacters.Add(accountChar);
			}
		}

		#endregion

		#region 列表与槽位刷新

		/// <summary>
		/// 刷新角色列表、队伍槽位和确认按钮状态。
		/// </summary>
		private void RefreshUI()
		{
			RefreshCharacterList();
			RefreshTeamSlots();
			UpdateConfirmButtonState();
		}

		/// <summary>
		/// 刷新拥有角色的列表显示。
		/// </summary>
		private void RefreshCharacterList()
		{
			if (mCharacterListPanel == null)
				return;

			Transform content = mCharacterListPanel.Find("Viewport/Content");
			if (content == null)
			{
				content = mCharacterListPanel;
			}

			foreach (Transform child in content)
			{
				Object.Destroy(child.gameObject);
			}

			GridLayoutGroup layout = content.GetComponent<GridLayoutGroup>();
			if (layout == null)
			{
				layout = content.gameObject.AddComponent<GridLayoutGroup>();
			}

			RectTransform contentRt = content.GetComponent<RectTransform>();
			float containerWidth = contentRt.rect.width;
			int maxColumns = 5;
			float paddingX = 5f;
			float paddingY = 5f;

			float totalSpacing = paddingX * (maxColumns + 1);
			float cellWidth = (containerWidth - totalSpacing) / maxColumns;
			float cellHeight = cellWidth * 0.85f;

			layout.cellSize = new Vector2(cellWidth, cellHeight);
			layout.spacing = new Vector2(paddingX, paddingY);
			layout.padding = new RectOffset((int)paddingX, (int)paddingX, (int)paddingY, (int)paddingY);
			layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
			layout.startAxis = GridLayoutGroup.Axis.Horizontal;
			layout.childAlignment = TextAnchor.UpperLeft;
			layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
			layout.constraintCount = maxColumns;

			ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
			if (fitter == null)
			{
				fitter = content.gameObject.AddComponent<ContentSizeFitter>();
			}
			fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
			fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

			LogKit.Log($"[TeamPanel] 拥有角色数量：{mOwnedCharacters.Count}");

			foreach (var character in mOwnedCharacters)
			{
				CreateCharacterItem(character, content);
			}
		}

		/// <summary>
		/// 创建单个角色列表项并绑定点击事件。
		/// </summary>
		private void CreateCharacterItem(AccountItemData character, Transform parent)
		{
			GameObject characterItem = new GameObject($"Character_{character.ItemId}");
			characterItem.transform.SetParent(parent, false);

			RectTransform rect = characterItem.AddComponent<RectTransform>();

			Image image = characterItem.AddComponent<Image>();
			SpriteIconLoader.SetCharacterIcon(image, character.ItemId);
			if (image.sprite == null)
			{
				image.color = mSelectedCharacters.Contains(character.ItemId) ? Color.green : Color.white;
			}

			Button button = characterItem.AddComponent<Button>();

			// 角色列表只显示头像，不再叠加文本
			string itemId = character.ItemId;
			button.onClick.AddListener(() => OnCharacterClicked(itemId));
		}

		/// <summary>
		/// 刷新队伍槽位中的角色图标与名称。
		/// </summary>
		private void RefreshTeamSlots()
		{
			Button[] roleButtons = { mRole1, mRole2, mRole3, mRole4 };

			for (int i = 0; i < MAX_TEAM_SIZE; i++)
			{
				Button roleBtn = roleButtons[i];
				if (roleBtn == null) continue;

				Image iconImage = roleBtn.GetComponent<Image>();

				Text text = null;
				Transform textTransform = roleBtn.transform.Find("Text");
				if (textTransform != null)
				{
					text = textTransform.GetComponent<Text>();
				}

				roleBtn.onClick.RemoveAllListeners();

				string charId = mSelectedCharacters[i];
				if (charId != null)
				{
					var character = mOwnedCharacters.Find(c => c.ItemId == charId);
					int position = i;

					if (iconImage != null)
					{
						iconImage.gameObject.SetActive(true);
						SpriteIconLoader.SetCharacterIcon(iconImage, charId);
						if (iconImage.sprite == null)
						{
							iconImage.color = GetCharacterIconColor(charId);
						}
					}

					if (text != null)
					{
						text.gameObject.SetActive(true);
						text.text = character != null ? character.ItemId : charId;
					}

					roleBtn.interactable = true;
					roleBtn.onClick.AddListener(() => RemoveFromTeamAtPosition(position));
				}
				else
				{
					if (iconImage != null)
					{
						iconImage.gameObject.SetActive(true);
						// 空位：置换为 TeamAdd 占位图，清除上一角色的残留图片
						SpriteIconLoader.SetOtherIcon(iconImage, "TeamAdd");
						if (iconImage.sprite == null)
						{
							// 资源缺失时彻底清空残影：sprite=null + 不透明灰色
							iconImage.color = Color.gray;
						}
					}

					if (text != null)
					{
						text.gameObject.SetActive(true);
						text.text = "空";
					}

					roleBtn.interactable = false;
				}
			}
		}

		/// <summary>
		/// 根据角色标识获取无图标时使用的颜色。
		/// </summary>
		private Color GetCharacterIconColor(string characterId)
		{
			switch (characterId)
			{
				case "Warrior": return new Color(1f, 0.3f, 0.3f);
				case "Mage": return new Color(0.3f, 0.3f, 1f);
				case "Archer": return new Color(0.3f, 1f, 0.3f);
				case "Healer": return new Color(1f, 1f, 0.3f);
				default: return Color.gray;
			}
		}
		#endregion

		#region 角色选择与移除

		/// <summary>
		/// 处理角色选择或取消选择。
		/// </summary>
		private void OnCharacterClicked(string characterId)
		{
			for (int i = 0; i < MAX_TEAM_SIZE; i++)
			{
				if (mSelectedCharacters[i] == characterId)
				{
					RemoveFromTeamAtPosition(i);
					RefreshUI();
					return;
				}
			}

			for (int i = 0; i < MAX_TEAM_SIZE; i++)
			{
				if (mSelectedCharacters[i] == null)
				{
					mSelectedCharacters[i] = characterId;
					RefreshUI();
					return;
				}
			}

			LogKit.Warning("[TeamPanel] 队伍已满，最多4人");
		}

		/// <summary>
		/// 按角色标识从队伍中移除角色。
		/// </summary>
		private void RemoveFromTeam(string characterId)
		{
			for (int i = 0; i < MAX_TEAM_SIZE; i++)
			{
				if (mSelectedCharacters[i] == characterId)
				{
					RemoveFromTeamAtPosition(i);
					return;
				}
			}
		}

		/// <summary>
		/// 按槽位移除队伍中的角色。
		/// </summary>
		private void RemoveFromTeamAtPosition(int position)
		{
			if (position >= 0 && position < MAX_TEAM_SIZE)
			{
				mSelectedCharacters[position] = null;
				RefreshUI();
			}
		}

		/// <summary>
		/// 根据队伍是否有角色更新确认按钮状态。
		/// </summary>
		private void UpdateConfirmButtonState()
		{
			if (mConfirmButton != null)
			{
				bool hasCharacters = false;
				for (int i = 0; i < MAX_TEAM_SIZE; i++)
				{
					if (mSelectedCharacters[i] != null)
					{
						hasCharacters = true;
						break;
					}
				}
				mConfirmButton.interactable = hasCharacters;
			}
		}

		#endregion

		#region 确认与返回

		/// <summary>
		/// 确认当前队伍并发送战斗开始事件。
		/// </summary>
		private void OnConfirmButtonClicked()
		{
			List<string> validCharacters = new List<string>();
			for (int i = 0; i < MAX_TEAM_SIZE; i++)
			{
				if (mSelectedCharacters[i] != null)
				{
					validCharacters.Add(mSelectedCharacters[i]);
				}
			}

			if (validCharacters.Count == 0)
				return;

			LogKit.Log($"[TeamPanel] 编队完成：{string.Join(", ", validCharacters)}");

			string dungeonId = mData is TeamPanelData tpData ? tpData.DungeonId : "";

			EventKit.Type.Send(new BattleStartEvent
			{
				SelectedCharacterIds = validCharacters,
				OwnedCharacters = mOwnedCharacters,
				DungeonId = dungeonId
			});
		}

		/// <summary>
		/// 返回副本选择面板。
		/// </summary>
		private void OnBackButtonClicked()
		{
			LogKit.Log("[TeamPanel] 返回副本选择");
			UIKit.ClosePanel<TeamPanel>();
			UIKit.OpenPanel<DungeonSelectPanel>();
		}

		#endregion
	}
}

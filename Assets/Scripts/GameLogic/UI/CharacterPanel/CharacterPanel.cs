using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GameUI
{
	public sealed class CharacterPanelData : IUIData
	{
		public string SelectedCharacterId { get; set; }
		public string OpenerPanelType { get; set; }
	}

	/// <summary>
	/// 角色面板：管理角色属性、装备、星魂和天赋。
	/// </summary>
	public partial class CharacterPanel : UIPanel
	{
		#region 生命周期与角色详情
		private List<AccountItemData> mCharacters = new List<AccountItemData>();
		private Font mDefaultFont;
		private AccountItemData mSelectedCharacter;
		private string mCurrentTab = "Attributes";
		private string mCurrentSkillType = "Normal";
		private int mCurrentStarLevel = 0;

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as CharacterPanelData ?? new CharacterPanelData();
			LoadDefaultFont();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as CharacterPanelData ?? mData;
			LoadCharacters();
			InitializeStarTalentUI();
			RefreshUI();
			RegisterButtonEvents();
			RegisterEventListeners();

			if (mCharacters.Count > 0)
			{
				mSelectedCharacter = mCharacters[0];
				UpdateTabContent(mSelectedCharacter);
			}
		}

		protected override void OnShow()
		{
			SetPanelSortingOrder();

			if (mBackButton != null)
			{
				mBackButton.onClick.RemoveAllListeners();
				mBackButton.onClick.AddListener(OnBackButtonClicked);
			}
			UpdatePanelVisibility();

			// 从子面板返回时刷新角色数据（如装备武器后立即更新显示）
			if (mSelectedCharacter != null)
			{
				string accountId = AccountManager.Instance.IsLoggedIn ? AccountManager.Instance.CurrentAccount.AccountId : "";
				var freshData = AccountManager.Instance.GetAccountItem(accountId, "Character", mSelectedCharacter.ItemId);
				if (freshData != null)
				{
					mSelectedCharacter = freshData;
					UpdateTabContent(mSelectedCharacter);
				}
			}
		}

		protected override void OnHide()
		{
			if (mBackButton != null)
				mBackButton.onClick.RemoveAllListeners();
		}

		protected override void OnClose()
		{
			mSelectedCharacter = null;
			mCurrentTab = "Attributes";
			mCurrentSkillType = "Normal";
			mCurrentStarLevel = 0;
			UnregisterEventListeners();
		}

		private void RegisterButtonEvents()
		{
			if (mBtnMoreAttr != null)
			{
				mBtnMoreAttr.onClick.RemoveAllListeners();
				mBtnMoreAttr.onClick.AddListener(OnBtnMoreAttrClicked);
			}

			if (mBtnWeaponSlot != null)
			{
				mBtnWeaponSlot.onClick.RemoveAllListeners();
				mBtnWeaponSlot.onClick.AddListener(OnBtnWeaponClicked);
			}

			if (mBtnWeaponEquip != null)
			{
				mBtnWeaponEquip.onClick.RemoveAllListeners();
				mBtnWeaponEquip.onClick.AddListener(OnBtnEquippedClicked);
			}

			if (mBtnWeaponImage != null)
			{
				mBtnWeaponImage.onClick.RemoveAllListeners();
				mBtnWeaponImage.onClick.AddListener(OnBtnWeaponImageClicked);
			}

			if (mBtnWeaponUpgrade != null)
			{
				mBtnWeaponUpgrade.onClick.RemoveAllListeners();
				mBtnWeaponUpgrade.onClick.AddListener(OnBtnWeaponUpgradeClicked);
			}

			if (mBtnHead != null)
				{
					mBtnHead.onClick.RemoveAllListeners();
					mBtnHead.onClick.AddListener(() => OnRelicSlotClicked("Head"));
				}

			if (mBtnHand != null)
				{
					mBtnHand.onClick.RemoveAllListeners();
					mBtnHand.onClick.AddListener(() => OnRelicSlotClicked("Hand"));
				}

			if (mBtnBody != null)
			{
				mBtnBody.onClick.RemoveAllListeners();
				mBtnBody.onClick.AddListener(() => OnRelicSlotClicked("Body"));
			}

			if (mBtnFeet != null)
				{
					mBtnFeet.onClick.RemoveAllListeners();
					mBtnFeet.onClick.AddListener(() => OnRelicSlotClicked("Feet"));
				}

			if (mBtnStar1 != null)
			{
				mBtnStar1.onClick.RemoveAllListeners();
				mBtnStar1.onClick.AddListener(() => OnStarClicked(1));
			}

			if (mBtnStar2 != null)
			{
				mBtnStar2.onClick.RemoveAllListeners();
				mBtnStar2.onClick.AddListener(() => OnStarClicked(2));
			}

			if (mBtnStar3 != null)
			{
				mBtnStar3.onClick.RemoveAllListeners();
				mBtnStar3.onClick.AddListener(() => OnStarClicked(3));
			}

			if (mBtnStar4 != null)
			{
				mBtnStar4.onClick.RemoveAllListeners();
				mBtnStar4.onClick.AddListener(() => OnStarClicked(4));
			}

			if (mBtnStar5 != null)
			{
				mBtnStar5.onClick.RemoveAllListeners();
				mBtnStar5.onClick.AddListener(() => OnStarClicked(5));
			}

			if (mBtnStarUnlock != null)
			{
				mBtnStarUnlock.onClick.RemoveAllListeners();
				mBtnStarUnlock.onClick.AddListener(OnBtnLockClicked);
			}

			if (mTogNormal != null)
			{
				mTogNormal.onValueChanged.RemoveAllListeners();
				mTogNormal.onValueChanged.AddListener((bool value) => { if (value) OnSkillToggleChanged("Normal"); });
			}

			if (mTogSkill1 != null)
			{
				mTogSkill1.onValueChanged.RemoveAllListeners();
				mTogSkill1.onValueChanged.AddListener((bool value) => { if (value) OnSkillToggleChanged("Skill1"); });
			}

			if (mTogSkill2 != null)
			{
				mTogSkill2.onValueChanged.RemoveAllListeners();
				mTogSkill2.onValueChanged.AddListener((bool value) => { if (value) OnSkillToggleChanged("Skill2"); });
			}

			if (mTogUltimate != null)
			{
				mTogUltimate.onValueChanged.RemoveAllListeners();
				mTogUltimate.onValueChanged.AddListener((bool value) => { if (value) OnSkillToggleChanged("Ultimate"); });
			}

			if (mBtnTalentUpgrade != null)
			{
				mBtnTalentUpgrade.onClick.RemoveAllListeners();
				mBtnTalentUpgrade.onClick.AddListener(OnTalentUpgradeClicked);
			}

			if (mBtnAllAttrClose != null)
			{
				mBtnAllAttrClose.onClick.RemoveAllListeners();
				mBtnAllAttrClose.onClick.AddListener(OnBtnAllAttrCloseClicked);
			}

			if (mBtnUpgrade != null)
			{
				mBtnUpgrade.onClick.RemoveAllListeners();
				mBtnUpgrade.onClick.AddListener(OnBtnUpgradeClicked);
			}

			InitializeAllAttrUI();
			SetPanelActive(mPanelAllAttr, false);
		}

		private void InitializeAllAttrUI()
		{
			if (mPanelAllAttr == null) return;

			if (mScrollAllAttr == null)
			{
				Transform scrollTrans = mPanelAllAttr.Find("ScrollView");
				if (scrollTrans != null)
				{
					mScrollAllAttr = scrollTrans.GetComponent<ScrollRect>();
				}
			}

			if (mAllAttrContent == null)
			{
				if (mScrollAllAttr != null && mScrollAllAttr.content != null)
				{
					mAllAttrContent = mScrollAllAttr.content;
				}
				else
				{
					Transform contentTrans = mPanelAllAttr.Find("ScrollView/Viewport/Content");
					if (contentTrans != null)
					{
						mAllAttrContent = contentTrans;
					}
				}
			}

			if (mScrollAllAttr != null)
			{
				mScrollAllAttr.horizontal = false;
				mScrollAllAttr.vertical = true;
				mScrollAllAttr.movementType = ScrollRect.MovementType.Clamped;
				mScrollAllAttr.inertia = false;
			}
		}

		private void SetPanelSortingOrder()
		{
			Canvas canvas = GetComponent<Canvas>();
			if (canvas != null && UIManager.Instance != null)
			{
				canvas.sortingOrder = UIManager.Instance.AllocateSortingOrder();
			}
		}

		private void RegisterEventListeners()
		{
			EventKit.Type.Register<UIEvents.WeaponEquippedEvent>(OnWeaponEquipped);
			EventKit.Type.Register<UIEvents.RelicEquippedEvent>(OnRelicEquipped);
			EventKit.Type.Register<UIEvents.CharacterDataChangedEvent>(OnCharacterDataChanged);
			EventKit.Type.Register<UIEvents.StarUnlockResultEvent>(OnStarUnlockResult);
			EventKit.Type.Register<UIEvents.TalentUpgradeResultEvent>(OnTalentUpgradeResult);
		}

		private void UnregisterEventListeners()
		{
			EventKit.Type.UnRegister<UIEvents.WeaponEquippedEvent>(OnWeaponEquipped);
			EventKit.Type.UnRegister<UIEvents.RelicEquippedEvent>(OnRelicEquipped);
			EventKit.Type.UnRegister<UIEvents.CharacterDataChangedEvent>(OnCharacterDataChanged);
			EventKit.Type.UnRegister<UIEvents.StarUnlockResultEvent>(OnStarUnlockResult);
			EventKit.Type.UnRegister<UIEvents.TalentUpgradeResultEvent>(OnTalentUpgradeResult);
		}

		private void OnStarUnlockResult(UIEvents.StarUnlockResultEvent e)
		{
			if (mSelectedCharacter == null || mSelectedCharacter.ItemId != e.CharacterId)
				return;

			if (e.Success && AccountManager.Instance.IsLoggedIn)
			{
				mSelectedCharacter.StarLevel = e.StarLevel;
				LogKit.Log($"[CharacterPanel] 星魂解锁成功 - 星级:{e.StarLevel}");
			}
			ShowStar(mSelectedCharacter);
		}

		private void OnTalentUpgradeResult(UIEvents.TalentUpgradeResultEvent e)
		{
			if (mSelectedCharacter == null || mSelectedCharacter.ItemId != e.CharacterId)
				return;

			if (e.Success && AccountManager.Instance.IsLoggedIn)
			{
				string accountId = AccountManager.Instance.CurrentAccount.AccountId;
				AccountItemData updatedCharacter = AccountManager.Instance.GetAccountItem(accountId, "Character", e.CharacterId);
				if (updatedCharacter != null)
				{
					mSelectedCharacter = updatedCharacter;
					UpdateTabContent(mSelectedCharacter);
					if (mPanelAllAttr != null && mPanelAllAttr.gameObject.activeSelf)
					{
						ShowAllAttributes(mSelectedCharacter);
					}
					LogKit.Log($"[CharacterPanel] 天赋升级成功 - 技能:{e.SkillType}, 等级:{e.NewLevel}");
				}
			}
		}

		private void OnWeaponEquipped(UIEvents.WeaponEquippedEvent e)
		{
			if (mSelectedCharacter != null && mSelectedCharacter.ItemId == e.CharacterId)
			{
				// 重新加载角色数据以获取最新的武器装备状态
				if (AccountManager.Instance.IsLoggedIn)
				{
					string accountId = AccountManager.Instance.CurrentAccount.AccountId;
					AccountItemData updatedCharacter = AccountManager.Instance.GetAccountItem(accountId, "Character", e.CharacterId);
					if (updatedCharacter != null)
					{
						mSelectedCharacter = updatedCharacter;
						UpdateTabContent(mSelectedCharacter);
					}
				}
				else
				{
					UpdateTabContent(mSelectedCharacter);
				}
			}
		}

		private void OnRelicEquipped(UIEvents.RelicEquippedEvent e)
		{
			if (mSelectedCharacter != null && mSelectedCharacter.ItemId == e.CharacterId)
			{
				// 重新加载角色数据以获取最新的属性
				if (AccountManager.Instance.IsLoggedIn)
				{
					string accountId = AccountManager.Instance.CurrentAccount.AccountId;
					AccountItemData updatedCharacter = AccountManager.Instance.GetAccountItem(accountId, "Character", e.CharacterId);
					if (updatedCharacter != null)
					{
						mSelectedCharacter = updatedCharacter;
						UpdateTabContent(mSelectedCharacter);
					}
				}
			}
		}

		private void OnCharacterDataChanged(UIEvents.CharacterDataChangedEvent e)
		{
			if (mSelectedCharacter != null && mSelectedCharacter.ItemId == e.CharacterId)
			{
				// 重新加载角色数据以获取最新的等级、属性等
				if (AccountManager.Instance.IsLoggedIn)
				{
					string accountId = AccountManager.Instance.CurrentAccount.AccountId;
					AccountItemData updatedCharacter = AccountManager.Instance.GetAccountItem(accountId, "Character", e.CharacterId);
					if (updatedCharacter != null)
					{
						mSelectedCharacter = updatedCharacter;
						UpdateTabContent(mSelectedCharacter);
						if (mPanelAllAttr != null && mPanelAllAttr.gameObject.activeSelf)
						{
							ShowAllAttributes(mSelectedCharacter);
						}
						LogKit.Log($"[CharacterPanel] 角色数据变化，刷新显示: {e.CharacterId}");
					}
				}
			}
		}

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

		private void LoadCharacters()
		{
			if (!AccountManager.Instance.IsLoggedIn)
				return;

			string accountId = AccountManager.Instance.CurrentAccount.AccountId;
			mCharacters = AccountManager.Instance.GetItemsByType(accountId, "Character");
		}

		private void InitializeStarTalentUI()
		{
			// 星魂面板：动态查找或创建texStarDetail
			if (mPanelStar != null && mTexStarDetail == null)
			{
				Transform texStarDetailTrans = mPanelStar.Find("texDetail");
				if (texStarDetailTrans != null)
				{
					mTexStarDetail = texStarDetailTrans.GetComponent<Text>();
				}
			}

			// 星魂面板：动态创建材料显示
			if (mPanelStar != null && mStarMaterial == null)
			{
				GameObject materialObj = new GameObject("btnMaterial");
				materialObj.transform.SetParent(mPanelStar, false);
				RectTransform rect = materialObj.AddComponent<RectTransform>();
				rect.sizeDelta = new Vector2(200, 40);
				rect.anchoredPosition = new Vector2(0, -200);
				mStarMaterial = materialObj.transform;

				Image bg = materialObj.AddComponent<Image>();
				bg.color = new Color(0.9f, 0.9f, 0.9f);

				Text text = materialObj.AddComponent<Text>();
				text.font = mDefaultFont;
				text.fontSize = 14;
				text.alignment = TextAnchor.MiddleCenter;
				text.color = Color.black;
				text.text = "";
				text.horizontalOverflow = HorizontalWrapMode.Wrap;
				text.verticalOverflow = VerticalWrapMode.Overflow;
			}

			// 天赋面板：动态创建3个材料显示
			if (mPanelTalent != null)
			{
				if (mTalentMaterial1 == null)
					mTalentMaterial1 = CreateTalentMaterial(mPanelTalent, 0);
				if (mTalentMaterial2 == null)
					mTalentMaterial2 = CreateTalentMaterial(mPanelTalent, 1);
				if (mTalentMaterial3 == null)
					mTalentMaterial3 = CreateTalentMaterial(mPanelTalent, 2);
			}
		}

		private Transform CreateTalentMaterial(Transform parent, int index)
		{
			GameObject materialObj = new GameObject($"btnMaterial{index + 1}");
			materialObj.transform.SetParent(parent, false);
			RectTransform rect = materialObj.AddComponent<RectTransform>();
			rect.sizeDelta = new Vector2(180, 35);
			rect.anchoredPosition = new Vector2(0, -150 - index * 40);
			rect.anchorMin = new Vector2(0.5f, 1f);
			rect.anchorMax = new Vector2(0.5f, 1f);
			rect.pivot = new Vector2(0.5f, 1f);

			Image bg = materialObj.AddComponent<Image>();
			bg.color = new Color(0.9f, 0.9f, 0.9f);

			Text text = materialObj.AddComponent<Text>();
			text.font = mDefaultFont;
			text.fontSize = 14;
			text.alignment = TextAnchor.MiddleCenter;
			text.color = Color.black;
			text.text = "";
			text.horizontalOverflow = HorizontalWrapMode.Wrap;
			text.verticalOverflow = VerticalWrapMode.Overflow;

			return materialObj.transform;
		}

		private void RefreshUI()
		{
			RefreshCharacterList();
			RefreshTabPanel();
			ClearDetail();
			UpdatePanelVisibility();
		}

		private void RefreshCharacterList()
		{
			if (mCharacterList == null || Object.ReferenceEquals(mCharacterList, null))
				return;

			Transform content = null;
			try
			{
				content = mCharacterList.Find("Viewport/Content");
			}
			catch (MissingReferenceException)
			{
				return;
			}

			if (content == null)
			{
				content = mCharacterList;
			}

			List<GameObject> childrenToDestroy = new List<GameObject>();
			foreach (Transform child in content)
			{
				childrenToDestroy.Add(child.gameObject);
			}
			foreach (var child in childrenToDestroy)
			{
				Object.Destroy(child);
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

			ScrollRect scrollRect = mCharacterList.GetComponent<ScrollRect>();
			if (scrollRect != null)
			{
				scrollRect.horizontal = false;
				scrollRect.vertical = true;
				scrollRect.movementType = ScrollRect.MovementType.Clamped;
				scrollRect.inertia = false;
			}

			LogKit.Log($"[CharacterPanel] 角色数量：{mCharacters.Count}");

			foreach (var character in mCharacters)
			{
				CreateCharacterItem(character, content);
			}
		}

		private void CreateCharacterItem(AccountItemData character, Transform parent)
		{
			GameObject item = new GameObject(character.ItemId);
			item.transform.SetParent(parent, false);

			RectTransform rect = item.AddComponent<RectTransform>();

			Image image = item.AddComponent<Image>();
			SpriteIconLoader.SetCharacterIcon(image, character.ItemId);
			if (image.sprite == null)
			{
				image.color = GetCharacterIconColor(character.ItemId);
			}

			Button button = item.AddComponent<Button>();

			// 角色列表只显示头像，不再叠加文本
			button.onClick.AddListener(() => OnCharacterClicked(character));
		}

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

		private Color GetRelicRarityColor(int rarity)
		{
			switch (rarity)
			{
				case 5: return new Color(1f, 0.85f, 0.3f);
				case 4: return new Color(0.7f, 0.4f, 0.8f);
				case 3: return new Color(0.3f, 0.5f, 0.9f);
				case 2: return new Color(0.2f, 0.7f, 0.3f);
				default: return Color.gray;
			}
		}

		private void ClearDetail()
		{
			if (mImgIcon != null)
				mImgIcon.color = Color.clear;

			if (mTexName != null)
				mTexName.text = "";

			if (mTexBaseAttr != null)
				mTexBaseAttr.text = "";

			if (mImgWeapon != null)
				mImgWeapon.color = Color.clear;

			if (mTexDetail != null)
				mTexDetail.text = "";

			if (mTexRelicAttr != null)
				mTexRelicAttr.text = "";

			if (mTexSetEffect != null)
				mTexSetEffect.text = "";

			if (mTexTalentDetail != null)
				mTexTalentDetail.text = "";
		}

		private void OnBackButtonClicked()
		{
			LogKit.Log("[CharacterPanel] 返回上一级面板");
			var characterPanelData = mData as CharacterPanelData;
			if (characterPanelData != null && characterPanelData.OpenerPanelType == "MainMenuPanel")
			{
				UIKit.ClosePanel<CharacterPanel>();
			}
			else
			{
				UIKit.ClosePanel<CharacterPanel>();
				UIKit.OpenPanel<MainMenuPanel>(UILevel.AlwayBottom);
			}
		}

		private void OnBtnMoreAttrClicked()
		{
			if (mPanelAllAttr == null) return;

			if (mSelectedCharacter != null)
			{
				ShowAllAttributes(mSelectedCharacter);
			}

			SetPanelActive(mPanelAllAttr, true);
			LogKit.Log("[CharacterPanel] 打开详细属性面板");
		}

		private void OnBtnAllAttrCloseClicked()
		{
			if (mPanelAllAttr == null) return;
			SetPanelActive(mPanelAllAttr, false);
			LogKit.Log("[CharacterPanel] 关闭详细属性面板");
		}

		private const float ATTR_ROW_HEIGHT = 46f;

		private void ShowAllAttributes(AccountItemData character)
		{
			if (mAllAttrContent == null) return;

			List<GameObject> childrenToDestroy = new List<GameObject>();
			foreach (Transform child in mAllAttrContent)
			{
				childrenToDestroy.Add(child.gameObject);
			}
			foreach (var child in childrenToDestroy)
			{
				Object.Destroy(child);
			}

			float containerWidth = mAllAttrContent.GetComponent<RectTransform>().rect.width;
			float yOffset = 0f;

			yOffset = AddAttributeRow("等级", character.Level.ToString(), containerWidth, yOffset);
			yOffset = AddAttributeRow("经验", $"{character.Exp}/{character.ExpToNextLevel}", containerWidth, yOffset);

			var finalStats = UpgradeSystem.Instance.CalculateFinalStats(
				AccountManager.Instance.CurrentAccount.AccountId, character.ItemId);

			if (finalStats.ContainsKey("MaxHp"))
				yOffset = AddAttributeRow("生命值", Mathf.RoundToInt(finalStats["MaxHp"]).ToString(), containerWidth, yOffset);
			if (finalStats.ContainsKey("Attack"))
				yOffset = AddAttributeRow("攻击力", Mathf.RoundToInt(finalStats["Attack"]).ToString(), containerWidth, yOffset);
			if (finalStats.ContainsKey("Defense"))
				yOffset = AddAttributeRow("防御力", Mathf.RoundToInt(finalStats["Defense"]).ToString(), containerWidth, yOffset);
			if (finalStats.ContainsKey("Speed"))
				yOffset = AddAttributeRow("速度", Mathf.RoundToInt(finalStats["Speed"]).ToString(), containerWidth, yOffset);
			if (finalStats.ContainsKey("CritRate"))
				yOffset = AddAttributeRow("暴击率", $"{Mathf.RoundToInt(finalStats["CritRate"] * 100)}%", containerWidth, yOffset);
			if (finalStats.ContainsKey("CritDamage"))
				yOffset = AddAttributeRow("暴击伤害", $"{Mathf.RoundToInt(finalStats["CritDamage"] * 100)}%", containerWidth, yOffset);
			if (finalStats.ContainsKey("DamageBonus"))
				yOffset = AddAttributeRow("伤害加成", $"{Mathf.RoundToInt(finalStats["DamageBonus"] * 100)}%", containerWidth, yOffset);
			if (!string.IsNullOrEmpty(character.EquippedWeapon))
			{
				var weaponItem = GetEquippedItem("Weapon", character.EquippedWeapon);
				var weaponConfig = GameDataMgr.Instance.lightConeConfigs.Weapons
					.FirstOrDefault(w => weaponItem != null && w.WeaponId == weaponItem.ItemId);
				if (weaponConfig != null)
					yOffset = AddAttributeRow("装备武器", weaponConfig.Name, containerWidth, yOffset);
			}

			if (character.EquippedRelics != null)
			{
				foreach (var relic in character.EquippedRelics)
				{
					var relicItem = GetEquippedItem("Relic", relic.Value);
					var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
						.FirstOrDefault(r => relicItem != null && r.RelicId == relicItem.ItemId);
					if (relicConfig != null)
						yOffset = AddAttributeRow($"遗器[{relic.Key}]", relicConfig.Name, containerWidth, yOffset);
				}
			}

			yOffset = AddAttributeRow("星魂等级", character.StarLevel.ToString(), containerWidth, yOffset);

			var talentSkills = new[] { "NormalAttack", "Skill1", "Skill2", "Ultimate" };
			foreach (var skill in talentSkills)
			{
				int talentLevel = character.GetTalentLevel(skill);
				if (talentLevel > 1)
					yOffset = AddAttributeRow($"天赋[{skill}]", talentLevel.ToString(), containerWidth, yOffset);
			}

			RectTransform contentRect = mAllAttrContent.GetComponent<RectTransform>();
			contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, yOffset);
		}

		private float AddAttributeRow(string attrName, string attrValue, float containerWidth, float yOffset)
		{
			GameObject rowObj = new GameObject($"AttrRow_{attrName}");
			rowObj.transform.SetParent(mAllAttrContent, false);

			RectTransform rect = rowObj.AddComponent<RectTransform>();
			rect.anchorMin = new Vector2(0, 1);
			rect.anchorMax = new Vector2(0, 1);
			rect.pivot = new Vector2(0, 1);
			rect.sizeDelta = new Vector2(containerWidth, ATTR_ROW_HEIGHT);
			rect.anchoredPosition = new Vector2(0, -yOffset);

			Image bg = rowObj.AddComponent<Image>();
			bg.color = new Color(0.95f, 0.95f, 0.95f);

			GameObject nameObj = new GameObject("Name");
			nameObj.transform.SetParent(rowObj.transform, false);
			RectTransform nameRect = nameObj.AddComponent<RectTransform>();
			nameRect.anchorMin = new Vector2(0, 0);
			nameRect.anchorMax = new Vector2(0, 1);
			nameRect.pivot = new Vector2(0, 0.5f);
			nameRect.sizeDelta = new Vector2(containerWidth * 0.5f, 0);
			nameRect.anchoredPosition = new Vector2(10, 0);
			Text nameText = nameObj.AddComponent<Text>();
			nameText.font = mDefaultFont;
			nameText.fontSize = 14;
			nameText.alignment = TextAnchor.MiddleLeft;
			nameText.color = Color.black;
			nameText.text = attrName;

			GameObject valueObj = new GameObject("Value");
			valueObj.transform.SetParent(rowObj.transform, false);
			RectTransform valueRect = valueObj.AddComponent<RectTransform>();
			valueRect.anchorMin = new Vector2(1, 0);
			valueRect.anchorMax = new Vector2(1, 1);
			valueRect.pivot = new Vector2(1, 0.5f);
			valueRect.sizeDelta = new Vector2(containerWidth * 0.5f, 0);
			valueRect.anchoredPosition = new Vector2(-10, 0);
			Text valueText = valueObj.AddComponent<Text>();
			valueText.font = mDefaultFont;
			valueText.fontSize = 14;
			valueText.alignment = TextAnchor.MiddleRight;
			valueText.color = new Color(0.2f, 0.4f, 0.8f);
			valueText.text = attrValue;

			return yOffset + ATTR_ROW_HEIGHT;
		}

		private void OnBtnWeaponClicked()
		{
			LogKit.Log("[CharacterPanel] 切换到武器页面");
			mCurrentTab = "Weapon";
			UpdateTabContent(mSelectedCharacter);
			RefreshTabPanel();
		}

		private void OnBtnWeaponImageClicked()
		{
			if (mSelectedCharacter == null) return;

			string characterId = mSelectedCharacter.ItemId;
			CharacterPanelData openerData = mData as CharacterPanelData;

			LogKit.Log("[CharacterPanel] 打开武器选择列表，为角色 " + characterId + " 选择武器");
			UIKit.OpenPanel<sonWeaponPanel>(UILevel.Pop, data: new sonWeaponPanelData 
			{ 
				TargetCharacterId = characterId,
				OpenerPanelType = "CharacterPanel",
				OpenerData = openerData
			});
		}

		private void OnBtnEquippedClicked()
		{
			if (mSelectedCharacter == null) return;

			if (mCurrentTab == "Weapon" || mCurrentTab == "Attributes")
			{
				OnBtnWeaponImageClicked();
			}
		}

		private void OnBtnWeaponUpgradeClicked()
		{
			if (mSelectedCharacter == null) return;
			if (string.IsNullOrEmpty(mSelectedCharacter.EquippedWeapon)) return;

			string weaponInstanceId = mSelectedCharacter.EquippedWeapon;
			var weaponItem = GetEquippedItem("Weapon", weaponInstanceId);
			if (weaponItem == null) return;

			string characterId = mSelectedCharacter.ItemId;
			CharacterPanelData openerData = mData as CharacterPanelData;

			LogKit.Log("[CharacterPanel] 打开武器升级面板，武器: " + weaponItem.ItemId);
			UIKit.OpenPanel<sonWeaponUpgradePanel>(UILevel.Pop, data: new sonWeaponUpgradePanelData 
			{ 
				WeaponId = weaponItem.ItemId,
				WeaponInstanceId = weaponInstanceId,
				TargetCharacterId = characterId,
				OpenerPanelType = "CharacterPanel",
				OpenerData = openerData
			});
		}

		private void OnRelicSlotClicked(string slot)
		{
			if (mSelectedCharacter == null) return;

			string characterId = mSelectedCharacter.ItemId;
			CharacterPanelData openerData = mData as CharacterPanelData;

			LogKit.Log($"[CharacterPanel] 打开遗器选择列表，为角色 {characterId} 选择 {slot} 部位遗器");
			UIKit.OpenPanel<sonRelicPanel>(UILevel.Pop, data: new sonRelicPanelData 
			{ 
				TargetCharacterId = characterId,
				TargetRelicSlot = slot,
				OpenerPanelType = "CharacterPanel",
				OpenerData = openerData
			});
		}

		private string NormalizeRelicSlot(string slot)
		{
			switch (slot)
			{
				case "Hand": return "Hands";
				case "Hands": return "Hands";
				default: return slot;
			}
		}

		private void OnStarClicked(int starLevel)
		{
			mCurrentStarLevel = starLevel;
			if (mSelectedCharacter != null)
			{
				ShowStar(mSelectedCharacter);
			}
		}

		private void OnBtnLockClicked()
		{
			if (mSelectedCharacter == null) return;
			if (mCurrentStarLevel < 1 || mCurrentStarLevel > 5) return;

			int starLevel = mSelectedCharacter.StarLevel;
			if (mCurrentStarLevel <= starLevel) return;
			if (mCurrentStarLevel > starLevel + 1) return;

			if (AccountManager.Instance.IsLoggedIn)
			{
				LogKit.Log($"[CharacterPanel] 请求解锁星魂 {mCurrentStarLevel}");
				EventKit.Type.Send(new UIEvents.StarUnlockRequestedEvent
				{
					CharacterId = mSelectedCharacter.ItemId,
					StarLevel = mCurrentStarLevel
				});
			}
			else
			{
				ShowStar(mSelectedCharacter);
			}
		}

		private void OnSkillToggleChanged(string skillType)
		{
			SetPanelActive(mPanelAllAttr, false);
			mCurrentSkillType = skillType;
			if (mSelectedCharacter != null)
			{
				ShowTalent(mSelectedCharacter);
			}
		}

		private string GetConfigSkillType(string uiSkillType)
		{
			if (uiSkillType == "Normal")
				return "NormalAttack";
			return uiSkillType;
		}

		private void OnTalentUpgradeClicked()
		{
			if (mCurrentTab == "Talent" && mSelectedCharacter != null)
			{
				if (AccountManager.Instance.IsLoggedIn)
				{
					string configSkillType = GetConfigSkillType(mCurrentSkillType);
					LogKit.Log($"[CharacterPanel] 请求升级 {mCurrentSkillType} 天赋");
					EventKit.Type.Send(new UIEvents.TalentUpgradeRequestedEvent
					{
						CharacterId = mSelectedCharacter.ItemId,
						SkillType = configSkillType
					});
				}
				else
				{
					ShowTalent(mSelectedCharacter);
				}
			}
		}

		private void OnBtnUpgradeClicked()
		{
			if (mCurrentTab != "Attributes" || mSelectedCharacter == null) return;

			string characterId = mSelectedCharacter.ItemId;
			CharacterPanelData openerData = mData as CharacterPanelData;

			LogKit.Log("[CharacterPanel] 打开角色升级面板，角色: " + characterId);
			UIKit.OpenPanel<sonCharacterUpgradePanel>(UILevel.Pop, data: new sonCharacterUpgradePanelData
			{
				CharacterId = characterId,
				OpenerPanelType = "CharacterPanel",
				OpenerData = openerData
			});
		}

		private void UpdateUpgradeButtonState(AccountItemData character)
		{
			if (mBtnUpgrade == null) return;

			int maxLevel = GetCharacterMaxLevel(character.ItemId);
			bool isMaxLevel = character.Level >= maxLevel;

			mBtnUpgrade.interactable = !isMaxLevel;

			Text btnText = mBtnUpgrade.GetComponentInChildren<Text>();
			if (btnText != null)
			{
				btnText.text = isMaxLevel ? "已满级" : "升级";
				btnText.color = isMaxLevel ? Color.gray : Color.white;
			}

			Image btnImage = mBtnUpgrade.GetComponent<Image>();
			if (btnImage != null)
			{
				btnImage.color = isMaxLevel
					? new Color(0.5f, 0.5f, 0.5f)
					: new Color(0.2f, 0.6f, 0.2f);
			}
		}

		private int GetCharacterMaxLevel(string characterId)
		{
			var config = GameDataMgr.Instance.playerCharacterConfigs.Characters
				.FirstOrDefault(c => c.CharacterId == characterId);
			return config != null && config.MaxLevel > 0 ? config.MaxLevel : 80;
		}

		private void RefreshTabPanel()
		{
			if (mTogglePanel == null)
				return;

			foreach (Transform child in mTogglePanel)
			{
				Object.Destroy(child.gameObject);
			}

			GridLayoutGroup layout = mTogglePanel.GetComponent<GridLayoutGroup>();
			if (layout == null)
			{
				layout = mTogglePanel.gameObject.AddComponent<GridLayoutGroup>();
				layout.cellSize = new Vector2(100, 35);
				layout.spacing = new Vector2(5, 5);
				layout.startCorner = GridLayoutGroup.Corner.UpperLeft;
				layout.startAxis = GridLayoutGroup.Axis.Horizontal;
				layout.childAlignment = TextAnchor.MiddleCenter;
			}

			ToggleGroup toggleGroup = mTogglePanel.GetComponent<ToggleGroup>();
			if (toggleGroup == null)
			{
				toggleGroup = mTogglePanel.gameObject.AddComponent<ToggleGroup>();
			}

			string[] tabLabels = { "属性", "武器", "遗器", "星魂", "天赋" };
			string[] tabKeys = { "Attributes", "Weapon", "Relic", "Star", "Talent" };

			for (int i = 0; i < tabLabels.Length; i++)
			{
				CreateTabToggle(tabLabels[i], tabKeys[i], mTogglePanel, toggleGroup);
			}
		}

		private void CreateTabToggle(string label, string key, Transform parent, ToggleGroup toggleGroup)
		{
			GameObject toggleObj = new GameObject($"Tab_{key}");
			toggleObj.transform.SetParent(parent, false);

			RectTransform rect = toggleObj.AddComponent<RectTransform>();

			Image image = toggleObj.AddComponent<Image>();
			bool isSelected = mCurrentTab == key;
			image.color = isSelected ? new Color(0.6f, 0.8f, 1f) : new Color(0.8f, 0.8f, 0.8f);

			GameObject textObj = new GameObject("Text");
			textObj.transform.SetParent(toggleObj.transform, false);

			Text text = textObj.AddComponent<Text>();
			text.font = mDefaultFont;
			text.text = label;
			text.fontSize = 14;
			text.alignment = TextAnchor.MiddleCenter;
			text.color = Color.black;

			RectTransform textRect = textObj.GetComponent<RectTransform>();
			textRect.sizeDelta = new Vector2(100, 35);
			textRect.localPosition = Vector3.zero;

			Toggle toggle = toggleObj.AddComponent<Toggle>();
			if (toggleGroup != null)
			{
				toggle.group = toggleGroup;
			}
			toggle.transition = Selectable.Transition.None;
			toggle.isOn = isSelected;

			toggle.onValueChanged.AddListener((bool value) =>
			{
				image.color = value ? new Color(0.6f, 0.8f, 1f) : new Color(0.8f, 0.8f, 0.8f);
				if (value)
				{
					mCurrentTab = key;
					SetPanelActive(mPanelAllAttr, false);
					if (mSelectedCharacter != null)
					{
						UpdateTabContent(mSelectedCharacter);
					}
				}
			});
		}

		private void OnCharacterClicked(AccountItemData character)
		{
			mSelectedCharacter = character;
			UpdateTabContent(character);
		}

		private void UpdateTabContent(AccountItemData character)
		{
			UpdatePanelVisibility();

			switch (mCurrentTab)
			{
				case "Attributes":
					ShowAttributes(character);
					break;
				case "Weapon":
					ShowWeapon(character);
					break;
				case "Relic":
					ShowRelics(character);
					break;
				case "Star":
					mCurrentStarLevel = 0;
					ShowStar(character);
					break;
				case "Talent":
					ShowTalent(character);
					break;
			}
		}

		private void UpdatePanelVisibility()
		{
			SetPanelActive(mPanelDetail, mCurrentTab == "Attributes");
			SetPanelActive(mPanelWeapon, mCurrentTab == "Weapon");
			SetPanelActive(mPanelRelic, mCurrentTab == "Relic");
			SetPanelActive(mPanelStar, mCurrentTab == "Star");
			SetPanelActive(mPanelTalent, mCurrentTab == "Talent");
		}

		private void SetPanelActive(Transform panel, bool active)
		{
			if (panel != null && panel.gameObject != null)
			{
				panel.gameObject.SetActive(active);
			}
		}

		private void ShowAttributes(AccountItemData character)
		{
			if (mImgIcon != null)
			{
				SpriteIconLoader.SetCharacterIcon(mImgIcon, character.ItemId);
				if (mImgIcon.sprite == null)
				{
					mImgIcon.color = GetCharacterIconColor(character.ItemId);
				}
			}

			if (mTexName != null)
				mTexName.text = character.ItemId;

			if (mTexBaseAttr != null)
			{
				var finalStats = UpgradeSystem.Instance.CalculateFinalStats(AccountManager.Instance.CurrentAccount.AccountId, character.ItemId);
				float hp = finalStats.TryGetValue("MaxHp", out var hpVal) ? hpVal : 0f;
				float attack = finalStats.TryGetValue("Attack", out var atkVal) ? atkVal : 0f;
				float defense = finalStats.TryGetValue("Defense", out var defVal) ? defVal : 0f;
				float speed = finalStats.TryGetValue("Speed", out var spdVal) ? spdVal : 0f;
				float critRate = finalStats.TryGetValue("CritRate", out var crVal) ? crVal : 0f;
				float critDamage = finalStats.TryGetValue("CritDamage", out var cdVal) ? cdVal : 0f;

				mTexBaseAttr.text = $"等级: {character.Level}\n" +
									$"攻击力: {Mathf.RoundToInt(attack)}\n" +
									$"生命值: {Mathf.RoundToInt(hp)}\n" +
									$"防御力: {Mathf.RoundToInt(defense)}\n" +
									$"速度: {Mathf.RoundToInt(speed)}\n" +
									$"暴击率: {Mathf.RoundToInt(critRate * 100)}%\n" +
									$"暴击伤害: {Mathf.RoundToInt(critDamage * 100)}%";
			}

			if (mBtnWeaponSlot != null && mImgWeapon != null)
			{
				if (!string.IsNullOrEmpty(character.EquippedWeapon))
				{
					var weaponItem = GetEquippedItem("Weapon", character.EquippedWeapon);
					SpriteIconLoader.SetWeaponIcon(mImgWeapon, weaponItem != null ? weaponItem.ItemId : "");
					if (mImgWeapon.sprite == null)
					{
						mImgWeapon.color = new Color(0.7f, 0.7f, 0.7f);
					}
					else
					{
						mImgWeapon.color = Color.white;
					}

					// 同时设置按钮自身的图片组件
					Image btnImg = mBtnWeaponSlot.GetComponent<Image>();
					if (btnImg != null && btnImg != mImgWeapon)
					{
						SpriteIconLoader.SetWeaponIcon(btnImg, weaponItem != null ? weaponItem.ItemId : "");
						if (btnImg.sprite != null)
							btnImg.color = Color.white;
					}
				}
				else
				{
					mImgWeapon.sprite = null;
					mImgWeapon.color = Color.clear;

					// 清空按钮自身的图片
					Image btnImg = mBtnWeaponSlot.GetComponent<Image>();
					if (btnImg != null && btnImg != mImgWeapon)
					{
						btnImg.sprite = null;
						btnImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
					}
				}
			}

			if (mTexDetail != null)
				mTexDetail.text = "";

			if (mTexRelicAttr != null)
				mTexRelicAttr.text = "";

			if (mTexSetEffect != null)
				mTexSetEffect.text = "";

			UpdateUpgradeButtonState(character);
		}

		private AccountItemData GetEquippedItem(string itemType, string instanceId)
		{
			if (!AccountManager.Instance.IsLoggedIn || string.IsNullOrEmpty(instanceId))
				return null;

			return AccountManager.Instance.GetAccountItemByInstanceId(
				AccountManager.Instance.CurrentAccount.AccountId, itemType, instanceId);
		}

		private void ShowWeapon(AccountItemData character)
		{
			bool hasWeapon = !string.IsNullOrEmpty(character.EquippedWeapon);

			if (mImgWeapon != null)
			{
				if (hasWeapon)
				{
					var weaponItem = GetEquippedItem("Weapon", character.EquippedWeapon);
					SpriteIconLoader.SetWeaponIcon(mImgWeapon, weaponItem != null ? weaponItem.ItemId : "");
					if (mImgWeapon.sprite == null)
					{
						mImgWeapon.color = new Color(0.7f, 0.7f, 0.7f);
					}
					else
					{
						mImgWeapon.color = Color.white;
					}
				}
				else
				{
					// 卸下装备后清空图片，不再显示透明的旧图片
					mImgWeapon.sprite = null;
					mImgWeapon.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
				}
			}

			if (mBtnWeaponEquip != null)
			{
				mBtnWeaponEquip.gameObject.SetActive(true);
				Text equipText = mBtnWeaponEquip.GetComponentInChildren<Text>();
				if (equipText != null)
				{
					equipText.text = hasWeapon ? "更换武器" : "装备武器";
				}
			}

			if (mBtnWeaponUpgrade != null)
				mBtnWeaponUpgrade.gameObject.SetActive(hasWeapon);

			if (mTexDetail != null)
			{
				if (!hasWeapon)
				{
					mTexDetail.text = "未装备武器";
				}
				else
				{
					var weaponItem = GetEquippedItem("Weapon", character.EquippedWeapon);
					var weaponConfig = GameDataMgr.Instance.lightConeConfigs.Weapons
						.FirstOrDefault(w => weaponItem != null && w.WeaponId == weaponItem.ItemId);

					if (weaponConfig != null)
					{
						int weaponLevel = weaponItem.Level;
						UpgradeSystem.Instance.CalculateWeaponBaseStats(weaponConfig, weaponLevel, out float currentMaxHp, out float currentAttack, out float currentDefense);

						mTexDetail.text = $"武器名称: {weaponConfig.Name}\n" +
										 $"等级: Lv.{weaponLevel}\n" +
										 $"攻击力加成: {Mathf.RoundToInt(currentAttack)}\n" +
										 $"防御力加成: {Mathf.RoundToInt(currentDefense)}\n" +
										 $"生命值加成: {Mathf.RoundToInt(currentMaxHp)}\n\n";

						if (weaponConfig.Effects != null && weaponConfig.Effects.Count > 0)
						{
							mTexDetail.text += "武器效果:\n";
							foreach (var effect in weaponConfig.Effects)
							{
								mTexDetail.text += $"  - {effect.EffectName}: {effect.Description}\n";
							}
						}
					}
					else
					{
						mTexDetail.text = $"武器ID: {character.EquippedWeapon}\n(配置文件中未找到)";
					}
				}
			}

			if (mImgIcon != null)
				mImgIcon.color = Color.clear;

			if (mTexName != null)
				mTexName.text = "";

			if (mTexBaseAttr != null)
				mTexBaseAttr.text = "";

			if (mTexRelicAttr != null)
				mTexRelicAttr.text = "";

			if (mTexSetEffect != null)
				mTexSetEffect.text = "";
		}

		private void ShowRelics(AccountItemData character)
		{
			if (mTexRelicAttr != null)
			{
				mTexRelicAttr.text = CalculateRelicStatsSummary(character);
			}

			if (mTexSetEffect != null)
			{
				mTexSetEffect.text = CalculateSetEffectSummary(character);
			}

			// 更新遗器槽位按钮图片
			UpdateRelicSlotImages(character);
		}

		private void UpdateRelicSlotImages(AccountItemData character)
		{
			var slotButtons = new Dictionary<string, Button>
			{
				{ "Head", mBtnHead },
				{ "Hand", mBtnHand },
				{ "Body", mBtnBody },
				{ "Feet", mBtnFeet }
			};

			foreach (var kvp in slotButtons)
			{
				var btn = kvp.Value;
				if (btn == null) continue;

				Image btnImg = btn.GetComponent<Image>();
				if (btnImg == null) continue;

				string slot = kvp.Key;
				string normalizedSlot = NormalizeRelicSlot(slot);

				if (character.EquippedRelics != null && character.EquippedRelics.ContainsKey(normalizedSlot))
				{
					var relicItem = GetEquippedItem("Relic", character.EquippedRelics[normalizedSlot]);
					SpriteIconLoader.SetRelicIcon(btnImg, relicItem != null ? relicItem.ItemId : "");
					if (btnImg.sprite == null)
					{
						var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
							.FirstOrDefault(r => relicItem != null && r.RelicId == relicItem.ItemId);
						btnImg.color = relicConfig != null ? GetRelicRarityColor(relicConfig.Rarity) : Color.gray;
					}
				}
				else
				{
					btnImg.sprite = null;
					btnImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
				}
			}
		}
		
		public void RefreshRelicAttr(AccountItemData character)
		{
			// 刷新遗器属性和套装效果显示
			ShowRelics(character);
			// 刷新角色最终属性显示（因为遗器装备情况变化会影响最终属性）
			ShowAttributes(character);
		}

		private string CalculateRelicStatsSummary(AccountItemData character)
		{
			if (character.EquippedRelics == null || character.EquippedRelics.Count == 0)
				return "遗器加成: 无";

			// 统计所有已装备遗器的属性加成
			float maxHpFlat = 0f;
			float maxHpPercent = 0f;
			float attackFlat = 0f;
			float attackPercent = 0f;
			float defensePercent = 0f;
			float speed = 0f;
			float critRate = 0f;
			float critDamage = 0f;

			foreach (var kvp in character.EquippedRelics)
			{
				var relicItem = GetEquippedItem("Relic", kvp.Value);
				var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
					.FirstOrDefault(r => relicItem != null && r.RelicId == relicItem.ItemId);

				if (relicConfig == null) continue;

				AccountRelicData relicData = null;
				if (relicItem.ExtraData != null && relicItem.ExtraData.ContainsKey("RelicData"))
				{
					relicData = JsonUtility.FromJson<AccountRelicData>(relicItem.ExtraData["RelicData"]);
				}

				if (relicData == null)
					continue;

				// 主词条
				if (relicData.MainStatType == RelicMainStatType.MaxHp)
					maxHpFlat += relicData.MainStatValue;
				else if (relicData.MainStatType == RelicMainStatType.MaxHpPercent)
					maxHpPercent += relicData.MainStatValue;
				else if (relicData.MainStatType == RelicMainStatType.Attack)
					attackFlat += relicData.MainStatValue;
				else if (relicData.MainStatType == RelicMainStatType.AttackPercent)
					attackPercent += relicData.MainStatValue;
				else if (relicData.MainStatType == RelicMainStatType.DefensePercent)
					defensePercent += relicData.MainStatValue;
				else if (relicData.MainStatType == RelicMainStatType.Speed)
					speed += relicData.MainStatValue;
				else if (relicData.MainStatType == RelicMainStatType.CritRate)
					critRate += relicData.MainStatValue;
				else if (relicData.MainStatType == RelicMainStatType.CritDamage)
					critDamage += relicData.MainStatValue;

				// 副词条
				foreach (var subStat in relicData.SubStats)
				{
					if (subStat.StatType == RelicMainStatType.MaxHp)
						maxHpFlat += subStat.Value;
					else if (subStat.StatType == RelicMainStatType.MaxHpPercent)
						maxHpPercent += subStat.Value;
					else if (subStat.StatType == RelicMainStatType.Attack)
						attackFlat += subStat.Value;
					else if (subStat.StatType == RelicMainStatType.AttackPercent)
						attackPercent += subStat.Value;
					else if (subStat.StatType == RelicMainStatType.DefensePercent)
						defensePercent += subStat.Value;
					else if (subStat.StatType == RelicMainStatType.Speed)
						speed += subStat.Value;
					else if (subStat.StatType == RelicMainStatType.CritRate)
						critRate += subStat.Value;
					else if (subStat.StatType == RelicMainStatType.CritDamage)
						critDamage += subStat.Value;
				}
			}

			// 获取角色基础属性
			var charConfig = GameDataMgr.Instance.playerCharacterConfigs.Characters
				.FirstOrDefault(c => c.CharacterId == character.ItemId);

			string result = "遗器属性加成:\n";

			if (maxHpFlat > 0 || maxHpPercent > 0)
			{
				int totalMaxHpBonus = 0;
				if (charConfig != null)
				{
					// 百分比属性存储为0-1之间的小数，直接相乘
					totalMaxHpBonus = Mathf.RoundToInt(charConfig.MaxHp * maxHpPercent) + Mathf.RoundToInt(maxHpFlat);
				}
				result += $"生命值: +{totalMaxHpBonus}";
				if (maxHpPercent > 0) result += $" ({Mathf.RoundToInt(maxHpPercent * 100)}% + {Mathf.RoundToInt(maxHpFlat)})\n";
				else result += "\n";
			}

			if (attackFlat > 0 || attackPercent > 0)
			{
				int totalAttackBonus = 0;
				if (charConfig != null)
				{
					totalAttackBonus = Mathf.RoundToInt(charConfig.Attack * attackPercent) + Mathf.RoundToInt(attackFlat);
				}
				result += $"攻击力: +{totalAttackBonus}";
				if (attackPercent > 0) result += $" ({Mathf.RoundToInt(attackPercent * 100)}% + {Mathf.RoundToInt(attackFlat)})\n";
				else result += "\n";
			}

			if (defensePercent > 0)
			{
				int totalDefenseBonus = 0;
				if (charConfig != null)
				{
					totalDefenseBonus = Mathf.RoundToInt(charConfig.Defense * defensePercent);
				}
				result += $"防御力: +{totalDefenseBonus} ({Mathf.RoundToInt(defensePercent * 100)}%)\n";
			}

			if (speed > 0)
				result += $"速度: +{Mathf.RoundToInt(speed)}\n";

			if (critRate > 0)
				result += $"暴击率: +{Mathf.RoundToInt(critRate * 100)}%\n";

			if (critDamage > 0)
				result += $"暴击伤害: +{Mathf.RoundToInt(critDamage * 100)}%\n";

			return result.Trim();
		}

		private string CalculateSetEffectSummary(AccountItemData character)
		{
			if (character.EquippedRelics == null || character.EquippedRelics.Count == 0)
				return "套装效果: 无";

			// 统计各套装数量
			Dictionary<string, int> setCounts = new Dictionary<string, int>();
			Dictionary<string, RelicConfig> setConfigs = new Dictionary<string, RelicConfig>();

			foreach (var kvp in character.EquippedRelics)
			{
				var relicItem = GetEquippedItem("Relic", kvp.Value);
				var relicConfig = GameDataMgr.Instance.relicConfigs.Relics
					.FirstOrDefault(r => relicItem != null && r.RelicId == relicItem.ItemId);

				if (relicConfig == null || string.IsNullOrEmpty(relicConfig.SetName))
					continue;

				string setName = relicConfig.SetName;
				if (!setCounts.ContainsKey(setName))
				{
					setCounts[setName] = 0;
					setConfigs[setName] = relicConfig;
				}
				setCounts[setName]++;
			}

			string result = "";
			foreach (var kvp in setCounts)
			{
				string setName = kvp.Key;
				int count = kvp.Value;
				var config = setConfigs[setName];

				result += $"{setName}: {count}/4\n";

				if (count >= 1 && !string.IsNullOrEmpty(config.SetEffect1))
				{
					result += $"  [已激活] {config.SetEffect1}\n";
				}

				if (count >= 3 && !string.IsNullOrEmpty(config.SetEffect2))
				{
					result += $"  [已激活] {config.SetEffect2}\n";
				}
			}

			if (result == "")
				result = "套装效果: 无";

			return result.Trim();
		}

		private void ShowStar(AccountItemData character)
		{
			int starLevel = character.StarLevel;

			var config = GameDataMgr.Instance.playerCharacterConfigs.Characters
				.FirstOrDefault(c => c.CharacterId == character.ItemId);

			// 为每个星魂按钮设置对应星魂图片（按角色+星级区分）
			UpdateStarButtonImages(character.ItemId, starLevel, config);

			// 星魂详情文本（使用星魂面板专用的texDetail）
			if (mTexStarDetail != null)
			{
				if (mCurrentStarLevel >= 1 && mCurrentStarLevel <= 5 && config != null && config.StarEffects != null)
				{
					var starEffect = config.StarEffects.FirstOrDefault(s => s.StarLevel == mCurrentStarLevel);
					if (starEffect != null)
					{
						mTexStarDetail.text = $"星魂 {starEffect.StarLevel}\n效果: {starEffect.Description}";
					}
					else
					{
						mTexStarDetail.text = "";
					}
				}
				else
				{
					mTexStarDetail.text = "";
				}
			}

			// 星魂解锁按钮状态
			if (mBtnStarUnlock != null)
			{
				if (mCurrentStarLevel < 1 || mCurrentStarLevel > 5)
				{
					mBtnStarUnlock.gameObject.SetActive(false);
				}
				else
				{
					mBtnStarUnlock.gameObject.SetActive(true);
					Text btnText = mBtnStarUnlock.GetComponentInChildren<Text>();
					
					if (starLevel >= mCurrentStarLevel)
					{
						if (btnText != null) btnText.text = "已解锁";
						mBtnStarUnlock.interactable = false;
					}
					else if (mCurrentStarLevel == starLevel + 1)
					{
						if (AccountManager.Instance.IsLoggedIn)
						{
							string accountId = AccountManager.Instance.CurrentAccount.AccountId;
							bool canUnlock = UpgradeSystem.Instance.CanUnlockStar(accountId, character.ItemId, mCurrentStarLevel);
							if (btnText != null) btnText.text = "解锁";
							mBtnStarUnlock.interactable = canUnlock;
						}
						else
						{
							if (btnText != null) btnText.text = "解锁";
							mBtnStarUnlock.interactable = true;
						}
					}
					else
					{
						if (btnText != null) btnText.text = "先解锁上一级";
						mBtnStarUnlock.interactable = false;
					}
				}
			}

			// 星魂解锁材料显示
			UpdateStarMaterialDisplay(character, config);

			if (mImgIcon != null)
				mImgIcon.color = Color.clear;

			if (mTexName != null)
				mTexName.text = "";

			if (mTexBaseAttr != null)
				mTexBaseAttr.text = "";

			if (mImgWeapon != null)
				mImgWeapon.color = Color.clear;

			if (mTexRelicAttr != null)
				mTexRelicAttr.text = "";

			if (mTexSetEffect != null)
				mTexSetEffect.text = "";
		}

		private void UpdateStarMaterialDisplay(AccountItemData character, PlayerCharacterConfig config)
		{
			if (mStarMaterial == null) return;

			Text materialText = mStarMaterial.GetComponentInChildren<Text>(true);
			Image materialBg = mStarMaterial.GetComponent<Image>();

			if (mCurrentStarLevel < 1 || mCurrentStarLevel > 5 || character.StarLevel >= mCurrentStarLevel)
			{
				mStarMaterial.gameObject.SetActive(false);
				return;
			}

			if (config == null || config.StarEffects == null)
			{
				mStarMaterial.gameObject.SetActive(false);
				return;
			}

			var starEffect = config.StarEffects.FirstOrDefault(s => s.StarLevel == mCurrentStarLevel);
			if (starEffect == null || starEffect.UnlockCost == null || starEffect.UnlockCost.Count == 0)
			{
				mStarMaterial.gameObject.SetActive(false);
				return;
			}

			mStarMaterial.gameObject.SetActive(true);

			string accountId = AccountManager.Instance.IsLoggedIn ? AccountManager.Instance.CurrentAccount.AccountId : "";

			var costItem = starEffect.UnlockCost[0];
			int currentCount = 0;
			if (AccountManager.Instance.IsLoggedIn)
			{
				var item = AccountManager.Instance.GetAccountItem(accountId, "Consumable", costItem.ItemId);
				if (item != null)
					currentCount = item.Count;
			}

			bool isEnough = currentCount >= costItem.Count;

			if (materialText != null)
			{
				materialText.text = $"{currentCount}/{costItem.Count}";
				materialText.color = new Color(0.0368f, 0.8679f, 0.8679f);
				materialText.alignment = TextAnchor.LowerRight;
				materialText.fontStyle = FontStyle.Bold;
			}

			if (materialBg != null)
			{
				SpriteIconLoader.SetConsumableIcon(materialBg, costItem.ItemId);
				if (materialBg.sprite != null)
					materialBg.color = Color.white;
				else
					materialBg.color = isEnough ? new Color(0.8f, 1f, 0.8f) : new Color(1f, 0.8f, 0.8f);
			}
		}

		/// <summary>
		/// 为星魂按钮设置对应星魂图片，使用 {CharacterId}_Star{Level} 格式区分不同角色的星魂
		/// </summary>
		private void UpdateStarButtonImages(string characterId, int currentStarLevel, PlayerCharacterConfig config)
		{
			Button[] starButtons = { mBtnStar1, mBtnStar2, mBtnStar3, mBtnStar4, mBtnStar5 };

			for (int i = 0; i < starButtons.Length; i++)
			{
				var btn = starButtons[i];
				if (btn == null) continue;

				Image btnImage = btn.GetComponent<Image>();
				if (btnImage == null) continue;

				int targetStarLevel = i + 1;

				// 检查该星级是否有星魂配置
				bool hasStarEffect = config != null && config.StarEffects != null &&
					config.StarEffects.Any(s => s.StarLevel == targetStarLevel);

				if (hasStarEffect)
				{
					// 使用 {CharacterId}_Star{Level} 格式加载星魂图片，确保每个角色的星魂可区分
					SpriteIconLoader.SetStarConeIcon(btnImage, $"{characterId}_Star{targetStarLevel}");

					if (btnImage.sprite != null)
					{
						// 已解锁正常显示，未解锁金色提示
						btnImage.color = targetStarLevel <= currentStarLevel
							? Color.white
							: new Color(1f, 0.85f, 0.3f);
					}
					else
					{
						// 无图片资源时用颜色区分：已解锁正常白色，未解锁金色
						btnImage.color = targetStarLevel <= currentStarLevel
							? new Color(0.9f, 0.9f, 0.9f, 0.9f)
							: new Color(1f, 0.85f, 0.3f, 0.9f);
					}
				}
				else
				{
					// 该角色没有此星级的配置，清空图片
					btnImage.sprite = null;
					btnImage.color = new Color(0.3f, 0.3f, 0.3f, 0.3f);
				}
			}
		}

		private void ShowTalent(AccountItemData character)
		{
			var config = GameDataMgr.Instance.playerCharacterConfigs.Characters
				.FirstOrDefault(c => c.CharacterId == character.ItemId);

			string configSkillType = GetConfigSkillType(mCurrentSkillType);
			int currentTalentLevel = character.GetTalentLevel(configSkillType);

			if (mTexTalentDetail != null)
			{
				if (config != null && config.TalentEffects != null)
				{
					var talentConfig = config.TalentEffects.FirstOrDefault(t => t.SkillType == configSkillType);
					if (talentConfig != null)
					{
						var currentLevelEffect = talentConfig.Effects.FirstOrDefault(e => e.Level == currentTalentLevel);
						var nextLevelEffect = talentConfig.Effects.FirstOrDefault(e => e.Level == currentTalentLevel + 1);

						string skillTypeName = GetSkillTypeName(mCurrentSkillType);
						
						if (currentLevelEffect != null)
						{
							string valStr;
							if (currentLevelEffect.Value > 0 && currentLevelEffect.Value2 > 0)
								valStr = $"效果值: {currentLevelEffect.Value} / {currentLevelEffect.Value2}";
							else if (currentLevelEffect.Value2 > 0)
								valStr = $"效果值: {currentLevelEffect.Value2}";
							else
								valStr = $"效果值: {currentLevelEffect.Value}";
							mTexTalentDetail.text = $"{skillTypeName} (等级 {currentTalentLevel})\n" +
													$"{valStr}\n" +
													$"{currentLevelEffect.Description}";
						}
						else
						{
							mTexTalentDetail.text = $"{skillTypeName} (等级 {currentTalentLevel})\n暂无数据";
						}

						if (nextLevelEffect != null)
						{
							string nextValStr;
							if (nextLevelEffect.Value > 0 && nextLevelEffect.Value2 > 0)
								nextValStr = $"效果值: {nextLevelEffect.Value} / {nextLevelEffect.Value2}";
							else if (nextLevelEffect.Value2 > 0)
								nextValStr = $"效果值: {nextLevelEffect.Value2}";
							else
								nextValStr = $"效果值: {nextLevelEffect.Value}";
							mTexTalentDetail.text += $"\n\n升级到等级 {nextLevelEffect.Level}:\n" +
													$"{nextValStr}\n" +
													$"{nextLevelEffect.Description}";
						}
					}
					else
					{
						mTexTalentDetail.text = $"{GetSkillTypeName(mCurrentSkillType)}: 暂无天赋配置";
					}
				}
				else
				{
					mTexTalentDetail.text = "暂无天赋数据";
				}
			}

			// 更新天赋材料显示
			UpdateTalentMaterialDisplay(character, config, configSkillType, currentTalentLevel);

			if (mBtnTalentUpgrade != null)
			{
				bool canUpgrade = false;
				if (config != null && config.TalentEffects != null)
				{
					var talentConfig = config.TalentEffects.FirstOrDefault(t => t.SkillType == configSkillType);
					if (talentConfig != null)
					{
						int nextLevel = currentTalentLevel + 1;
						var nextLevelEffect = talentConfig.Effects.FirstOrDefault(e => e.Level == nextLevel);
						if (nextLevelEffect != null && currentTalentLevel < 10)
						{
							if (AccountManager.Instance.IsLoggedIn)
							{
								string accountId = AccountManager.Instance.CurrentAccount.AccountId;
								canUpgrade = UpgradeSystem.Instance.CanUpgradeTalent(accountId, character.ItemId, configSkillType);
							}
						}
					}
				}
				mBtnTalentUpgrade.interactable = canUpgrade;
			}

			// 更新天赋金币消耗显示
			UpdateTalentGoldCostDisplay(character, config, configSkillType, currentTalentLevel);

			if (mImgIcon != null)
				mImgIcon.color = Color.clear;

			if (mTexName != null)
				mTexName.text = "";

			if (mTexBaseAttr != null)
				mTexBaseAttr.text = "";

			if (mImgWeapon != null)
				mImgWeapon.color = Color.clear;

			if (mTexRelicAttr != null)
				mTexRelicAttr.text = "";

			if (mTexSetEffect != null)
				mTexSetEffect.text = "";
		}

		private void UpdateTalentMaterialDisplay(AccountItemData character, PlayerCharacterConfig config, string configSkillType, int currentTalentLevel)
		{
			Transform[] materials = { mTalentMaterial1, mTalentMaterial2, mTalentMaterial3 };

			foreach (var mat in materials)
			{
				if (mat != null)
					mat.gameObject.SetActive(false);
			}

			if (config == null || config.TalentEffects == null) return;

			var talentConfig = config.TalentEffects.FirstOrDefault(t => t.SkillType == configSkillType);
			if (talentConfig == null) return;

			int nextLevel = currentTalentLevel + 1;
			if (nextLevel > 10) return;

			var nextLevelEffect = talentConfig.Effects.FirstOrDefault(e => e.Level == nextLevel);
			if (nextLevelEffect == null || nextLevelEffect.UpgradeCost == null || nextLevelEffect.UpgradeCost.Count == 0)
				return;

			string accountId = AccountManager.Instance.IsLoggedIn ? AccountManager.Instance.CurrentAccount.AccountId : "";

			Color[] materialColors = { 
				new Color(0.8f, 0.6f, 0.4f),
				new Color(0.4f, 0.6f, 0.8f),
				new Color(0.6f, 0.4f, 0.8f)
			};

			for (int i = 0; i < nextLevelEffect.UpgradeCost.Count && i < 3; i++)
			{
				var costItem = nextLevelEffect.UpgradeCost[i];
				var materialTrans = materials[i];
				if (materialTrans == null) continue;

				materialTrans.gameObject.SetActive(true);

				Text materialText = materialTrans.GetComponentInChildren<Text>(true);
				Image materialBg = materialTrans.GetComponent<Image>();

				int currentCount = 0;
				if (AccountManager.Instance.IsLoggedIn)
				{
					var item = AccountManager.Instance.GetAccountItem(accountId, "Consumable", costItem.ItemId);
					if (item != null)
						currentCount = item.Count;
				}

				bool isEnough = currentCount >= costItem.Count;

				if (materialText != null)
				{
					string itemName = GetItemDisplayName(costItem.ItemId);
					materialText.text = $"{itemName}\n{currentCount}/{costItem.Count}";
					materialText.color = new Color(0.0368f, 0.8679f, 0.8679f);
					materialText.alignment = TextAnchor.LowerRight;
					materialText.fontStyle = FontStyle.Bold;
				}

				if (materialBg != null)
				{
					SpriteIconLoader.SetConsumableIcon(materialBg, costItem.ItemId);
					if (materialBg.sprite != null)
						materialBg.color = Color.white;
					else
						materialBg.color = materialColors[i % materialColors.Length];
				}
			}
		}

		private void UpdateTalentGoldCostDisplay(AccountItemData character, PlayerCharacterConfig config, string configSkillType, int currentTalentLevel)
		{
			if (mTexTalentGoldCost == null) return;

			if (currentTalentLevel >= 10)
			{
				mTexTalentGoldCost.text = "已达最高等级";
				mTexTalentGoldCost.color = Color.gray;
				return;
			}

			int goldCost = UpgradeSystem.Instance.GetTalentUpgradeGoldCost(currentTalentLevel);
			int currentGold = 0;
			if (AccountManager.Instance.IsLoggedIn)
			{
				string accountId = AccountManager.Instance.CurrentAccount.AccountId;
				currentGold = AccountManager.Instance.GetAccountItemCount(accountId, "Consumable", "Gold");
			}

			mTexTalentGoldCost.text = $"所需金币\n{currentGold}/{goldCost}";
			mTexTalentGoldCost.color = currentGold >= goldCost ? new Color(0.2384f, 0.8490f, 0.2042f) : new Color(0.8509f, 0.3627f, 0.2039f);
		}

		private string GetItemDisplayName(string itemId)
		{
			var consumableConfig = GameDataMgr.Instance.consumableConfigs?.Consumables
				.FirstOrDefault(c => c.ItemId == itemId);
			if (consumableConfig != null)
				return consumableConfig.Name;
			
			return itemId;
		}

		private ConsumableConfig GetConsumableConfig(string itemId)
		{
			return GameDataMgr.Instance.consumableConfigs?.Consumables
				.FirstOrDefault(c => c.ItemId == itemId);
		}

		private string GetSkillTypeName(string skillType)
		{
			switch (skillType)
			{
				case "Normal":
				case "NormalAttack": return "普攻";
				case "Skill1": return "技1";
				case "Skill2": return "技2";
				case "Ultimate": return "大招";
				default: return skillType;
			}
		}
    }
#endregion
}
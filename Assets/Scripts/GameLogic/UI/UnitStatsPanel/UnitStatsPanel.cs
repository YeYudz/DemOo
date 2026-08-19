using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YokiFrame;
using System.Linq;

namespace GameUI
{
	/// <summary>
	/// 单位属性面板所需的界面数据。
	/// </summary>
	public sealed class UnitStatsPanelData : IUIData
	{
		public BattleSystem BattleSystem { get; set; }
	}

	/// <summary>
	/// 显示单位列表、属性和状态效果的面板。
	/// </summary>
	public partial class UnitStatsPanel : UIPanel
	{
		private BattleUnitData mSelectedUnit;
		private Font mDefaultFont;
		private bool mShowPlayerUnits = true;

		#region 生命周期

		// 面板生命周期：初始化、打开、显示和关闭。
		/// <summary>
		/// 初始化面板数据并加载默认字体。
		/// </summary>
		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as UnitStatsPanelData ?? new UnitStatsPanelData();
			LoadDefaultFont();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as UnitStatsPanelData ?? mData;

			LogKit.Log($"[UnitStatsPanel] OnOpen - mStatusEffectsPanel: {(mStatusEffectsPanel == null ? "null" : mStatusEffectsPanel.name)}");

			

			if (mCloseButton != null)
			{
				mCloseButton.onClick.RemoveAllListeners();
				mCloseButton.onClick.AddListener(OnCloseClicked);
			}

			if (mToggleUnitSideButton != null)
			{
				mToggleUnitSideButton.onClick.RemoveAllListeners();
				mToggleUnitSideButton.onClick.AddListener(OnToggleUnitSideClicked);
				UpdateToggleButtonText();
			}

			RefreshUnitList();
			RefreshStats();
		}

		protected override void OnShow()
		{
			RefreshUnitList();
			RefreshStats();
		}

		protected override void OnClose()
		{
			mSelectedUnit = null;
		}

		/// <summary>
		/// 加载运行时创建文本使用的默认字体。
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
		/// 按当前阵营刷新可选单位列表。
		/// </summary>
		private void RefreshUnitList()
		{
			if (mUnitListPanel == null)
				return;

			foreach (Transform child in mUnitListPanel)
			{
				Object.Destroy(child.gameObject);
			}

			if (!mUnitListPanel.GetComponent<GridLayoutGroup>())
			{
				GridLayoutGroup layout = mUnitListPanel.gameObject.AddComponent<GridLayoutGroup>();
				layout.spacing = new Vector2(10, 10);
				layout.cellSize = new Vector2(120, 180);
				layout.childAlignment = TextAnchor.UpperLeft;
				layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
				layout.constraintCount = 4;
				layout.padding = new RectOffset(5, 0, 5, 5);
			}

			if (mShowPlayerUnits)
			{
				var playerUnits = BattleSystem.Instance.GetPlayerUnits();
				foreach (var unit in playerUnits)
				{
					if (unit.IsAlive)
						CreateUnitIcon(unit, true);
				}
			}
			else
			{
				var enemyUnits = BattleSystem.Instance.GetEnemyUnits();
				foreach (var unit in enemyUnits)
				{
					if (unit.IsAlive)
						CreateUnitIcon(unit, false);
				}
			}
		}

		/// <summary>
		/// 创建单位列表中的图标和选择按钮。
		/// </summary>
		private void CreateUnitIcon(BattleUnitData unit, bool isPlayer)
		{
			GameObject iconObj = new GameObject($"{unit.Name}_{unit.GetHashCode()}");
			iconObj.transform.SetParent(mUnitListPanel, false);

			RectTransform rect = iconObj.AddComponent<RectTransform>();
			rect.sizeDelta = new Vector2(120, 180);

			Image bg = iconObj.AddComponent<Image>();
			bg.color = isPlayer ? new Color(0.1f, 0.3f, 0.5f, 0.8f) : new Color(0.5f, 0.1f, 0.1f, 0.8f);

			bool isSelected = mSelectedUnit != null && object.ReferenceEquals(mSelectedUnit, unit);
			if (isSelected)
			{
				GameObject glow = new GameObject("Glow");
				glow.transform.SetParent(iconObj.transform, false);
				RectTransform glowRect = glow.AddComponent<RectTransform>();
				glowRect.sizeDelta = new Vector2(120, 180);
				glowRect.localPosition = Vector3.zero;
				Image glowImage = glow.AddComponent<Image>();
				glowImage.color = new Color(1f, 0.8f, 0f, 0.4f);
				glow.transform.SetSiblingIndex(0);
			}

			GameObject iconBg = new GameObject("IconBg");
			iconBg.transform.SetParent(iconObj.transform, false);
			RectTransform iconBgRect = iconBg.AddComponent<RectTransform>();
			iconBgRect.sizeDelta = new Vector2(90, 90);
			iconBgRect.localPosition = new Vector3(0, 25, 0);
			Image iconBgImage = iconBg.AddComponent<Image>();
			iconBgImage.color = isSelected ? new Color(1f, 0.8f, 0.3f) : Color.white;

			GameObject icon = new GameObject("Icon");
			icon.transform.SetParent(iconBg.transform, false);
			RectTransform iconRect = icon.AddComponent<RectTransform>();
			iconRect.sizeDelta = new Vector2(70, 70);
			iconRect.localPosition = Vector3.zero;
			Image iconImage = icon.AddComponent<Image>();
			if (unit.UnitType == BattleUnitType.Enemy)
			{
				SpriteIconLoader.SetEnemyIcon(iconImage, unit.UnitId);
			}
			else
			{
				SpriteIconLoader.SetCharacterIcon(iconImage, unit.UnitId);
			}
			if (iconImage.sprite == null)
			{
				iconImage.color = GetCharacterIconColor(unit);
			}

			GameObject nameObj = new GameObject("Name");
			nameObj.transform.SetParent(iconObj.transform, false);
			RectTransform nameRect = nameObj.AddComponent<RectTransform>();
			nameRect.sizeDelta = new Vector2(110, 25);
			nameRect.localPosition = new Vector3(0, -55, 0);
			Text nameText = nameObj.AddComponent<Text>();
			if (mDefaultFont != null)
				nameText.font = mDefaultFont;
			nameText.text = unit.Name;
			nameText.fontSize = 14;
			nameText.alignment = TextAnchor.MiddleCenter;
			nameText.color = Color.white;

			Button button = iconObj.AddComponent<Button>();
			button.onClick.AddListener(() => OnUnitClicked(unit));
		}

		/// <summary>
		/// 获取单位图标的配置颜色。
		/// </summary>
		private Color GetCharacterIconColor(BattleUnitData unit)
		{
			if (unit.UnitType == BattleUnitType.Player)
			{
				var config = GameDataMgr.Instance.playerCharacterConfigs.Characters
					.FirstOrDefault(c => c.CharacterId == unit.UnitId);
				if (config != null)
				{
					return new Color(config.IconColorR, config.IconColorG, config.IconColorB);
				}
			}
			else if (unit.UnitType == BattleUnitType.Enemy)
			{
				var config = GameDataMgr.Instance.enemyConfigs.Enemies
					.FirstOrDefault(e => e.EnemyId == unit.UnitId);
				if (config != null)
				{
					return new Color(config.IconColorR, config.IconColorG, config.IconColorB);
				}
			}
			return Color.gray;
		}

		/// <summary>
		/// 选中单位并刷新列表与属性显示。
		/// </summary>
		private void OnUnitClicked(BattleUnitData unit)
		{
			mSelectedUnit = unit;
			RefreshUnitList();
			RefreshStats();
		}

		private void RefreshStats()
		{
			if (mSelectedUnit == null)
			{
				HideStats();
				return;
			}

			ShowStats();

			if (mUnitNameText != null)
			{
				mUnitNameText.text = mSelectedUnit.Name;
				mUnitNameText.color = mSelectedUnit.UnitType == BattleUnitType.Player ? Color.blue : Color.red;
			}

			if (mHpText != null)
				mHpText.text = $"生命值: {mSelectedUnit.CurrentHp}/{mSelectedUnit.MaxHp}";

			if (mAttackText != null)
				mAttackText.text = $"攻击力: {mSelectedUnit.CurrentAttack}";

			if (mDefenseText != null)
				mDefenseText.text = $"防御力: {mSelectedUnit.CurrentDefense}";

			if (mSpeedText != null)
				mSpeedText.text = $"速度: {mSelectedUnit.CurrentSpeed}";

			if (mCritRateText != null)
				mCritRateText.text = $"暴击率: {(mSelectedUnit.CurrentCritRate * 100).ToString("F1")}%";

			if (mCritDamageText != null)
				mCritDamageText.text = $"暴击伤害: {(mSelectedUnit.CurrentCritDamage * 100).ToString("F1")}%";

			if (mDamageBonusText != null)
				mDamageBonusText.text = $"增伤量: {(mSelectedUnit.CurrentDamageBonus * 100).ToString("F1")}%";

			if (mResistanceText != null)
				mResistanceText.text = $"抗性: {(mSelectedUnit.CurrentResistance * 100).ToString("F1")}%";

			if (mShieldText != null)
				mShieldText.text = $"护盾: {mSelectedUnit.Shield}";

			RefreshStatusEffectsPanel();
		}

		private void RefreshStatusEffectsPanel()
		{
			if (mStatusEffectsPanel == null)
			{
				LogKit.Warning("[UnitStatsPanel] mStatusEffectsPanel is null! Please bind it in Inspector.");
				return;
			}

			LogKit.Log($"[UnitStatsPanel] mStatusEffectsPanel name: {mStatusEffectsPanel.name}, childCount: {mStatusEffectsPanel.childCount}");

			Transform contentTransform = GetContentTransform();
			if (contentTransform == null)
			{
				LogKit.Warning("[UnitStatsPanel] contentTransform is null!");
				return;
			}

			LogKit.Log($"[UnitStatsPanel] contentTransform name: {contentTransform.name}, childCount: {contentTransform.childCount}");

			SetupStatusEffectsScrollView(contentTransform);

			for (int i = contentTransform.childCount - 1; i >= 0; i--)
			{
				Destroy(contentTransform.GetChild(i).gameObject);
			}

			// 1. 显示角色特殊状态（刺杀、神鬼无赦等）
			CreateSpecialStateTexts(mSelectedUnit, contentTransform);

			// 2. 显示常规状态效果，跳过Value=0的（避免显示"攻击力加成0%"等无意义信息）
			//    例外：Fear（惶恐）即使Value=0也要显示，因为它是机制性状态
			var effects = GetStatusEffects(mSelectedUnit);
			if (effects != null)
			{
				foreach (var effect in effects)
				{
					// 跳过Value=0的非机制性状态（AttackUp=0、SpeedUp=0等）
					// Fear是机制性状态，即使Value=0也要显示
					if (effect.Value == 0 && effect.Type != StatusEffectType.Fear)
					{
						LogKit.Log($"[UnitStatsPanel] 跳过0值状态: {effect.Type}");
						continue;
					}

					CreateStatusEffectText(effect, contentTransform);
				}
			}
		}

		/// <summary>
		/// 显示角色特殊状态（刺杀状态、神鬼无赦状态等）。
		/// 这些状态不是StatusEffect，而是BattleUnitData上的独立字段。
		/// </summary>
		private void CreateSpecialStateTexts(BattleUnitData unit, Transform parentTransform)
		{
			if (unit == null || parentTransform == null) return;

			// 刺杀状态
			if (unit.AssassinationState > 0)
			{
				CreateSpecialStateText(parentTransform, "刺杀状态", unit.AssassinationState, true);
			}

			// 神鬼无赦状态
			if (unit.GodSlayerState > 0)
			{
				CreateSpecialStateText(parentTransform, "神鬼无赦", unit.GodSlayerState, true);
			}
		}

		/// <summary>
		/// 创建特殊状态的文本显示。
		/// </summary>
		private void CreateSpecialStateText(Transform parentTransform, string stateName, int duration, bool isBuff)
		{
			GameObject textObj = new GameObject($"Special_{stateName}");
			textObj.transform.SetParent(parentTransform, false);

			Text text = textObj.AddComponent<Text>();
			if (mDefaultFont != null)
				text.font = mDefaultFont;

			text.text = $"{stateName}（{duration}回合）";
			text.fontSize = 25;
			text.alignment = TextAnchor.MiddleLeft;
			text.color = isBuff ? new Color(0.0654f, 0.2772f, 0.9245f) : new Color(0.9254f, 0.0753f, 0.0666f);

			RectTransform rt = textObj.GetComponent<RectTransform>();
			rt.sizeDelta = new Vector2(0, 30);
		}

		private Transform GetContentTransform()
		{
			if (mStatusEffectsPanel == null)
				return null;

			for (int i = 0; i < mStatusEffectsPanel.childCount; i++)
			{
				Transform child = mStatusEffectsPanel.GetChild(i);
				if (child.name == "Scroll View" || child.name == "ScrollView")
				{
					for (int j = 0; j < child.childCount; j++)
					{
						Transform viewport = child.GetChild(j);
						if (viewport.name == "Viewport")
						{
							for (int k = 0; k < viewport.childCount; k++)
							{
								Transform content = viewport.GetChild(k);
								if (content.name == "Content")
								{
									return content;
								}
							}
						}
					}
				}
			}

			return mStatusEffectsPanel;
		}

		/// <summary>
		/// 配置状态效果滚动区域及其布局组件。
		/// </summary>
		private void SetupStatusEffectsScrollView(Transform contentTransform)
		{
			ScrollRect scrollRect = mStatusEffectsPanel.GetComponentInChildren<ScrollRect>();
			if (scrollRect != null)
			{
				scrollRect.horizontal = false;
				scrollRect.vertical = true;
				scrollRect.movementType = ScrollRect.MovementType.Clamped;
				scrollRect.inertia = false;
			}

			RectTransform panelRect = mStatusEffectsPanel.GetComponent<RectTransform>();
			if (panelRect != null)
			{
				RectTransform scrollViewRect = scrollRect?.GetComponent<RectTransform>();
				if (scrollViewRect != null)
				{
					scrollViewRect.offsetMin = Vector2.zero;
					scrollViewRect.offsetMax = Vector2.zero;
					scrollViewRect.anchorMin = Vector2.zero;
					scrollViewRect.anchorMax = Vector2.one;
				}

				Transform viewport = scrollRect?.GetComponentInChildren<Mask>()?.transform;
				if (viewport != null)
				{
					RectTransform viewportRect = viewport.GetComponent<RectTransform>();
					if (viewportRect != null)
					{
						viewportRect.offsetMin = Vector2.zero;
						viewportRect.offsetMax = Vector2.zero;
						viewportRect.anchorMin = Vector2.zero;
						viewportRect.anchorMax = Vector2.one;
					}
				}

				RectTransform contentRect = contentTransform.GetComponent<RectTransform>();
				if (contentRect != null)
				{
					contentRect.offsetMin = new Vector2(0, 0);
					contentRect.offsetMax = new Vector2(0, 0);
					contentRect.anchorMin = new Vector2(0, 1);
					contentRect.anchorMax = new Vector2(1, 1);
					contentRect.pivot = new Vector2(0.5f, 1f);
				}
			}

			HorizontalLayoutGroup hLayout = contentTransform.GetComponent<HorizontalLayoutGroup>();
			if (hLayout != null)
				Destroy(hLayout);

			VerticalLayoutGroup vLayout = contentTransform.GetComponent<VerticalLayoutGroup>();
			if (vLayout == null)
			{
				vLayout = contentTransform.gameObject.AddComponent<VerticalLayoutGroup>();
			}
			vLayout.spacing = 5;
			vLayout.childAlignment = TextAnchor.UpperLeft;
			vLayout.childControlWidth = true;
			vLayout.childControlHeight = false;
			vLayout.childForceExpandWidth = true;
			vLayout.childForceExpandHeight = false;
			vLayout.padding = new RectOffset(5, 5, 5, 5);

			ContentSizeFitter sizeFitter = contentTransform.GetComponent<ContentSizeFitter>();
			if (sizeFitter == null)
			{
				sizeFitter = contentTransform.gameObject.AddComponent<ContentSizeFitter>();
			}
			sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
			sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

			GridLayoutGroup gridLayout = contentTransform.GetComponent<GridLayoutGroup>();
			if (gridLayout != null)
				Destroy(gridLayout);
		}

		private void CreateStatusEffectText(StatusEffect effect, Transform parentTransform)
		{
			if (parentTransform == null)
				return;

			GameObject textObj = new GameObject($"Effect_{effect.Type}");
			textObj.transform.SetParent(parentTransform, false);

			Text text = textObj.AddComponent<Text>();
			if (mDefaultFont != null)
				text.font = mDefaultFont;

			string effectName = GetStatusEffectName(effect.Type);
			// 机制性状态（Fear等Value=0）只显示名称和持续回合，不显示无意义的"+0%"
			if (effect.Type == StatusEffectType.Fear)
			{
				text.text = $"{effectName}（{effect.Duration}回合）";
			}
			else
			{
				text.text = $"{effectName} +{effect.Value}% ({effect.Duration}回合)";
			}
			text.fontSize = 25;
			text.alignment = TextAnchor.MiddleLeft;
			text.color = IsDebuff(effect.Type) ? new Color(0.9254f, 0.0753f, 0.0666f) : new Color(0.0654f, 0.2772f, 0.9245f);

			RectTransform rt = textObj.GetComponent<RectTransform>();
			rt.sizeDelta = new Vector2(0, 30);
		}

		private bool IsDebuff(StatusEffectType type)
		{
			return type == StatusEffectType.AttackDown ||
				   type == StatusEffectType.DefenseDown ||
				   type == StatusEffectType.SpeedDown ||
				   type == StatusEffectType.Fear;
		}

		private List<StatusEffect> GetStatusEffects(BattleUnitData unit)
		{
			System.Reflection.FieldInfo field = unit.GetType().GetField("mStatusEffects", 
				System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			if (field != null)
			{
				return field.GetValue(unit) as List<StatusEffect>;
			}
			return new List<StatusEffect>();
		}

		private string GetStatusEffectName(StatusEffectType type)
		{
			switch (type)
			{
				case StatusEffectType.AttackUp: return "攻击力提升";
				case StatusEffectType.AttackDown: return "攻击力降低";
				case StatusEffectType.DefenseUp: return "防御力提升";
				case StatusEffectType.DefenseDown: return "防御力降低";
				case StatusEffectType.SpeedUp: return "速度提升";
				case StatusEffectType.SpeedDown: return "速度降低";
				case StatusEffectType.Shield: return "护盾";
				case StatusEffectType.DamageUp: return "伤害提升";
				case StatusEffectType.CritRateUp: return "暴击率提升";
				case StatusEffectType.CritDamageUp: return "暴击伤害提升";
				case StatusEffectType.DefensePenetration: return "穿透";
				case StatusEffectType.Fear: return "惶恐";
				default: return "未知";
			}
		}

		private void ShowStats()
		{
			if (mUnitNameText != null) mUnitNameText.gameObject.SetActive(true);
			if (mHpText != null) mHpText.gameObject.SetActive(true);
			if (mAttackText != null) mAttackText.gameObject.SetActive(true);
			if (mDefenseText != null) mDefenseText.gameObject.SetActive(true);
			if (mSpeedText != null) mSpeedText.gameObject.SetActive(true);
			if (mCritRateText != null) mCritRateText.gameObject.SetActive(true);
			if (mCritDamageText != null) mCritDamageText.gameObject.SetActive(true);
			if (mDamageBonusText != null) mDamageBonusText.gameObject.SetActive(true);
			if (mResistanceText != null) mResistanceText.gameObject.SetActive(true);
			if (mShieldText != null) mShieldText.gameObject.SetActive(true);
			if (mStatusEffectsPanel != null) mStatusEffectsPanel.gameObject.SetActive(true);
		}

		/// <summary>
		/// 隐藏属性区域及状态效果面板。
		/// </summary>
		private void HideStats()
		{
			if (mUnitNameText != null) mUnitNameText.gameObject.SetActive(false);
			if (mHpText != null) mHpText.gameObject.SetActive(false);
			if (mAttackText != null) mAttackText.gameObject.SetActive(false);
			if (mDefenseText != null) mDefenseText.gameObject.SetActive(false);
			if (mSpeedText != null) mSpeedText.gameObject.SetActive(false);
			if (mCritRateText != null) mCritRateText.gameObject.SetActive(false);
			if (mCritDamageText != null) mCritDamageText.gameObject.SetActive(false);
			if (mDamageBonusText != null) mDamageBonusText.gameObject.SetActive(false);
			if (mResistanceText != null) mResistanceText.gameObject.SetActive(false);
			if (mShieldText != null) mShieldText.gameObject.SetActive(false);
			if (mStatusEffectsPanel != null) mStatusEffectsPanel.gameObject.SetActive(false);
		}

		private void OnToggleUnitSideClicked()
		{
			mShowPlayerUnits = !mShowPlayerUnits;
			UpdateToggleButtonText();
			// 切换阵营后清除当前选中的单位，避免显示另一阵营的过期属性。
			mSelectedUnit = null;
			RefreshUnitList();
			RefreshStats();
		}

		private void UpdateToggleButtonText()
		{
			if (mToggleUnitSideButton == null)
				return;

			Text buttonText = mToggleUnitSideButton.GetComponentInChildren<Text>();
			if (buttonText != null)
			{
				buttonText.text = mShowPlayerUnits ? "显示敌方" : "显示我方";
			}
		}

		/// <summary>
		/// 关闭单位属性面板。
		/// </summary>
		private void OnCloseClicked()
		{
			UIKit.ClosePanel<UnitStatsPanel>();
		}
		#endregion
	}
}

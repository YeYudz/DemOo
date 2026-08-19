using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
using YokiFrame;
using TurnManagement;
using EnergyEvents;
using CardEvents;
using WaveEvents;
using UnitEvents;
using StateEvents;
using GameUI;
using Game;
using Game.GameLogic.Battle.Core;
using static GameUI.UIEvents;

namespace GameUI
{
	public sealed class BattlePanelData : IUIData
	{
		public BattleSystem BattleSystem { get; set; }
	}

	/// <summary>
	/// 战斗面板：显示单位、手牌、行动顺序并响应战斗事件。
	/// </summary>
	public partial class BattlePanel : UIPanel
	{
		#region 生命周期与战斗显示

		private SkillCardData mSelectedCard;
		private Font mDefaultFont;
		private bool mIsNormalAttackMode = false;
		private bool mIsSkillTargetSelectionMode = false;
		private TargetType mCurrentTargetType = TargetType.Enemy;

		private string mCurrentActorId = "";
		private BattleUnitType mCurrentActorType = BattleUnitType.Player;
		private int mCurrentEnergy = 0;
		private int mMaxEnergy = 0;
		private int mManualDiscardCost = 0;
		private List<BattleUnitData> mPlayerUnits = new List<BattleUnitData>();
		private List<BattleUnitData> mEnemyUnits = new List<BattleUnitData>();
		private List<SkillCardData> mHandCards = new List<SkillCardData>();
		private List<ITurnOrder> mTurnOrderUnits = new List<ITurnOrder>();

		private class CardComponentCache
		{
			public RectTransform Rect { get; set; }
			public Image CardBg { get; set; }
			public Image ImgSkill { get; set; }
			public Text TexCost { get; set; }
			public Text TexName { get; set; }
			public Button BtnUse { get; set; }
			public Button BtnCheck { get; set; }
		}

		private Dictionary<GameObject, CardComponentCache> mCardComponentCache = new Dictionary<GameObject, CardComponentCache>();
		private readonly Dictionary<GameObject, Material> mCardFlowMaterials = new Dictionary<GameObject, Material>();
		private Material mCardFlowMaterialTemplate;

		private class CardPool : PoolKit<GameObject>
		{
			private Transform mParent;
			private BattlePanel mPanel;

			public CardPool(Transform parent, BattlePanel panel)
			{
				mParent = parent;
				mPanel = panel;
				SetFactoryMethod(() =>
				{
					var prefab = ResKit.Load<GameObject>("SkillCard");
#if UNITY_EDITOR
					prefab ??= AssetDatabase.LoadAssetAtPath<GameObject>(
						"Assets/Resources/UI/Battle/SkillCard.prefab");
#endif
					if (prefab == null)
					{
						LogKit.Error(
							"[BattlePanel] 未找到手牌预制体 SkillCard。请确认 UI Object Collector " +
							"已构建并发布当前 Windows 资源包。"
						);
						return null;
					}

					var cardObj = UnityEngine.Object.Instantiate(prefab, mParent);
					mPanel.CacheCardComponents(cardObj);
					return cardObj;
				});
			}

			public override bool Recycle(GameObject obj)
			{
				if (obj == null) return false;
				obj.SetActive(false);
				PushCachedObject(obj);
				return true;
			}
		}

		private void CacheCardComponents(GameObject cardObj)
		{
			if (mCardComponentCache.ContainsKey(cardObj))
				return;

			var cache = new CardComponentCache();
			cache.Rect = cardObj.GetComponent<RectTransform>();
			cache.CardBg = cardObj.GetComponent<Image>();
			
			Transform imgSkillTransform = cardObj.transform.Find("imgSkill");
			if (imgSkillTransform != null)
				cache.ImgSkill = imgSkillTransform.GetComponent<Image>();
			
			Transform texCostTransform = cardObj.transform.Find("texCost");
			if (texCostTransform != null)
				cache.TexCost = texCostTransform.GetComponent<Text>();
			
			Transform texNameTransform = cardObj.transform.Find("texName");
			if (texNameTransform != null)
				cache.TexName = texNameTransform.GetComponent<Text>();
			
			Transform btnUseTransform = cardObj.transform.Find("btnUse");
			if (btnUseTransform != null)
				cache.BtnUse = btnUseTransform.GetComponent<Button>();
			
			Transform btnCheckTransform = cardObj.transform.Find("btnCheck");
			if (btnCheckTransform != null)
				cache.BtnCheck = btnCheckTransform.GetComponent<Button>();

			mCardComponentCache[cardObj] = cache;
		}

		private CardPool mCardPool;
		private List<GameObject> mActiveCards = new List<GameObject>();

		protected override void OnInit(IUIData uiData = null)
		{
			mData = uiData as BattlePanelData ?? new BattlePanelData();
			LoadDefaultFont();
		}

		protected override void OnOpen(IUIData uiData = null)
		{
			mData = uiData as BattlePanelData ?? mData;

			// 播放战斗背景音乐与开始音效
			AudioManager.Instance.PlayBGM(AudioManager.BGM_Battle);
			AudioManager.Instance.PlaySFX(AudioManager.SFX_BattleStart);

			// 创建战斗表现层播放器
			BattleActionPlayer.Create();

			// 注册敌方/我方面板引用，供表现层遍历查找单位位置
			if (BattleActionPlayer.Instance != null)
			{
				BattleActionPlayer.Instance.RegisterUnitPanels(mEnemyUnitsPanel, mPlayerUnitsPanel);
				BattleActionPlayer.Instance.RegisterHandCardsPanel(mHandCardsPanel);
			}

			RegisterEvents();

			EventKit.Type.Send(new BattlePanelOpenedEvent());

			if (mNormalAttackButton != null)
			{
				mNormalAttackButton.onClick.RemoveAllListeners();
				mNormalAttackButton.onClick.AddListener(OnNormalAttackClicked);
				mNormalAttackButton.interactable = false;
			}

			if (mManualDiscardButton != null)
			{
				mManualDiscardButton.onClick.RemoveAllListeners();
				mManualDiscardButton.onClick.AddListener(OnManualDiscardClicked);
				mManualDiscardButton.interactable = false;
			}

			if (mShowStatsButton != null)
			{
				mShowStatsButton.onClick.RemoveAllListeners();
				mShowStatsButton.onClick.AddListener(OnShowStatsClicked);
			}

			if (mBtnQuit != null)
			{
				mBtnQuit.onClick.RemoveAllListeners();
				mBtnQuit.onClick.AddListener(OnQuitButtonClicked);
			}

			if (mBtnSure != null)
			{
				mBtnSure.onClick.RemoveAllListeners();
				mBtnSure.onClick.AddListener(OnSureQuitButtonClicked);
			}

			if (mBtnCancel != null)
			{
				mBtnCancel.onClick.RemoveAllListeners();
				mBtnCancel.onClick.AddListener(OnCancelQuitButtonClicked);
			}

			if (mPanelQuit != null)
			{
				mPanelQuit.SetActive(false);
			}

			if (mBattleLogText != null)
			{
				mBattleLogText.text = "战斗开始！";
			}

			if (mTurnOrderScrollView != null)
			{
				mTurnOrderScrollView.horizontal = true;
				mTurnOrderScrollView.vertical = false;
				mTurnOrderScrollView.movementType = ScrollRect.MovementType.Clamped;
				mTurnOrderScrollView.inertia = false;
			}

			RefreshUI();
		}

		protected override void OnShow()
		{
			RefreshUI();
		}

		protected override void OnHide()
		{
		}

		protected override void OnClose()
		{
			UnregisterEvents();

			// 停止战斗背景音乐
			AudioManager.Instance.StopBGM();

			// 销毁战斗表现层播放器
			BattleActionPlayer.DestroySelf();

			EventKit.Type.Send(new BattlePanelClosedEvent());
		}

		private void RegisterEvents()
		{
			EventKit.Type.Register<BattleUIDataEvent>(OnBattleUIData);
			EventKit.Type.Register<BattleStateChangeEvent>(OnBattleStateChange);
			EventKit.Type.Register<EnergyChangedEvent>(OnEnergyChanged);
			EventKit.Type.Register<CardPlayedEvent>(OnCardPlayed);
			EventKit.Type.Register<HandUpdatedEvent>(OnHandUpdated);
			EventKit.Type.Register<UnitDamagedEvent>(OnUnitDamaged);
			EventKit.Type.Register<UnitHealedEvent>(OnUnitHealed);
			EventKit.Type.Register<UnitDefeatedEvent>(OnUnitDefeated);
			EventKit.Type.Register<UnitSpawnedEvent>(OnUnitSpawned);
			EventKit.Type.Register<EnemySpawnEvent>(OnEnemySpawn);
			EventKit.Type.Register<WaveCompleteEvent>(OnWaveComplete);
			EventKit.Type.Register<BattleWinEvent>(OnBattleWin);
			EventKit.Type.Register<BattleLoseEvent>(OnBattleLose);
			EventKit.Type.Register<TurnEvents.TurnOrderUpdatedEvent>(OnTurnOrderUpdated);
			EventKit.Type.Register<TurnEvents.UnitActingEvent>(OnUnitActing);
			EventKit.Type.Register<TurnEvents.ActionCompleteEvent>(OnActionComplete);
			EventKit.Type.Register<UnitEvents.UnitHealthUpdatedEvent>(OnUnitHealthUpdated);
			EventKit.Type.Register<UnitEvents.UnitDamagedEvent>(OnUnitDamagedForLog);
			EventKit.Type.Register<UnitEvents.UnitHealedEvent>(OnUnitHealedForLog);
		}

		private void UnregisterEvents()
		{
			EventKit.Type.UnRegister<BattleUIDataEvent>(OnBattleUIData);
			EventKit.Type.UnRegister<BattleStateChangeEvent>(OnBattleStateChange);
			EventKit.Type.UnRegister<EnergyChangedEvent>(OnEnergyChanged);
			EventKit.Type.UnRegister<CardPlayedEvent>(OnCardPlayed);
			EventKit.Type.UnRegister<HandUpdatedEvent>(OnHandUpdated);
			EventKit.Type.UnRegister<UnitDamagedEvent>(OnUnitDamaged);
			EventKit.Type.UnRegister<UnitHealedEvent>(OnUnitHealed);
			EventKit.Type.UnRegister<UnitDefeatedEvent>(OnUnitDefeated);
			EventKit.Type.UnRegister<UnitSpawnedEvent>(OnUnitSpawned);
			EventKit.Type.UnRegister<EnemySpawnEvent>(OnEnemySpawn);
			EventKit.Type.UnRegister<WaveCompleteEvent>(OnWaveComplete);
			EventKit.Type.UnRegister<BattleWinEvent>(OnBattleWin);
			EventKit.Type.UnRegister<BattleLoseEvent>(OnBattleLose);
			EventKit.Type.UnRegister<TurnEvents.TurnOrderUpdatedEvent>(OnTurnOrderUpdated);
			EventKit.Type.UnRegister<TurnEvents.UnitActingEvent>(OnUnitActing);
			EventKit.Type.UnRegister<TurnEvents.ActionCompleteEvent>(OnActionComplete);
			EventKit.Type.UnRegister<UnitEvents.UnitHealthUpdatedEvent>(OnUnitHealthUpdated);
			EventKit.Type.UnRegister<UnitEvents.UnitDamagedEvent>(OnUnitDamagedForLog);
			EventKit.Type.UnRegister<UnitEvents.UnitHealedEvent>(OnUnitHealedForLog);
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

		private void RefreshUI()
		{
			RefreshPlayerUnits();
			RefreshEnemyUnits();
			RefreshHandCards();
			RefreshEnergy();
			RefreshTurnOrder();
			RefreshTurnOrderVisual();
			UpdateNormalAttackButton();
			UpdateManualDiscardButton();
			UpdateShowStatsButton();
			RefreshWaveInfo();
		}

		private void RefreshPlayerUnits()
		{
			if (mPlayerUnitsPanel == null)
				return;

			// 清除表现层播放器中的单位视图注册
			if (BattleActionPlayer.Instance != null)
			{
				foreach (var unit in mPlayerUnits)
				{
					if (unit != null)
						BattleActionPlayer.Instance.UnregisterUnitView(unit.UnitId);
				}
			}

			foreach (Transform child in mPlayerUnitsPanel)
			{
				Object.Destroy(child.gameObject);
			}

			if (!mPlayerUnitsPanel.GetComponent<HorizontalLayoutGroup>())
			{
				HorizontalLayoutGroup layout = mPlayerUnitsPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
				layout.spacing = 10;
				layout.childAlignment = TextAnchor.MiddleLeft;
			}

			// 按UnitId字符串匹配当前行动者（避免引用不一致导致高亮失效）
			// 仅在我方角色回合时高亮我方面板的角色
			bool isPlayerTurn = mCurrentActorType == BattleUnitType.Player;
			foreach (var unit in mPlayerUnits)
			{
				bool isCurrentActor = isPlayerTurn && unit.UnitId == mCurrentActorId;
				CreateUnitDisplay(unit, mPlayerUnitsPanel, Color.green, isCurrentActor);
			}
		}

		private void RefreshEnemyUnits()
		{
			if (mEnemyUnitsPanel == null)
				return;

			// 清除表现层播放器中的单位视图注册
			if (BattleActionPlayer.Instance != null)
			{
				foreach (var unit in mEnemyUnits)
				{
					if (unit != null)
						BattleActionPlayer.Instance.UnregisterUnitView(unit.UnitId);
				}
			}

			foreach (Transform child in mEnemyUnitsPanel)
			{
				Object.Destroy(child.gameObject);
			}

			if (!mEnemyUnitsPanel.GetComponent<HorizontalLayoutGroup>())
			{
				HorizontalLayoutGroup layout = mEnemyUnitsPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
				layout.spacing = 10;
				layout.childAlignment = TextAnchor.MiddleLeft;
			}

			// 按UnitId字符串匹配当前行动者（避免引用不一致导致高亮失效）
			// 仅在敌方角色回合时高亮敌方面板的角色
			bool isEnemyTurn = mCurrentActorType == BattleUnitType.Enemy;
			foreach (var unit in mEnemyUnits)
			{
				bool isCurrentActor = isEnemyTurn && unit.UnitId == mCurrentActorId;
				CreateUnitDisplay(unit, mEnemyUnitsPanel, Color.red, isCurrentActor);
			}
		}

		private void CreateUnitDisplay(BattleUnitData unit, Transform parent, Color hpColor, bool isCurrentActor)
		{
			if (unit == null || parent == null)
				return;

			GameObject unitObj = new GameObject(unit.UnitId);
			unitObj.transform.SetParent(parent, false);
			float scale = 1f;

			RectTransform rect = unitObj.AddComponent<RectTransform>();
			rect.sizeDelta = new Vector2(100 * scale, 120 * scale);

			// 注册到表现层播放器，供技能视觉效果获取单位位置
			if (BattleActionPlayer.Instance != null)
			{
				BattleActionPlayer.Instance.RegisterUnitView(unit.UnitId, rect);
			}

			Image bg = unitObj.AddComponent<Image>();
			bg.color = unit.UnitType == BattleUnitType.Player ? new Color(0.1f, 0.3f, 0.5f, 0.8f) : new Color(0.5f, 0.1f, 0.1f, 0.8f);

			if (isCurrentActor)
			{
				// 当前行动角色添加淡黄色高亮
				GameObject glow = new GameObject("Glow");
				glow.transform.SetParent(unitObj.transform, false);
				RectTransform glowRect = glow.AddComponent<RectTransform>();
				glowRect.sizeDelta = new Vector2(110, 130);
				glowRect.localPosition = Vector3.zero;
				Image glowImage = glow.AddComponent<Image>();
				glowImage.color = new Color(1f, 0.92f, 0.5f, 0.4f);
				glow.transform.SetSiblingIndex(0);
			}

			GameObject iconBg = new GameObject("IconBg");
			iconBg.transform.SetParent(unitObj.transform, false);
			RectTransform iconBgRect = iconBg.AddComponent<RectTransform>();
			iconBgRect.sizeDelta = new Vector2(80 * scale, 80 * scale);
			iconBgRect.localPosition = new Vector3(0, 20 * scale, 0);
			Image iconBgImage = iconBg.AddComponent<Image>();
			iconBgImage.color = isCurrentActor ? new Color(1f, 0.8f, 0.3f) : Color.white;

			GameObject icon = new GameObject("Icon");
			icon.transform.SetParent(iconBg.transform, false);
			RectTransform iconRect = icon.AddComponent<RectTransform>();
			iconRect.sizeDelta = new Vector2(60 * scale, 60 * scale);
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

			GameObject hpBarBg = new GameObject("HpBarBg");
			hpBarBg.transform.SetParent(unitObj.transform, false);
			RectTransform hpBgRect = hpBarBg.AddComponent<RectTransform>();
			hpBgRect.sizeDelta = new Vector2(90 * scale, 10 * scale);
			hpBgRect.localPosition = new Vector3(0, -35 * scale, 0);
			Image hpBgImage = hpBarBg.AddComponent<Image>();
			hpBgImage.color = Color.black;

			GameObject hpBar = new GameObject("HpBar");
			hpBar.transform.SetParent(unitObj.transform, false);
			RectTransform hpBarRect = hpBar.AddComponent<RectTransform>();
			float hpPercent = (float)unit.CurrentHp / unit.MaxHp;
			hpBarRect.sizeDelta = new Vector2(90 * hpPercent * scale, 10 * scale);
			hpBarRect.localPosition = new Vector3(-45 * scale + hpBarRect.sizeDelta.x / 2, -35 * scale, 0);
			Image hpBarImage = hpBar.AddComponent<Image>();
			hpBarImage.color = hpColor;

			GameObject hpTextObj = new GameObject("HpText");
			hpTextObj.transform.SetParent(unitObj.transform, false);
			RectTransform hpTextRect = hpTextObj.AddComponent<RectTransform>();
			hpTextRect.sizeDelta = new Vector2(100 * scale, 15 * scale);
			hpTextRect.localPosition = new Vector3(0, -50 * scale, 0);
			Text hpText = hpTextObj.AddComponent<Text>();
			if (mDefaultFont != null)
				hpText.font = mDefaultFont;
			hpText.text = $"{unit.CurrentHp}/{unit.MaxHp}";
			hpText.fontSize = (int)(11 * scale);
			hpText.alignment = TextAnchor.MiddleCenter;
			hpText.color = isCurrentActor ? Color.yellow : Color.white;

			Button button = unitObj.AddComponent<Button>();
			if (unit.UnitType == BattleUnitType.Enemy)
			{
				button.onClick.AddListener(() => OnEnemyClicked(unit));
			}
			else if (unit.UnitType == BattleUnitType.Player)
			{
				button.onClick.AddListener(() => OnPlayerClicked(unit));
			}
		}

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

		private void RefreshHandCards()
		{
			if (mHandCardsPanel == null)
				return;

			if (mCardPool == null)
			{
				mCardPool = new CardPool(mHandCardsPanel, this);
			}

			foreach (var cardObj in mActiveCards)
			{
				if (mCardFlowMaterials.TryGetValue(cardObj, out var material))
				{
					Object.Destroy(material);
					mCardFlowMaterials.Remove(cardObj);
				}

				Transform skillCheck = cardObj.transform.Find("SkillCheck");
				if (skillCheck != null)
				{
					Object.Destroy(skillCheck.gameObject);
				}
				Image imgSkill = cardObj.transform.Find("imgSkill")?.GetComponent<Image>();
				if (imgSkill != null)
				{
					imgSkill.sprite = null;
				}
				mCardPool.Recycle(cardObj);
			}
			mActiveCards.Clear();

			if (!mHandCardsPanel.GetComponent<HorizontalLayoutGroup>())
			{
				HorizontalLayoutGroup layout = mHandCardsPanel.gameObject.AddComponent<HorizontalLayoutGroup>();
				layout.spacing = 10;
				layout.childAlignment = TextAnchor.MiddleCenter;
			}

			var handCards = mHandCards;
			float cardSize = CalculateCardSize(handCards.Count);

			foreach (var card in handCards)
			{
				CreateCardDisplay(card, cardSize);
			}

			LogKit.Log(
				$"[BattlePanel] 手牌刷新完成: dataCount={handCards.Count}, " +
				$"activeCardCount={mActiveCards.Count}, childCount={mHandCardsPanel.childCount}"
			);
		}

		private float CalculateCardSize(int cardCount)
		{
			if (mHandCardsPanel == null)
				return 80f;

			float panelWidth = mHandCardsPanel.GetComponent<RectTransform>().rect.width;
			float spacing = 10f;
			float totalSpacing = 9 * spacing;
			float availableWidth = panelWidth - totalSpacing - 40f;
			return availableWidth / 10f;
		}

		private void ApplyCardRarityFlow(GameObject cardObj, Image cardBg, SkillCardData card)
		{
			if (cardObj == null || cardBg == null)
				return;

			if (mCardFlowMaterialTemplate == null)
			{
				mCardFlowMaterialTemplate = ResKit.Load<Material>("UI_CardRarityFlow");
#if UNITY_EDITOR
				mCardFlowMaterialTemplate ??= AssetDatabase.LoadAssetAtPath<Material>(
					"Assets/UIEffects/UI_CardRarityFlow.mat");
#endif
			}

			if (mCardFlowMaterialTemplate == null)
			{
				LogKit.Error("[BattlePanel] 未找到 UI_CardRarityFlow；请确认 YooAsset 已构建 UIEffects Collector，或编辑器资源路径存在。");
				return;
			}

			if (!mCardFlowMaterials.TryGetValue(cardObj, out var material) || material == null)
			{
				material = new Material(mCardFlowMaterialTemplate);
				mCardFlowMaterials[cardObj] = material;
				cardBg.material = material;
			}

			int rarity = GetCardRarity(card);
			Color flowColor = rarity >= 5 ? new Color(1f, 0.82f, 0.25f) :
				rarity == 4 ? new Color(0.78f, 0.45f, 1f) : new Color(0.4f, 0.75f, 1f);
			material.SetColor("_FlowColor", flowColor);
			material.SetFloat("_FlowSpeed", rarity >= 5 ? 0.58f : rarity == 4 ? 0.45f : 0.32f);
			material.SetFloat("_FlowIntensity", rarity >= 5 ? 1.6f : rarity == 4 ? 1.25f : 0.9f);
			material.SetFloat("_PhaseOffset", Mathf.Repeat(cardObj.GetInstanceID() * 0.6180339f, 1f));
		}

		private int GetCardRarity(SkillCardData card)
		{
			var character = GameDataMgr.Instance?.playerCharacterConfigs?.Characters
				.FirstOrDefault(config => config.CharacterId == card.CharacterId);
			return character?.Rarity ?? 3;
		}

		private void CreateCardDisplay(SkillCardData card, float cardWidth)
		{
			GameObject cardObj = mCardPool.Allocate();
			if (cardObj == null)
				return;

			cardObj.SetActive(true);
			cardObj.transform.SetParent(mHandCardsPanel, false);
			cardObj.name = card.SkillId;
			mActiveCards.Add(cardObj);

			if (!mCardComponentCache.TryGetValue(cardObj, out var cache))
			{
				CacheCardComponents(cardObj);
				mCardComponentCache.TryGetValue(cardObj, out cache);
			}

			if (cache == null)
			{
				LogKit.Error($"[BattlePanel] 卡牌组件缓存失败: {cardObj.name}");
				return;
			}

			LogKit.Log(
				$"[BattlePanel] 绑定手牌: id={card.SkillId}, name={card.Name}, cost={card.Cost}, " +
				$"img={cache.ImgSkill != null}, costText={cache.TexCost != null}, " +
				$"nameText={cache.TexName != null}, useButton={cache.BtnUse != null}"
			);

			if (cache.Rect != null)
			{
				cache.Rect.sizeDelta = new Vector2(cardWidth, cardWidth * 1.4f);
			}

			if (cache.CardBg == null)
				cache.CardBg = cardObj.AddComponent<Image>();

			ApplyCardRarityFlow(cardObj, cache.CardBg, card);

			if (cache.ImgSkill != null)
			{
				cache.ImgSkill.transform.localScale = Vector3.one;
				string spriteSkillId = StripCardIndexSuffix(card.SkillId);
				SpriteIconLoader.SetSkillIcon(cache.ImgSkill, spriteSkillId);
				if (cache.ImgSkill.sprite == null)
				{
					cache.ImgSkill.color = GetCardColor(card.EffectType);
				}
				else
				{
					cache.ImgSkill.color = Color.white;
				}
			}

			int effectiveCost = GetEffectiveCardCost(card);
			if (cache.TexCost != null)
			{
				if (mDefaultFont != null)
					cache.TexCost.font = mDefaultFont;
				cache.TexCost.text = effectiveCost.ToString();
				cache.TexCost.fontSize = Mathf.RoundToInt(cardWidth * 0.18f);
				// 星魂减费时用绿色高亮显示
				int originalCost = card.Cost;
				cache.TexCost.color = (effectiveCost < originalCost) ? Color.green : Color.yellow;
				cache.TexCost.alignment = TextAnchor.MiddleCenter;
			}

			if (cache.TexName != null)
			{
				if (mDefaultFont != null)
					cache.TexName.font = mDefaultFont;
				cache.TexName.text = card.Name;
				cache.TexName.fontSize = Mathf.RoundToInt(cardWidth * 0.14f);
				cache.TexName.alignment = TextAnchor.MiddleCenter;
			}

			LogKit.Log(
				$"[BattlePanel] 手牌赋值完成: id={card.SkillId}, " +
				$"costText={cache.TexCost?.text}, nameText={cache.TexName?.text}, " +
				$"icon={(cache.ImgSkill?.sprite != null)}, object={cardObj.GetInstanceID()}"
			);

			int effCost = GetEffectiveCardCost(card);
			bool canAfford = mCurrentEnergy >= effCost;
			bool isCurrentActorCard = IsCurrentActorCard(card);

			ApplyCardVisualState(card, cache.CardBg, cache.ImgSkill, cache.TexName, canAfford, isCurrentActorCard);

			if (cache.BtnUse != null)
			{
				cache.BtnUse.onClick.RemoveAllListeners();
				cache.BtnUse.interactable = canAfford && isCurrentActorCard;
				cache.BtnUse.onClick.AddListener(() => OnCardClicked(card));
			}

			if (cache.BtnCheck != null)
			{
				cache.BtnCheck.onClick.RemoveAllListeners();
				cache.BtnCheck.interactable = true;
				cache.BtnCheck.onClick.AddListener(() => ShowSkillCheck(card, cardObj));
			}
		}

		private bool IsCurrentActorCard(SkillCardData card)
		{
			if (mCurrentActorType != BattleUnitType.Player || string.IsNullOrEmpty(mCurrentActorId))
				return false;

			return card.CharacterId == mCurrentActorId;
		}

		/// <summary>
		/// 获取卡牌的有效能量消耗（考虑星魂减费，如Mage星魂1使技能2消耗变0）。
		/// 星魂减费基于卡牌拥有者的星魂等级，与当前行动者无关。
		/// </summary>
		private int GetEffectiveCardCost(SkillCardData card)
		{
			// 星魂减费是角色自身属性，与当前行动者无关
			// 即使在其他角色回合，Mage星魂1的技能2仍应显示0消耗
			var handler = CharacterHandlerRegistry.Get(card.CharacterId);
			if (handler == null)
				return card.Cost;

			var playerUnit = mPlayerUnits.FirstOrDefault(u => u.UnitId == card.CharacterId);
			int starLevel = playerUnit != null ? playerUnit.StarLevel : 0;

			return handler.GetEffectiveSkillCost(card.SkillType, card.Cost, starLevel);
		}

		private void ApplyCardVisualState(SkillCardData card, Image cardBg, Image imgSkill, Text nameText, bool canAfford, bool isCurrentActorCard)
		{
			Color normalBg = new Color(0.5f, 0.15f, 0.25f);
			Color noEnergyColor = new Color(1f, 0.8f, 0.2f, 0.85f);   // 自己的牌但能量不够 - 金色

			if (!isCurrentActorCard)
			{
				// 非当前回合角色的卡牌 - 添加淡黑色遮罩
				cardBg.color = new Color(0.2f, 0.2f, 0.2f, 0.7f);
				if (imgSkill != null)
					imgSkill.color = imgSkill.sprite != null ? new Color(0.5f, 0.5f, 0.5f, 0.6f) : new Color(0.2f, 0.2f, 0.2f, 0.5f);
				if (nameText != null)
					nameText.color = new Color(0.6f, 0.6f, 0.6f);
			}
			else if (!canAfford)
			{
				// 自己的牌但能量不够 - 金色
				cardBg.color = noEnergyColor;
				if (imgSkill != null)
					imgSkill.color = imgSkill.sprite != null ? new Color(1f, 0.95f, 0.7f) : new Color(0.6f, 0.5f, 0.15f);
				if (nameText != null)
					nameText.color = new Color(0.8f, 0.6f, 0.1f);
			}
			else
			{
				// 自己的牌且能使用 - 无额外效果
				cardBg.color = normalBg;
				if (imgSkill != null)
					imgSkill.color = imgSkill.sprite != null ? Color.white : GetCardColor(card.EffectType);
				if (nameText != null)
					nameText.color = Color.white;
			}
		}

		private string StripCardIndexSuffix(string skillId)
		{
			if (string.IsNullOrEmpty(skillId)) return skillId;
			int lastUnderscore = skillId.LastIndexOf('_');
			if (lastUnderscore > 0 && int.TryParse(skillId.Substring(lastUnderscore + 1), out int index))
			{
				return skillId.Substring(0, lastUnderscore);
			}
			return skillId;
		}

		private Color GetCardColor(SkillEffectType effectType)
    {
        switch (effectType)
        {
            case SkillEffectType.Damage: return new Color(0.4f, 0.1f, 0.1f);
            case SkillEffectType.Heal: return new Color(0.1f, 0.4f, 0.1f);
            case SkillEffectType.EnergyGain: return new Color(0.1f, 0.2f, 0.5f);
            case SkillEffectType.DrawCard: return new Color(0.2f, 0.1f, 0.5f);
            case SkillEffectType.Shield: return new Color(0.4f, 0.4f, 0.1f);
            case SkillEffectType.AttackUp: return new Color(0.5f, 0.1f, 0.3f);
            case SkillEffectType.AttackDown: return new Color(0.3f, 0.1f, 0.1f);
            case SkillEffectType.DefenseUp: return new Color(0.1f, 0.3f, 0.1f);
            case SkillEffectType.DefenseDown: return new Color(0.3f, 0.1f, 0.1f);
            case SkillEffectType.SpeedUp: return new Color(0.1f, 0.4f, 0.4f);
            case SkillEffectType.SpeedDown: return new Color(0.3f, 0.3f, 0.1f);
            case SkillEffectType.DamageUp: return new Color(0.6f, 0.2f, 0.1f);
            case SkillEffectType.CritRateUp: return new Color(0.5f, 0.3f, 0.1f);
            case SkillEffectType.CritDamageUp: return new Color(0.6f, 0.4f, 0.1f);
            case SkillEffectType.DefensePenetration: return new Color(0.4f, 0.1f, 0.5f);
            case SkillEffectType.DiscardAndDraw: return new Color(0.3f, 0.1f, 0.5f);
            default: return new Color(0.3f, 0.3f, 0.5f);
        }
    }

		private GameObject mSkillCheck;
		private Button mSkillCheckBtnClose;
		private Text mSkillCheckTexDes;

		private void ShowSkillCheck(SkillCardData card, GameObject parentCard)
		{
			if (mSkillCheck == null)
			{
				var prefab = ResKit.Load<GameObject>("SkillCheck");
#if UNITY_EDITOR
				prefab ??= AssetDatabase.LoadAssetAtPath<GameObject>(
					"Assets/Resources/UI/Battle/SkillCheck.prefab");
#endif
				if (prefab == null)
				{
					LogKit.Warning("[BattlePanel] SkillCheck预制体不存在，请创建 Assets/Resources/UI/Battle/SkillCheck.prefab");
					return;
				}

				Transform toastTransform = transform.root.Find("Toast");
				if (toastTransform == null)
				{
					toastTransform = transform;
				}

				mSkillCheck = UnityEngine.Object.Instantiate(prefab, toastTransform, false);
				mSkillCheck.name = "SkillCheck";
				mSkillCheck.SetActive(false);

				Transform btnCloseTransform = mSkillCheck.transform.Find("btnClose");
				if (btnCloseTransform != null)
				{
					mSkillCheckBtnClose = btnCloseTransform.GetComponent<Button>();
					if (mSkillCheckBtnClose != null)
					{
						mSkillCheckBtnClose.onClick.RemoveAllListeners();
						mSkillCheckBtnClose.onClick.AddListener(() => HideSkillCheck());
					}
				}

				Transform texSkillDesTransform = mSkillCheck.transform.Find("texSkillDes");
				if (texSkillDesTransform != null)
				{
					mSkillCheckTexDes = texSkillDesTransform.GetComponent<Text>();
					if (mSkillCheckTexDes != null && mDefaultFont != null)
					{
						mSkillCheckTexDes.font = mDefaultFont;
					}
				}
			}

			// 居中显示：锚点和位置都设为屏幕正中心
			RectTransform checkRect = mSkillCheck.GetComponent<RectTransform>();
			if (checkRect != null)
			{
				checkRect.anchorMin = new Vector2(0.5f, 0.5f);
				checkRect.anchorMax = new Vector2(0.5f, 0.5f);
				checkRect.pivot = new Vector2(0.5f, 0.5f);
				checkRect.anchoredPosition = Vector2.zero;
			}

			if (mSkillCheckTexDes != null)
			{
				mSkillCheckTexDes.text = card.Description;
			}

			mSkillCheck.SetActive(true);
		}

		private void HideSkillCheck()
		{
			if (mSkillCheck != null)
			{
				mSkillCheck.SetActive(false);
			}
		}

		private void RefreshEnergy()
		{
			if (mEnergySlider != null && mEnergyText != null)
			{
				mEnergySlider.maxValue = mMaxEnergy;
				mEnergySlider.value = mCurrentEnergy;
				mEnergyText.text = $"{mCurrentEnergy}/{mMaxEnergy}";
			}
		}

		private void RefreshTurnOrder()
		{
			if (mTurnOrderText == null)
				return;

			if (string.IsNullOrEmpty(mCurrentActorId))
			{
				mTurnOrderText.text = "战斗准备中...";
				return;
			}

			string side = "";
			string name = "";

			switch (mCurrentActorType)
			{
				case BattleUnitType.Player:
					side = "我方";
					name = mCurrentActorId;
					mTurnOrderText.color = Color.blue;
					break;
				case BattleUnitType.Enemy:
					side = "敌方";
					name = mCurrentActorId;
					mTurnOrderText.color = Color.red;
					break;
				case BattleUnitType.CardDealer:
					side = "发牌";
					name = "发牌器";
					mTurnOrderText.color = new Color(0.9f, 0.5f, 1f);
					break;
				default:
					mTurnOrderText.text = "等待行动...";
					return;
			}

			mTurnOrderText.text = $"当前是{side}-{name}回合";
		}

		private void RefreshTurnOrderVisual()
		{
			if (mTurnOrderScrollView == null)
				return;

			Transform content = mTurnOrderScrollView.content;
			if (content == null)
				return;

			var units = mTurnOrderUnits;
			if (units == null) return;

			var aliveUnits = units.Where(u => u.IsAlive).ToList();
			string currentActorId = mCurrentActorId;

			// 生成未来行动点：每个单位从当前AV开始，每次行动后AV += BaseAV
			List<(ITurnOrder unit, float avValue, int cycle, int unitIndex)> allActionPoints = new List<(ITurnOrder, float, int, int)>();
			int maxSlots = 10;

			for (int unitIdx = 0; unitIdx < aliveUnits.Count; unitIdx++)
			{
				var unit = aliveUnits[unitIdx];
				float currentAV = unit.CurrentActionValue;
				float baseAV = unit.BaseActionValue;

				for (int cycle = 0; cycle < maxSlots; cycle++)
				{
					float av = currentAV + cycle * baseAV;
					if (av >= 0)
					{
						allActionPoints.Add((unit, av, cycle, unitIdx));
					}
				}
			}

			// 按AV升序排序，AV相同则按编队顺序
			allActionPoints.Sort((a, b) =>
			{
				int cmp = a.avValue.CompareTo(b.avValue);
				if (cmp != 0) return cmp;
				return a.unitIndex.CompareTo(b.unitIndex);
			});

			// 将当前行动者的cycle=0条目移到最左
			if (!string.IsNullOrEmpty(currentActorId))
			{
				int currentIdx = -1;
				for (int i = 0; i < allActionPoints.Count; i++)
				{
					var ap = allActionPoints[i];
					if (ap.cycle == 0 && ap.unit.UnitId == currentActorId)
					{
						currentIdx = i;
						break;
					}
				}
				if (currentIdx > 0)
				{
					var item = allActionPoints[currentIdx];
					allActionPoints.RemoveAt(currentIdx);
					allActionPoints.Insert(0, item);
				}
			}

			// 裁剪到maxSlots个
			if (allActionPoints.Count > maxSlots)
				allActionPoints = allActionPoints.Take(maxSlots).ToList();

			// 收集已创建的槽位（content的直接子物体上的Image）
			List<Image> slots = new List<Image>();
			List<Text> avTexts = new List<Text>();
			foreach (Transform child in content)
			{
				Image img = child.GetComponent<Image>();
				if (img != null)
				{
					slots.Add(img);
					Text[] texts = child.GetComponentsInChildren<Text>();
					avTexts.Add(texts.Length > 0 ? texts[0] : null);
				}
			}

			for (int i = 0; i < slots.Count; i++)
			{
				Image icon = slots[i];
				Text avText = i < avTexts.Count ? avTexts[i] : null;

				if (i < allActionPoints.Count)
				{
					var actionPoint = allActionPoints[i];
					var unit = actionPoint.unit;

					bool isCurrentActor = (i == 0)
						&& !string.IsNullOrEmpty(currentActorId)
						&& actionPoint.cycle == 0
						&& actionPoint.unit.UnitId == currentActorId;

					icon.gameObject.SetActive(true);

					if (unit.UnitType == BattleUnitType.Player)
					{
						SpriteIconLoader.SetCharacterIcon(icon, unit.UnitId);
						icon.color = icon.sprite != null ? Color.white : new Color(0.1f, 0.3f, 0.5f, 0.9f);
					}
					else if (unit.UnitType == BattleUnitType.Enemy)
					{
						SpriteIconLoader.SetEnemyIcon(icon, unit.UnitId);
						icon.color = icon.sprite != null ? Color.white : new Color(0.5f, 0.1f, 0.1f, 0.9f);
					}
					else
					{
						SpriteIconLoader.SetDealerIcon(icon);
						icon.color = icon.sprite != null ? Color.white : new Color(0.9f, 0.5f, 1f, 0.9f);
					}

					// 当前行动者金色描边高亮
					var outline = icon.GetComponent<Outline>();
					if (isCurrentActor)
					{
						if (outline == null)
							outline = icon.gameObject.AddComponent<Outline>();
						outline.effectColor = new Color(1f, 0.92f, 0.5f, 1f);
						outline.effectDistance = new Vector2(4f, 4f);
					}
					else
					{
						if (outline != null)
							Object.Destroy(outline);
					}

					if (avText != null)
					{
						avText.gameObject.SetActive(true);
						// 级联激活子物体（AVBg等）
						for (int c = 0; c < avText.transform.childCount; c++)
						{
							var child = avText.transform.GetChild(c);
							if (!child.gameObject.activeSelf)
								child.gameObject.SetActive(true);
						}

						if (avText.font == null && mDefaultFont != null)
							avText.font = mDefaultFont;

						int displayAV;
						if (isCurrentActor)
						{
							displayAV = 0;
						}
						else
						{
							int rounded = Mathf.RoundToInt(actionPoint.avValue);
							displayAV = (rounded <= 0) ? 1 : rounded;
						}
						avText.text = $"{displayAV}";
						avText.color = isCurrentActor ? Color.yellow : Color.white;
					}
				}
				else
				{
					icon.gameObject.SetActive(false);
					if (avText != null)
						avText.gameObject.SetActive(false);
				}
			}
		}

		private void UpdateNormalAttackButton()
		{
			if (mNormalAttackButton == null)
				return;

			bool canAct = mCurrentActorType == BattleUnitType.Player && !string.IsNullOrEmpty(mCurrentActorId);
			mNormalAttackButton.interactable = canAct;
		}

		private void UpdateShowStatsButton()
		{
			if (mShowStatsButton == null)
				return;

			// 查看状态按钮仅在我方回合可用
			mShowStatsButton.interactable = mCurrentActorType == BattleUnitType.Player;
		}

		private void UpdateManualDiscardButton()
		{
			if (mManualDiscardButton == null)
				return;

			bool canAct = mCurrentActorType == BattleUnitType.Player && !string.IsNullOrEmpty(mCurrentActorId);

			if (!canAct)
			{
				mManualDiscardButton.interactable = false;
				return;
			}

			mManualDiscardButton.interactable = mCurrentEnergy >= mManualDiscardCost;
		}

		private void RefreshWaveInfo()
		{
			if (mWaveText != null)
			{
				mWaveText.text = $"战斗中";
			}
		}

		private void OnCardClicked(SkillCardData card)
		{
			if (mCurrentActorType != BattleUnitType.Player)
				return;

			if (!card.SkillId.StartsWith(mCurrentActorId))
				return;

			int effCost = GetEffectiveCardCost(card);
			if (mCurrentEnergy < effCost)
			{
				LogKit.Log($"[BattlePanel] 能量不足：需要{effCost}（原{card.Cost}），当前{mCurrentEnergy}");
				return;
			}

			mSelectedCard = card;
			mIsNormalAttackMode = false;

			switch (card.TargetType)
			{
				case TargetType.Self:
					mIsSkillTargetSelectionMode = true;
					mCurrentTargetType = card.TargetType;
					if (mTurnOrderText != null)
					{
						mTurnOrderText.text = $"请选择技能拥有者";
						mTurnOrderText.color = Color.blue;
					}
					break;
				case TargetType.Ally:
				case TargetType.AllAllies:
					mIsSkillTargetSelectionMode = true;
					mCurrentTargetType = card.TargetType;
					if (mTurnOrderText != null)
					{
						mTurnOrderText.text = $"请选择友方目标";
						mTurnOrderText.color = Color.green;
					}
					break;
				case TargetType.Enemy:
				case TargetType.AllEnemies:
					mIsSkillTargetSelectionMode = true;
					mCurrentTargetType = card.TargetType;
					if (mTurnOrderText != null)
					{
						mTurnOrderText.text = $"请选择敌方目标";
						mTurnOrderText.color = Color.red;
					}
					break;
			}
		}

		private void OnNormalAttackClicked()
		{
			if (mCurrentActorType != BattleUnitType.Player)
				return;

			var unit = mPlayerUnits.FirstOrDefault(u => u.UnitId == mCurrentActorId);
			if (unit == null)
				return;

			mIsNormalAttackMode = true;
			mSelectedCard = null;

			if (mTurnOrderText != null)
			{
				mTurnOrderText.text = $"请选择普通攻击目标";
				mTurnOrderText.color = Color.yellow;
			}
		}

		private void OnManualDiscardClicked()
		{
			EventKit.Type.Send(new ManualDiscardRequestedEvent());
		}

		private void OnEnemyClicked(BattleUnitData enemy)
		{
			if (mIsNormalAttackMode)
			{
				var attacker = mPlayerUnits.FirstOrDefault(u => u.UnitId == mCurrentActorId);
				if (attacker != null)
				{
					EventKit.Type.Send(new NormalAttackRequestedEvent
					{
						Attacker = attacker,
						Target = enemy
					});
				}
				mIsNormalAttackMode = false;
				return;
			}

			if (mIsSkillTargetSelectionMode && 
				(mCurrentTargetType == TargetType.Enemy || mCurrentTargetType == TargetType.AllEnemies))
			{
				EventKit.Type.Send(new CardPlayRequestedEvent
				{
					Card = mSelectedCard,
					Target = enemy
				});
				mSelectedCard = null;
				mIsSkillTargetSelectionMode = false;
				return;
			}
		}

		private void OnPlayerClicked(BattleUnitData player)
		{
			if (mIsSkillTargetSelectionMode && 
				(mCurrentTargetType == TargetType.Ally || mCurrentTargetType == TargetType.AllAllies))
			{
				EventKit.Type.Send(new CardPlayRequestedEvent
				{
					Card = mSelectedCard,
					Target = player
				});
				mSelectedCard = null;
				mIsSkillTargetSelectionMode = false;
			}

			if (mIsSkillTargetSelectionMode && mCurrentTargetType == TargetType.Self)
			{
				if (mSelectedCard != null && mSelectedCard.SkillId.StartsWith(player.UnitId))
				{
					EventKit.Type.Send(new CardPlayRequestedEvent
					{
						Card = mSelectedCard,
						Target = player
					});
					mSelectedCard = null;
					mIsSkillTargetSelectionMode = false;
				}
			}
		}

		private void OnBattleUIData(BattleUIDataEvent evt)
		{
			mPlayerUnits = evt.PlayerUnits ?? mPlayerUnits;
			mEnemyUnits = evt.EnemyUnits ?? mEnemyUnits;
			mHandCards = evt.HandCards ?? mHandCards;
			mCurrentEnergy = evt.CurrentEnergy;
			mMaxEnergy = evt.MaxEnergy;
			mCurrentActorId = evt.CurrentActorId ?? "";
			mCurrentActorType = evt.CurrentActorType;
			mTurnOrderUnits = evt.TurnOrderUnits ?? mTurnOrderUnits;
			mManualDiscardCost = evt.ManualDiscardCost;
			RefreshUI();
		}

		private void OnBattleStateChange(BattleStateChangeEvent evt)
		{
			EventKit.Type.Send(new BattleUIRefreshRequestedEvent());
		}

		private void OnCardPlayed(CardPlayedEvent evt)
		{
			EventKit.Type.Send(new BattleUIRefreshRequestedEvent());
		}

		private void OnHandUpdated(HandUpdatedEvent evt)
		{
			if (evt.Cards != null)
				mHandCards = evt.Cards;
			RefreshHandCards();
		}

		private void OnEnergyChanged(EnergyChangedEvent evt)
		{
			mCurrentEnergy = evt.CurrentEnergy;
			mMaxEnergy = evt.MaxEnergy;
			RefreshEnergy();
			RefreshHandCards();
		}

		private void OnUnitDamaged(UnitDamagedEvent evt)
		{
			if (evt.Unit != null && !string.IsNullOrEmpty(evt.Unit.UnitId))
				StartCoroutine(PlayDamageAnimation(evt.Unit.UnitId));
			if (evt.Attacker != null && !string.IsNullOrEmpty(evt.Attacker.UnitId))
				StartCoroutine(PlayAttackAnimation(evt.Attacker.UnitId));
		}

		private void OnUnitHealed(UnitHealedEvent evt)
		{
			if (evt.Unit != null && !string.IsNullOrEmpty(evt.Unit.UnitId))
				StartCoroutine(PlayHealAnimation(evt.Unit.UnitId));
		}

		private void OnUnitHealthUpdated(UnitEvents.UnitHealthUpdatedEvent evt)
		{
			RefreshPlayerUnits();
			RefreshEnemyUnits();
		}

		private void OnUnitDefeated(UnitDefeatedEvent evt)
		{
			EventKit.Type.Send(new BattleUIRefreshRequestedEvent());
		}

		private void OnUnitSpawned(UnitSpawnedEvent evt)
		{
			EventKit.Type.Send(new BattleUIRefreshRequestedEvent());
		}

		private void OnEnemySpawn(EnemySpawnEvent evt)
		{
			EventKit.Type.Send(new BattleUIRefreshRequestedEvent());
		}

		private void OnWaveComplete(WaveCompleteEvent evt)
		{
			EventKit.Type.Send(new BattleUIRefreshRequestedEvent());
		}

		private void OnTurnOrderUpdated(TurnEvents.TurnOrderUpdatedEvent evt)
		{
			if (evt.Units != null)
				mTurnOrderUnits = evt.Units;
			RefreshTurnOrder();
			RefreshTurnOrderVisual();
			UpdateNormalAttackButton();
		}

		private void OnUnitActing(TurnEvents.UnitActingEvent evt)
		{
			mCurrentActorId = evt.UnitId ?? "";
			mCurrentActorType = evt.UnitType;
			RefreshUI();
		}

		private void OnActionComplete(TurnEvents.ActionCompleteEvent evt)
		{
			// 行动完成后清空当前行动者ID，等待下一个行动者被选定
			mCurrentActorId = "";
			// 刷新角色列表以清除当前行动高亮
			RefreshPlayerUnits();
			RefreshEnemyUnits();
			RefreshTurnOrder();
			RefreshTurnOrderVisual();
			UpdateNormalAttackButton();
			UpdateManualDiscardButton();
		}

		private void AddBattleLog(string message)
		{
			if (mBattleLogText == null) return;

			mBattleLogText.text = message;
		}

		private void OnUnitDamagedForLog(UnitEvents.UnitDamagedEvent evt)
		{
			if (evt.Attacker != null)
			{
				AddBattleLog($"{evt.Attacker.Name} 对 {evt.Unit.Name} 造成了 {evt.Damage} 点伤害");
			}
			else
			{
				AddBattleLog($"{evt.Unit.Name} 受到了 {evt.Damage} 点伤害");
			}
		}

		private void OnUnitHealedForLog(UnitEvents.UnitHealedEvent evt)
		{
			AddBattleLog($"{evt.Unit.Name} 恢复了 {evt.HealAmount} 点生命值");
		}

		private void OnShowStatsClicked()
		{
			UIKit.OpenPanel<UnitStatsPanel>(UILevel.Pop);
		}

		private void OnQuitButtonClicked()
		{
			if (mPanelQuit != null)
				mPanelQuit.SetActive(true);
		}

		private void OnSureQuitButtonClicked()
		{
			if (mPanelQuit != null)
				mPanelQuit.SetActive(false);

			EventKit.Type.Send(new BattleEndRequestedEvent());
			UIKit.ClosePanel<BattlePanel>();
			UIKit.OpenPanel<MainMenuPanel>(UILevel.AlwayBottom);
		}

		private void OnCancelQuitButtonClicked()
		{
			if (mPanelQuit != null)
				mPanelQuit.SetActive(false);
		}

		private IEnumerator PlayAttackAnimation(string unitId)
		{
			GameObject unitObj = FindUnitDisplayObject(unitId);
			if (unitObj == null) yield break;

			RectTransform rect = unitObj.GetComponent<RectTransform>();
			if (rect == null) yield break;

			Vector3 originalScale = rect.localScale;
			Vector3 targetScale = originalScale * 1.2f;

			float duration = 0.15f;
			float elapsed = 0f;

			while (elapsed < duration)
			{
				if (rect == null) yield break;
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				rect.localScale = Vector3.Lerp(originalScale, targetScale, t);
				yield return null;
			}

			elapsed = 0f;
			while (elapsed < duration)
			{
				if (rect == null) yield break;
				elapsed += Time.deltaTime;
				float t = Mathf.Clamp01(elapsed / duration);
				rect.localScale = Vector3.Lerp(targetScale, originalScale, t);
				yield return null;
			}

			if (rect != null)
				rect.localScale = originalScale;
		}

		private IEnumerator PlayDamageAnimation(string unitId)
		{
			GameObject unitObj = FindUnitDisplayObject(unitId);
			if (unitObj == null) yield break;

			Image bg = unitObj.GetComponent<Image>();
			if (bg == null)
			{
				bg = unitObj.GetComponentInChildren<Image>();
				if (bg == null) yield break;
			}

			Color originalColor = bg.color;
			Color flashColor = new Color(1f, 0.2f, 0.2f, originalColor.a);

			for (int i = 0; i < 2; i++)
			{
				if (bg == null) yield break;
				bg.color = flashColor;
				yield return new WaitForSeconds(0.1f);
				if (bg == null) yield break;
				bg.color = originalColor;
				yield return new WaitForSeconds(0.05f);
			}

			if (bg != null)
				bg.color = originalColor;
		}

		private IEnumerator PlayHealAnimation(string unitId)
		{
			GameObject unitObj = FindUnitDisplayObject(unitId);
			if (unitObj == null) yield break;

			Image bg = unitObj.GetComponent<Image>();
			if (bg == null)
			{
				bg = unitObj.GetComponentInChildren<Image>();
				if (bg == null) yield break;
			}

			Color originalColor = bg.color;
			Color flashColor = new Color(0.2f, 1f, 0.2f, originalColor.a);

			for (int i = 0; i < 2; i++)
			{
				if (bg == null) yield break;
				bg.color = flashColor;
				yield return new WaitForSeconds(0.1f);
				if (bg == null) yield break;
				bg.color = originalColor;
				yield return new WaitForSeconds(0.05f);
			}

			if (bg != null)
				bg.color = originalColor;
		}

		private GameObject FindUnitDisplayObject(string unitId)
		{
			if (mPlayerUnitsPanel != null)
			{
				foreach (Transform child in mPlayerUnitsPanel)
				{
					if (child.name == unitId)
						return child.gameObject;
				}
			}

			if (mEnemyUnitsPanel != null)
			{
				foreach (Transform child in mEnemyUnitsPanel)
				{
					if (child.name == unitId)
						return child.gameObject;
				}
			}

			return null;
		}

		private void OnBattleWin(BattleWinEvent evt)
		{
			UIKit.ClosePanel<BattlePanel>();
			var data = new StageClearPanelData
			{
				IsSuccess = true,
				TotalExp = evt.TotalExp,
				RewardItems = evt.RewardItems
			};
			UIKit.OpenPanel<StageClearPanel>(data: data);
		}

		private void OnBattleLose(BattleLoseEvent evt)
		{
			UIKit.ClosePanel<BattlePanel>();
			var data = new StageClearPanelData
			{
				IsSuccess = false
			};
			UIKit.OpenPanel<StageClearPanel>(data: data);
		}
	}
#endregion
}
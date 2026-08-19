using System.Collections.Generic;
using UnityEngine;
using TurnManagement;

namespace GameUI
{
	/// <summary>
	/// UI 层事件定义集合。
	/// </summary>
	public static class UIEvents
	{
		#region 装备与遗器

		/// <summary>装备武器事件。</summary>
		public class WeaponEquippedEvent
		{
			/// <summary>角色 ID。</summary>
			public string CharacterId { get; set; }
			/// <summary>武器 ID。</summary>
			public string WeaponId { get; set; }
		}

		/// <summary>装备遗器事件。</summary>
		public class RelicEquippedEvent
		{
			/// <summary>角色 ID。</summary>
			public string CharacterId { get; set; }
		}

		/// <summary>请求变更武器事件，统一处理装备、卸下和替换。</summary>
		public class WeaponChangeRequestedEvent
		{
			/// <summary>目标角色 ID。</summary>
			public string TargetCharacterId { get; set; }
			/// <summary>武器 ID。</summary>
			public string WeaponId { get; set; }
			/// <summary>武器实例 ID。</summary>
			public string WeaponInstanceId { get; set; }
		}

		/// <summary>武器变更结果事件。</summary>
		public class WeaponChangeResultEvent
		{
			/// <summary>目标角色 ID。</summary>
			public string TargetCharacterId { get; set; }
			/// <summary>武器 ID。</summary>
			public string WeaponId { get; set; }
			/// <summary>武器实例 ID。</summary>
			public string WeaponInstanceId { get; set; }
			/// <summary>是否变更成功。</summary>
			public bool Success { get; set; }
			/// <summary>变更类型：装备、卸下或替换。</summary>
			public string ChangeType { get; set; }
		}

		/// <summary>请求变更遗器事件，统一处理装备、卸下和替换。</summary>
		public class RelicChangeRequestedEvent
		{
			/// <summary>目标角色 ID。</summary>
			public string TargetCharacterId { get; set; }
			/// <summary>遗器 ID。</summary>
			public string RelicId { get; set; }
			/// <summary>遗器实例 ID。</summary>
			public string RelicInstanceId { get; set; }
			/// <summary>遗器槽位。</summary>
			public string RelicSlot { get; set; }
		}

		/// <summary>遗器变更结果事件。</summary>
		public class RelicChangeResultEvent
		{
			/// <summary>目标角色 ID。</summary>
			public string TargetCharacterId { get; set; }
			/// <summary>遗器 ID。</summary>
			public string RelicId { get; set; }
			/// <summary>遗器实例 ID。</summary>
			public string RelicInstanceId { get; set; }
			/// <summary>遗器槽位。</summary>
			public string RelicSlot { get; set; }
			/// <summary>是否变更成功。</summary>
			public bool Success { get; set; }
			/// <summary>变更类型：装备、卸下或替换。</summary>
			public string ChangeType { get; set; }
		}

		/// <summary>请求升级武器事件。</summary>
		public class WeaponUpgradeRequestedEvent
		{
			/// <summary>武器 ID。</summary>
			public string WeaponId { get; set; }
			/// <summary>武器实例 ID。</summary>
			public string WeaponInstanceId { get; set; }
			/// <summary>目标角色 ID。</summary>
			public string TargetCharacterId { get; set; }
		}

		/// <summary>武器升级结果事件。</summary>
		public class WeaponUpgradeResultEvent
		{
			/// <summary>武器 ID。</summary>
			public string WeaponId { get; set; }
			/// <summary>武器实例 ID。</summary>
			public string WeaponInstanceId { get; set; }
			/// <summary>目标角色 ID。</summary>
			public string TargetCharacterId { get; set; }
			/// <summary>是否升级成功。</summary>
			public bool Success { get; set; }
		}

		/// <summary>请求强化遗器事件。</summary>
		public class RelicEnhanceRequestedEvent
		{
			/// <summary>遗器 ID。</summary>
			public string RelicId { get; set; }
			/// <summary>遗器实例 ID。</summary>
			public string RelicInstanceId { get; set; }
			/// <summary>目标角色 ID。</summary>
			public string TargetCharacterId { get; set; }
		}

		/// <summary>遗器强化结果事件。</summary>
		public class RelicEnhanceResultEvent
		{
			/// <summary>遗器 ID。</summary>
			public string RelicId { get; set; }
			/// <summary>遗器实例 ID。</summary>
			public string RelicInstanceId { get; set; }
			/// <summary>目标角色 ID。</summary>
			public string TargetCharacterId { get; set; }
			/// <summary>是否强化成功。</summary>
			public bool Success { get; set; }
		}

		#endregion

		#region 战斗操作

		/// <summary>请求打开战斗面板事件。</summary>
		public class BattlePanelOpenedEvent { }

		/// <summary>请求关闭战斗面板事件。</summary>
		public class BattlePanelClosedEvent { }

		/// <summary>请求打出技能卡事件。</summary>
		public class CardPlayRequestedEvent
		{
			/// <summary>要打出的技能卡。</summary>
			public SkillCardData Card { get; set; }
			/// <summary>技能卡目标。</summary>
			public BattleUnitData Target { get; set; }
		}

		/// <summary>请求普通攻击事件。</summary>
		public class NormalAttackRequestedEvent
		{
			/// <summary>攻击者。</summary>
			public BattleUnitData Attacker { get; set; }
			/// <summary>攻击目标。</summary>
			public BattleUnitData Target { get; set; }
		}

		/// <summary>请求手动弃牌事件。</summary>
		public class ManualDiscardRequestedEvent { }

		/// <summary>技能目标选择事件。</summary>
		public class SkillTargetSelectedEvent
		{
			/// <summary>选中的目标。</summary>
			public BattleUnitData Target { get; set; }
			/// <summary>目标类型。</summary>
			public TargetType TargetType { get; set; }
		}

		/// <summary>请求结束战斗事件。</summary>
		public class BattleEndRequestedEvent { }

		/// <summary>战斗开始事件。</summary>
		public class BattleStartEvent
		{
			/// <summary>选中的角色 ID 列表。</summary>
			public List<string> SelectedCharacterIds { get; set; } = new List<string>();
			/// <summary>拥有的角色数据列表。</summary>
			public List<AccountItemData> OwnedCharacters { get; set; } = new List<AccountItemData>();
			/// <summary>副本 ID。</summary>
			public string DungeonId { get; set; }
		}

		/// <summary>请求刷新战斗 UI 事件。</summary>
		public class BattleUIRefreshRequestedEvent { }

		#endregion

		#region 战斗界面数据

		/// <summary>战斗 UI 数据事件。</summary>
		public class BattleUIDataEvent
		{
			/// <summary>玩家单位列表。</summary>
			public List<BattleUnitData> PlayerUnits { get; set; }
			/// <summary>敌方单位列表。</summary>
			public List<BattleUnitData> EnemyUnits { get; set; }
			/// <summary>手牌列表。</summary>
			public List<SkillCardData> HandCards { get; set; }
			/// <summary>当前能量。</summary>
			public int CurrentEnergy { get; set; }
			/// <summary>最大能量。</summary>
			public int MaxEnergy { get; set; }
			/// <summary>当前行动者 ID。</summary>
			public string CurrentActorId { get; set; }
			/// <summary>当前行动者类型。</summary>
			public BattleUnitType CurrentActorType { get; set; }
			/// <summary>行动顺序单位列表。</summary>
			public List<ITurnOrder> TurnOrderUnits { get; set; }
			/// <summary>手动弃牌消耗。</summary>
			public int ManualDiscardCost { get; set; }
		}

		#endregion

		#region 角色养成

		/// <summary>请求解锁星魂事件。</summary>
		public class StarUnlockRequestedEvent
		{
			/// <summary>角色 ID。</summary>
			public string CharacterId { get; set; }
			/// <summary>目标星魂等级。</summary>
			public int StarLevel { get; set; }
		}

		/// <summary>星魂解锁结果事件。</summary>
		public class StarUnlockResultEvent
		{
			/// <summary>角色 ID。</summary>
			public string CharacterId { get; set; }
			/// <summary>目标星魂等级。</summary>
			public int StarLevel { get; set; }
			/// <summary>是否解锁成功。</summary>
			public bool Success { get; set; }
		}

		/// <summary>请求升级天赋事件。</summary>
		public class TalentUpgradeRequestedEvent
		{
			/// <summary>角色 ID。</summary>
			public string CharacterId { get; set; }
			/// <summary>技能类型。</summary>
			public string SkillType { get; set; }
		}

		/// <summary>天赋升级结果事件。</summary>
		public class TalentUpgradeResultEvent
		{
			/// <summary>角色 ID。</summary>
			public string CharacterId { get; set; }
			/// <summary>技能类型。</summary>
			public string SkillType { get; set; }
			/// <summary>是否升级成功。</summary>
			public bool Success { get; set; }
			/// <summary>升级后的等级。</summary>
			public int NewLevel { get; set; }
		}

		/// <summary>角色等级、装备或养成数据更新后的通知事件。</summary>
		public class CharacterDataChangedEvent
		{
			/// <summary>发生变化的角色 ID。</summary>
			public string CharacterId { get; set; }
		}

		#endregion

		#region 战斗表现

		/// <summary>技能表现请求事件，请求播放技能视觉效果。</summary>
		public class SkillVisualRequestEvent
		{
			/// <summary>技能 ID。</summary>
			public string SkillId { get; set; }
			/// <summary>攻击单位 ID。</summary>
			public string AttackerUnitId { get; set; }
			/// <summary>目标单位 ID 列表。</summary>
			public List<string> TargetUnitIds { get; set; } = new List<string>();
			/// <summary>目标在单位列表中的索引，用于区分同名单位。</summary>
			public List<int> TargetUnitIndices { get; set; } = new List<int>();
		}

		/// <summary>技能表现命中事件，通知目标被命中并触发伤害结算。</summary>
		public class SkillVisualHitEvent
		{
			/// <summary>目标单位 ID。</summary>
			public string TargetUnitId { get; set; }
			/// <summary>目标在单位列表中的索引，-1 表示未提供索引。</summary>
			public int TargetUnitIndex { get; set; } = -1;
		}

		/// <summary>技能表现完成事件，表示所有视觉效果播放完毕。</summary>
		public class SkillVisualCompleteEvent { }

		#endregion
	}
}

#region 手牌管理

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YokiFrame;

namespace CardManagement
{
/// <summary>
/// 战斗模块中的 CardHand，负责对应的战斗流程处理。
/// </summary>
    public class CardHand : Singleton<CardHand>
    {

        private const int MAX_HAND_SIZE = 10;

        private List<SkillCardData> mCards = new List<SkillCardData>();
        private bool mIsInitialized = false;
        public delegate void OnExcessCardHandler(SkillCardData card);
        public event OnExcessCardHandler OnExcessCard;

        public List<SkillCardData> Cards => new List<SkillCardData>(mCards);
        public int Count => mCards.Count;
        public bool IsFull => mCards.Count >= MAX_HAND_SIZE;
        public int AvailableSlots => MAX_HAND_SIZE - mCards.Count;
        public bool IsInitialized => mIsInitialized;

    /// <summary>
    /// 执行 AddCard 相关的战斗处理。
    /// </summary>
        public void AddCard(SkillCardData card)
        {
            if (card == null) return;

            if (!IsFull)
            {
                mCards.Add(card);
                NotifyHandUpdated();
            }
            else
            {
                OnExcessCard?.Invoke(card);
            }
        }

    /// <summary>
    /// 执行 AddCards 相关的战斗处理。
    /// </summary>
        public void AddCards(List<SkillCardData> cards)
        {
            foreach (var card in cards)
            {
                if (card == null) continue;

                if (!IsFull)
                {
                    mCards.Add(card);
                }
                else
                {
                    OnExcessCard?.Invoke(card);
                }
            }
            NotifyHandUpdated();
        }

    /// <summary>
    /// 执行 RemoveCard 相关的战斗处理。
    /// </summary>
        public void RemoveCard(SkillCardData card)
        {
            if (mCards.Remove(card))
            {
                NotifyHandUpdated();
            }
        }

    /// <summary>
    /// 执行 Clear 相关的战斗处理。
    /// </summary>
        public void Clear()
        {
            mCards.Clear();
            mIsInitialized = true;
            NotifyHandUpdated();
        }

    /// <summary>
    /// 执行 ClearExcessCardHandler 相关的战斗处理。
    /// </summary>
        public void ClearExcessCardHandler()
        {
            OnExcessCard = null;
        }

        private void NotifyHandUpdated()
        {
            EventKit.Type.Send(new CardEvents.HandUpdatedEvent
            {
                Cards = new List<SkillCardData>(mCards)
            });
        }
    }
}

#endregion

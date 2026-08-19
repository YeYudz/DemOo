#region 卡组管理

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using YokiFrame;
using System.Linq;

namespace CardManagement
{
/// <summary>
/// 战斗模块中的 CardDeck，负责对应的战斗流程处理。
/// </summary>
    public class CardDeck : Singleton<CardDeck>
    {

        private List<SkillCardData> mCards = new List<SkillCardData>();
        private List<SkillCardData> mDiscardPile = new List<SkillCardData>();
        private HashSet<string> mAliveCharacterIds = new HashSet<string>();
        private bool mIsInitialized = false;

        public int Count => mCards.Count;
        public int DiscardCount => mDiscardPile.Count;
        public bool IsInitialized => mIsInitialized;

    /// <summary>
    /// 执行 Initialize 相关的战斗处理。
    /// </summary>
        public void Initialize(List<SkillCardData> cards)
        {
            mCards = new List<SkillCardData>(cards);
            mDiscardPile.Clear();
            mAliveCharacterIds.Clear();
            Shuffle();
        }

    /// <summary>
    /// 执行 SetAliveCharacters 相关的战斗处理。
    /// </summary>
        public void SetAliveCharacters(List<string> aliveCharacterIds)
        {
            mAliveCharacterIds.Clear();
            if (aliveCharacterIds != null)
            {
                foreach (var id in aliveCharacterIds)
                {
                    mAliveCharacterIds.Add(id);
                }
            }
        }

    /// <summary>
    /// 执行 Shuffle 相关的战斗处理。
    /// </summary>
        public void Shuffle()
        {
            for (int i = mCards.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var temp = mCards[i];
                mCards[i] = mCards[j];
                mCards[j] = temp;
            }
        }

    /// <summary>
    /// 执行 Draw 相关的战斗处理。
    /// </summary>
        public SkillCardData Draw()
        {
            if (mCards.Count == 0)
            {
                Reshuffle();
                if (mCards.Count == 0)
                    return null;
            }

            var card = mCards[0];
            mCards.RemoveAt(0);

            return card;
        }

    /// <summary>
    /// 执行 Draw 相关的战斗处理。
    /// </summary>
        public List<SkillCardData> Draw(int count)
        {
            List<SkillCardData> drawn = new List<SkillCardData>();
            for (int i = 0; i < count; i++)
            {
                var card = Draw();
                if (card != null)
                    drawn.Add(card);
            }
            return drawn;
        }

    /// <summary>
    /// 执行 Discard 相关的战斗处理。
    /// </summary>
        public void Discard(SkillCardData card)
        {
            mDiscardPile.Add(card);
        }

    /// <summary>
    /// 执行 Discard 相关的战斗处理。
    /// </summary>
        public void Discard(List<SkillCardData> cards)
        {
            mDiscardPile.AddRange(cards);
        }

        private void Reshuffle()
        {
            List<SkillCardData> validCards = new List<SkillCardData>();
            
            foreach (var card in mDiscardPile)
            {
                if (mAliveCharacterIds.Count == 0 || 
                    mAliveCharacterIds.Contains(card.CharacterId))
                {
                    validCards.Add(card);
                }
            }
            
            mCards.AddRange(validCards);
            mDiscardPile.Clear();
            Shuffle();
        }

    /// <summary>
    /// 执行 RemoveDeadCharacterCards 相关的战斗处理。
    /// </summary>
        public void RemoveDeadCharacterCards(string characterId)
        {
            var deadCards = mCards.Where(c => c.CharacterId == characterId).ToList();
            foreach (var card in deadCards)
            {
                mCards.Remove(card);
                mDiscardPile.Add(card);
            }
            LogKit.Log($"[CardDeck] 移除死亡角色 {characterId} 的 {deadCards.Count} 张卡牌");
        }

    /// <summary>
    /// 执行 Clear 相关的战斗处理。
    /// </summary>
        public void Clear()
        {
            mCards.Clear();
            mDiscardPile.Clear();
            mAliveCharacterIds.Clear();
            mIsInitialized = true;
        }
    }
}

#endregion

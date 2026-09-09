using System.Collections.Generic;
using UnityEngine;
using TMPro;

public partial class ViewController
{
    // 增量刷新本方手牌。删除工作由所有区域处理完成后的统一清理负责。
    public void RefreshLocalHandView(
        CardNetworkState[] cards,
        CardNetworkState pendingAttack,
        CardNetworkState pendingTarget,
        PendingChoice pendingChoice)
    {
        Transform handContainer = LocalPlayer.Find("Hand");

        for(int i = 0; i < cards.Length; i++)
        {
            CardNetworkState cardState = cards[i];
            CardView cardView = GetOrCreateCardView(cardState, handContainer);

            // 本方手牌始终允许查看预览，并在悬浮时升起。
            cardView.SetPointerActions(
                CardPreviewController.Show,
                CardPreviewController.Hide,
                true);

            if(currentPhase == GamePhase.WaitingForMainAction &&
                isLocalPlayerExpected)
            {
                cardView.SetClickAction(PlayCardDecision);
            }

            cardView.Highlight.SetActive(
                pendingAttack.CardInstanceID == cardState.CardInstanceID);
            cardView.Aimed.SetActive(
                pendingTarget.CardInstanceID == cardState.CardInstanceID);

            bool isCandidate = pendingChoice != null &&
                pendingChoice.CandidateCardInstanceIDs.Contains(
                    cardState.CardInstanceID);
            cardView.Candidate.SetActive(isCandidate);

            // 待选择操作的优先级高于普通出牌操作。
            if(isCandidate)
            {
                cardView.SetClickAction(ChooseDecision);
            }

            // 服务器数组顺序同时决定手牌布局顺序和UI遮挡顺序。
            cardView.transform.SetSiblingIndex(i);
        }
    }

    // 增量刷新场地。双方场地都属于公开区域，因此共用统一CardView字典。
    public void RefreshFieldView(CardNetworkState[] cards,
        Transform playerTransform,
        CardNetworkState pendingAttack, 
        CardNetworkState pendingTarget,
        PendingChoice pendingChoice)
    {
        Transform fieldContainer = playerTransform.Find("Field");
        bool isLocalField = playerTransform == LocalPlayer;

        for(int i = 0; i < cards.Length; i++)
        {
            CardNetworkState cardState = cards[i];
            CardView cardView = GetOrCreateCardView(cardState, fieldContainer);

            // 双方场地都是明牌，可以查看预览，但不需要像手牌一样升起。
            cardView.SetPointerActions(
                CardPreviewController.Show,
                CardPreviewController.Hide,
                false);

            if(isLocalField &&
                currentPhase == GamePhase.WaitingForMainAction &&
                isLocalPlayerExpected)
            {
                cardView.SetClickAction(AttackDecision);
            }
            //如果是本方场上卡牌，且当前是本方阻挡决策阶段，则绑定阻挡事件
            if(isLocalField &&
                currentPhase == GamePhase.WaitingForBlockDecision &&
                isLocalPlayerExpected)
            {
                cardView.SetClickAction(BlockDecision);
            }

            if(isLocalField &&
                currentPhase == GamePhase.WaitingForFrenzyAttack &&
                isLocalPlayerExpected)
            {
                //只给上次攻击的卡牌绑定攻击事件
                if(pendingAttack.CardInstanceID == cardState.CardInstanceID)
                {
                    cardView.SetClickAction(AttackDecision);
                }
            }

            cardView.Highlight.SetActive(
                pendingAttack.CardInstanceID == cardState.CardInstanceID);
            cardView.Aimed.SetActive(
                pendingTarget.CardInstanceID == cardState.CardInstanceID);

            // 高亮显示当前待选择的卡牌
            bool isCandidate = pendingChoice != null &&
                pendingChoice.CandidateCardInstanceIDs.Contains(
                    cardState.CardInstanceID);
            cardView.Candidate.SetActive(isCandidate);
            if(isCandidate)
            {
                cardView.SetClickAction(ChooseDecision);
            }

            cardView.transform.SetSiblingIndex(i);
        }
    }

    // 刷新对手手牌视图，显示为背面,并且数量与对手手牌数量一致
    public void RefreshOpponentHandView(int opponentHandCount, Transform opponentTransform)
    {
        Transform handContainer = opponentTransform.Find("Hand");
        // 先移出容器再销毁，使随后生成的新手牌可以立刻按正确数量居中。
        for(int i = handContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = handContainer.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        // 创建新的手牌视图
        for(int i = 0; i < opponentHandCount; i++)
        {
            GameObject cardViewObj = Instantiate(CardViewPrefab, handContainer);
            CardView cardView = cardViewObj.GetComponent<CardView>();
            // 对手手牌只显示卡背，不向该客户端填入任何真实卡牌数据。
            cardView.SetCardBack(true);
        }

        handContainer.GetComponent<HandCardLayout>().RefreshLayout();
    }

    public void RefreshPendingCardsView(CardNetworkState pendingCard)
    {
        if(pendingCard.CardInstanceID == -1)
        {
            return;
        }

        CardView cardView = GetOrCreateCardView(
            pendingCard,
            PendingCardsContainer);
        cardView.SetPointerActions(null, null, false);
        cardView.transform.localPosition = new Vector3(0, 0, 0); // 调整卡牌位置
        cardView.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); // 确保卡牌缩放为0.5
    }

    public void RefreshPlayerPortrait(bool isLocalPlayer, int life, int mindbugCount,
        bool isPlayerExpected)
    {
        Transform portraitTransform = 
            isLocalPlayer ? LocalPlayer.Find("Portrait") : OpponentPlayer.Find("Portrait");
        TMP_Text lifeText = portraitTransform.Find("LifeText").GetComponent<TMP_Text>();
        lifeText.text = life.ToString();
        TMP_Text mindbugText = portraitTransform.Find("MindbugCount").GetComponent<TMP_Text>();
        mindbugText.text = mindbugCount.ToString();
        GameObject expectedText = portraitTransform.Find("Highlight").gameObject;
        expectedText.SetActive(isPlayerExpected);
    }

    public void RefreshDiscardCount(bool isLocalPlayer, int discardCount)
    {
        Transform portraitTransform = 
            isLocalPlayer ? LocalPlayer.Find("Discard") : OpponentPlayer.Find("Discard");
        TMP_Text discardText = portraitTransform.Find("DiscardCount").GetComponent<TMP_Text>();
        discardText.text = discardCount.ToString();
    }

    public void RefreshDiscardPilePannel(bool isLocalPlayer,
        CardNetworkState[] DiscardPile,
        PendingChoice pendingChoice)
    {
        Transform startPoint = isLocalPlayer ? DiscardPilePannel.Find("local") : DiscardPilePannel.Find("opponent");

        for(int i = 0; i < DiscardPile.Length; i++)
        {
            CardNetworkState cardState = DiscardPile[i];
            CardView cardView = GetOrCreateCardView(cardState, startPoint);
            // 弃牌区是公开信息，可以查看预览，但不需要像手牌一样升起。
            cardView.SetPointerActions(
                CardPreviewController.Show,
                CardPreviewController.Hide,
                false);

            bool isCandidate = pendingChoice != null &&
                pendingChoice.CandidateCardInstanceIDs.Contains(
                    cardState.CardInstanceID);
            cardView.Candidate.SetActive(isCandidate);
            if(isCandidate)
            {
                cardView.SetClickAction(ChooseDecision);
            }

            cardView.transform.SetSiblingIndex(i);
        }
    }

    // 取得某个实例唯一对应的CardView，并在卡牌跨区域时直接移动原对象。
    private CardView GetOrCreateCardView(
        CardNetworkState cardState,
        Transform targetContainer)
    {
        visibleCardInstanceIDs.Add(cardState.CardInstanceID);

        if(!cardViews.TryGetValue(cardState.CardInstanceID, out CardView cardView))
        {
            GameObject cardViewObject = Instantiate(CardViewPrefab, targetContainer);
            cardView = cardViewObject.GetComponent<CardView>();
            cardViews.Add(cardState.CardInstanceID, cardView);
        }
        else if(cardView.transform.parent != targetContainer)
        {
            // 离开原区域时结束旧的悬浮状态，并关闭仍然引用该卡牌的预览。
            CardPreviewController.Hide(cardView);
            cardView.ResetPointerState();
            cardView.transform.SetParent(targetContainer, false);
        }

        CardData cardData = GetCardDataByID(cardState.CardDataID);
        cardView.UpdateCardView(
            cardData.CardName,
            cardData.Description,
            cardState.currentPower,
            cardState.CardInstanceID,
            cardState.keywords,
            cardState.isExhausted);

        // 同一个CardView会被不同区域复用，每次先清除旧区域留下的交互和标记。
        cardView.SetClickAction(null);
        cardView.SetSelected(false);
        cardView.Highlight.SetActive(false);
        cardView.Aimed.SetActive(false);
        cardView.Candidate.SetActive(false);

        return cardView;
    }

    // 只有在所有区域都没有出现的实例才真正离开当前客户端的可见状态。
    private void RemoveInvisibleCardViews()
    {
        foreach(int cardInstanceID in cardViews.Keys)
        {
            if(!visibleCardInstanceIDs.Contains(cardInstanceID))
            {
                removedCardInstanceIDs.Add(cardInstanceID);
            }
        }

        foreach(int cardInstanceID in removedCardInstanceIDs)
        {
            CardView cardView = cardViews[cardInstanceID];
            CardPreviewController.Hide(cardView);
            cardView.ResetPointerState();

            // 立即停用并移出布局容器，Destroy会在当前帧结束时真正执行。
            cardView.gameObject.SetActive(false);
            cardView.transform.SetParent(transform, false);
            Destroy(cardView.gameObject);
            cardViews.Remove(cardInstanceID);
        }
    }
    public void RefreshDeckCount(bool isLocalPlayer, int deckCount)
    {
        Transform portraitTransform = 
            isLocalPlayer ? LocalPlayer.Find("Deck") : OpponentPlayer.Find("Deck");
        TMP_Text deckText = portraitTransform.Find("DeckCount").GetComponent<TMP_Text>();
        deckText.text = deckCount.ToString();
    }

    public void RefreshWinnerView(int winnerPlayerID,int localPlayerID)
    {

        if(winnerPlayerID == -1)
        {
            WinnerContainer.Find("Win").gameObject.SetActive(false);
            WinnerContainer.Find("Lose").gameObject.SetActive(false);
            return;
        }
        if(winnerPlayerID == localPlayerID)
        {
            WinnerContainer.Find("Win").gameObject.SetActive(true);
            WinnerContainer.Find("Lose").gameObject.SetActive(false);

        }
        else
        {
            WinnerContainer.Find("Win").gameObject.SetActive(false);
            WinnerContainer.Find("Lose").gameObject.SetActive(true);
        }
    }
    public CardData GetCardDataByID(int cardDataID)
    {
        // 这里你需要实现根据cardDataID从你的卡牌数据库中获取CardData的逻辑
        // 例如，你可以有一个CardDatabase类来管理所有的CardData
        CardData cardData = gameController.CardDatabase.Find(
                c => c.CardDataID == cardDataID);
        return cardData;
    }
}

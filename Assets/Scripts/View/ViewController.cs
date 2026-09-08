using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class ViewController : MonoBehaviour
{
    public GameObject CardViewPrefab;
    public Transform LocalPlayer;
    public Transform OpponentPlayer;
    public Transform PendingCardsContainer;
    public Transform WinnerContainer;
    public Transform DiscardPilePannel;
    public CardPreviewController CardPreviewController;

    public Button NoBlockButton;
    public Button NoMindbugButton;
    public Button UseMindbugButton;
    public Button AttackButton;
    public Button BlockButton;
    public Button NoFrenzyAttackButton;
    public Button NextGameButton;
    public Button ChooseButton;
    public Button PlayButton;

    public GameController gameController;
    public NetworkController networkController;

    private GamePhase currentPhase;
    private bool isLocalPlayerExpected;
    private CardView selectedCard;
    private List<CardView> choosedCards = new List<CardView>();

    // 本方手牌使用实例ID保存对应的显示对象，使状态刷新时可以复用已有CardView。
    private Dictionary<int, CardView> localHandCardViews =
        new Dictionary<int, CardView>();

    private PendingChoice pendingChoice;

    // Start is called before the first frame update
    void Start()
    {
        NoBlockButton.onClick.AddListener(OnNoBlockButtonClicked);
        AttackButton.onClick.AddListener(OnAttackButtonClicked);
        BlockButton.onClick.AddListener(OnBlockButtonClicked);
        NoFrenzyAttackButton.onClick.AddListener(OnNoFrenzyAttackButtonClicked);
        ChooseButton.onClick.AddListener(OnChooseButtonClicked);
        PlayButton.onClick.AddListener(OnPlayButtonClicked);

        DiscardPilePannel.Find("CloseButton").GetComponent<Button>().
            onClick.AddListener(() => OnDiscardPileClicked(true));
        LocalPlayer.Find("Discard").GetComponent<DiscardPileView>().IsLocalPlayer = true;
        LocalPlayer.Find("Discard").GetComponent<DiscardPileView>().
            SetClickAction(OnDiscardPileClicked);
        OpponentPlayer.Find("Discard").GetComponent<DiscardPileView>().IsLocalPlayer = false;
        OpponentPlayer.Find("Discard").GetComponent<DiscardPileView>().
            SetClickAction(OnDiscardPileClicked);
    }
    public void RefreshView(
        int gamePhase,
        int winnerPlayerID,
        int localPlayerID,
        int ActivePlayerID,
        int ExpectedPlayerID,
        int localPlayerLife,
        int opponentPlayerLife,
        int localPlayerMindbugCount,
        int opponentPlayerMindbugCount,
        int localPlayerDeckCount,
        int opponentPlayerDeckCount,
        CardNetworkState[] localPlayerDiscard,
        CardNetworkState[] opponentPlayerDiscard,
        CardNetworkState[] localPlayerHand,
        CardNetworkState[] localPlayerField,
        CardNetworkState[] opponentPlayerField,
        int opponentHandCount,
        CardNetworkState pendingCard,
        CardNetworkState pendingAttack,
        CardNetworkState pendingTarget,
        bool hasPendingChoice,
        int maxSelectCount,
        int minSelectCount,
        int[] candidateCardInstanceIDs
        )
    {
        // 刷新会销毁并重新生成卡牌，因此先关闭仍引用旧CardView的预览。
        CardPreviewController.Hide();

        currentPhase = (GamePhase)gamePhase;
        isLocalPlayerExpected = (localPlayerID == ExpectedPlayerID);

        selectedCard = null;
        choosedCards.Clear();
        PlayButton.gameObject.SetActive(false);
        AttackButton.gameObject.SetActive(false);
        if(hasPendingChoice)
        {
            pendingChoice = new PendingChoice
            {
                PlayerID = localPlayerID,
                MaxSelectCount = maxSelectCount,
                MinSelectCount = minSelectCount,
                CandidateCardInstanceIDs = new List<int>(candidateCardInstanceIDs)
            };
        }
        else
        {
            pendingChoice = null;
        }
        
        RefreshPlayerPortrait(true, localPlayerLife, 
            localPlayerMindbugCount, isLocalPlayerExpected);
        RefreshPlayerPortrait(false, opponentPlayerLife, opponentPlayerMindbugCount,
            !isLocalPlayerExpected);
        RefreshLocalHandView(
            localPlayerHand,
            pendingAttack,
            pendingTarget,
            pendingChoice);
        RefreshHandOrFieldView(localPlayerField, LocalPlayer, "Field", pendingAttack, pendingTarget,pendingChoice);
        RefreshHandOrFieldView(opponentPlayerField, OpponentPlayer, "Field", pendingAttack, pendingTarget,pendingChoice);
        RefreshOpponentHandView(opponentHandCount, OpponentPlayer);
        RefreshPendingCardsView(pendingCard);

        RefreshDeckCount(true, localPlayerDeckCount);
        RefreshDeckCount(false, opponentPlayerDeckCount);

        RefreshDiscardCount(true, localPlayerDiscard.Length);
        RefreshDiscardPilePannel(true, localPlayerDiscard,pendingChoice);

        RefreshDiscardCount(false, opponentPlayerDiscard.Length);
        RefreshDiscardPilePannel(false, opponentPlayerDiscard,pendingChoice);

        RefreshButtons(localPlayerMindbugCount,pendingTarget);
        RefreshWinnerView(winnerPlayerID, localPlayerID);

        //等待从弃牌区选择卡牌时，自动打开对应玩家的弃牌区界面
        if(isLocalPlayerExpected &&
            currentPhase == GamePhase.WaitingForChoice)
        {
            if(ContainsChoiceCandidate(localPlayerDiscard))
            {
                OpenDiscardPilePannel(true);
                ChooseButton.gameObject.SetActive(false);
            }
            else if(ContainsChoiceCandidate(opponentPlayerDiscard))
            {
                OpenDiscardPilePannel(false);
                ChooseButton.gameObject.SetActive(false);
            }
        }
        
    }

    // 增量刷新本方手牌：保留仍在手牌中的CardView，只创建或删除发生变化的卡牌。
    public void RefreshLocalHandView(
        CardNetworkState[] cards,
        CardNetworkState pendingAttack,
        CardNetworkState pendingTarget,
        PendingChoice pendingChoice)
    {
        Transform handContainer = LocalPlayer.Find("Hand");
        HashSet<int> currentCardIDs = new HashSet<int>();
        List<int> removedCardIDs = new List<int>();

        // 先记录服务器状态中当前仍然存在的手牌ID。
        foreach(CardNetworkState card in cards)
        {
            currentCardIDs.Add(card.CardInstanceID);
        }

        // 遍历字典期间不能直接删除内容，因此先单独记录已经离开手牌的ID。
        foreach(int cardInstanceID in localHandCardViews.Keys)
        {
            if(!currentCardIDs.Contains(cardInstanceID))
            {
                removedCardIDs.Add(cardInstanceID);
            }
        }

        foreach(int cardInstanceID in removedCardIDs)
        {
            CardView cardView = localHandCardViews[cardInstanceID];
            CardPreviewController.Hide(cardView);

            // 立即停用并移出Hand，使本帧随后的布局不会统计到待销毁卡牌。
            // 以后加入出牌、弃牌动画时，可在这里改为交给动画系统接管。
            cardView.gameObject.SetActive(false);
            cardView.transform.SetParent(transform, false);
            Destroy(cardView.gameObject);
            localHandCardViews.Remove(cardInstanceID);
        }

        for(int i = 0; i < cards.Length; i++)
        {
            CardNetworkState cardState = cards[i];

            if(!localHandCardViews.TryGetValue(
                cardState.CardInstanceID,
                out CardView cardView))
            {
                GameObject cardViewObject =
                    Instantiate(CardViewPrefab, handContainer);
                cardView = cardViewObject.GetComponent<CardView>();

                localHandCardViews.Add(
                    cardState.CardInstanceID,
                    cardView);

                // 本方手牌始终允许查看预览，并在悬浮时升起。
                cardView.SetPointerActions(
                    CardPreviewController.Show,
                    CardPreviewController.Hide,
                    true);
            }

            CardData cardData = GetCardDataByID(cardState.CardDataID);
            cardView.UpdateCardView(
                cardData.CardName,
                cardData.Description,
                cardState.currentPower,
                cardState.CardInstanceID,
                cardState.keywords,
                cardState.isExhausted);

            // CardView会被跨阶段复用，必须先清除上一次刷新留下的交互和选中状态。
            cardView.SetClickAction(null);
            cardView.SetSelected(false);

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

        handContainer.GetComponent<HandCardLayout>().RefreshLayout();
    }

    public void RefreshHandOrFieldView(CardNetworkState[] cards,
        Transform playerTransform,
        string handOrField, 
        CardNetworkState pendingAttack, 
        CardNetworkState pendingTarget,
        PendingChoice pendingChoice)
    {
        Transform handContainer = playerTransform.Find(handOrField);
        // Destroy会到当前帧结束时才真正删除对象。
        // 先把旧卡牌移出容器，避免本帧计算布局时把旧卡牌也统计进去。
        for(int i = handContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = handContainer.GetChild(i);
            child.SetParent(null);
            Destroy(child.gameObject);
        }
        // 创建新的手牌视图
        for(int i = 0; i < cards.Length; i++)
        {
            GameObject cardViewObj = Instantiate(CardViewPrefab, handContainer);
            CardView cardView = cardViewObj.GetComponent<CardView>();
            // 创建CardInstance对象
            CardData cardData = GetCardDataByID(cards[i].CardDataID);
            cardView.UpdateCardView(cardData.CardName, 
                cardData.Description, 
                cards[i].currentPower, 
                cards[i].CardInstanceID,
                cards[i].keywords,
                cards[i].isExhausted);

            bool isLocalHand = (playerTransform == LocalPlayer && handOrField == "Hand");
            bool isLocalField = (playerTransform == LocalPlayer && handOrField == "Field");

            // 本方手牌和双方场上的明牌可以查看预览；只有本方手牌会在悬浮时升起。
            bool canShowPreview = isLocalHand || handOrField == "Field";
            if(canShowPreview)
            {
                cardView.SetPointerActions(
                    CardPreviewController.Show,
                    CardPreviewController.Hide,
                    isLocalHand);
            }

            //如果是本方手牌，且当前是本方主动回合，则绑定出牌事件
            if(isLocalHand &&
                currentPhase == GamePhase.WaitingForMainAction &&
                isLocalPlayerExpected)
            {
                cardView.SetClickAction(PlayCardDecision);
            }
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
                if(pendingAttack.CardInstanceID == cards[i].CardInstanceID)
                {
                    cardView.SetClickAction(AttackDecision);
                }
            }
            // 高亮显示当前待攻击决策的卡牌
            if(pendingAttack.CardInstanceID == cards[i].CardInstanceID)
            {
                cardView.Highlight.SetActive(true);
            }
            else
            {
                cardView.Highlight.SetActive(false);
            }

            // 高亮显示当前被选中的卡牌
            if(pendingTarget.CardInstanceID == cards[i].CardInstanceID)
            {
                cardView.Aimed.SetActive(true);
            }
            else
            {
                cardView.Aimed.SetActive(false);
            }
            // 高亮显示当前待选择的卡牌
            if(pendingChoice != null && pendingChoice.CandidateCardInstanceIDs.Contains(cards[i].CardInstanceID))
            {
                cardView.Candidate.SetActive(true);
                cardView.SetClickAction(ChooseDecision);
            }
            else
            {
                cardView.Candidate.SetActive(false);
            }

        }

        if(handOrField == "Hand")
        {
            handContainer.GetComponent<HandCardLayout>().RefreshLayout();
        }
        else
        {
            handContainer.GetComponent<FieldCardLayout>().RefreshLayout();
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
        // 清空现有待决策卡牌视图
        foreach (Transform child in PendingCardsContainer)
        {
            Destroy(child.gameObject);
        }
        if(pendingCard.CardInstanceID == -1)
        {
            
            return;
        }
        GameObject cardViewObj = Instantiate(CardViewPrefab, PendingCardsContainer);
        CardView cardView = cardViewObj.GetComponent<CardView>();
        cardView.UpdateCardView(GetCardDataByID(pendingCard.CardDataID).CardName,
            GetCardDataByID(pendingCard.CardDataID).Description,
            pendingCard.currentPower, 
            pendingCard.CardInstanceID, 
            pendingCard.keywords, 
            pendingCard.isExhausted);
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
        // 清空现有弃牌堆视图
        foreach (Transform child in startPoint)
        {
            Destroy(child.gameObject);
        }
        // 创建新的弃牌堆视图
        for(int i = 0; i < DiscardPile.Length; i++)
        {
            GameObject cardViewObj = Instantiate(CardViewPrefab, startPoint);
            CardView cardView = cardViewObj.GetComponent<CardView>();
            cardView.UpdateCardView(GetCardDataByID(DiscardPile[i].CardDataID).CardName,
                GetCardDataByID(DiscardPile[i].CardDataID).Description,
                DiscardPile[i].currentPower, 
                DiscardPile[i].CardInstanceID, 
                DiscardPile[i].keywords, 
                DiscardPile[i].isExhausted);
            cardView.transform.localPosition = new Vector3(i * 120, 0, 0); // 调整卡牌位置
            cardView.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f); // 确保卡牌缩放为0.4
            if(pendingChoice != null && pendingChoice.CandidateCardInstanceIDs.Contains(DiscardPile[i].CardInstanceID))
            {
                cardView.Candidate.SetActive(true);
                cardView.SetClickAction(ChooseDecision);
            }
            else
            {
                cardView.Candidate.SetActive(false);
            }
            
        }
        
    }
    public void RefreshDeckCount(bool isLocalPlayer, int deckCount)
    {
        Transform portraitTransform = 
            isLocalPlayer ? LocalPlayer.Find("Deck") : OpponentPlayer.Find("Deck");
        TMP_Text deckText = portraitTransform.Find("DeckCount").GetComponent<TMP_Text>();
        deckText.text = deckCount.ToString();
    }

    public void RefreshButtons(int localPlayerMindbugCount,CardNetworkState pendingTarget)
    {
        UseMindbugButton.gameObject.SetActive(false);
        NoMindbugButton.gameObject.SetActive(false);
        NoBlockButton.gameObject.SetActive(false);
        NoFrenzyAttackButton.gameObject.SetActive(false);
        NextGameButton.gameObject.SetActive(false);
        ChooseButton.gameObject.SetActive(false);

        if(currentPhase == GamePhase.WaitingForMindbugDecision && isLocalPlayerExpected)
        {
            if(localPlayerMindbugCount > 0)
            {
                UseMindbugButton.gameObject.SetActive(true);
            }
            else
            {
                UseMindbugButton.gameObject.SetActive(false);
            }
            NoMindbugButton.gameObject.SetActive(true);
        }
        else if(currentPhase == GamePhase.WaitingForBlockDecision && isLocalPlayerExpected)
        {
            if(pendingTarget.CardInstanceID != -1)//狩猎目标如果存在
            {
                NoBlockButton.gameObject.SetActive(false);
            }
            else
            {
                NoBlockButton.gameObject.SetActive(true);
            }
        }
        else if(currentPhase == GamePhase.WaitingForFrenzyAttack && isLocalPlayerExpected)
        {
            NoFrenzyAttackButton.gameObject.SetActive(true);
        }
        else if(currentPhase == GamePhase.GameOver)
        {
            NextGameButton.gameObject.SetActive(true);
        }
        else if(currentPhase == GamePhase.WaitingForChoice && isLocalPlayerExpected)
        {
            ChooseButton.gameObject.SetActive(true);
        }
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
    public void PlayCardDecision(CardView cardView)
    {
        if(selectedCard == null)
        {
            selectedCard = cardView;
            selectedCard.SetSelected(true);
            PlayButton.gameObject.SetActive(true);
        }
        else
        {
            selectedCard.SetSelected(false);
            selectedCard = null;
            PlayButton.gameObject.SetActive(false);
        }
    }

    public void OnPlayButtonClicked()
    {
        if(selectedCard != null)
        {
            selectedCard.SetSelected(false);
            //发送请求前先取消选中，避免同步刷新后继续访问已销毁的卡牌对象。
            networkController.PlayCardRequest(selectedCard.CardInstanceID);
            selectedCard = null;
            PlayButton.gameObject.SetActive(false);
        }
    }

    public void AttackDecision(CardView cardView)
    {
        if(selectedCard == null)
        {
            selectedCard = cardView;
            selectedCard.SetSelected(true);
            AttackButton.gameObject.SetActive(true);
        }
        else
        {
            selectedCard.SetSelected(false);
            selectedCard = null;
            AttackButton.gameObject.SetActive(false);
        }
    }

    public void ChooseDecision(CardView cardView)
    {
        if(currentPhase != GamePhase.WaitingForChoice)
        {
            Debug.LogWarning("当前不在等待选择阶段，无法选择卡牌");
            return;
        }
        if (!pendingChoice.CandidateCardInstanceIDs.Contains(cardView.CardInstanceID))
        {
            Debug.LogWarning("选择的卡牌ID不在备选列表中");
            return;
        }
        if(choosedCards.Contains(cardView))
        {
            ChooseButton.gameObject.SetActive(true);
            choosedCards.Remove(cardView);
            cardView.SetAimed(false);
            return;
        }
        if(choosedCards.Count >= pendingChoice.MaxSelectCount)
        {
            Debug.LogWarning("已达到最大选择数量");
            return;
        }
        else
        {
            ChooseButton.gameObject.SetActive(true);
            choosedCards.Add(cardView);
            cardView.SetAimed(true);
        }
    }

    public void OnChooseButtonClicked()
    {
        if(choosedCards.Count < pendingChoice.MinSelectCount || choosedCards.Count > pendingChoice.MaxSelectCount)
        {
            Debug.LogWarning("选择的卡牌数量不符合要求");
            return;
        }
        List<int> selectedCardInstanceIDs = choosedCards.ConvertAll(c => c.CardInstanceID);
        networkController.SelectCardsRequest(selectedCardInstanceIDs);
    }

    public void OnAttackButtonClicked()
    {
        if(selectedCard != null)
        {
            selectedCard.SetSelected(false);
            //注意，这里需要先把selectedCard的待选设为false然后再发送网络请求
            //因为在网络请求发送后，可能会触发UI刷新，导致selectedCard被销毁，从而无法设置选中状态

            networkController.AttackDecisionRequest(selectedCard.CardInstanceID);
            
            selectedCard = null;
            AttackButton.gameObject.SetActive(false);
        }
    }

    public void BlockDecision(CardView cardView)
    {
        if(selectedCard == null)
        {
            selectedCard = cardView;
            selectedCard.SetSelected(true);
            BlockButton.gameObject.SetActive(true);
        }
        else
        {
            selectedCard.SetSelected(false);
            selectedCard = null;
            BlockButton.gameObject.SetActive(false);
        }
    }

    public void OnBlockButtonClicked()
    {
        if(selectedCard != null)
        {
            selectedCard.SetSelected(false);
            //注意，这里需要先把selectedCard的待选设为false然后再发送网络请求
            //因为在网络请求发送后，可能会触发UI刷新，导致selectedCard被销毁，从而无法设置选中状态
            networkController.BlockDecisionRequest(true, selectedCard.CardInstanceID);
            selectedCard = null;
            BlockButton.gameObject.SetActive(false);
        }
    }


    public void OnNoBlockButtonClicked()
    {
        networkController.BlockDecisionRequest(false, -1);
    }

    public void OnNoFrenzyAttackButtonClicked()
    {
        networkController.SkipFrenzyAttackRequest();
    }

    public void OnDiscardPileClicked(bool isLocalPlayer)
    {
        DiscardPilePannel.gameObject.SetActive(!DiscardPilePannel.gameObject.activeSelf);
        if(isLocalPlayer)
        {
            DiscardPilePannel.Find("local").gameObject.SetActive(true);
            DiscardPilePannel.Find("opponent").gameObject.SetActive(false);
        }
        else
        {
            DiscardPilePannel.Find("local").gameObject.SetActive(false);
            DiscardPilePannel.Find("opponent").gameObject.SetActive(true);
        }
    }

    public void OpenDiscardPilePannel(bool isLocalPlayer)
    {
        DiscardPilePannel.gameObject.SetActive(true);
        DiscardPilePannel.Find("local").gameObject.SetActive(isLocalPlayer);
        DiscardPilePannel.Find("opponent").gameObject.SetActive(!isLocalPlayer);
    }

    private bool ContainsChoiceCandidate(CardNetworkState[] cards)
    {
        if(pendingChoice == null)
        {
            return false;
        }

        foreach(var card in cards)
        {
            if(pendingChoice.CandidateCardInstanceIDs.Contains(card.CardInstanceID))
            {
                return true;
            }
        }

        return false;
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

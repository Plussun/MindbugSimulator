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

    // 所有能够看到真实实例ID的卡牌共用同一个字典。
    // 卡牌在手牌、场地、待决策区和弃牌区之间移动时，可以直接复用同一个CardView。
    private Dictionary<int, CardView> cardViews =
        new Dictionary<int, CardView>();

    // 每次刷新时记录新状态中仍然可见的卡牌，最后再统一删除真正消失的对象。
    private HashSet<int> visibleCardInstanceIDs = new HashSet<int>();
    private List<int> removedCardInstanceIDs = new List<int>();

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
        // 所有带真实实例ID的区域必须先全部处理完，再统一删除未出现的CardView。
        // 这样卡牌跨区域移动时只会改变父物体，不会被旧区域提前销毁。
        visibleCardInstanceIDs.Clear();
        removedCardInstanceIDs.Clear();

        RefreshLocalHandView(
            localPlayerHand,
            pendingAttack,
            pendingTarget,
            pendingChoice);
        RefreshFieldView(localPlayerField, LocalPlayer, pendingAttack, pendingTarget,pendingChoice);
        RefreshFieldView(opponentPlayerField, OpponentPlayer, pendingAttack, pendingTarget,pendingChoice);
        RefreshOpponentHandView(opponentHandCount, OpponentPlayer);
        RefreshPendingCardsView(pendingCard);

        RefreshDeckCount(true, localPlayerDeckCount);
        RefreshDeckCount(false, opponentPlayerDeckCount);

        RefreshDiscardCount(true, localPlayerDiscard.Length);
        RefreshDiscardPilePannel(true, localPlayerDiscard,pendingChoice);

        RefreshDiscardCount(false, opponentPlayerDiscard.Length);
        RefreshDiscardPilePannel(false, opponentPlayerDiscard,pendingChoice);

        RemoveInvisibleCardViews();

        // 清理完成后再布局，避免已经离开某区域的旧对象参与本次排版。
        LocalPlayer.Find("Hand").GetComponent<HandCardLayout>().RefreshLayout();
        LocalPlayer.Find("Field").GetComponent<FieldCardLayout>().RefreshLayout();
        OpponentPlayer.Find("Field").GetComponent<FieldCardLayout>().RefreshLayout();
        CardPreviewController.RefreshCurrentPreview();

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
            cardView.SetPointerActions(null, null, false);
            cardView.transform.localPosition = new Vector3(i * 120, 0, 0); // 调整卡牌位置
            cardView.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f); // 确保卡牌缩放为0.4

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

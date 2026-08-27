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

    public Button NoBlockButton;
    public Button NoMindbugButton;
    public Button UseMindbugButton;
    public Button AttackButton;
    public Button BlockButton;

    public GameController gameController;
    public NetworkController networkController;

    private GamePhase currentPhase;
    private bool isLocalPlayerExpected;
    private CardView selectedCard;

    // Start is called before the first frame update
    void Start()
    {
        NoBlockButton.onClick.AddListener(OnNoBlockButtonClicked);
        AttackButton.onClick.AddListener(OnAttackButtonClicked);
        BlockButton.onClick.AddListener(OnBlockButtonClicked);
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
        int localPlayerDiscardCount,
        int opponentPlayerDiscardCount,
        CardNetworkState[] localPlayerHand,
        CardNetworkState[] localPlayerField,
        CardNetworkState[] opponentPlayerField,
        int opponentHandCount,
        CardNetworkState pendingCard,
        CardNetworkState pendingAttack
        )
    {
        currentPhase = (GamePhase)gamePhase;
        isLocalPlayerExpected = (localPlayerID == ExpectedPlayerID);

        selectedCard = null;
        AttackButton.gameObject.SetActive(false);
        
        RefreshPlayerPortrait(true, localPlayerLife, 
            localPlayerMindbugCount, isLocalPlayerExpected);
        RefreshPlayerPortrait(false, opponentPlayerLife, opponentPlayerMindbugCount,
            !isLocalPlayerExpected);
        RefreshHandOrFieldView(localPlayerHand, LocalPlayer, "Hand", pendingAttack);
        RefreshHandOrFieldView(localPlayerField, LocalPlayer, "Field", pendingAttack);
        RefreshHandOrFieldView(opponentPlayerField, OpponentPlayer, "Field", pendingAttack);
        RefreshOpponentHandView(opponentHandCount, OpponentPlayer);
        RefreshPendingCardsView(pendingCard);

        RefreshDeckCount(true, localPlayerDeckCount);
        RefreshDeckCount(false, opponentPlayerDeckCount);
        RefreshDiscardCount(true, localPlayerDiscardCount);
        RefreshDiscardCount(false, opponentPlayerDiscardCount);
        RefreshButtons(localPlayerMindbugCount);
        RefreshWinnerView(winnerPlayerID, localPlayerID);
        
    }

    public void RefreshHandOrFieldView(CardNetworkState[] cards,
        Transform playerTransform,
        string handOrField,CardNetworkState pendingAttack)
    {
        Transform handContainer = playerTransform.Find(handOrField);
        // 清空现有手牌视图
        foreach (Transform child in handContainer)
        {
            Destroy(child.gameObject);
        }
        // 创建新的手牌视图
        for(int i = 0; i < cards.Length; i++)
        {
            GameObject cardViewObj = Instantiate(CardViewPrefab, handContainer);
            CardView cardView = cardViewObj.GetComponent<CardView>();
            // 创建CardInstance对象
            CardData cardData = GetCardDataByID(cards[i].CardDataID);
            cardView.UpdateCardView(cardData.CardName, cards[i].currentPower, cards[i].CardInstanceID,cards[i].isExhausted);

            bool isLocalHand = (playerTransform == LocalPlayer && handOrField == "Hand");
            bool isLocalField = (playerTransform == LocalPlayer && handOrField == "Field");
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


            cardView.transform.localPosition = new Vector3(i * 100, 0, 0); // 调整卡牌位置
            cardView.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f); // 确保卡牌缩放为0.4
            
            // 高亮显示当前待攻击决策的卡牌
            if(pendingAttack.CardInstanceID == cards[i].CardInstanceID)
            {
                cardView.transform.Find("Highlight").gameObject.SetActive(true);
            }
            else
            {
                cardView.transform.Find("Highlight").gameObject.SetActive(false);
            }
        }
    }

    // 刷新对手手牌视图，显示为背面,并且数量与对手手牌数量一致
    public void RefreshOpponentHandView(int opponentHandCount, Transform opponentTransform)
    {
        Transform handContainer = opponentTransform.Find("Hand");
        // 清空现有手牌视图
        foreach (Transform child in handContainer)
        {
            Destroy(child.gameObject);
        }
        // 创建新的手牌视图
        for(int i = 0; i < opponentHandCount; i++)
        {
            GameObject cardViewObj = Instantiate(CardViewPrefab, handContainer);
            CardView cardView = cardViewObj.GetComponent<CardView>();
            // 设置卡牌为背面显示
            cardView.UpdateCardView("Back", 0, -1, false); // 使用-1表示未知的CardInstanceID
            cardView.transform.localPosition = new Vector3(i * 100, 0, 0); // 调整卡牌位置
            cardView.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f); // 确保卡牌缩放为0.4
        }
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
            pendingCard.currentPower, pendingCard.CardInstanceID, pendingCard.isExhausted);
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
    public void RefreshDeckCount(bool isLocalPlayer, int deckCount)
    {
        Transform portraitTransform = 
            isLocalPlayer ? LocalPlayer.Find("Deck") : OpponentPlayer.Find("Deck");
        TMP_Text deckText = portraitTransform.Find("DeckCount").GetComponent<TMP_Text>();
        deckText.text = deckCount.ToString();
    }

    public void RefreshButtons(int localPlayerMindbugCount)
    {
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

            NoBlockButton.gameObject.SetActive(false);
        }
        else if(currentPhase == GamePhase.WaitingForBlockDecision && isLocalPlayerExpected)
        {
            
            UseMindbugButton.gameObject.SetActive(false);
            NoMindbugButton.gameObject.SetActive(false);

            NoBlockButton.gameObject.SetActive(true);
        }
        else
        {
            UseMindbugButton.gameObject.SetActive(false);
            NoMindbugButton.gameObject.SetActive(false);

            NoBlockButton.gameObject.SetActive(false);
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
        networkController.PlayCardRequest(cardView.CardInstanceID);
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


    public CardData GetCardDataByID(int cardDataID)
    {
        // 这里你需要实现根据cardDataID从你的卡牌数据库中获取CardData的逻辑
        // 例如，你可以有一个CardDatabase类来管理所有的CardData
        CardData cardData = gameController.CardDatabase.Find(
                c => c.CardDataID == cardDataID);
        return cardData;
    }
}

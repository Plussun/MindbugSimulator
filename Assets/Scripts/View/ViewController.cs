using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
public class ViewController : MonoBehaviour
{
    public GameObject CardViewPrefab;
    public Transform LocalPlayer;
    public Transform OpponentPlayer;
    public Transform PendingCardsContainer;
    public GameController gameController;
    public NetworkController networkController;

    // Start is called before the first frame update
    void Start()
    {
        
    }
    public void RefreshView(CardNetworkState[] localPlayerHand,
        CardNetworkState[] localPlayerField,
        CardNetworkState[] opponentPlayerField,
        int opponentHandCount,
        CardNetworkState pendingCard
        )
    {
        RefreshHandOrFieldView(localPlayerHand, LocalPlayer, "Hand");
        RefreshHandOrFieldView(localPlayerField, LocalPlayer, "Field");
        RefreshHandOrFieldView(opponentPlayerField, OpponentPlayer, "Field");
        RefreshOpponentHandView(opponentHandCount, OpponentPlayer);
        RefreshPendingCardsView(pendingCard);
        
    }

    public void RefreshHandOrFieldView(CardNetworkState[] cards,
        Transform playerTransform,
        string handOrField = "Hand")
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
            cardView.UpdateCardView(cardData.CardName, cards[i].currentPower, cards[i].CardInstanceID);

            if(playerTransform == LocalPlayer&& handOrField == "Hand")
            {
                cardView.SetClickAction(networkController.PlayCardRequest);
            }
            cardView.transform.localPosition = new Vector3(i * 100, 0, 0); // 调整卡牌位置
            cardView.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f); // 确保卡牌缩放为0.4
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
            cardView.UpdateCardView("Back", 0, -1); // 使用-1表示未知的CardInstanceID
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
            pendingCard.currentPower, pendingCard.CardInstanceID);
        cardView.transform.localPosition = new Vector3(0, 0, 0); // 调整卡牌位置
        cardView.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f); // 确保卡牌缩放为0.5
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

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 只负责牌库的显示。数量由ViewController根据客户端快照传入。
public class DeckPileView : MonoBehaviour
{
    public RectTransform DeckStartPoint;
    public GameObject DeckCardBackPrefab;

    // 本方牌库向右上偏移；对方牌库可在Inspector中设为(2, -2)。
    public Vector2 CardOffset = new Vector2(2f, 2f);

    private readonly List<GameObject> cardBacks = new List<GameObject>();

    public void RefreshPile(int cardCount)
    {
        if(DeckStartPoint == null || DeckCardBackPrefab == null)
        {
            Debug.LogWarning("DeckPileView需要绑定DeckStartPoint和DeckCardBackPrefab", this);
            return;
        }

        cardCount = Mathf.Max(0, cardCount);

        // 增量更新：保留已有卡背，只补充或删除数量差额。
        while(cardBacks.Count < cardCount)
        {
            GameObject cardBack = Instantiate(
                DeckCardBackPrefab, DeckStartPoint, false);
            // 弃牌堆的卡背需要接收射线，点击事件会向上传递给DiscardPileView。
            // 牌库共用这个预制体，仍保留其原本不接收点击的设置。
            if(TryGetComponent<DiscardPileView>(out _))
            {
                foreach(Graphic graphic in cardBack.GetComponentsInChildren<Graphic>(true))
                {
                    graphic.raycastTarget = true;
                }
            }
            cardBacks.Add(cardBack);
        }

        while(cardBacks.Count > cardCount)
        {
            int lastIndex = cardBacks.Count - 1;
            GameObject cardBack = cardBacks[lastIndex];
            cardBacks.RemoveAt(lastIndex);

            // Destroy在帧末生效，先隐藏，避免多余卡背在本帧继续显示。
            cardBack.SetActive(false);
            Destroy(cardBack);
        }

        for(int i = 0; i < cardBacks.Count; i++)
        {
            RectTransform cardRect = (RectTransform)cardBacks[i].transform;
            cardRect.anchoredPosition = CardOffset * i;

            // 索引0始终是顶层牌，固定在StartPoint中心。
            // 偏移的下层牌放在更早的兄弟层级，才会露出右上方的牌边。
            cardRect.SetAsFirstSibling();
        }
    }
}

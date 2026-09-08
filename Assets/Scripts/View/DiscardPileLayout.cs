using UnityEngine;

// 将弃牌区卡牌从左向右排列；单行超过最大宽度后自动分成两行。
// 两行会尽量平均分配卡牌，并在卡牌过多时缩短横向间距，始终限制在最大宽度内。
public class DiscardPileLayout : MonoBehaviour
{
    // 从当前物体位置开始，弃牌区允许占据的最大宽度。
    public float MaxWidth = 1000f;
    // 空间足够时，相邻卡牌中心之间的距离。
    public float PreferredSpacing = 120f;
    // 两行卡牌中心之间的垂直距离。
    public float RowSpacing = 180f;
    // 弃牌区卡牌的统一缩放比例。
    public float CardScale = 0.4f;

    public void RefreshLayout()
    {
        int cardCount = transform.childCount;
        if(cardCount == 0)
        {
            return;
        }

        RectTransform firstCard = transform.GetChild(0) as RectTransform;
        float cardDisplayWidth = firstCard.rect.width * CardScale;
        float singleRowWidth = cardDisplayWidth +
            PreferredSpacing * (cardCount - 1);

        // 一行能够放下时保持单行；否则最多分成两行。
        int firstRowCount = singleRowWidth <= MaxWidth
            ? cardCount
            : (cardCount + 1) / 2;
        int secondRowCount = cardCount - firstRowCount;

        LayoutRow(0, firstRowCount, 0, cardDisplayWidth);
        if(secondRowCount > 0)
        {
            LayoutRow(
                firstRowCount,
                secondRowCount,
                -RowSpacing,
                cardDisplayWidth);
        }
    }

    private void LayoutRow(
        int startIndex,
        int rowCardCount,
        float y,
        float cardDisplayWidth)
    {
        float availableSpacingWidth = Mathf.Max(0, MaxWidth - cardDisplayWidth);
        float spacing = rowCardCount <= 1
            ? 0
            : Mathf.Min(
                PreferredSpacing,
                availableSpacingWidth / (rowCardCount - 1));

        float rowWidth = cardDisplayWidth + spacing * (rowCardCount - 1);
        // 当前物体作为布局区域的左侧起点，每一行在MaxWidth范围内单独居中。
        float firstCardX = (MaxWidth - rowWidth) / 2f + cardDisplayWidth / 2f;

        for(int i = 0; i < rowCardCount; i++)
        {
            RectTransform card = transform.GetChild(startIndex + i) as RectTransform;
            card.anchoredPosition = new Vector2(firstCardX + i * spacing, y);
            card.localScale = Vector3.one * CardScale;
        }
    }
}

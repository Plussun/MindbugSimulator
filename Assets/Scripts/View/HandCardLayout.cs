using UnityEngine;

// 将当前物体的所有子物体按手牌形式居中排列。
// 卡牌较少时使用固定间距，卡牌较多时自动缩短间距并允许互相遮叠。
public class HandCardLayout : MonoBehaviour
{
    // 整组手牌允许占据的最大宽度。
    public float MaxWidth = 800f;
    // 空间足够时，相邻卡牌中心之间的距离。
    public float PreferredSpacing = 120f;
    // 扇形两端相对中心卡牌的垂直偏移量。
    public float ArcHeight = 40f;
    // 扇形最左侧和最右侧卡牌的最大旋转角度。
    public float MaxAngle = 8f;
    // 手牌正常显示时的缩放比例。
    public float CardScale = 0.4f;
    // 敌方手牌位于画面上方，需要反转弧形的弯曲方向。
    public bool InvertArc;

    public void RefreshLayout()
    {
        int cardCount = transform.childCount;
        if(cardCount == 0)
        {
            return;
        }

        // 直接读取卡牌根节点宽度，使预制体成为卡牌尺寸的唯一来源。
        RectTransform firstCard = transform.GetChild(0) as RectTransform;
        float cardDisplayWidth = firstCard.rect.width * CardScale;
        float availableWidth = Mathf.Max(0, MaxWidth - cardDisplayWidth);

        // 手牌放不下时只缩短间距，不缩小卡牌。
        float spacing = cardCount == 1
            ? 0
            : Mathf.Min(PreferredSpacing, availableWidth / (cardCount - 1));

        // 使用可带有0.5小数的中心下标，使奇数和偶数张牌都能关于原点对称。
        float centerIndex = (cardCount - 1) / 2f;
        float arcDirection = InvertArc ? 1 : -1;

        for(int i = 0; i < cardCount; i++)
        {
            RectTransform card =
                transform.GetChild(i).GetComponent<RectTransform>();

            float offset = i - centerIndex;
            float normalized = centerIndex == 0 ? 0 : offset / centerIndex;
            float x = offset * spacing;

            // 使用平方曲线：中心变化较缓，两端逐渐向弧形外侧弯曲。
            float y = normalized * normalized * ArcHeight * arcDirection;
            float angle = normalized * MaxAngle * arcDirection;

            card.anchoredPosition = new Vector2(x, y);
            card.localRotation = Quaternion.Euler(0, 0, angle);
            card.localScale = Vector3.one * CardScale;
        }
    }
}

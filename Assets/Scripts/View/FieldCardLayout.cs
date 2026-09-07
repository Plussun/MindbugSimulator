using UnityEngine;

// 将当前物体的所有子物体按场地卡牌形式居中排列。
// 卡牌放不下时统一缩小整组卡牌，保证相邻卡牌不会互相遮叠。
public class FieldCardLayout : MonoBehaviour
{
    // 整组场地卡牌允许占据的最大宽度。
    public float MaxWidth = 800f;
    // 卡牌预制体未经缩放时的实际宽度。
    public float CardWidth = 300f;
    // 未缩放状态下，两张卡牌边缘之间预留的距离。
    public float Gap = 20f;
    // 空间足够时，场地卡牌使用的正常缩放比例。
    public float NormalScale = 0.4f;

    public void RefreshLayout()
    {
        int cardCount = transform.childCount;
        if(cardCount == 0)
        {
            return;
        }

        // 先计算原始尺寸下需要的总宽度，再求出能够放入MaxWidth的缩放比例。
        float requiredWidth =
            cardCount * CardWidth + (cardCount - 1) * Gap;
        float scale = Mathf.Min(NormalScale, MaxWidth / requiredWidth);
        float spacing = (CardWidth + Gap) * scale;

        // 使用中心下标计算位置，使整组卡牌始终关于容器原点对称。
        float centerIndex = (cardCount - 1) / 2f;

        for(int i = 0; i < cardCount; i++)
        {
            RectTransform card =
                transform.GetChild(i).GetComponent<RectTransform>();

            float x = (i - centerIndex) * spacing;
            card.anchoredPosition = new Vector2(x, 0);
            card.localScale = Vector3.one * scale;

            // 不修改旋转角度，保留CardView设置的横置状态。
        }
    }
}

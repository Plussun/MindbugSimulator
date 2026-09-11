using UnityEngine;

// 管理唯一的卡牌信息预览对象。
// 预览只复制显示数据，不参与点击、选择或任何游戏逻辑。
public class CardPreviewController : MonoBehaviour
{
    public GameObject CardViewPrefab;

    // 预览卡相对于预制体原始大小的缩放比例。
    public float PreviewScale = 0.8f;
    // 预览卡与原卡牌边缘之间的距离。
    public float HorizontalSpacing = 20f;
    // 预览卡相对原卡牌中心向下偏移的距离。
    public float VerticalOffset = 30f;
    // 预览卡与屏幕四边之间保留的最小距离。
    public float ScreenMargin = 20f;
    private Canvas canvas;
    private RectTransform canvasRect;
    private RectTransform previewRoot;
    private RectTransform previewCardRect;
    private CardView previewCard;
    private CardView currentSourceCard;

    private readonly Vector3[] sourceWorldCorners = new Vector3[4];

    private void Awake()
    {
        canvas = GetComponentInParent<Canvas>();
        canvasRect = canvas.transform as RectTransform;
        previewRoot = transform as RectTransform;

        GameObject previewObject = Instantiate(CardViewPrefab, transform);
        previewCard = previewObject.GetComponent<CardView>();
        previewCardRect = previewObject.transform as RectTransform;
        previewCardRect.anchoredPosition = Vector2.zero;
        previewCardRect.localScale = Vector3.one * PreviewScale;

        // CanvasGroup可以一次性阻止整个预览对象拦截鼠标事件。
        // 因此鼠标不会因为预览出现在附近而离开原卡牌或触发预览点击。
        CanvasGroup canvasGroup = previewObject.AddComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;

        // 预览卡只显示卡牌信息，不显示任何游戏状态标记。
        previewCard.Highlight.SetActive(false);
        previewCard.Selected.SetActive(false);
        previewCard.Aimed.SetActive(false);
        previewCard.Candidate.SetActive(false);

        previewObject.SetActive(false);
    }

    public void Show(CardView sourceCard)
    {
        currentSourceCard = sourceCard;

        // 预览始终保持正向显示，即使场上的原卡牌处于横置状态。
        previewCard.UpdateCardView(
            sourceCard.CurrentCardName,
            sourceCard.CurrentCardDescription,
            sourceCard.CurrentPower,
            sourceCard.CardInstanceID,
            (int)sourceCard.CurrentKeywords,
            false,
            sourceCard.CardImage.sprite);

        previewCardRect.localScale = Vector3.one * PreviewScale;
        PositionPreview(sourceCard);

        // CardPreviewRoot位于ViewObject最后方，保证预览绘制在其他游戏UI之上。
        previewRoot.SetAsLastSibling();
        previewCard.gameObject.SetActive(true);
    }

    // 只有正在预览的原卡牌离开时才隐藏。
    // 这样旧卡牌迟到的PointerExit不会错误隐藏刚显示的新预览。
    public void Hide(CardView sourceCard)
    {
        if(currentSourceCard != sourceCard)
        {
            return;
        }

        Hide();
    }

    public void Hide()
    {
        currentSourceCard = null;

        if(previewCard != null)
        {
            previewCard.gameObject.SetActive(false);
        }
    }

    // 状态和布局刷新完成后，同步当前预览的数值与位置。
    public void RefreshCurrentPreview()
    {
        if(currentSourceCard != null)
        {
            Show(currentSourceCard);
        }
    }

    private void PositionPreview(CardView sourceCard)
    {
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : canvas.worldCamera;

        // 使用实际卡面的四角计算范围，横置或缩放后的卡牌也能正确定位预览。
        RectTransform sourceRect = sourceCard.CardBackground;

        sourceRect.GetWorldCorners(sourceWorldCorners);

        float minX = float.MaxValue;
        float maxX = float.MinValue;
        float minY = float.MaxValue;
        float maxY = float.MinValue;

        // 先把卡牌四角统一转换为Canvas局部坐标，横置卡牌也能得到正确边界。
        for(int i = 0; i < sourceWorldCorners.Length; i++)
        {
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(
                uiCamera,
                sourceWorldCorners[i]);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                screenPoint,
                uiCamera,
                out Vector2 canvasPoint);

            minX = Mathf.Min(minX, canvasPoint.x);
            maxX = Mathf.Max(maxX, canvasPoint.x);
            minY = Mathf.Min(minY, canvasPoint.y);
            maxY = Mathf.Max(maxY, canvasPoint.y);
        }

        float sourceCenterX = (minX + maxX) / 2f;
        float sourceCenterY = (minY + maxY) / 2f;
        // 直接读取预制体中的卡面尺寸，避免在脚本中重复维护固定数值。
        float previewHalfWidth =
            previewCard.CardBackground.rect.width * PreviewScale / 2f;
        float previewHalfHeight =
            previewCard.CardBackground.rect.height * PreviewScale / 2f;

        // 原卡在左半边时预览放右侧，原卡在右半边时预览放左侧。
        float previewX = sourceCenterX < canvasRect.rect.center.x
            ? maxX + HorizontalSpacing + previewHalfWidth
            : minX - HorizontalSpacing - previewHalfWidth;
        float previewY = sourceCenterY - VerticalOffset;

        // 对最终位置进行四边限制，避免任何分辨率下预览超出屏幕。
        previewX = Mathf.Clamp(
            previewX,
            canvasRect.rect.xMin + previewHalfWidth + ScreenMargin,
            canvasRect.rect.xMax - previewHalfWidth - ScreenMargin);
        previewY = Mathf.Clamp(
            previewY,
            canvasRect.rect.yMin + previewHalfHeight + ScreenMargin,
            canvasRect.rect.yMax - previewHalfHeight - ScreenMargin);

        // CardPreviewRoot不直接挂在Canvas下，所以先从Canvas局部坐标转换到世界坐标。
        previewRoot.position = canvasRect.TransformPoint(
            new Vector3(previewX, previewY, 0));
    }
}

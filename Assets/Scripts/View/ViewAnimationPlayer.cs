using System.Collections;
using UnityEngine;

// 只负责播放单条显示动画。
// 动画队列、状态刷新和整体播放流程由ViewController负责。
public class ViewAnimationPlayer : MonoBehaviour
{
    public float DrawDuration = 1.0f;
    public float DrawInterval = 0.2f;
    public float DrawArcHeight = 150f;

    public IEnumerator PlayAnimation(ClientAnimationEvent animationEvent)
    {
        switch(animationEvent.AnimationType)
        {
            case GameAnimationType.PlayCardToPending:
                // 暂时不播放移动过程；ViewController随后应用事件快照时，
                // 会把同一个CardView从手牌转移到待选区。
                yield break;

            case GameAnimationType.DrawCard:
                // 双方抽牌都由ViewController准备实际CardView后调用对应方法。
                yield break;

            case GameAnimationType.DiscardCard:
                yield return PlayDiscardAnimation(animationEvent);
                break;

            case GameAnimationType.StateRefresh:
                // 普通状态刷新没有动画，直接交还给ViewController处理最终状态。
                yield break;
        }
    }

    // 只负责移动已经存在的正式CardView，不创建或销毁任何卡牌对象。
    public IEnumerator PlayDrawAnimation(
        CardView cardView,
        Vector3 targetPosition,
        Quaternion targetRotation)
    {
        Animator animator = cardView.GetComponent<Animator>();

        // 本方抽牌需要翻到正面；根节点移动仍由下面的共用协程负责。
        animator.SetTrigger("Flip");

        yield return PlayCardMoveAnimation(
            cardView,
            targetPosition,
            targetRotation,
            DrawArcHeight);
    }

    // 对手抽到的牌始终保持背面，并使用向下拱起的反向弧线。
    public IEnumerator PlayOpponentDrawAnimation(
        CardView cardView,
        Vector3 targetPosition,
        Quaternion targetRotation)
    {
        yield return PlayCardMoveAnimation(
            cardView,
            targetPosition,
            targetRotation,
            -DrawArcHeight);
    }

    // 双方抽牌共用同一套移动和旋转计算，arcHeight的正负决定弧线方向。
    private IEnumerator PlayCardMoveAnimation(
        CardView cardView,
        Vector3 targetPosition,
        Quaternion targetRotation,
        float arcHeight)
    {
        RectTransform cardRect = cardView.transform as RectTransform;
        Vector3 startPosition = cardRect.localPosition;
        Quaternion startRotation = cardRect.localRotation;

        float elapsed = 0f;
        while(elapsed < DrawDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            // 把已经播放的时间转换为0到1之间的进度：
            // 0表示动画刚开始，1表示已经到达终点。
            float progress = Mathf.Clamp01(elapsed / DrawDuration);

            // SmoothStep把匀速进度变成两端慢、中间快的进度，
            // 让卡牌起步和停下时更加柔和。
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // 先沿起点到终点之间的直线移动。
            Vector3 position = Vector3.Lerp(
                startPosition,
                targetPosition,
                smoothProgress);

            // sin(0)=0、sin(PI/2)=1、sin(PI)=0，
            // 因此弧线在起点和终点没有偏移，并在动画中点达到最大偏移。
            // arcHeight为正时向上拱，为负时向下拱。
            position.y += Mathf.Sin(progress * Mathf.PI) * arcHeight;

            cardRect.localPosition = position;

            // 移动的同时从起始角度平滑旋转到手牌布局计算出的最终角度。
            cardRect.localRotation = Quaternion.Lerp(
                startRotation,
                targetRotation,
                smoothProgress);

            yield return null;
        }
        
        yield return new WaitForSecondsRealtime(DrawInterval);

        // 明确写入终点，避免最后一帧因为浮点误差与正式布局产生轻微跳动。
        cardRect.localPosition = targetPosition;
        cardRect.localRotation = targetRotation;
    }

    private IEnumerator PlayDiscardAnimation(ClientAnimationEvent animationEvent)
    {
        Debug.Log("玩家" + animationEvent.PlayerID + "的卡牌"
            + animationEvent.CardInstanceID + "播放了弃牌动画");

        // 暂时等待一帧模拟动画，之后在这里替换为实际弃牌动画。
        yield return null;
    }
}

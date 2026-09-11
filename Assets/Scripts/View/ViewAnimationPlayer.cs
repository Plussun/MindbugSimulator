using System.Collections;
using UnityEngine;

// 一张卡牌在AnimationCanvas坐标系中的移动终点。
// 多张卡牌同时阵亡或转移控制权时，可以组成数组一起播放。
public struct CardMoveAnimationTarget
{
    // CardView在开始播放时已经位于AnimationCanvas中。
    public CardView CardView;
    // 以下三个值也都使用AnimationCanvas的本地坐标系。
    public Vector3 TargetPosition;
    public Quaternion TargetRotation;
    public Vector3 TargetScale;
    // 正数向上拱，负数向下拱，0表示直线移动。
    public float ArcHeight;

    public CardMoveAnimationTarget(
        CardView cardView,
        Vector3 targetPosition,
        Quaternion targetRotation,
        Vector3 targetScale,
        float arcHeight)
    {
        CardView = cardView;
        TargetPosition = targetPosition;
        TargetRotation = targetRotation;
        TargetScale = targetScale;
        ArcHeight = arcHeight;
    }
}

// 只负责播放单条显示动画。
// 动画队列、状态刷新和整体播放流程由ViewController负责。
public class ViewAnimationPlayer : MonoBehaviour
{
    [Header("战斗动画")]
    public float CombatPrepareDuration = 0.10f;
    public float CombatStrikeDuration = 0.12f;
    public float CombatImpactPause = 0.06f;
    public float CombatReturnDuration = 0.18f;
    public float CombatScaleMultiplier = 1.12f;
    // 在动画层的本地坐标中，撞击时与阻挡牌中心保留的距离。
    public float CombatStopDistance = 50f;

    [Header("阵亡特效")]
    // 在Inspector中绑定DefeatEffect预制体。实例化后由预制体上的Animator自动播放。
    public GameObject DefeatEffectPrefab;
    // 应与DefeatEffect动画片段的长度一致；时间结束后销毁临时特效对象。
    public float DefeatEffectDuration = 1f;

    [Header("卡牌移动")]
    public float DrawDuration = 1.0f;
    public float DrawInterval = 0.2f;
    public float DrawArcHeight = 150f;
    public float CardMoveDuration = 0.6f;
    public float CardMoveArcHeight = 100f;
    // 卡牌飞到弃牌堆图标时相对起始大小的缩放比例。
    public float DiscardTargetScaleMultiplier = 0.6f;

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
            cardView.transform.localScale,
            DrawDuration,
            DrawArcHeight);

        yield return new WaitForSecondsRealtime(DrawInterval);
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
            cardView.transform.localScale,
            DrawDuration,
            -DrawArcHeight);

        yield return new WaitForSecondsRealtime(DrawInterval);
    }

    // 正式CardView已由ViewController移入动画层，目标位置也使用该层坐标。
    // 这里只播放动作；战斗结果和阵亡特效由后续事件处理。
    public IEnumerator PlayCombatAnimation(CardView attackCard, Vector3 targetPosition)
    {
        Transform cardTransform = attackCard.transform;
        Vector3 startPosition = cardTransform.localPosition;
        Quaternion startRotation = cardTransform.localRotation;
        Vector3 startScale = cardTransform.localScale;
        Vector3 enlargedScale = startScale * CombatScaleMultiplier;

        // 沿两张牌的连线接近目标；保留一点距离，避免两张牌中心完全重叠。
        Vector3 direction = targetPosition - startPosition;
        float stopDistance = Mathf.Clamp(CombatStopDistance, 0f, direction.magnitude);
        Vector3 impactPosition = targetPosition - direction.normalized * stopDistance;

        yield return PlayCardMoveAnimation(
            attackCard, startPosition, startRotation, enlargedScale,
            CombatPrepareDuration, 0f);
        yield return PlayCardMoveAnimation(
            attackCard, impactPosition, startRotation, enlargedScale,
            CombatStrikeDuration, 0f);

        // 短暂停在撞击点，给玩家看清接触瞬间的时间。
        yield return new WaitForSecondsRealtime(CombatImpactPause);

        yield return PlayCardMoveAnimation(
            attackCard, startPosition, startRotation, startScale,
            CombatReturnDuration, 0f);
    }

    // 所有卡牌区域移动共用同一套位置、旋转和缩放计算。
    // duration决定移动时间，arcHeight的正负决定弧线方向。
    public IEnumerator PlayCardMoveAnimation(
        CardView cardView,
        Vector3 targetPosition,
        Quaternion targetRotation,
        Vector3 targetScale,
        float duration,
        float arcHeight)
    {
        // 单张移动也包装成数组交给批量入口，保证所有卡牌移动只维护一套插值代码。
        CardMoveAnimationTarget[] targets =
        {
            new CardMoveAnimationTarget(
                cardView,
                targetPosition,
                targetRotation,
                targetScale,
                arcHeight)
        };

        yield return PlayCardMoveAnimations(targets, duration);
    }

    // 在同一个逐帧循环中移动多张牌，确保同时阵亡等动画真正同步开始和结束。
    public IEnumerator PlayCardMoveAnimations(
        CardMoveAnimationTarget[] targets,
        float duration)
    {
        if(targets == null || targets.Length == 0)
        {
            yield break;
        }

        // 播放开始时冻结所有对象的起点，之后目标区域怎样刷新都不会改变本次轨迹。
        Vector3[] startPositions = new Vector3[targets.Length];
        Quaternion[] startRotations = new Quaternion[targets.Length];
        Vector3[] startScales = new Vector3[targets.Length];

        for(int i = 0; i < targets.Length; i++)
        {
            RectTransform cardRect =
                targets[i].CardView.transform as RectTransform;
            startPositions[i] = cardRect.localPosition;
            startRotations[i] = cardRect.localRotation;
            startScales[i] = cardRect.localScale;
        }

        float elapsed = 0f;
        while(elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            // 把已经播放的时间转换为0到1之间的进度：
            // 0表示动画刚开始，1表示已经到达终点。
            float progress = Mathf.Clamp01(elapsed / duration);

            // SmoothStep把匀速进度变成两端慢、中间快的进度，
            // 让卡牌起步和停下时更加柔和。
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // 所有目标在同一帧使用相同的progress，因此能够真正同时开始和结束。
            for(int i = 0; i < targets.Length; i++)
            {
                CardMoveAnimationTarget target = targets[i];
                RectTransform cardRect =
                    target.CardView.transform as RectTransform;

                // 先沿起点到终点之间的直线移动。
                Vector3 position = Vector3.Lerp(
                    startPositions[i],
                    target.TargetPosition,
                    smoothProgress);

                // sin(0)=0、sin(PI/2)=1、sin(PI)=0，
                // 因此弧线在起点和终点没有偏移，并在动画中点达到最大偏移。
                // ArcHeight为正时向上拱，为负时向下拱。
                position.y += Mathf.Sin(progress * Mathf.PI) *
                    target.ArcHeight;

                cardRect.localPosition = position;
                cardRect.localRotation = Quaternion.Lerp(
                    startRotations[i],
                    target.TargetRotation,
                    smoothProgress);
                cardRect.localScale = Vector3.Lerp(
                    startScales[i],
                    target.TargetScale,
                    smoothProgress);
            }

            yield return null;
        }

        // 明确写入终点，避免最后一帧因为浮点误差与正式布局产生轻微跳动。
        for(int i = 0; i < targets.Length; i++)
        {
            RectTransform cardRect =
                targets[i].CardView.transform as RectTransform;
            cardRect.localPosition = targets[i].TargetPosition;
            cardRect.localRotation = targets[i].TargetRotation;
            cardRect.localScale = targets[i].TargetScale;
        }
    }

    // 在所有真正阵亡的卡牌上同时播放特效。
    // 这里生成的只是短暂的特效对象，正式CardView仍由之后的弃牌移动动画继续复用。
    public IEnumerator PlayDefeatEffects(
        CardView[] defeatedCards,
        Transform effectParent)
    {
        if(defeatedCards == null || defeatedCards.Length == 0)
        {
            yield break;
        }

        if(DefeatEffectPrefab == null)
        {
            Debug.LogError("ViewAnimationPlayer没有绑定DefeatEffect预制体");
            yield break;
        }

        GameObject[] effects = new GameObject[defeatedCards.Length];

        for(int i = 0; i < defeatedCards.Length; i++)
        {
            if(defeatedCards[i] == null)
            {
                continue;
            }

            // 特效放在AnimationCanvas最上层，并使用卡牌中心的世界坐标。
            // 因此卡牌属于本方还是对方、处于哪个场地，都不需要分别换算位置。
            GameObject effect = Instantiate(DefeatEffectPrefab, effectParent);
            RectTransform effectRect = effect.transform as RectTransform;
            effectRect.position = defeatedCards[i].transform.position;
            effectRect.localRotation = Quaternion.identity;
            effectRect.SetAsLastSibling();
            effects[i] = effect;
        }

        // 使用Realtime保证以后即使暂停Time.timeScale，界面特效仍能正常播完。
        yield return new WaitForSecondsRealtime(DefeatEffectDuration);

        for(int i = 0; i < effects.Length; i++)
        {
            if(effects[i] != null)
            {
                Destroy(effects[i]);
            }
        }
    }

    private IEnumerator PlayDiscardAnimation(ClientAnimationEvent animationEvent)
    {
        Debug.Log("玩家" + animationEvent.PlayerID + "的卡牌"
            + animationEvent.CardInstanceID + "播放了弃牌动画");

        // 暂时等待一帧模拟动画，之后在这里替换为实际弃牌动画。
        yield return null;
    }
}

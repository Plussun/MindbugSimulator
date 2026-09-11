using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public partial class ViewController
{
    public ViewAnimationPlayer AnimationPlayer;
    public GameObject AnimationCanvas;

    private readonly Queue<ClientAnimationEvent> animationQueue =
        new Queue<ClientAnimationEvent>();
    private bool isProcessingAnimation;

    // 网络层只需要把该玩家可见的动画事件交给这个入口。
    // 如果之前的动画还没有播放完，新事件会直接追加到现有队列末尾。
    public void ReceiveAnimationEvents(ClientAnimationEvent[] animationEvents)
    {
        foreach(ClientAnimationEvent animationEvent in animationEvents)
        {
            animationQueue.Enqueue(animationEvent);
        }

        if(!isProcessingAnimation)
        {
            StartCoroutine(ProcessAnimationQueue());
        }
    }

    private IEnumerator ProcessAnimationQueue()
    {
        isProcessingAnimation = true;

        while(animationQueue.Count > 0)
        {
            ClientAnimationEvent animationEvent = animationQueue.Dequeue();

            // 抽牌前需要由ViewController准备实际CardView和布局终点。
            if(animationEvent.AnimationType == GameAnimationType.DrawCard)
            {
                if(animationEvent.PlayerID ==
                    animationEvent.StateAfterEvent.LocalPlayerID)
                {
                    yield return ProcessLocalDrawEvent(animationEvent);
                }
                else
                {
                    yield return ProcessOpponentDrawEvent(animationEvent);
                }
            }
            else
            {
                yield return AnimationPlayer.PlayAnimation(animationEvent);
            }

            // 动画完成后再应用事件后的状态；增量刷新会继续复用刚才的CardView。
            RefreshSnapShot(animationEvent.StateAfterEvent);
        }

        isProcessingAnimation = false;
    }

    // 准备本方抽牌事件所需的CardView和布局终点，
    // 实际的移动与翻转仍交给ViewAnimationPlayer播放。
    private IEnumerator ProcessLocalDrawEvent(
        ClientAnimationEvent animationEvent)
    {
        CardNetworkState[] handAfterDraw =
            animationEvent.StateAfterEvent.LocalPlayerHand;
        CardNetworkState drawnCard = default;
        int drawnCardIndex = -1;

        // 不假定新牌必定处于数组末尾，直接以服务器快照中的顺序为准。
        for(int i = 0; i < handAfterDraw.Length; i++)
        {
            if(handAfterDraw[i].CardInstanceID == animationEvent.CardInstanceID)
            {
                drawnCard = handAfterDraw[i];
                drawnCardIndex = i;
                break;
            }
        }

        if(drawnCardIndex == -1)
        {
            Debug.LogError("抽牌后的手牌中找不到对应卡牌实例：" +
                animationEvent.CardInstanceID);
            yield break;
        }

        Transform handContainer = LocalPlayer.Find("Hand");
        Transform deckTransform = LocalPlayer.Find("Deck");
        HandCardLayout handLayout = handContainer.GetComponent<HandCardLayout>();

        // 这是该实例唯一的正式CardView。先注册到统一字典，后续快照不会再次创建。
        CardView cardView = GetOrCreateCardView(drawnCard, handContainer);
        cardView.SetPointerActions(null, null, false);
        cardView.SetCardBack(true);
        cardView.transform.SetSiblingIndex(drawnCardIndex);

        // 先让真实手牌布局计算一次，直接取得新牌最终应处于的位置和角度。
        // 新牌移出Hand后不再刷新布局，其他手牌会保留已经让出空位后的坐标。
        handLayout.RefreshLayout();

        RectTransform cardRect = cardView.transform as RectTransform;
        cardRect.SetParent(AnimationCanvas.transform, true);
        cardRect.SetAsLastSibling();

        // SetParent(true)保留了刚才的最终世界姿态，此时记录下AnimationCanvas坐标系中的终点。
        Vector3 targetPosition = cardRect.localPosition;
        Quaternion targetRotation = cardRect.localRotation;
        Vector3 startPosition = AnimationCanvas.transform.InverseTransformPoint(
            deckTransform.position);

        cardRect.localPosition = startPosition;
        cardRect.localRotation = Quaternion.identity;

        yield return AnimationPlayer.PlayDrawAnimation(
            cardView,
            targetPosition,
            targetRotation);
    }

    // 敌方手牌没有真实实例ID，因此创建一张匿名卡背来表示本次抽牌。
    // 动画结束后的快照刷新会继续复用这张CardView。
    private IEnumerator ProcessOpponentDrawEvent(
        ClientAnimationEvent animationEvent)
    {
        int handCountAfterDraw =
            animationEvent.StateAfterEvent.OpponentHandCount;

        // 正常情况下每个DrawCard事件只会让手牌数量增加一张。
        // 如果当前数量已经达到快照数量，直接交给最终快照校正，避免重复创建。
        if(opponentHandViews.Count >= handCountAfterDraw)
        {
            Debug.LogWarning("敌方抽牌事件没有增加手牌数量，跳过抽牌动画");
            yield break;
        }

        Transform handContainer = OpponentPlayer.Find("Hand");
        Transform deckTransform = OpponentPlayer.Find("Deck");
        HandCardLayout handLayout = handContainer.GetComponent<HandCardLayout>();

        CardView cardView = CreateOpponentHandCard(handContainer);
        cardView.transform.SetAsLastSibling();

        // 先把新卡加入敌方手牌布局，取得抽牌完成后真正的目标位置和角度。
        handLayout.RefreshLayout();

        RectTransform cardRect = cardView.transform as RectTransform;
        cardRect.SetParent(AnimationCanvas.transform, true);
        cardRect.SetAsLastSibling();

        // 保留布局计算出的终点，再把同一张匿名卡背移动到敌方牌库作为起点。
        Vector3 targetPosition = cardRect.localPosition;
        Quaternion targetRotation = cardRect.localRotation;
        Vector3 startPosition = AnimationCanvas.transform.InverseTransformPoint(
            deckTransform.position);

        cardRect.localPosition = startPosition;
        cardRect.localRotation = Quaternion.identity;

        yield return AnimationPlayer.PlayOpponentDrawAnimation(
            cardView,
            targetPosition,
            targetRotation);
    }
}

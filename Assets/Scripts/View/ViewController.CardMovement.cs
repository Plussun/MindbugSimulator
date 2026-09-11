using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 负责把不同规则事件转换成CardView的起点和终点。
// 实际逐帧移动统一交给ViewAnimationPlayer，事件完成后仍由ViewController应用快照。
public partial class ViewController
{
    private struct CardAnimationStartPose
    {
        // 卡牌被移入AnimationCanvas后，在该坐标系中的起始姿态。
        // 计算目标布局时卡牌会被临时移动，因此必须先保存这些数据。
        public CardView CardView;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
    }

    // 处理双方从手牌打出卡牌的显示事件。
    // 本方手牌可以通过真实ID直接取得；对手手牌则要先从匿名卡背转换为公开卡牌。
    private IEnumerator ProcessPlayCardToPendingEvent(
        ClientAnimationEvent animationEvent)
    {
        bool isLocalPlayer = animationEvent.PlayerID ==
            animationEvent.StateAfterEvent.LocalPlayerID;
        CardNetworkState pendingCard =
            animationEvent.StateAfterEvent.PendingCard;

        if(pendingCard.CardInstanceID == -1)
        {
            Debug.LogError("出牌事件中没有待选卡牌数据");
            yield break;
        }

        CardView cardView;
        if(isLocalPlayer)
        {
            // 本方一直知道自己的手牌身份，可以直接复用统一字典中的对象。
            if(!cardViews.TryGetValue(
                pendingCard.CardInstanceID,
                out cardView))
            {
                Debug.LogError("本方手牌中找不到要打出的CardView");
                yield break;
            }
        }
        else
        {
            // 对手手牌只保存匿名卡背，卡牌打出后才根据公开快照写入真实数据。
            if(opponentHandViews.Count == 0)
            {
                Debug.LogError("对手没有可转换的匿名手牌");
                yield break;
            }

            cardView = PromoteOpponentHandCardToVisible(pendingCard);
            // 对手卡牌从隐藏信息变为公开信息时播放翻面。
            cardView.GetComponent<Animator>().SetTrigger("Flip");
        }

        CardAnimationStartPose startPose = CaptureAnimationStart(cardView);

        // 临时放入待选区，以正式显示比例计算准确的动画终点。
        RectTransform cardRect = cardView.transform as RectTransform;
        cardRect.SetParent(PendingCardsContainer, false);
        cardRect.localPosition = Vector3.zero;
        cardRect.localRotation = Quaternion.identity;
        cardRect.localScale = Vector3.one * 0.5f;

        float arcHeight = isLocalPlayer
            ? AnimationPlayer.CardMoveArcHeight
            : -AnimationPlayer.CardMoveArcHeight;
        CardMoveAnimationTarget target =
            CaptureTargetAndRestoreStart(startPose, arcHeight);

        yield return AnimationPlayer.PlayCardMoveAnimation(
            target.CardView,
            target.TargetPosition,
            target.TargetRotation,
            target.TargetScale,
            AnimationPlayer.CardMoveDuration,
            target.ArcHeight);
    }

    // 处理卡牌进入场地的动画。
    // 普通部署从待选区开始；从弃牌区部署则从场面上的弃牌堆图标开始。
    private IEnumerator ProcessDeployCardEvent(
        ClientAnimationEvent animationEvent,
        bool fromDiscardPile)
    {
        if(!cardViews.TryGetValue(
            animationEvent.CardInstanceID,
            out CardView cardView))
        {
            Debug.LogError("找不到要部署的CardView：" +
                animationEvent.CardInstanceID);
            yield break;
        }

        bool fromLocalDiscard = cardView.transform.IsChildOf(
            DiscardPilePannel.Find("local"));
        CardAnimationStartPose startPose = CaptureAnimationStart(cardView);

        if(fromDiscardPile)
        {
            // 弃牌区面板可能处于关闭状态，因此统一从场面上的弃牌堆图标出发。
            Transform discardIcon = fromLocalDiscard
                ? LocalPlayer.Find("Discard")
                : OpponentPlayer.Find("Discard");
            DiscardPilePannel.gameObject.SetActive(false);
            startPose.Position = AnimationCanvas.transform.InverseTransformPoint(
                discardIcon.position);
            startPose.Rotation = Quaternion.identity;
            RestoreAnimationStart(startPose);
        }

        // 先按照事件完成后的快照临时排好双方场地，才能取得新卡的真实落点。
        ArrangeFieldsForSnapshot(animationEvent.StateAfterEvent);

        if(!TryGetFieldContainer(
            animationEvent.StateAfterEvent,
            animationEvent.CardInstanceID,
            out Transform destinationField))
        {
            Debug.LogError("部署后的场地中找不到卡牌：" +
                animationEvent.CardInstanceID);
            yield break;
        }

        float arcHeight = destinationField == LocalPlayer.Find("Field")
            ? AnimationPlayer.CardMoveArcHeight
            : -AnimationPlayer.CardMoveArcHeight;
        CardMoveAnimationTarget target =
            CaptureTargetAndRestoreStart(startPose, arcHeight);

        yield return AnimationPlayer.PlayCardMoveAnimation(
            target.CardView,
            target.TargetPosition,
            target.TargetRotation,
            target.TargetScale,
            AnimationPlayer.CardMoveDuration,
            target.ArcHeight);
    }

    // 处理单张手牌进入弃牌堆。
    // 弃牌区面板不一定打开，因此动画终点统一使用场面上的弃牌堆图标。
    private IEnumerator ProcessDiscardCardEvent(
        ClientAnimationEvent animationEvent)
    {
        bool isLocalPlayer = animationEvent.PlayerID ==
            animationEvent.StateAfterEvent.LocalPlayerID;
        CardView cardView;

        if(isLocalPlayer)
        {
            // 本方弃牌在客户端一直是公开对象，可以直接从cardViews取得。
            if(!cardViews.TryGetValue(
                animationEvent.CardInstanceID,
                out cardView))
            {
                Debug.LogError("本方手牌中找不到要弃掉的CardView");
                yield break;
            }
        }
        else
        {
            // 对手弃牌在离开手牌前仍是匿名对象，公开后才能加入真实ID字典。
            if(opponentHandViews.Count == 0 ||
                !TryFindCardState(
                    animationEvent.StateAfterEvent.OpponentPlayerDiscard,
                    animationEvent.CardInstanceID,
                    out CardNetworkState discardedCard))
            {
                Debug.LogError("无法把对手匿名手牌转换为弃牌");
                yield break;
            }

            cardView = PromoteOpponentHandCardToVisible(discardedCard);
            cardView.GetComponent<Animator>().SetTrigger("Flip");
        }

        CardAnimationStartPose startPose = CaptureAnimationStart(cardView);
        Transform discardIcon = isLocalPlayer
            ? LocalPlayer.Find("Discard")
            : OpponentPlayer.Find("Discard");
        float arcHeight = isLocalPlayer
            ? AnimationPlayer.CardMoveArcHeight
            : -AnimationPlayer.CardMoveArcHeight;
        CardMoveAnimationTarget target =
            CreateDiscardTarget(startPose, discardIcon, arcHeight);

        yield return AnimationPlayer.PlayCardMoveAnimation(
            target.CardView,
            target.TargetPosition,
            target.TargetRotation,
            target.TargetScale,
            AnimationPlayer.CardMoveDuration,
            target.ArcHeight);
    }

    // 处理一张或多张生物同时被击败。
    // 所有目标会在同一个协程中移动，全部完成后外层才应用一次最终快照。
    private IEnumerator ProcessDefeatCardsEvent(
        ClientAnimationEvent animationEvent)
    {
        List<CardMoveAnimationTarget> targets =
            new List<CardMoveAnimationTarget>();

        foreach(int cardInstanceID in animationEvent.CardInstanceIDs)
        {
            if(!cardViews.TryGetValue(cardInstanceID, out CardView cardView))
            {
                continue;
            }

            // 卡牌已经不在最终场地快照中，因此根据它出现在哪一方弃牌区判断归属。
            bool isLocalCard = TryFindCardState(
                animationEvent.StateAfterEvent.LocalPlayerDiscard,
                cardInstanceID,
                out _);
            bool isOpponentCard = TryFindCardState(
                animationEvent.StateAfterEvent.OpponentPlayerDiscard,
                cardInstanceID,
                out _);
            if(!isLocalCard && !isOpponentCard)
            {
                continue;
            }

            CardAnimationStartPose startPose = CaptureAnimationStart(cardView);
            Transform discardIcon = isLocalCard
                ? LocalPlayer.Find("Discard")
                : OpponentPlayer.Find("Discard");
            float arcHeight = isLocalCard
                ? AnimationPlayer.CardMoveArcHeight
                : -AnimationPlayer.CardMoveArcHeight;
            targets.Add(CreateDiscardTarget(
                startPose,
                discardIcon,
                arcHeight));
        }

        yield return AnimationPlayer.PlayCardMoveAnimations(
            targets.ToArray(),
            AnimationPlayer.CardMoveDuration);
    }

    // 处理一张或多张场上卡牌转移控制权。
    // 卡牌始终公开，只需记录原场地姿态并计算新控制者场地中的目标姿态。
    private IEnumerator ProcessTakeControlCardsEvent(
        ClientAnimationEvent animationEvent)
    {
        Dictionary<int, CardAnimationStartPose> startPoses =
            CaptureVisibleCardStarts(animationEvent.CardInstanceIDs);

        // 批量卡牌必须一起加入目标场地后再排版，否则各自计算出的落点会互相重叠。
        ArrangeFieldsForSnapshot(animationEvent.StateAfterEvent);
        CardMoveAnimationTarget[] targets = CreateFieldTargets(
            animationEvent.CardInstanceIDs,
            startPoses,
            animationEvent.StateAfterEvent);

        yield return AnimationPlayer.PlayCardMoveAnimations(
            targets,
            AnimationPlayer.CardMoveDuration);
    }

    // 同一个偷牌事件在双方客户端具有相反的可见性变化：
    // 偷牌者看到“匿名卡背变为本方公开手牌”，被偷者看到“本方公开手牌变为匿名卡背”。
    private IEnumerator ProcessStealHandCardsEvent(
        ClientAnimationEvent animationEvent)
    {
        bool isLocalPlayerStealing = animationEvent.PlayerID ==
            animationEvent.StateAfterEvent.LocalPlayerID;
        Dictionary<int, CardAnimationStartPose> startPoses =
            new Dictionary<int, CardAnimationStartPose>();

        foreach(int cardInstanceID in animationEvent.CardInstanceIDs)
        {
            CardView cardView;
            if(isLocalPlayerStealing)
            {
                // 服务器只在偷牌完成后的本方手牌快照中公开被偷卡牌的数据。
                if(opponentHandViews.Count == 0 ||
                    !TryFindCardState(
                        animationEvent.StateAfterEvent.LocalPlayerHand,
                        cardInstanceID,
                        out CardNetworkState stolenCard))
                {
                    continue;
                }

                cardView = PromoteOpponentHandCardToVisible(stolenCard);
                cardView.GetComponent<Animator>().SetTrigger("Flip");
                startPoses.Add(cardInstanceID, CaptureAnimationStart(cardView));
            }
            else
            {
                // 被偷者原本知道这张牌，但它进入对手手牌后必须从公开字典中移除。
                if(!cardViews.TryGetValue(cardInstanceID, out cardView))
                {
                    continue;
                }

                CardAnimationStartPose startPose =
                    CaptureAnimationStart(cardView);
                ConvertVisibleCardToOpponentHand(cardInstanceID);
                startPoses.Add(cardInstanceID, startPose);
            }
        }

        // 身份转换全部完成后，再一次性排好接收方的最终手牌布局。
        if(isLocalPlayerStealing)
        {
            ArrangeLocalHandForSnapshot(
                animationEvent.StateAfterEvent.LocalPlayerHand);
        }
        else
        {
            ArrangeOpponentHandForAnimation();
        }

        List<CardMoveAnimationTarget> targets =
            new List<CardMoveAnimationTarget>();
        foreach(int cardInstanceID in animationEvent.CardInstanceIDs)
        {
            if(!startPoses.TryGetValue(
                cardInstanceID,
                out CardAnimationStartPose startPose))
            {
                continue;
            }

            float arcHeight = isLocalPlayerStealing
                ? AnimationPlayer.CardMoveArcHeight
                : -AnimationPlayer.CardMoveArcHeight;
            targets.Add(CaptureTargetAndRestoreStart(
                startPose,
                arcHeight));
        }

        yield return AnimationPlayer.PlayCardMoveAnimations(
            targets.ToArray(),
            AnimationPlayer.CardMoveDuration);
    }

    // 处理弃牌堆中的全部卡牌回到手牌。
    // 自己拿回时卡牌保持公开；对手拿回时，这些牌要重新转成匿名卡背。
    private IEnumerator ProcessReturnDiscardPileToHandEvent(
        ClientAnimationEvent animationEvent)
    {
        bool isLocalPlayer = animationEvent.PlayerID ==
            animationEvent.StateAfterEvent.LocalPlayerID;
        Transform discardIcon = isLocalPlayer
            ? LocalPlayer.Find("Discard")
            : OpponentPlayer.Find("Discard");
        Dictionary<int, CardAnimationStartPose> startPoses =
            new Dictionary<int, CardAnimationStartPose>();

        // 关闭详情面板后，所有卡牌统一从场面上的弃牌堆图标飞出。
        DiscardPilePannel.gameObject.SetActive(false);

        foreach(int cardInstanceID in animationEvent.CardInstanceIDs)
        {
            if(!cardViews.TryGetValue(cardInstanceID, out CardView cardView))
            {
                continue;
            }

            CardAnimationStartPose startPose = CaptureAnimationStart(cardView);
            startPose.Position = AnimationCanvas.transform.InverseTransformPoint(
                discardIcon.position);
            startPose.Rotation = Quaternion.identity;
            RestoreAnimationStart(startPose);

            if(!isLocalPlayer)
            {
                // 对手拿回弃牌后，这些公开卡牌重新成为本客户端不可见的匿名手牌。
                ConvertVisibleCardToOpponentHand(cardInstanceID);
            }

            startPoses.Add(cardInstanceID, startPose);
        }

        // 卡牌完成公开/匿名转换后，根据接收方最终手牌数量一次性计算落点。
        if(isLocalPlayer)
        {
            ArrangeLocalHandForSnapshot(
                animationEvent.StateAfterEvent.LocalPlayerHand);
        }
        else
        {
            ArrangeOpponentHandForAnimation();
        }

        List<CardMoveAnimationTarget> targets =
            new List<CardMoveAnimationTarget>();
        foreach(int cardInstanceID in animationEvent.CardInstanceIDs)
        {
            if(!startPoses.TryGetValue(
                cardInstanceID,
                out CardAnimationStartPose startPose))
            {
                continue;
            }

            float arcHeight = isLocalPlayer
                ? AnimationPlayer.CardMoveArcHeight
                : -AnimationPlayer.CardMoveArcHeight;
            targets.Add(CaptureTargetAndRestoreStart(
                startPose,
                arcHeight));
        }

        yield return AnimationPlayer.PlayCardMoveAnimations(
            targets.ToArray(),
            AnimationPlayer.CardMoveDuration);
    }

    // 将卡牌移到动画层并保存其当前世界显示转换后的本地姿态。
    // SetParent(true)保证换父物体时卡牌不会在画面中跳动。
    private CardAnimationStartPose CaptureAnimationStart(CardView cardView)
    {
        RectTransform cardRect = cardView.transform as RectTransform;
        cardRect.SetParent(AnimationCanvas.transform, true);
        cardRect.SetAsLastSibling();

        return new CardAnimationStartPose
        {
            CardView = cardView,
            Position = cardRect.localPosition,
            Rotation = cardRect.localRotation,
            Scale = cardRect.localScale
        };
    }

    // 临时计算目标布局后，将卡牌重新放回AnimationCanvas中的动画起点。
    private void RestoreAnimationStart(CardAnimationStartPose startPose)
    {
        RectTransform cardRect = startPose.CardView.transform as RectTransform;
        cardRect.SetParent(AnimationCanvas.transform, false);
        cardRect.localPosition = startPose.Position;
        cardRect.localRotation = startPose.Rotation;
        cardRect.localScale = startPose.Scale;
        cardRect.SetAsLastSibling();
    }

    // 卡牌已经被临时放入目标容器并完成布局后，记录终点并恢复动画起点。
    private CardMoveAnimationTarget CaptureTargetAndRestoreStart(
        CardAnimationStartPose startPose,
        float arcHeight)
    {
        RectTransform cardRect = startPose.CardView.transform as RectTransform;
        cardRect.SetParent(AnimationCanvas.transform, true);

        CardMoveAnimationTarget target = new CardMoveAnimationTarget(
            startPose.CardView,
            cardRect.localPosition,
            cardRect.localRotation,
            cardRect.localScale,
            arcHeight);

        RestoreAnimationStart(startPose);
        return target;
    }

    private CardMoveAnimationTarget CreateDiscardTarget(
        CardAnimationStartPose startPose,
        Transform discardIcon,
        float arcHeight)
    {
        // 弃牌动画不需要真的把CardView放入弃牌区面板；
        // 动画先飞向弃牌堆图标，之后的快照刷新再把对象放进实际弃牌列表。
        return new CardMoveAnimationTarget(
            startPose.CardView,
            AnimationCanvas.transform.InverseTransformPoint(
                discardIcon.position),
            Quaternion.identity,
            startPose.Scale * AnimationPlayer.DiscardTargetScaleMultiplier,
            arcHeight);
    }

    // 批量移动前先保存每张公开卡牌的起点，随后可以安全地重排目标区域。
    private Dictionary<int, CardAnimationStartPose> CaptureVisibleCardStarts(
        int[] cardInstanceIDs)
    {
        Dictionary<int, CardAnimationStartPose> startPoses =
            new Dictionary<int, CardAnimationStartPose>();

        foreach(int cardInstanceID in cardInstanceIDs)
        {
            if(cardViews.TryGetValue(cardInstanceID, out CardView cardView))
            {
                startPoses.Add(cardInstanceID, CaptureAnimationStart(cardView));
            }
        }

        return startPoses;
    }

    // 按事件完成后的快照临时重建双方场地顺序，只用于计算动画落点。
    // 真正的数据显示更新仍在动画结束后的RefreshSnapShot中完成。
    private void ArrangeFieldsForSnapshot(ClientGameStateSnapshot snapshot)
    {
        ArrangeFieldCards(snapshot.LocalPlayerField, LocalPlayer.Find("Field"));
        ArrangeFieldCards(
            snapshot.OpponentPlayerField,
            OpponentPlayer.Find("Field"));

        LocalPlayer.Find("Field").GetComponent<FieldCardLayout>().RefreshLayout();
        OpponentPlayer.Find("Field").GetComponent<FieldCardLayout>().RefreshLayout();
    }

    // 将已经存在的CardView放入快照指定的场地和顺序中。
    private void ArrangeFieldCards(
        CardNetworkState[] cards,
        Transform fieldContainer)
    {
        for(int i = 0; i < cards.Length; i++)
        {
            if(!cardViews.TryGetValue(
                cards[i].CardInstanceID,
                out CardView cardView))
            {
                continue;
            }

            cardView.transform.SetParent(fieldContainer, false);
            cardView.transform.SetSiblingIndex(i);
            cardView.transform.localRotation = cards[i].isExhausted
                ? Quaternion.Euler(0, 0, 90)
                : Quaternion.identity;
        }
    }

    // 目标场地完成布局后，逐张记录落点并恢复各自的动画起点。
    private CardMoveAnimationTarget[] CreateFieldTargets(
        int[] cardInstanceIDs,
        Dictionary<int, CardAnimationStartPose> startPoses,
        ClientGameStateSnapshot snapshot)
    {
        List<CardMoveAnimationTarget> targets =
            new List<CardMoveAnimationTarget>();

        foreach(int cardInstanceID in cardInstanceIDs)
        {
            if(!startPoses.TryGetValue(
                cardInstanceID,
                out CardAnimationStartPose startPose) ||
                !TryGetFieldContainer(
                    snapshot,
                    cardInstanceID,
                    out Transform destinationField))
            {
                continue;
            }

            float arcHeight = destinationField == LocalPlayer.Find("Field")
                ? AnimationPlayer.CardMoveArcHeight
                : -AnimationPlayer.CardMoveArcHeight;
            targets.Add(CaptureTargetAndRestoreStart(
                startPose,
                arcHeight));
        }

        return targets.ToArray();
    }

    // 偷牌或弃牌回手时，按照最终快照顺序临时排列本方手牌并计算落点。
    private void ArrangeLocalHandForSnapshot(CardNetworkState[] cards)
    {
        Transform handContainer = LocalPlayer.Find("Hand");
        for(int i = 0; i < cards.Length; i++)
        {
            if(cardViews.TryGetValue(
                cards[i].CardInstanceID,
                out CardView cardView))
            {
                cardView.transform.SetParent(handContainer, false);
                cardView.transform.SetSiblingIndex(i);
            }
        }

        handContainer.GetComponent<HandCardLayout>().RefreshLayout();
    }

    // 对手手牌没有公开数组顺序，直接按照本地匿名卡背列表进行最终排版。
    private void ArrangeOpponentHandForAnimation()
    {
        Transform handContainer = OpponentPlayer.Find("Hand");
        for(int i = 0; i < opponentHandViews.Count; i++)
        {
            opponentHandViews[i].transform.SetParent(handContainer, false);
            opponentHandViews[i].transform.SetSiblingIndex(i);
        }

        handContainer.GetComponent<HandCardLayout>().RefreshLayout();
    }

    // 根据最终快照判断一张公开卡牌落入本方还是对方场地。
    private bool TryGetFieldContainer(
        ClientGameStateSnapshot snapshot,
        int cardInstanceID,
        out Transform fieldContainer)
    {
        if(TryFindCardState(
            snapshot.LocalPlayerField,
            cardInstanceID,
            out _))
        {
            fieldContainer = LocalPlayer.Find("Field");
            return true;
        }

        if(TryFindCardState(
            snapshot.OpponentPlayerField,
            cardInstanceID,
            out _))
        {
            fieldContainer = OpponentPlayer.Find("Field");
            return true;
        }

        fieldContainer = null;
        return false;
    }

    // 在一个公开卡牌数组中按实例ID查找状态，避免各事件重复编写遍历代码。
    private bool TryFindCardState(
        CardNetworkState[] cards,
        int cardInstanceID,
        out CardNetworkState cardState)
    {
        foreach(CardNetworkState card in cards)
        {
            if(card.CardInstanceID == cardInstanceID)
            {
                cardState = card;
                return true;
            }
        }

        cardState = default;
        return false;
    }
}

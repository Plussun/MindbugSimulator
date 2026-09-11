// Unity 已经定义了 UnityEngine.AnimationEvent，
// 因此这里使用 GameAnimationEvent，避免以后引用时产生命名冲突。
[System.Serializable]
public class GameAnimationEvent
{
    public int SequenceID;
    public GameAnimationType AnimationType;

    // 与本次变化有关的玩家和卡牌。
    // StateRefresh 不对应具体对象时保持为 -1。
    public int PlayerID = -1;
    public int CardInstanceID = -1;

    // 同一次变化涉及多张卡牌时使用，例如同时击败、批量控制和偷牌。
    // 单张卡牌事件仍然使用CardInstanceID，避免简单事件也变得难读。
    public int[] CardInstanceIDs;

    // 本事件完成后的显示状态。
    // 动画播放完毕后，显示端用它刷新并校正整个界面。
    public GameStateSnapshot StateAfterEvent;

    public GameAnimationEvent(
        int sequenceID,
        GameAnimationType animationType,
        GameStateSnapshot stateAfterEvent,
        int playerID = -1,
        int cardInstanceID = -1,
        int[] cardInstanceIDs = null)
    {
        SequenceID = sequenceID;
        AnimationType = animationType;
        StateAfterEvent = stateAfterEvent;
        PlayerID = playerID;
        CardInstanceID = cardInstanceID;
        CardInstanceIDs = cardInstanceIDs ?? new int[0];
    }
}

public enum GameAnimationType
{
    // 暂时没有专用动画，只需要把显示刷新到事件完成后的状态。
    StateRefresh,
    // 卡牌从手牌进入待Mindbug决策区。
    PlayCardToPending,
    // 从牌库加入手牌。
    DrawCard,
    // 从手牌进入弃牌堆。
    DiscardCard,
    // 从待Mindbug决策区进入场地。
    DeployCard,
    // 从任意一方弃牌区直接部署到场地。
    DeployCardFromDiscard,
    // 一张或多张场地卡牌同时进入弃牌堆。
    DefeatCards,
    // 一张或多张场地卡牌改变控制者。
    TakeControlCards,
    // 一张或多张手牌在双方玩家之间转移。
    StealHandCards,
    // 一名玩家把自己的整个弃牌堆拿回手牌。
    ReturnDiscardPileToHand
}

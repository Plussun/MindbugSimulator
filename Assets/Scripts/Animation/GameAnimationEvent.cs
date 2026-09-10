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

    // 本事件完成后的显示状态。
    // 动画播放完毕后，显示端用它刷新并校正整个界面。
    public GameStateSnapshot StateAfterEvent;

    public GameAnimationEvent(
        int sequenceID,
        GameAnimationType animationType,
        GameStateSnapshot stateAfterEvent,
        int playerID = -1,
        int cardInstanceID = -1)
    {
        SequenceID = sequenceID;
        AnimationType = animationType;
        StateAfterEvent = stateAfterEvent;
        PlayerID = playerID;
        CardInstanceID = cardInstanceID;
    }
}

public enum GameAnimationType
{
    // 暂时没有专用动画，只需要把显示刷新到事件完成后的状态。
    StateRefresh,
    // 卡牌从手牌进入待Mindbug决策区。
    PlayCardToPending,
    DrawCard,
    DiscardCard
}

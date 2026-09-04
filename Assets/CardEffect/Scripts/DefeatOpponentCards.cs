using UnityEngine;

[CreateAssetMenu(fileName = "CardEffect",
menuName = "MindBug/CardEffect/DefeatOpponentCards")]
public class DefeatOpponentCards : CardEffect
{
    public int MaxCount;
    public int MinCount;
    public bool DefeatAll;

    public int MaxPower = -1; //-1表示没有最高力量限制
    public int MinPower = -1; //-1表示没有最低力量限制

    public bool CanTargetAlliedCards;
    public bool OnlyIfFewerCreatures;

    public override void Resolve(
        GameEngine gameEngine,
        int ownerPlayerID,
        CardInstance cardInstance)
    {
        gameEngine.EventQueue.EnqueueNext(
            new DefeatOpponentCardsEvent(
                ownerPlayerID,
                MinCount,
                MaxCount,
                MinPower,
                MaxPower,
                DefeatAll,
                CanTargetAlliedCards,
                OnlyIfFewerCreatures));
    }
}

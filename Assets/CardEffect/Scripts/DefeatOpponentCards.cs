using UnityEngine;

[CreateAssetMenu(fileName = "CardEffect",
menuName = "MindBug/CardEffect/DefeatOpponentCards")]
public class DefeatOpponentCards : CardEffect
{
    public int MaxCount;
    public int MinCount;
    public int MaxPower = -1; //-1表示没有最高力量限制
    public int MinPower = -1; //-1表示没有最低力量限制

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
                MaxPower));
    }
}

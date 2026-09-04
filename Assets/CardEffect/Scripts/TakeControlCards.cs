using UnityEngine;
[CreateAssetMenu(fileName = "CardEffect", 
menuName = "MindBug/CardEffect/TakeControlCards")]
public class TakeControlCards : CardEffect
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
            new TakeControlCardsEvent(
                ownerPlayerID,
                MinCount,
                MaxCount,
                MinPower,
                MaxPower));
    }
}

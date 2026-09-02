using UnityEngine;
[CreateAssetMenu(fileName = "CardEffect", 
menuName = "MindBug/CardEffect/DiscardCards")]
public class DiscardCards : CardEffect
{
    public int CardsToDiscard;
    public bool random;

    public override void Resolve(
        GameEngine gameEngine, 
        int ownerPlayerID, 
        CardInstance cardInstance)
    {
        int opponentPlayerID = 1 - ownerPlayerID;
        //测试，随机弃牌
        gameEngine.EventQueue.Enqueue(
            new DiscardHandEvent(opponentPlayerID, random, CardsToDiscard));
    }
}
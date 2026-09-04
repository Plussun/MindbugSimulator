using UnityEngine;

[CreateAssetMenu(
    fileName = "CardEffect",
    menuName = "MindBug/CardEffect/ReturnDiscardPileToHand")]
public class ReturnDiscardPileToHand : CardEffect
{
    public override void Resolve(
        GameEngine gameEngine,
        int ownerPlayerID,
        CardInstance cardInstance)
    {
        gameEngine.ReturnDiscardPileToHand(ownerPlayerID);
    }
}

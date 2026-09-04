using UnityEngine;

[CreateAssetMenu(
    fileName = "CardEffect",
    menuName = "MindBug/CardEffect/StealRandomHandCards")]
public class StealRandomHandCards : CardEffect
{
    public int CardsToSteal = 2;

    public override void Resolve(
        GameEngine gameEngine,
        int ownerPlayerID,
        CardInstance cardInstance)
    {
        gameEngine.StealRandomHandCards(ownerPlayerID, CardsToSteal);
    }
}

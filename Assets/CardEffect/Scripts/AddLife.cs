using UnityEngine;
[CreateAssetMenu(fileName = "CardEffect", 
menuName = "MindBug/CardEffect/AddLife")]
public class AddLife : CardEffect
{
    public int LifeToAdd;

    public override void Resolve(
        GameEngine gameEngine, 
        int ownerPlayerID, 
        CardInstance cardInstance)
    {
        gameEngine.LoseLife(ownerPlayerID, -LifeToAdd);
    }
}
using UnityEngine;
[CreateAssetMenu(fileName = "CardEffect", 
menuName = "MindBug/CardEffect/AddOrLoseLife")]
public class AddOrLoseLife : CardEffect
{
    public int LifeToAdd;
    public bool isOwnerPlayer = true;

    public override void Resolve(
        GameEngine gameEngine, 
        int ownerPlayerID, 
        CardInstance cardInstance)
    {
        if (isOwnerPlayer)
        {
            gameEngine.LoseLife(ownerPlayerID, -LifeToAdd);
        }
        else
        {
            gameEngine.LoseLife(1 - ownerPlayerID, -LifeToAdd);
        }
    }
}
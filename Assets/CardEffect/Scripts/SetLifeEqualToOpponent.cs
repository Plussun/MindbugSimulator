using UnityEngine;

[CreateAssetMenu(
    fileName = "CardEffect",
    menuName = "MindBug/CardEffect/SetLifeEqualToOpponent")]
public class SetLifeEqualToOpponent : CardEffect
{
    public override void Resolve(
        GameEngine gameEngine,
        int ownerPlayerID,
        CardInstance cardInstance)
    {
        int ownerLife = gameEngine.State.Players[ownerPlayerID].Life;
        int opponentLife = gameEngine.State.Players[1 - ownerPlayerID].Life;
        int lifeToLose = ownerLife - opponentLife;

        if(lifeToLose != 0)
        {
            gameEngine.LoseLife(ownerPlayerID, lifeToLose);
        }
    }
}

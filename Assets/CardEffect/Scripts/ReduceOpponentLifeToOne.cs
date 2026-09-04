using UnityEngine;

[CreateAssetMenu(
    fileName = "CardEffect",
    menuName = "MindBug/CardEffect/ReduceOpponentLifeToOne")]
public class ReduceOpponentLifeToOne : CardEffect
{
    public override void Resolve(
        GameEngine gameEngine,
        int ownerPlayerID,
        CardInstance cardInstance)
    {
        int opponentPlayerID = 1 - ownerPlayerID;
        int lifeToLose = gameEngine.State.Players[opponentPlayerID].Life - 1;

        if(lifeToLose > 0)
        {
            gameEngine.LoseLife(opponentPlayerID, lifeToLose);
        }
    }
}

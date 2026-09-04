using UnityEngine;

[CreateAssetMenu(
    fileName = "CardEffect",
    menuName = "MindBug/CardEffect/GlobalBlockPowerRestriction")]
public class GlobalBlockPowerRestriction : CardFieldEffect
{
    // 力量低于此数值的敌方生物不能进行任何阻挡。
    public int MinimumPowerToBlock;
    public override bool ResolveAfterCardUpdate => true;

    public override void Resolve(
        GameEngine gameEngine,
        int ownerPlayerID,
        CardInstance cardInstance)
    {
        int opponentPlayerID = 1 - ownerPlayerID;

        foreach(CardInstance opponentCard in gameEngine.State.Players[opponentPlayerID].Field)
        {
            if(opponentCard.CurrentPower < MinimumPowerToBlock)
            {
                gameEngine.AddGlobalBlockRestriction(
                    opponentPlayerID,
                    opponentCard.CardInstanceID);
            }
        }
    }
}

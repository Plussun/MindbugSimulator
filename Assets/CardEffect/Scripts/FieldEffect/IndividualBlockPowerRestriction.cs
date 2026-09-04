using UnityEngine;

[CreateAssetMenu(
    fileName = "CardEffect",
    menuName = "MindBug/CardEffect/IndividualBlockPowerRestriction")]
public class IndividualBlockPowerRestriction : CardFieldEffect
{
    // 力量低于此数值的敌方生物不能阻挡此效果的拥有者。
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
            if(opponentCard.CurrentPower <= MinimumPowerToBlock)
            {
                gameEngine.AddIndividualBlockRestriction(
                    opponentPlayerID,
                    cardInstance.CardInstanceID,
                    opponentCard.CardInstanceID);
            }
        }
    }
}

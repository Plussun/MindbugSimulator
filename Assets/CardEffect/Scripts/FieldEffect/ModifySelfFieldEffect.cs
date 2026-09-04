using UnityEngine;

[CreateAssetMenu(
    fileName = "CardEffect",
    menuName = "MindBug/CardEffect/ModifySelfFieldEffect")]
public class ModifySelfFieldEffect : CardFieldEffect
{
    public bool OnlyOnOwnerTurn;
    public bool OnlyAlliedCreature;

    public int PowerToAdd;
    public Keywords KeywordsToAdd;

    public override void Resolve(
        GameEngine gameEngine,
        int ownerPlayerID,
        CardInstance cardInstance)
    {
        if(OnlyOnOwnerTurn && gameEngine.State.ActivePlayerID != ownerPlayerID)
        {
            return;
        }

        if(OnlyAlliedCreature &&
            gameEngine.State.Players[ownerPlayerID].Field.Count != 1)
        {
            return;
        }

        cardInstance.TempPower += PowerToAdd;
        cardInstance.TempKeywords |= KeywordsToAdd;
    }
}

using UnityEngine;

[CreateAssetMenu(
    fileName = "CardEffect",
    menuName = "MindBug/CardEffect/DisableOpponentEffectTrigger")]
public class DisableOpponentEffectTrigger : CardFieldEffect
{
    public EffectTrigger TriggerToDisable;

    public override void Resolve(
        GameEngine gameEngine,
        int ownerPlayerID,
        CardInstance cardInstance)
    {
        int opponentPlayerID = 1 - ownerPlayerID;
        PlayerState opponent = gameEngine.State.Players[opponentPlayerID];

        if(!opponent.DisabledEffectTriggers.Contains(TriggerToDisable))
        {
            opponent.DisabledEffectTriggers.Add(TriggerToDisable);
        }
    }
}

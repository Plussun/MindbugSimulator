using UnityEngine;
public class AfterCombatEvent : GameEvent
{


    public AfterCombatEvent()
    {

    }

    public override void Resolve(GameEngine gameEngine)
    {
        gameEngine.AfterCombat();
    }
}
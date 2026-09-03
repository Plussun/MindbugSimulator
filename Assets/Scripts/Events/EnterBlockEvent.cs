using System.Collections.Generic;
using UnityEngine;

public class EnterBlockEvent : GameEvent
{

    public int AttackerPlayerID;
    public int AttackCardInstanceID;
    public int TargetCardInstanceID;
    public EnterBlockEvent(int playerID, int cardInstanceID, int targetCardInstanceID)
    {
        AttackerPlayerID = playerID;
        AttackCardInstanceID = cardInstanceID;
        TargetCardInstanceID = targetCardInstanceID;
    }

    public override void Resolve(GameEngine gameEngine)
    {   
        gameEngine.BeginAttack(AttackerPlayerID, AttackCardInstanceID, TargetCardInstanceID);
        
    }


}
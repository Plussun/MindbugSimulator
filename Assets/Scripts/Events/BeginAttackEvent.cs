using System.Collections.Generic;
using UnityEngine;

public class BeginAttackEvent : GameEvent
{

    public int AttackerPlayerID;
    public int AttackCardInstanceID;
    public int TargetCardInstanceID;
    public BeginAttackEvent(int playerID, int cardInstanceID, int targetCardInstanceID)
    {
        AttackerPlayerID = playerID;
        AttackCardInstanceID = cardInstanceID;
        TargetCardInstanceID = targetCardInstanceID;
    }

    public override void Resolve(GameEngine gameEngine)
    {   
        Debug.Log("开始攻击事件触发了");
        gameEngine.BeginAttack(AttackerPlayerID, AttackCardInstanceID, TargetCardInstanceID);
        
    }


}
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "CardEffect", 
menuName = "MindBug/CardEffect/DeployACardFromDiscardPile")]
public class DeployACardFromDiscardPile : CardEffect
{
    public bool fromLocalDiscardPile; // true表示从本地玩家的弃牌堆中部署，false表示从对手的弃牌堆中部署
    public override void Resolve(
        GameEngine gameEngine, 
        int ownerPlayerID, 
        CardInstance cardInstance)
    {
        gameEngine.EventQueue.EnqueueNext(new DeployACardFromDiscardPileEvent(ownerPlayerID, fromLocalDiscardPile));
        
    }
}
using System.Collections.Generic;   
using UnityEngine;
[CreateAssetMenu(fileName = "CardEffect", 
menuName = "MindBug/CardEffect/AddPowerToAllFriend")]
public class AddPowerToAllFriend : CardFieldEffect
{
    public int PowerToAdd;

    public override void Resolve(
        GameEngine gameEngine, 
        int ownerPlayerID, 
        CardInstance cardInstance)
    {
        List<CardInstance> playerField = 
            gameEngine.State.Players[ownerPlayerID].Field;
        foreach(CardInstance card in playerField)
        {
            if(card == cardInstance)
            {
                //只给所有友方卡牌加buff，不包括自己
                continue;
            }
            card.TempPower += PowerToAdd;
        }

    }
}
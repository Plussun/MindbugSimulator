using UnityEngine;
using System.Collections.Generic;
public class DefeatEvent : GameEvent
{
    public int PlayerID;
    public int CardInstanceID;

    public DefeatEvent(int playerID, int cardInstanceID)
    {
        PlayerID = playerID;
        CardInstanceID = cardInstanceID;
    }

    public override void Resolve(GameEngine gameEngine)
    {
        CardInstance cardInstance = gameEngine.GetCardInstanceByID(CardInstanceID);
        if (cardInstance == null)
        {
            Debug.LogError("找不到卡牌实例");
            return; 
        }
        //Tough抵消本次击败时，不触发OnDefeat效果
        if(!gameEngine.DefeatCard(PlayerID, CardInstanceID))
        {
            return;
        }

        List<GameEvent> eventsToEnqueue = new List<GameEvent>();
        //加入OnDefeat事件处理，触发卡牌的OnDefeat效果
        foreach(var effect in cardInstance.CardData.CardEffects)
        {
            if(effect.Trigger == EffectTrigger.OnDefeat)
            {
                eventsToEnqueue.Add(new CardEffectEvent(effect, PlayerID, CardInstanceID));
            }
        }
        gameEngine.EventQueue.EqueueNextRange(eventsToEnqueue);
    }
}

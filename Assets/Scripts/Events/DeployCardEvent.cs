using UnityEngine;
using System.Collections.Generic;
public class DeployCardEvent : GameEvent
{
    public int PlayerID;
    public int CardInstanceID;

    public DeployCardEvent(int playerID, int cardInstanceID)
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
        List<GameEvent> eventsToEnqueue = new List<GameEvent>();
        //加入OnDeploy事件处理，触发卡牌的OnDeploy效果
        foreach(var effect in cardInstance.CardData.CardEffects)
        {
            if(effect.Trigger == EffectTrigger.OnDeploy)
            {
                eventsToEnqueue.Add(new CardEffectEvent(effect, PlayerID, CardInstanceID));
            }
        }
        gameEngine.EventQueue.EqueueNextRange(eventsToEnqueue);
        gameEngine.DeployCard(PlayerID, cardInstance);
    }
}
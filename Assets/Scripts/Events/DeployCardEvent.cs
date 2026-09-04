using UnityEngine;
using System.Collections.Generic;
public class DeployCardEvent : GameEvent
{
    public int PlayerID;
    public CardInstance CardInstanceToDeploy;

    public DeployCardEvent(int playerID, CardInstance cardInstance)
    {
        PlayerID = playerID;
        CardInstanceToDeploy = cardInstance;
    }

    public override void Resolve(GameEngine gameEngine)
    {
        if (CardInstanceToDeploy == null || CardInstanceToDeploy.CardInstanceID == -1)
        {
            Debug.LogError("找不到卡牌实例");
            return; 
        }
        List<GameEvent> eventsToEnqueue = new List<GameEvent>();
        //加入OnDeploy事件处理，触发卡牌的OnDeploy效果
        if(gameEngine.CanTriggerEffect(PlayerID, EffectTrigger.OnDeploy))
        {
            foreach(var effect in CardInstanceToDeploy.CardData.CardEffects)
            {
                if(effect.Trigger == EffectTrigger.OnDeploy)
                {
                    eventsToEnqueue.Add(new CardEffectEvent(
                        effect,
                        PlayerID,
                        CardInstanceToDeploy.CardInstanceID));
                }
            }
        }
        gameEngine.EventQueue.EqueueNextRange(eventsToEnqueue);
        gameEngine.DeployCard(PlayerID, CardInstanceToDeploy);
    }
}

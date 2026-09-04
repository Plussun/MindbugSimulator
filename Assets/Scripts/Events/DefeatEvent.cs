using UnityEngine;
using System.Collections.Generic;
public class DefeatEvent : GameEvent
{
    public List<(int playerID, int cardInstanceID)> Targets;

    //单张卡牌被击败
    public DefeatEvent(int playerID, int cardInstanceID)
    {
        Targets = new List<(int, int)>
        {
            (playerID, cardInstanceID)
        };
    }

    //多张卡牌同时被击败
    public DefeatEvent(List<(int playerID, int cardInstanceID)> targets)
    {
        Targets = targets;
    }

    public override void Resolve(GameEngine gameEngine)
    {
        //先处理所有卡牌的离场，再触发真正阵亡卡牌的OnDefeat效果
        List<(int playerID, CardInstance card)> defeatedCards =
            gameEngine.DefeatCard(Targets);

        List<GameEvent> eventsToEnqueue = new List<GameEvent>();
        foreach(var defeatedCard in defeatedCards)
        {
            if(defeatedCard.card.CardData.CardEffects == null)
            {
                continue;
            }
            if(!gameEngine.CanTriggerEffect(
                defeatedCard.playerID,
                EffectTrigger.OnDefeat))
            {
                continue;
            }

            foreach(var effect in defeatedCard.card.CardData.CardEffects)
            {
                if(effect.Trigger == EffectTrigger.OnDefeat)
                {
                    eventsToEnqueue.Add(new CardEffectEvent(
                        effect,
                        defeatedCard.playerID,
                        defeatedCard.card.CardInstanceID));
                }
            }
        }
        gameEngine.EventQueue.EqueueNextRange(eventsToEnqueue);
    }
}

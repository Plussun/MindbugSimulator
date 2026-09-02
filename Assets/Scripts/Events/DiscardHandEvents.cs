using UnityEngine;
using System.Collections.Generic;
public class DiscardHandEvent : GameEvent
{
    public int PlayerID;
    public bool RandomDiscard;
    public int Count;

    public DiscardHandEvent(int playerID, bool randomDiscard, int count)
    {
        PlayerID = playerID;
        RandomDiscard = randomDiscard;
        Count = count;
    }

    public override void Resolve(GameEngine gameEngine)
    {
        if (!RandomDiscard)
        {
            //进入对手选择弃牌
            List<int> candidateCardInstanceIDs = 
                gameEngine.State.Players[PlayerID].Hand.ConvertAll(c => c.CardInstanceID);
            if(candidateCardInstanceIDs.Count == 0)
            {
                Debug.Log("没有可弃的卡牌");
                return;
            }
            gameEngine.EventQueue.RequestChoice(new PendingChoice
            {
                PlayerID = PlayerID,
                MaxSelectCount = Count,
                MinSelectCount = Count<candidateCardInstanceIDs.Count?
                    Count:candidateCardInstanceIDs.Count,
                CandidateCardInstanceIDs = candidateCardInstanceIDs
            }, gameEngine, this);
            return;
        }
        else
        {
            //随机弃牌
            PlayerState player = gameEngine.State.Players[PlayerID];
            int discardCount = Mathf.Min(Count, player.Hand.Count);
            for (int i = 0; i < discardCount; i++)
            {
                int randomIndex = Random.Range(0, player.Hand.Count);
                CardInstance cardToDiscard = player.Hand[randomIndex];
                //TODO:也要改进为事件队列
                gameEngine.DiscardCard(PlayerID, cardToDiscard.CardInstanceID);
                gameEngine.Refill(PlayerID);
            }
            gameEngine.Refill(PlayerID);
        }
    }

    public override void ResolveChoice(GameEngine gameEngine, List<int> selectedCardInstanceIDs)
    {
        foreach (var cardInstanceID in selectedCardInstanceIDs)
        {
            gameEngine.DiscardCard(PlayerID, cardInstanceID);
            gameEngine.Refill(PlayerID);
        }
        gameEngine.Refill(PlayerID);
    }
}
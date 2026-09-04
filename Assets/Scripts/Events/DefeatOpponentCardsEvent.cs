using UnityEngine;
using System.Collections.Generic;

public class DefeatOpponentCardsEvent : GameEvent
{
    public int PlayerID;
    public int MinCount;
    public int MaxCount;
    public int MinPower;
    public int MaxPower;

    public DefeatOpponentCardsEvent(
        int playerID,
        int minCount,
        int maxCount,
        int minPower,
        int maxPower)
    {
        PlayerID = playerID;
        MinCount = minCount;
        MaxCount = maxCount;
        MinPower = minPower;
        MaxPower = maxPower;
    }

    public override void Resolve(GameEngine gameEngine)
    {
        int opponentPlayerID = 1 - PlayerID;
        List<int> candidateCardInstanceIDs = new List<int>();

        foreach(var card in gameEngine.State.Players[opponentPlayerID].Field)
        {
            if(MinPower != -1 && card.CurrentPower < MinPower)
            {
                continue;
            }
            if(MaxPower != -1 && card.CurrentPower > MaxPower)
            {
                continue;
            }
            candidateCardInstanceIDs.Add(card.CardInstanceID);
        }

        if(candidateCardInstanceIDs.Count == 0)
        {
            Debug.Log("对手场上没有符合条件的生物");
            return;
        }

        gameEngine.EventQueue.RequestChoice(new PendingChoice
        {
            PlayerID = PlayerID,
            MinSelectCount = Mathf.Min(MinCount, candidateCardInstanceIDs.Count),
            MaxSelectCount = Mathf.Min(MaxCount, candidateCardInstanceIDs.Count),
            CandidateCardInstanceIDs = candidateCardInstanceIDs
        }, gameEngine, this);
    }

    public override void ResolveChoice(
        GameEngine gameEngine,
        List<int> selectedCardInstanceIDs)
    {
        int opponentPlayerID = 1 - PlayerID;
        List<(int playerID, int cardInstanceID)> defeatTargets =
            new List<(int, int)>();

        foreach(var cardInstanceID in selectedCardInstanceIDs)
        {
            defeatTargets.Add((opponentPlayerID, cardInstanceID));
        }

        gameEngine.EventQueue.EnqueueNext(new DefeatEvent(defeatTargets));
    }
}

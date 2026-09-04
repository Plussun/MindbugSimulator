using UnityEngine;
using System.Collections.Generic;

public class DefeatOpponentCardsEvent : GameEvent
{
    public int PlayerID;
    public int MinCount;
    public int MaxCount;
    public int MinPower;
    public int MaxPower;
    public bool DefeatAll;
    public bool CanTargetAlliedCards;
    public bool OnlyIfFewerCreatures;

    public List<(int playerID, int cardInstanceID)> CandidateTargets;

    public DefeatOpponentCardsEvent(
        int playerID,
        int minCount,
        int maxCount,
        int minPower,
        int maxPower,
        bool defeatAll,
        bool canTargetAlliedCards,
        bool onlyIfFewerCreatures)
    {
        PlayerID = playerID;
        MinCount = minCount;
        MaxCount = maxCount;
        MinPower = minPower;
        MaxPower = maxPower;
        DefeatAll = defeatAll;
        CanTargetAlliedCards = canTargetAlliedCards;
        OnlyIfFewerCreatures = onlyIfFewerCreatures;
        CandidateTargets = new List<(int, int)>();
    }

    public override void Resolve(GameEngine gameEngine)
    {
        int opponentPlayerID = 1 - PlayerID;

        if(OnlyIfFewerCreatures &&
            gameEngine.State.Players[PlayerID].Field.Count >=
            gameEngine.State.Players[opponentPlayerID].Field.Count)
        {
            Debug.Log("己方生物数量不少于对手，击败效果不生效");
            return;
        }

        CandidateTargets.Clear();
        List<int> candidateCardInstanceIDs = new List<int>();

        foreach(var player in gameEngine.State.Players)
        {
            if(player.PlayerID == PlayerID && !CanTargetAlliedCards)
            {
                continue;
            }

            foreach(var card in player.Field)
            {
                if(MinPower != -1 && card.CurrentPower < MinPower)
                {
                    continue;
                }
                if(MaxPower != -1 && card.CurrentPower > MaxPower)
                {
                    continue;
                }

                CandidateTargets.Add((player.PlayerID, card.CardInstanceID));
                candidateCardInstanceIDs.Add(card.CardInstanceID);
            }
        }

        if(candidateCardInstanceIDs.Count == 0)
        {
            Debug.Log("场上没有符合条件的生物");
            return;
        }

        if(DefeatAll)
        {
            gameEngine.EventQueue.EnqueueNext(
                new DefeatEvent(new List<(int, int)>(CandidateTargets)));
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
        List<(int playerID, int cardInstanceID)> defeatTargets =
            new List<(int, int)>();

        foreach(var cardInstanceID in selectedCardInstanceIDs)
        {
            foreach(var candidateTarget in CandidateTargets)
            {
                if(candidateTarget.cardInstanceID == cardInstanceID)
                {
                    defeatTargets.Add(candidateTarget);
                    break;
                }
            }
        }

        gameEngine.EventQueue.EnqueueNext(new DefeatEvent(defeatTargets));
    }
}

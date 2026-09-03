using UnityEngine;
using System.Collections.Generic;
public class DeployACardFromDiscardPileEvent : GameEvent
{
    public int PlayerID;
    public bool fromLocalDiscardPile; // true表示从本地玩家的弃牌堆中部署，false表示从对手的弃牌堆中部署


    public DeployACardFromDiscardPileEvent(int playerID, bool fromLocalDiscardPile)
    {
        PlayerID = playerID;
        this.fromLocalDiscardPile = fromLocalDiscardPile;

    }

    public override void Resolve(GameEngine gameEngine)
    {
        List<int> CandidateCardInstanceIDs = new List<int>();
        if (fromLocalDiscardPile)
        {
            foreach (var card in gameEngine.State.Players[PlayerID].DiscardPile)
            {
                CandidateCardInstanceIDs.Add(card.CardInstanceID);
            }
        }
        else
        {
            int opponentPlayerID = 1 - PlayerID;
            foreach (var card in gameEngine.State.Players[opponentPlayerID].DiscardPile)
            {
                CandidateCardInstanceIDs.Add(card.CardInstanceID);
            }
        }
        if (CandidateCardInstanceIDs.Count == 0)
        {
            Debug.Log("没有可部署的卡牌");
            return;
        }
        gameEngine.EventQueue.RequestChoice(new PendingChoice
        {
            PlayerID = PlayerID,
            MaxSelectCount = 1,
            MinSelectCount = 1,
            CandidateCardInstanceIDs = CandidateCardInstanceIDs
        }, gameEngine, this);

    }

    public override void ResolveChoice(GameEngine gameEngine, List<int> selectedCardInstanceIDs)
    {
        if (selectedCardInstanceIDs.Count == 0)
        {
            Debug.Log("未选择卡牌");
            return;
        }
        int discardOwnerPlayerID = fromLocalDiscardPile ? PlayerID : 1 - PlayerID;
        PlayerState discardOwner = gameEngine.State.Players[discardOwnerPlayerID];
        CardInstance cardInstanceToDeploy = discardOwner.DiscardPile.Find(
            card => card.CardInstanceID == selectedCardInstanceIDs[0]);
        if(cardInstanceToDeploy == null)
        {
            Debug.Log("弃牌堆中没有找到要部署的卡牌");
            return;
        }

        discardOwner.DiscardPile.Remove(cardInstanceToDeploy);
        gameEngine.DeployCard(PlayerID, cardInstanceToDeploy);
    }
}

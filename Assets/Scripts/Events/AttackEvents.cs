using System.Collections.Generic;
using UnityEngine;

public class AttackEvent : GameEvent
{

    public int AttackerPlayerID;
    public int AttackCardInstanceID;
    public AttackEvent(int playerID, int cardInstanceID)
    {
        AttackerPlayerID = playerID;
        AttackCardInstanceID = cardInstanceID;
    }

    public override void Resolve(GameEngine gameEngine)
    {   //TODO:从gameEngine中获得攻击卡牌示例
        CardInstance attackCard = gameEngine.GetCardInstanceByID(AttackCardInstanceID);

        if(attackCard == null)
        {
            Debug.LogError("找不到攻击卡牌");
            return;
        }
        //TODO:根据卡牌示例的keyword，决定是否要选择目标，如果需要
        //则调用RequestChoice方法，把该事件暂存于WaitingEvent中，
        // 并把游戏状态改为WaitingForChoice，等待玩家选择目标(由requestchoice方法来处理)
        if(attackCard.CurrentKeywords.HasFlag(Keywords.Hunter))
        {
            List<int> candidateCardInstanceIDs = gameEngine.State.Players[1 - AttackerPlayerID].Field.ConvertAll(c => c.CardInstanceID);
            if(candidateCardInstanceIDs.Count == 0)
            {
                //如果没有可选目标，直接调用gameEngine.BeginAttack方法，开始攻击流程
                gameEngine.BeginAttack(AttackerPlayerID, AttackCardInstanceID, -1);
                return;
            }
            gameEngine.EventQueue.RequestChoice(new PendingChoice
            {
                PlayerID = AttackerPlayerID,
                MaxSelectCount = 1,
                MinSelectCount = 0,
                CandidateCardInstanceIDs = candidateCardInstanceIDs
            }, gameEngine, this);
            return;
        }

        //TODO:如果不需要选择目标，直接调用gameEngine.AttackDecision方法，开始攻击流程
        gameEngine.BeginAttack(AttackerPlayerID, AttackCardInstanceID, -1);
        
    }

    public override void ResolveChoice(GameEngine gameEngine, 
        List<int> selectedCardInstanceIDs)
    {
        //TODO:调用gameEngine.AttackDecision方法，并传入阻挡卡牌ID
        //开始攻击流程
        int targetCardInstanceID = selectedCardInstanceIDs.Count > 0 ? selectedCardInstanceIDs[0] : -1;
        gameEngine.BeginAttack(AttackerPlayerID, AttackCardInstanceID, 
            targetCardInstanceID);
    }
}
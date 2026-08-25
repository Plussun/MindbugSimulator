using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameEngine
{
    public GameState State;

    public GameEngine()
    {
        State = new GameState();
    }

    public void StartGame()
    {
        State.CurrentPhase = GamePhase.WaitingForMainAction;
        SetRandomActivePlayer();
        SetExpectedPlayer(State.ActivePlayerID);
        Debug.Log("游戏开始，当前游戏阶段" + State.CurrentPhase);
    }
    public void SetRandomActivePlayer()
    {
        //State.ActivePlayerID = Random.Range(0, 2);
        //Debug.Log("随机选择玩家" + State.ActivePlayerID + "作为当前回合玩家");
        State.ActivePlayerID = 0; 
        Debug.Log("测试时固定选择玩家" + State.ActivePlayerID + "作为当前回合玩家");
    }

    public void SetExpectedPlayer(int playerID)
    {
        State.ExpectedPlayerID = playerID;
        Debug.Log("设置玩家" + playerID + "为等待决策的玩家");
    }
    public void SetPlayerHand(int playerID, List<CardData> hand)
    {
        PlayerState player = State.Players[playerID];
        player.Hand.Clear();
        int instanceIDCounter = 0;
        foreach (var cardData in hand)
        {
            //卡牌ID的规则是前两位是玩家ID+1，后两位是卡牌实例ID，
            // 如玩家ID为0，卡牌实例ID为2，则卡牌ID为1002
            instanceIDCounter++;
            int thisInstanceID = (player.PlayerID + 1) * 1000 + instanceIDCounter; 
            CardInstance cardInstance = new CardInstance(cardData,thisInstanceID);
            player.Hand.Add(cardInstance);
        }
        Debug.Log("玩家" + playerID + "的手牌已设置");
    }
    public void ChangeActivePlayer(int playerID)
    {
        State.ActivePlayerID = playerID;
        State.ExpectedPlayerID = playerID; // 切换当前回合玩家时，也将等待决策的玩家设置为当前回合玩家
        Debug.Log("切换当前回合玩家为玩家" + State.ActivePlayerID);
    }
    public void ChangeGamePhase(GamePhase newPhase)
    {
        State.CurrentPhase = newPhase;
        Debug.Log("游戏阶段切换为" + State.CurrentPhase);
    }
    public void PlayCard(int playerID, int cardInstanceID)
    {
        // 检查当前游戏阶段是否允许出牌
        if(State.CurrentPhase != GamePhase.WaitingForMainAction)
        {
            Debug.Log("当前不是出牌阶段，无法打出卡牌");
            return;
        }
        if(playerID != State.ActivePlayerID)
        {
            Debug.Log("玩家" + playerID + "不是当前回合玩家，无法打出卡牌");
            return;
        }


        PlayerState player = State.Players[playerID];
        CardInstance cardToPlay = player.Hand.Find(card => card.CardInstanceID == cardInstanceID);
        if (cardToPlay != null)
        {
            Debug.Log("玩家" + playerID + "打出了卡牌" + cardToPlay.CardData.CardName);

            ChangeGamePhase(GamePhase.WaitingForMindbugDecision);
            SetExpectedPlayer(1 - playerID); // 假设有两个玩家，切换到另一个玩家
            State.PendingCardInstance = cardToPlay;
            player.Hand.Remove(cardToPlay);
            Debug.Log("游戏阶段切换为" + State.CurrentPhase + 
                "，等待玩家" + State.ExpectedPlayerID + "的Mindbug决策");
        }
        else
        {
            Debug.Log("玩家" + playerID + "没有找到卡牌实例ID为" + cardInstanceID + "的卡牌");
        }
    }
    public void MindbugDecision(int playerID, bool useMindbug)
    {
        if(State.CurrentPhase != GamePhase.WaitingForMindbugDecision)
        {
            Debug.Log("当前不是等待Mindbug决策阶段，无法进行Mindbug决策");
            return;
        }
        if(playerID != State.ExpectedPlayerID)
        {
            Debug.Log("玩家" + playerID + "不是当前等待决策的玩家，无法进行Mindbug决策");
            return;
        }

        if(State.Players[State.ExpectedPlayerID].MindbugCount > 0 
            && useMindbug)
        {
            // 假设使用Mindbug
        
            State.Players[State.ExpectedPlayerID].Field.Add(State.PendingCardInstance);
            State.Players[State.ExpectedPlayerID].MindbugCount-=1;
            Debug.Log("玩家" + playerID + "使用了Mindbug，卡牌" 
            + State.PendingCardInstance.CardData.CardName + "加入到玩家" + State.ExpectedPlayerID + "的场上");
            ChangeGamePhase(GamePhase.WaitingForMainAction);
            ChangeActivePlayer(State.ActivePlayerID); // 回合玩家不变
  
        }
        else
        {
            //如果没有使用mindbug，则将卡牌加入到当前回合玩家的场上,切换另一个玩家
            
            State.Players[State.ActivePlayerID].Field.Add(State.PendingCardInstance);
            ChangeGamePhase(GamePhase.WaitingForMainAction);
            ChangeActivePlayer(1 - State.ActivePlayerID); // 切换为另一个
            Debug.Log("玩家" + playerID + "没有使用Mindbug");
        }

         // 切换回当前回合玩家
        State.PendingCardInstance = null;
    }

    public void AttackDecision(int playerID, int cardInstanceID)
    {
        if(State.CurrentPhase != GamePhase.WaitingForMainAction)
        {
            Debug.Log("当前不是主要行动阶段，无法进行攻击决策");
            return;
        }
        if(playerID != State.ActivePlayerID)
        {
            Debug.Log("玩家" + playerID + "不是当前回合玩家，无法进行攻击决策");
            return;
        }
        State.PendingAttackCardInstance = State.Players[State.ActivePlayerID].Field.Find(
            card => card.CardInstanceID == cardInstanceID);
        if(State.PendingAttackCardInstance == null)
        {
            Debug.Log("玩家" + playerID + "的场地中没有ID为"
                 + cardInstanceID + "的卡牌");
            return;
        }
        State.ExpectedPlayerID = 1 - State.ActivePlayerID; // 假设有两个玩家，切换到另一个玩家
        ChangeGamePhase(GamePhase.WaitingForBlockDecision);
    }

    public void BlockDecision(int playerID, bool useBlock, int BlockCardInstanceID)
    {
        if(State.CurrentPhase != GamePhase.WaitingForBlockDecision)
        {
            Debug.Log("当前不是等待阻挡决策阶段，无法进行阻挡决策");
            return;
        }
        if(playerID != State.ExpectedPlayerID)
        {
            Debug.Log("玩家" + playerID + "不是当前等待决策的玩家，无法进行阻挡决策");
            return;
        }

        if (!useBlock)
        {
            if(LoseLife(State.ExpectedPlayerID, 1))
            {
                Debug.Log("玩家" + State.ExpectedPlayerID + 
                    "没有使用阻挡，生命值减少1，当前生命值为0，游戏结束");
                State.PendingAttackCardInstance = null;
                State.PendingBlockCardInstance = null;
                return;
            }
            else
            {
                Debug.Log("玩家" + State.ExpectedPlayerID + "没有使用阻挡，生命值减少1，当前生命值为" 
                    + State.Players[State.ExpectedPlayerID].Life);
            }
        }
        else
        {
            CardInstance blockCard = State.Players[State.ExpectedPlayerID].Field.Find(
                card => card.CardInstanceID == BlockCardInstanceID);
            if(blockCard == null)
            {
                Debug.Log("玩家" + playerID + "没有找到阻挡卡牌实例ID为" 
                    + BlockCardInstanceID + "的卡牌");
                return;
            }
            else
            {
                State.PendingBlockCardInstance = blockCard;
                if(State.PendingBlockCardInstance.CurrentPower > State.PendingAttackCardInstance.CurrentPower)
                {
                    Debug.Log("玩家" + playerID + "使用了卡牌" 
                        + blockCard.CardData.CardName + "，成功阻挡了攻击");
                    //阻挡成功，攻击卡牌被移除，阻挡卡牌不变
                    State.Players[State.ActivePlayerID].Field.Remove(State.PendingAttackCardInstance);
                    State.Players[State.ActivePlayerID].DiscardPile.Add(State.PendingAttackCardInstance);
                }
                else if(State.PendingBlockCardInstance.CurrentPower == State.PendingAttackCardInstance.CurrentPower)
                {
                    Debug.Log("玩家" + playerID + "使用了卡牌" 
                        + blockCard.CardData.CardName + "，阻挡与攻击卡牌同等强度，双方都被移除");
                    State.Players[State.ActivePlayerID].Field.Remove(State.PendingAttackCardInstance);
                    State.Players[State.ExpectedPlayerID].Field.Remove(State.PendingBlockCardInstance);
                    State.Players[State.ActivePlayerID].DiscardPile.Add(State.PendingAttackCardInstance);
                    State.Players[State.ExpectedPlayerID].DiscardPile.Add(State.PendingBlockCardInstance);
                }
                else
                {
                    Debug.Log("玩家" + playerID + "使用了卡牌" 
                        + blockCard.CardData.CardName + "，阻挡失败，阻挡卡牌被移除");
                    State.Players[State.ExpectedPlayerID].Field.Remove(State.PendingBlockCardInstance);
                    State.Players[State.ExpectedPlayerID].DiscardPile.Add(State.PendingBlockCardInstance);
                }
            }
        }


        ChangeGamePhase(GamePhase.WaitingForMainAction);
        ChangeActivePlayer(1 - State.ActivePlayerID); // 切换为另一个玩家
        State.PendingAttackCardInstance = null;
        State.PendingBlockCardInstance = null;
    }

    //玩家失去生命，返回true表示玩家死亡游戏结束，返回false表示游戏继续
    public bool LoseLife(int playerID, int amount)
    {
        PlayerState player = State.Players[playerID];
        player.Life -= amount;
        Debug.Log("玩家" + playerID + "失去" + amount + "点生命值，当前生命值为" + player.Life);
        if(player.Life <= 0)
        {
            ChangeGamePhase(GamePhase.GameOver);
            State.WinnerPlayerID = 1 - playerID; // 设置获胜玩家ID
            Debug.Log("玩家" + playerID + "的生命值为0，游戏结束");
            return true; // 游戏结束
        }
        return false; // 游戏继续
    }
}

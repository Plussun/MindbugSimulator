using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

[System.Serializable]
public class GameEngine
{
    public GameState State;
    public EventQueue EventQueue = new EventQueue();
    public List<CardInstance> AllCardsBase = new List<CardInstance>();

    public GameEngine()
    {
        State = new GameState();
    }

    public void StartGame()
    {
        State.CurrentPhase = GamePhase.WaitingForMainAction;
        State.PendingCardInstance = null;
        State.PendingAttackCardInstance = null;
        State.PendingBlockCardInstance = null;
        
        SetRandomActivePlayer();
        SetExpectedPlayer(State.ActivePlayerID);

        SetAllCards();

        SetPlayerDeck(State.ActivePlayerID, State.AllCards, 10);
        SetPlayerDeck(1 - State.ActivePlayerID, State.AllCards, 10);

        ShuffleDeck(State.ActivePlayerID);
        ShuffleDeck(1 - State.ActivePlayerID);
        Refill(State.ActivePlayerID);
        Refill(1 - State.ActivePlayerID);
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

    //设置基础牌库，只是用于暂时测试阶段，具体实现完48张卡需要替换。
    public void SetAllCardsBase(List<CardData> allCardsBase)
    {
        AllCardsBase.Clear();
        int instanceIDCounter = 0;
        foreach (var cardData in allCardsBase)
        {
            instanceIDCounter++;
            int thisInstanceID = instanceIDCounter; // 全局唯一的卡牌实例ID
            CardInstance cardInstance = new CardInstance(cardData, thisInstanceID);
            AllCardsBase.Add(cardInstance);
        }
    }
    public void SetAllCards()
    {
        State.AllCards.Clear();
        State.AllCards = new List<CardInstance>(AllCardsBase); // 复制基础卡牌实例列表

        //对所有卡牌的总牌库进行洗牌
        for (int i = 0; i < State.AllCards.Count; i++)
        {
            CardInstance temp = State.AllCards[i];
            int randomIndex = Random.Range(i, State.AllCards.Count);
            State.AllCards[i] = State.AllCards[randomIndex];
            State.AllCards[randomIndex] = temp;
        }
        
        Debug.Log("所有卡牌已设置，总数：" + State.AllCards.Count);
    }

    public void SetPlayerDeck(int playerID, List<CardInstance> AllCards,int deckCount)
    {
        PlayerState player = State.Players[playerID];
        player.Deck.Clear();
        for (int i = 0; i < deckCount; i++)
        {
            
            if (AllCards.Count > 0)
            {
                int randomIndex = Random.Range(0, AllCards.Count);
                player.Deck.Add(AllCards[randomIndex]);
                AllCards.RemoveAt(randomIndex);
            }
            else
            {
                Debug.LogWarning("卡牌数量不足，无法为玩家" + playerID + "设置完整的牌库");
                break;
            }
        }
        
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

    public void Refill(int playerID)
    {
        PlayerState player = State.Players[playerID];
        if(player.Hand.Count >= 5)
        {
            Debug.Log("玩家" + playerID + "的手牌已满，无需补充抽牌");
            return;
        }
        int cardsToDraw = 5 - player.Hand.Count;
        if(cardsToDraw > 0)
        {
            DrawCard(playerID, cardsToDraw);
            Debug.Log("玩家" + playerID + "的手牌不足5张，补充抽牌至5张");
        }
    }

    public void DrawCard(int playerID,int number)
    {
        PlayerState player = State.Players[playerID];
        for(int i = 0; i < number; i++)
        {
            if(player.Deck.Count <= 0)
            {
                Debug.Log("玩家" + playerID + "的牌库为空，无法抽牌");
                return;
            }
            CardInstance drawnCard = player.Deck[0];
            player.Deck.RemoveAt(0);
            player.Hand.Add(drawnCard);
            Debug.Log("玩家" + playerID + "抽到卡牌" + drawnCard.CardData.CardName 
                + "，当前手牌数量为" + player.Hand.Count);
        }
    }

    public void ShuffleDeck(int playerID)
    {
        PlayerState player = State.Players[playerID];
        //Fisher–Yates Shuffle方法，把当前索引的卡牌与后面随机一个索引的卡牌交换位置
        for (int i = 0; i < player.Deck.Count; i++)
        {
            CardInstance temp = player.Deck[i];
            int randomIndex = Random.Range(i, player.Deck.Count);
            player.Deck[i] = player.Deck[randomIndex];
            player.Deck[randomIndex] = temp;
        }
        Debug.Log("玩家" + playerID + "的牌库已洗牌");
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
            Refill(playerID); // 出牌后补充手牌
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
        
            DeployCard(State.ExpectedPlayerID, State.PendingCardInstance);
            
            State.Players[State.ExpectedPlayerID].MindbugCount-=1;
            Debug.Log("玩家" + playerID + "使用了Mindbug，卡牌" 
            + State.PendingCardInstance.CardData.CardName + "加入到玩家" + State.ExpectedPlayerID + "的场上");
            StartNextTurn(State.ActivePlayerID, true); // 保持当前回合玩家不变，开始下一回合
  
        }
        else
        {
            //如果没有使用mindbug，则将卡牌加入到当前回合玩家的场上,切换另一个玩家
            
            DeployCard(State.ActivePlayerID, State.PendingCardInstance);

            StartNextTurn(State.ActivePlayerID, false); // 切换到另一个玩家
            Debug.Log("玩家" + playerID + "没有使用Mindbug");
        }

         // 切换回当前回合玩家
        State.PendingCardInstance = null;
    }

    public void AttackDecision(int playerID, int cardInstanceID)
    {
        if(State.CurrentPhase != GamePhase.WaitingForMainAction 
            && State.CurrentPhase != GamePhase.WaitingForFrenzyAttack)
        {
            Debug.Log("当前不是主要行动或二次攻击阶段，无法进行攻击决策");
            return;
        }
        if(playerID != State.ActivePlayerID)
        {
            Debug.Log("玩家" + playerID + "不是当前回合玩家，无法进行攻击决策");
            return;
        }
        if(State.CurrentPhase == GamePhase.WaitingForFrenzyAttack)
        {
            if(State.PendingAttackCardInstance == null)
            {
                Debug.Log("当前是二次攻击阶段，但没有找到上一轮的攻击卡牌实例，无法进行二次攻击");
                return;
            }
            if(State.PendingAttackCardInstance.CardInstanceID != cardInstanceID)
            {
                Debug.Log("当前是二次攻击阶段，但选择的攻击卡牌实例ID与上一轮的攻击卡牌实例ID不一致，无法进行二次攻击");
                return;
            }
        }
        else{
            //如果是正常攻击阶段，则寻找相关实例提交为PendingAttackCardInstance
            State.PendingAttackCardInstance = State.Players[State.ActivePlayerID].Field.Find(
                card => card.CardInstanceID == cardInstanceID);
        }
        if(State.PendingAttackCardInstance == null)
        {
            Debug.Log("玩家" + playerID + "没有找到攻击卡牌实例ID为" + cardInstanceID + "的卡牌");
            return;
        }
        AttackEvent attackEvent = new AttackEvent(playerID, 
            State.PendingAttackCardInstance.CardInstanceID);
        EventQueue.Enqueue(attackEvent);
        EventQueue.ProcessEvent(this);
    }

    public void BeginAttack(int playerID, int cardInstanceID, int targetCardInstanceID)
    {
        //TODO:在攻击决策做出判断后，执行攻击流程
        if(State.PendingAttackCardInstance != null)
        {
            State.PendingAttackCardInstance.AttackCount += 1; // 增加攻击次数
            //如果攻击卡牌有hunter关键词且存在目标卡牌
            if( State.PendingAttackCardInstance.HasKeyword(Keywords.Hunter) &&
                targetCardInstanceID >= 0)
            {
                State.PendingHunterTargetCardInstance = State.Players[1 - State.ActivePlayerID].Field.Find(
                    card => card.CardInstanceID == targetCardInstanceID);
                if(State.PendingHunterTargetCardInstance != null)
                {
                    Debug.Log("玩家" + playerID + "选择了卡牌" 
                        + State.PendingAttackCardInstance.CardData.CardName + "进行攻击，目标卡牌为" 
                        + State.PendingHunterTargetCardInstance.CardData.CardName);
                }
            }
            Debug.Log("玩家" + playerID + "选择了卡牌" 
                + State.PendingAttackCardInstance.CardData.CardName + "进行攻击");
            //触发攻击效果
            if(State.PendingAttackCardInstance.CardData.CardEffects != null)
            {
                foreach(var effect in State.PendingAttackCardInstance.CardData.CardEffects)
                {
                    if(effect.Trigger == EffectTrigger.OnAttack)
                    {
                        effect.Resolve(this, playerID, State.PendingAttackCardInstance);
                    }
                }
            }
        }
        else
        {
            //如果没有找到对应的卡牌实例，则输出错误信息
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
        //如果不适用阻挡
        if (!useBlock)
        {
            if (State.PendingHunterTargetCardInstance != null)
            {
                Debug.Log("由于有被狩猎目标，不允许不阻挡");
                return;
            }
            //死了
            if(LoseLife(State.ExpectedPlayerID, 1))
            {
                Debug.Log("玩家" + State.ExpectedPlayerID + 
                    "没有使用阻挡，生命值减少1，当前生命值为0，游戏结束");
                if(State.PendingAttackCardInstance != null)
                {
                    State.PendingAttackCardInstance.AttackCount = 0;
                }
                State.PendingAttackCardInstance = null;
                State.PendingBlockCardInstance = null;
                State.PendingHunterTargetCardInstance = null;
                return;
            }
            //没死
            else
            {
                Debug.Log("玩家" + State.ExpectedPlayerID + "没有使用阻挡，生命值减少1，当前生命值为" 
                    + State.Players[State.ExpectedPlayerID].Life);
            }
        }
        //如果使用了阻挡
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
                //判断是否可以阻挡
                if(!CanBlock(State.PendingAttackCardInstance, State.PendingBlockCardInstance, State.PendingHunterTargetCardInstance))
                {
                    Debug.Log("非法的阻挡对象");
                    return;
                }
                //触发阻挡效果
                foreach(var effect in State.PendingBlockCardInstance.CardData.CardEffects)
                {
                    if(effect.Trigger == EffectTrigger.OnBlock)
                    {
                        effect.Resolve(this, playerID, State.PendingBlockCardInstance);
                    }
                }

                ResolveCombat(State.PendingAttackCardInstance, State.PendingBlockCardInstance);
            }
        }

        //能够二次攻击的条件：
        // 攻击卡牌存在，且具有Frenzy关键词，
        // 且攻击次数小于2，且攻击卡牌仍在当前回合玩家的场上
        CardInstance attackCard = State.PendingAttackCardInstance;
        bool canAttackAgain = attackCard != null &&
            attackCard.HasKeyword(Keywords.Frenzy) &&
            attackCard.AttackCount < 2 &&
            State.Players[State.ActivePlayerID].Field.Contains(attackCard);
        if(canAttackAgain)
        {
            ChangeGamePhase(GamePhase.WaitingForFrenzyAttack);
            Debug.Log("玩家" + State.ActivePlayerID + "的攻击卡牌" 
                + attackCard.CardData.CardName + "具有Frenzy关键词，可以进行二次攻击");
            State.PendingBlockCardInstance = null; // 清除阻挡卡牌实例
            State.PendingHunterTargetCardInstance = null; // 清除狩猎目标卡牌
            State.ExpectedPlayerID = State.ActivePlayerID; // 设置等待决策的玩家为当前回合玩家
            return;
        }
        StartNextTurn(State.ActivePlayerID, false); // 切换到另一个玩家
    }

    public void SkipFrenzyAttack(int playerID)
    {
        if(State.CurrentPhase != GamePhase.WaitingForFrenzyAttack)
        {
            Debug.Log("当前不是Frenzy二次攻击阶段，无法放弃二次攻击");
            return;
        }
        if(playerID != State.ExpectedPlayerID)
        {
            Debug.Log("玩家" + playerID + "不是当前等待决策的玩家，无法放弃二次攻击");
            return;
        }

        StartNextTurn(playerID, false);
    }

    public bool CanBlock(CardInstance attackCard, CardInstance blockCard, CardInstance targetCard = null)
    {
        // 当前回合玩家是攻击方，另一名玩家是阻挡方。
        PlayerState blockingPlayer = State.Players[1 - State.ActivePlayerID];

        if(blockingPlayer.GlobalForbiddenBlockerInstanceIDs.Contains(blockCard.CardInstanceID))
        {
            return false;
        }

        IndividualBlockRestriction individualRestriction =
            blockingPlayer.IndividualBlockRestrictions.Find(
                restriction => restriction.AttackCardInstanceID == attackCard.CardInstanceID);
        if(individualRestriction != null &&
            individualRestriction.ForbiddenBlockerInstanceIDs.Contains(blockCard.CardInstanceID))
        {
            return false;
        }

        // 检查卡牌是否是敏捷，是则只能被敏捷卡牌阻挡
        if (attackCard.HasKeyword(Keywords.Sneaky))
        {
            if(!blockCard.HasKeyword(Keywords.Sneaky))
            {
                return false; // 阻挡卡牌不是敏捷卡牌，无法阻挡敏捷攻击
            }
        }

        // 检查卡牌是否是“狩猎”，是则只能被“狩猎”指定的卡牌阻挡
        if(attackCard.HasKeyword(Keywords.Hunter)){
            if(targetCard != null && blockCard != targetCard)
            {
                return false; // 阻挡卡牌不是指定的目标卡牌，无法阻挡
            }
        }

        return true; // 默认情况下可以阻挡
    }

    public void AddGlobalBlockRestriction(int playerID, int blockerCardInstanceID)
    {
        List<int> forbiddenBlockers = State.Players[playerID].GlobalForbiddenBlockerInstanceIDs;
        if(!forbiddenBlockers.Contains(blockerCardInstanceID))
        {
            forbiddenBlockers.Add(blockerCardInstanceID);
        }
    }

    public void AddIndividualBlockRestriction(
        int playerID,
        int attackCardInstanceID,
        int blockerCardInstanceID)
    {
        List<IndividualBlockRestriction> restrictions =
            State.Players[playerID].IndividualBlockRestrictions;
        IndividualBlockRestriction restriction = restrictions.Find(
            item => item.AttackCardInstanceID == attackCardInstanceID);

        if(restriction == null)
        {
            restriction = new IndividualBlockRestriction(attackCardInstanceID);
            restrictions.Add(restriction);
        }

        if(!restriction.ForbiddenBlockerInstanceIDs.Contains(blockerCardInstanceID))
        {
            restriction.ForbiddenBlockerInstanceIDs.Add(blockerCardInstanceID);
        }
    }

    public void ResolveCombat(CardInstance attackCard, CardInstance blockCard)
    {
        if (blockCard == null)
        {
            // 没有阻挡卡牌，攻击卡牌直接造成伤害
            LoseLife(State.ExpectedPlayerID, 1);
            Debug.Log("玩家" + State.ExpectedPlayerID + "没有阻挡，生命值减少1");
        }
        else
        {
            // 有阻挡卡牌，比较力量值
            if (blockCard.CurrentPower > attackCard.CurrentPower)
            {
                // 阻挡成功，攻击卡牌被移除，阻挡卡牌不变
                DefeatCard(State.ActivePlayerID, attackCard.CardInstanceID);
                //如果攻击卡牌有剧毒关键词，则阻挡卡牌还是要死亡
                if(attackCard.HasKeyword(Keywords.Poisonous))
                {
                    DefeatCard(State.ExpectedPlayerID, blockCard.CardInstanceID);
                    Debug.Log("玩家" + State.ExpectedPlayerID + "的阻挡卡牌" 
                        + blockCard.CardData.CardName + "被剧毒效果移除");
                    return;
                }
                Debug.Log("玩家" + State.ExpectedPlayerID + "成功阻挡了攻击");
            }
            else if (blockCard.CurrentPower == attackCard.CurrentPower)
            {
                // 双方力量值相等，双方都被移除
                DefeatCard(State.ActivePlayerID, attackCard.CardInstanceID);
                DefeatCard(State.ExpectedPlayerID, blockCard.CardInstanceID);
                Debug.Log("双方力量值相等，双方都被移除");
            }
            else
            {
                // 阻挡失败，阻挡卡牌被移除
                DefeatCard(State.ExpectedPlayerID, blockCard.CardInstanceID);
                Debug.Log("玩家" + State.ExpectedPlayerID + "阻挡失败，阻挡卡牌被移除");
                //如果阻挡卡牌有剧毒关键词，则攻击卡牌还是要死亡
                if(blockCard.HasKeyword(Keywords.Poisonous))
                {
                    DefeatCard(State.ActivePlayerID, attackCard.CardInstanceID);
                    Debug.Log("玩家" + State.ActivePlayerID + "的攻击卡牌" 
                        + attackCard.CardData.CardName + "被剧毒效果移除");
                    return;
                }
            }
        }
    }

    public void StartNextTurn(int playerID, bool keepActivePlayer)
    {
        if(State.CurrentPhase == GamePhase.GameOver)
        {
            Debug.Log("游戏已结束，无法开始下一回合");
            return;
        }
        if(playerID != State.ActivePlayerID)
        {
            Debug.Log("玩家" + playerID + "不是当前回合玩家，无法开始下一回合");
            return;
        }
        if (!keepActivePlayer)
        {
            State.ActivePlayerID = 1 - State.ActivePlayerID; // 切换为另一个玩家
        }

        State.ExpectedPlayerID = State.ActivePlayerID; // 设置等待决策的玩家为当前回合玩家
        ChangeGamePhase(GamePhase.WaitingForMainAction);

        State.PendingCardInstance = null;
        if(State.PendingAttackCardInstance != null)
        {
            State.PendingAttackCardInstance.AttackCount = 0; // 重置攻击次数
        }
        State.PendingAttackCardInstance = null;
        State.PendingBlockCardInstance = null;
        State.PendingHunterTargetCardInstance = null;
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

    public void ChangeBasePower(int playerID,int cardInstanceID,int amount)
    {
        PlayerState player = State.Players[playerID];
        CardInstance card = player.Field.Find(card => card.CardInstanceID == cardInstanceID);
        if(card != null)
        {
            card.BasePower += amount;
            card.UpdatePowerAndKeywords();
            Debug.Log("玩家" + playerID + "的卡牌" + card.CardData.CardName 
                + "的力量值变化为" + card.CurrentPower);
        }
        else
        {
            Debug.Log("玩家" + playerID + "没有找到卡牌实例ID为" 
                + cardInstanceID + "的卡牌");
        }
    }

    public void DeployCard(int playerID, CardInstance card)
    {
        PlayerState player = State.Players[playerID];
        if(card != null)
        {
            player.Field.Add(card);
            Debug.Log("玩家" + playerID + "部署了卡牌" + card.CardData.CardName);
        }
        else
        {
            Debug.Log("玩家" + playerID + "没有要部署的卡牌");
        }
        //TODO：触发部署事件
        if(card != null && card.CardData.CardEffects != null)
        {
            foreach(var effect in card.CardData.CardEffects)
            {
                if(effect.Trigger == EffectTrigger.OnDeploy)
                {
                    effect.Resolve(this, playerID, card);
                }
            }
        }
        RefreshFieldEffect(); // 刷新场上效果
    }
    public void DefeatCard(int playerID, int cardInstanceID)
    {
        PlayerState player = State.Players[playerID];
        CardInstance card = player.Field.Find(card => card.CardInstanceID == cardInstanceID);
        //处理坚韧关键词，检测其是否横置
        if (card.HasKeyword(Keywords.Tough))
        {
            if (!card.IsExhausted)
            {
                card.IsExhausted = true;
                return;
            }
        }
        if(card != null)
        {
            card.ClearTempEffects(); // 清除临时效果
            player.Field.Remove(card);
            player.DiscardPile.Add(card);
            Debug.Log("玩家" + playerID + "的卡牌" + card.CardData.CardName + "被击败，移至弃牌堆");
            //触发阵亡事件
            if(card.CardData.CardEffects != null)
            {
                foreach(var effect in card.CardData.CardEffects)
                {
                    if(effect.Trigger == EffectTrigger.OnDefeat)
                    {
                        effect.Resolve(this, playerID, card);
                    }
                }
            }
            
        }
        else
        {
            Debug.Log("玩家" + playerID + "没有找到卡牌实例ID为" 
                + cardInstanceID + "的卡牌");
        }
        
        RefreshFieldEffect(); // 刷新场上效果
    }

    public void DiscardCard(int playerID, int cardInstanceID)
    {
        PlayerState player = State.Players[playerID];
        CardInstance card = player.Hand.Find(card => card.CardInstanceID == cardInstanceID);
        if(card != null)
        {
            player.Hand.Remove(card);
            player.DiscardPile.Add(card);
            Debug.Log("玩家" + playerID + "的手牌" + card.CardData.CardName + "被弃置，移至弃牌堆");
        }
        else
        {
            Debug.Log("玩家" + playerID + "没有找到手牌实例ID为" 
                + cardInstanceID + "的卡牌");
        }
    }

    public void ClearAllOnFieldEffect()
    {
        foreach(var player in State.Players)
        {
            player.GlobalForbiddenBlockerInstanceIDs.Clear();
            player.IndividualBlockRestrictions.Clear();
            foreach(var card in player.Field)
            {
                card.ClearTempEffects();
            }
        }
    }
    public void RefreshFieldEffect()
    {
        ClearAllOnFieldEffect();
        // 阻挡限制依赖更新后的 CurrentPower 和 CurrentKeywords，
        // 因此先暂存这类效果，在本次刷新的普通场上效果结算后再执行。
        // 这只是 RefreshFieldEffect 内部的两阶段计算，与未来的事件队列无关。
        List<(CardFieldEffect effect, int playerID, CardInstance card)> afterCardUpdateEffects =
            new List<(CardFieldEffect, int, CardInstance)>();

        foreach(var player in State.Players)
        {
            foreach(var card in player.Field)
            {
                if(card.CardData.CardFieldEffects != null)
                {
                    foreach(var fieldEffect in card.CardData.CardFieldEffects)
                    {
                        if(fieldEffect.ResolveAfterCardUpdate)
                        {
                            afterCardUpdateEffects.Add((fieldEffect, player.PlayerID, card));
                        }
                        else
                        {
                            fieldEffect.Resolve(this, player.PlayerID, card);
                        }
                    }
                }
            }
        }
        UpdateAllFieldCardPowerAndKeywords();

        foreach(var afterCardUpdateEffect in afterCardUpdateEffects)
        {
            afterCardUpdateEffect.effect.Resolve(
                this,
                afterCardUpdateEffect.playerID,
                afterCardUpdateEffect.card);
        }
    }

    public void UpdateAllFieldCardPowerAndKeywords()
    {
        foreach(var player in State.Players)
        {
            foreach(var card in player.Field)
            {
                card.UpdatePowerAndKeywords();
            }
        }
    }

    public void SelectCards(int playerID, List<int> selectedCardInstanceIDs)
    {
        if(State.CurrentPhase != GamePhase.WaitingForChoice)
        {
            Debug.Log("当前不是等待选择阶段，无法进行选择");
            return;
        }
        if(playerID != State.ExpectedPlayerID)
        {
            Debug.Log("玩家" + playerID + "不是当前等待决策的玩家，无法进行选择");
            return;
        }
        EventQueue.SubmitChoice(this, selectedCardInstanceIDs);
    }

    public CardInstance GetCardInstanceByID(int cardInstanceID)
    {
        foreach(var player in State.Players)
        {
            foreach(var card in player.Hand)
            {
                if(card.CardInstanceID == cardInstanceID)
                {
                    return card;
                }
            }
            foreach(var card in player.Field)
            {
                if(card.CardInstanceID == cardInstanceID)
                {
                    return card;
                }
            }
            foreach(var card in player.Deck)
            {
                if(card.CardInstanceID == cardInstanceID)
                {
                    return card;
                }
            }
            foreach(var card in player.DiscardPile)
            {
                if(card.CardInstanceID == cardInstanceID)
                {
                    return card;
                }
            }
        }
        return null; // 如果没有找到对应的卡牌实例，返回null
    }
    
    public void NextGame()
    {
        if(State.CurrentPhase != GamePhase.GameOver)
        {
            Debug.Log("当前游戏未结束，无法开始下一局");
            return;
        }
        State = new GameState();
        Debug.Log("游戏重置，开始下一局");
        StartGame();
    }


}

using Unity.Netcode;
using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class NetworkController : NetworkBehaviour
{
    public GameController GameController;
    private const ulong UnassignedClientId = ulong.MaxValue;
    private ulong player0ClientId = UnassignedClientId;
    private ulong player1ClientId = UnassignedClientId;
    NetworkManager networkManager;
    public ViewController viewController;

    public TMP_Text StatusText;

    private GamePhase clientCurrentPhase = GamePhase.Setup;

    //以下为与连接有关的方法
    public override void OnNetworkSpawn()
    {
        //如果不是服务器端，则不执行后续逻辑
        if (!IsServer)
        {
            return;
        }

        networkManager = NetworkManager.Singleton;
        networkManager.OnClientConnectedCallback += OnClientConnectedCallback;
    }
    public override void OnNetworkDespawn()
    {
        if (!IsServer)
        {
            return;
        }

        networkManager.OnClientConnectedCallback -= OnClientConnectedCallback;
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        //分配客户端ID给玩家
        
        
        if (clientId == networkManager.LocalClientId)
        {
            player0ClientId = clientId;
            
        }
        else
        {
            player1ClientId = clientId;
            GameController.GameEngine.StartGame();
            SyncState(); // 在服务器端处理完请求后，同步状态到客户端
            
        }
        
        
    }


    //以下为客户端的命令接收方法
    //出牌请求
    public void PlayCardRequest(int cardInstanceId)
    {
        PlayCardServerRpc(cardInstanceId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayCardServerRpc(int cardInstanceId,
        ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        int playerId = GetPlayerIdByClientId(clientId);
        GameController.GameEngine.PlayCard(playerId, cardInstanceId);
        SyncState(); // 在服务器端处理完请求后，同步状态到客户端
        Debug.Log("玩家" + playerId + "请求出牌，卡牌实例ID为" 
            + cardInstanceId);
    }
    //使用夺心虫请求
    public void MindbugDecisionRequest(bool decision)
    {
        MindbugDecisionServerRpc(decision);
    }

    [ServerRpc(RequireOwnership = false)]
    private void MindbugDecisionServerRpc(bool useMindbug,
        ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        int playerId = GetPlayerIdByClientId(clientId);
        GameController.GameEngine.MindbugDecision(playerId, useMindbug);
        SyncState(); // 在服务器端处理完请求后，同步状态到客户端
        Debug.Log("玩家" + playerId + "提交夺心虫决策为" + useMindbug);
    }

    //攻击请求
    public void AttackDecisionRequest(int cardInstanceId)
    {
        AttackDecisionServerRpc(cardInstanceId);
    }
    [ServerRpc(RequireOwnership = false)]
    private void AttackDecisionServerRpc(int cardInstanceId,
        ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        int playerId = GetPlayerIdByClientId(clientId);
        GameController.GameEngine.AttackDecision(playerId, cardInstanceId);
        SyncState(); // 在服务器端处理完请求后，同步状态到客户端
        Debug.Log("玩家" + playerId + "请求攻击，卡牌实例ID为" 
            + cardInstanceId);
    }

    public void BlockDecisionRequest(bool useBlock,int blockCardInstanceId)
    {
        BlockDecisionServerRpc(useBlock, blockCardInstanceId);
    }
    [ServerRpc(RequireOwnership = false)]
    private void BlockDecisionServerRpc(bool useBlock, int blockCardInstanceId,
        ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        int playerId = GetPlayerIdByClientId(clientId);
        GameController.GameEngine.BlockDecision(playerId, useBlock, blockCardInstanceId);
        SyncState(); // 在服务器端处理完请求后，同步状态到客户端
        Debug.Log("玩家" + playerId + "请求阻挡，卡牌实例ID为" 
            + blockCardInstanceId);
    }

    

    //用于把Gamestate同步到两边的客户端
    private void SyncState()
    {
        GameState state = GameController.GameEngine.State;
        int phase = (int)state.CurrentPhase;
        int winnerPlayerID = state.WinnerPlayerID;
        int activePlayerId = state.ActivePlayerID;
        int expectedPlayerId = state.ExpectedPlayerID;
        CardNetworkState pendingCard = 
            new CardNetworkState
            {
                CardInstanceID = -1,
                CardDataID = 0,
                currentPower = 0
            };
        if(state.PendingCardInstance != null)
        {
            pendingCard = new CardNetworkState
            {
                CardInstanceID = state.PendingCardInstance.CardInstanceID,
                CardDataID = state.PendingCardInstance.CardData.CardDataID,
                currentPower = state.PendingCardInstance.CurrentPower
            };
        }
        CardNetworkState pendingAttackCard = 
            new CardNetworkState
            {
                CardInstanceID = -1,
                CardDataID = 0,
                currentPower = 0
            };
        if(state.PendingAttackCardInstance != null)
        {
            pendingAttackCard = new CardNetworkState
            {
                CardInstanceID = state.PendingAttackCardInstance.CardInstanceID,
                CardDataID = state.PendingAttackCardInstance.CardData.CardDataID,
                currentPower = state.PendingAttackCardInstance.CurrentPower
            };
        }

        CardNetworkState[] player0Hand = GetCardNetworkStates(state.Players[0].Hand);
        CardNetworkState[] player0Field = GetCardNetworkStates(state.Players[0].Field);
        CardNetworkState[] player0Discard = GetCardNetworkStates(state.Players[0].DiscardPile);
        CardNetworkState[] player1Hand = GetCardNetworkStates(state.Players[1].Hand);
        CardNetworkState[] player1Field = GetCardNetworkStates(state.Players[1].Field);
        CardNetworkState[] player1Discard = GetCardNetworkStates(state.Players[1].DiscardPile);

        int player0Life = state.Players[0].Life;
        int player1Life = state.Players[1].Life;
        int player0DeckCount = state.Players[0].Deck.Count;
        int player1DeckCount = state.Players[1].Deck.Count;
        int player0MindbugCount = state.Players[0].MindbugCount;
        int player1MindbugCount = state.Players[1].MindbugCount;

        // 设置目标客户端 ID 数组
        ClientRpcParams rpcParamsPlayer0 = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { player0ClientId }
            }
        };
        ClientRpcParams rpcParamsPlayer1 = new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { player1ClientId }
            }
        };

        UpdateStatusClientRpc(
            phase,
            winnerPlayerID,
            activePlayerId,
            expectedPlayerId,
            0,
            pendingCard,
            pendingAttackCard,
            player0Life,
            player1Life,
            player0DeckCount,
            player1DeckCount,
            player0MindbugCount,
            player1MindbugCount,
            player1Hand.Length,
            player0Hand,
            player0Field,
            player1Field,
            player0Discard,
            player1Discard,
            rpcParamsPlayer0
        );
        UpdateStatusClientRpc(
            phase,
            winnerPlayerID,
            activePlayerId,
            expectedPlayerId,
            1,
            pendingCard,
            pendingAttackCard,
            player1Life,
            player0Life,
            player1DeckCount,
            player0DeckCount,
            player1MindbugCount,
            player0MindbugCount,
            player0Hand.Length,
            player1Hand,
            player1Field,
            player0Field,
            player1Discard,
            player0Discard,
            rpcParamsPlayer1
        );
    }

    //调试阶段用于观察两边同步的状态，真实项目中不应该网络直接修改界面
    [ClientRpc]
    private void UpdateStatusClientRpc(
        int phase,
        int winnerPlayerID,
        int activePlayerId,
        int expectedPlayerId,
        int playerId,
        CardNetworkState pendingCard,
        CardNetworkState pendingAttackCard,
        int playerLife,
        int opponentLife,
        int playerDeckCount,
        int opponentDeckCount,
        int playerMindbugCount,
        int opponentMindbugCount,
        int opponentHandCount,
        CardNetworkState[] playerHand,
        CardNetworkState[] playerField,
        CardNetworkState[] opponentField,
        CardNetworkState[] playerDiscard,
        CardNetworkState[] opponentDiscard,
        ClientRpcParams clientRpcParams = default
    )
    {
        
        if (StatusText != null)
        {
            string handText = GetCardDataString(playerHand);
            string fieldText = GetCardDataString(playerField);
            string opponentFieldText = GetCardDataString(opponentField);
            string playerDiscardText = GetCardDataString(playerDiscard);
            string opponentDiscardText = GetCardDataString(opponentDiscard);
            GamePhase currentPhase = (GamePhase)phase;
            string phaseText;
            string actionText;

            switch (currentPhase)
            {
                case GamePhase.Setup:
                    phaseText = "准备阶段";
                    actionText = "等待游戏开始";
                    break;
                case GamePhase.WaitingForMainAction:
                    phaseText = "主要行动阶段";
                    actionText = expectedPlayerId == playerId
                        ? "请你出牌或发起攻击"
                        : "等待对手行动";
                    break;
                case GamePhase.WaitingForMindbugDecision:
                    phaseText = "夺心虫决定阶段";
                    actionText = expectedPlayerId == playerId
                        ? "请你决定是否使用夺心虫"
                        : "等待对手决定是否使用夺心虫";
                    break;
                case GamePhase.WaitingForBlockDecision:
                    phaseText = "阻挡决定阶段";
                    actionText = expectedPlayerId == playerId
                        ? "请你决定是否阻挡"
                        : "等待对手决定是否阻挡";
                    break;
                case GamePhase.GameOver:
                    phaseText = "游戏结束";
                    actionText = "对局已结束";
                    break;
                default:
                    phaseText = currentPhase.ToString();
                    actionText = "等待游戏状态更新";
                    break;
            }

            StatusText.text =
                "【阶段】" + phaseText + "  回合玩家 " + activePlayerId + "\n" +
                "【操作】" + actionText + "\n" +
                "【敌方】手牌 " + opponentHandCount + "  生命 " + opponentLife + "\n" +
                "场地：" + opponentFieldText + "\n" +
                "弃牌：" + opponentDiscardText + "\n" +
                "----------------\n" +
                "【我方】生命 " + playerLife + "\n" +
                "场地：" + fieldText + "\n" +
                "手牌：" + handText + "\n" +
                "弃牌：" + playerDiscardText;
            Debug.Log(StatusText.text);
            
        }

        clientCurrentPhase = (GamePhase)phase;
        viewController.RefreshView(
                gamePhase: phase,
                winnerPlayerID: winnerPlayerID,
                localPlayerID: playerId,
                ActivePlayerID: activePlayerId,
                ExpectedPlayerID: expectedPlayerId,
                localPlayerLife: playerLife,
                opponentPlayerLife: opponentLife,
                localPlayerDeckCount: playerDeckCount,
                opponentPlayerDeckCount: opponentDeckCount,
                localPlayerMindbugCount: playerMindbugCount,
                opponentPlayerMindbugCount: opponentMindbugCount,
                localPlayerDiscardCount: playerDiscard.Length,
                opponentPlayerDiscardCount: opponentDiscard.Length,
                localPlayerHand: playerHand,
                localPlayerField: playerField,
                opponentPlayerField: opponentField,
                opponentHandCount: opponentHandCount,
                pendingCard: pendingCard,
                pendingAttack: pendingAttackCard
            );
    }

    public int GetGamePhase()
    {
        return (int)clientCurrentPhase;
    }



    //一些工具性的方法
    //根据客户端ID获取玩家ID
    public int GetPlayerIdByClientId(ulong clientId)
    {
        if (clientId == player0ClientId)
        {
            return 0;
        }
        else if (clientId == player1ClientId)
        {
            return 1;
        }
        else
        {
            return -1; // 未知客户端ID
        }
    }

    //将服务器中的卡牌实例转换为RPC可以发送的数据
    private CardNetworkState[] GetCardNetworkStates(List<CardInstance> cards)
    {
        CardNetworkState[] networkStates = new CardNetworkState[cards.Count];

        for (int i = 0; i < cards.Count; i++)
        {
            networkStates[i] = new CardNetworkState
            {
                CardInstanceID = cards[i].CardInstanceID,
                CardDataID = cards[i].CardData.CardDataID,
                currentPower = cards[i].CurrentPower,
                isExhausted = cards[i].IsExhausted
            };
        }

        return networkStates;
    }

    //接收卡组从carddatabase中获取carddata并转换成string
    private string GetCardDataString(CardNetworkState[] cards)
    {
        if (cards.Length == 0)
        {
            return "无";
        }

        string[] cardTexts = new string[cards.Length];
        for(int i = 0; i < cards.Length; i++)
        {
            CardNetworkState card = cards[i];

            CardData cardData = GameController.CardDatabase.Find(
                c => c.CardDataID == card.CardDataID);
            string cardName = cardData != null ? 
                cardData.CardName : "未知卡牌";
            cardTexts[i] = cardName + "[#" + card.CardInstanceID
                + "/P" + card.currentPower + "]";
        }
        return string.Join(" ｜ ", cardTexts);
    }
        
    
}

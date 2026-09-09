using Unity.Netcode;
using UnityEngine;
using System.Collections.Generic;

public class NetworkController : NetworkBehaviour
{
    public GameController GameController;
    private const ulong UnassignedClientId = ulong.MaxValue;
    private ulong player0ClientId = UnassignedClientId;
    private ulong player1ClientId = UnassignedClientId;
    NetworkManager networkManager;
    public ViewController viewController;


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

    public void SelectCardsRequest(List<int> selectedCardInstanceIDs)
    {
        SelectCardsServerRpc(selectedCardInstanceIDs.ToArray());
    }
    [ServerRpc(RequireOwnership = false)]
    private void SelectCardsServerRpc(int[] selectedCardInstanceIDs,
        ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        int playerId = GetPlayerIdByClientId(clientId);
        GameController.GameEngine.SelectCards(playerId, new List<int>(selectedCardInstanceIDs));
        SyncState(); // 在服务器端处理完请求后，同步状态到客户端
        Debug.Log("玩家" + playerId + "提交选择，选择的卡牌实例ID为" 
            + string.Join(", ", selectedCardInstanceIDs));
    }

    public void SkipFrenzyAttackRequest()
    {
        SkipFrenzyAttackServerRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    private void SkipFrenzyAttackServerRpc(
        ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        int playerId = GetPlayerIdByClientId(clientId);
        GameController.GameEngine.SkipFrenzyAttack(playerId);
        SyncState(); // 在服务器端处理完请求后，同步状态到客户端
        Debug.Log("玩家" + playerId + "请求放弃Frenzy二次攻击");
    }

    public void NextGameRequest()
    {
        NextGameServerRpc();
    }
    [ServerRpc(RequireOwnership = false)]
    private void NextGameServerRpc(
        ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        int playerId = GetPlayerIdByClientId(clientId);
        GameController.GameEngine.NextGame();
        SyncState(); // 在服务器端处理完请求后，同步状态到客户端
        Debug.Log("玩家" + playerId + "请求开始下一局游戏");
    }

    

    //把服务器本次产生的动画事件分别转换为两名玩家可见的数据并发送。
    private void SyncState()
    {
        GameEngine gameEngine = GameController.GameEngine;

        //即使本次没有专用动画，也用最终快照校正阶段、按钮和所有界面数据。
        gameEngine.RecordAnimationEvent(GameAnimationType.StateRefresh);

        List<GameAnimationEvent> serverEvents =
            gameEngine.State.PendingAnimationEvents;

        ClientAnimationEvent[] player0Events =
            CreateClientAnimationEvents(serverEvents, 0);
        ClientAnimationEvent[] player1Events =
            CreateClientAnimationEvents(serverEvents, 1);

        ReceiveAnimationEventsClientRpc(
            player0Events,
            CreateClientRpcParams(player0ClientId));
        ReceiveAnimationEventsClientRpc(
            player1Events,
            CreateClientRpcParams(player1ClientId));

        //RPC调用已经把数据提交给NGO，服务器可以清除本批待发送事件。
        serverEvents.Clear();
    }

    private ClientRpcParams CreateClientRpcParams(ulong clientId)
    {
        return new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new ulong[] { clientId }
            }
        };
    }

    private ClientAnimationEvent[] CreateClientAnimationEvents(
        List<GameAnimationEvent> serverEvents,
        int localPlayerID)
    {
        ClientAnimationEvent[] clientEvents =
            new ClientAnimationEvent[serverEvents.Count];

        for(int i = 0; i < serverEvents.Count; i++)
        {
            GameAnimationEvent serverEvent = serverEvents[i];

            clientEvents[i] = new ClientAnimationEvent
            {
                SequenceID = serverEvent.SequenceID,
                AnimationType = serverEvent.AnimationType,
                PlayerID = serverEvent.PlayerID,
                CardInstanceID = GetVisibleAnimationCardID(
                    serverEvent,
                    localPlayerID),
                StateAfterEvent = CreateClientSnapshot(
                    serverEvent.StateAfterEvent,
                    localPlayerID)
            };
        }

        return clientEvents;
    }

    //对手抽到的牌仍然是隐藏信息，不向该客户端发送可以追踪的实例ID。
    private int GetVisibleAnimationCardID(
        GameAnimationEvent animationEvent,
        int localPlayerID)
    {
        bool isOpponentDraw =
            animationEvent.AnimationType == GameAnimationType.DrawCard &&
            animationEvent.PlayerID != localPlayerID;

        return isOpponentDraw ? -1 : animationEvent.CardInstanceID;
    }

    //把服务器完整快照转换成指定玩家视角的快照。
    private ClientGameStateSnapshot CreateClientSnapshot(
        GameStateSnapshot serverSnapshot,
        int localPlayerID)
    {
        int opponentPlayerID = 1 - localPlayerID;
        PlayerStateSnapshot localPlayer =
            serverSnapshot.Players[localPlayerID];
        PlayerStateSnapshot opponentPlayer =
            serverSnapshot.Players[opponentPlayerID];

        PendingChoiceSnapshot pendingChoice =
            serverSnapshot.PendingChoice;
        bool canSeePendingChoice =
            pendingChoice != null &&
            pendingChoice.PlayerID == localPlayerID;

        return new ClientGameStateSnapshot
        {
            CurrentPhase = serverSnapshot.CurrentPhase,
            WinnerPlayerID = serverSnapshot.WinnerPlayerID,
            LocalPlayerID = localPlayerID,
            ActivePlayerID = serverSnapshot.ActivePlayerID,
            ExpectedPlayerID = serverSnapshot.ExpectedPlayerID,

            LocalPlayerLife = localPlayer.Life,
            OpponentPlayerLife = opponentPlayer.Life,
            LocalPlayerMindbugCount = localPlayer.MindbugCount,
            OpponentPlayerMindbugCount = opponentPlayer.MindbugCount,
            LocalPlayerDeckCount = localPlayer.DeckCount,
            OpponentPlayerDeckCount = opponentPlayer.DeckCount,
            OpponentHandCount = opponentPlayer.Hand.Length,

            LocalPlayerHand = localPlayer.Hand,
            LocalPlayerField = localPlayer.Field,
            OpponentPlayerField = opponentPlayer.Field,
            LocalPlayerDiscard = localPlayer.DiscardPile,
            OpponentPlayerDiscard = opponentPlayer.DiscardPile,

            PendingCard = serverSnapshot.PendingCard,
            PendingAttackCard = serverSnapshot.PendingAttackCard,
            PendingTargetCard =
                serverSnapshot.PendingHunterTargetCard,

            HasPendingChoice = canSeePendingChoice,
            MaxSelectCount = canSeePendingChoice
                ? pendingChoice.MaxSelectCount
                : 0,
            MinSelectCount = canSeePendingChoice
                ? pendingChoice.MinSelectCount
                : 0,
            CandidateCardInstanceIDs = canSeePendingChoice
                ? pendingChoice.CandidateCardInstanceIDs
                : new int[0]
        };
    }

    [ClientRpc]
    private void ReceiveAnimationEventsClientRpc(
        ClientAnimationEvent[] animationEvents,
        ClientRpcParams clientRpcParams = default)
    {
        if(animationEvents.Length > 0)
        {
            clientCurrentPhase =
                animationEvents[animationEvents.Length - 1]
                    .StateAfterEvent.CurrentPhase;
        }

        viewController.ReceiveAnimationEvents(animationEvents);
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


}

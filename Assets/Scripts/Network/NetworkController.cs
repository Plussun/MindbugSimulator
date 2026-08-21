using Unity.Netcode;
using UnityEngine;

public class NetworkController : NetworkBehaviour
{
    public GameController GameController;
    private const ulong UnassignedClientId = ulong.MaxValue;
    private ulong player0ClientId = UnassignedClientId;
    private ulong player1ClientId = UnassignedClientId;
    NetworkManager networkManager;

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

    private void OnClientConnectedCallback(ulong clientId)
    {
        //如果是服务器端，则分配客户端ID给玩家
        if (IsServer)
        {
            if (clientId == networkManager.LocalClientId)
            {
                player0ClientId = clientId;
                return;
            }
            else
            {
                player1ClientId = clientId;
                return;
            }
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
        Debug.Log("玩家" + playerId + "使用夺心虫，决策为" + useMindbug);
    }




    //一些工具性的方法

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
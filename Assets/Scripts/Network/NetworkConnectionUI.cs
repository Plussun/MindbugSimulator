using System;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using Unity.Sync.Relay.Transport.Netcode;
using Unity.Sync.Relay.Model;
using Unity.Sync.Relay.Lobby;

public class NetworkConnectionUI : MonoBehaviour
{
    public GameObject ConnectionPanel;
    public TMP_Text ConnectionText;

    private const ushort Port = 7777;

    private NetworkManager networkManager;
    private UnityTransport unityTransport;
    private RelayTransportNetcode relayTransport;

    private string playerID;
    private string roomID;
    private bool useLocalNetworkDebug;

    private void Start()
    {
        networkManager = NetworkManager.Singleton;
        unityTransport = networkManager.GetComponent<UnityTransport>();
        relayTransport = networkManager.GetComponent<RelayTransportNetcode>();

        playerID = Guid.NewGuid().ToString();//生成唯一的玩家ID

        relayTransport.SetPlayerData(playerID,
        "player-"+playerID.Substring(0, 8));//设置玩家数据，使用前8位作为昵称

        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;

        ConnectionText.text = "请选择创建主机或加入游戏";
    }

    private void SelectTransport()
    {
        useLocalNetworkDebug = 
            ConnectionPanel.transform.Find("LocalToggle").GetComponent<Toggle>().isOn;
        if (useLocalNetworkDebug)
        {
            networkManager.NetworkConfig.NetworkTransport = unityTransport;
        }
        else
        {
            networkManager.NetworkConfig.NetworkTransport = relayTransport;
        }
    }

    public void StartHost()
    {
        SelectTransport();

        if (useLocalNetworkDebug)
        {
            StartLocalHost();
        }
        else
        {
            StartRelayHost();
        }
    }

    public void StartLocalHost()
    {
        unityTransport.SetConnectionData("127.0.0.1", Port, "0.0.0.0");

        if (!networkManager.StartHost())
        {
            ConnectionText.text = "主机创建失败";
            Debug.Log("主机创建失败");
            return;
        }

        ConnectionText.text = "主机已创建，等待玩家加入";
        Debug.Log("主机已创建，等待玩家加入");
    }
    public void StartRelayHost()
    {
        ConnectionText.text = "正在创建UOS房间，请稍候...";
        CreateRoomRequest request = new CreateRoomRequest();
        request.Name = "MindbugRoom";
        request.Namespace = "Mindbug";
        request.MaxPlayers = 2;
        request.Visibility = LobbyRoomVisibility.Public;
        request.OwnerId = playerID;

        StartCoroutine(
            LobbyService.AsyncCreateRoom(
                request, 
                OnRoomCreated
            )
        );
    }

    public void OnRoomCreated(CreateRoomResponse response)
    {
        if(response.Code != (uint)RelayCode.OK)
        {
            ConnectionText.text = 
                "UOS房间创建失败，错误码：" + response.Code;
            return;
        }
        if (response.Status != LobbyRoomStatus.ServerAllocated)
        {
            ConnectionText.text =
                "房间尚未准备好：" + response.Status;
            return;
        }

        roomID = response.RoomUuid;
        relayTransport.SetRoomData(response);

        if (!networkManager.StartHost())
        {
            ConnectionText.text = "主机创建失败";
            Debug.Log("主机创建失败");
            return;
        }
        ConnectionText.text = 
            "房间创建成功，房间码"+response.RoomCode;
    }

    public void StartClient()
    {
        SelectTransport();
        if (useLocalNetworkDebug)
        {
            StartLocalClient();
        }
        else
        {
            StartRelayClient();
        }
        
    }

    public void StartLocalClient()
    {
        unityTransport.SetConnectionData("127.0.0.1", Port);

        if (!networkManager.StartClient())
        {
            ConnectionText.text = "客户端启动失败";
            Debug.Log("客户端启动失败");
            return;
        }

        ConnectionText.text = "正在连接主机";
        Debug.Log("正在连接主机");
    }

    public void StartRelayClient()
    {
        string roomCode = 
            ConnectionPanel.transform.Find("RoomCodeInput").GetComponent<TMP_InputField>().text;

        if (string.IsNullOrEmpty(roomCode))
        {
            ConnectionText.text = "请输入房间码";
            return;
        }

        ConnectionText.text = "正在加入UOS房间，请稍候...";
        StartCoroutine(
            LobbyService.AsyncQueryRoomByRoomCode(
                roomCode, 
                OnRoomJoined
            )
        );
    }

    public void OnRoomJoined(QueryRoomResponse response)
    {
        if(response.Code != (uint)RelayCode.OK)
        {
            ConnectionText.text = 
                "UOS房间查询失败，错误码：" + response.Code;
            return;
        }
        if (response.Status != LobbyRoomStatus.Ready)
        {
            ConnectionText.text =
                "房间尚未准备好：" + response.Status;
            return;
        }

        roomID = response.RoomUuid;
        relayTransport.SetRoomData(response);

        if (!networkManager.StartClient())
        {
            ConnectionText.text = "客户端启动失败";
            Debug.Log("客户端启动失败");
            return;
        }

        ConnectionText.text = "正在连接主机";
        Debug.Log("正在连接主机");
    }

    public void Disconnect()
    {
        if (!networkManager.IsListening)
        {
            return;
        }

        //如果使用UOS房间
        if(!useLocalNetworkDebug &&
            networkManager.IsHost &&
            !string.IsNullOrEmpty(roomID))
        {
            StartCoroutine(
                LobbyService.AsyncCloseRoom(
                    roomID, 
                    OnRoomClosed
                )
            );
            return;//相关收尾处理中回调中实现了，这里就直接返回了
        }

        //如果是本地网络调试
        if (networkManager.IsListening)
        {
            networkManager.Shutdown();
        }

        ConnectionPanel.SetActive(true);
        ConnectionText.text = "已断开连接";
        Debug.Log("已断开连接");
    }

    private void OnRoomClosed(CloseRoomResponse response)
    {
        networkManager.Shutdown();
        roomID = null;
        ConnectionPanel.SetActive(true);
        ConnectionText.text = "房间已关闭";
        Debug.Log("房间已关闭");
    }

    private void OnClientConnected(ulong clientId)
    {
        if (networkManager.IsHost)
        {
            if (clientId == networkManager.LocalClientId)
            {
                ConnectionText.text = "主机已创建，等待玩家加入";
                Debug.Log("主机已创建，等待玩家加入");
            }
            else
            {
                ConnectionText.text = "玩家已加入游戏";
                Debug.Log("玩家已加入游戏，ClientId: " + clientId);
                ConnectionPanel.SetActive(false);
            }

            return;
        }

        if (clientId == networkManager.LocalClientId)
        {
            ConnectionText.text = "已成功加入游戏";
            Debug.Log("已成功加入游戏，ClientId: " + clientId);
            ConnectionPanel.SetActive(false);
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log("客户端已断开，ClientId: " + clientId);
        ConnectionPanel.SetActive(true);

        if (networkManager.IsHost && clientId != networkManager.LocalClientId)
        {
            ConnectionText.text = "玩家已离开游戏";
        }
        else
        {
            ConnectionText.text = "连接失败或已断开";
        }
    }


    private void OnDestroy()
    {
        if (networkManager == null)
        {
            return;
        }

        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnected;
    }
}

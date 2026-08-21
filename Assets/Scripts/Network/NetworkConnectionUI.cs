using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkConnectionUI : MonoBehaviour
{
    public GameObject ConnectionPanel;
    public TMP_Text ConnectionText;

    private const ushort Port = 7777;

    private NetworkManager networkManager;
    private UnityTransport unityTransport;

    private void Start()
    {
        networkManager = NetworkManager.Singleton;
        unityTransport = networkManager.GetComponent<UnityTransport>();

        networkManager.OnClientConnectedCallback += OnClientConnected;
        networkManager.OnClientDisconnectCallback += OnClientDisconnected;

        ConnectionText.text = "请选择创建主机或加入游戏";
    }

    public void StartHost()
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

    public void StartClient()
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

    public void Disconnect()
    {
        if (networkManager.IsListening)
        {
            networkManager.Shutdown();
        }

        ConnectionPanel.SetActive(true);
        ConnectionText.text = "已断开连接";
        Debug.Log("已断开连接");
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
            ConnectionText.text = "玩家已离开游戏，等待玩家加入";
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

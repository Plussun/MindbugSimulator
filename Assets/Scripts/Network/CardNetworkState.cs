using Unity.Netcode;
public struct CardNetworkState : INetworkSerializeByMemcpy
{
    public int CardInstanceID;
    public int CardDataID;
    public int currentPower;

    public bool isExhausted;

    public int keywords;
    
}
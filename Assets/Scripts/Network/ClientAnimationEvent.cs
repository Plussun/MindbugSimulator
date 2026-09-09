using Unity.Netcode;

// GameAnimationEvent经过玩家视角过滤后得到的网络版本。
// 客户端按SequenceID顺序播放，并在每次动画结束后应用StateAfterEvent。
[System.Serializable]
public struct ClientAnimationEvent : INetworkSerializable
{
    public int SequenceID;
    public GameAnimationType AnimationType;
    public int PlayerID;
    public int CardInstanceID;
    public ClientGameStateSnapshot StateAfterEvent;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref SequenceID);
        serializer.SerializeValue(ref AnimationType);
        serializer.SerializeValue(ref PlayerID);
        serializer.SerializeValue(ref CardInstanceID);
        serializer.SerializeValue(ref StateAfterEvent);
    }
}

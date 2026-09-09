using Unity.Netcode;

// 发送给某一名客户端的显示状态。
// 这里不存在对手手牌数组，只有数量，因此网络数据本身不会泄露对手手牌。
[System.Serializable]
public struct ClientGameStateSnapshot : INetworkSerializable
{
    public GamePhase CurrentPhase;
    public int WinnerPlayerID;
    public int LocalPlayerID;
    public int ActivePlayerID;
    public int ExpectedPlayerID;

    public int LocalPlayerLife;
    public int OpponentPlayerLife;
    public int LocalPlayerMindbugCount;
    public int OpponentPlayerMindbugCount;
    public int LocalPlayerDeckCount;
    public int OpponentPlayerDeckCount;
    public int OpponentHandCount;

    public CardNetworkState[] LocalPlayerHand;
    public CardNetworkState[] LocalPlayerField;
    public CardNetworkState[] OpponentPlayerField;
    public CardNetworkState[] LocalPlayerDiscard;
    public CardNetworkState[] OpponentPlayerDiscard;

    public CardNetworkState PendingCard;
    public CardNetworkState PendingAttackCard;
    public CardNetworkState PendingTargetCard;

    // 只有轮到本客户端选择时，服务器才会把这部分内容写入快照。
    public bool HasPendingChoice;
    public int MaxSelectCount;
    public int MinSelectCount;
    public int[] CandidateCardInstanceIDs;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref CurrentPhase);
        serializer.SerializeValue(ref WinnerPlayerID);
        serializer.SerializeValue(ref LocalPlayerID);
        serializer.SerializeValue(ref ActivePlayerID);
        serializer.SerializeValue(ref ExpectedPlayerID);

        serializer.SerializeValue(ref LocalPlayerLife);
        serializer.SerializeValue(ref OpponentPlayerLife);
        serializer.SerializeValue(ref LocalPlayerMindbugCount);
        serializer.SerializeValue(ref OpponentPlayerMindbugCount);
        serializer.SerializeValue(ref LocalPlayerDeckCount);
        serializer.SerializeValue(ref OpponentPlayerDeckCount);
        serializer.SerializeValue(ref OpponentHandCount);

        serializer.SerializeValue(ref LocalPlayerHand);
        serializer.SerializeValue(ref LocalPlayerField);
        serializer.SerializeValue(ref OpponentPlayerField);
        serializer.SerializeValue(ref LocalPlayerDiscard);
        serializer.SerializeValue(ref OpponentPlayerDiscard);

        serializer.SerializeValue(ref PendingCard);
        serializer.SerializeValue(ref PendingAttackCard);
        serializer.SerializeValue(ref PendingTargetCard);

        serializer.SerializeValue(ref HasPendingChoice);
        serializer.SerializeValue(ref MaxSelectCount);
        serializer.SerializeValue(ref MinSelectCount);
        serializer.SerializeValue(ref CandidateCardInstanceIDs);
    }
}

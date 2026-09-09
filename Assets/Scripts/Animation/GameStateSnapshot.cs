using System.Collections.Generic;

// 用于动画与显示的状态副本，不是可以继续运行规则的 GameState，也不是存档。
// 副本中只保存纯数值和新建的数组，因此之后修改 GameState 不会改变旧事件中的状态。
[System.Serializable]
public class GameStateSnapshot
{
    public GamePhase CurrentPhase;
    public int ActivePlayerID;
    public int ExpectedPlayerID;
    public int WinnerPlayerID;

    public PlayerStateSnapshot[] Players;

    public CardNetworkState PendingCard;
    public CardNetworkState PendingAttackCard;
    public CardNetworkState PendingBlockCard;
    public CardNetworkState PendingHunterTargetCard;

    public PendingChoiceSnapshot PendingChoice;

    // 在规则事件完成后调用，冻结这一刻显示端需要的完整状态。
    // 此处生成的是服务器内部副本；发送前仍要按接收玩家隐藏对手手牌和他人的选择信息。
    public static GameStateSnapshot Capture(GameState state)
    {
        PlayerStateSnapshot[] players = new PlayerStateSnapshot[state.Players.Count];
        for (int i = 0; i < state.Players.Count; i++)
        {
            players[i] = PlayerStateSnapshot.Capture(state.Players[i]);
        }

        return new GameStateSnapshot
        {
            CurrentPhase = state.CurrentPhase,
            ActivePlayerID = state.ActivePlayerID,
            ExpectedPlayerID = state.ExpectedPlayerID,
            WinnerPlayerID = state.WinnerPlayerID,
            Players = players,
            PendingCard = CaptureCard(state.PendingCardInstance),
            PendingAttackCard = CaptureCard(state.PendingAttackCardInstance),
            PendingBlockCard = CaptureCard(state.PendingBlockCardInstance),
            PendingHunterTargetCard = CaptureCard(state.PendingHunterTargetCardInstance),
            PendingChoice = PendingChoiceSnapshot.Capture(state.PendingChoice)
        };
    }

    internal static CardNetworkState CaptureCard(CardInstance card)
    {
        if (card == null || card.CardData == null)
        {
            return new CardNetworkState
            {
                CardInstanceID = -1
            };
        }

        return new CardNetworkState
        {
            CardInstanceID = card.CardInstanceID,
            CardDataID = card.CardData.CardDataID,
            currentPower = card.CurrentPower,
            isExhausted = card.IsExhausted,
            keywords = (int)card.CurrentKeywords
        };
    }

    internal static CardNetworkState[] CaptureCards(List<CardInstance> cards)
    {
        CardNetworkState[] cardSnapshots = new CardNetworkState[cards.Count];
        for (int i = 0; i < cards.Count; i++)
        {
            cardSnapshots[i] = CaptureCard(cards[i]);
        }

        return cardSnapshots;
    }
}

[System.Serializable]
public class PlayerStateSnapshot
{
    public int PlayerID;
    public int Life;
    public int MindbugCount;
    public int DeckCount;

    public CardNetworkState[] Hand;
    public CardNetworkState[] Field;
    public CardNetworkState[] DiscardPile;

    public static PlayerStateSnapshot Capture(PlayerState player)
    {
        return new PlayerStateSnapshot
        {
            PlayerID = player.PlayerID,
            Life = player.Life,
            MindbugCount = player.MindbugCount,
            DeckCount = player.Deck.Count,
            Hand = GameStateSnapshot.CaptureCards(player.Hand),
            Field = GameStateSnapshot.CaptureCards(player.Field),
            DiscardPile = GameStateSnapshot.CaptureCards(player.DiscardPile)
        };
    }
}

[System.Serializable]
public class PendingChoiceSnapshot
{
    public int PlayerID;
    public int MaxSelectCount;
    public int MinSelectCount;
    public int[] CandidateCardInstanceIDs;

    public static PendingChoiceSnapshot Capture(PendingChoice pendingChoice)
    {
        if (pendingChoice == null)
        {
            return null;
        }

        return new PendingChoiceSnapshot
        {
            PlayerID = pendingChoice.PlayerID,
            MaxSelectCount = pendingChoice.MaxSelectCount,
            MinSelectCount = pendingChoice.MinSelectCount,
            CandidateCardInstanceIDs = pendingChoice.CandidateCardInstanceIDs != null
                ? pendingChoice.CandidateCardInstanceIDs.ToArray()
                : new int[0]
        };
    }
}

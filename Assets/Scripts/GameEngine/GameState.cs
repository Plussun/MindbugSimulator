
using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public class GameState
{
    public GamePhase CurrentPhase;
    public List<PlayerState> Players = new List<PlayerState>();
    public int ActivePlayerID;//当前回合的玩家
    public int ExpectedPlayerID;//等待玩家决定（使用bug或者抵挡）的玩家

    public CardInstance PendingCardInstance; // 当前正在等待Mindbug决策的卡牌实例
    public CardInstance PendingBlockCardInstance; // 当前正在等待Block决策的卡牌实例

    public GameState()
    {
        CurrentPhase = GamePhase.Setup;
        Players.Add(new PlayerState(0));
        Players.Add(new PlayerState(1));
    }
}

public enum GamePhase
{
    Setup,
    WaitingForMainAction,
    WaitingForMindbugDecision,
    WaitingForBlockDecision,
    GameOver
}
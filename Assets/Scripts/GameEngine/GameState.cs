
using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public class GameState
{
    public GamePhase CurrentPhase;
    public List<CardInstance> AllCards = new List<CardInstance>();
    public List<PlayerState> Players = new List<PlayerState>();
    public int ActivePlayerID;//当前回合的玩家
    public int ExpectedPlayerID;//等待玩家决定（使用bug或者抵挡）的玩家
    public int WinnerPlayerID = -1; // 游戏结束时的获胜玩家ID，-1表示游戏未结束

    public CardInstance PendingCardInstance; // 当前正在等待Mindbug决策的卡牌实例
    public CardInstance PendingAttackCardInstance; // 当前正在Attack决策的攻击卡牌实例
    public CardInstance PendingBlockCardInstance; // 当前正在Block决策的卡牌实例
    public CardInstance PendingHunterTargetCardInstance; // 当前正在Hunter决策的目标卡牌实例

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
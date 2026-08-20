
using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public class GameState
{
    public GamePhase CurrentPhase;
    public List<PlayerState> Players = new List<PlayerState>();

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
    GameOver
}
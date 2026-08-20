using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class GameEngine
{
    public GameState GameState;
    public GameEngine()
    {
        GameState = new GameState();
    }
}

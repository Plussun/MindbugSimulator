using System.Collections.Generic;
[System.Serializable]
public class PlayerState
{
    public int PlayerID;
    public List<CardInstance> Hand = new List<CardInstance>();
    public List<CardInstance> Deck = new List<CardInstance>();
    public List<CardInstance> Field = new List<CardInstance>();
    public List<CardInstance> DiscardPile = new List<CardInstance>();
    public int Life = 3;
    public int MindbugCount = 2;

    public PlayerState(int playerID)
    {
        PlayerID = playerID;
    }
}
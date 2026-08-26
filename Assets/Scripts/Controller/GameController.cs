using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public GameEngine GameEngine;
    public List<CardData> TestHand;
    public List<CardData> CardDatabase; // 卡牌数据库
    // Start is called before the first frame update
    void Awake()
    {
        GameEngine = new GameEngine();
        GameEngine.SetAllCards(GetRandomAllCards(48));
    }

    public List<CardData> GetRandomAllCards(int allCardsCount)
    {
        List<CardData> randomCards = new List<CardData>();
        for (int i = 0; i < allCardsCount; i++)
        {
            int randomIndex = Random.Range(0, CardDatabase.Count);
            randomCards.Add(CardDatabase[randomIndex]);
        }
        return randomCards;
    }
}

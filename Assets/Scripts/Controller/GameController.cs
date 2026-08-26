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
        GameEngine.SetPlayerDeck(0, TestHand);
        GameEngine.SetPlayerDeck(1, TestHand);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

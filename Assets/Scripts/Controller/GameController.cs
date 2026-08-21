using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public GameEngine GameEngine;
    public List<CardData> TestHand;
    // Start is called before the first frame update
    void Awake()
    {
        GameEngine = new GameEngine();
        GameEngine.SetPlayerHand(0, TestHand);
        GameEngine.SetPlayerHand(1, TestHand);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

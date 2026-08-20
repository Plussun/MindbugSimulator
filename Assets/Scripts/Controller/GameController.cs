using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public GameEngine GameEngine;
    // Start is called before the first frame update
    void Start()
    {
        GameEngine = new GameEngine();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}

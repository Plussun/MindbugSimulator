using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Unity.Netcode;

public class TestGameEngine : MonoBehaviour
{
    public GameController GameController;

    public TMP_InputField TestInputField;
    public Button TestButton;
    NetworkManager networkManager;
    public NetworkController NetworkController;

    // Start is called before the first frame update
    void Start()
    {
        networkManager = NetworkManager.Singleton;
    }


    public void OnTestButtonClick()
    {
        string inputText = TestInputField.text;
        switch(inputText)
        {
            case "start":
                GameController.GameEngine.StartGame();
                break;
            case string s when s.StartsWith("play"):
                string playCardID = s.Substring(4).Trim();
                NetworkController.PlayCardRequest(int.Parse(playCardID));
                break;
            case string s when s.StartsWith("attack"):
                string attackCardID = s.Substring(6).Trim();
                NetworkController.AttackDecisionRequest(int.Parse(attackCardID));
                break;
            case "noblock":
                NetworkController.BlockDecisionRequest(false, 0);
                break;
            case string s when s.StartsWith("block"):
                string blockCardID = s.Substring(5).Trim();
                NetworkController.BlockDecisionRequest(true, int.Parse(blockCardID));
                break;
            case "bug":
                NetworkController.MindbugDecisionRequest(true);
                break;
            case "nobug":
                NetworkController.MindbugDecisionRequest(false);
                break;
            case "change":
                GameController.GameEngine.ChangeActivePlayer(
                    1-GameController.GameEngine.State.ActivePlayerID);
                break;
            case "end":
                GameController.GameEngine.ChangeGamePhase(GamePhase.GameOver);
                break;
            default:
                Debug.Log("未知的测试命令");
                break;
        }
    }
}

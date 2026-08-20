using UnityEngine;
[CreateAssetMenu(fileName = "CardData", menuName = "MindBug/CardData")]
public class CardData:ScriptableObject
{
    public string CardName;
    public int CardDataID;
    public int Power;
    public string Description = "No description provided.";
}
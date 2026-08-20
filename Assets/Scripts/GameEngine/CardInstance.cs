[System.Serializable]
public class CardInstance
{
    public CardData CardData;
    public int CardInstanceID;
    public int currentPower;
    public CardInstance(CardData cardData, int cardInstanceID)
    {
        CardData = cardData;
        CardInstanceID = cardInstanceID;
        currentPower = cardData.Power;
    }
}
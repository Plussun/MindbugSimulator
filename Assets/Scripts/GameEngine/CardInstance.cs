[System.Serializable]
public class CardInstance
{
    public CardData CardData;
    public int CardInstanceID;
    public int CurrentPower;
    public CardInstance(CardData cardData, int cardInstanceID)
    {
        CardData = cardData;
        CardInstanceID = cardInstanceID;
        CurrentPower = cardData.Power;
    }
}
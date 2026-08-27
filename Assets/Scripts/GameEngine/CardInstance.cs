[System.Serializable]
public class CardInstance
{
    public CardData CardData;
    public int CardInstanceID;
    public int CurrentPower = 0;
    public int BasePower = 0;
    public int TempPower = 0;

    public Keywords CurrentKeywords = Keywords.None;
    public Keywords BaseKeywords = Keywords.None;
    public Keywords TempKeywords = Keywords.None;
    public CardInstance(CardData cardData, int cardInstanceID)
    {
        CardData = cardData;
        CardInstanceID = cardInstanceID;
        BasePower = CardData.Power;
        BaseKeywords = CardData.CardKeywords;
        UpdatePowerAndKeywords();
    }

    public void UpdatePowerAndKeywords()
    {
        CurrentPower = BasePower + TempPower;
        CurrentKeywords = BaseKeywords|TempKeywords;
    }

    public void ClearTempEffects()
    {
        TempPower = 0;
        TempKeywords = Keywords.None;
        UpdatePowerAndKeywords();
    }
}
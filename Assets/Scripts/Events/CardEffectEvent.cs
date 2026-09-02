using UnityEngine;
public class CardEffectEvent : GameEvent
{
    private CardEffect effect;
    public int OwnerPlayerID;
    public int CardInstanceID;

    public CardEffectEvent(CardEffect cardEffect, int ownerPlayerID, int cardInstanceID)
    {
        effect = cardEffect;
        OwnerPlayerID = ownerPlayerID;
        CardInstanceID = cardInstanceID;
    }

    public override void Resolve(GameEngine gameEngine)
    {
        CardInstance cardInstance = gameEngine.GetCardInstanceByID(CardInstanceID);
        if (cardInstance == null)
        {
            Debug.LogError("找不到卡牌实例");
            return;
        }

        effect.Resolve(gameEngine, OwnerPlayerID, cardInstance);
    }
}
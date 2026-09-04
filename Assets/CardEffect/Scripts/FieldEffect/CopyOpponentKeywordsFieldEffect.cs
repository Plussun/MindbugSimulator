using UnityEngine;

[CreateAssetMenu(
    fileName = "CardEffect",
    menuName = "MindBug/CardEffect/CopyOpponentKeywordsFieldEffect")]
public class CopyOpponentKeywordsFieldEffect : CardFieldEffect
{
    //复制效果需要读取其他场上效果已经计算完成的关键词。
    public override bool ResolveAfterKeywordUpdate => true;

    public override void Resolve(
        GameEngine gameEngine,
        int ownerPlayerID,
        CardInstance cardInstance)
    {
        int opponentPlayerID = 1 - ownerPlayerID;
        Keywords copyableKeywords =
            Keywords.Hunter |
            Keywords.Sneaky |
            Keywords.Frenzy |
            Keywords.Poisonous;
        Keywords copiedKeywords = Keywords.None;

        foreach(CardInstance opponentCard in
            gameEngine.State.Players[opponentPlayerID].Field)
        {
            copiedKeywords |= opponentCard.CurrentKeywords & copyableKeywords;
        }

        //这里只写入TempKeywords，等全部复制效果处理完后再统一更新CurrentKeywords。
        cardInstance.TempKeywords |= copiedKeywords;
    }
}

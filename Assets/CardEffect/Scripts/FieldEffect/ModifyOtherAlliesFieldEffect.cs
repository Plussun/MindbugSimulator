using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(
    fileName = "CardEffect",
    menuName = "MindBug/CardEffect/ModifyOtherAlliesFieldEffect")]
public class ModifyOtherAlliesFieldEffect : CardFieldEffect
{
    public int PowerToAdd;
    public Keywords KeywordsToAdd;

    public bool OnlyOnOwnerTurn;
    public int MinPower = -1; //-1表示没有最低力量限制
    public int MaxPower = -1; //-1表示没有最高力量限制

    //需要按力量筛选关键词时，先等待其他力量光环计算完成。
    public override bool ResolveAfterPowerUpdate =>
        KeywordsToAdd != Keywords.None &&
        (MinPower != -1 || MaxPower != -1);

    public override void Resolve(
        GameEngine gameEngine,
        int ownerPlayerID,
        CardInstance cardInstance)
    {
        if(OnlyOnOwnerTurn && gameEngine.State.ActivePlayerID != ownerPlayerID)
        {
            return;
        }

        List<CardInstance> playerField =
            gameEngine.State.Players[ownerPlayerID].Field;

        foreach(CardInstance card in playerField)
        {
            //只影响其他己方生物，不包括效果来源自己。
            if(card == cardInstance)
            {
                continue;
            }
            if(MinPower != -1 && card.CurrentPower < MinPower)
            {
                continue;
            }
            if(MaxPower != -1 && card.CurrentPower > MaxPower)
            {
                continue;
            }

            card.TempPower += PowerToAdd;
            card.TempKeywords |= KeywordsToAdd;
        }
    }
}

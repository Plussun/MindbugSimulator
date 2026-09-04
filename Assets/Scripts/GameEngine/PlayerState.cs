using System.Collections.Generic;

[System.Serializable]
public class IndividualBlockRestriction
{
    public int AttackCardInstanceID;
    public List<int> ForbiddenBlockerInstanceIDs = new List<int>();

    public IndividualBlockRestriction(int attackCardInstanceID)
    {
        AttackCardInstanceID = attackCardInstanceID;
    }
}

[System.Serializable]
public class PlayerState
{
    public int PlayerID;
    public List<CardInstance> Hand = new List<CardInstance>();
    public List<CardInstance> Deck = new List<CardInstance>();
    public List<CardInstance> Field = new List<CardInstance>();
    public List<CardInstance> DiscardPile = new List<CardInstance>();
    // 该玩家无论面对哪张攻击卡都不能用于阻挡的卡牌。
    public List<int> GlobalForbiddenBlockerInstanceIDs = new List<int>();
    // 该玩家面对特定攻击卡时不能用于阻挡的卡牌。
    public List<IndividualBlockRestriction> IndividualBlockRestrictions =
        new List<IndividualBlockRestriction>();
    // 当前被场上效果禁止触发的卡牌效果时机。
    public List<EffectTrigger> DisabledEffectTriggers = new List<EffectTrigger>();
    public int Life = 3;
    public int MindbugCount = 2;

    public PlayerState(int playerID)
    {
        PlayerID = playerID;
    }
}

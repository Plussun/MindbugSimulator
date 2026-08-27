using UnityEngine;
using System.Collections.Generic;
[CreateAssetMenu(fileName = "CardData", menuName = "MindBug/CardData")]
public class CardData:ScriptableObject
{
    public string CardName;
    public int CardDataID;
    public int Power;
    public string Description = "No description provided.";

    public Keywords CardKeywords;

    public List<CardEffect> CardEffects = new List<CardEffect>();
    public List<CardFieldEffect> CardFieldEffects = new List<CardFieldEffect>();
}

// Flags 枚举让一张卡可以同时拥有多个关键词。
// 每个关键词占一个独立的二进制位，因此数值必须写成 1 << 0、1 << 1、1 << 2……，不能重复。
// 组合关键词：Keywords.Sneaky | Keywords.Poisonous
// 判断关键词：(keywords & Keywords.Sneaky) != 0，或 keywords.HasFlag(Keywords.Sneaky)
// 添加关键词：keywords |= Keywords.Tough；移除关键词：keywords &= ~Keywords.Tough
// None 必须为 0，表示没有任何关键词。
[System.Flags]
public enum Keywords
{
    None = 0,
    Sneaky = 1 << 0,//敏捷只能被敏捷阻挡
    Poisonous = 1 << 1,//中毒造成伤害就会击杀对方
    Tough = 1 << 2,//坚韧受到击杀时会横置，第二次被击杀才会退场
    Frenzy = 1 << 3,//狂暴一回合可以攻击两次
    Hunter = 1 << 4,//猎人可以选择对方特定生物进行阻挡
}

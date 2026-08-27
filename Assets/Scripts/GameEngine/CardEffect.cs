using UnityEngine;

//此处是卡牌效果基类
//每个基础卡牌效果需要继承此类并编写脚本，然后生成对应asset
//最后在CardData中添加对应的CardEffect asset
public abstract class CardEffect:ScriptableObject
{
    public EffectTrigger Trigger;

    public abstract void Resolve(
        GameEngine gameEngine, 
        int ownerPlayerID, 
        CardInstance cardInstance);
}


public enum EffectTrigger
{
    OnDeploy,//当卡牌被打出时触发
    OnField,//当卡牌在场上时触发
    OnAttack,//当卡牌发动攻击时触发
    OnBlock,//当卡牌阻挡时触发
    OnDefeat,//当卡牌被击败时触发
}
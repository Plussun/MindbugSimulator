using UnityEngine;

public abstract class CardFieldEffect:ScriptableObject
{
    //光环类效果的基类，因为触发条件显著不同，所以单独抽象出来
    //依赖力量判断的关键词效果需要在普通力量效果更新后执行。
    public virtual bool ResolveAfterPowerUpdate => false;

    //复制关键词需要等普通关键词效果全部更新后执行，
    //否则结果会受到场上卡牌遍历顺序影响。
    public virtual bool ResolveAfterKeywordUpdate => false;

    //阻挡限制需要等力量、普通关键词和复制关键词全部更新后再计算。
    public virtual bool ResolveAfterCardUpdate => false;

    public abstract void Resolve(
        GameEngine gameEngine, 
        int ownerPlayerID, 
        CardInstance cardInstance);
}

using UnityEngine;

public abstract class CardFieldEffect:ScriptableObject
{
    //光环类效果的基类，因为触发条件显著不同，所以单独抽象出来
    public abstract void Resolve(
        GameEngine gameEngine, 
        int ownerPlayerID, 
        CardInstance cardInstance);
}



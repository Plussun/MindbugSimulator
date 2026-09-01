using System.Collections.Generic;
public abstract class GameEvent
{
    public abstract void Resolve(GameEngine gameEngine);
    public virtual void ResolveChoice(GameEngine gameEngine, 
        List<int> selectedCardInstanceIDs)
    {
        // 默认实现为空，子类可以根据需要覆盖此方法
    }
}
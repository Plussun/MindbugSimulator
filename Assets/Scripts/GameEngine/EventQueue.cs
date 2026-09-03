using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public class EventQueue
{
    public List<GameEvent> Events { get; private set; }
    public GameEvent WaitingEvent;

    public EventQueue()
    {
        Events = new List<GameEvent>();
    }

    public void Enqueue(GameEvent gameEvent)
    {
        Events.Add(gameEvent);
    }

    public void EnqueueNext(GameEvent gameEvent)
    {
        Events.Insert(0, gameEvent);
    }
    public void EqueueNextRange(List<GameEvent> gameEvents)
    {
        Events.InsertRange(0, gameEvents);
    }

    public void ProcessEvent(GameEngine gameEngine)
    {
        while (WaitingEvent == null &&Events.Count > 0)
        {
            GameEvent nextEvent = Events[0];
            Events.RemoveAt(0);
            nextEvent.Resolve(gameEngine);
        }
    }
    //这里的数据流是，如果有需要选择目标的事件，事件会调用RequestChoice方法
    //把该事件先暂存于waitingEvent中，然后把游戏状态改为等待玩家选择,
    //把待选择的备选卡牌ID存在pendingChoice中
    //接着玩家通过UI和网络选择目标，然后调用SubmitChoice方法，传回选择的卡牌ID数组
    //接着继续进行事件的ResolveChoice方法，从而完成有目标的事件处理
    //事件完成后清理Wa1itingEvent，继续处理事件队列

    public void RequestChoice(PendingChoice pendingChoice, GameEngine gameEngine,
        GameEvent gameEvent)
    {
        WaitingEvent = gameEvent;
        gameEngine.State.CurrentPhase = GamePhase.WaitingForChoice;
        gameEngine.State.ExpectedPlayerID = pendingChoice.PlayerID;//切换到需要选择的玩家
        gameEngine.State.PendingChoice = pendingChoice;
    }

    public void SubmitChoice(GameEngine gameEngine, List<int> selectedCardInstanceIDs)
    {
        if (WaitingEvent != null)
        {
            if(selectedCardInstanceIDs.Count < gameEngine.State.PendingChoice.MinSelectCount ||
                selectedCardInstanceIDs.Count > gameEngine.State.PendingChoice.MaxSelectCount)
            {
                Debug.LogWarning("选择的卡牌数量不符合要求");
                return;
            }
            if(selectedCardInstanceIDs.Distinct().Count() != selectedCardInstanceIDs.Count)
            {
                Debug.LogWarning("不允许选择重复卡牌");
                return;
            }
            foreach (var id in selectedCardInstanceIDs)
            {
                if (!gameEngine.State.PendingChoice.CandidateCardInstanceIDs.Contains(id))
                {
                    Debug.LogWarning("选择的卡牌ID不在备选列表中");
                    return;
                }
            }

            GameEvent eventToResume = WaitingEvent;
            WaitingEvent = null;
            gameEngine.State.PendingChoice = null;
            eventToResume.ResolveChoice(gameEngine, selectedCardInstanceIDs);
        
            ProcessEvent(gameEngine);
        }
    }
}
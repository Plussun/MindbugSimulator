using System.Collections;
using UnityEngine;

// 只负责播放单条显示动画。
// 动画队列、状态刷新和整体播放流程由ViewController负责。
public class ViewAnimationPlayer : MonoBehaviour
{
    public IEnumerator PlayAnimation(ClientAnimationEvent animationEvent)
    {
        switch(animationEvent.AnimationType)
        {
            case GameAnimationType.DrawCard:
                yield return PlayDrawAnimation(animationEvent);
                break;

            case GameAnimationType.DiscardCard:
                yield return PlayDiscardAnimation(animationEvent);
                break;

            case GameAnimationType.StateRefresh:
                // 普通状态刷新没有动画，直接交还给ViewController处理最终状态。
                yield break;
        }
    }

    private IEnumerator PlayDrawAnimation(ClientAnimationEvent animationEvent)
    {
        Debug.Log("玩家" + animationEvent.PlayerID + "的卡牌"
            + animationEvent.CardInstanceID + "播放了抽牌动画");

        // 暂时等待一帧模拟动画，之后在这里替换为实际抽牌动画。
        yield return new WaitForSeconds(1f);
    }

    private IEnumerator PlayDiscardAnimation(ClientAnimationEvent animationEvent)
    {
        Debug.Log("玩家" + animationEvent.PlayerID + "的卡牌"
            + animationEvent.CardInstanceID + "播放了弃牌动画");

        // 暂时等待一帧模拟动画，之后在这里替换为实际弃牌动画。
        yield return null;
    }
}

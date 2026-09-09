using System.Collections;
using System.Collections.Generic;

public partial class ViewController
{
    public ViewAnimationPlayer AnimationPlayer;

    private readonly Queue<ClientAnimationEvent> animationQueue =
        new Queue<ClientAnimationEvent>();
    private bool isProcessingAnimation;

    // 网络层只需要把该玩家可见的动画事件交给这个入口。
    // 如果之前的动画还没有播放完，新事件会直接追加到现有队列末尾。
    public void ReceiveAnimationEvents(ClientAnimationEvent[] animationEvents)
    {
        foreach(ClientAnimationEvent animationEvent in animationEvents)
        {
            animationQueue.Enqueue(animationEvent);
        }

        if(!isProcessingAnimation)
        {
            StartCoroutine(ProcessAnimationQueue());
        }
    }

    private IEnumerator ProcessAnimationQueue()
    {
        isProcessingAnimation = true;

        while(animationQueue.Count > 0)
        {
            ClientAnimationEvent animationEvent = animationQueue.Dequeue();

            // 等待本条动画播放完成，再应用该事件完成后的状态副本。
            yield return AnimationPlayer.PlayAnimation(animationEvent);
            RefreshSnapShot(animationEvent.StateAfterEvent);
        }

        isProcessingAnimation = false;
    }
}

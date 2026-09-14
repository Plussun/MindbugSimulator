using UnityEngine;

/// <summary>
/// 集中管理游戏背景音乐和界面/卡牌音效。
/// 背景音乐与音效使用不同的 AudioSource，避免播放音效时中断 BGM。
/// </summary>
[DisallowMultipleComponent]
public class GameAudioPlayer : MonoBehaviour
{
    [Header("背景音乐")]
    [Tooltip("显示层收到新一局开始事件后循环播放的背景音乐。")]
    public AudioClip BackgroundMusic;

    [Range(0f, 1f)]
    [Tooltip("背景音乐音量。")]
    public float BackgroundMusicVolume = 0.5f;

    [Header("游戏音效")]
    [Tooltip("任意一方从手牌打出卡牌时播放的音效。")]
    public AudioClip PlayCardSound;

    [Range(0f, 1f)]
    [Tooltip("出牌音效音量。")]
    public float SoundEffectVolume = 1f;

    private AudioSource backgroundMusicSource;
    private AudioSource soundEffectSource;

    private void Awake()
    {
        // 分开创建两个声音通道：BGM 持续循环，短音效可以叠加播放。
        backgroundMusicSource = gameObject.AddComponent<AudioSource>();
        backgroundMusicSource.playOnAwake = false;
        backgroundMusicSource.loop = true;
        backgroundMusicSource.spatialBlend = 0f;
        backgroundMusicSource.volume = BackgroundMusicVolume;

        soundEffectSource = gameObject.AddComponent<AudioSource>();
        soundEffectSource.playOnAwake = false;
        soundEffectSource.loop = false;
        soundEffectSource.spatialBlend = 0f;
        soundEffectSource.volume = SoundEffectVolume;
    }

    /// <summary>
    /// 显示层收到 GameStart 动画事件后调用。
    /// 无论当前是否正在播放，都会先停止并回到0秒，确保每一局从头播放。
    /// </summary>
    public void RestartBackgroundMusic()
    {
        if(BackgroundMusic == null)
        {
            return;
        }

        backgroundMusicSource.Stop();
        backgroundMusicSource.clip = BackgroundMusic;
        backgroundMusicSource.time = 0f;
        backgroundMusicSource.Play();
    }

    /// <summary>
    /// 播放一次出牌音效。PlayOneShot 不会中断 BGM，连续出牌时音效也能重叠。
    /// </summary>
    public void PlayCard()
    {
        if(PlayCardSound != null)
        {
            soundEffectSource.PlayOneShot(PlayCardSound);
        }
    }
}

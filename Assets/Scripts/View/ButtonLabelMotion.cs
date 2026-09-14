using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>让文字跟随 Sprite Swap 实际显示的图片，保持瞬间切换同步。</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class ButtonLabelMotion : MonoBehaviour
{
    [Tooltip("按钮的 TMP 子对象；留空时自动查找。不要指定按钮自身。")]
    public TMP_Text Label;

    [Tooltip("悬停图片显示时，相对文字初始位置的偏移（UI 单位）。")]
    public Vector2 HoverOffset = Vector2.zero;

    [Tooltip("按下图片显示时，相对文字初始位置的偏移（UI 单位）。")]
    public Vector2 PressedOffset = new Vector2(0f, -3f);

    [Min(0.01f)]
    [Tooltip("按下时相对文字初始大小的缩放。只变暗的素材可设为 1。")]
    public float PressedScale = 0.97f;

    private Button button;
    private RectTransform labelRect;
    private Vector2 initialPosition;
    private Vector3 initialScale;

    private void Awake()
    {
        button = GetComponent<Button>();
        if(Label == null)
        {
            Label = GetComponentInChildren<TMP_Text>(true);
        }

        if(Label == null || Label.transform == transform ||
            !Label.transform.IsChildOf(transform))
        {
            Debug.LogWarning("ButtonLabelMotion 需要指定按钮下的 TMP 文字子对象。", this);
            enabled = false;
            return;
        }

        labelRect = Label.rectTransform;
        initialPosition = labelRect.anchoredPosition;
        initialScale = labelRect.localScale;
    }

    private void LateUpdate()
    {
        if(labelRect == null)
        {
            return;
        }

        Vector2 offset = Vector2.zero;
        float scale = 1f;
        Image targetImage = button.targetGraphic as Image;

        // 读取实际 Sprite Swap 结果，也涵盖键盘提交和 Selected 状态。
        // 禁用 Button、CanvasGroup 禁止交互或改用其他过渡时恢复原样。
        if(button.isActiveAndEnabled && button.IsInteractable() &&
            button.transition == Selectable.Transition.SpriteSwap && targetImage != null)
        {
            Sprite currentSprite = targetImage.overrideSprite;
            SpriteState sprites = button.spriteState;
            if(sprites.pressedSprite != null && currentSprite == sprites.pressedSprite)
            {
                offset = PressedOffset;
                scale = PressedScale;
            }
            else if(sprites.highlightedSprite != null &&
                currentSprite == sprites.highlightedSprite)
            {
                offset = HoverOffset;
            }
        }

        labelRect.anchoredPosition = initialPosition + offset;
        labelRect.localScale = initialScale * scale;
    }

    private void OnDisable()
    {
        if(labelRect != null)
        {
            labelRect.anchoredPosition = initialPosition;
            labelRect.localScale = initialScale;
        }
    }
}

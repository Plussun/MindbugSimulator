using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 使用固定的普通态 Sprite 做透明度命中检测。
/// Sprite Swap 可以继续切换悬停、按下图片，但不会改变按钮的点击轮廓。
/// 挂在按钮 Image 所在对象上；目前支持 Simple Image、未打包的 Sprite。
/// 普通态图片需要在导入设置中开启 Read/Write，且不能加入 Sprite Atlas。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Image))]
public class ButtonHit : MonoBehaviour, ICanvasRaycastFilter
{
    [Tooltip("固定用于点击检测的普通态图片。留空时，在 Awake 中记录 Image 的 Source Image。")]
    public Sprite NormalSprite;

    [Range(0f, 1f)]
    [Tooltip("像素透明度至少达到此值才能点击。0.1 可排除透明留白；0 表示使用显示矩形。")]
    public float AlphaThreshold = 0.1f;

    private Image buttonImage;
    private bool hasReportedConfigurationError;

    private void Awake()
    {
        buttonImage = GetComponent<Image>();

        // sprite 是 Inspector 中的 Source Image；overrideSprite 才是 Sprite Swap
        // 当前覆盖显示的图片。只记录普通态，不能每次点击都读取 overrideSprite。
        if(NormalSprite == null)
        {
            NormalSprite = buttonImage.sprite;
        }

        // Image 自己也是一个射线过滤器。必须关闭它的 Alpha 检测，
        // 否则即使本脚本判断命中，它仍可能根据按下态的透明像素拒绝点击。
        buttonImage.alphaHitTestMinimumThreshold = 0f;
    }

    /// <summary>
    /// Unity 的 UI 射线检测会调用此方法：true 允许命中，false 忽略此位置。
    /// 子对象文字的射线也会经过父对象上的过滤器，因此文字不会绕过轮廓检测。
    /// 此处只判断是否命中，不调用 onClick，也不改变底图、文字或按钮的位置。
    /// </summary>
    public bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
    {
        // 过滤器被禁用时不拦截射线，恢复普通的矩形 UI 点击行为。
        if(!isActiveAndEnabled || buttonImage == null)
        {
            return true;
        }

        // 不可读纹理无法进行 CPU 像素采样；打包后的 Sprite 还可能被旋转或裁切。
        // 本脚本只处理 Simple、未打包的图片，避免错误套用坐标公式。
        // 配置有误时只警告一次，并放行矩形点击，避免按钮彻底无法操作。
        if(NormalSprite == null || buttonImage.type != Image.Type.Simple ||
            NormalSprite.packed || !NormalSprite.texture.isReadable)
        {
            if(!hasReportedConfigurationError)
            {
                Debug.LogWarning(
                    "ButtonHit：请指定普通态图片，将 Image Type 设为 Simple，" +
                    "开启普通态纹理的 Read/Write，并将其移出 Sprite Atlas。" +
                    "当前回退为矩形点击检测。", this);
                hasReportedConfigurationError = true;
            }
            return true;
        }

        RectTransform rectTransform = buttonImage.rectTransform;

        // 屏幕坐标转换为 Image 自身的局部坐标，Unity 会处理旋转、缩放和 Canvas。
        // 使用事件系统传来的相机；Overlay Canvas 下 eventCamera 为 null 是正常的。
        if(!RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rectTransform, screenPoint, eventCamera, out Vector2 localPoint))
        {
            return false;
        }

        Rect rect = buttonImage.GetPixelAdjustedRect();
        Rect spriteRect = NormalSprite.rect;
        if(rect.width <= 0f || rect.height <= 0f)
        {
            return false;
        }

        // Preserve Aspect 会在 RectTransform 内按原图比例显示图片，产生留白。
        // 根据普通态的比例求出实际显示矩形，并按 pivot 修正矩形起点。
        // 即使状态图片的形状不同，这个用于点击的矩形也始终保持不变。
        if(buttonImage.preserveAspect)
        {
            float spriteRatio = spriteRect.width / spriteRect.height;
            if(rect.width / rect.height > spriteRatio)
            {
                float width = rect.height * spriteRatio;
                rect.x += (rect.width - width) * rectTransform.pivot.x;
                rect.width = width;
            }
            else
            {
                float height = rect.width / spriteRatio;
                rect.y += (rect.height - height) * rectTransform.pivot.y;
                rect.height = height;
            }
        }

        // 先拒绝显示矩形外的点，防止纹理采样的 Clamp/Repeat 将留白误判为命中。
        if(!rect.Contains(localPoint))
        {
            return false;
        }

        // 将局部坐标归一化：左下角为 (0, 0)，右上角为 (1, 1)。
        float u = (localPoint.x - rect.x) / rect.width;
        float v = (localPoint.y - rect.y) / rect.height;

        // Sprite 可能只是原始纹理中切出的一块，因此需要加上 spriteRect 的起点，
        // 再除以整张纹理尺寸，得到 GetPixelBilinear 所需的纹理 UV 坐标。
        Texture2D texture = NormalSprite.texture;
        float textureU = (spriteRect.x + u * spriteRect.width) / texture.width;
        float textureV = (spriteRect.y + v * spriteRect.height) / texture.height;

        // 只采样普通态纹理的 Alpha，不受 Image 颜色、悬停发光或文字移动影响。
        return texture.GetPixelBilinear(textureU, textureV).a >= AlphaThreshold;
    }
}

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 让不规则形状的 UI 按钮只在普通态图片的不透明区域响应鼠标。
///
/// Unity 的 GraphicRaycaster 会先找到鼠标下方的 Image，然后自动调用
/// ICanvasRaycastFilter.IsRaycastLocationValid。这个方法返回 true，Button 才能继续
/// 收到悬停、按下和点击事件；返回 false，射线就会忽略当前位置。
///
/// 本脚本会在 Awake 时固定保存普通态 Sprite。之后即使 Button 使用 Sprite Swap
/// 显示悬停态或按下态，命中检测仍然只读取普通态图片的 Alpha。因此动画图片改变
/// 轮廓时，按钮的点击区域也不会跟着收缩、跳动或闪烁。
///
/// 检测过程：屏幕坐标 -> Image 局部坐标 -> Sprite 内的比例坐标 -> 纹理 UV
/// -> 读取对应像素的 Alpha -> 与 AlphaThreshold 比较。
///
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

        // Image.sprite 是 Inspector 中设置的 Source Image，也就是按钮的普通态图片。
        // Button 的 Sprite Swap 会通过 Image.overrideSprite 临时显示悬停态、按下态，
        // 不会修改这里保存的 NormalSprite。因此后续检测始终使用同一张普通态图片。
        if(NormalSprite == null)
        {
            NormalSprite = buttonImage.sprite;
        }

        // 这里只关闭 Image 自带的“按当前显示图片检测 Alpha”，并没有关闭 UI 射线。
        // Image 的 Raycast Target 仍然必须开启，否则 GraphicRaycaster 找不到这个按钮，
        // 本脚本的 IsRaycastLocationValid 也不会被调用。
        //
        // 如果不设为 0，Image 可能根据当前悬停态/按下态图片再次过滤射线，导致
        // 本脚本刚按普通态判断为命中，随后又被状态图片的透明区域拒绝。
        buttonImage.alphaHitTestMinimumThreshold = 0f;
    }

    /// <summary>
    /// 这个方法不是由我们主动调用，而是由 Unity 的 UI 射线系统自动调用。
    /// true 表示鼠标位于普通态图片的不透明部分，允许 Button 接收事件；
    /// false 表示鼠标位于透明部分，忽略这次命中。
    ///
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

        // 最终需要调用 texture.GetPixelBilinear 读取像素，所以纹理必须允许 CPU 读取。
        // 打包进 Sprite Atlas 后，Sprite 可能被裁切或旋转，下面的简单坐标公式便不再可靠。
        // 因此这里只支持 Simple、未打包且开启 Read/Write 的普通态图片。
        // 配置有误时回退为普通矩形点击，并且只警告一次，避免按钮彻底失效或刷屏。
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

        // 第一步：把鼠标的屏幕坐标转换成 Image 自身的局部坐标。
        // 后面的计算必须知道鼠标位于按钮内部的什么位置，而不能直接使用屏幕坐标。
        // Unity 会在这里处理 Canvas、相机以及 RectTransform 的位移、旋转和缩放。
        // Screen Space - Overlay Canvas 下 eventCamera 为 null 是正常情况。
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

        // 第二步：确定普通态图片在 RectTransform 中实际占据的显示矩形。
        // 开启 Preserve Aspect 后，图片为了保持宽高比，可能不会铺满整个 RectTransform，
        // 两侧或上下会留下空白。这里根据普通态 Sprite 的比例缩小检测矩形，
        // 再根据 pivot 修正矩形起点，避免把这些留白误认为图片的一部分。
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

        // 鼠标不在普通态图片的实际显示矩形内，直接判定为没有命中。
        // 这也可以防止纹理采样的 Clamp/Repeat 把矩形外位置映射到边缘像素。
        if(!rect.Contains(localPoint))
        {
            return false;
        }

        // 第三步：计算鼠标位于图片内部的相对位置。
        // 左下角是 (0, 0)，中心是 (0.5, 0.5)，右上角是 (1, 1)。
        // 例如 u = 0.25 表示鼠标位于图片宽度的四分之一处。
        float u = (localPoint.x - rect.x) / rect.width;
        float v = (localPoint.y - rect.y) / rect.height;

        // 第四步：把 Sprite 内的相对位置换算成整张 Texture 的 UV 坐标。
        // Sprite 可能只是原始纹理中切出来的一部分，所以不能直接使用 u、v；
        // 必须先加上 Sprite 在原始纹理中的起点，再除以整张纹理的宽高。
        // GetPixelBilinear 只接受这种 0～1 范围的纹理 UV。
        Texture2D texture = NormalSprite.texture;
        float textureU = (spriteRect.x + u * spriteRect.width) / texture.width;
        float textureV = (spriteRect.y + v * spriteRect.height) / texture.height;

        // 第五步，也是最终的命中判断：读取普通态纹理对应位置的 Alpha。
        // Alpha 大于等于阈值，说明这里属于可见触手，允许点击；否则属于透明留白。
        // 因为读取的是固定的 NormalSprite，所以切换悬停态、按下态 Sprite，
        // 或者改变 Image 颜色、移动文字，都不会改变按钮的命中轮廓。
        return texture.GetPixelBilinear(textureU, textureV).a >= AlphaThreshold;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class CardView : MonoBehaviour,
    IPointerClickHandler,
    IPointerEnterHandler,
    IPointerExitHandler
{
    public RectTransform CardViewVisual;
    public GameObject Selected;
    public GameObject Aimed;
    public GameObject Highlight;
    public GameObject Candidate;
    public RectTransform CardBackground;

    public TMP_Text CardNameText;
    public TMP_Text CardPowerText;
    public TMP_Text CardDescribeText;
    public TMP_Text CardKeywordsText;
    public int CardInstanceID;
    public Keywords CurrentKeywords; // 添加一个字段来存储当前的关键词

    // 保存当前卡牌的显示数据，预览卡可以直接复制，而不需要反向读取文本组件。
    public string CurrentCardName { get; private set; }
    public string CurrentCardDescription { get; private set; }
    public int CurrentPower { get; private set; }

    private Action<CardView> clickAction;
    private Action<CardView> pointerEnterAction;
    private Action<CardView> pointerExitAction;

    private bool liftOnHover;
    private bool isHovered;
    private Vector2 visualPositionBeforeHover;
    private int siblingIndexBeforeHover;

    // 手牌悬浮时升起的距离。场地卡牌不会使用该参数。
    public float HandHoverHeight = 30f;

    public void SetClickAction(Action<CardView> action)
    {
        clickAction = action;
    }

    // 设置悬浮时的显示回调，并决定该卡是否需要在悬浮时升起。
    public void SetPointerActions(
        Action<CardView> enterAction,
        Action<CardView> exitAction,
        bool shouldLiftOnHover)
    {
        pointerEnterAction = enterAction;
        pointerExitAction = exitAction;
        liftOnHover = shouldLiftOnHover;
    }

    public void UpdateCardView(string cardName,
        string cardDescribe, 
        int currentPower, 
        int cardInstanceID, 
        int currentKeywords,
        bool isExhausted)
    {
        CurrentCardName = cardName;
        CurrentCardDescription = cardDescribe;
        CurrentPower = currentPower;

        CardNameText.text = cardName;
        CardPowerText.text = currentPower.ToString();
        CurrentKeywords = (Keywords)currentKeywords;
        CardDescribeText.text = cardDescribe;
        CardKeywordsText.text = "";
        if(CurrentKeywords.HasFlag(Keywords.Sneaky))
        {
            CardKeywordsText.text += "敏捷 ";
        }
        if(CurrentKeywords.HasFlag(Keywords.Frenzy))
        {
            CardKeywordsText.text += "狂暴 ";
        }
        if(CurrentKeywords.HasFlag(Keywords.Hunter))
        {
            CardKeywordsText.text += "猎杀 ";
        }
        if(CurrentKeywords.HasFlag(Keywords.Poisonous))
        {
            CardKeywordsText.text += "剧毒 ";
        }
        if(CurrentKeywords.HasFlag(Keywords.Tough))
        {
            CardKeywordsText.text += "坚韧 ";
        }

        CardInstanceID = cardInstanceID;
        // 根据isExhausted更新卡牌的横置状态
        transform.rotation = isExhausted ? Quaternion.Euler(0, 0, 90) : Quaternion.identity;
    }
    public void SetSelected(bool isSelected)
    {
        // 这里可以添加选中状态的视觉反馈，比如改变边框颜色
        if (isSelected)
        {
            Selected.SetActive(true);
        }
        else
        {
            Selected.SetActive(false);
        }
    }
    public void SetAimed(bool isAimed)
    {
        // 这里可以添加瞄准状态的视觉反馈，比如改变边框颜色
        if (isAimed)
        {
            Aimed.SetActive(true);
        }
        else
        {
            Aimed.SetActive(false);
        }
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        if(clickAction == null)
        {
            return;
        }
        Debug.Log("Card clicked: " + CardInstanceID);
        clickAction?.Invoke(this);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if(isHovered)
        {
            return;
        }

        isHovered = true;

        if(liftOnHover)
        {
            // 根节点的位置属于手牌布局，悬浮只移动内部的视觉节点。
            visualPositionBeforeHover = CardViewVisual.anchoredPosition;
            siblingIndexBeforeHover = transform.GetSiblingIndex();

            // 升起后放到最后绘制，避免被相邻的重叠手牌遮挡。
            CardViewVisual.anchoredPosition =
                visualPositionBeforeHover + Vector2.up * HandHoverHeight;
            transform.SetAsLastSibling();
        }

        pointerEnterAction?.Invoke(this);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if(!isHovered)
        {
            return;
        }

        isHovered = false;

        if(liftOnHover)
        {
            CardViewVisual.anchoredPosition = visualPositionBeforeHover;

            // 恢复原来的层级位置，防止悬浮操作改变手牌顺序。
            transform.SetSiblingIndex(siblingIndexBeforeHover);
        }

        pointerExitAction?.Invoke(this);
    }
}

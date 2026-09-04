using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class CardView : MonoBehaviour, IPointerClickHandler
{
    public TMP_Text CardNameText;
    public TMP_Text CardPowerText;
    public TMP_Text CardDescribeText;
    public TMP_Text CardKeywordsText;
    public int CardInstanceID;
    public Keywords CurrentKeywords; // 添加一个字段来存储当前的关键词

    private Action<CardView> clickAction;

    public void SetClickAction(Action<CardView> action)
    {
        clickAction = action;
    }

    public void UpdateCardView(string cardName,
        string cardDescribe, 
        int currentPower, 
        int cardInstanceID, 
        int currentKeywords,
        bool isExhausted)
    {
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
            transform.Find("Selected").gameObject.SetActive(true); // 假设有一个名为"Selected"的子对象用于显示选中状态
        }
        else
        {
            transform.Find("Selected").gameObject.SetActive(false); // 隐藏选中状态
        }
    }
    public void SetAimed(bool isAimed)
    {
        // 这里可以添加瞄准状态的视觉反馈，比如改变边框颜色
        if (isAimed)
        {
            transform.Find("Aimed").gameObject.SetActive(true); // 假设有一个名为"Aimed"的子对象用于显示瞄准状态
        }
        else
        {
            transform.Find("Aimed").gameObject.SetActive(false); // 隐藏瞄准状态
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
}

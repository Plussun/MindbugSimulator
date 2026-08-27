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
    public int CardInstanceID;

    private Action<CardView> clickAction;

    public void SetClickAction(Action<CardView> action)
    {
        clickAction = action;
    }

    public void UpdateCardView(string cardName, int currentPower, int cardInstanceID, bool isExhausted)
    {
        CardNameText.text = cardName;
        CardPowerText.text = currentPower.ToString();
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

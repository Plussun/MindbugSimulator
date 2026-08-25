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

    private Action<int> clickAction;

    public void SetClickAction(Action<int> action)
    {
        clickAction = action;
    }

    public void UpdateCardView(string cardName, int currentPower, int cardInstanceID)
    {
        CardNameText.text = cardName;
        CardPowerText.text = currentPower.ToString();
        CardInstanceID = cardInstanceID;
    }
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("Card clicked: " + CardInstanceID);
        clickAction?.Invoke(CardInstanceID);
    }
}

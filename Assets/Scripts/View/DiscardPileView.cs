using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System;
using UnityEngine.EventSystems;

public class DiscardPileView : MonoBehaviour, IPointerClickHandler
{


    private Action<bool> clickAction;
    public bool IsLocalPlayer; // 是否为本地玩家的弃牌堆

    public void SetClickAction(Action<bool> action)
    {
        clickAction = action;
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if(clickAction == null)
        {
            return;
        }
        clickAction?.Invoke(IsLocalPlayer);
    }
}

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

        // 文字矩形通常大于实际内容，让名称底板负责命中，避免空白处也能点击。
        foreach(TMP_Text text in GetComponentsInChildren<TMP_Text>(true))
        {
            text.raycastTarget = false;
        }
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

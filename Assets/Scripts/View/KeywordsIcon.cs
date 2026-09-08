using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeywordsIcon : MonoBehaviour
{
    public Sprite Poisonous;
    public Sprite Frenzy;
    public Sprite Hunter;
    public Sprite Tough;
    public Sprite Sneaky;

    public Image KeywordIconImage;

    // 每个图标对象只表示一个关键词，根据传入关键词切换对应贴图。
    public void SetIconImage(Keywords keyword)
    {
        switch(keyword)
        {
            case Keywords.Sneaky:
                KeywordIconImage.sprite = Sneaky;
                break;
            case Keywords.Frenzy:
                KeywordIconImage.sprite = Frenzy;
                break;
            case Keywords.Hunter:
                KeywordIconImage.sprite = Hunter;
                break;
            case Keywords.Poisonous:
                KeywordIconImage.sprite = Poisonous;
                break;
            case Keywords.Tough:
                KeywordIconImage.sprite = Tough;
                break;
            default:
                KeywordIconImage.sprite = null;
                break;
        }
    }

}

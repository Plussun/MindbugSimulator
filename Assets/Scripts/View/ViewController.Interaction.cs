using System.Collections.Generic;
using UnityEngine;

public partial class ViewController
{
    public void RefreshButtons(int localPlayerMindbugCount,CardNetworkState pendingTarget)
    {
        UseMindbugButton.gameObject.SetActive(false);
        NoMindbugButton.gameObject.SetActive(false);
        NoBlockButton.gameObject.SetActive(false);
        NoFrenzyAttackButton.gameObject.SetActive(false);
        NextGameButton.gameObject.SetActive(false);
        ChooseButton.gameObject.SetActive(false);

        if(currentPhase == GamePhase.WaitingForMindbugDecision && isLocalPlayerExpected)
        {
            if(localPlayerMindbugCount > 0)
            {
                UseMindbugButton.gameObject.SetActive(true);
            }
            else
            {
                UseMindbugButton.gameObject.SetActive(false);
            }
            NoMindbugButton.gameObject.SetActive(true);
        }
        else if(currentPhase == GamePhase.WaitingForBlockDecision && isLocalPlayerExpected)
        {
            if(pendingTarget.CardInstanceID != -1)//狩猎目标如果存在
            {
                NoBlockButton.gameObject.SetActive(false);
            }
            else
            {
                NoBlockButton.gameObject.SetActive(true);
            }
        }
        else if(currentPhase == GamePhase.WaitingForFrenzyAttack && isLocalPlayerExpected)
        {
            NoFrenzyAttackButton.gameObject.SetActive(true);
        }
        else if(currentPhase == GamePhase.GameOver)
        {
            NextGameButton.gameObject.SetActive(true);
        }
        else if(currentPhase == GamePhase.WaitingForChoice && isLocalPlayerExpected)
        {
            ChooseButton.gameObject.SetActive(true);
        }
    }

    // 根据客户端收到的状态更新操作提示，只影响本地UI，不修改任何游戏状态。
    private void RefreshInformationText(
        int winnerPlayerID,
        int localPlayerID,
        int localPlayerMindbugCount,
        int minSelectCount,
        int maxSelectCount)
    {
        if(InformationText == null)
        {
            return;
        }

        switch(currentPhase)
        {
            case GamePhase.Setup:
                InformationText.text = "等待游戏开始";
                break;

            case GamePhase.WaitingForMainAction:
                InformationText.text = isLocalPlayerExpected
                    ? "你的回合：请选择出牌或发起攻击"
                    : "等待对手出牌或发起攻击";
                break;

            case GamePhase.WaitingForMindbugDecision:
                if(!isLocalPlayerExpected)
                {
                    InformationText.text = "等待对手决定是否使用夺心虫";
                }
                else if(localPlayerMindbugCount > 0)
                {
                    InformationText.text = "请选择是否使用夺心虫";
                }
                else
                {
                    InformationText.text = "你没有可用的夺心虫，请选择不使用";
                }
                break;

            case GamePhase.WaitingForBlockDecision:
                if(!isLocalPlayerExpected)
                {
                    InformationText.text = "等待对手决定是否阻挡";
                }
                else if(hasRequiredBlockTarget)
                {
                    InformationText.text = "猎杀：请选择指定生物进行阻挡";
                }
                else
                {
                    InformationText.text = "请选择生物进行阻挡，或选择不阻挡";
                }
                break;

            case GamePhase.WaitingForFrenzyAttack:
                InformationText.text = isLocalPlayerExpected
                    ? "狂暴：请选择再次攻击，或放弃再次攻击"
                    : "等待对手决定是否再次攻击";
                break;

            case GamePhase.WaitingForChoice:
                InformationText.text = isLocalPlayerExpected
                    ? GetChoiceInformation(minSelectCount, maxSelectCount)
                    : "等待对手选择卡牌";
                break;

            default:
                InformationText.text = "等待游戏状态更新";
                break;
        }
    }

    private string GetChoiceInformation(int minSelectCount, int maxSelectCount)
    {
        if(minSelectCount == maxSelectCount)
        {
            return "请选择" + minSelectCount + "张卡牌";
        }
        if(minSelectCount == 0)
        {
            return "请选择至多" + maxSelectCount + "张卡牌";
        }

        return "请选择" + minSelectCount + "至" + maxSelectCount + "张卡牌";
    }

    // 选择过程中直接更新本地提示，不需要等待服务器再次同步状态。
    private void RefreshChoiceSelectionInformation()
    {
        if(InformationText == null || pendingChoice == null)
        {
            return;
        }

        if(choosedCards.Count < pendingChoice.MinSelectCount)
        {
            InformationText.text = "已选择" + choosedCards.Count
                + "张，还需至少选择" + pendingChoice.MinSelectCount + "张";
        }
        else
        {
            InformationText.text = "已选择" + choosedCards.Count
                + "张，请点击确认选择";
        }
    }

    public void PlayCardDecision(CardView cardView)
    {
        if(selectedCard == null)
        {
            selectedCard = cardView;
            selectedCard.SetSelected(true);
            PlayButton.gameObject.SetActive(true);
            InformationText.text = "已选择「" + cardView.CurrentCardName
                + "」，请点击出牌确认";
        }
        else
        {
            selectedCard.SetSelected(false);
            selectedCard = null;
            PlayButton.gameObject.SetActive(false);
            InformationText.text = "你的回合：请选择出牌或发起攻击";
        }
    }

    public void OnPlayButtonClicked()
    {
        if(selectedCard != null)
        {
            selectedCard.SetSelected(false);
            //发送请求前先取消选中，避免同步刷新后继续访问已销毁的卡牌对象。
            networkController.PlayCardRequest(selectedCard.CardInstanceID);
            selectedCard = null;
            PlayButton.gameObject.SetActive(false);
        }
    }

    public void AttackDecision(CardView cardView)
    {
        if(selectedCard == null)
        {
            selectedCard = cardView;
            selectedCard.SetSelected(true);
            AttackButton.gameObject.SetActive(true);
            InformationText.text = "已选择「" + cardView.CurrentCardName
                + "」，请点击攻击确认";
        }
        else
        {
            selectedCard.SetSelected(false);
            selectedCard = null;
            AttackButton.gameObject.SetActive(false);
            InformationText.text = "你的回合：请选择出牌或发起攻击";
        }
    }

    public void ChooseDecision(CardView cardView)
    {
        if(currentPhase != GamePhase.WaitingForChoice)
        {
            Debug.LogWarning("当前不在等待选择阶段，无法选择卡牌");
            return;
        }
        if (!pendingChoice.CandidateCardInstanceIDs.Contains(cardView.CardInstanceID))
        {
            Debug.LogWarning("选择的卡牌ID不在备选列表中");
            return;
        }
        if(choosedCards.Contains(cardView))
        {
            ChooseButton.gameObject.SetActive(true);
            choosedCards.Remove(cardView);
            cardView.SetAimed(false);
            RefreshChoiceSelectionInformation();
            return;
        }
        if(choosedCards.Count >= pendingChoice.MaxSelectCount)
        {
            Debug.LogWarning("已达到最大选择数量");
            return;
        }
        else
        {
            ChooseButton.gameObject.SetActive(true);
            choosedCards.Add(cardView);
            cardView.SetAimed(true);
            RefreshChoiceSelectionInformation();
        }
    }

    public void OnChooseButtonClicked()
    {
        if(choosedCards.Count < pendingChoice.MinSelectCount || choosedCards.Count > pendingChoice.MaxSelectCount)
        {
            Debug.LogWarning("选择的卡牌数量不符合要求");
            return;
        }
        List<int> selectedCardInstanceIDs = choosedCards.ConvertAll(c => c.CardInstanceID);
        networkController.SelectCardsRequest(selectedCardInstanceIDs);
    }

    public void OnAttackButtonClicked()
    {
        if(selectedCard != null)
        {
            selectedCard.SetSelected(false);
            //注意，这里需要先把selectedCard的待选设为false然后再发送网络请求
            //因为在网络请求发送后，可能会触发UI刷新，导致selectedCard被销毁，从而无法设置选中状态

            networkController.AttackDecisionRequest(selectedCard.CardInstanceID);
            
            selectedCard = null;
            AttackButton.gameObject.SetActive(false);
        }
    }

    public void BlockDecision(CardView cardView)
    {
        if(selectedCard == null)
        {
            selectedCard = cardView;
            selectedCard.SetSelected(true);
            BlockButton.gameObject.SetActive(true);
            InformationText.text = "已选择「" + cardView.CurrentCardName
                + "」，请点击阻挡确认";
        }
        else
        {
            selectedCard.SetSelected(false);
            selectedCard = null;
            BlockButton.gameObject.SetActive(false);
            InformationText.text = hasRequiredBlockTarget
                ? "猎杀：请选择指定生物进行阻挡"
                : "请选择生物进行阻挡，或选择不阻挡";
        }
    }

    public void OnBlockButtonClicked()
    {
        if(selectedCard != null)
        {
            selectedCard.SetSelected(false);
            //注意，这里需要先把selectedCard的待选设为false然后再发送网络请求
            //因为在网络请求发送后，可能会触发UI刷新，导致selectedCard被销毁，从而无法设置选中状态
            networkController.BlockDecisionRequest(true, selectedCard.CardInstanceID);
            selectedCard = null;
            BlockButton.gameObject.SetActive(false);
        }
    }


    public void OnNoBlockButtonClicked()
    {
        networkController.BlockDecisionRequest(false, -1);
    }

    public void OnNoFrenzyAttackButtonClicked()
    {
        networkController.SkipFrenzyAttackRequest();
    }

    public void OnDiscardPileClicked(bool isLocalPlayer)
    {
        DiscardPilePannel.gameObject.SetActive(!DiscardPilePannel.gameObject.activeSelf);
        if(isLocalPlayer)
        {
            DiscardPilePannel.Find("local").gameObject.SetActive(true);
            DiscardPilePannel.Find("opponent").gameObject.SetActive(false);
        }
        else
        {
            DiscardPilePannel.Find("local").gameObject.SetActive(false);
            DiscardPilePannel.Find("opponent").gameObject.SetActive(true);
        }
    }

    public void OpenDiscardPilePannel(bool isLocalPlayer)
    {
        DiscardPilePannel.gameObject.SetActive(true);
        DiscardPilePannel.Find("local").gameObject.SetActive(isLocalPlayer);
        DiscardPilePannel.Find("opponent").gameObject.SetActive(!isLocalPlayer);
    }

    private bool ContainsChoiceCandidate(CardNetworkState[] cards)
    {
        if(pendingChoice == null)
        {
            return false;
        }

        foreach(var card in cards)
        {
            if(pendingChoice.CandidateCardInstanceIDs.Contains(card.CardInstanceID))
            {
                return true;
            }
        }

        return false;
    }
}

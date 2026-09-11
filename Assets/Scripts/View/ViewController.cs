using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
public partial class ViewController : MonoBehaviour
{
    public GameObject CardViewPrefab;
    public Transform LocalPlayer;
    public Transform OpponentPlayer;
    public Transform PendingCardsContainer;
    public Transform WinnerContainer;
    public Transform DiscardPilePannel;
    public CardPreviewController CardPreviewController;

    public Button NoBlockButton;
    public Button NoMindbugButton;
    public Button UseMindbugButton;
    public Button AttackButton;
    public Button BlockButton;
    public Button NoFrenzyAttackButton;
    public Button NextGameButton;
    public Button ChooseButton;
    public Button PlayButton;

    public TMP_Text InformationText;

    public GameController gameController;
    public NetworkController networkController;

    private GamePhase currentPhase;
    private bool isLocalPlayerExpected;
    private bool hasRequiredBlockTarget;
    private CardView selectedCard;
    private List<CardView> choosedCards = new List<CardView>();

    // 所有能够看到真实实例ID的卡牌共用同一个字典。
    // 卡牌在手牌、场地、待决策区和弃牌区之间移动时，可以直接复用同一个CardView。
    private Dictionary<int, CardView> cardViews =
        new Dictionary<int, CardView>();

    // 对手手牌只公开数量，不公开真实实例ID，因此单独保存为匿名卡背列表。
    // 列表中的CardView只代表一张未知手牌，不能加入上面的真实卡牌字典。
    private List<CardView> opponentHandViews = new List<CardView>();

    // 每次刷新时记录新状态中仍然可见的卡牌，最后再统一删除真正消失的对象。
    private HashSet<int> visibleCardInstanceIDs = new HashSet<int>();
    private List<int> removedCardInstanceIDs = new List<int>();

    private PendingChoice pendingChoice;

    // Start is called before the first frame update
    void Start()
    {
        NoBlockButton.onClick.AddListener(OnNoBlockButtonClicked);
        AttackButton.onClick.AddListener(OnAttackButtonClicked);
        BlockButton.onClick.AddListener(OnBlockButtonClicked);
        NoFrenzyAttackButton.onClick.AddListener(OnNoFrenzyAttackButtonClicked);
        ChooseButton.onClick.AddListener(OnChooseButtonClicked);
        PlayButton.onClick.AddListener(OnPlayButtonClicked);

        DiscardPilePannel.Find("CloseButton").GetComponent<Button>().
            onClick.AddListener(() => OnDiscardPileClicked(true));
        LocalPlayer.Find("Discard").GetComponent<DiscardPileView>().IsLocalPlayer = true;
        LocalPlayer.Find("Discard").GetComponent<DiscardPileView>().
            SetClickAction(OnDiscardPileClicked);
        OpponentPlayer.Find("Discard").GetComponent<DiscardPileView>().IsLocalPlayer = false;
        OpponentPlayer.Find("Discard").GetComponent<DiscardPileView>().
            SetClickAction(OnDiscardPileClicked);
    }

    public void RefreshSnapShot(ClientGameStateSnapshot snapshot )
    {
        RefreshView(
            gamePhase: (int)snapshot.CurrentPhase,
            winnerPlayerID: snapshot.WinnerPlayerID,
            localPlayerID: snapshot.LocalPlayerID,
            ActivePlayerID: snapshot.ActivePlayerID,
            ExpectedPlayerID: snapshot.ExpectedPlayerID,

            localPlayerLife: snapshot.LocalPlayerLife,
            opponentPlayerLife: snapshot.OpponentPlayerLife,
            localPlayerMindbugCount: snapshot.LocalPlayerMindbugCount,
            opponentPlayerMindbugCount: snapshot.OpponentPlayerMindbugCount,
            localPlayerDeckCount: snapshot.LocalPlayerDeckCount,
            opponentPlayerDeckCount: snapshot.OpponentPlayerDeckCount,

            localPlayerDiscard: snapshot.LocalPlayerDiscard,
            opponentPlayerDiscard: snapshot.OpponentPlayerDiscard,
            localPlayerHand: snapshot.LocalPlayerHand,
            localPlayerField: snapshot.LocalPlayerField,
            opponentPlayerField: snapshot.OpponentPlayerField,
            opponentHandCount: snapshot.OpponentHandCount,

            pendingCard: snapshot.PendingCard,
            pendingAttack: snapshot.PendingAttackCard,
            pendingTarget: snapshot.PendingTargetCard,

            hasPendingChoice: snapshot.HasPendingChoice,
            maxSelectCount: snapshot.MaxSelectCount,
            minSelectCount: snapshot.MinSelectCount,
            candidateCardInstanceIDs: snapshot.CandidateCardInstanceIDs
        );
    }
    public void RefreshView(
        int gamePhase,
        int winnerPlayerID,
        int localPlayerID,
        int ActivePlayerID,
        int ExpectedPlayerID,
        int localPlayerLife,
        int opponentPlayerLife,
        int localPlayerMindbugCount,
        int opponentPlayerMindbugCount,
        int localPlayerDeckCount,
        int opponentPlayerDeckCount,
        CardNetworkState[] localPlayerDiscard,
        CardNetworkState[] opponentPlayerDiscard,
        CardNetworkState[] localPlayerHand,
        CardNetworkState[] localPlayerField,
        CardNetworkState[] opponentPlayerField,
        int opponentHandCount,
        CardNetworkState pendingCard,
        CardNetworkState pendingAttack,
        CardNetworkState pendingTarget,
        bool hasPendingChoice,
        int maxSelectCount,
        int minSelectCount,
        int[] candidateCardInstanceIDs
        )
    {
        currentPhase = (GamePhase)gamePhase;
        isLocalPlayerExpected = (localPlayerID == ExpectedPlayerID);
        hasRequiredBlockTarget = pendingTarget.CardInstanceID != -1;

        selectedCard = null;
        choosedCards.Clear();
        PlayButton.gameObject.SetActive(false);
        AttackButton.gameObject.SetActive(false);
        if(hasPendingChoice)
        {
            pendingChoice = new PendingChoice
            {
                PlayerID = localPlayerID,
                MaxSelectCount = maxSelectCount,
                MinSelectCount = minSelectCount,
                CandidateCardInstanceIDs = new List<int>(candidateCardInstanceIDs)
            };
        }
        else
        {
            pendingChoice = null;
        }

        RefreshInformationText(
            winnerPlayerID,
            localPlayerID,
            localPlayerMindbugCount,
            minSelectCount,
            maxSelectCount);
        
        RefreshPlayerPortrait(true, localPlayerLife, 
            localPlayerMindbugCount, isLocalPlayerExpected);
        RefreshPlayerPortrait(false, opponentPlayerLife, opponentPlayerMindbugCount,
            !isLocalPlayerExpected);
        // 所有带真实实例ID的区域必须先全部处理完，再统一删除未出现的CardView。
        // 这样卡牌跨区域移动时只会改变父物体，不会被旧区域提前销毁。
        visibleCardInstanceIDs.Clear();
        removedCardInstanceIDs.Clear();

        RefreshLocalHandView(
            localPlayerHand,
            pendingAttack,
            pendingTarget,
            pendingChoice);
        RefreshFieldView(localPlayerField, LocalPlayer, pendingAttack, pendingTarget,pendingChoice);
        RefreshFieldView(opponentPlayerField, OpponentPlayer, pendingAttack, pendingTarget,pendingChoice);
        RefreshOpponentHandView(opponentHandCount, OpponentPlayer);
        RefreshPendingCardsView(pendingCard);

        RefreshDeckCount(true, localPlayerDeckCount);
        RefreshDeckCount(false, opponentPlayerDeckCount);

        RefreshDiscardCount(true, localPlayerDiscard.Length);
        RefreshDiscardPilePannel(true, localPlayerDiscard,pendingChoice);

        RefreshDiscardCount(false, opponentPlayerDiscard.Length);
        RefreshDiscardPilePannel(false, opponentPlayerDiscard,pendingChoice);

        RemoveInvisibleCardViews();

        // 清理完成后再布局，避免已经离开某区域的旧对象参与本次排版。
        LocalPlayer.Find("Hand").GetComponent<HandCardLayout>().RefreshLayout();
        LocalPlayer.Find("Field").GetComponent<FieldCardLayout>().RefreshLayout();
        OpponentPlayer.Find("Field").GetComponent<FieldCardLayout>().RefreshLayout();
        DiscardPilePannel.Find("local").GetComponent<DiscardPileLayout>().RefreshLayout();
        DiscardPilePannel.Find("opponent").GetComponent<DiscardPileLayout>().RefreshLayout();
        CardPreviewController.RefreshCurrentPreview();

        RefreshButtons(localPlayerMindbugCount,pendingTarget);
        RefreshWinnerView(winnerPlayerID, localPlayerID);

        //等待从弃牌区选择卡牌时，自动打开对应玩家的弃牌区界面
        if(isLocalPlayerExpected &&
            currentPhase == GamePhase.WaitingForChoice)
        {
            if(ContainsChoiceCandidate(localPlayerDiscard))
            {
                OpenDiscardPilePannel(true);
                ChooseButton.gameObject.SetActive(false);
            }
            else if(ContainsChoiceCandidate(opponentPlayerDiscard))
            {
                OpenDiscardPilePannel(false);
                ChooseButton.gameObject.SetActive(false);
            }
        }
        
    }
}

using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System;
using DG.Tweening;
using System.Linq;
using Febucci.UI;

//전투시 카드들을 관리합니다.

public partial class CardBattleManager : MonoBehaviour
{
    #region 싱글톤
    public static CardBattleManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }
    #endregion

    [Header("Prefabs")]

    [Header("Card Positions")]
    public Transform DeckPosition;
    public Transform HandPositionLeft;
    public Transform HandPositionRight;
    public Transform UsedPosition;
    public Transform HoldPosition;
    public Transform GameOverHidePosition;

    //하이에라키에 정렬
    [SerializeField] Transform cardParent;

    [Header("UI Instance")]
    [SerializeField] Transform CardDeckParent;
    public CardDeck CardDeckInstance;
    [SerializeField] Transform UsedCardsParent;
    public UsedCards UsedCardsInstance;

    //이건 BattleSceneManager로 이전하자---
    [SerializeField] Arrow arrowInstance;
    //이렇게---

    /// <summary>
    /// 손패입니다.
    /// </summary>
    public List<CardInstance> HandCards = new();
    /// <summary>
    /// 생성된모든 카드 인스턴스
    /// </summary>
    public List<CardInstance> ActiveCardInstance = new();

    //플래그
    public bool IsCardHold
    {
        get { return isCardHold; }
        set 
        {
            isCardHold = value;
            if(!isCardHold)
                IndicatorSystem.Instance.StopShowingPreview();

            if (!IsCardHold && !IsCardDrag)
            {
                //큐가 돌아가지 않을때에만 활성화 시켜야합니다.
                if(!IsQueueRunning)
                    TurnManager.Instance.SetEndTurnButton(true);
            }
            else
                TurnManager.Instance.SetEndTurnButton(false);
        }

    }
    bool isCardHold;
    public bool IsCardDrag
    {
        get { return isCardDrag; }
        set
        {
            isCardDrag = value;

            if (!IsCardHold && !IsCardDrag)
            {
                //큐가 돌아가지 않을때에만 활성화 시켜야합니다.
                if (!IsQueueRunning)
                    TurnManager.Instance.SetEndTurnButton(true);
            } 
            else
                TurnManager.Instance.SetEndTurnButton(false);
        }
    }
    bool isCardDrag;

    /// <summary>
    /// 현재 홀드된 카드
    /// </summary>
    public CardInstance HoldCard { get; private set; }

    [Header("UI Panel")]
    public ScrollView ScrollView;
    [SerializeField] CardSelectPanel cardSelectPanel;
    public bool IsCardSelectMode => cardSelectPanel.IsCardSelectMode;

    /// <summary>
    /// 마우스로 선택된 카드
    /// </summary>
    CardInstance selectCard;

    //현재 임의로 결정한 카드 크기
    public float CardSize = 0.3f;
    int dropAreaLayer;
    int handAreaLayer;



    private void Start()
    {
        handAreaLayer = LayerMask.NameToLayer("HandArea");
        dropAreaLayer = LayerMask.NameToLayer("DropArea");

        TooltipManager.Instance.SetCanvasCameraToUI();
    }

    private void Update()
    {
        if (IsCardDrag)
            CardDragUpdate();
        else if(IsCardHold)
            CardHoldUpdate();
    }


    /// <summary>
    /// 전투 시작시 호출합니다.
    /// </summary>
    public void Init()
    {
        //관련 UI를 모두 활성화시킵니다.
        CardDeckParent.gameObject.SetActive(true);
        UsedCardsParent.gameObject.SetActive(true);
        //마나 UI 활성화
        BattleSceneManager.Instance.PlayerMpHUD.SetActive(true);
        //이동 UI 활성화
        BattleSceneManager.Instance.MoveCount.SetActive(true);

        arrowInstance.Dispose();
        cardSelectPanel.HidePanel();
        ScrollView.HideScrollView();
        jobQueue.Clear();

        //기존 카드들을 정리합니다.
        foreach (CardInstance card in ActiveCardInstance)
        {
            if (card != null)
                Destroy(card.gameObject);
        }
        ActiveCardInstance.Clear();

        HandCards.Clear();
        CardDeckInstance.Clear();
        UsedCardsInstance.Clear();
        //덱에 들어있는 수만큼 카드를 생성합니다.
        foreach (CardData cardData in CardManager.Instance.CardDeck)
        {
            //덱 위치에 생성합니다.
            CardInstance cardInst = CreateCard(cardData);
            CardDeckInstance.AddCard(cardInst);
        }
        CardDeckInstance.FYShuffle(CardDeckInstance.Cards);
    }

    /// <summary>
    /// 전투 관련 UI를 모두 해제합니다.
    /// </summary>
    public void Dispose()
    {
        jobQueue.Clear();
        StopAllCoroutines();
        //관련 UI를 모두 비활성화시킵니다.
        //기존 카드들을 정리합니다.
        foreach (CardInstance card in ActiveCardInstance)
        {
            if (card != null)
            {
                card.KillAllSequences();
                Destroy(card.gameObject);
            }
        }
        ActiveCardInstance.Clear();

        HandCards.Clear();
        CardDeckInstance.Clear();
        UsedCardsInstance.Clear();
        arrowInstance.Dispose();
        ScrollView.HideScrollView();
        cardSelectPanel.HidePanel();

        CardDeckParent.gameObject.SetActive(false);
        UsedCardsParent.gameObject.SetActive(false);
        BattleSceneManager.Instance.PlayerMpHUD.SetActive(false);
        BattleSceneManager.Instance.MoveCount.SetActive(false);
    }

    /// <summary>
    /// 카드를 전부 집어넣고, 덱을 초기화해줍니다.
    /// </summary>
    public IEnumerator SetBattleToIdleState()
    {
        while(isQueueRunning)
        {
            yield return null;
        }
        yield return StartCoroutine(ClearHandCards());


        //기존 카드들을 정리합니다.
        foreach (CardInstance card in ActiveCardInstance)
        {
            if (card != null)
            {
                card.KillAllSequences();
                Destroy(card.gameObject);
            }
        }
        ActiveCardInstance.Clear();

        HandCards.Clear();
        CardDeckInstance.Clear();
        UsedCardsInstance.Clear();
        arrowInstance.Dispose();
        ScrollView.HideScrollView();
        cardSelectPanel.HidePanel();

        CardDeckParent.gameObject.SetActive(false);
        UsedCardsParent.gameObject.SetActive(false);
        BattleSceneManager.Instance.PlayerMpHUD.SetActive(false);
        BattleSceneManager.Instance.MoveCount.SetActive(false);


        //마지막으로 현재 방이 몹이 있었던 방이면, 보상을 줍니다.
        RoomType currentRoom = MapManager.Instance.GetCurrentRoomType();

        if (currentRoom == RoomType.Monster || currentRoom == RoomType.Elite || currentRoom == RoomType.Boss)
        {
            Player player = FieldManager.Instance.PlayerInstance;

            // 플레이어가 방을 이동하는 로딩(초기화) 상태가 아닐 때(즉, 실제 전투 종료 시)만 보상을 띄움
            if (player != null && !player.IsLoadingRoom)
            {
                if (CardReward.Instance != null)
                {
                    CardReward.Instance.ShowReward();
                }
            }
        }
    }

    /// <summary>
    /// 현재 전투 상황인지 체크합니다.
    /// </summary>
    /// <returns></returns>
    public bool IsBattleState()
    {
        //1.현재 맵에 몹이 존재할 경우
        //2.현재 맵에 몹이 곧 소환될 경우
        //3.이벤트 요소로 판정에 들어갈 경우

        //1. 몹이 존재하면 전투상황
        var enemyList = BattleSceneManager.Instance.EnemyList;
        if (enemyList != null && enemyList.Count > 0)
            return true;

        //2. 곧 소환될 몹이 존재하면 전투상황

        //3. 전투 판정이면 전투상황


        return false;
    }

    ///// <summary>
    ///// 다음 방으로 가는 등, 현재 카드를 전부 섞어 넣고 초기상태로 되돌릴 때 사용합니다.
    ///// </summary>

    /// <summary>
    /// 게임 오버시 Dispose 하지 않고, 각각의 요소들을 자연스럽게 숨기고 상호작용하지 않게 만듭니다.
    /// </summary>
    public void Hide()
    {
        selectCard = null;
        HoldCard = null;

        jobQueue.Clear();
        StopAllCoroutines();
        //관련 UI를 모두 비활성화시킵니다.
        //핸드는 아래로
        foreach (CardInstance card in HandCards)
        {
            if (card != null)
            {
                card.KillAllSequences();
                card.SetCollider(false);
                card.UpdateOutline(false);
                card.transform.DOMove(GameOverHidePosition.position, 3f);
            }
        }
        //혹시 핸드를 참조하여 다른 작업이 이루어질 수 있으니 일단 비워둡니다. 해제는 AciveCardInstance를 통해 합니다.
        HandCards.Clear();
        arrowInstance.Dispose();
        ScrollView.HideScrollView();
        cardSelectPanel.HidePanel();

        //각각 옆으로 사라져!!!
        UsedCardsParent.gameObject.transform.DOMoveX(30f, 2.5f).SetUpdate(true);
        CardDeckParent.gameObject.transform.DOMoveX(-30f, 2.5f).SetUpdate(true);
        BattleSceneManager.Instance.PlayerMpHUD.transform.DOMoveX(-30f, 2.5f).SetUpdate(true);
        BattleSceneManager.Instance.MoveCount.transform.DOMoveX(-30f, 2.5f).SetUpdate(true);
    }


    /// <summary>
    /// 카드를 생성합니다.
    /// 실제 덱에 넣는건 아닙니다!
    /// </summary>
    public CardInstance CreateCard(CardData cardData)
    {
        //덱 위치에 생성합니다.
        CardInstance cardInst = Instantiate(CardManager.Instance.CardPrefab, DeckPosition.position, DeckPosition.transform.rotation).GetComponent<CardInstance>();
        cardInst.transform.localScale = Vector3.one * 0.3f;
        cardInst.transform.SetParent(cardParent);
        //값을 넣어줍니다.
        cardInst.Initialize(cardData);
        ActiveCardInstance.Add(cardInst);

        return cardInst;
    }


    /// <summary>
    /// 카드를 생성합니다.
    /// 손패로 바로 넣을시 카드 생성 후 따로 외곽선 조절해봅시다
    /// 실제 덱에 넣는건 아닙니다!
    /// </summary>
    public CardInstance CreateCard(CardEntitySO originalData)
    {
        //덱 위치에 생성합니다.
        CardInstance cardInst = Instantiate(CardManager.Instance.CardPrefab, DeckPosition.position, DeckPosition.transform.rotation).GetComponent<CardInstance>();
        cardInst.transform.localScale = Vector3.one * 0.3f;
        cardInst.transform.SetParent(cardParent);
        //값을 넣어줍니다.
        CardData cardData = new();
        cardData.Init(originalData);
        cardInst.Initialize(cardData);
        ActiveCardInstance.Add(cardInst);

        return cardInst;
    }


    /// <summary>
    /// 핸드에 있는 카드의 외곽선을 마나에 따라 조정합니다.
    /// </summary>
    public void CardOutlineUpdate(bool enabled = true)
    {
        //내 턴이 아니면 외곽선을 보여주지 않습니다.
        if (!TurnManager.Instance.IsMyTurn) return;

        foreach(CardInstance card in HandCards)
        {
            card.UpdateOutline(enabled);
        }

    }

    /// <summary>
    /// 핸드에 있는 카드의 동적 정보를 갱신합니다.
    /// </summary>
    public void CardInfoUpdateAll()
    {
        foreach(CardInstance card in HandCards)
        {
            card.CardInfoUpdate();
        }
    }


    #region 카드-마우스 상호작용
    const float ENLARGE_HEIGHT = -2.5f;

    public void CardMouseEnter(CardInstance card)
    {
        if (IsCardDrag || card.IsSelected) return;
        card.Order.SetOrderToFront(true);

        card.KillAllSequences();
        card.Sequence = DOTween.Sequence();
        card.Sequence.Append(card.transform.DOMove(new Vector3(card.OriginalTransform.position.x, ENLARGE_HEIGHT, -10f), 0.4f)).SetEase(Ease.OutQuad)
                               .Join(card.transform.DORotateQuaternion(Quaternion.identity, 0.4f)).SetEase(Ease.OutQuad)
                               .Join(card.transform.DOScale(Vector3.one * 0.7f, 0.4f)).SetEase(Ease.OutQuad);

        //양쪽에 카드가 있을경우 카드를 벌려줍니다.
        int idx = HandCards.IndexOf(card);
        CardInstance leftCard = null;
        CardInstance rightCard = null;

        if(idx > 0)
            leftCard = HandCards[idx - 1];
        if(idx < HandCards.Count - 1)
            rightCard = HandCards[idx + 1];

        if (leftCard != null && !(cardSelectPanel.IsCardSelectMode && cardSelectPanel.SelectedCards.Contains(leftCard)) && !leftCard.IsSelected) // 특수 상호작용으로 카드가 올라갔을땐 발동하면 안돼!!!
        {
            leftCard.KillAllSequences();
            leftCard.Sequence = DOTween.Sequence();
            leftCard.Sequence.Append(leftCard.transform.DOMove(new Vector3(leftCard.OriginalTransform.position.x - 0.5f, leftCard.OriginalTransform.position.y, leftCard.OriginalTransform.position.z), 0.4f)).SetEase(Ease.OutQuad)
                             .Join(leftCard.transform.DORotateQuaternion(leftCard.OriginalTransform.rotation, 0.4f))
                             .Join(leftCard.transform.DOScale(leftCard.OriginalTransform.scale, 0.4f));
        }


        if (rightCard != null && !(cardSelectPanel.IsCardSelectMode && cardSelectPanel.SelectedCards.Contains(rightCard)) && !rightCard.IsSelected)
        {
            rightCard.KillAllSequences();
            rightCard.Sequence = DOTween.Sequence();
            rightCard.Sequence.Append(rightCard.transform.DOMove(new Vector3(rightCard.OriginalTransform.position.x + 0.5f, rightCard.OriginalTransform.position.y, rightCard.OriginalTransform.position.z), 0.4f)).SetEase(Ease.OutQuad)
                              .Join(rightCard.transform.DORotateQuaternion(rightCard.OriginalTransform.rotation, 0.4f))
                              .Join(rightCard.transform.DOScale(rightCard.OriginalTransform.scale, 0.4f));
        }

        //카드 효과에 따라 툴팁을 띄워줍니다.
        TooltipManager.Instance.SetTooltip(card);
        ////카드와 상호작용 시작시 유닛 상호적용 비허용
    }

    public void CardMouseOver(CardInstance card)
    {
        if(IsCardDrag || card.IsSelected) return;
        selectCard = card;
    }

    public void CardMouseExit(CardInstance card)
    {
        if (IsCardDrag || IsCardHold) return;

        TooltipManager.Instance.HideTooltips();
        //선택효과가 실행되는 경우에는 선택을 받고 있는 카드만 이 함수가 실행되어선 안됨.
        if (card.IsSelected) return;

        selectCard = null;

        card.Order.SetOrderToFront(false);

        if (!HandCards.Contains(card)) return;
        card.KillAllSequences();
        card.Sequence = DOTween.Sequence();
        card.Sequence.Append(card.transform.DOMove(card.OriginalTransform.position, 0.4f))
                              .Join(card.transform.DORotateQuaternion(card.OriginalTransform.rotation, 0.4f))
                              .Join(card.transform.DOScale(card.OriginalTransform.scale, 0.4f))
                              .OnComplete(() => MoveOriginalTransform(card));

        //양쪽에 카드가 있을경우 원상복귀
        int idx = HandCards.IndexOf(card);

        CardInstance leftCard = null;
        CardInstance rightCard = null;

        if (idx > 0)
            leftCard = HandCards[idx - 1];
        if (idx < HandCards.Count - 1)
            rightCard = HandCards[idx + 1];

        if (leftCard != null && !(cardSelectPanel.IsCardSelectMode && cardSelectPanel.SelectedCards.Contains(leftCard)) && !leftCard.IsSelected) //특수 상호작용으로 카드가 올라갔으면, 실행하면 안돼!
        {
            leftCard.KillAllSequences();
            leftCard.Sequence = DOTween.Sequence();
            leftCard.Sequence.Append(leftCard.transform.DOMove(leftCard.OriginalTransform.position, 0.4f))
                             .Join(leftCard.transform.DORotateQuaternion(leftCard.OriginalTransform.rotation, 0.4f))
                             .Join(leftCard.transform.DOScale(leftCard.OriginalTransform.scale, 0.4f))
                             .OnComplete(() => MoveOriginalTransform(leftCard));
        }
        if (rightCard != null && !(cardSelectPanel.IsCardSelectMode && cardSelectPanel.SelectedCards.Contains(rightCard)) && !rightCard.IsSelected)
        {
            rightCard.KillAllSequences();
            rightCard.Sequence = DOTween.Sequence();
            rightCard.Sequence.Append(rightCard.transform.DOMove(rightCard.OriginalTransform.position, 0.4f))
                              .Join(rightCard.transform.DORotateQuaternion(rightCard.OriginalTransform.rotation, 0.4f))
                              .Join(rightCard.transform.DOScale(rightCard.OriginalTransform.scale, 0.4f))
                              .OnComplete(() => MoveOriginalTransform(rightCard));
        }

        ////카드와 상호작용 끝나면 유닛 상호작용 허용


        //두트윈을 사용하기때문에, 애니메이션 도중 목적지가 바뀌는것을 대비해 애니메이션이 끝날때 위치를 업데이트합니다.
        void MoveOriginalTransform(CardInstance card)
        {
            card.transform.SetPositionAndRotation(card.OriginalTransform.position, card.OriginalTransform.rotation);
            card.transform.localScale = card.OriginalTransform.scale;
        }
    }

    public void CardMouseDown(CardInstance card)
    {
        TooltipManager.Instance.HideTooltips();

        card.KillAllSequences();

        //현재 Select모드가 활성화중이면 카드를 발동하는게 아니라, 단순히 선택만합니다.
        if (cardSelectPanel.IsCardSelectMode)
        {
            cardSelectPanel.SetCard(card);
            return;
        }

        IsCardDrag = true;
    }


    public void CardMouseUp()
    {
        TooltipManager.Instance.HideTooltips();
        //홀드상태에서 마우스를 땔때
        if (IsCardHold && HoldCard != null && !IsOnTargetArea(dropAreaLayer))
        {
            if(!HoldCard.OriginalData.CardEntitySO.IsSelectable)
                UseCard();
            else
            {
                //타일을 선택해야하는 카드는 타일 위에서 마우스를 땔 때 사용합니다.

                CatsWork.Tile tile = FieldManager.Instance.GetTileFromMousePosInRange(HoldCard);
                if (tile != null)
                    UseCard(tile);
            }

            return;
        }

        if (selectCard != null && selectCard.IsSelected)
            return;


        IsCardDrag = false;

        if (selectCard == null)
        {
            return;
        }


        //카드가 손패에 없으면 애니메이션 실행하면 안됨.
        if (!HandCards.Contains(selectCard)) return;

        //원래 자리로 가는 애니메이션
        selectCard.KillAllSequences();
        selectCard.Sequence = DOTween.Sequence();
        selectCard.Sequence.Append(selectCard.transform.DOMove(selectCard.OriginalTransform.position, 0.4f))
                               .Join(selectCard.transform.DORotateQuaternion(selectCard.OriginalTransform.rotation, 0.4f))
                               .Join(selectCard.transform.DOScale(selectCard.OriginalTransform.scale, 0.4f));
    }


    /// <summary>
    /// 카드 드래그중. 우클릭시 취소합니다.
    /// </summary>
    void CardDragUpdate()
    {
        if (selectCard != null)
        {
            selectCard.transform.position = Utility.UIMousePos;

            if (!IsOnTargetArea(handAreaLayer))
            {
                //카드를 낼 수 있는지 체크
                if(selectCard.IsAvailable())
                {
                    //카드를 적절한 위치로 이동 및 화살표 생성
                    IsCardDrag = false;
                    SetHoldCard(selectCard);

                    return;
                }
                else 
                {
                    //낼 수 없는 카드라면 카드를 돌려보냅니다.
                    IsCardDrag = false;
                    CardMouseExit(selectCard);

                    //추가로 부족함을 알려주는 것들 작성

                    return;
                }

            }

            if (Input.GetMouseButtonDown(1))
            {
                IsCardDrag = false;
                CardMouseExit(selectCard);
            }
        }
    }

    /// <summary>
    /// Hold중.
    /// </summary>
    void CardHoldUpdate()
    {
        if (HoldCard != null)
        {
            //선택이 필요 없는 카드라면 마우스를 따라옵니다.
            if(!HoldCard.OriginalData.CardEntitySO.IsSelectable)
                HoldCard.transform.position = Utility.UIMousePos;

            //카드 홀드중 우클릭 선택 취소
            if (Input.GetMouseButtonDown(1) || IsOnTargetArea(dropAreaLayer))
            {
                IsCardHold = false;

                arrowInstance.Dispose();
                CardMouseExit(HoldCard);
                HoldCard = null;

                HandCards.ForEach(x => x.SetCollider(true));
            }

            //카드 홀드중 좌클릭(화살표 카드만 가능)
            if(Input.GetMouseButtonDown(0))
            {
                if (HoldCard.OriginalData.CardEntitySO.IsSelectable)
                {
                    CatsWork.Tile tile = FieldManager.Instance.GetTileFromMousePosInRange(HoldCard);
                    if (tile != null)
                        UseCard(tile);
                }

            }
        }
    }

    /// <summary>
    /// 카드를 사용하기위해 집습니다.(화살표 상호작용)
    /// </summary>
    void SetHoldCard(CardInstance card)
    {
        ///Hold에는 두가지 종류가 있습니다.
        ///1.대상을 지정하는 카드의 경우
        ///2.대상을 지정하지 않는 카드의 경우
        
        IsCardHold = true;
        HandCards.ForEach(x => x.SetCollider(false));

        HoldCard = card;

        //대상을 지정하는 경우
        if (card.OriginalData.CardEntitySO.IsSelectable)
        {
            arrowInstance.gameObject.SetActive(true);
            arrowInstance.SetAttachedCard(card);

            card.KillAllSequences();
            card.Sequence = DOTween.Sequence();
            card.Sequence.Append(card.transform.DOMove(HoldPosition.position, 0.6f)).SetEase(Ease.OutQuad)
                             .Join(card.transform.DORotate(HoldPosition.position, 0.6f)).SetEase(Ease.OutQuad)
                             .Join(card.transform.DOScale(card.OriginalTransform.scale, 0.6f)).SetEase(Ease.OutQuad);
        }
        else //대상을 지정하지 않는 경우
        {
            card.KillAllSequences();
            card.Sequence = DOTween.Sequence();
            card.Sequence.Append(card.transform.DORotateQuaternion(Quaternion.identity, 0.3f).SetEase(Ease.OutQuad))
                             .Join(card.transform.DOScale(card.OriginalTransform.scale, 0.3f).SetEase(Ease.OutQuad));
        }
    }

    //카드가 발동이 되면, 즉시 사용한 카드는 덱으로.
    public void UseCard(CatsWork.Tile tile = null)
    {
        if(IsCardHold && tile == null)
        {
            //지정하지 않는 카드 발동**********
            if (!HoldCard.UseCard())    //발동 실패
            {
                IsCardHold = false;
            }
            else
            {
                IsCardHold = false;
                selectCard = null;
            }
        }
        else if(IsCardHold && tile != null)
        {
            //대상을 지정하는 카드 발동***********
            if(!HoldCard.UseCard(tile))    //발동 실패
            {
                ////원래 코드
                IsCardHold = false;

            }
            else
            {
                IsCardHold = false;
                selectCard = null;
                arrowInstance.Dispose();

            }
        }

        SetHandShape();
        //카드를 사용했을때, 드로우큐가 활성화중이면 카드의 콜라이더 활성화는 자동으로 맡깁니다.

        //이제 카드를 사용하면 즉시 핸드의 카드들을 전부 다시 상호작용 가능하게합니다.
        foreach (CardInstance card in HandCards)
        {
            card.SetCollider(true);
        }
        
    }

    /// <summary>
    /// 현재 손패 상태가 어떻든 가지런히 정렬하고 선택가능하게 합니다.
    /// </summary>
    public void ResetSelectedCards()
    {
        IsCardHold = false;
        arrowInstance.Dispose();
        if (HoldCard != null)
        {
            CardMouseExit(HoldCard);
            HoldCard = null;
        }

        //살짝만 들고있는 경우가 해당
        if(selectCard != null && IsCardDrag)
        {
            IsCardDrag = false;
            CardMouseExit(selectCard);
            selectCard = null;
        }

        foreach(CardInstance card in HandCards)
        {
            card.SetCollider(true);
            card.Order.SetOrderToFront(false);
        }
    }


    /// <summary>
    /// 마우스가 특정 레이어 영역에 있는지 검사합니다
    /// </summary>
    public bool IsOnTargetArea(int targetLayer)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(Utility.UIMousePos, Vector3.forward);
        return Array.Exists(hits, x => x.collider.gameObject.layer == targetLayer);
    }


    #endregion

    /// <summary>
    /// 카드를 선택하기위해 panel을 활성화
    /// </summary>
    public void UseCardSelect(string text, int selectNum, Action<List<CardInstance>> onCardSelected, Predicate<CardInstance> selectableCondition = null)
    {
        cardSelectPanel.ShowPanel(text, selectNum, onCardSelected, selectableCondition);
    }

    /// <summary>
    /// 카드를 선택하기위해 ScrollView를 활성화
    /// </summary>
    public void UseCardSelect(string text = null, List<CardInstance> targetCards = null, Action<CardInstance> onCardSelected = null, bool isReturnAvailable = false)
    {
        //    //카드가 없거나 하면 null로 다시 보내줘

        ScrollView.SetTargetCards(text, targetCards, onCardSelected, isReturnAvailable);
    }

    public void UseCardSelect(string text = null, List<CardData> targetCards = null, Action<CardData> onCardSelected = null, CardUISelectType selectType = CardUISelectType.SelectScrollView, bool isReturnAvailable = false)
    {

        ScrollView.SetTargetCards(text, targetCards, onCardSelected, selectType, isReturnAvailable);
    }

    public void SetSelectedCard(CardInstance card)
    {
        selectCard = card;
    }
}
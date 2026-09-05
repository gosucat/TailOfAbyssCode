using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;

//드로우 관련
public partial class CardBattleManager
{
    [SerializeField] CardShuffleEffect cardShuffleEffect;

    public bool IsQueueRunning
    {
        get 
        {
            return isQueueRunning;
        }
        set
        {
            isQueueRunning = value;
            TurnManager.Instance.SetEndTurnButton(!value);
        }

    }bool isQueueRunning;
    //드로우 관련 작업은 여기에 보관됩니다.
    Queue<JobRoutine> jobQueue = new();

    const string DRAW_QUEUE_TWEEN_ID = "DrawQueue";



    /// <summary>
    /// 핸드의 카드들의 Order를 정비합니다.
    /// </summary>
    public void SetOriginalOrder()
    {
        for (int i = 0; i < HandCards.Count; i++)
        {
            HandCards[i].Order.SetOriginalOrder(i);
        }
    }

    /// <summary>
    /// 손패의 카드들을 둥글게 정렬합니다.
    /// 드로우 할때마다 호출됩니다.
    /// </summary>
    public void SetHandShape()
    {
        int handCount = HandCards.Count;

        //각각의 카드가 얼만큼 띄엄띄엄있을지
        float[] objLerps = new float[handCount];

        switch (handCount)
        {
            case 1: objLerps = new float[] { 0.5f }; break;
            case 2: objLerps = new float[] { 0.37f, 0.63f }; break;
            case 3: objLerps = new float[] { 0.25f, 0.5f, 0.75f }; break;
            case 4: objLerps = new float[] { 0.12f, 0.37f, 0.62f, 0.87f }; break;
            default:
                float interval = 1f / (handCount - 1);
                for (int i = 0; i < handCount; i++)
                    objLerps[i] = interval * i;
                break;
        }

        for (int i = 0; i < handCount; i++)
        {
            Vector3 targetPos = Vector3.Lerp(HandPositionLeft.position, HandPositionRight.position, objLerps[i]);
            var targetRot = Quaternion.identity;
            //원의방정식 계산
            if (handCount >= 4)
            {
                float curve = Mathf.Sqrt(Mathf.Pow(0.5f, 2) - Mathf.Pow(objLerps[i] - 0.5f, 2));

                targetPos.y += curve;
                //회전 보간
                targetRot = Quaternion.Slerp(HandPositionLeft.rotation, HandPositionRight.rotation, objLerps[i]);
            }
            CardInstance card = HandCards[i];

            card.OriginalTransform.position = targetPos;
            card.OriginalTransform.rotation = targetRot;
            card.OriginalTransform.scale = Vector3.one * CardSize;

            //손에 들고있는 카드는 일단 정비하지않아요
            if (card == selectCard)
                continue;

            card.KillAllSequences();
            card.Sequence = DOTween.Sequence();

            card.Sequence.SetId(DRAW_QUEUE_TWEEN_ID)
                         .Append(card.transform.DOMove(targetPos, 0.25f))
                         .Join(card.transform.DORotateQuaternion(targetRot, 0.25f))
                         .Join(card.transform.DOScale(Vector3.one * CardSize, 0.25f));
        }
    }

    /// <summary>
    /// 손패의 카드들을 둥글게 정렬합니다.
    /// 특정 카드를 제외합니다.(카드 선택 화면 전용)
    /// 선택된 카드를 제외하고 정렬합니다.
    /// </summary>
    public void SetHandShape(CardInstance exceptionCard)
    {
        int handCount = HandCards.Count;

        //각각의 카드가 얼만큼 띄엄띄엄있을지
        float[] objLerps = new float[handCount];

        switch (handCount)
        {
            case 1: objLerps = new float[] { 0.5f }; break;
            case 2: objLerps = new float[] { 0.37f, 0.63f }; break;
            case 3: objLerps = new float[] { 0.25f, 0.5f, 0.75f }; break;
            case 4: objLerps = new float[] { 0.12f, 0.37f, 0.62f, 0.87f }; break;
            default:
                float interval = 1f / (handCount - 1);
                for (int i = 0; i < handCount; i++)
                    objLerps[i] = interval * i;
                break;
        }

        for (int i = 0; i < handCount; i++)
        {
            Vector3 targetPos = Vector3.Lerp(HandPositionLeft.position, HandPositionRight.position, objLerps[i]);
            var targetRot = Quaternion.identity;
            //원의방정식 계산
            if (handCount >= 4)
            {
                float curve = Mathf.Sqrt(Mathf.Pow(0.5f, 2) - Mathf.Pow(objLerps[i] - 0.5f, 2));

                targetPos.y += curve;
                //회전 보간
                targetRot = Quaternion.Slerp(HandPositionLeft.rotation, HandPositionRight.rotation, objLerps[i]);
            }
            CardInstance card = HandCards[i];

            //예외 카드는 넘깁니다.
            if (card == exceptionCard || card.IsSelected)
                continue;

            card.OriginalTransform.position = targetPos;
            card.OriginalTransform.rotation = targetRot;
            card.OriginalTransform.scale = Vector3.one * CardSize;

            //손에 들고있는 카드는 일단 정비하지않아요
            if (card == selectCard)
                continue;

            Debug.Log("HandShape");
            card.KillAllSequences();
            card.Sequence = DOTween.Sequence();

            card.Sequence.SetId(DRAW_QUEUE_TWEEN_ID)
                         .Append(card.transform.DOMove(targetPos, 0.25f))
                         .Join(card.transform.DORotateQuaternion(targetRot, 0.25f))
                         .Join(card.transform.DOScale(Vector3.one * CardSize, 0.25f));
        }
    }


    #region new draw system

    public void EnqueueDrawCard(int drawCount)
    {
        if (jobQueue.Count == 0 && IsQueueRunning == false)
        {
            IsQueueRunning = true;
            jobQueue.Enqueue(new JobRoutine(JobType.Draw, DrawCard(drawCount)));
            RunJobRoutine();
        }
        else
        {
            jobQueue.Enqueue(new JobRoutine(JobType.Draw, DrawCard(drawCount)));
        }
    }

    public void RunJobRoutine()
    {
        CardOutlineUpdate(false);
        // 다음 큐 실행
        if (jobQueue.Count != 0)
        {
            JobRoutine jobRoutine = jobQueue.Dequeue();

            StartCoroutine(jobRoutine.JobCo);
        }
        else
        {
            IsQueueRunning = false;


            // 콜라이더 활성화 및 후처리
            foreach (CardInstance card in HandCards)
                card.SetCollider(true);
            CardOutlineUpdate();
            CardInfoUpdateAll();
        }
    }


    public IEnumerator DrawCard(int drawCount)
    {
        // 이번 드로우에서 뽑을 카드들을 미리 확정
        List<CardInstance> drawBuffer = new List<CardInstance>();
        yield return StartCoroutine(MakeDrawBuffer(drawCount, drawBuffer));

        // 버퍼의 카드를 순서대로 핸드에 추가하며 연출
        for (int i = 0; i < drawBuffer.Count; i++)
        {
            yield return StartCoroutine(TryDrawCard(drawBuffer[i]));
            yield return Utility.WaitForSeconds(0.1f);
        }

        // 트윈 애니메이션이 끝날 때까지 대기
        while (DG.Tweening.DOTween.IsTweening(DRAW_QUEUE_TWEEN_ID))
            yield return null;

        //// 콜라이더 활성화 및 후처리

        // 다음 큐 실행
        RunJobRoutine();
    }


    IEnumerator TryDrawCard(CardInstance card)
    {
        // 핸드가 가득 차면 스킵
        if (HandCards.Count >= 10 || card == null)
            yield break;

        // 실제로 핸드에 추가
        HandCards.Add(card);
        //뽑을때 한번 위치 초기화
        card.transform.position = DeckPosition.position;
        card.ShowCard();

        ////버그 방지용으로 콜라이더를 항상 꺼줍니다.
        SetOriginalOrder();
        SetHandShape();

        // 🔸 드로우 효과 실행
        card.CardFunction.OnDraw(card);

        yield return null;
    }

    /// <summary>
    /// 이번 드로우에 뽑을 카드들을 미리 확정하는 버퍼 생성 함수
    /// </summary>
    IEnumerator MakeDrawBuffer(int drawCount, List<CardInstance> buffer)
    {
        for (int i = 0; i < drawCount; i++)
        {
            if (HandCards.Count + buffer.Count >= 10)
                break;

            // 덱이 비었으면 사용된 카드로부터 셔플
            if (CardDeckInstance.CardCount == 0)
            {
                if (UsedCardsInstance.CardCount == 0)
                    break;

                yield return StartCoroutine(UsedToDeck());
            }

            // 덱이 비었으면 종료
            if (CardDeckInstance.CardCount == 0)
                break;

            // 이번 드로우에서 실제로 뽑을 카드 예약
            CardInstance reservedCard = CardDeckInstance.PopCard(); // 새로 추가할 함수
            buffer.Add(reservedCard);
        }

        yield return null;
    }

    /// <summary>
    /// Used에서 덱으로 카드를 옮겨옵니다
    /// </summary>
    IEnumerator UsedToDeck()
    {
        // 덱이나 핸드에 변화가 있을 수 있기 때문에 콜라이더 비활성화
        // 큐가 끝나면 활성화 해줍니다.
        foreach (CardInstance card in HandCards)
            card.SetCollider(false);
        CardOutlineUpdate(false);

        //실행된 순간의 카드들만 옮겨줍니다.
        List<CardInstance> targetCards = UsedCardsInstance.Cards.ToList();
        

        foreach (CardInstance targetCard in targetCards)
        {
            targetCard.transform.position = DeckPosition.position;

            UsedCardsInstance.RemoveCard(targetCard);
        }

        yield return Utility.WaitForSeconds(0.4f);

        //한번 섞고 덱에 넣습니다.
        CardDeckInstance.FYShuffle(targetCards);
        //이펙트
        yield return StartCoroutine(cardShuffleEffect.ShowEffect(targetCards.Count));

        foreach (CardInstance card in targetCards)
        {
            if (!CardDeckInstance.Cards.Contains(card))
                CardDeckInstance.AddCard(card);
            //    DebugManager.Instance.ShowDebugLog("UsedToDeck : Deck에 중복 삽입 시도 확인");
        }

        yield return Utility.WaitForSeconds(0.3f);
    }

    ////모든 드로우가 끝날때까지 기다리고, 손패를 비활성화
    //    //// 덱이나 핸드에 변화가 있을 수 있기 때문에 콜라이더 비활성화
    //    //// 큐가 끝나면 활성화 해줍니다.

    //           jobQueue.Count > 0 ||

    //드로우가 아니더라도 작업을 큐에 넣습니다.
    public void EnqueueSequence(IEnumerator sequence)
    {
        if (jobQueue.Count == 0 && IsQueueRunning == false)
        {
            IsQueueRunning = true;
            jobQueue.Enqueue(new JobRoutine(JobType.Sequence, sequence));
            RunJobRoutine();
        }
        else
        {
            jobQueue.Enqueue(new JobRoutine(JobType.Sequence, sequence));
        }
    }

    #endregion


    public void EnqueueSetCardToUsed(CardInstance card)
    {
        // 이미 Used에 들어간 카드는 다시 큐에 넣지 않음
        if (UsedCardsInstance.Cards.Contains(card))
        {
            Debug.LogError($"EnqueueSetCardToUsed : Used에 중복 삽입 시도 확인");
            //DebugManager.Instance.ShowDebugLog("EnqueueSetCardToUsed : Used에 중복 삽입 시도 확인");
            return;
        }

        if (jobQueue.Count == 0 && IsQueueRunning == false)
        {
            IsQueueRunning = true;
            jobQueue.Enqueue(new JobRoutine(JobType.SetCardToUsed, SetCardToUsed(card)));
            RunJobRoutine();
        }
        else
        {
            jobQueue.Enqueue(new JobRoutine(JobType.SetCardToUsed, SetCardToUsed(card)));
        }
    }

    IEnumerator SetCardToUsed(CardInstance card)
    {
        // 이미 들어가 있으면 스킵
        if (UsedCardsInstance.Cards.Contains(card))
        {
            Debug.LogError($"SetCardToUsed : Used에 중복 삽입 시도 확인");
            //DebugManager.Instance.ShowDebugLog("SetCardToUsed : Used에 중복 삽입 시도 확인");
            if (jobQueue.Count != 0)
                RunJobRoutine();
            else
            {
                IsQueueRunning = false;
                CardOutlineUpdate();
                CardInfoUpdateAll();
            }
            yield break;
        }

        card.SetCollider(false);
        card.UpdateOutline(false);
        ////들어가야할 카드가 선택되어있으면 안됨

        ResetCardData(card);

        card.KillAllSequences();
        card.Sequence = DOTween.Sequence();
        card.Sequence.Append(card.transform.DOScale(Vector3.one * 0.2f, 0.2f))
                    .Append(card.transform.DOMove(UsedPosition.position, 0.2f).SetEase(Ease.InQuad))
                    .Join(card.transform.DORotateQuaternion(UsedPosition.rotation, 0.2f).SetEase(Ease.InQuad));

        yield return card.Sequence.WaitForCompletion();
        card.HideCard();
        UsedCardsInstance.AddCard(card);

        //이전 큐의 드로우 모션 끝날때까지 대기
        while (DG.Tweening.DOTween.IsTweening(DRAW_QUEUE_TWEEN_ID))
            yield return null;

        if (jobQueue.Count != 0)
        {
            RunJobRoutine();
        }
        else
        {
            IsQueueRunning = false;
            //큐가 끝났으면 콜라이더 활성화. 문제있을 수 있음
            foreach (CardInstance c in HandCards)
                c.SetCollider(true);
            CardOutlineUpdate();
            CardInfoUpdateAll();
            //여기까지
        }

        //일단 추가
        {
            SetOriginalOrder();
            SetHandShape();
        }
    }

    /// <summary>
    /// 조금 더 빠르게 카드를 덱에 집어넣습니다.(턴 종료시만 매니저에서 호출)
    /// </summary>
    /// <param name="card"></param>
    /// <returns></returns>
    public IEnumerator SetCardToUsedFaster(CardInstance card)
    {
        HandCards.Remove(card);
        UsedCardsInstance.AddCard(card);

        card.SetCollider(false);
        card.UpdateOutline(false);

        ResetCardData(card);

        card.KillAllSequences();
        card.Sequence = DOTween.Sequence();
        card.Sequence.Append(card.transform.DOScale(Vector3.one * 0.2f, 0.09f))
                    .Append(card.transform.DOMove(UsedPosition.position, 0.09f).SetEase(Ease.InQuad))
                    .Join(card.transform.DORotateQuaternion(UsedPosition.rotation, 0.09f).SetEase(Ease.InQuad)
                    .OnComplete(() => card.HideCard()));

        yield return Utility.WaitForSeconds(0.1f);
    }

    /// <summary>
    /// 손패를 전부 덱으로 옮깁니다.
    /// 드로우 큐를 사용하지 않기때문에 턴 종료시에만(드로우 큐가 빌때에만) 가능합니다.
    /// </summary>
    public IEnumerator ClearHandCards()
    {
        List<CardInstance> handCards = HandCards.ToList();
        HandCards.Clear();

        //덱으로 옮길때 콜라이더를 우선 다 잠급니다.
        foreach (CardInstance card in handCards)
        {
            card.SetCollider(false);
            card.UpdateOutline(false);
        }

        //덱으로 넣습니다.
        for (int i= handCards.Count-1; i >=0; i--)
        {
            yield return StartCoroutine(SetCardToUsedFaster(handCards[i]));
        }

        yield return null;
    }



    /// <summary>
    /// 카드를 버립니다.
    /// </summary>
    public void Discard(CardInstance card)
    {
        //핸드에서 지우되 덱에는 아직 넣지 않습니다.
        HandCards.Remove(card);

        card.SetCollider(false);
        card.UpdateOutline(false);

        card.CardFunction.OnDiscard(card);
        TurnManager.Instance.DiscardCountThisTurn++;

        FieldManager.Instance.PlayerInstance.UseChaosBuff();


        if(ActiveCardInstance.Contains(card))
            EnqueueSetCardToUsed(card);
        ////버리기 관련 동적 텍스트 업데이트
    }

    /// <summary>
    /// 카드를 소멸시킵니다.
    /// 손에서 소멸될 경우 이펙트를 손에서 보여주며, 
    /// 덱에서 소멸될경우 이펙트를 꺼내서 보여줍니다.
    /// </summary>
    /// <param name="card"></param>
    public void ConsumeCard(CardInstance card, bool isConsumedOnHand = true)
    {
        ActiveCardInstance.Remove(card);
        card.SetCollider(false);
        card.UpdateOutline(false);

        if (selectCard == card)
            selectCard = null;

        //손에서 소멸될 경우
        if (isConsumedOnHand)
        {
            HandCards.Remove(card);
            card.ConsumeCard(1.2f);

            {
                SetOriginalOrder();
                SetHandShape();
            }
        }
        else // 이외엔 덱에서 꺼내서 보여줘
        {
            Debug.Log("IsOnDeck");
            CardDeckInstance.Cards.Remove(card);
            UsedCardsInstance.Cards.Remove(card);

            card.ShowCard();
            //중앙에서 보여줘
            // 카드가 현재 있는 깊이를 기준으로 스크린→월드 변환
            float depth = CinemachineManager.Instance.UICamera.WorldToScreenPoint(card.transform.position).z;
            Vector3 screenCenter = new(Screen.width / 2f, Screen.height / 2f, depth);
            Vector3 worldCenter = CinemachineManager.Instance.UICamera.ScreenToWorldPoint(screenCenter);

            card.ConsumeCard(1.2f, worldCenter);
        }

        CardInfoUpdateAll();
    }



    /// <summary>
    /// 카드의 데이터를 원형으로 되돌립니다.
    /// </summary>
    void ResetCardData(CardInstance card)
    {
        card.Value = card.OriginalData.Value;
        card.Cost = card.OriginalData.Cost;
    }

    /// <summary>
    /// 해당 카드를 몇 장 보유하고 있는지 셉니다.
    /// </summary>
    public int GetCardCountInDeck(CardEntitySO cardSO)
    {
        int count = 0;

        for (int i = 0; i < ActiveCardInstance.Count; i++)
        {
            CardInstance card = ActiveCardInstance[i];

            if (card.OriginalData.CardEntitySO == cardSO)
            {
                count += 1;
            }
        }

        return count;
    }
}

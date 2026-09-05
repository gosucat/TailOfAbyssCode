using System.Collections;
using DG.Tweening;
using UnityEngine;

public partial class CardBattleManager
{

    public void EnqueueCreateCardAndNegativeEffect(CardEntitySO cardSo, Transform fromTransform, bool insertToDeckRandom = true)
    {
        EnqueueSequence(CreateCardAndNegativeEffect(cardSo, fromTransform, insertToDeckRandom));
    }

    public IEnumerator CreateCardAndNegativeEffect(CardEntitySO cardSo, Transform startTransform, bool insertToDeckRandom)
    {
        CardInstance card = CreateCard(cardSo);

        Vector3 screenStartPos = Utility.FieldWorldToUIWorldPos(startTransform.position);
        card.transform.position = screenStartPos + new Vector3(0f, 0.5f, 0f);

        card.transform.rotation = Quaternion.identity;
        card.transform.localScale = Vector3.one * 0.1f;

        card.SetCollider(false);
        card.StartNegativeOutline();
        card.ShowCard();
        card.KillAllSequences();

        Vector3 popTarget = card.transform.position + new Vector3(0f, 0.6f, 0f);

        Vector3 uiCenter = Utility.GetUICenterWorldPos();

        // (start * 1 + center * 4) / 5  -> 화면 중앙쪽에 더 가까운 내분점
        Vector3 focusTarget = (card.transform.position + uiCenter * 2f) / 3f;

        Vector3 deckTarget = DeckPosition.position;

        card.Sequence = DOTween.Sequence();
        // 1) 화면 중앙 쪽(내분점)으로 이동
        card.Sequence.Append(card.transform.DOMove(focusTarget, 0.3f).SetEase(Ease.OutQuad));
        card.Sequence.Join(card.transform.DOScale(Vector3.one * 0.5f, 0.3f).SetEase(Ease.OutBack));
        card.Sequence.AppendInterval(0.25f);
        card.Sequence.Join(card.OutlineFadeTo(true, 0.5f));
        card.Sequence.AppendInterval(0.15f);
        card.Sequence.Join(card.transform.DOShakePosition(0.3f, new Vector3(0.12f, 0.12f, 0f), 25, 90f, false, true));
        card.Sequence.Join(card.transform.DOShakeRotation(0.3f, new Vector3(0f, 0f, 6f), 20, 90f, true));

        // 2) 덱으로 이동
        card.Sequence.Append(card.transform.DOMove(deckTarget, 0.28f).SetEase(Ease.InQuad));
        card.Sequence.Join(card.transform.DORotateQuaternion(DeckPosition.rotation, 0.28f).SetEase(Ease.InQuad));
        card.Sequence.Join(card.transform.DOScale(Vector3.one * 0.22f, 0.28f).SetEase(Ease.InQuad));
        card.Sequence.Join(card.OutlineFadeTo(false, 0.1f));

        yield return card.Sequence.WaitForCompletion();
        card.HideCard();
        card.EndNegativeOutline();

        int insertIndex = 0;

        if (insertToDeckRandom)
        {
            insertIndex = Random.Range(0, CardDeckInstance.CardCount + 1);
        }

        CardDeckInstance.InsertCard(insertIndex, card);

        card.transform.position = DeckPosition.position;
        card.transform.rotation = DeckPosition.rotation;
        card.transform.localScale = Vector3.one * 0.3f;

        yield return null;
    }

    public IEnumerator CreateCardAndEffect(CardEntitySO cardSo, Transform startTransform, bool insertToDeckRandom)
    {
        CardInstance card = CreateCard(cardSo);

        Vector3 screenStartPos = Utility.FieldWorldToUIWorldPos(startTransform.position);
        card.transform.position = screenStartPos + new Vector3(0f, 0.5f, 0f);

        card.transform.rotation = Quaternion.identity;
        card.transform.localScale = Vector3.one * 0.1f;

        card.SetCollider(false);
        card.ShowCard();
        card.KillAllSequences();

        Vector3 popTarget = card.transform.position + new Vector3(0f, 0.6f, 0f);

        Vector3 uiCenter = Utility.GetUICenterWorldPos();

        // (start * 1 + center * 4) / 5  -> 화면 중앙쪽에 더 가까운 내분점
        Vector3 focusTarget = (card.transform.position + uiCenter * 2f) / 3f;

        Vector3 deckTarget = DeckPosition.position;

        card.Sequence = DOTween.Sequence();
        // 1) 화면 중앙 쪽(내분점)으로 이동
        card.Sequence.Append(card.transform.DOMove(focusTarget, 0.3f).SetEase(Ease.OutQuad));
        card.Sequence.Join(card.transform.DOScale(Vector3.one * 0.5f, 0.3f).SetEase(Ease.OutBack));
        card.Sequence.AppendInterval(0.4f);
        card.Sequence.Join(card.transform.DOShakePosition(0.3f, new Vector3(0.12f, 0.12f, 0f), 25, 90f, false, true));
        card.Sequence.Join(card.transform.DOShakeRotation(0.3f, new Vector3(0f, 0f, 6f), 20, 90f, true));

        // 2) 덱으로 이동
        card.Sequence.Append(card.transform.DOMove(deckTarget, 0.28f).SetEase(Ease.InQuad));
        card.Sequence.Join(card.transform.DORotateQuaternion(DeckPosition.rotation, 0.28f).SetEase(Ease.InQuad));
        card.Sequence.Join(card.transform.DOScale(Vector3.one * 0.22f, 0.28f).SetEase(Ease.InQuad));
        yield return card.Sequence.WaitForCompletion();
        card.HideCard();

        int insertIndex = 0;

        if (insertToDeckRandom)
        {
            insertIndex = Random.Range(0, CardDeckInstance.CardCount + 1);
        }

        CardDeckInstance.InsertCard(insertIndex, card);

        card.transform.position = DeckPosition.position;
        card.transform.rotation = DeckPosition.rotation;
        card.transform.localScale = Vector3.one * 0.3f;

        yield return null;
    }
}
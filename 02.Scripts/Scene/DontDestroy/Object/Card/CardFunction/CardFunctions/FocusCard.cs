using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CatsWork;

//집중 카드
public class FocusCard : CardFunctionBase
{
    public override void OnUsed(CardInstance card, Tile targetTile = null)
    {
        base.OnUsed(card, targetTile);

        CardBattleManager.Instance.EnqueueDrawCard(1);

        new Focus().Apply(FieldManager.Instance.PlayerInstance, card.Value);

        FieldManager.Instance.PlayerInstance.PlayAnim(card.OriginalData.CardEntitySO);
    }
}
using System.Collections;
using System.Collections.Generic;
using CatsWork;
using UnityEngine;
using UnityEngine.InputSystem;

public class CardFunctionBase : ICardFunction
{
    public virtual bool UseValidateCheck(CardInstance card) { return true; }
    public virtual void OnDraw(CardInstance card) { }                     //드로우 시
    public virtual void OnUsed(CardInstance card, Tile targetTile = null)
    {
        CardBattleManager.Instance.HandCards.Remove(card);
        card.SetCollider(false);
        card.UpdateOutline(false);
    } // 사용시
    public virtual void OnTurnStart(CardInstance card) { }               // 턴 시작시
    public virtual void OnTurnEnd(CardInstance card) { }                 // 턴 종료시
    public virtual void OnDiscard(CardInstance card) { }                     // 버릴 시
    //public virtual void RefreshDynamicTextInfo(CardInstance card) { }    // 동적 텍스트가 있을 경우 업데이트
    public virtual string GetDynamicInfoValue(CardInstance card) { return null; } // 카드 동적 수치 정보 가져오기

    public virtual void OnDestroy(CardInstance card) { }


    /// <summary>
    /// 타일에 데미지를 줍니다.
    /// 버프등등을 고려합니다.
    /// </summary>
    protected virtual void DamageToTiles(List<Tile> targetTiles, int damage, CardInstance card, bool dontTargetPlayer = true)
    {
        int cardValue = damage;
        BuffBase usedBuff = null;

        if (card.CardType == CardType.Martial)
        {
            //물리 카드에 아드레날린 적용
            foreach (BuffBase buff in FieldManager.Instance.PlayerInstance.Buffs)
            {
                if (buff.BuffType == BuffType.Adrenaline)
                {
                    cardValue += buff.Stack;
                    usedBuff = buff;
                    break;
                }
            }
        }
        else if (card.CardType == CardType.Magic)
        {
            //마법 카드에 집중 적용
            foreach (BuffBase buff in FieldManager.Instance.PlayerInstance.Buffs)
            {
                if (buff.BuffType == BuffType.Focus)
                {
                    cardValue += buff.Stack;
                    //적용 후 버프 지우기
                    buff.Remove();
                    break;
                }
            }
        }

        bool isDamageTaken = false;

        // 스킬 발동 시점에 타일과 그 위의 대상을 캐싱합니다.
        // 타일 순회 중 유닛이 죽어 새로운 유닛이 스폰되더라도, 이 리스트에 없는 유닛은 안전합니다.
        List<(Tile targetTile, UnitBase targetUnit)> preCalculatedTargets = new();

        foreach (Tile tile in targetTiles)
        {
            // 거대 유닛의 경우 여러 타일을 점유하므로 리스트에 여러 번 들어갑니다.
            preCalculatedTargets.Add((tile, tile.MyUnit));
        }

        // 캐싱된 리스트를 순회하며 실제 타격과 이펙트를 진행합니다.
        foreach (var target in preCalculatedTargets)
        {
            Tile tile = target.targetTile;
            UnitBase originalUnit = target.targetUnit;

            //오브젝트는 타일을 기준으로 무조건 타격합니다.
            tile.TakeDamageToObject(cardValue, FieldManager.Instance.PlayerInstance);

            //해당 타일에 원래 서있던 유닛이 존재하는 경우에만 타격합니다.
            if (originalUnit != null)
            {
                if (dontTargetPlayer && originalUnit.UnitType == UnitType.Player)
                    continue;

                if (originalUnit.UnitType == UnitType.Enemy)
                    isDamageTaken = true;

                // SO에 할당된 이펙트 프리팹이 존재할 경우, 유닛의 스프라이트 위치에 생성합니다.
                if (card.OriginalData.CardEntitySO.CardEffectPrefab != null)
                {
                    GameObject effect = Object.Instantiate(card.OriginalData.CardEntitySO.CardEffectPrefab, originalUnit.GetSpritePosition(), Quaternion.identity);
                }

                originalUnit.TakeDamage(cardValue, FieldManager.Instance.PlayerInstance);
            }
        }

        //적용한 아드레날린/집중은 지워줍니다.
        if (isDamageTaken && usedBuff != null)
            usedBuff.Remove();
    }

    protected virtual void DamageToTiles(Tile targetTile, int damage, CardInstance card, bool dontTargetPlayer = true)
    {
        List<Tile> temp = new();
        temp.Add(targetTile);
        DamageToTiles(temp, damage, card, dontTargetPlayer);
    }
}
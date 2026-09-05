using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CatsWork;

// 폴짝B
// 폴짝 실행 뒤 기민함 부여
public class HopB : CardFunctionBase
{
    public override void OnUsed(CardInstance card, Tile targetTile = null)
    {
        base.OnUsed(card, targetTile);

        Player player = FieldManager.Instance.PlayerInstance;
        if (targetTile == null || player == null) return;

        // 타격 대상 존재 여부 확인
        bool isDamageDealt = targetTile.MyUnit != null;

        // 공격 애니메이션 및 데미지 적용
        player.PlayAnim(card.OriginalData.CardEntitySO);
        DamageToTiles(targetTile, card.Value, card);


        if (!isDamageDealt)
        {
            return;
        }

        // 뒤로 물러나기
        // 목표 타일을 향한 방향을 구한 뒤, 현재 위치에서 그 방향을 빼서 반대 위치를 구합니다.
        Vector2Int dir = FieldManager.Instance.GetDir(player.GridPosition, targetTile.GridPosition);
        Vector2Int destPos = player.GridPosition - dir;

        // 이동하려는 타일이 맵 안에 존재하고 비어있다면(Empty) 이동합니다.
        if (FieldManager.Instance.Tiles.TryGetValue(destPos, out Tile destTile))
        {
            if (destTile.CurrentState == Tile.TileState.Empty)
            {
                player.Move(destTile);
            }
        }

        CardBattleManager.Instance.EnqueueDrawCard(2);

        //기민함을 부여합니다.
        new Haste().Apply(player, 1);
    }
}
using System.Collections.Generic;
using CatsWork;
using UnityEngine;

// 리비엘식: 수렴
// 필드의 모든 인장을 대상 위치 한 칸으로 끌어모아 피해를 합산한다.
public class LibielArtsConvergence : CardFunctionBase
{
    // 생존 인장이 자리를 잡은 뒤 나머지가 날아들기 시작하기까지의 간격
    const float AbsorbStartDelay = 0.12f;
    // 흡수 인장들이 하나씩 차례로 날아드는 간격
    const float AbsorbInterval = 0.08f;

    public override void OnUsed(CardInstance card, Tile targetTile = null)
    {
        base.OnUsed(card, targetTile);

        Player player = FieldManager.Instance.PlayerInstance;
        CardEntitySO so = card.OriginalData.CardEntitySO;

        if (targetTile == null)
        {
            player.PlayAnim(so);
            return;
        }

        // 인장이 아닌 오브젝트가 있는 칸에는 모을 수 없다. (SealOfLight 와 동일한 방어 코드)
        if (targetTile.MyObject != null && !(targetTile.MyObject is TrapBase))
        {
            Debug.LogWarning("LibielArtsConvergence 인장을 모을 수 없는 칸이 지정되었음");
            player.PlayAnim(so);
            return;
        }

        List<TrapBase> traps = GetFieldTraps();
        if (traps.Count == 0)
        {
            player.PlayAnim(so);
            return;
        }

        TrapBase survivor = GetSurvivor(traps, targetTile);
        int totalDamage = CalculateTotalDamage(card, traps);

        //--- 로직 확정 구간 ---
        // 연출이 끝나기 전에 플레이어가 다음 카드를 쓸 수 있으므로
        // 인장의 위치/피해/개수는 이 안에서 전부 확정한다.
        List<TrapBase> absorbed = new();
        foreach (TrapBase trap in traps)
        {
            if (trap == survivor) continue;

            // 게임 로직에서 즉시 분리한다. 이후로는 순수한 연출용 오브젝트다.
            // 타일에서 떼지 않고 대상 칸으로 Move 하면, 이 인장이 사라질 때
            // 생존 인장의 타일 등록까지 지워져 밟아도 발동하지 않는 유령이 된다.
            if (FieldManager.Instance.Tiles.TryGetValue(trap.GridPosition, out Tile trapTile))
                trapTile.DisposePlaceableObject();

            BattleSceneManager.Instance.RemovePlaceableObject(trap);
            absorbed.Add(trap);
        }

        // 생존 인장이 먼저 대상 칸에 자리를 잡는다.
        if (survivor.GridPosition != targetTile.GridPosition)
            survivor.Move(targetTile);

        survivor.SetDamage(totalDamage);

        //--- 연출 구간 ---
        Vector3 destination = FieldManager.Instance.GetTilePosition(targetTile);
        for (int i = 0; i < absorbed.Count; i++)
            absorbed[i].AbsorbInto(destination, AbsorbStartDelay + i * AbsorbInterval);

        player.PlayAnim(so);
    }



    /// <summary>
    /// 인장들이 합쳐졌을 때의 최종 피해.
    /// 강화판은 이 메서드만 오버라이드하면 카드 설명의 미리보기 수치까지 함께 따라온다.
    /// </summary>
    protected virtual int CalculateTotalDamage(CardInstance card, List<TrapBase> traps)
    {
        int total = 0;
        foreach (TrapBase trap in traps)
            total += trap.TrapDamage;

        return total;
    }

    /// <summary>
    /// 대상 칸에 인장이 있으면 그 인장이, 없으면 피해가 가장 높은 인장이 살아남는다.
    /// 살아남은 인장의 종류(빛/그림자)와 발동 횟수가 결과에 그대로 유지된다.
    /// </summary>
    private TrapBase GetSurvivor(List<TrapBase> traps, Tile targetTile)
    {
        if (targetTile.MyObject is TrapBase trapOnTarget)
            return trapOnTarget;

        TrapBase survivor = traps[0];
        foreach (TrapBase trap in traps)
        {
            if (trap.TrapDamage > survivor.TrapDamage)
                survivor = trap;
        }

        return survivor;
    }

    protected List<TrapBase> GetFieldTraps()
    {
        List<TrapBase> traps = new();
        foreach (PlaceableObjectBase obj in BattleSceneManager.Instance.PlaceableObjects)
        {
            if (obj is TrapBase trap)
                traps.Add(trap);
        }

        return traps;
    }
}

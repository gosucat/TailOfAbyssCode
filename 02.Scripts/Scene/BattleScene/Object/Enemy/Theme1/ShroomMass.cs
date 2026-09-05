using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CatsWork;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEditor.PlayerSettings;

public class ShroomMass : UnitBase
{
    [Header("ShroomMass Settings")]
    [SerializeField] private GameObject sporeShroomPrefab; // 포자 버섯 프리팹
    [SerializeField] private GameObject runningShroomPrefab; // 달리는 버섯 프리팹
    [SerializeField] private GameObject runningShrromEffect; // 소환이펙트

    [SerializeField] private int shroomSpawnCount = 2;     // 소환할 버섯 수
    [SerializeField] private int absorbHealAmount = 20;    // 흡수 시 회복량
    [SerializeField] private int absorbBuffAmount = 5;     // 흡수 시 공격력 증가량


    public override UnitType UnitType { get; } = UnitType.Enemy;

    public override List<Vector2Int> OccupiedGridOffset { get; } = new List<Vector2Int>()
    {
        new Vector2Int(0, 0),
        new Vector2Int(0, 1),
        new Vector2Int(1, 0),
        new Vector2Int(1, 1),
        new Vector2Int(2, 0),
        new Vector2Int(2, 1)

    };

    // 보스 패턴 페이즈 관리를 위한 Enum과 변수들
    private enum ShroomPhase { Phase1, Phase2, Phase3 }
    private enum ShroomAction
    {
        None,
        SummonShrooms,
        AbsorbShroom,
        WeakAttack,
        ReserveArtillery,
        WakeUpAndPoison,
        ShieldOnly,
        ShieldAndPoison
    }

    private ShroomPhase currentPhase = ShroomPhase.Phase1;
    private ShroomAction plannedAction = ShroomAction.None;
    private bool isPhaseCycle1 = true; // 페이즈 순환1과 2를 번갈아가며 사용하기 위한 플래그

    private UnitBase targetShroom = null;

    private List<Vector2Int?> artilleryTargetPosList = null;

    /// <summary>
    /// AI가 다음 행동을 업데이트 합니다.
    /// 기획된 보스의 1 -> 2 -> 3 페이즈 순서를 결정합니다.
    /// </summary>
    public override void CalculateNextBehavior()
    {
        List<UnitBase> activeShrooms = GetAllSporeShrooms();
        Vector2Int playerPos = FieldManager.Instance.PlayerInstance.GridPosition;
        List<BehaviorData> nextBehaviors = new List<BehaviorData>();

        //포자버섯 2기 소환
        void SummonShrooms()
        {
            plannedAction = ShroomAction.SummonShrooms;
            nextBehaviors.Add(new BehaviorData(this, BehaviorType.Summon));
        }
        //버섯 하나 흡수
        void AbsorbShroom(UnitBase shroom)
        {
            if (shroom != null)
            {
                plannedAction = ShroomAction.AbsorbShroom;
                nextBehaviors.Add(new BehaviorData(this, BehaviorType.Heal));
                nextBehaviors.Add(new BehaviorData(this, BehaviorType.Buff));
            }
            else
            {
                //버섯이 없다면 약한 직접 공격

                int distance = GetClosestDistanceToGrid(playerPos, GridPosition);
                if (distance > Range) return;
                
                plannedAction = ShroomAction.WeakAttack;
                nextBehaviors.Add(new BehaviorData(this, BehaviorType.Attack, GridPosition, playerPos, Mathf.RoundToInt(damage * 0.5f)));
            }
        }

        switch (currentPhase)
        {
            case ShroomPhase.Phase1: // 1. 포자버섯 소환 페이즈
                if (isPhaseCycle1)
                {
                    if (activeShrooms.Count <= 4)
                    {
                        //필드에 포자 버섯이 4기 이하라면 2기 소환
                        SummonShrooms();
                    }
                    else
                    {
                        // 주변에 버섯이 있다면 흡수
                        targetShroom = GetAdjacentSporeShroom(activeShrooms);
                        AbsorbShroom(targetShroom);
                    }
                }
                else
                {
                    //주변에 버섯이 있다면 흡수
                    targetShroom = GetAdjacentSporeShroom(activeShrooms);
                    if (targetShroom != null)
                    {
                        AbsorbShroom(targetShroom);
                    }
                    else
                    {
                        //없을때만 소환
                        SummonShrooms();
                    }

                }

                break;

            case ShroomPhase.Phase2: // 2. 광역 공격 페이즈
                plannedAction = ShroomAction.ReserveArtillery;

                // 이번 턴에 아직 인디케이터 예약을 하지 않았을 때만 실행
                if (artilleryTargetPosList == null)
                {
                    artilleryTargetPosList = new List<Vector2Int?>();

                    Vector2Int[] offsets = {
                        Vector2Int.zero,
                        Vector2Int.up,
                        Vector2Int.down,
                        Vector2Int.left,
                        Vector2Int.right
                    };

                    foreach (var offset in offsets)
                    {
                        Vector2Int targetPos = playerPos + offset;

                        // 타일이 실제로 필드 위에 존재하는지 체크 후 리스트에 추가
                        if (FieldManager.Instance.Tiles.ContainsKey(targetPos))
                        {
                            artilleryTargetPosList.Add(targetPos);
                        }
                    }

                    IndicatorSystem.Instance.ShowUnitAttackReserveIndicators(this, artilleryTargetPosList);
                }

                if(artilleryTargetPosList != null && artilleryTargetPosList.Count > 0)
                    nextBehaviors.Add(new BehaviorData(this, BehaviorType.Attack, GridPosition, artilleryTargetPosList, damage * 2));

                break;

            case ShroomPhase.Phase3: // 3. 포자버섯 변환 / 포자 방어막 페이즈
                if (isPhaseCycle1)
                {
                    // 3-1. 포자버섯 변환 페이즈
                    if (activeShrooms.Count > 0)
                    {
                        plannedAction = ShroomAction.WakeUpAndPoison;
                        nextBehaviors.Add(new BehaviorData(this, BehaviorType.Special)); // 일으키기 시각 효과용
                        int distance = GetClosestDistanceToGrid(playerPos, GridPosition);
                        if (distance > Range) return;
                        nextBehaviors.Add(new BehaviorData(this, BehaviorType.Attack, GridPosition, playerPos, damage)); // 중독 공격
                    }
                    else
                    {
                        plannedAction = ShroomAction.ShieldOnly;
                        nextBehaviors.Add(new BehaviorData(this, BehaviorType.Buff));
                    }
                }
                else
                {
                    // 3-2. 포자 방어막 페이즈
                    plannedAction = ShroomAction.ShieldAndPoison;
                    nextBehaviors.Add(new BehaviorData(this, BehaviorType.Buff)); // 방어막
                    int distance = GetClosestDistanceToGrid(playerPos, GridPosition);
                    if (distance > Range) return;
                    nextBehaviors.Add(new BehaviorData(this, BehaviorType.Attack, GridPosition, playerPos, damage)); // 중독 공격
                }

                break;
        }

        SetBehaviorData(nextBehaviors);
    }

    /// <summary>
    /// 보스 전용
    /// </summary>
    public override IEnumerator DoBehavior()
    {
        behaviorDatas.Add(new BehaviorData(this, BehaviorType.BehaviorPlayed));

        //기반 클래스의 DoBehavior 를 타지 않으므로 사망 검사를 직접 해야 한다.
        if (IsDying)
            yield break;

        LookAtPlayer();

        //각 행동을 StartCoroutine 으로 감싸면 이 유닛이 코루틴의 주인이 되어,
        //행동 도중 유닛이 파괴될 때 호출자(TurnManager)가 완료 통보를 영영 받지 못한다.
        //따라서 호출자의 코루틴 안에서 직접 이어 돌린다.
        switch (plannedAction)
        {
            case ShroomAction.SummonShrooms:
                yield return SummonShroomsCo();
                break;
            case ShroomAction.AbsorbShroom:
                yield return AbsorbShroomCo();
                break;
            case ShroomAction.WeakAttack:
                yield return WeakAttackCo();
                break;
            case ShroomAction.ReserveArtillery:
                yield return ReserveArtilleryCo();
                break;
            case ShroomAction.WakeUpAndPoison:
                yield return WakeUpShroomsCo();
                yield return new WaitForSeconds(0.2f);
                yield return PoisonAttackCo();
                break;
            case ShroomAction.ShieldOnly:
                yield return CastShieldCo();
                break;
            case ShroomAction.ShieldAndPoison:
                yield return CastShieldCo();
                yield return new WaitForSeconds(0.2f);
                yield return PoisonAttackCo();
                break;
        }

        plannedAction = ShroomAction.None;

        // (수정) 행동이 끝난 후, 다음 턴을 위해 페이즈를 넘깁니다.
        AdvancePhase();
    }

    public override IEnumerator DoStartTurnBehavior()
    {
        yield return null;
        ClearBehaviorData(false);
    }

    /// <summary>
    /// 보스의 행동이 성공적으로 끝난 직후 페이즈를 변경합니다.
    /// </summary>
    private void AdvancePhase()
    {
        switch (currentPhase)
        {
            case ShroomPhase.Phase1:
                currentPhase = ShroomPhase.Phase2; // 다음 페이즈로 전환
                break;
            case ShroomPhase.Phase2:
                currentPhase = ShroomPhase.Phase3; // 다음 페이즈로 전환
                artilleryTargetPosList = null; // 다음 사이클을 위해 곡사포 타겟 초기화 (리스트로 변경)
                break;
            case ShroomPhase.Phase3:
                currentPhase = ShroomPhase.Phase1;
                // 페이즈 전환
                isPhaseCycle1 = !isPhaseCycle1;
                break;
        }
    }


    #region Boss Behaviors

    //소환
    private IEnumerator SummonShroomsCo()
    {

        List<Tile> spawnableTiles = GetFrontEmptyTiles(shroomSpawnCount);
        foreach (Tile tile in spawnableTiles)
        {
            BattleSceneManager.Instance.AddEnemy(sporeShroomPrefab, tile);
        }

        animator.Play("Attack");

        yield return new WaitForSeconds(0.5f);
    }

    //흡수
    private IEnumerator AbsorbShroomCo()
    {
        if (targetShroom != null)
        {
            // 버섯 파괴
            targetShroom.TakeDamage(9999, this);

            // 능력치 증가 로직
            CurrentHp += absorbHealAmount;
            damage += absorbBuffAmount;

            DamagePopupManager.Instance.ShowDamage(GetSpritePosition(), absorbHealAmount); // 회복 팝업 (색상 변경 필요시 팝업 매니저에서 지원 필요)
                                                                                           // TODO: 공격력 증가(Buff) 텍스트 또는 이펙트 추가

            targetShroom = null;
        }

        animator.Play("Attack");
        yield return new WaitForSeconds(0.5f);
    }

    //약한 공격
    private IEnumerator WeakAttackCo()
    {
        //죽은 유닛은 공격하지 못한다.
        if (IsDying) yield break;

        LookAtPlayer();
        animator.Play("Attack");

        int weakDamage = Mathf.RoundToInt(damage * 0.5f);
        FieldManager.Instance.PlayerInstance.TakeDamage(weakDamage, this);

        Tile targetTile = FieldManager.Instance.Tiles[FieldManager.Instance.PlayerInstance.GridPosition];
        yield return AttackReaction(targetTile);
    }

    //예약된 곡사포 발사
    private IEnumerator ReserveArtilleryCo()
    {
        if (artilleryTargetPosList == null || artilleryTargetPosList.Count == 0) yield break;
        IndicatorSystem.Instance.PlayBehaviorHighlight(artilleryTargetPosList);

        yield return new WaitForSeconds(0.8f);

        //저장해둔 위치에 발사
        foreach (Vector2Int targetPos in artilleryTargetPosList)
        {
            FieldManager.Instance.Tiles[targetPos].TakeDamageToUnit(damage * 2, this, true);

        }

        animator.Play("Attack");

        yield return new WaitForSeconds(0.5f);
    }

    //일으키기
    private IEnumerator WakeUpShroomsCo()
    {
        List<UnitBase> activeShrooms = GetAllSporeShrooms();
        foreach (var shroom in activeShrooms)
        {
            int hp = shroom.CurrentHp;
            Vector2Int pos = shroom.GridPosition;
            Tile targetTile = FieldManager.Instance.Tiles[pos];
            shroom.DestroyUnit();

            UnitBase runningShroom = BattleSceneManager.Instance.AddEnemy(runningShroomPrefab, targetTile);
            runningShroom.CurrentHp = hp;

            yield return Utility.WaitForSeconds(0.2f);
            Instantiate(runningShrromEffect, runningShroom.GetSpritePosition(), Quaternion.identity);
        }

        animator.Play("Attack");

        yield return new WaitForSeconds(0.5f);
    }

    //
    private IEnumerator CastShieldCo()
    {
        new Solidity().Apply(this, 2);

        animator.Play("Attack");
        yield return new WaitForSeconds(0.5f);
    }

    private IEnumerator PoisonAttackCo()
    {
        //죽은 유닛은 공격하지 못한다.
        if (IsDying) yield break;

        LookAtPlayer();

        FieldManager.Instance.PlayerInstance.TakeDamage(damage, this);
        // TODO: 플레이어에게 '중독(Poison)' 디버프 부여 로직 추가

        Tile targetTile = FieldManager.Instance.Tiles[FieldManager.Instance.PlayerInstance.GridPosition];

        animator.Play("Attack");
        yield return AttackReaction(targetTile);
    }

    public override void TakeDamage(int damage, UnitBase attacker = null)
    {
        base.TakeDamage(damage, attacker);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// 필드에 존재하는 포자 버섯 목록을 반환합니다.
    /// </summary>
    private List<UnitBase> GetAllSporeShrooms()
    {
        List<UnitBase> sporeShrooms = new List<UnitBase>();

        foreach (UnitBase unit in BattleSceneManager.Instance.EnemyList)
        {
            if (unit is Sporeshroom shroom)
            {
                sporeShrooms.Add(shroom);
            }
        }

        return sporeShrooms;
    }

    /// <summary>
    /// 보스의 2x2 반경에 인접한 포자 버섯 1기를 찾아 반환합니다.
    /// </summary>
    private UnitBase GetAdjacentSporeShroom(List<UnitBase> allShrooms)
    {
        List<Vector2Int> myGrids = GetOccupiedGrids(GridPosition);

        foreach (var shroom in allShrooms)
        {
            foreach (var myGrid in myGrids)
            {
                // 인접(맨해튼 거리 1)한 타일에 버섯이 존재하는지 검사
                if (Utility.GetManhattanDistance(myGrid, shroom.GridPosition) == 1)
                {
                    return shroom;
                }
            }
        }
        return null;
    }

    /// <summary>
    /// 플레이어에게 가까운 쪽에 포자버섯들을 소환하기위한 위치를 가져옵니다.
    /// </summary>
    private List<Tile> GetFrontEmptyTiles(int count)
    {
        List<Tile> emptyTiles = new List<Tile>();

        // 보스가 차지하고 있는 기준 타일들
        List<Vector2Int> myGrids = GetOccupiedGrids(GridPosition);

        //플레이어에 가까운 방향으로 보스가 차지하고 있는 타일인 myGrids (0, 0)(0, 1)(1, 0)(1, 1) 타일들을 기준으로 빈 타일을 최대 두개 반환합니다.

        Vector2Int playerPos = FieldManager.Instance.PlayerInstance.GridPosition;
        HashSet<Tile> candidateTiles = new HashSet<Tile>(); // 중복 탐색을 막기 위한 HashSet

        // 상하좌우 탐색 (searchRadius 기준)
        Vector2Int[] offsets = {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        // 1. 보스의 몸체와 인접한 타일 중 빈 타일(Empty) 수집
        foreach (Vector2Int myGrid in myGrids)
        {
            foreach (Vector2Int offset in offsets)
            {
                Vector2Int neighborPos = myGrid + offset;

                // 해당 위치가 보스 자신의 몸체(2x2) 내부라면 제외
                if (myGrids.Contains(neighborPos)) continue;

                // 필드에 타일이 존재하고, 현재 상태가 빈 곳(Empty)인지 확인
                if (FieldManager.Instance.Tiles.TryGetValue(neighborPos, out Tile tile))
                {
                    if (tile.CurrentState == Tile.TileState.Empty)
                    {
                        candidateTiles.Add(tile);
                    }
                }
            }
        }

        // 2. 수집된 후보 타일들을 플레이어와의 맨해튼 거리가 가까운 순(오름차순)으로 정렬
        List<Tile> sortedCandidates = candidateTiles.ToList();
        sortedCandidates.Sort((a, b) =>
        {
            int distA = Utility.GetManhattanDistance(a.GridPosition, playerPos);
            int distB = Utility.GetManhattanDistance(b.GridPosition, playerPos);
            return distA.CompareTo(distB);
        });

        // 3. 정렬된 리스트에서 count만큼만 추출하여 반환
        for (int i = 0; i < sortedCandidates.Count && emptyTiles.Count < count; i++)
        {
            emptyTiles.Add(sortedCandidates[i]);
        }

        return emptyTiles;
    }

    #endregion


    //소울 생성 타이밍 이슈로 오버라이드
    protected override IEnumerator OnUnitDestroy()
    {
        //실제 타일 간섭 구간
        //플레이어를 제외하면 경험치를 떨어뜨려요. 플레이어는 override했음
        foreach (Vector2Int gridOffset in OccupiedGridOffset)
        {
            Vector2Int targetGrid = GridPosition + gridOffset;
            Tile targetTile = FieldManager.Instance.Tiles[targetGrid];
            targetTile.DisposeUnit();
        }
        BattleSceneManager.Instance.RemoveEnemy(this);

        if (TurnManager.Instance.IsMyTurn)
            BehaviorManager.Instance.CalcAllEnemyBehavior();


        StartCoroutine(CreateOrbLater(1.5f));

        //연출 구간
        int stateHash = Animator.StringToHash("Dead");
        if (animator.HasState(0, stateHash))
        {
            animator.Play(stateHash);

            yield return null;

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

            // 현재 재생 중인 애니메이션이 맞고, 진행도가 100% 미만인 동안 대기
            while (stateInfo.shortNameHash == stateHash && stateInfo.normalizedTime < 1.0f)
            {
                yield return null;
                stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            }
        }
        Dispose();
    }

    private IEnumerator CreateOrbLater(float delay)
    {
        yield return Utility.WaitForSeconds(delay);
        OrbManager.Instance.CreateOrb(sr.transform.position, expAmount);
        FullScreenShakeController.Instance.Shake(2.8f, 10f, true);
    }
}
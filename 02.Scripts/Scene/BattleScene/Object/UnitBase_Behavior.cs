using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using CatsWork;
using TMPro;

public partial class UnitBase
{
    protected List<BehaviorData> behaviorDatas = new();
    private List<BehaviorUI> activeBehaviorUIs = new();

    //행동 변경시 유닛 머리위에 뜰 느낌표 아이콘
    private BehaviorChanged behaviorChangeMark;

    public List<BehaviorData> GetBehaviorDatas()
    {
        return behaviorDatas;
    }
    public void ClearBehaviorData(bool showbehaviorChanged)
    {
        behaviorDatas.Clear();

        OnBehaviorUpdate(behaviorDatas, showbehaviorChanged);
    }

    public void SetBehaviorData(BehaviorData data, bool showBehaviorChanged = true, bool showBehaviorHUD = true)
    {
        //표시되는 정보가 기존과 같으면 ui를 변경 안함
        if (behaviorDatas.Count == 1 && behaviorDatas[0].IsSameAs(data))
        {
            showBehaviorHUD = false;
        }

        behaviorDatas.Clear();

        if (data != null)
            behaviorDatas.Add(data);

        if (showBehaviorHUD)
            OnBehaviorUpdate(behaviorDatas, showBehaviorChanged);
    }

    protected void SetBehaviorData(List<BehaviorData> datas, bool showBehaviorChanged = true, bool showBehaviorHUD = true)
    {
        //표시되는 정보가 기존과 같으면 ui를 변경 안함
        if (behaviorDatas.Count == datas.Count)
        {
            bool isAllSame = true;
            for (int i = 0; i < behaviorDatas.Count; i++)
            {
                if (!behaviorDatas[i].IsSameAs(datas[i]))
                {
                    isAllSame = false;
                    break;
                }
            }

            if (isAllSame)
            {
                showBehaviorHUD = false;
            }
        }

        behaviorDatas.Clear();

        foreach (BehaviorData data in datas)
        {
            if (data != null)
                behaviorDatas.Add(data);
        }

        if (showBehaviorHUD)
            OnBehaviorUpdate(behaviorDatas, showBehaviorChanged);
    }


    /// <summary>
    /// 턴 시작 전 추가 행동
    /// </summary>
    /// <returns></returns>
    public virtual IEnumerator DoStartTurnBehavior()
    {
        //StartCoroutine 으로 감싸면 이 유닛이 코루틴의 주인이 되어,
        //행동 도중 유닛이 파괴될 때 호출자(TurnManager)가 완료 통보를 영영 받지 못한다.
        //따라서 호출자의 코루틴 안에서 직접 이어 돌린다.
        yield return DoBehavior();

        if (this == null || isDying)
            yield break;

        ClearBehaviorData(false);
    }
    public virtual void CalculateStartTurnBehavior()
    {
        ClearBehaviorData(false);
    }
    public virtual IEnumerator DoBehavior()
    {
        behaviorDatas.Add(new BehaviorData(this, BehaviorType.BehaviorPlayed));

        if (behaviorDatas == null)
            yield break;

        
        foreach (var behaviorData in behaviorDatas)
        {
            //덫을 밟는 등으로 앞선 행동 도중 죽었다면 남은 행동을 진행하지 않는다.
            if (isDying)
                yield break;

            //각 행동을 StartCoroutine 으로 감싸면 이 유닛이 코루틴의 주인이 되어,
            //행동 도중 유닛이 파괴될 때 호출자(TurnManager)가 완료 통보를 영영 받지 못한다.
            //따라서 호출자의 코루틴 안에서 직접 이어 돌린다.
            if (behaviorData.Type == BehaviorType.Attack)
                yield return AttackBehavior(behaviorData);
            else if (behaviorData.Type == BehaviorType.Move)
                yield return MoveBehavior(behaviorData);
            else if (behaviorData.Type == BehaviorType.MoveAttack)
                yield return MoveAttackBehavior(behaviorData);
            else if (behaviorData.Type == BehaviorType.AttackWarning)
                yield return AttackWarningBehavior(behaviorData);
            else if (behaviorData.Type == BehaviorType.Heal)
                yield return HealBehavior(behaviorData);
            else if (behaviorData.Type == BehaviorType.Buff)
                yield return BuffBehavior(behaviorData);
            else if (behaviorData.Type == BehaviorType.Debuff)
                yield return DebuffBehavior(behaviorData);
            else if (behaviorData.Type == BehaviorType.Special)
                yield return SpecialBehavior();
        }
    }

    protected virtual IEnumerator AttackBehavior(BehaviorData behaviorData)
    {
        //덫을 밟고 죽은 유닛은 공격하지 못한다.
        if (isDying) yield break;

        Player player = FieldManager.Instance.PlayerInstance;
        if (player == null) yield break;
        LookAtPlayer();

        IndicatorSystem.Instance.PlayBehaviorHighlight(player.GridPosition);
        animator.Play("Attack");
        FieldManager.Instance.PlayerInstance.TakeDamage(damage, this);

        Tile targetTile = FieldManager.Instance.Tiles[player.GridPosition];
        yield return AttackReaction(targetTile);
    }
    protected virtual IEnumerator SpecialBehavior() { yield return null; }

    protected virtual IEnumerator MoveBehavior(BehaviorData behaviorData)
    {
        if (behaviorData.TargetPos == null)
            yield break;

        LookAtPlayer();

        var tiles = FieldManager.Instance.Tiles;

        foreach (Vector2Int? position in behaviorData.TargetPos)
        {
            if (position == null)
                yield break;

            if (!FieldManager.Instance.CanOccupyMultipleTiles(tiles, this, position.Value))
                yield break;



            IndicatorSystem.Instance.PlayBehaviorHighlight(this, position.Value);
            yield return MoveStepCo(tiles[position.Value]);

            //덫을 밟고 죽었다면 남은 이동을 중단한다.
            if (isDying)
                yield break;
        }
    }

    protected virtual IEnumerator MoveStepCo(Tile destTile)
    {
        LookAtPlayer();

        foreach (Vector2Int gridOffset in OccupiedGridOffset)
        {
            Vector2Int targetGrid = GridPosition + gridOffset;
            FieldManager.Instance.Tiles[targetGrid].DisposeUnit();
        }

        //SetUnit 이 덫을 발동시켜 이 칸에서 유닛이 죽을 수 있다.
        //사망 처리는 GridPosition 을 기준으로 타일을 비우므로, 위치를 먼저 확정해야
        //도착 타일이 죽은 유닛에게 점유된 채로 남지 않는다.
        CurrentPosition = FieldManager.Instance.GetTilePosition(destTile);
        GridPosition = destTile.GridPosition;

        foreach (Vector2Int gridOffset in OccupiedGridOffset)
        {
            Vector2Int targetGrid = destTile.GridPosition + gridOffset;
            FieldManager.Instance.Tiles[targetGrid].SetUnit(this);
        }


        Tween tween = transform.DOMove(CurrentPosition, 0.1f).SetEase(Ease.OutQuad);
        yield return tween.WaitForCompletion();

        //덫을 밟고 죽었다면 이후 연출/행동을 진행하지 않는다.
        if (isDying)
            yield break;

        LookAtPlayer();
        yield return null;
    }


    protected virtual IEnumerator MoveAttackBehavior(BehaviorData behaviorData)
    {
        if (behaviorData.TargetPos == null) yield break;

        foreach (Vector2Int? position in behaviorData.TargetPos)
        {
            if (position == null) yield break;

            if (!FieldManager.Instance.CanOccupyMultipleTiles(FieldManager.Instance.Tiles, this, position.Value))
                yield break;

            Tile destTile = FieldManager.Instance.Tiles[position.Value];
            IndicatorSystem.Instance.PlayBehaviorHighlight(this, position.Value);
            yield return MoveStepCo(destTile);

            //공격 지점에 닿기 전에 덫을 밟고 죽었다면 공격하지 못하고 끝난다.
            if (isDying)
                yield break;
        }

        //MoveAttack은 Indicator 관련 코드 편의상 플레이어의 타일이 포함되어 있지 않습니다. 따라서 추가로 표시해줍니다.
        Player player = FieldManager.Instance.PlayerInstance;
        if (player == null) yield break;

        LookAtPlayer();
        yield return AttackBehavior(behaviorData);
    }

    protected virtual IEnumerator AttackWarningBehavior(BehaviorData behaviorData)
    {
        yield return null;
    }

    protected virtual IEnumerator HealBehavior(BehaviorData behaviorData)
    {
        yield return null;
    }
    protected virtual IEnumerator BuffBehavior(BehaviorData behaviorData)
    {
        yield return null;
    }
    protected virtual IEnumerator DebuffBehavior(BehaviorData behaviorData)
    {
        yield return null;
    }

    public bool IsBehaviorPlayed()
    {
        foreach (BehaviorData data in behaviorDatas)
        {
            if (data.Type == BehaviorType.BehaviorPlayed)
                return true;
        }

        return false;

    }

    /// <summary>
    /// 이제 플레이어와 접촉시(이동이 불필요할경우) null  을 반환합니다.
    /// 이동 필요시 Vector2Int을 반환합니다.
    /// </summary>
    protected Vector2Int? CalcSubMovePosition()
    {
        Debug.Log("SubMove");
        var virtualTiles = FieldManager.Instance.VirtualTiles;
        Vector2Int[] directions =
        {
            Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right,
        };

        float minDistance = Vector2Int.Distance(GridPosition, FieldManager.Instance.PlayerInstance.GridPosition);
        Vector2Int resultPos = new(-99, -99);

        foreach (Vector2Int dir in directions)
        {
            Vector2Int newPos = GridPosition + dir;


            if (!FieldManager.Instance.CanOccupyMultipleTiles(virtualTiles, this, newPos))
                continue;

            float distance = Vector2Int.Distance(newPos, FieldManager.Instance.PlayerInstance.GridPosition);
            if (distance < minDistance)
            {
                minDistance = distance;
                resultPos = newPos;
            }
        }


        //최종 목적지를 반환합니다.
        if (resultPos == new Vector2Int(-99, -99))
            return null;
        else
            return resultPos;
    }


    public virtual void TryCalculateNextBehavior()
    {
        //스턴 상태라면 행동을 업데이트 하지 않습니다.
        if (IsStunned())
        {
            return;
        }

        CalculateNextBehavior();
    }

    /// <summary>
    /// AI가 다음 행동을 업데이트 합니다.
    /// </summary>
    public virtual void CalculateNextBehavior() { }


    public virtual void Move(Tile destTile)
    {
        //기존 타일 해제 작업
        foreach (Vector2Int gridOffset in OccupiedGridOffset)
        {
            Vector2Int targetGrid = GridPosition + gridOffset;
            FieldManager.Instance.Tiles[targetGrid].DisposeUnit();
        }

        //SetUnit 이 덫을 발동시켜 이 자리에서 유닛이 죽을 수 있다.
        //사망 처리는 GridPosition 을 기준으로 타일을 비우므로, 위치를 먼저 확정해야
        //도착 타일이 죽은 유닛에게 점유된 채로 남지 않는다.
        CurrentPosition = FieldManager.Instance.GetTilePosition(destTile);
        GridPosition = destTile.GridPosition;

        //이동할 타일 작업
        foreach (Vector2Int gridOffset in OccupiedGridOffset)
        {
            Vector2Int targetGrid = destTile.GridPosition + gridOffset;
            FieldManager.Instance.Tiles[targetGrid].SetUnit(this);
        }

        PositionUpdate();
    }

    /// <summary>
    /// 이동 중 사거리 안으로 들어올 경우 행동을 MoveAttack으로 지정합니다.
    /// MoveAttack이 아닐경우 행동을 Move로 정합니다.
    /// </summary>
    protected void SetBehaviorMoveOrMoveAttack(List<Vector2Int> path)
    {
        if (path == null || path.Count <= 1)
        {
            SetBehaviorData(new BehaviorData(this, BehaviorType.None));
            return;
        }

        Player player = FieldManager.Instance.PlayerInstance;
        if (player == null)
        {
            SetBehaviorData(new BehaviorData(this, BehaviorType.None));
            return;
        }

        List<Vector2Int?> resultPath = new();

        // path[0]은 현재 위치이므로 1부터 시작
        int maxStep = Mathf.Min(MoveRange, path.Count - 1);

        for (int i = 1; i <= maxStep; i++)
        {
            Vector2Int nextPos = path[i];
            resultPath.Add(nextPos);

            //이 칸에 이동한 뒤 사거리 체크
            int currentDistance = GetClosestDistanceToGrid(player.GridPosition, nextPos);

            if (currentDistance <= Range)
            {
                // 이동한 칸 수(i)가 이동력보다 작아 잔여 이동력이 남았을 때만 공격
                if (i < MoveRange)
                {
                    SetBehaviorData(new BehaviorData(this, BehaviorType.MoveAttack, GridPosition, resultPath, damage));
                }
                else
                {
                    // 이동력을 모두 소진했다면 공격 불가, 이동만 수행
                    SetBehaviorData(new BehaviorData(this, BehaviorType.Move, GridPosition, resultPath));
                }
                return;
            }
        }

        // 끝까지 가도 사거리 밖이면 Move
        SetBehaviorData(new BehaviorData(this, BehaviorType.Move, GridPosition, resultPath));
    }


    /// <summary>
    /// 체력바 위에 행동을 업데이트합니다.
    /// </summary>
    public void OnBehaviorUpdate(List<BehaviorData> datas, bool showChangeMark = true)
    {
        ClearBehaviorUIs();

        if (datas == null)
            return;

        if (datas.Count == 0)
            return;

        int visibleCount = 0;

        for (int i = 0; i < datas.Count; i++)
        {
            BehaviorData data = datas[i];

            if (data == null)
                continue;

            if (data.Type == BehaviorType.None || data.Type == BehaviorType.BehaviorPlayed)
                continue;

            visibleCount++;
        }

        if (visibleCount == 0)
            return;

        float behaviorUISpacing = 0.38f;
        float startOffsetX = -(visibleCount - 1) * behaviorUISpacing * 0.5f;
        int visibleIndex = 0;

        for (int i = 0; i < datas.Count; i++)
        {
            BehaviorData data = datas[i];

            if (data == null)
                continue;

            if (data.Type == BehaviorType.None || data.Type == BehaviorType.BehaviorPlayed)
                continue;

            if (!BehaviorManager.Instance.BehaviorList.TryGetValue(data.Type, out GameObject prefab))
                continue;

            BehaviorUI ui = Instantiate(prefab, BattleSceneManager.Instance.HudCanvasRect).GetComponent<BehaviorUI>();

            float xOffset = startOffsetX + behaviorUISpacing * visibleIndex;
            ui.InitBehavior(spriteTransform, height, new Vector2(xOffset, 0f));

            if (data.Value != null)
            {
                TMP_Text tmp = ui.ValueText;
                if (tmp != null)
                {
                    tmp.enabled = true;
                    tmp.text = data.Value.ToString();
                }
            }
            else
            {
                TMP_Text tmp = ui.ValueText;
                if (tmp != null)
                {
                    ui.ValueText.enabled = false;
                }
            }

            ui.Show();
            activeBehaviorUIs.Add(ui);
            visibleIndex++;
        }

        if (showChangeMark)
        {
            float markOffset = startOffsetX - 0.22f;
            behaviorChangeMark.OnBehaviorChanged(spriteTransform, height, markOffset);
        }
    }

    void ClearBehaviorUIs()
    {
        for (int i = 0; i < activeBehaviorUIs.Count; i++)
        {
            BehaviorUI ui = activeBehaviorUIs[i];
            if (ui != null)
                ui.Destroy();
        }

        activeBehaviorUIs.Clear();
    }


    /// <summary>
    /// 현재 유닛이 스턴 상태인지 검사합니다.
    /// </summary>
    public bool IsStunned()
    {
        foreach (BehaviorData data in behaviorDatas)
        {
            if (data.Type == BehaviorType.Stun)
                return true;
        }
        return false;
    }
}

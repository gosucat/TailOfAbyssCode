using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CatsWork;
using DG.Tweening;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.Rendering;

public partial class UnitBase : MonoBehaviour
{
    
    public virtual UnitType UnitType { get;} = UnitType.Enemy;
    public Vector2Int GridPosition { get; set; }

    /// <summary>
    /// 유닛이 차지하고 있는 칸(0, 0을 기준으로 하며, 우상단으로 확장합니다.)
    /// </summary>
    public virtual List<Vector2Int> OccupiedGridOffset { get; } = new List<Vector2Int>() { Vector2Int.zero };


    /// <summary>
    /// 해당 위치에 이 유닛이 차지하는 칸들을 반환합니다.
    /// </summary>
    public virtual List<Vector2Int> GetOccupiedGrids(Vector2Int anchor)
    {
        List<Vector2Int> result = new();

        for (int i = 0; i < OccupiedGridOffset.Count; i++)
        {
            result.Add(anchor + OccupiedGridOffset[i]);
        }

        return result;
    }


    public List<BuffBase> Buffs { get; set; } = new();



    [SerializeField] protected EnemyEntitySO Data;
    public int MaxHp
    {
        get { return maxHp; }
        set
        {
            int newValue = Mathf.Clamp(value, 1, 999);
            maxHp = newValue;
            hudFollow.MaxValue = maxHp;
        }
    }
    int maxHp;
    public int CurrentHp
    {
        get
        {
            return currentHp;
        }
        set
        {
            if (isDying)
                return;

            currentHp = Mathf.Clamp(value, 0, MaxHp);

            if (hudFollow != null)
                hudFollow.CurrentValue = currentHp;

            if (currentHp <= 0)
            {
                isDying = true;
                StartCoroutine(OnUnitDestroy());
            }
        }
    }
    int currentHp;
    bool isDying = false;
    /// <summary>
    /// 사망 처리가 시작되었는지. 덫을 밟아 행동 도중 죽는 경우가 있으므로
    /// 남은 행동을 계속 진행해도 되는지 판단할 때 사용한다.
    /// </summary>
    public bool IsDying => isDying;
    public int Range
    {
        get
        {
            return range;
        }
        set
        {
            if (range != value)
            {
                range = value;
                BehaviorManager.Instance.CalcAllEnemyBehavior();
            }
        }
    }int range;
    //이동력 : 이동력만큼 칸을 움직일 수 있고, 공격할 때에도 이동력을 소모합니다.
    public int MoveRange
    {
        get
        {
            return moveRange;
        }
        set
        {
            if (moveRange != value)
            {
                moveRange = value;
                BehaviorManager.Instance.CalcAllEnemyBehavior();
            }
        }
    } int moveRange;

    [HideInInspector]
    public int SpawnID;

    protected int damage;
    protected int expAmount;
    protected float Height
    {
        get { return height; }
        set 
        {
            height = value;
        }
    } float height;

    protected Animator animator;
    protected HUDFollow hudFollow;

    //유닛의 실제 위치
    protected Vector3 CurrentPosition;
    /// <summary>
    /// 스프라이트 위치
    /// </summary>
    [SerializeField] protected Transform spriteTransform;
    //이펙트를 위해 저장
    [SerializeField] protected SpriteRenderer sr;

    protected SpriteRenderer[] allRenderers;
    private MaterialPropertyBlock mpb;

    [SerializeField] private bool isSpriteFacingRight;

    [SerializeField] protected Transform transformOffset;
    [SerializeField] private SortingGroup sortingGroup;


    protected virtual void Awake()
    {
        animator = GetComponentInChildren<Animator>();

        if (transformOffset != null)
        {
            allRenderers = transformOffset.GetComponentsInChildren<SpriteRenderer>(true);
        }
        else
        {
            allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        hudFollow = Instantiate(BattleSceneManager.Instance.HUDFollowPrefab,
                                BattleSceneManager.Instance.HudCanvasRect).GetComponent<HUDFollow>();

        behaviorChangeMark = Instantiate(BehaviorManager.Instance.BehaviorChangedMark, 
                                         BattleSceneManager.Instance.HudCanvasRect);

        SetBehaviorData(new BehaviorData(this, BehaviorType.BehaviorPlayed));
    }


    public virtual void Init(Tile targetTile, int spawnID)
    {
        originalColor = sr.color;

        //배치보다 위치와 스탯이 먼저 확정되어야 한다.
        //- 위치: 사망 처리는 GridPosition 기준으로 타일을 비운다. 확정 전에 죽으면 엉뚱한 칸이 비워지고
        //        실제 소환 칸은 점유 상태로 남아 인디케이터에서 제외된다.
        //- 스탯: MaxHp 가 0인 상태에서 피해를 받으면 피해량과 무관하게 즉사한다.
        CurrentPosition = FieldManager.Instance.GetTilePosition(targetTile);
        GridPosition = targetTile.GridPosition;

        //실제 좌표도 여기서 맞춰둔다. 등장 연출이 시작되기 전에 참조되는 경우를 대비한다.
        transform.position = CurrentPosition;

        SpawnID = spawnID;

        if (Data != null && hudFollow != null)
        {
            hudFollow.Init(transform, Data.HpBarOffset);

            MaxHp = Data.Hp;
            CurrentHp = MaxHp;
            damage = Data.Damage;
            expAmount = Data.GoldAmount;
            range = Data.Range;
            moveRange = Data.MoveRange;
            Height = Data.Height;

        }

        //배치할 타일 작업
        //이 칸의 덫은 여기서 발동시키지 않는다. 배치와 동시에 발동시키면 등장 연출과 사망 연출이
        //겹쳐서 유닛이 툭 나타났다 사라진다. 등장 연출이 끝난 뒤 SpawnEffectCo 가 발동시킨다.
        foreach (Vector2Int gridOffset in OccupiedGridOffset)
        {
            Vector2Int targetGrid = targetTile.GridPosition + gridOffset;
            FieldManager.Instance.Tiles[targetGrid].SetUnit(this, false);
        }


        LookAtPlayer();
        SpawnEffect(CurrentPosition);
    }

    /// <summary>
    /// 등장 연출이 끝난 뒤, 배치된 칸의 오브젝트(덫 등)를 발동시킨다.
    /// Init 에서 SetUnit(this, false) 로 미뤄둔 발동을 여기서 처리한다.
    /// </summary>
    protected virtual void TriggerPlacedObjects()
    {
        foreach (Vector2Int gridOffset in OccupiedGridOffset)
        {
            //앞선 칸의 덫에 죽었다면 나머지 칸은 발동시키지 않는다.
            if (isDying)
                return;

            Vector2Int targetGrid = GridPosition + gridOffset;
            if (!FieldManager.Instance.Tiles.TryGetValue(targetGrid, out Tile tile))
                continue;

            if (tile.MyObject != null)
                tile.MyObject.OnObjectStepped(this);
        }
    }

    /// <summary>
    /// 유닛 생성 연출
    /// </summary>
    protected virtual void SpawnEffect(Vector3 SpawnPosition)
    {
        StartCoroutine(SpawnEffectCo(SpawnPosition));
    }

    IEnumerator SpawnEffectCo(Vector3 SpawnPosition)
    {
        float side;
        if (FieldManager.Instance.PlayerInstance.GridPosition.x <= GridPosition.x)
            side = 0.2f;
        else
            side = -0.2f;

        transform.position = SpawnPosition;

        Vector3 originalLocalPos = sr.transform.localPosition;
        Vector3 startLocalPos = originalLocalPos + new Vector3(side, 0f, 0f);

        sr.transform.localPosition = startLocalPos;

        Color originalColor = sr.color;
        Color startColor = new Color(0f, 0f, 0f, 0f);
        sr.color = startColor;

        float t = 0f;
        float duration = 0.5f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            float k = Mathf.SmoothStep(0f, 1f, n);

            sr.transform.localPosition = Vector3.Lerp(startLocalPos, originalLocalPos, k);
            sr.color = Color.Lerp(startColor, originalColor, k);

            yield return null;
        }

        sr.transform.localPosition = originalLocalPos;
        sr.color = originalColor;

        //등장이 끝난 뒤에야 이 칸의 덫을 밟는다.
        TriggerPlacedObjects();
    }
    protected virtual void PositionUpdate()
    {
        gameObject.transform.DOMove(CurrentPosition, 0.1f).SetEase(Ease.OutQuad);
    }

    public void OnBuffsUpdate()
    {
        hudFollow.OnBuffsUpdate(Buffs);
    }


    public void Freeze(bool isFreeze)
    {
        if(isFreeze)
        {
            animator.speed = 0;
        }
        else
        {
            animator.speed = 1;
        }
    }

    /// <summary>
    /// 데미지 받기, attacker를 바라봄
    /// </summary>
    public virtual void TakeDamage(int damage, UnitBase attacker = null)
    {
        int totalDamage = damage;
        foreach(BuffBase buff in Buffs)
        {
            //견고함 버프가 있을 경우 데미지를 50%만 받습니다.
            if(buff.BuffType == BuffType.Solidity)
            {
                totalDamage = totalDamage / 2;
                if (totalDamage < 1 && damage > 0)
                    totalDamage = 1;

                break;
            }
        }
        CurrentHp -= totalDamage;
        DamagePopupManager.Instance.ShowDamage(spriteTransform.position, totalDamage);

        //타격자의 위치를 바라봐야함
        if(attacker != null)
        {
            //공격자가 나보다 오른쪽인데 현재 왼쪽을 보는중이면
            if (attacker.GridPosition.x > GridPosition.x && !IsFacingRight)
                SetFacingRight(true);
            else if (attacker.GridPosition.x < GridPosition.x && IsFacingRight)
                SetFacingRight(false);
        }
    }

    protected IEnumerator AttackReaction(Tile targetTile)
    {
        //공격할땐 hud와 대상 위로 오도록 조정합니다. 현재 hud 2, 플레이어 1, 유닛 기본 0
        if (sortingGroup != null)
            sortingGroup.sortingOrder = 3;
        else
            sr.sortingOrder = 3;
        // 현재 오프셋의 "초기 local 위치"를 기준점으로 사용
        Vector3 localStart = transformOffset.localPosition;

        // 월드 방향(대상 - 유닛 실제 위치) 
        Vector3 worldDir = (FieldManager.Instance.PlayerInstance.CurrentPosition - CurrentPosition).normalized;

        // transformOffset의 부모인 최상위 transform을 기준으로 로컬 방향 변환
        // transformOffset 자체의 scale.x 반전은 자식에게만 영향을 주므로 방향이 꼬이지 않습니다.
        Vector3 localDir = transform.InverseTransformVector(worldDir);

        // 2D면 Z는 0으로 고정
        localDir.z = 0f;

        float moveDistance = 0.25f;
        Vector3 localTarget = localStart + localDir * moveDistance;

        float forwardTime = 0.9f;
        float returnTime = 0.2f;
        float t = 0f;
        float fastPhase = 0.1f;
        float slowPhase = forwardTime - fastPhase;

        // 전진(전진은 시간 영향을 안받아야 자연스럽습니다)
        while (t < forwardTime)
        {
            t += Time.unscaledDeltaTime;
            float progress;
            if (t <= fastPhase)
                progress = Mathf.SmoothStep(0f, 0.9f, t / fastPhase);
            else
            {
                float remain = Mathf.Clamp01((t - fastPhase) / slowPhase);
                progress = Mathf.Lerp(0.9f, 1f, Mathf.SmoothStep(0f, 1f, remain));
            }

            transformOffset.localPosition = Vector3.Lerp(localStart, localTarget, progress);
            yield return null;
        }

        // 복귀
        t = 0f;
        while (t < returnTime)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / returnTime);
            float eased = progress * progress * (3f - 2f * progress);
            transformOffset.localPosition = Vector3.Lerp(localTarget, localStart, eased);
            yield return null;
        }

        // 보정
        transformOffset.localPosition = localStart;

        if (sortingGroup != null)
            sortingGroup.sortingOrder = 0;
        else
            sr.sortingOrder = 0;
    }




    //문제생기면 FlipX로 바꿔보자
    protected bool baseFlipX;
    public virtual bool IsFacingRight
    {
        get
        {
            bool isScalePositive = transformOffset.localScale.x > 0;
            // 스프라이트 원본이 오른쪽을 본다면 양수 스케일일 때 오른쪽, 반대면 음수 스케일일 때 오른쪽
            return isSpriteFacingRight ? isScalePositive : !isScalePositive;
        }
    }

    protected virtual void SetFacingRight(bool faceRight)
    {
        Vector3 s = transformOffset.localScale;
        float absX = Mathf.Abs(s.x);

        if (isSpriteFacingRight)
        {
            s.x = faceRight ? absX : -absX;
        }
        else
        {
            s.x = faceRight ? -absX : absX;
        }

        transformOffset.localScale = s;
    }

    /// <summary>
    /// 플레이어를 바라보도록
    /// </summary>
    public virtual void LookAtPlayer()
    {

        foreach(BehaviorData data in behaviorDatas)
        {
            //특수행동중이면 해당 타일을 계속 바라보고 있어야합니다.
            if (data.IsTriggerBehaviorActivate)
                return;
        }

        //플레이어가 몹 왼쪽에 있고 && 몹은 오른쪽을 바라보는 상태면
        if (FieldManager.Instance.PlayerInstance.GridPosition.x < GridPosition.x && IsFacingRight)
        {
            SetFacingRight(false);
        }
        else if(FieldManager.Instance.PlayerInstance.GridPosition.x > GridPosition.x && !IsFacingRight)
        {
            SetFacingRight(true);
        }
    }

    public void ForceLookAtTile(Vector2Int targetTile)
    {
        //해당 타일이 왼쪽에 있고, 현재 오른쪽을 바라보는 상태면
        if (targetTile.x < GridPosition.x && IsFacingRight)
        {
            SetFacingRight(false);
        }
        else if (targetTile.x > GridPosition.x && !IsFacingRight)
        {
            SetFacingRight(true);
        }
        else if (targetTile.x == GridPosition.x)
        {


        }
    }

    /// <summary>
    /// 유닛 스프라이트 위치
    /// </summary>
    public virtual Vector3 GetUnitShapePos()
    {
        return new(spriteTransform.transform.position.x, spriteTransform.transform.position.y, spriteTransform.transform.position.z);
    }


    private Color originalColor;
    public void Highlight()
    {
        if (allRenderers == null || allRenderers.Length == 0) return;

        if (mpb == null) mpb = new MaterialPropertyBlock();

        Color orangeColor = new Color(0.7f, 0.5f, 0.7f, 1.0f);
        float intensity;
        if (Data != null)
            intensity = Data.HighlightIntensity;
        else
            intensity = 1.8f;
        float hdrFactor = Mathf.Pow(2, intensity);
        Color finalHdrColor = orangeColor * hdrFactor;

        finalHdrColor.a = 1.0f;

        foreach (var renderer in allRenderers)
        {
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_HDRColor"))
            {
                renderer.GetPropertyBlock(mpb);
                mpb.SetColor("_HDRColor", finalHdrColor);
                renderer.SetPropertyBlock(mpb);
            }
        }
    }

    public void ResetHighlight()
    {
        if (allRenderers == null || allRenderers.Length == 0) return;

        if (mpb == null) mpb = new MaterialPropertyBlock();

        foreach (var renderer in allRenderers)
        {
            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty("_HDRColor"))
            {
                renderer.GetPropertyBlock(mpb);
                mpb.SetColor("_HDRColor", Color.white);
                renderer.SetPropertyBlock(mpb);
            }
        }
    }

    public Transform GetInteractableTransform()
    {
        return spriteTransform.transform;
    }


    public virtual List<Vector2Int> GetTargetGridPosition()
    {
        List<Vector2Int?> targetPos = new();

        foreach (BehaviorData behaviorData in behaviorDatas)
        {
            if(behaviorData.TargetPos != null)
                targetPos.AddRange(behaviorData.TargetPos);
        }

        if (targetPos.Count == 0) return null;

        List<Vector2Int> targetValue = new();
        foreach (var pos in targetPos)
            targetValue.Add(pos.Value);

        return targetValue;
    }

    /// <summary>
    /// 해당 그리드로부터 가장 가까운 오프셋으로부터의 거리를 반환합니다.
    /// </summary>
    /// <param name="targetGrid"></param>
    /// <returns></returns>
    protected int GetClosestDistanceToGrid(Vector2Int targetGrid, Vector2Int currentGrid)
    {
        int minDistance = int.MaxValue;

        foreach (Vector2Int girdOffset in OccupiedGridOffset)
        {
            Vector2Int myGrid = currentGrid + girdOffset;
            int distance = Utility.GetManhattanDistance(targetGrid, myGrid);

            if (distance < minDistance)
            {
                minDistance = distance;
            }
        }

        return minDistance;
    }

    /// <summary>
    /// 현재 오브젝트가 해당 그리드를 타격하기 위해 서 있을 수 있는 위치들을 반환합니다.
    /// </summary>
    /// <param name="targetGrid"></param>
    /// <returns></returns>
    protected List<Vector2Int> GetAttackableAnchorCandidates(Vector2Int targetGrid)
    {
        List<Vector2Int> result = new();

        for (int dx = -Range; dx <= Range; dx++)
        {
            int remain = Range - Mathf.Abs(dx);

            for (int dy = -remain; dy <= remain; dy++)
            {
                Vector2Int attackableCell = new Vector2Int(targetGrid.x + dx, targetGrid.y + dy);

                for (int j = 0; j < OccupiedGridOffset.Count; j++)
                {
                    Vector2Int candidateAnchor = attackableCell - OccupiedGridOffset[j];

                    if (!FieldManager.Instance.CanOccupyMultipleTiles(FieldManager.Instance.VirtualTiles, this, candidateAnchor))
                        continue;

                    int distance = GetClosestDistanceToGrid(targetGrid, candidateAnchor);
                    if (distance > Range)
                        continue;

                    result.Add(candidateAnchor);
                }
            }
        }
        

        return result;
    }

    /// <summary>
    /// 유닛의 스프라이트 중앙 위치
    /// </summary>
    /// <returns></returns>
    public Vector3 GetSpritePosition()
    {
        if (this == null || sr == null)
        {
            return CurrentPosition; 
        }

        return sr.transform.position;
    }

    public Transform GetSpriteTransform()
    {
        if (this == null || sr == null)
        {
            if (gameObject != null)
                return gameObject.transform;
            else
                return null;
        }

        return sr.transform;
    }

    public virtual EnemyEntitySO GetTargetEnemySO()
    {
        return Data;
    }

    //유닛이 도망갈때
    public virtual IEnumerator OnUnitFlee()
    {
        //실제 타일 간섭 구간
        foreach (Vector2Int gridOffset in OccupiedGridOffset)
        {
            Vector2Int targetGrid = GridPosition + gridOffset;
            Tile targetTile = FieldManager.Instance.Tiles[targetGrid];
            targetTile.DisposeUnit();
        }
        BattleSceneManager.Instance.RemoveEnemy(this);

        //연출 구간

        // 플레이어의 위치를 확인하여 반대 방향(도망칠 방향)을 결정합니다.
        float fleeDirectionX = 1f; // 기본 우측으로 도망
        float fleeDistance = 2.0f; // 도망가는 거리

        if (FieldManager.Instance.PlayerInstance != null)
        {
            // 플레이어가 유닛보다 오른쪽에 있다면, 유닛은 왼쪽으로 도망가야 함
            if (FieldManager.Instance.PlayerInstance.GridPosition.x > GridPosition.x)
            {
                fleeDirectionX = -1f;
                SetFacingRight(false); // 왼쪽을 바라보며 도망
            }
            // 플레이어가 유닛보다 왼쪽에 (또는 같은 위치에) 있다면, 유닛은 오른쪽으로 도망가야 함
            else
            {
                fleeDirectionX = 1f;
                SetFacingRight(true); // 오른쪽을 바라보며 도망
            }
        }

        // 이동할 시작 위치와 목표 위치 설정
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + new Vector3(fleeDirectionX * fleeDistance, 0f, 0f);

        // 시작 시 색상 저장
        Color originalColor = sr.color;

        float t = 0f;
        float duration = 0.5f; // 연출 시간

        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);
            float k = Mathf.SmoothStep(0f, 1f, n);

            // 1. 위치 이동 (플레이어 반대 방향으로 멀어짐)
            transform.position = Vector3.Lerp(startPos, targetPos, k);

            // 2. 점점 어두워지고 투명해짐
            Color targetColor = Color.Lerp(originalColor, Color.black, k);
            targetColor.a = Mathf.Lerp(originalColor.a, 0f, k);
            sr.color = targetColor;

            yield return null;
        }

        sr.color = new Color(0f, 0f, 0f, 0f);

        Dispose();
    }

    protected virtual IEnumerator OnUnitDestroy()
    {
        //실제 타일 간섭 구간
        //플레이어를 제외하면 경험치를 떨어뜨려요. 플레이어는 override했음
        foreach(Vector2Int gridOffset in OccupiedGridOffset)
        {
            Vector2Int targetGrid = GridPosition + gridOffset;
            Tile targetTile = FieldManager.Instance.Tiles[targetGrid];
            targetTile.DisposeUnit();
        }
        BattleSceneManager.Instance.RemoveEnemy(this);
        OrbManager.Instance.CreateOrb(sr.transform.position, expAmount);

        if(TurnManager.Instance.IsMyTurn)
            BehaviorManager.Instance.CalcAllEnemyBehavior();

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
        else
        {
            // 시작 시 색상 저장
            Color originalColor = sr.color;

            float t = 0f;
            float duration = 0.5f; // 연출 시간

            while (t < duration)
            {
                t += Time.deltaTime;
                float n = Mathf.Clamp01(t / duration);
                float k = Mathf.SmoothStep(0f, 1f, n);

                // 점점 어두워지고 투명해짐
                // originalColor에서 검은색(0,0,0)으로 보간하고, 동시에 알파는 0으로 감소
                Color targetColor = Color.Lerp(originalColor, Color.black, k);
                targetColor.a = Mathf.Lerp(originalColor.a, 0f, k);
                sr.color = targetColor;

                yield return null;
            }
            sr.color = new Color(0f, 0f, 0f, 0f);
        }
        Dispose();
    }


    /// <summary>
    /// 오브젝트가 죽었을 때 삭제작업.
    /// 또한 몹의 사망 로직 후 작용하므로, 클리어 검사에도 유용합니다.
    /// </summary>
    protected void Dispose()
    {
        IndicatorSystem.Instance.ReturnUnitAttackReserveIndicators(this);

        Destroy(hudFollow.gameObject);
        if (behaviorChangeMark != null)
            Destroy(behaviorChangeMark.gameObject);

        for (int i = 0; i < activeBehaviorUIs.Count; i++)
        {
            if (activeBehaviorUIs[i] != null)
                Destroy(activeBehaviorUIs[i].gameObject);
        }
        Destroy(gameObject);

        //클리어 검사
        MapManager.Instance.CheckRoomCleared();
        //전투 검사
        bool isBattleState = CardBattleManager.Instance.IsBattleState();
        FieldManager.Instance.PlayerInstance.IsBattleState = isBattleState;
    }


    /// <summary>
    /// 유닛 삭제(매니저 초기화용)
    /// </summary>
    public virtual void DestroyUnit()
    {
        BattleSceneManager.Instance.RemoveEnemy(this);
        IndicatorSystem.Instance.ReturnUnitAttackReserveIndicators(this);

        foreach (Vector2Int gridOffset in OccupiedGridOffset)
        {
            Vector2Int targetGrid = GridPosition + gridOffset;
            Tile targetTile = FieldManager.Instance.Tiles[targetGrid];
            targetTile.DisposeUnit();
        }

        if(hudFollow != null)
            Destroy(hudFollow.gameObject);
        if (behaviorChangeMark != null)
            Destroy(behaviorChangeMark.gameObject);

        for (int i = 0; i < activeBehaviorUIs.Count; i++)
        {
            if (activeBehaviorUIs[i] != null)
                Destroy(activeBehaviorUIs[i].gameObject);
        }


        Destroy(gameObject);
    }



}

public enum UnitType
{
    Enemy,
    Player,
    None,
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using Cinemachine;
using CatsWork;
using JetBrains.Annotations;


public class Player : UnitBase
{
    public Character MyCharacter { get; set; }
    public override UnitType UnitType { get; } = UnitType.Player;

    /// <summary>
    /// 현재 플레이어를 움직일 수 있는 상태인지
    /// </summary>
    public bool IsAvailable { get; set; }
    public int MaxMp
    {
        get { return maxMp; }
        set
        {
            maxMp = value;
            mpHUD.MaxValue = maxMp;
        }
    }
    int maxMp;
    public int CurrentMp
    {
        get { return currentMp; }
        set
        {
            if (currentMp == value) return;
            int old = currentMp;
            currentMp = value;
            mpHUD.CurrentValue = currentMp;

            int delta = currentMp - old; // +면 회복/증가, -면 소모
            RelicManager.Instance.OnChangeMana(delta);
            CardBattleManager.Instance.CardOutlineUpdate();
        }
    }
    int currentMp;

    public bool IsBattleState
    {
        get { return isBattleState; }
        set
        {
            if (isBattleState == value) return;
            if (stateRoutine != null)
            {
                StopCoroutine(stateRoutine);
                Debug.LogError("State Error: 상태가 중간에 변경되었습니다.");
            }
            stateRoutine = StartCoroutine(ChangeBattleState(value));
        }

    }
    bool isBattleState;
    Coroutine stateRoutine;

    [Header("EffectController")]
    [SerializeField] PlayerEffectController playerEffectController;

    ////플레이어는 전용 이펙트도 포함하므로,FlipX가 아닌 scale.x를 반전해줍니다.

    const float HEIGHT = 160f;

    PlayerMP mpHUD;

    [SerializeField] CharacterDialogueSO characterDialogueSO;

    protected override void Awake()
    {
        animator = GetComponent<Animator>();

        if (transformOffset != null)
        {
            allRenderers = transformOffset.GetComponentsInChildren<SpriteRenderer>(true);
        }
        else
        {
            allRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }
    }

    public override void Init(Tile targetTile, int spawnID)
    {
        base.Init(targetTile, spawnID);
        sr.sortingOrder = 1;

        //hpHud 참조
        if (BattleSceneManager.Instance != null)
        {
            if (hudFollow == null)
            {
                hudFollow = Instantiate(BattleSceneManager.Instance.HUDFollowPrefab,
                                    BattleSceneManager.Instance.HudCanvasRect).GetComponent<HUDFollow>();
                hudFollow.Init(transform, Vector3.zero);

                MaxHp = 25;
                CurrentHp = MaxHp;
            }
            if (mpHUD == null)
            {
                mpHUD = BattleSceneManager.Instance.PlayerMpHUD;
                MaxMp = 3;
                CurrentMp = MaxMp;
            }
        }

        //버프 초기화
        Buffs.Clear();
        OnBuffsUpdate();

        //첫 초기화를 위해 원래 상태를 true로 설정한 뒤 false로 변경해줍니다.
        isBattleState = true;
        IsBattleState = false;

        //시작시 오른쪽 보도록


        ////모든 유닛이 플레이어를 보도록

    }



    public bool IsOpeningUI { get; set; } = false;
    public bool IsLoadingRoom { get; set; } = true;
    /// <summary>
    /// 현재 플레이어가 조작 가능한 상태인지 체크합니다.
    /// </summary>
    /// <returns></returns>
    public bool IsPlayerAvailable()
    {
        if (IsOpeningUI || IsLoadingRoom || !IsAvailable)
            return false;

        return true;
    }

    float moveDelayTimer = 0f;
    const float MOVE_DURATION = 0.24f;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            DialogueManager.Instance.ShowDialogue(characterDialogueSO, "tutorial0", 0);

        }

        if (moveDelayTimer > 0)
        {
            moveDelayTimer -= Time.deltaTime;
        }

        MoveInput();


    }

    IEnumerator ChangeBattleState(bool isBattleState)
    {
        //현재 통상 상태일때
        if (!this.isBattleState)
        {
            this.isBattleState = isBattleState;
            //통상 상태 기본 활성화

            //마나 UI 비활성화
            BattleSceneManager.Instance.PlayerMpHUD.SetActive(false);
            //이동 UI 비활성화
            BattleSceneManager.Instance.MoveCount.SetActive(false);
            IsAvailable = true;

            //전투 상태로 전환
            if (isBattleState)
            {
                CardBattleManager.Instance.Init();
                //턴 활성화
                TurnManager.Instance.Init();
            }
        }
        //현재 전투 상태일때
        else
        {
            this.isBattleState = isBattleState;
            //전투 상태 기본 활성화

            //마나 UI 활성화
            BattleSceneManager.Instance.PlayerMpHUD.SetActive(true);
            //이동 UI 활성화
            BattleSceneManager.Instance.MoveCount.SetActive(true);
            //통상 상태로 전환
            if (!isBattleState)
            {
                //이동 UI 비활성화

                //턴 비활성화 및 카드 정리
                TurnManager.Instance.Dispose();

                //로딩중 플레이어 비활성화
                IsAvailable = false;

                yield return StartCoroutine(CardBattleManager.Instance.SetBattleToIdleState());
                IsAvailable = true;
            }
        }
        stateRoutine = null;
    }

    void MoveInput()
    {
        //MoveInput은 플레이어 조작 가능할때만 실행됩니다.
        if (!IsPlayerAvailable())
            return;

        if (IsBattleState)
        {
            //전투시에는 마나 검사를 해야합니다.
            if (CurrentMp < BattleSceneManager.Instance.MoveCount.CurrentValue)
            {
                //흔들림 이펙트
                return;
            }

            if (Input.GetKeyDown(KeyCode.W))
            {
                Vector2Int destPos = new Vector2Int(GridPosition.x, GridPosition.y + 1);
                ValidateMove(destPos);
            }
            else if (Input.GetKeyDown(KeyCode.A))
            {
                if (IsFacingRight)
                    SetFacingRight(false);

                Vector2Int destPos = new Vector2Int(GridPosition.x - 1, GridPosition.y);
                ValidateMove(destPos);
            }
            else if (Input.GetKeyDown(KeyCode.S))
            {
                Vector2Int destPos = new Vector2Int(GridPosition.x, GridPosition.y - 1);
                ValidateMove(destPos);
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                if (!IsFacingRight)
                    SetFacingRight(true);

                Vector2Int destPos = new Vector2Int(GridPosition.x + 1, GridPosition.y);
                ValidateMove(destPos);
            }
        }
        else
        {
            // 마을 맵 (비전투 상태)
            bool moved = false;

            // GetKeyDown: 쿨타임을 무시하고 즉시 이동
            // GetKey: 쿨타임이 0 이하일 때만 발동
            bool pressW = Input.GetKeyDown(KeyCode.W) || (Input.GetKey(KeyCode.W) && moveDelayTimer <= 0);
            bool pressA = Input.GetKeyDown(KeyCode.A) || (Input.GetKey(KeyCode.A) && moveDelayTimer <= 0);
            bool pressS = Input.GetKeyDown(KeyCode.S) || (Input.GetKey(KeyCode.S) && moveDelayTimer <= 0);
            bool pressD = Input.GetKeyDown(KeyCode.D) || (Input.GetKey(KeyCode.D) && moveDelayTimer <= 0);

            if (pressW)
            {
                Vector2Int destPos = new Vector2Int(GridPosition.x, GridPosition.y + 1);
                moved = ValidateMove(destPos);
            }
            else if (pressA)
            {
                if (IsFacingRight)
                    SetFacingRight(false);

                Vector2Int destPos = new Vector2Int(GridPosition.x - 1, GridPosition.y);
                moved = ValidateMove(destPos);
            }
            else if (pressS)
            {
                Vector2Int destPos = new Vector2Int(GridPosition.x, GridPosition.y - 1);
                moved = ValidateMove(destPos);
            }
            else if (pressD)
            {
                if (!IsFacingRight)
                    SetFacingRight(true);

                Vector2Int destPos = new Vector2Int(GridPosition.x + 1, GridPosition.y);
                moved = ValidateMove(destPos);
            }

            // 실제 이동이 발생했을 때만 쿨타임을 다시 채워줍니다.
            if (moved)
            {
                moveDelayTimer = MOVE_DURATION;
            }
        }
    }

    /// <summary>
    /// 움직일 수 있는지 체크하고, 가능하면 움직입니다.
    /// </summary>
    /// <returns></returns>
    bool ValidateMove(Vector2Int destPos)
    {
        //타일이 존재하는지
        if (FieldManager.Instance.Tiles.TryGetValue(destPos, out CatsWork.Tile destTile))
        {
            //타일이 비어있는지
            if (destTile.CurrentState == Tile.TileState.Empty)
            {
                //마나 사용
                UseManaToMove();
                //움직임
                Move(destTile);
                return true;
            }
        }
        return false;
    }
    public new void Move(Tile destTile)
    {
        if (CardBattleManager.Instance != null)
            CardBattleManager.Instance.ResetSelectedCards();

        //기존 타일 해제 작업
        FieldManager.Instance.Tiles[GridPosition].DisposeUnit();

        playerEffectController.SpawnSmoke(transform.position, IsFacingRight);

        //새로 갈 타일 작업
        destTile.SetUnit(this);
        CurrentPosition = FieldManager.Instance.GetTilePosition(destTile);
        GridPosition = destTile.GridPosition;
        PositionUpdate();
        //애니메이션
        animator.Play("Move", -1, 0f);

        //플레이어가 움직일때는 유닛들의 방향을 조정합니다.
        foreach (UnitBase enemy in BattleSceneManager.Instance.EnemyList)
        {
            enemy.LookAtPlayer();
        }



        TurnManager.Instance.MoveCountThisTurn++;
        //또한 상호작용 가능한 유닛 목록을 갱신합니다
        UnitInteractManager.Instance.RefreshInteractSystem();
        //핸드 카드들 중 이동 시너지 관련 동적 카드들을 갱신해줍니다.
        CardBattleManager.Instance.CardInfoUpdateAll();
        //바람처럼 버프가 있을경우 데미지도 줍니다.
        foreach (BuffBase buff in Buffs)
        {
            if (buff.BuffType == BuffType.Windborne)
            {
                WindborneBuff windborne = buff as WindborneBuff;
                windborne.OnPlayerMoved();
                break;
            }
        }

        //보법 버프가 있을경우 스택을 증가시킵니다.
        foreach (BuffBase buff in Buffs)
        {
            if (buff.BuffType == BuffType.Footwork || buff.BuffType == BuffType.FootworkA)
            {
                buff.Stack++;
                break;
            }
        }
    }

    void UseManaToMove()
    {
        //전투상태가 아니라면 마나를 소모하지 않습니다.
        if (!IsBattleState)
            return;

        // Haste 확인 : 있을시 대신 소모하여 이동합니다.
        foreach (BuffBase buff in Buffs)
        {
            if (buff.BuffType == BuffType.Haste && buff.Stack > 0)
            {
                buff.Stack--;
                return;
            }
        }


        //이번 턴에 움직인 만큼 마나를 잡아먹습니다. (0, 1, 2, 3, 3, 3....) 3이 최대
        int moveCount = BattleSceneManager.Instance.MoveCount.CurrentValue;
        CurrentMp -= moveCount;
    }

    Tween tween;
    protected override void PositionUpdate()
    {



        tween.Kill();
        tween = gameObject.transform.DOMove(CurrentPosition, 0.29f).SetEase(Ease.InOutSine).OnComplete(() => spriteTransform.rotation = Quaternion.identity);

        //플레이어가 움직이면 플레이어를 타겟팅할 가능성이 있는 적들이 행동을 재설정해야합니다.
        BehaviorManager.Instance.CalcAllEnemyBehavior();
    }


    //특정 타일쪽을 바라봅니다.
    public void LookTargetTile(Tile targetTile)
    {
        //타겟이 왼쪽에있어!
        if (targetTile.GridPosition.x < GridPosition.x)
        {
            SetFacingRight(false);
        }
        //타겟이 오른쪽에!!
        else if (targetTile.GridPosition.x > GridPosition.x)
        {
            SetFacingRight(true);
        }
    }


    public override Vector3 GetUnitShapePos()
    {
        return new(spriteTransform.transform.position.x, spriteTransform.transform.position.y, spriteTransform.transform.position.z);
    }

    //플레이어는 예상 행동을 반환하지 않습니다. 내가 조종하기때문!
    public override List<Vector2Int> GetTargetGridPosition()
    {
        return null;
    }

    public override void TakeDamage(int damage, UnitBase attacker = null)
    {
        base.TakeDamage(damage, attacker);
        animator.Play("Attacked", -1, 0f);
        playerEffectController.ShowAttackedEffect();

        if(damage > 0)
            FullScreenShakeController.Instance.Shake();
    }

    public void PlayAnim(CardEntitySO so)
    {
        if (so.CardType == CardType.Martial)
        {
            string animName = UnityEngine.Random.value < 0.5f ? "Attack1" : "Attack2";
            animator.Play(animName);
        }
        else
        {
            animator.Play("Stretch");
        }
    }

    public void PlayAnim(CardType type)
    {
        if (type == CardType.Martial)
        {
            string animName = UnityEngine.Random.value < 0.5f ? "Attack1" : "Attack2";
            animator.Play(animName);
        }
        else
        {
            animator.Play("Stretch");
        }
    }

    public void UseChaosBuff()
    {
        BuffBase chaos = null;
        foreach (BuffBase buff in Buffs)
        {
            if (buff.BuffType == BuffType.Chaos)
            {
                chaos = buff;
                break;
            }
        }
        if (chaos == null) return;

        int stack = chaos.Stack;
        if (stack <= 0) return;

        // 총 데미지 = 스택
        int totalDamage = stack;

        // 5스택당 단검 1개, 최대 10개
        int calculatedDaggers = Mathf.CeilToInt(stack / 5f);
        int daggerCount = Mathf.Min(calculatedDaggers, 10);

        // 사거리 내 가장 가까운 적 1명만 타깃
        List<UnitBase> enemyList = FieldManager.Instance.GetInRangeEnemyList(4); // 사거리 4
        enemyList = FieldManager.Instance.SortEnemyByDistance(enemyList);
        if (enemyList == null || enemyList.Count == 0) return;

        UnitBase target = enemyList[0];
        if (target == null) return;

        // 단검별 데미지 분배(마지막 단검에 잔여 몰아주기)
        var damages = new List<int>(daggerCount);
        int remainDamage = totalDamage;
        for (int i = 0; i < daggerCount; i++)
        {
            int damage = Mathf.Min(5, remainDamage);
            remainDamage -= damage;
            damages.Add(damage);
        }

        //공격으로 죽으면 애니메이션 전 미리 리스트에서 해제시켜서, 자동공격을 방지해줍니다. 죽으면 자동으로 MonsterSpawner.RemoveEnemy까지 실행됨
        //나중에 공격을 받았을때, 체력에 변동이 있는 유닛이 존재하면 고칠 필요가 있습니다. 몬스터가 죽지 않았는데 리스트에서 지워질 우려가 있기때문
        if (target.CurrentHp <= totalDamage)
            BattleSceneManager.Instance.EnemyList.Remove(target);

        StartCoroutine(playerEffectController.SpawnDagger(target, damages));
    }



    /// <summary>
    /// 플레이어가 죽을경우, GameOver 작업을 실행합니다.
    /// </summary>
    protected override IEnumerator OnUnitDestroy()
    {
        Debug.Log("GameOver");
        GameOver.Instance.StartGameOver(sr.transform);

        sr.sortingOrder = 11;

        yield return null;
    }

    public new void Dispose()
    {
        StopAllCoroutines();
        playerEffectController.Dispose();
        DestroyUnit();
    }
}

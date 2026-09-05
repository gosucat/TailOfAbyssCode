using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

//기본적으로 전투는
//내턴 - 턴종료 - 적의 턴 - 턴 시작 으로 이루어집니다.
public partial class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }


    public bool IsMyTurn { get; private set; }
    public int TurnCount { get; private set; }
    public int UsedCardCountThisTurn { get; set; } //이번턴에 사용한 카드 갯수
    public int DiscardCountThisTurn { get; set; } //이번턴에 버린 카드 갯수

    public int MoveCountThisTurn  //이번턴에 이동한 횟수
    {
        get
        {
            return moveCountThisTurn;
        }
        set
        {
            moveCountThisTurn = value;
            BattleSceneManager.Instance.MoveCount.CurrentValue = moveCountThisTurn;
        }
    
    }int moveCountThisTurn;
    [Header("EndTurn Button")]
    [SerializeField] Transform endTurnButtonParent;
    public Button EndTurnButton;
    [SerializeField] Image endTurnButtonEdge;

    [SerializeField] TurnStartUI turnStartUI;



    /// <summary>
    /// 첫 턴 초기화
    /// </summary>
    public void Init()
    {
        IsMyTurn = false;
        //InitCo에서 준비가 끝나면 다시 true로 만들어줍니다.
        FieldManager.Instance.PlayerInstance.IsAvailable = false;

        endTurnButtonParent.gameObject.SetActive(true);
        SetEndTurnButton(false);

        TurnCount = 0;

        StartCoroutine(InitCo());
    }

    IEnumerator InitCo()
    {
        yield return Utility.WaitForSeconds(1.0f);
        StartCoroutine(StartTurnWorks());
    }

    public void Dispose()
    {
        StopAllCoroutines();
        endTurnButtonParent.gameObject.SetActive(false);
        //마나 UI 비활성화
        BattleSceneManager.Instance.PlayerMpHUD.SetActive(false);
        BattleSceneManager.Instance.MoveCount.SetActive(false);
        EnemyPriorityUI.Instance.Dispose();
    }


    /// <summary>
    /// 턴이 시작될때, 카드나 유닛들을 관리합니다.
    /// </summary>
    IEnumerator StartTurnWorks()
    {
        TurnCount++;
        //이번 턴 카드 사용 횟수를 초기화합니다.
        UsedCardCountThisTurn = 0;
        DiscardCountThisTurn = 0;
        MoveCountThisTurn = 0;

        //턴 시작 전 적 유닛의 특수 준비행동
        yield return StartCoroutine(EnemyStartTurn());

        turnStartUI.Show();

        //1. 카드를 정해진 수만큼 뽑습니다.
        yield return StartCoroutine(CardBattleManager.Instance.DrawCard(4));
        //2. 턴 시작시의 특수 기능을 수행합니다. 나중에 추가합니다.
        //yield return StartCoroutine(특수기능);

        yield return null;

        //마나를 채웁니다.
        Player player = FieldManager.Instance.PlayerInstance;
        player.CurrentMp = player.MaxMp;


        yield return StartCoroutine(OnTurnStartBuff());

        //턴 시작전 적 유닛들의 행동을 정합니다.
        BehaviorManager.Instance.CalcResetAllEnemyBehavior();

        IsMyTurn = true;
        SetEndTurnButton(true);
        player.IsAvailable = true;

        if (TutorialManager.Instance.IsTutorialAvailable)
            TutorialManager.Instance.StartTutorial();

        foreach (CardInstance card in CardBattleManager.Instance.HandCards)
        {
            card.SetCollider(true);
            card.UpdateOutline(true);
        }
    }



    /// <summary>
    /// 턴이 종료되었을때, 카드나 유닛들을 관리합니다.
    /// </summary>
    IEnumerator EndTurnWorks()
    {
        
        //각종 작업들을 차례대로 수행합니다.

        FieldManager.Instance.PlayerInstance.IsAvailable = false;

        //1.핸드를 버립니다.
        yield return StartCoroutine(CardBattleManager.Instance.ClearHandCards());
        //2.턴 종료시의 특수 기능을 수행합니다.
        yield return StartCoroutine(OnTurnEndBuff());

        if (IsAllEnemyCoward())
        {
            yield return StartCoroutine(SetAllEnemyFlee());
        }
        yield return null;
    }

    IEnumerator EnemyTurn()
    {
        var sortedEnemy = BattleSceneManager.Instance.EnemyList.ToList();

        IndicatorSystem.Instance.ReturnAllUnitAttackReserveIndicators();

        for (int i = 0; i < sortedEnemy.Count; i++)
        {
            var enemy = sortedEnemy[i];
            if (enemy == null)
                continue;

            EnemyPriorityUI.Instance.HighlightCurrentEnemy(enemy);

            yield return StartCoroutine(RunUnitRoutineSafe(enemy, enemy.DoBehavior()));
            yield return Utility.WaitForSeconds(0.3f);
        }

    }

    IEnumerator EnemyStartTurn()
    {
        var sortedEnemy = BattleSceneManager.Instance.EnemyList.ToList();

        BehaviorManager.Instance.CalcAllEnemyTurnStartBehavior();

        for (int i = 0; i < sortedEnemy.Count; i++)
        {
            var enemy = sortedEnemy[i];
            if (enemy == null)
                continue;

            yield return StartCoroutine(RunUnitRoutineSafe(enemy, enemy.DoStartTurnBehavior()));
            yield return Utility.WaitForSeconds(0.1f);
        }

    }
    

    //턴 종료 버튼을 눌렀을때 작업
    IEnumerator OnClickEndTurnCoroutine()
    {
        //턴 종료 작업
        yield return StartCoroutine(EndTurnWorks());

        //적의 턴 작업
        yield return StartCoroutine(EnemyTurn());

        //턴 시작 작업
        yield return StartCoroutine(StartTurnWorks());
    }
    
    public void OnClickEndTurn()
    {
        IsMyTurn = false;
        SetEndTurnButton(false);

        StartCoroutine(OnClickEndTurnCoroutine());
    }

    public void SetEndTurnButton(bool enable)
    {
        bool isEnable = enable;
        if (!IsMyTurn)
            isEnable = false;
        EndTurnButton.interactable= isEnable;
        endTurnButtonEdge.enabled = isEnable;
    }
}

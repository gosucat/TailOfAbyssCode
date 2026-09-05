using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CatsWork;
using Cinemachine;
using TMPro;
using UnityEngine;


public partial class BattleSceneManager : MonoBehaviour
{
    public static BattleSceneManager Instance;

    public PlayerMP PlayerMpHUD;
    public MoveCount MoveCount;

    public GameObject HUDFollowPrefab;
    public RectTransform HudCanvasRect;


    public int ShopRemoveCount { get; set; }

    [Header("Relic LayoutGroup") ]
    public Transform RelicListParent;
    [Header("Effect Parent")]
    public Transform EffectParent;

    [Header("InGame ESC Panel"), SerializeField]
    InGameESCPanel inGameESCPanel;


    #region 게임오버 통계
    //진행한 턴
    //처치한 적
    //KillCountEvent
    //사용한 카드 수
    public int UsedCardAmount { get; set; }
    //소모한 마나
    public int UsedManaAmount { get; set; }

    #endregion


    private void Awake()
    {
        if (Instance == null)
            Instance = this;
    }

    private void Start()
    {
        FieldManager.Instance.InstantiateField();
    }

    /// <summary>
    /// 전투씬 초기화
    /// </summary>
    public void InitBattlefield()
    {
        DisposeObjects();
        ResetSpawnCount();

        UsedCardAmount = 0;
        UsedManaAmount = 0;
        ShopRemoveCount = 0;

        PlayerMpHUD.SetActive(true);
        MoveCount.SetActive(true);
        FieldManager.Instance.PlayerInstance.GetComponent<Collider2D>().enabled = true;

        inGameESCPanel.Hide();

        //나중에 제거 요망(에디터 전용)
        CardManager.Instance.SetCardPoolTestOnly();

        CardManager.Instance.SetStarterPack();
        OrbManager.Instance.Init();
        MapManager.Instance.InitBattleMap();
    }
    /// <summary>
    /// 마을씬 초기화
    /// </summary>
    public void InitVillage()
    {
        DisposeObjects();

        PlayerMpHUD.SetActive(false);
        MoveCount.SetActive(false);
        FieldManager.Instance.PlayerInstance.GetComponent<Collider2D>().enabled = false;

        inGameESCPanel.Hide();
        MapManager.Instance.EnterVillage();
    }


    public void Dispose()
    {
        DisposeObjects();

        PlayerMpHUD.SetActive(false);
        MoveCount.SetActive(false);
        inGameESCPanel.Hide();

        
        CardBattleManager.Instance.Dispose();
        TurnManager.Instance.Dispose();
        OrbManager.Instance.Dispose();
        GameOver.Instance.Dispose();
        MapManager.Instance.Dispose();
    }

    private void Update()
    {
        //이제 씬은 로드씬 / 메인씬 두개로 이루어집니다.
        //그래서 여기서 말하는 씬은, 로드씬을 제외한 인게임에서 플레이어가 이동하는 단절된 공간의 단위입니다.
        if (GameManager.Instance.CurrentScene == SceneType.MainMenu) return;
        //게임오버시에는 열지 않습니다
        if (GameOver.Instance.IsRunning) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            //우선 카드를 들고있거나 특정 행동중이면 다 꺼야합니다.
            if (CardBattleManager.Instance != null && (CardBattleManager.Instance.IsCardHold || CardBattleManager.Instance.IsCardDrag))
            {
                CardBattleManager.Instance.ResetSelectedCards();
                return;
            }

            //게임오버중이면 열지 않습니다.

            //더 없으면 설정창을 엽니다
            if (!inGameESCPanel.GetActiveSelf())
            {
                inGameESCPanel.Show();
            }
            else
            {
                inGameESCPanel.Hide();
            }
        }
    }


    /// <summary>
    /// 우선순위에 맞춰 유닛을 정렬합니다. 우선순위가 같다면 플레이어와 가까운 순서대로 정렬합니다.
    /// 
    /// TODO : 이제 우선순위가 같다면 가까운 순서가 아니라 생성된 순서대로 진행되어야합니다.(추후에 재정렬 되더라도, 같은 우선순위의 몹들의 순서가 변경되어선 안됨)
    /// </summary>


    //    // 플레이어와 유닛 사이의 가장 가까운 맨해튼 거리를 구하는 로컬 함수




    //        //Priority 값을 비교하여 오름차순 정렬합니다.

    //        // 가중치가 다르다면 가중치를 우선하여 정렬합니다.

    //        // 가중치가 같다면 타일 기준 맨해튼 거리순으로 정렬합니다.

    public void SortEnemyByPriority()
    {
        if (EnemyList == null || EnemyList.Count == 0 || FieldManager.Instance.PlayerInstance == null)
            return;

        EnemyList.Sort((a, b) =>
        {
            //Priority 값을 비교하여 오름차순 정렬합니다.
            int priorityResult = a.GetTargetEnemySO().Priority.CompareTo(b.GetTargetEnemySO().Priority);

            // 가중치가 다르다면 가중치를 우선하여 정렬합니다.
            if (priorityResult != 0)
                return priorityResult;

            // 가중치가 같다면 생성된 순서 기준으로 정렬하여 순서 섞임을 방지합니다.
            return a.SpawnID.CompareTo(b.SpawnID);
        });

        //정렬이 끝난 후 상단 턴 순서 UI를 갱신합니다.
        EnemyPriorityUI.Instance.UpdateTurnOrderUI(EnemyList);
    }

}

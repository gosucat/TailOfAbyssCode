using System;
using System.Collections;
using System.Collections.Generic;
using CatsWork;
using UnityEngine;

[Serializable]
public class BehaviorDictionaryPair
{
    public BehaviorType BehaviorType;
    public GameObject BehaviorPrefab;
}

public class BehaviorManager : MonoBehaviour
{
    public static BehaviorManager Instance;

    /// <summary>
    /// 행동 타입에 따른 행동 프리팹 UI
    /// </summary>
    public Dictionary<BehaviorType, GameObject> BehaviorList = new();
    [SerializeField] List<BehaviorDictionaryPair> behaviorPairs = new();

    [Tooltip("행동 변경시 느낌표 마크")]
    public BehaviorChanged BehaviorChangedMark;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        BehaviorList = new Dictionary<BehaviorType, GameObject>();
        foreach (BehaviorDictionaryPair pair in behaviorPairs)
        {
            if (!BehaviorList.ContainsKey(pair.BehaviorType))
                BehaviorList.Add(pair.BehaviorType, pair.BehaviorPrefab);
            else
                Debug.LogError("BehaviorManager has same Behavior!!");
        }

        behaviorPairs.Clear();
    }


    //현재 계산 기준 : 새로운 유닛이 생성되었을 때(턴 시작시 스폰 제외), 유닛이 이동되어졌을때), 플레이어가 이동할때, 유닛 사거리가 변경되었을때

    /// <summary>
    /// 순차적으로 모든 유닛의 행동을 계산합니다.
    /// 실행된 행동은 재계산하지 않습니다.
    /// </summary>
    public void CalcAllEnemyBehavior()
    {
        var virtualTiles = FieldManager.Instance.VirtualTiles;

        virtualTiles.Clear();

        //실행되는 즉시 현재 타일의 상태를 복사해옵니다.
        //즉, 이 함수가 실행되었을때는 이미 모든 유닛이 타일 상에 위치해야합니다.
        foreach (Vector2Int gridPos in FieldManager.Instance.Tiles.Keys)
        {
            Tile tile = FieldManager.Instance.Tiles[gridPos];
            virtualTiles.Add(gridPos, new VirtualTileData(tile.CurrentState, tile.MyUnit, tile.MyObject));
        }

        //계산 전 유닛을 한번 정렬하여 순서를 보장해줍니다.
        BattleSceneManager.Instance.SortEnemyByPriority();
        foreach (UnitBase enemy in BattleSceneManager.Instance.EnemyList)
        {
            if (enemy.IsBehaviorPlayed())
                continue;
            else
                enemy.TryCalculateNextBehavior();
        }
    }

    /// <summary>
    /// 순차적으로 모든 유닛의 행동을 초기화 후 재계산합니다.
    /// 턴 시작 전용
    /// </summary>
    public void CalcResetAllEnemyBehavior()
    {
        var virtualTiles = FieldManager.Instance.VirtualTiles;

        virtualTiles.Clear();

        foreach (Vector2Int gridPos in FieldManager.Instance.Tiles.Keys)
        {
            Tile tile = FieldManager.Instance.Tiles[gridPos];
            virtualTiles.Add(gridPos, new VirtualTileData(tile.CurrentState, tile.MyUnit, tile.MyObject));
        }

        //계산 전 유닛을 한번 정렬하여 순서를 보장해줍니다.
        BattleSceneManager.Instance.SortEnemyByPriority();
        foreach (UnitBase enemy in BattleSceneManager.Instance.EnemyList)
        {
            enemy.ClearBehaviorData(false);
            enemy.TryCalculateNextBehavior();
        }
    }

    /// <summary>
    /// 모든 유닛의 턴 시작 소행동을 계산합니다.
    /// </summary>
    public void CalcAllEnemyTurnStartBehavior()
    {
        var virtualTiles = FieldManager.Instance.VirtualTiles;

        virtualTiles.Clear();

        foreach (Vector2Int gridPos in FieldManager.Instance.Tiles.Keys)
        {
            Tile tile = FieldManager.Instance.Tiles[gridPos];
            virtualTiles.Add(gridPos, new VirtualTileData(tile.CurrentState, tile.MyUnit, tile.MyObject));
        }

        //계산 전 유닛을 한번 정렬하여 순서를 보장해줍니다.
        BattleSceneManager.Instance.SortEnemyByPriority();
        foreach (UnitBase enemy in BattleSceneManager.Instance.EnemyList)
        {
            enemy.CalculateStartTurnBehavior();
        }
    }


    /// <summary>
    /// 모든 유닛의 행동을 해제합니다.
    /// </summary>
    public void DisposeAllEnemyBehavior()
    {
        foreach (UnitBase enemy in BattleSceneManager.Instance.EnemyList)
        {
            enemy.SetBehaviorData(new(enemy, BehaviorType.None), false);
        }
    }
}
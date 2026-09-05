using System.Collections;
using System.Collections.Generic;
using CatsWork;
using UnityEngine;

public partial class UnitBase
{
    /// <summary>
    /// catswork pathfinding 사용
    /// </summary>
    protected List<Vector2Int> PathFindingToPlayer(MovingType movingType, MovingWeight movingWeight = MovingWeight.None)
    {
        Vector2Int edge1 = new();
        Vector2Int edge2 = new();
        if (GameManager.Instance.CurrentScene == SceneType.Battlefield)
        {
            edge1 = new(0, 0);
            edge2 = new(FieldManager.Instance.BattlefieldMapSize.x - 1, FieldManager.Instance.BattlefieldMapSize.y - 1);
        }

        return FieldManager.Instance.AStar.PathFindingMultipleTileObject(FieldManager.Instance.VirtualTiles,
                                                        this,
                                                        edge1,
                                                        edge2,
                                                        GridPosition.x,
                                                        GridPosition.y,
                                                        FieldManager.Instance.PlayerInstance.GridPosition.x,
                                                        FieldManager.Instance.PlayerInstance.GridPosition.y,
                                                        movingType,
                                                        movingWeight
                                                        );
    }

    /// <summary>
    /// /혼잡도 미추가버전
    /// </summary>




    //        List<Vector2Int> path = FieldManager.Instance.AStar.PathFindingMultipleTileObject(
    //            FieldManager.Instance.VirtualTiles,
    //            edge1,
    //            edge2,
    //            GridPosition.x,
    //            GridPosition.y,
    //            goal.x,
    //            goal.y,
    //            movingType,
    //            movingWeight




    //혼잡도 추가버전
    protected List<Vector2Int> PathFindingToAttackablePosition(MovingType movingType, MovingWeight movingWeight = MovingWeight.None)
    {
        Vector2Int edge1 = new();
        Vector2Int edge2 = new();
        if (GameManager.Instance.CurrentScene == SceneType.Battlefield)
        {
            edge1 = new(0, 0);
            edge2 = new(FieldManager.Instance.BattlefieldMapSize.x - 1, FieldManager.Instance.BattlefieldMapSize.y - 1);
        }

        Vector2Int targetPos = FieldManager.Instance.PlayerInstance.GridPosition;
        List<Vector2Int> candidates = GetAttackableAnchorCandidates(targetPos);
        if (candidates == null || candidates.Count == 0)
            return null;

        List<Vector2Int> bestPath = null;
        int bestPathCount = int.MaxValue;
        int bestCongestionScore = int.MaxValue; // 추가된 혼잡도 변수

        for (int i = 0; i < candidates.Count; i++)
        {
            Vector2Int goal = candidates[i];

            // 해당 후보지에 대한 혼잡도 점수 계산
            int congestionScore = GetCongestionScore(goal, targetPos);

            List<Vector2Int> path = FieldManager.Instance.AStar.PathFindingMultipleTileObject(
                FieldManager.Instance.VirtualTiles,
                this,
                edge1,
                edge2,
                GridPosition.x,
                GridPosition.y,
                goal.x,
                goal.y,
                movingType,
                movingWeight
            );

            if (path == null)
                continue;

            //점수 비교
            //혼잡도가 낮으면 무조건 우선 (다른 애들이 못 가는 곳 선점)
            //혼잡도가 같으면, 경로가 더 짧은 곳 우선
            if (congestionScore < bestCongestionScore ||
               (congestionScore == bestCongestionScore && path.Count < bestPathCount))
            {
                bestCongestionScore = congestionScore;
                bestPath = path;
                bestPathCount = path.Count;
            }
        }

        return bestPath;
    }



    /// <summary>
    /// 플레이어로부터 멀어지는 도망 경로를 찾습니다.
    /// </summary>
    protected List<Vector2Int> PathFindingToFleePosition(MovingType movingType, int moveRange, MovingWeight movingWeight = MovingWeight.None)
    {
        Vector2Int playerPos = FieldManager.Instance.PlayerInstance.GridPosition;
        List<Vector2Int> candidates = new List<Vector2Int>();
        int currentDist = GetClosestDistanceToGrid(playerPos, GridPosition);

        // 현재 이동력 범위 내의 타일들을 후보로 수집합니다.
        for (int x = -MoveRange; x <= MoveRange; x++)
        {
            int remain = MoveRange - Mathf.Abs(x);
            for (int y = -remain; y <= remain; y++)
            {
                Vector2Int candidatePos = new Vector2Int(GridPosition.x + x, GridPosition.y + y);

                if (candidatePos == GridPosition) continue;

                // 해당 타일을 점유할 수 있는지 검사
                if (!FieldManager.Instance.CanOccupyMultipleTiles(FieldManager.Instance.VirtualTiles, this, candidatePos))
                    continue;

                // 후보 위치에서 플레이어까지의 거리 계산
                int distToPlayer = GetClosestDistanceToGrid(playerPos, candidatePos);

                // 현재보다 멀어지거나, 최소한 목표로 하는 안전 거리 이상인 곳만 후보로 등록
                if (distToPlayer > currentDist || distToPlayer >= moveRange)
                {
                    candidates.Add(candidatePos);
                }
            }
        }

        if (candidates.Count == 0)
            return null;

        // 플레이어로부터 가장 먼 타일 순으로 내림차순 정렬합니다.
        candidates.Sort((a, b) =>
        {
            int distA = GetClosestDistanceToGrid(playerPos, a);
            int distB = GetClosestDistanceToGrid(playerPos, b);
            return distB.CompareTo(distA);
        });

        Vector2Int edge1 = new Vector2Int();
        Vector2Int edge2 = new Vector2Int();
        if (GameManager.Instance.CurrentScene == SceneType.Battlefield)
        {
            edge1 = new Vector2Int(0, 0);
            edge2 = new Vector2Int(FieldManager.Instance.BattlefieldMapSize.x - 1, FieldManager.Instance.BattlefieldMapSize.y - 1);
        }

        // 가장 먼 곳부 실제 도달 가능한지 검사합니다.
        foreach (Vector2Int goal in candidates)
        {
            List<Vector2Int> path = FieldManager.Instance.AStar.PathFindingMultipleTileObject(
                FieldManager.Instance.VirtualTiles,
                this,
                edge1,
                edge2,
                GridPosition.x,
                GridPosition.y,
                goal.x,
                goal.y,
                movingType,
                movingWeight
            );

            // 경로가 존재하고, 내 이동력 내에서 도달 가능하다면 해당 경로를 채택합니다.
            // A* 결과 path에는 시작점도 포함되므로 Count - 1 로 비교
            if (path != null && (path.Count - 1) <= MoveRange)
            {
                return path;
            }
        }

        return null;
    }


    /// <summary>
    /// 해당 공격 목표 지점이 다른 아군들과 얼마나 겹치는지 혼잡도 점수를 계산합니다.
    /// 점수가 낮을수록 나만 갈 수 있는 좋은 자리입니다.
    /// </summary>
    private int GetCongestionScore(Vector2Int anchorCandidate, Vector2Int targetGrid)
    {
        int score = 0;
        // 내가 이 앵커에 섰을 때 차지하는 실제 타일들 (2x2 등 다중 타일 고려)
        List<Vector2Int> myOccupied = GetOccupiedGrids(anchorCandidate);

        foreach (UnitBase otherEnemy in BattleSceneManager.Instance.EnemyList)
        {
            if (otherEnemy == (UnitBase)this) continue; // 자기 자신은 제외

            UnitBase otherUnit = otherEnemy as UnitBase;
            if (otherUnit == null) continue;

            // 다른 유닛이 목표를 때리기 위해 설 수 있는 앵커 후보들
            List<Vector2Int> otherCandidates = otherUnit.GetAttackableAnchorCandidates(targetGrid);
            if (otherCandidates == null) continue;

            bool canOtherReachAndOverlap = false;

            foreach (Vector2Int otherAnchor in otherCandidates)
            {
                //A* 대신 맨해튼 거리로 다른 유닛이 해당 앵커에 도달할 가능성이 있는지 어림짐작합니다.
                if (Utility.GetManhattanDistance(otherUnit.GridPosition, otherAnchor) <= otherUnit.MoveRange)
                {
                    List<Vector2Int> otherOccupied = otherUnit.GetOccupiedGrids(otherAnchor);

                    // 다른 유닛이 점유할 타일과 내가 점유할 타일이 하나라도 겹치면 혼잡도 증가
                    foreach (Vector2Int myTile in myOccupied)
                    {
                        if (otherOccupied.Contains(myTile))
                        {
                            canOtherReachAndOverlap = true;
                            break;
                        }
                    }
                }

                if (canOtherReachAndOverlap) break;
            }

            if (canOtherReachAndOverlap)
            {
                score++;
            }
        }

        return score;
    }
}

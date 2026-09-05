using System;
using System.Collections;
using System.Collections.Generic;
using CatsWork;
using UnityEngine;

namespace CatsWork
{
    public enum MovingType
    {
        None,
        UDRL,
        OnlyCross,
        UDRLCross,
    }
    /// <summary>
    /// 유닛이 이동시 우선할 방향 가중치 프리셋
    /// </summary>
    public enum MovingWeight
    { 
        None,
        Vertical,
        Horizontal,
    }

    struct PQNode : IComparable<PQNode>
    {
        public int F;
        public int G;
        public int Y;
        public int X;

        public int CompareTo(PQNode other)
        {
            if (F == other.F)
                return 0;
            return F < other.F ? 1 : -1;
        }
    }


    public class AStar
    {
        /// <summary>
        /// edge1 : 맵의 좌하단좌표, edge2 : 맵의 우상단좌표
        /// occupiedGridPos : 유닛이 차지하는 공간 오프셋(0, 0)부터 우상단으로 확장
        /// 길찾기 실패할경우: 플레이어가 둘러싸임 or 길이 없음
        /// </summary>
        public List<Vector2Int> PathFindingMultipleTileObject(Dictionary<Vector2Int, VirtualTileData> tiles, UnitBase unit, Vector2Int edge1, Vector2Int edge2, int xPos, int yPos, int destX, int destY, MovingType movingType, MovingWeight movingWeight = MovingWeight.None)
        {

            //


            bool pathFound = false;

            int[] deltaY = new int[] { -1, 0, 1, 0, -1, 1, 1, -1};
            int[] deltaX = new int[] { 0, -1, 0, 1, -1, -1, 1, 1};
            //기본적으로 대각선 이동은 하지 않습니다.
            int[] cost = new int[] { 10, 10, 10, 10, 14, 14, 14, 14};

            if(movingWeight == MovingWeight.Vertical)
                cost = new int[] { 1, 10, 1, 10, 14, 14, 14, 14 };
            else if(movingWeight == MovingWeight.Horizontal)
                cost = new int[] { 10, 1, 10, 1, 14, 14, 14, 14 };


            //점수 매기기
            //F = 최종 점수(작을수록 좋다)
            //G = 시작점에서 해당 좌표까지 이동하는데 드는 비용
            //H = 휴리스틱(목적지에서 얼마나 가까운지)

            //이미 방문 여부
            bool[,] closed = new bool[edge2.y + 1 - edge1.y, edge2.x + 1 - edge1.x];

            //(x,y) 가는 길을 한번이라도 발견했는지
            // 발견했다면 MaxValue
            //발견했다면 F = G + H 저장
            int[,] open = new int[edge2.y + 1 - edge1.y, edge2.x + 1 - edge1.x];
            for (int x = edge1.x; x < edge2.x + 1; x++)
                for (int y = edge1.y; y < edge2.y + 1; y++)
                    open[y, x] = Int32.MaxValue;

            Vector2Int[,] parent = new Vector2Int[edge2.y + 1 - edge1.y, edge2.x + 1 - edge1.x];

            //오픈리스트에 있는 정보들 중 가장 좋은 후보를 가져오기위해 사용
            PriorityQueue<PQNode> priorityQueue = new PriorityQueue<PQNode>();

            // 시작점 발견
            open[yPos, xPos] = 10 * (Math.Abs(destY - yPos) + Math.Abs(destX - xPos));
            priorityQueue.Push(new PQNode() {
                F  = 10 * (Math.Abs(destY - yPos) + Math.Abs(destX - xPos)), 
                G = 0, 
                Y = yPos, 
                X = xPos 
            });
            parent[yPos, xPos] = new Vector2Int(xPos, yPos);

            while (priorityQueue.Count>0)
            {
                //제일 좋은 후보를 찾는다
                PQNode node = priorityQueue.Pop();

                //동일 좌표를 여러 경로로 찾아, 더 빠른경로로 인해 이미 방문(closed)된경우 스킵
                if (closed[node.Y, node.X])
                    continue;

                //방문시작
                closed[node.Y, node.X] = true;
                //목적지 도착했으면 바로 종료
                if (node.Y == destY && node.X == destX)
                {
                    pathFound = true;
                    break;
                }

                //방향 제어
                int dir1;
                int dir2;
                if(movingType == MovingType.UDRL)
                {
                    dir1 = 0;
                    dir2 = 4;
                }
                else if(movingType == MovingType.OnlyCross)
                {
                    dir1 = 4;
                    dir2 = 8;
                }
                else
                {
                    dir1 = 0;
                    dir2 = 8;
                }

                for(int i = dir1; i<dir2; i++)
                {
                    int nextY = node.Y + deltaY[i];
                    int nextX = node.X + deltaX[i];

                    ////유효 범위를 벗어나면 스킵
                    ////막혀서 갈 수 없으면 스킵
                    //    || tiles[new Vector2Int(nextX, nextY)] == Tile.TileState.Occupied)

                    if (!FieldManager.Instance.CanOccupyMultipleTiles(tiles, unit, new Vector2Int(nextX, nextY)))
                        continue;

                    //이미 방문한 곳이면 스킵
                    if (closed[nextY, nextX])
                        continue;

                    //비용계산
                    int g = node.G + cost[i];
                    int h = 10 * (Math.Abs(destY - nextY) + Math.Abs(destX - nextX));
                    //다른  경로에서 더 빠른 길 이미 찾았으면 스킵
                    if (open[nextY, nextX] < g + h)
                        continue;

                    // 예약 진행
                    open[nextY, nextX] = g + h;
                    priorityQueue.Push(new PQNode() { F = g + h, G = g, Y = nextY, X = nextX });
                    parent[nextY, nextX] = new Vector2Int(node.X, node.Y);

                }
            }

            if (!pathFound)
                return null;

            List<Vector2Int> path = new();
            int tempY = destY;
            int tempX = destX;
            while (parent[tempY, tempX].y != tempY || parent[tempY, tempX].x != tempX)
            {
                path.Add(new Vector2Int(tempX, tempY));
                Vector2Int pos = parent[tempY, tempX];
                tempY = pos.y;
                tempX = pos.x;
            }
            path.Add(new Vector2Int(tempX, tempY));
            path.Reverse();

            return path;
        }



        /// <summary>
        /// edge1 : 좌하단좌표, edge2 : 우상단좌표
        /// 길찾기 실패할경우: 플레이어가 둘러싸임 or 길이 없음
        /// </summary>
        public List<Vector2Int> PathFinding(Dictionary<Vector2Int, Tile.TileState> tiles, Vector2Int edge1, Vector2Int edge2, int xPos, int yPos, int destX, int destY, MovingType movingType, MovingWeight movingWeight = MovingWeight.None)
        {


            bool pathFound = false;

            int[] deltaY = new int[] { -1, 0, 1, 0, -1, 1, 1, -1 };
            int[] deltaX = new int[] { 0, -1, 0, 1, -1, -1, 1, 1 };
            //기본적으로 대각선 이동은 하지 않습니다.
            int[] cost = new int[] { 10, 10, 10, 10, 14, 14, 14, 14 };

            if (movingWeight == MovingWeight.Vertical)
                cost = new int[] { 1, 10, 1, 10, 14, 14, 14, 14 };
            else if (movingWeight == MovingWeight.Horizontal)
                cost = new int[] { 10, 1, 10, 1, 14, 14, 14, 14 };


            //점수 매기기
            //F = 최종 점수(작을수록 좋다)
            //G = 시작점에서 해당 좌표까지 이동하는데 드는 비용
            //H = 휴리스틱(목적지에서 얼마나 가까운지)

            //이미 방문 여부
            bool[,] closed = new bool[edge2.y + 1 - edge1.y, edge2.x + 1 - edge1.x];

            //(x,y) 가는 길을 한번이라도 발견했는지
            // 발견했다면 MaxValue
            //발견했다면 F = G + H 저장
            int[,] open = new int[edge2.y + 1 - edge1.y, edge2.x + 1 - edge1.x];
            for (int x = edge1.x; x < edge2.x + 1; x++)
                for (int y = edge1.y; y < edge2.y + 1; y++)
                    open[y, x] = Int32.MaxValue;

            Vector2Int[,] parent = new Vector2Int[edge2.y + 1 - edge1.y, edge2.x + 1 - edge1.x];

            //오픈리스트에 있는 정보들 중 가장 좋은 후보를 가져오기위해 사용
            PriorityQueue<PQNode> priorityQueue = new PriorityQueue<PQNode>();

            // 시작점 발견
            open[yPos, xPos] = 10 * (Math.Abs(destY - yPos) + Math.Abs(destX - xPos));
            priorityQueue.Push(new PQNode()
            {
                F = 10 * (Math.Abs(destY - yPos) + Math.Abs(destX - xPos)),
                G = 0,
                Y = yPos,
                X = xPos
            });
            parent[yPos, xPos] = new Vector2Int(xPos, yPos);

            while (priorityQueue.Count > 0)
            {
                //제일 좋은 후보를 찾는다
                PQNode node = priorityQueue.Pop();

                //동일 좌표를 여러 경로로 찾아, 더 빠른경로로 인해 이미 방문(closed)된경우 스킵
                if (closed[node.Y, node.X])
                    continue;

                //방문시작
                closed[node.Y, node.X] = true;
                //목적지 도착했으면 바로 종료
                if (node.Y == destY && node.X == destX)
                {
                    pathFound = true;
                    break;
                }

                //방향 제어
                int dir1;
                int dir2;
                if (movingType == MovingType.UDRL)
                {
                    dir1 = 0;
                    dir2 = 4;
                }
                else if (movingType == MovingType.OnlyCross)
                {
                    dir1 = 4;
                    dir2 = 8;
                }
                else
                {
                    dir1 = 0;
                    dir2 = 8;
                }

                for (int i = dir1; i < dir2; i++)
                {
                    int nextY = node.Y + deltaY[i];
                    int nextX = node.X + deltaX[i];

                    //유효 범위를 벗어나면 스킵
                    if (nextX < edge1.x || nextX >= edge2.x + 1 || nextY < edge1.y || nextY >= edge2.y + 1)
                        continue;
                    //막혀서 갈 수 없으면 스킵
                    if (tiles[new Vector2Int(nextX, nextY)] == Tile.TileState.Blocked
                        || tiles[new Vector2Int(nextX, nextY)] == Tile.TileState.Occupied)
                        continue;
                    //이미 방문한 곳이면 스킵
                    if (closed[nextY, nextX])
                        continue;

                    //비용계산
                    int g = node.G + cost[i];
                    int h = 10 * (Math.Abs(destY - nextY) + Math.Abs(destX - nextX));
                    //다른  경로에서 더 빠른 길 이미 찾았으면 스킵
                    if (open[nextY, nextX] < g + h)
                        continue;

                    // 예약 진행
                    open[nextY, nextX] = g + h;
                    priorityQueue.Push(new PQNode() { F = g + h, G = g, Y = nextY, X = nextX });
                    parent[nextY, nextX] = new Vector2Int(node.X, node.Y);

                }
            }

            if (!pathFound)
                return null;

            List<Vector2Int> path = new();
            int tempY = destY;
            int tempX = destX;
            while (parent[tempY, tempX].y != tempY || parent[tempY, tempX].x != tempX)
            {
                path.Add(new Vector2Int(tempX, tempY));
                Vector2Int pos = parent[tempY, tempX];
                tempY = pos.y;
                tempX = pos.x;
            }
            path.Add(new Vector2Int(tempX, tempY));
            path.Reverse();

            return path;
        }







    }
}
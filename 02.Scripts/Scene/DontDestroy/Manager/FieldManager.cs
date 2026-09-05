using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using CatsWork;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections;
using UnityEngine.InputSystem;

public class FieldManager : MonoBehaviour
{
    public static FieldManager Instance;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

    }

    public AStar AStar { get; } = new AStar();
    public Player PlayerInstance { get; private set; }

    [SerializeField] GameObject playerPrefab;


    private Vector3 lastPosition;
    [SerializeField] LayerMask GridLayerMask;

    ///// <summary>
    ///// 그리드 모양
    ///// </summary>
    [SerializeField] Tilemap tilemap;

    [Header("타일 시각화 세팅")]
    [SerializeField] GameObject baseTilePrefab; // 실제 맵에 깔릴 타일 프리팹
    [SerializeField] Transform tileParent; // 하이어라키가 지저분해지지 않게 묶어둘 부모

    // 생성된 실제 타일 프리팹들을 담아둘 리스트 (방 넘어갈 때 삭제용)
    private List<GameObject> visualTiles = new List<GameObject>();

    /// <summary>
    /// 타일의 가로새로 개수
    /// </summary>
    public Vector2Int BattlefieldMapSize;
    /// <summary>
    /// 좌하단 (0, 0) 으로 시작하는 Tile모음
    /// </summary>
    public Dictionary<Vector2Int, CatsWork.Tile> Tiles { get; private set; } = new();
    /// <summary>
    /// 유닛의 행동을 결정하기위해 예약한 타일입니다.
    /// </summary>
    public Dictionary<Vector2Int, VirtualTileData> VirtualTiles { get; private set; } = new();


    private void OnDrawGizmos()
    {
        foreach (var pair in Tiles)
        {
            if (pair.Value.CurrentState == CatsWork.Tile.TileState.Player || pair.Value.MyUnit != null)
            {


                Vector3 tilePos = GetTilePosition(pair.Value);
                tilePos.x += 0.6f;
                tilePos.y += 0.35f;

                Gizmos.DrawSphere(tilePos, 0.18f);
            }

        }
    }

    /// <summary>
    /// 플레이어와 필드를 임시로 1회 생성합니다.
    /// </summary>
    public void InstantiateField()
    {
        //플레이어 임시 생성
        BattlefieldMapSize = new(1, 1);
        InitBattleSceneTiles(BattlefieldMapSize.x, BattlefieldMapSize.y);
        InitPlayer(0, 0);
    }



    /// <summary>
    /// 동적으로 방 생성
    /// </summary>
    public void GenerateDynamicRoom(int width, int height, bool disableVisualTiles = false)
    {
        // 1. 기존에 생성된 논리적 타일 데이터와 시각적 타일(오브젝트) 지우기
        foreach (GameObject vTile in visualTiles)
        {
            if (vTile != null) Destroy(vTile);
        }
        visualTiles.Clear();
        Tiles.Clear();

        // 2. 새로운 크기로 배틀 씬 타일 맵 생성 및 배치
        BattlefieldMapSize = new Vector2Int(width, height);
        InitBattleSceneTiles(width, height, disableVisualTiles);
    }
    // ----------------------------------------------

    #region tile Management

    /// <summary>
    /// 필드의 타일을 초기화합니다.
    /// </summary>
    void InitBattleSceneTiles(int xSize, int ySize, bool disableVisualTiles = false)
    {
        Tiles.Clear();

        for (int i = 0; i < xSize; i++)
        {
            for (int j = 0; j < ySize; j++)
            {
                Vector2Int gridPos = new Vector2Int(i, j);
                CatsWork.Tile newTile = new();
                newTile.Initialize(gridPos);
                Tiles[gridPos] = newTile;

                //VisualTile 배치
                if (baseTilePrefab != null && disableVisualTiles == false)
                {
                    Vector3 worldPos = GetTilePositionFlat(gridPos);
                    GameObject vTile = Instantiate(baseTilePrefab, worldPos, Quaternion.identity, tileParent);
                    vTile.name = $"Tile_{i}_{j}";
                    visualTiles.Add(vTile);
                }
            }
        }

    }

    /// <summary>
    /// 마우스의 위치로부터 타일을 가져옵니다. 카드의 효과를 발동할 수 있는 범위 내여야 가져옵니다.
    /// </summary>
    /// <returns></returns>
    public CatsWork.Tile GetTileFromMousePosInRange(CardInstance card)
    {
        if (card == null) return null;

        Vector3Int cellPos = tilemap.WorldToCell(Utility.FieldMousePos);
        Vector2Int tilePos = new Vector2Int(cellPos.x, cellPos.y);

        if (Tiles.TryGetValue(tilePos, out CatsWork.Tile tile))
        {
            //타일을 가져오는데 성공했음. 플레이어 사거리안인지 검사

            CardEntitySO so = card.OriginalData.CardEntitySO;

            //해당 카드의 효과를 발동할 수 있는 타일들을 전부 가져옵니다.
            List<CatsWork.Tile> selectableTiles = GetCardSelectableTiles(card);

            if (selectableTiles.Contains(tile))
                return tile;

        }
        return null;
    }

    /// <summary>
    /// 특정 위치에 있는 타일을 가져옵니다.
    /// </summary>
    public CatsWork.Tile GetTileFromPosition(Vector2 position)
    {
        Vector3Int cellPos = tilemap.WorldToCell(position);
        Vector2Int tilePos = new Vector2Int(cellPos.x, cellPos.y);

        if (Tiles.TryGetValue(tilePos, out CatsWork.Tile tile))
        {
            //타일을 가져오는데 성공했음.
            return tile;
        }
        return null;
    }

    public CatsWork.Tile GetTileFromPosition(Vector2Int position)
    {
        if (Tiles.TryGetValue(position, out CatsWork.Tile tile))
        {
            //타일을 가져오는데 성공했음.
            return tile;
        }
        return null;
    }


    /// <summary>
    /// 해당 타일의 실제 위치를 가져옵니다. z축이 y와 동일합니다.
    /// </summary>
    public Vector3 GetTilePosition(CatsWork.Tile tile)
    {
        Vector3Int gridPos = new Vector3Int(tile.GridPosition.x, tile.GridPosition.y, 0);
        var result = tilemap.CellToWorld(gridPos);
        //sortOrder 조절 대신 사용합니다.
        result.z = tile.GridPosition.y;
        return result;
    }

    /// <summary>
    /// 해당 타일의 실제 위치를 가져옵니다.
    /// </summary>
    public Vector3 GetTilePositionFlat(Vector2Int position)
    {
        Vector3Int cellPos = new Vector3Int(position.x, position.y, 0);
        Vector3 result = tilemap.CellToWorld(cellPos);
        return result;
    }

    /// <summary>
    /// 해당 타일의 실제 중앙 위치를 가져옵니다.
    /// </summary>
    public Vector3 GetTilePositionCenter(CatsWork.Tile tile)
    {
        Vector3Int gridPos = new Vector3Int(tile.GridPosition.x, tile.GridPosition.y, 0);
        Vector3 centerOffset = new(0.5f, 0.45f, 0f);
        var result = tilemap.CellToWorld(gridPos) + centerOffset;

        return result;
    }


    /// <summary>
    /// 플레이어를 기준으로 타일을들 가져옵니다. 단, 플레이어의 타일은 가져오지 않습니다.
    /// </summary>
    List<CatsWork.Tile> GetTilesFromPlayer(List<Vector2Int> area)
    {
        List<CatsWork.Tile> result = new();

        Vector2Int playerPos = PlayerInstance.GridPosition;
        foreach (Vector2Int pos in area)
        {
            if (pos.x == 0 && pos.y == 0) continue;

            Vector2Int tilePos = new Vector2Int(playerPos.x + pos.x, playerPos.y + pos.y);
            if (Tiles.TryGetValue(tilePos, out CatsWork.Tile tile))
            {
                result.Add(tile);
            }
        }

        return result;
    }

    /// <summary>
    /// 타일을 기준으로 타일들을 가져옵니다.
    /// </summary>
    List<CatsWork.Tile> GetTilesFromTile(List<Vector2Int> area, CatsWork.Tile targetTile)
    {
        List<CatsWork.Tile> result = new();

        Vector2Int targetPos = targetTile.GridPosition;
        foreach (Vector2Int pos in area)
        {
            Vector2Int tilePos = new Vector2Int(targetPos.x + pos.x, targetPos.y + pos.y);
            if (Tiles.TryGetValue(tilePos, out CatsWork.Tile tile))
            {
                result.Add(tile);
            }
        }

        return result;
    }

    /// <summary>
    /// 플레이어 기준으로 카드의 효과로 선택 가능한 타일을 리턴합니다.
    /// </summary>
    public List<CatsWork.Tile> GetCardSelectableTiles(CardInstance card)
    {
        if (PlayerInstance == null) return null;

        CardEntitySO so = card.OriginalData.CardEntitySO;
        //벽에 막히는 카드는 따로 처리해줍니다.
        if (so.MovingType != MovingType.None)
            return GetBlockedCardSelectableTiles(card);

        // 카드에 전용 SelectableArea가 있으면 그것을 사용
        if (so.SelectableAreaFromPlayer != null && so.SelectableAreaFromPlayer.Count > 0)
        {
            List<CatsWork.Tile> targetTiles = GetTilesFromPlayer(so.SelectableAreaFromPlayer);

            //만약 해당 카드가 적을 대상으로 사용할 수 없는 카드라면(이동 등) 적이 있는 위치를 목표 타일에서 빼줍니다.
            if (so.IsCannotTargetEnemy)
                targetTiles = GetTilesWithNoOccupied(targetTiles);

            return targetTiles;
        }


        //선택 가능한 타일은 반드시 사거리가 존재해야합니다.
        //사거리가 -1이면 선택 불가능한 상태이거나, 선택 범위가 존재하지 않는 카드라는 의미입니다.
        if (so.Range == -1)
        {
            return null;
        }

        // 전용 SelectableArea가 없으면 카드의 사거리를 기준으로 가져옵니다.
        List<CatsWork.Tile> result = new List<CatsWork.Tile>();
        Vector2Int playerPos = PlayerInstance.GridPosition;

        //맨해튼으로 변경
        for (int x = -so.Range; x <= so.Range; x++)
        {
            int maxY = so.Range - Mathf.Abs(x);

            for (int y = -maxY; y <= maxY; y++)
            {
                //현재 플레이어의 타일은 제외
                if (x == 0 && y == 0) continue;

                Vector2Int tilePos = new Vector2Int(playerPos.x + x, playerPos.y + y);
                CatsWork.Tile tile;
                if (Tiles.TryGetValue(tilePos, out tile))
                {
                    result.Add(tile);
                }
            }
        }

        //만약 해당 카드가 적을 대상으로 사용할 수 없는 카드라면(이동 등) 적이 있는 위치를 목표 타일에서 빼줍니다.
        if (so.IsCannotTargetEnemy)
            result = GetTilesWithNoOccupied(result);

        return result;
    }

    /// <summary>
    /// 플레이어 기준으로 카드의 효과로 선택 가능한 타일을 리턴합니다.
    /// 이동형 카드 전용(장애물에 막히는 카드)
    /// </summary>
    private List<CatsWork.Tile> GetBlockedCardSelectableTiles(CardInstance card)
    {
        CardEntitySO so = card.OriginalData.CardEntitySO;
        if (so.MovingType == MovingType.None || PlayerInstance == null) return null;

        List<CatsWork.Tile> result = new List<CatsWork.Tile>();

        Vector2Int origin = PlayerInstance.GridPosition;

        Vector2Int[] dirArr = null;
        if (so.MovingType == MovingType.UDRL)
        {
            dirArr = new Vector2Int[4];
            dirArr[0] = Vector2Int.up;
            dirArr[1] = Vector2Int.down;
            dirArr[2] = Vector2Int.left;
            dirArr[3] = Vector2Int.right;
        }
        else if (so.MovingType == MovingType.OnlyCross)
        {
            dirArr = new Vector2Int[4];
            dirArr[0] = new Vector2Int(1, 1);
            dirArr[1] = new Vector2Int(1, -1);
            dirArr[2] = new Vector2Int(-1, 1);
            dirArr[3] = new Vector2Int(-1, -1);
        }
        else if (so.MovingType == MovingType.UDRLCross)
        {
            dirArr = new Vector2Int[8];
            dirArr[0] = Vector2Int.up;
            dirArr[1] = Vector2Int.down;
            dirArr[2] = Vector2Int.left;
            dirArr[3] = Vector2Int.right;
            dirArr[4] = new Vector2Int(1, 1);
            dirArr[5] = new Vector2Int(1, -1);
            dirArr[6] = new Vector2Int(-1, 1);
            dirArr[7] = new Vector2Int(-1, -1);
        }

        for (int i = 0; i < dirArr.Length; i++)
        {
            Vector2Int dir = dirArr[i];

            for (int step = 1; step <= card.Range; step++)
            {
                Vector2Int pos = new Vector2Int(origin.x + dir.x * step, origin.y + dir.y * step);

                CatsWork.Tile tile;
                //타일의 끝이면 종료
                if (!Tiles.TryGetValue(pos, out tile))
                    break;

                // 막히는 타일(유닛/오브젝트/벽)은 그 타일에서 종료
                if (tile.CurrentState == CatsWork.Tile.TileState.Occupied || tile.CurrentState == CatsWork.Tile.TileState.Blocked)
                {
                    //적을 선택 가능한 카드면 적을 포함하고 종료
                    if (!so.IsCannotTargetEnemy)
                        result.Add(tile);

                    break;
                }

                //여기까지 왔다면 막힌게 아니므로 포함
                result.Add(tile);
            }
        }

        return result;
    }


    /// <summary>
    /// A에서 B로 가는 정규화된 방향을 반환합니다.
    /// </summary>
    public Vector2Int GetDir(Vector2Int from, Vector2Int to)
    {
        int dx = to.x - from.x;
        int dy = to.y - from.y;

        if (dx == 0 && dy == 0) return Vector2Int.zero;

        int nx = 0;
        int ny = 0;

        if (dx > 0) nx = 1;
        if (dx < 0) nx = -1;
        if (dy > 0) ny = 1;
        if (dy < 0) ny = -1;

        return new Vector2Int(nx, ny);
    }


    /// <summary>
    /// 무언가 올라가 있는 타일을 목표 타일에서 빼줍니다.
    /// </summary>
    /// <returns></returns>
    private List<CatsWork.Tile> GetTilesWithNoOccupied(List<CatsWork.Tile> tiles)
    {
        List<CatsWork.Tile> resultTiles = tiles.ToList();

        foreach (CatsWork.Tile tile in tiles)
        {
            if (tile.CurrentState == CatsWork.Tile.TileState.Occupied || tile.CurrentState == CatsWork.Tile.TileState.Blocked)
            {
                //해당 타일을 리스트에서 제거합니다.
                resultTiles.Remove(tile);
            }
        }

        return resultTiles;
    }

    /// <summary>
    /// 카드의 효과 범위의 타일들을 리턴합니다.
    /// IndicatorSystem 전용
    /// </summary>
    public List<CatsWork.Tile> GetCardEffectAreaTiles(CardInstance card, CatsWork.Tile targetTile = null)
    {
        List<Vector2Int> effectArea = card.OriginalData.CardEntitySO.EffectArea;

        if (card.OriginalData.CardEntitySO.EffectArea == null && !card.OriginalData.CardEntitySO.IsSelectable)
            return null;

        //대상을 지정하는 카드의 경우
        if (targetTile != null)
            return GetTilesFromTile(card.OriginalData.CardEntitySO.EffectArea, targetTile);
        else //지정하지 않는데 범위가 존재하면, 플레이어로부터
            return GetTilesFromPlayer(card.OriginalData.CardEntitySO.EffectArea);
    }

    /// <summary>
    /// 카드의 효과 범위의 타일들을 리턴합니다.
    /// </summary>
    public List<CatsWork.Tile> GetCardEffectTiles(CardInstance card, CatsWork.Tile targetTile = null)
    {
        List<Vector2Int> effectArea = card.OriginalData.CardEntitySO.EffectArea;

        //대상을 지정하는 카드의 경우
        if (targetTile != null)
            return GetTilesFromTile(card.OriginalData.CardEntitySO.EffectArea, targetTile);
        else //지정하지 않는데 범위가 존재하면, 플레이어로부터
            return GetTilesFromPlayer(card.OriginalData.CardEntitySO.EffectArea);
    }

    /// <summary>
    /// 해당 타일로 이동할 유닛의 범위중 하나라도 맵을 벗어났거나 막혀서 갈 수 없으면 false
    /// </summary>
    public bool CanOccupyMultipleTiles(Dictionary<Vector2Int, VirtualTileData> virtualTiles, UnitBase unit, Vector2Int destPos)
    {
        List<Vector2Int> occupiedGridOffset = unit.OccupiedGridOffset;
        for (int j = 0; j < occupiedGridOffset.Count; j++)
        {
            Vector2Int targetPos = destPos + occupiedGridOffset[j];

            if (!virtualTiles.TryGetValue(targetPos, out VirtualTileData virtualTile))
                return false;


            //현재 가고자 하는 위치가 내 유닛이 점령한 곳이면(즉, 뚠뚠이 유닛이 본인 몸을 포함한 타일로 이동하려고 하는 경우)
            if (virtualTile.MyUnit == unit)
                continue;

            if (virtualTile.CurrentState == CatsWork.Tile.TileState.Occupied || virtualTile.CurrentState == CatsWork.Tile.TileState.Blocked)
                return false;
        }

        return true;
    }

    /// <summary>
    /// 해당 타일로 이동할 유닛의 범위중 하나라도 맵을 벗어났거나 막혀서 갈 수 없으면 false
    /// </summary>
    public bool CanOccupyMultipleTiles(Dictionary<Vector2Int, CatsWork.Tile> tiles, UnitBase unit, Vector2Int destPos)
    {
        List<Vector2Int> occupiedGridOffset = unit.OccupiedGridOffset;
        for (int j = 0; j < occupiedGridOffset.Count; j++)
        {
            Vector2Int targetPos = destPos + occupiedGridOffset[j];

            if (!tiles.TryGetValue(targetPos, out CatsWork.Tile tile))
                return false;


            //현재 가고자 하는 위치가 내 유닛이 점령한 곳이면(즉, 뚠뚠이 유닛이 본인 몸을 포함한 타일로 이동하려고 하는 경우)
            if (tile.MyUnit == unit)
                continue;

            if (tile.CurrentState == CatsWork.Tile.TileState.Occupied || tile.CurrentState == CatsWork.Tile.TileState.Blocked)
                return false;
        }

        return true;
    }


    #endregion

    #region unit Management

    /// <summary>
    /// x,y 좌표에 플레이어를 초기화합니다.
    /// </summary>
    /// <param name="xPos"></param>
    /// <param name="yPos"></param>
    public void InitPlayer(int xPos, int yPos)
    {
        if (PlayerInstance == null)
        {
            switch (GameManager.Instance.MyCharacter)
            {
                case Character.Bini:
                    PlayerInstance = Instantiate(playerPrefab).GetComponent<Player>();
                    PlayerInstance.MyCharacter = Character.Bini;
                    break;
                    //case2

            }
        }
        else
        {
            ////기존에 있던 자리 초기화
        }

        CatsWork.Tile startTile = Tiles[new Vector2Int(xPos, yPos)];
        PlayerInstance.Init(startTile, -1);
    }

    //몹이 플레이어를 향해 오는게 불가능한지 검사(경로가 다 막혀있음)
    public bool IsPlayerSurrounded(MovingType movingType)
    {
        switch (movingType)
        {
            //길이 한곳이라도 존재하면 false 반환
            case MovingType.UDRL:
                return !ValidateUDRL();
            case MovingType.OnlyCross:
                return !ValidateUDRL() && !ValidateCross(); // 대각선 이동 유닛은 가중치가 대각선에 높은것 뿐입니다.
            case MovingType.UDRLCross:
                return !ValidateUDRL() && !ValidateCross();
            default:
                Debug.Log("MovingType이 잘못됨");
                return false;
        }

    }


    /// <summary>
    /// 길이 있으면 true를 반환합니다.
    /// </summary>
    bool ValidateUDRL()
    {
        Vector2Int targetPos = new Vector2Int(PlayerInstance.GridPosition.x, PlayerInstance.GridPosition.y + 1);
        if (Tiles.TryGetValue(targetPos, out CatsWork.Tile upTile))
        {
            if (upTile.CurrentState == CatsWork.Tile.TileState.Empty)
                return true;
        }
        targetPos = new Vector2Int(PlayerInstance.GridPosition.x - 1, PlayerInstance.GridPosition.y);
        if (Tiles.TryGetValue(targetPos, out CatsWork.Tile leftTile))
        {
            if (leftTile.CurrentState == CatsWork.Tile.TileState.Empty)
                return true;
        }
        targetPos = new Vector2Int(PlayerInstance.GridPosition.x + 1, PlayerInstance.GridPosition.y);
        if (Tiles.TryGetValue(targetPos, out CatsWork.Tile rightTile))
        {
            if (rightTile.CurrentState == CatsWork.Tile.TileState.Empty)
                return true;
        }
        targetPos = new Vector2Int(PlayerInstance.GridPosition.x, PlayerInstance.GridPosition.y - 1);
        if (Tiles.TryGetValue(targetPos, out CatsWork.Tile downTile))
        {
            if (downTile.CurrentState == CatsWork.Tile.TileState.Empty)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 길이 있으면 true를 반환합니다.
    /// </summary>
    bool ValidateCross()
    {
        Vector2Int targetPos = new Vector2Int(PlayerInstance.GridPosition.x - 1, PlayerInstance.GridPosition.y + 1);
        if (Tiles.TryGetValue(targetPos, out CatsWork.Tile upLeftTile))
        {
            if (upLeftTile.CurrentState == CatsWork.Tile.TileState.Empty)
                return true;
        }
        targetPos = new Vector2Int(PlayerInstance.GridPosition.x + 1, PlayerInstance.GridPosition.y + 1);
        if (Tiles.TryGetValue(targetPos, out CatsWork.Tile upRightTile))
        {
            if (upRightTile.CurrentState == CatsWork.Tile.TileState.Empty)
                return true;
        }
        targetPos = new Vector2Int(PlayerInstance.GridPosition.x - 1, PlayerInstance.GridPosition.y - 1);
        if (Tiles.TryGetValue(targetPos, out CatsWork.Tile downLeftTile))
        {
            if (downLeftTile.CurrentState == CatsWork.Tile.TileState.Empty)
                return true;
        }
        targetPos = new Vector2Int(PlayerInstance.GridPosition.x + 1, PlayerInstance.GridPosition.y - 1);
        if (Tiles.TryGetValue(targetPos, out CatsWork.Tile downRightTile))
        {
            if (downRightTile.CurrentState == CatsWork.Tile.TileState.Empty)
                return true;
        }
        return false;
    }

    /// <summary>
    /// 플레이어 사거리 내의 몬스터를 가져옵니다.
    /// </summary>
    public List<UnitBase> GetInRangeEnemyList(int range)
    {
        List<UnitBase> InRangeUnits = new();

        foreach (UnitBase enemy in BattleSceneManager.Instance.EnemyList)
        {
            //유닛의 크기가 1 이상일 수 있습니다.
            for (int i = 0; i < enemy.OccupiedGridOffset.Count; i++)
            {
                int distance = Utility.GetManhattanDistance(PlayerInstance.GridPosition, enemy.GridPosition + enemy.OccupiedGridOffset[i]);
                if (distance <= range)
                {
                    InRangeUnits.Add(enemy);
                    break;
                }
            }
        }

        return InRangeUnits;
    }


    /// <summary>
    /// 받은 적들을 가까운순으로 정렬해서 리턴합니다.
    /// </summary>
    /// <param name="enemyList"></param>
    /// <returns></returns>
    public List<UnitBase> SortEnemyByDistance(List<UnitBase> enemyList)
    {
        if (enemyList == null || enemyList.Count == 0)
            return null;

        Vector2Int playerPos = PlayerInstance.GridPosition;
        Vector3 playerSpritePos = PlayerInstance.GetInteractableTransform().position;

        int GetClosestDistance(UnitBase unit)
        {
            int minDistance = int.MaxValue;

            for (int i = 0; i < unit.OccupiedGridOffset.Count; i++)
            {
                Vector2Int occupiedPos = unit.GridPosition + unit.OccupiedGridOffset[i];
                int distance = Utility.GetManhattanDistance(playerPos, occupiedPos);

                if (distance < minDistance)
                    minDistance = distance;
            }

            return minDistance;
        }

        List<UnitBase> tempList = enemyList.ToList();
        tempList.Sort((a, b) =>
        {
            // 1️ 맨해튼 거리
            int distA = GetClosestDistance(a);
            int distB = GetClosestDistance(b);
            int result = distA.CompareTo(distB);
            if (result != 0)
                return result;

            //// 2️ HP 낮은 순

            // 3️ 스프라이트 거리 (화면상 진짜 가까운 순)
            float worldDistA = Vector3.SqrMagnitude(a.GetInteractableTransform().position - playerSpritePos);
            float worldDistB = Vector3.SqrMagnitude(b.GetInteractableTransform().position - playerSpritePos);
            return worldDistA.CompareTo(worldDistB);
        });

        return tempList;
    }

    /// <summary>
    /// 오브젝트를 위아래로 밀치기 시도
    /// 덜 밀쳐진 오브젝트가 있으면 false
    /// </summary>
    public bool TryPushObjectVertical(CatsWork.Tile targetTile)
    {
        if (targetTile == null) return false;
        if (targetTile.CurrentState == CatsWork.Tile.TileState.Blocked) return false;

        Vector2Int origin = targetTile.GridPosition;
        //밀쳐질 타일들
        List<CatsWork.Tile> pushedTilesForUnit = new List<CatsWork.Tile>();
        List<CatsWork.Tile> pushedTilesForObject = new List<CatsWork.Tile>();

        Vector2Int upPos = new Vector2Int(origin.x, origin.y + 1);
        if (Tiles.TryGetValue(upPos, out CatsWork.Tile upTile))
        {
            if (upTile.CurrentState == CatsWork.Tile.TileState.Empty)
            {
                if (upTile.MyObject == null)
                    pushedTilesForObject.Add(upTile);

                pushedTilesForUnit.Add(upTile);
            }
        }

        Vector2Int downPos = new Vector2Int(origin.x, origin.y - 1);
        if (Tiles.TryGetValue(downPos, out CatsWork.Tile downTile))
        {
            if (downTile.CurrentState == CatsWork.Tile.TileState.Empty)
            {
                if (downTile.MyObject == null)
                    pushedTilesForObject.Add(downTile);

                pushedTilesForUnit.Add(downTile);
            }
        }


        //타일의 오브젝트들을 밀쳐줍니다.
        UnitBase targetUnit = targetTile.MyUnit;
        PlaceableObjectBase targetObject = targetTile.MyObject;

        if (targetObject != null && pushedTilesForObject.Count != 0)
        {
            int randomIndex = Random.Range(0, pushedTilesForObject.Count);
            CatsWork.Tile destTile = pushedTilesForObject[randomIndex];

            targetObject.Move(destTile);
        }

        if (targetUnit != null && pushedTilesForUnit.Count != 0)
        {
            int randomIndex = Random.Range(0, pushedTilesForUnit.Count);
            CatsWork.Tile destTile = pushedTilesForUnit[randomIndex];

            targetUnit.Move(destTile);
        }


        //현재 타일에 아직 유닛이나 길을 막는 오브젝트가 남아있다면 false
        if (targetTile.CurrentState != CatsWork.Tile.TileState.Empty)
            return false;

        return true;
    }
    #endregion

}
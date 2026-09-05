using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CatsWork.Tile;

namespace CatsWork
{
    /// <summary>
    /// 타일맵의 한 타일입니다.
    /// </summary>
    public class Tile
    {
        public enum TileState
        {
            Empty,      // 유닛이 없는 상태
            Occupied,   // 유닛이 있는 상태
            Blocked,     // 배치 불가능한 상태
            Player,     // 올라간 유닛이 플레이어인 상태
        }

        /// <summary>
        /// 타일의 현재 상태
        /// </summary>
        public TileState CurrentState { get; private set; } = TileState.Empty;

        /// <summary>
        /// 그리드상의 타일 위치
        /// </summary>
        public Vector2Int GridPosition { get; private set; }
        public UnitBase MyUnit { get; private set; }
        public PlaceableObjectBase MyObject { get; private set; }

        /// <summary>
        /// 타일 위의 유닛에 데미지를 줍니다.
        /// </summary>
        public void TakeDamageToUnit(int value, UnitBase Attacker = null, bool damgeToPlayer = false)
        {
            //플레이어 데미지 허용
            if (damgeToPlayer)
            {
                if (CurrentState == TileState.Occupied || CurrentState == TileState.Player)
                {
                    if (MyUnit != null)
                        MyUnit.TakeDamage(value, Attacker);
                }
            }
            else
            {
                if (CurrentState == TileState.Occupied)
                {
                    if (MyUnit != null)
                        MyUnit.TakeDamage(value, Attacker);
                }
            }

            ////오브젝트가 따로 있으면 또 줍니다.
        }

        /// <summary>
        /// 타일 위의 비유닛에 데미지를 줍니다.
        /// </summary>
        public void TakeDamageToObject(int value, UnitBase Attacker = null, bool damgeToPlayer = false)
        {
            if (MyObject != null)
                MyObject.OnObjectHit();
        }



        public void Initialize(Vector2Int gridPos)
        {
            GridPosition = gridPos;
            CurrentState = TileState.Empty;
        }

        /// <summary>
        /// 타일에 유닛을 올립니다.
        /// triggerObject 가 false 면 이 칸의 오브젝트(덫 등)를 발동시키지 않습니다.
        /// 유닛 등장처럼 발동 시점을 뒤로 미뤄야 하는 경우에 사용하며,
        /// 그 경우 호출한 쪽이 적절한 시점에 OnObjectStepped 를 직접 불러야 합니다.
        /// </summary>
        public void SetUnit(UnitBase unit, bool triggerObject = true)
        {
            MyUnit = unit;

            if(unit.UnitType == UnitType.Player)
                CurrentState = TileState.Player;
            else
                CurrentState = TileState.Occupied;

            if (triggerObject && MyObject != null)
                MyObject.OnObjectStepped(unit);

        }

        public void SetPlaceableObject(PlaceableObjectBase obj)
        {
            MyObject = obj;

            if(obj.IsBlocked)
            {
                if (MyUnit != null)
                    Debug.LogError("blocked 오브젝트가 유닛 위에 존재합니다.");
                CurrentState = TileState.Blocked;
            }

        }

        public void DisposeUnit()
        {
            MyUnit = null;
            CurrentState = TileState.Empty;
        }

        public void DisposePlaceableObject()
        {
            MyObject = null;
            if (MyUnit != null)
                CurrentState = TileState.Occupied;
            else
                CurrentState = TileState.Empty;
        }
    }



    public class VirtualTileData
    {
        public TileState CurrentState;
        public UnitBase MyUnit;
        public PlaceableObjectBase MyObject;

        public VirtualTileData(TileState state, UnitBase myUnit, PlaceableObjectBase myObject)
        {
            CurrentState = state;
            MyUnit = myUnit;
            MyObject = myObject;
        }
    }
}
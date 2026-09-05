using System.Collections.Generic;
using System;
using UnityEngine;
using CatsWork;
using System.Linq;

[Serializable]
public class BehaviorData
{
    public BehaviorType Type { get; }
    public List<Vector2Int?> TargetPos { get; } // 행동 대상 타일
    public int? Value { get; }    // UI 예고용


    public bool IsTriggerBehaviorActivate = false;
    /// <summary>
    /// Move가 들어가는 행동의 경우 startPos를 반드시 전달해야합니다.
    /// </summary>
    public BehaviorData(UnitBase unit, BehaviorType type, Vector2Int? startPos = null, List<Vector2Int?> targetPos = null, int? value = null, bool isTriggerBehaviorActivate = false, List<Vector2Int> occupiedGridOffset = null)
    {
        Type = type;

        if (targetPos == null)
            TargetPos = null;
        else
            TargetPos = new(targetPos);

        //유닛은 기본적으로 1칸입니다.
        if(occupiedGridOffset == null)
        {
            occupiedGridOffset = new() { Vector2Int.zero };
        }


        if (type == BehaviorType.Move || type == BehaviorType.MoveAttack)
        {
            //현재 위치를 유닛의 몸집민큼 예약취소
            foreach (Vector2Int offset in occupiedGridOffset)
            {
                VirtualTileData virtualTile = FieldManager.Instance.VirtualTiles[startPos.Value + offset];
                virtualTile.CurrentState = Tile.TileState.Empty;
                virtualTile.MyUnit = null;
            }

            //마지막 위치를 예약합니다
            foreach (Vector2Int offset in occupiedGridOffset)
            {
                Vector2Int targetValue = TargetPos.Last().Value + offset;

                VirtualTileData virtualTile = FieldManager.Instance.VirtualTiles[targetValue];
                virtualTile.CurrentState = Tile.TileState.Occupied;
                virtualTile.MyUnit = unit;
            }
        }

        if (value != null)
            Value = value.Value;
        else
            Value = null;

        IsTriggerBehaviorActivate = isTriggerBehaviorActivate;
    }
    /// <summary>
    /// Move가 들어가는 행동의 경우 startPos를 반드시 전달해야합니다.
    /// </summary>
    public BehaviorData(UnitBase unit, BehaviorType type, Vector2Int? startPos, Vector2Int targetPos, int? value = null, bool isTriggerBehaviorActivate = false, List<Vector2Int> occupiedGridOffset = null)
    {
        Type = type;

        if (targetPos == null)
            TargetPos = null;
        else
            TargetPos = new() { targetPos };


        //유닛은 기본적으로 1칸입니다.
        if (occupiedGridOffset == null)
        {
            occupiedGridOffset = new() { Vector2Int.zero };
        }


        if (type == BehaviorType.Move || type == BehaviorType.MoveAttack)
        {
            //현재 위치를 유닛의 몸집민큼 예약취소
            foreach (Vector2Int offset in occupiedGridOffset)
            {
                VirtualTileData virtualTile = FieldManager.Instance.VirtualTiles[startPos.Value + offset];
                virtualTile.CurrentState = Tile.TileState.Empty;
                virtualTile.MyUnit = null;
            }

            //마지막 위치를 예약합니다
            foreach (Vector2Int offset in occupiedGridOffset)
            {
                Vector2Int targetValue = TargetPos.Last().Value + offset;

                VirtualTileData virtualTile = FieldManager.Instance.VirtualTiles[targetValue];
                virtualTile.CurrentState = Tile.TileState.Occupied;
                virtualTile.MyUnit = unit;
            }
        }



        if (value != null)
            Value = value.Value;
        else
            Value = null;

        IsTriggerBehaviorActivate = isTriggerBehaviorActivate;
    }

    /// <summary>
    /// UI 재활성화 여부를 결정하기 위해 변경되는 정보가 기존과 같은지 검사합니다.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool IsSameAs(BehaviorData other)
    {
        if (other == null)
        {
            return false;
        }

        if (Type != other.Type)
        {
            return false;
        }

        if (Value != other.Value)
        {
            return false;
        }

        if (IsTriggerBehaviorActivate != other.IsTriggerBehaviorActivate)
        {
            return false;
        }

        if (TargetPos == null && other.TargetPos == null)
        {
            return true;
        }

        if (TargetPos == null || other.TargetPos == null)
        {
            return false;
        }



        return true;
    }
}

/// <summary>
/// 유닛의 턴 종료시 행동 예고
/// </summary>
public enum BehaviorType
{
    None,
    Move,
    Attack,
    MoveAttack,
    AttackWarning,
    Heal,
    Special,
    BehaviorPlayed, // 다음턴이 시작될때까지 행동을 쉽니다. (턴을 종료하거나 유닛이 행동하여 행동이 실행되었을 경우)
    Stun,
    Summon,
    Buff,
    Debuff,
    TrapAttack, // 덫의 공격력 표시용. 행동 시스템에는 참여하지 않고 HUD/툴팁만 사용한다.
}
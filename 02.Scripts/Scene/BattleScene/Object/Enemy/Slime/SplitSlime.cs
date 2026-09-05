using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

//
public class SplitSlime : UnitBase
{
    public override UnitType UnitType { get; } = UnitType.Enemy;


    public override void CalculateNextBehavior()
    {
        base.CalculateNextBehavior();

        //플레이어와의 사거리 체크
        int distance = Utility.GetManhattanDistance(FieldManager.Instance.PlayerInstance.GridPosition, GridPosition);
        if (distance <= Range)
        {
            if (MoveRange > 0)
            {
                //사거리 내에 들어왔으니 할일해
                SetBehaviorData(new BehaviorData(this, BehaviorType.Attack, GridPosition, FieldManager.Instance.PlayerInstance.GridPosition, damage));
            }
        }
        else //사거리 내에 없을경우에만 길찾기 알고리즘을 사용합니다.
        {
            //사거리 이내에 없으면 이동시도
            //이동이 가능한지 체크 먼저

            //플레이어를 향해 전진
            List<Vector2Int> path = PathFindingToAttackablePosition(CatsWork.MovingType.UDRL);

            if (path == null)
            {
                //길찾기가 실패할경우 SubMove 시도
                Vector2Int? destPos = CalcSubMovePosition();
                if (destPos != null)
                {
                    SetBehaviorData(new BehaviorData(this, BehaviorType.Move, GridPosition, destPos.Value));
                }
                else
                {
                    SetBehaviorData(new BehaviorData(this, BehaviorType.None));
                }
            }
            else
            {
                SetBehaviorMoveOrMoveAttack(path);
            }
        }
    }

    public override void TakeDamage(int damage, UnitBase attacker = null)
    {
        base.TakeDamage(damage, attacker);
    }


}

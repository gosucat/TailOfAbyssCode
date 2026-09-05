using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class TurnManager
{

    /// <summary>
    /// 턴 시작시 관련 버프 발동
    /// </summary>
    private IEnumerator OnTurnStartBuff()
    {
        Player player = FieldManager.Instance.PlayerInstance;

        //유닛들의 버프를 실행합니다.
        for (int i = 0; i < player.Buffs.Count; i++)
        {
            bool isActive = player.Buffs[i].OnTurnStart();
            if (isActive)
                yield return Utility.WaitForSeconds(0.2f);
        }

        yield return Utility.WaitForSeconds(0.2f);

        var sortedEnemy = BattleSceneManager.Instance.EnemyList.ToList();
        for (int i = 0; i < sortedEnemy.Count; i++)
        {
            List<BuffBase> buffs = sortedEnemy[i].Buffs;
            for (int j = 0; j < buffs.Count; j++)
            {
                bool isActive = buffs[j].OnTurnStart();
                if (isActive)
                    yield return Utility.WaitForSeconds(0.2f);
            }
        }
    }

    /// <summary>
    /// 턴 종료시 관련 버프 발동
    /// </summary>
    private IEnumerator OnTurnEndBuff()
    {
        Player player = FieldManager.Instance.PlayerInstance;

        //유닛들의 버프를 실행합니다.
        for (int i = 0; i < player.Buffs.Count; i++)
        {
            bool isActive = player.Buffs[i].OnTurnEnd();
            if(isActive)
                yield return Utility.WaitForSeconds(0.2f);
        }

        yield return Utility.WaitForSeconds(0.2f);

        var sortedEnemy = BattleSceneManager.Instance.EnemyList.ToList();
        for (int i = 0; i < sortedEnemy.Count; i++)
        {
            List<BuffBase> buffs = sortedEnemy[i].Buffs;
            for (int j = 0; j < buffs.Count; j++)
            {
                bool isActive = buffs[j].OnTurnEnd();
                if (isActive)
                    yield return Utility.WaitForSeconds(0.2f);
            }
        }
    }



    private IEnumerator SetAllEnemyFlee()
    {
        var sortedEnemy = BattleSceneManager.Instance.EnemyList.ToList();
        for (int i = 0; i < sortedEnemy.Count; i++)
        {
            yield return StartCoroutine(RunUnitRoutineSafe(sortedEnemy[i], sortedEnemy[i].OnUnitFlee()));
            yield return Utility.WaitForSeconds(0.1f);
        }
    }

    /// <summary>
    /// 유닛의 행동 코루틴을 안전하게 실행합니다.
    /// 유닛이 행동 도중 파괴되면(덫을 밟고 죽는 경우 등) 그 유닛이 주인인 하위 코루틴이 그대로 멈추고,
    /// 완료를 기다리던 턴 진행이 영구히 대기 상태에 빠집니다.
    /// 유닛이 사라졌는지를 매 프레임 감시해서, 그런 경우 기다림을 끊고 턴을 계속 진행시킵니다.
    /// </summary>
    private IEnumerator RunUnitRoutineSafe(UnitBase unit, IEnumerator routine)
    {
        if (unit == null || routine == null)
            yield break;

        bool isFinished = false;
        StartCoroutine(RunAndNotify(routine, () => isFinished = true));

        while (!isFinished)
        {
            if (unit == null)
                yield break;

            yield return null;
        }
    }

    private IEnumerator RunAndNotify(IEnumerator routine, System.Action onFinished)
    {
        yield return StartCoroutine(routine);

        onFinished();
    }


    /// <summary>
    /// 현존하는 모든 적 유닛이 겁쟁이인지
    /// </summary>
    private bool IsAllEnemyCoward()
    {
        var sortedEnemy = BattleSceneManager.Instance.EnemyList;
        for (int i = 0; i < sortedEnemy.Count; i++)
        {
            List<BuffBase> buffs = sortedEnemy[i].Buffs;

            bool isCoward = false;
            for (int j = 0; j < buffs.Count; j++)
            {
                if (buffs[j].BuffType == BuffType.Coward)
                {
                    isCoward = true;
                    break;
                }
            }

            if (!isCoward)
                return false;
        }

        return true;
    }



}

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using CatsWork;
using UnityEngine;

public partial class BattleSceneManager
{
    public List<UnitBase> EnemyList = new();
    public List<PlaceableObjectBase> PlaceableObjects = new();
    public List<UnitBase> NPCList = new();

    [SerializeField] Transform enemyParent;
    [SerializeField] Transform placeableObjectParent;
    [SerializeField] Transform NPCParent;

    int spawnCount;

    private void ResetSpawnCount()
    {
        spawnCount = 0;
    }

    public UnitBase AddEnemy(GameObject enemyPrefab, Tile targetTile)
    {
        if (enemyPrefab == null) return null;

        UnitBase enemy = Instantiate(enemyPrefab, enemyParent).GetComponent<UnitBase>();
        if (enemy == null) return null;

        //Init 안에서 덫을 밟아 즉시 죽을 수 있다. 그 경우 사망 처리가 RemoveEnemy 를 호출하므로
        //목록 등록이 먼저 끝나 있어야 죽은 유닛이 목록에 남지 않는다.
        EnemyList.Add(enemy);

        enemy.Init(targetTile, spawnCount);
        spawnCount++;

        //소환되자마자 죽었다면 호출한 쪽이 이 유닛을 참조하지 않도록 한다.
        if (enemy.IsDying)
            return null;

        return enemy;
    }

    public void RemoveEnemy(UnitBase unit)
    {
        if (unit == null) return;

        EnemyList.Remove(unit);
    }

    public void AddPlaceableObject(PlaceableObjectBase obj, Tile targetTile)
    {
        if(obj == null) return;

        PlaceableObjectBase placeableObject = Instantiate(obj, placeableObjectParent);
        placeableObject.Init(targetTile);

        PlaceableObjects.Add(placeableObject);
    }

    /// <summary>
    /// 덫 전용 스폰. Init 이전에 공격력을 주입하므로 HUD가 처음부터 올바른 값으로 표시된다.
    /// 대상 칸에 이미 덫이 있으면 새로 생성하지 않고 기존 덫에 중첩 처리를 위임한다.
    /// </summary>
    public TrapBase AddTrap(TrapBase trapPrefab, Tile targetTile, int damage)
    {
        if (trapPrefab == null || targetTile == null) return null;

        // 덫 위에 덫 설치 → 기존 덫이 효과를 결정 (기본: 데미지 합산)
        TrapBase existingTrap = targetTile.MyObject as TrapBase;
        if (existingTrap != null)
        {
            existingTrap.OnTrapStacked(trapPrefab, damage);
            return existingTrap;
        }

        TrapBase trap = Instantiate(trapPrefab, placeableObjectParent);
        trap.SetDamage(damage);     // Init 전 주입
        trap.Init(targetTile);

        PlaceableObjects.Add(trap);
        return trap;
    }

    public void RemovePlaceableObject(PlaceableObjectBase obj)
    {
        if (obj == null) return;

        PlaceableObjects.Remove(obj);
    }

    public void AddNPC(GameObject npcPrefab, Tile targetTile)
    {
        if (npcPrefab == null) return;

        UnitBase npc = Instantiate(npcPrefab, NPCParent).GetComponent<UnitBase>();
        if(npc == null) return;

        npc.Init(targetTile, spawnCount);
        spawnCount++;

        NPCList.Add(npc);
    }

    public void RemoveNPC(UnitBase npc)
    {
        if (npc == null) return;

        NPCList.Remove(npc);
    }

    /// <summary>
    /// 플레이어를 제외한 현재 필드에 있는 모든 오브젝트들을 정리합니다.
    /// </summary>
    public void DisposeObjects()
    {
        //현재 유닛을 정리
        foreach (UnitBase unit in EnemyList.ToList())
        {
            unit.DestroyUnit();
        }
        EnemyList.Clear();
        //현재 오브젝트도 정리
        foreach (PlaceableObjectBase obj in PlaceableObjects.ToList())
        {
            obj.DestroyObject();
        }
        PlaceableObjects.Clear();

        //흡수 연출 등으로 목록에서 미리 빠진 오브젝트까지 정리합니다.
        for (int i = placeableObjectParent.childCount - 1; i >= 0; i--)
            Destroy(placeableObjectParent.GetChild(i).gameObject);

        //NPC도 정리
        foreach (UnitBase unit in NPCList.ToList())
        {
            unit.DestroyUnit();
        }
        NPCList.Clear();
    }
}

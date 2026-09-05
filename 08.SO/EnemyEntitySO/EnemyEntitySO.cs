using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몹 데이터의 틀입니다.
/// </summary>
[CreateAssetMenu(fileName = "Enemy_01", menuName = "ScriptableObject/Enemy Data")]
public class EnemyEntitySO : ScriptableObject
{
    public string Name;
    public int Hp;
    public int Damage;
    public int GoldAmount;
    public int Range;
    public int MoveRange;
    [Header("행동 우선권(낮을수록 빠름")]
    public int Priority;
    [Header("키 : 행동HUD의 높이")]
    public float Height;
    [Header("hp바 오프셋")]
    public Vector3 HpBarOffset;

    //[Header("기동 타격 유닛(비주얼 전용)")]

    [Header("유닛 아이콘")]
    public Sprite Icon;
    [Header("유닛이 너무 클 경우, intensity가 너무 높으면 눈부십니다.")]
    public float HighlightIntensity = 1.8f;

    [Header("몹 기준 예상 행동 트리거 칸(없을시 사거리 기준)")]
    public List<Vector2Int> TriggerArea;

}

using System;
using System.Collections;
using System.Collections.Generic;
using CatsWork;
using UnityEngine;


/// <summary>
/// 카드 데이터의 틀입니다.
/// </summary>
[CreateAssetMenu(fileName = "Card_01", menuName = "ScriptableObject/Card Data")] //, order = int.MaxValue
public class CardEntitySO : ScriptableObject
{
    public string CardName;
    public int Cost;
    public int Value;
    [Header("사거리")]
    public int Range = -1;
    [Header("타겟/비타겟 설정")]
    public bool IsSelectable;
    [Header("적/오브젝트에 쓸수 없는 카드(직접 이동 등)")]
    public bool IsCannotTargetEnemy;
    [Header("수치가 영향을 받는 공격 카드인지")]
    public bool IsDamageEnhanceCard;
    [Header("오브젝트에는 효과가 없는 카드(true면 인디케이터가 Occupied 타일을 제외)")]
    public bool IsNoEffectOnObject;
    [Header("돌진형 카드 타입(장애물에 막히는 카드인지)")]
    public MovingType MovingType;

    [Header("카드 타입")]
    public CardType CardType;
    [Header("희귀도 가중치(숫자가 클수록 흔함")]
    public float RarityWeight;
    public Rarity Rarity;
    [Header("캐릭터 전용 카드")]
    public Character Character;

    [Header("카드 강화A,B")]
    public List<CardEntitySO> EnhancedCards;
    [Header("카드 프리뷰 지원")]
    public CardEntitySO PreviewCard;
    //해당 카드의 기본 키워드들
    public List<KeywordType> KeywordTypes;
    [Header("툴팁으로만 설명할 키워드를 정합니다.")]
    public List<KeywordType> TooltipInfo;
    [Header("플레이어 기준 카드를 사용 가능한 칸(없을시 사거리 기준")]
    public List<Vector2Int> SelectableAreaFromPlayer;
    [Header("발동 위치로부터 효과 범위")]
    public List<Vector2Int> EffectArea;
    public Sprite CardImage;

    [Header("효과 대상에게 보여질 이펙트 프리팹")]
    public GameObject CardEffectPrefab;

    [Header("설치형 카드가 생성할 오브젝트(덫 등) 프리팹")]
    public PlaceableObjectBase SpawnObjectPrefab;

    [Header("카드의 기능을 이름과 분리하여 따로 정할 수 있습니다. 없으면 이름으로 검색합니다.")]
    public string CardFunctionKey;
    [Header("카드의 번역을 이름과 분리하여 따로 정할 수 있습니다. 없으면 이름으로 검색합니다.")]
    public string CardInfoKey;
    [Header("리비엘식")]
    public bool LibielArts = false;
}

public enum CardType
{
    Martial,
    Magic,
    Tactic,
    Event,

}

public enum Rarity
{
    Common,
    Rare,
    Unique,
    Legendary,
    Basic,

}
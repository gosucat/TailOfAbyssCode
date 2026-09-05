using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


/// <summary>
/// 카드 덱 리스트의 요소인 CardData 입니다.
/// </summary>
public class CardData
{
    public string CardName { get; private set; }
    public int Cost { get; private set; }
    public int Value { get; private set; }

    public List<KeywordType> KeywordTypes { get; private set; } = new();

    public Sprite CardImage { get; private set; }

    public CardEntitySO CardEntitySO { get; private set; }
    /// <summary>
    /// CardEntitySO로부터 초기 데이터를 받아 초기화합니다.
    /// </summary>
    public void Init(CardEntitySO data)
    {
        CardName = data.CardName;
        Cost = data.Cost;
        Value = data.Value;
        KeywordTypes = data.KeywordTypes.ToList();

        CardImage = data.CardImage;

        CardEntitySO = data;
    }
}

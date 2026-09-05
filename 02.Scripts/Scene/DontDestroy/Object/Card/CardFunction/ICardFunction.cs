public interface ICardFunction
{
    //카드 사용이 가능한지(불가능할시 카드를 집는 행위가 불가능)
    bool UseValidateCheck(CardInstance card);
    void OnDraw(CardInstance card);
    void OnUsed(CardInstance card, CatsWork.Tile targetTile = null);
    void OnTurnStart(CardInstance card); 
    void OnTurnEnd(CardInstance card);
    void OnDiscard(CardInstance card);
    string GetDynamicInfoValue(CardInstance card);
    void OnDestroy(CardInstance card);
}
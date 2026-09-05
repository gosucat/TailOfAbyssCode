using System.Collections.Generic;
using System.Diagnostics;
using CatsWork;

public class Slash : CardFunctionBase
{
    public override void OnUsed(CardInstance card, Tile targetTile)
    {
        base.OnUsed(card, targetTile);
        List<CatsWork.Tile> tiles = FieldManager.Instance.GetCardEffectTiles(card, targetTile);

        FieldManager.Instance.PlayerInstance.PlayAnim(card.OriginalData.CardEntitySO);

        DamageToTiles(tiles, card.Value, card);

    }
}

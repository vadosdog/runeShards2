using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Patterns/SingleCellPattern")]
public class SingleCellPattern : AbstractTargetPattern
{
    public override List<HexCell> GetAffectedCells(HexCell centerCell, BattleHexUnit caster)
    {
        return new List<HexCell> { centerCell };
    }
}


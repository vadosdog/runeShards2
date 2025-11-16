using UnityEngine;
using System.Collections.Generic;

public abstract class AbstractTargetPattern : ScriptableObject // Например, крест, круг, линия
{
    public int size = 1;
    public abstract List<HexCell> GetAffectedCells(HexCell centerCell, BattleHexUnit caster);
}
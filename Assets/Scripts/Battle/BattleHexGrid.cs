using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// Component that represents an entire hexagon map for battle
/// </summary>
public class BattleHexGrid : HexGrid
{
    private int maxStamina; // Максимальная стамина для текущего поиска пути
    
    /// <summary>
    /// Включен ли туман войны. Если false, методы видимости не выполняются.
    /// </summary>
    public bool FogOfWarEnabled { get; set; } = false;

    public bool PathIsReachable => HasPath ? searchData[currentPathToIndex].distance <= maxStamina : false;

    public int MoveCost => this.HasPath ? this.searchData[this.currentPathToIndex].distance : -1;

    protected override float GetElevationStep() => HexMetrics.elevationStep * 2f;
    
    /// <summary>
    /// Переопределяем IncreaseVisibility - не работает, если туман войны отключен.
    /// </summary>
    public override void IncreaseVisibility(HexCell fromCell, int range)
    {
        if (!FogOfWarEnabled)
        {
            return; // Не пересчитываем видимость, если туман войны отключен
        }
        base.IncreaseVisibility(fromCell, range);
    }
    
    /// <summary>
    /// Переопределяем DecreaseVisibility - не работает, если туман войны отключен.
    /// </summary>
    public override void DecreaseVisibility(HexCell fromCell, int range)
    {
        if (!FogOfWarEnabled)
        {
            return; // Не пересчитываем видимость, если туман войны отключен
        }
        base.DecreaseVisibility(fromCell, range);
    }
    
    /// <summary>
    /// Переопределяем ResetVisibility - не работает, если туман войны отключен.
    /// </summary>
    public override void ResetVisibility()
    {
        if (!FogOfWarEnabled)
        {
            return; // Не пересчитываем видимость, если туман войны отключен
        }
        base.ResetVisibility();
    }
    
    /// <summary>
    /// Переопределяем IsCellVisible - всегда возвращает true, если туман войны отключен.
    /// </summary>
    public override bool IsCellVisible(int cellIndex)
    {
        if (!FogOfWarEnabled)
        {
            return true; // Все клетки всегда видимы, если туман войны отключен
        }
        return base.IsCellVisible(cellIndex);
    }

	/// <summary>
	/// Try to find a path.
	/// </summary>
	/// <param name="fromCell">Cell to start the search from.</param>
	/// <param name="toCell">Cell to find a path towards.</param>
	/// <param name="unit">Unit for which the path is.</param>
	public override void FindPath(HexCell fromCell, HexCell toCell, HexUnit unit)
	{
		if (currentPathFromIndex == fromCell.Index && currentPathToIndex == toCell.Index)
		{
			return;
		}
		ClearPath();
		currentPathFromIndex = fromCell.Index;
		currentPathToIndex = toCell.Index;

        BattleHexUnit battleUnit = unit as BattleHexUnit;
		maxStamina = battleUnit?.currentStamina ?? 5;

		currentPathExists = SearchWithStamina(fromCell, toCell, unit);
        ShowStaminaPath();
	}

	/// <summary>
    /// Поиск пути с ограничением по стамине
    /// </summary>
    private bool SearchWithStamina(HexCell fromCell, HexCell toCell, HexUnit unit)
    {
        searchFrontierPhase += 2;
        searchFrontier ??= new HexCellPriorityQueue(this);
        searchFrontier.Clear();

        searchData[fromCell.Index] = new HexCellSearchData
        {
            searchPhase = searchFrontierPhase,
            distance = 0
        };
        searchFrontier.Enqueue(fromCell.Index);
        
        while (searchFrontier.TryDequeue(out int currentIndex))
        {
            HexCell current = GetCell(currentIndex);
            int currentDistance = searchData[currentIndex].distance;
            searchData[currentIndex].searchPhase += 1;

            if (current == toCell)
            {
                return true;
            }

            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                if (!current.TryGetNeighbor(d, out HexCell neighbor))
				{
					continue;
				}
                
                HexCellSearchData neighborData = searchData[neighbor.Index];
                if (neighborData.searchPhase > searchFrontierPhase ||
                    !unit.IsValidDestination(neighbor))
                {
                    continue;
                }
                
                int moveCost = unit.GetMoveCost(current, neighbor, d);
                if (moveCost < 0)
                {
                    continue;
                }

                int distance = currentDistance + moveCost;

                if (neighborData.searchPhase < searchFrontierPhase)
                {
                    searchData[neighbor.Index] = new HexCellSearchData
                    {
                        searchPhase = searchFrontierPhase,
                        distance = distance,
                        pathFrom = currentIndex,
                        heuristic = neighbor.Coordinates.DistanceTo(toCell.Coordinates)
                    };
                    searchFrontier.Enqueue(neighbor.Index);
                }
                else if (distance < neighborData.distance)
                {
                    searchData[neighbor.Index].distance = distance;
                    searchData[neighbor.Index].pathFrom = currentIndex;
                    searchFrontier.Change(neighbor.Index, neighborData.SearchPriority);
                }
            }
        }
        return false;
    }

    /// <summary>
    /// Показать путь с учетом стамины
    /// </summary>
    private void ShowStaminaPath()
    {
        if (currentPathExists)
        {
            int currentIndex = currentPathToIndex;
            int totalCost = 0;
            bool firstInRange = true;

            while (currentIndex != currentPathFromIndex)
            {
                HexCell cell = GetCell(currentIndex);
                int cellCost = searchData[currentIndex].distance - searchData[searchData[currentIndex].pathFrom].distance;
                totalCost += cellCost;

                // Подсвечиваем клетки в зависимости от оставшейся стамины
                // Так как подсветка идет с 0, то первую (последнюю в пути) мы подсвечиваем зеленым
                Color pathColor;
                if (searchData[currentIndex].distance <= maxStamina)
                {
                    if (firstInRange)
                    {
                        firstInRange = false;
                        pathColor = Color.green;
                    }
                    else
                    {
                        pathColor = Color.white;
                    }

                }
                else
                {
                    pathColor = Color.gray;
                }
                ///	Color pathColor = searchData[currentIndex].distance <= maxStamina ? Color.white : Color.gray;
                EnableHighlight(currentIndex, pathColor);

                // Показываем стоимость перемещения до этой клетки
                SetLabel(currentIndex, searchData[currentIndex].distance.ToString());

                currentIndex = searchData[currentIndex].pathFrom;
            }
        }

        // Подсвечиваем стартовую и конечную клетки
        EnableHighlight(currentPathFromIndex, Color.green);
    }
}
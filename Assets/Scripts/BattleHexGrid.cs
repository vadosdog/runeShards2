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
		maxStamina = battleUnit?.currentStamina ?? unit.Speed;

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
        EnableHighlight(currentPathFromIndex, Color.blue);
        // EnableHighlight(currentPathToIndex, Color.green);
    }

    /// <summary>
    /// Получить стоимость пути до указанной клетки
    /// </summary>
    public int GetPathCost(HexCell toCell)
    {
        if (currentPathExists && toCell.Index == currentPathToIndex)
        {
            return searchData[toCell.Index].distance;
        }
        return int.MaxValue;
    }

    /// <summary>
    /// Проверить, достижима ли клетка в пределах стамины
    /// </summary>
    public bool IsCellReachable(HexCell fromCell, HexCell toCell, BattleHexUnit unit)
    {
        if (fromCell == null || toCell == null || unit == null)
            return false;

        // Быстрая проверка - если клетки нет в зоне досягаемости
        if (!IsInStaminaRange(fromCell, toCell, unit.currentStamina))
            return false;

        // Полноценный поиск пути
        FindPath(fromCell, toCell, unit);
        int pathCost = GetPathCost(toCell);
        
        return currentPathExists && pathCost <= unit.currentStamina;
    }

    /// <summary>
    /// Быстрая проверка нахождения в зоне досягаемости
    /// </summary>
    private bool IsInStaminaRange(HexCell fromCell, HexCell toCell, int stamina)
    {
        // Используем манхэттенское расстояние для быстрой оценки
        int distance = fromCell.Coordinates.DistanceTo(toCell.Coordinates);
        return distance <= stamina;
    }
}
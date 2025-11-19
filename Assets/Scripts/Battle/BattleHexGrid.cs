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
    
    // Траектория для дальних атак (аналогично currentPathFromIndex/ToIndex)
    private int currentTrajectoryFromIndex = -1;
    private int currentTrajectoryToIndex = -1;
    private List<HexCell> cachedTrajectoryCells = null;
    private HexCell cachedFirstObstacle = default;
    
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

    /// <summary>
    /// Получает список гексов на прямой между двумя гексами (исключая начальный и конечный)
    /// </summary>
    public List<HexCell> GetLineOfSightCells(HexCell fromCell, HexCell toCell)
    {
        List<HexCell> lineCells = new List<HexCell>();
        
        if (fromCell == toCell)
            return lineCells;

        HexCoordinates from = fromCell.Coordinates;
        HexCoordinates to = toCell.Coordinates;
        
        // Используем алгоритм построения линии для гексагональной сетки
        // Основан на интерполяции в кубических координатах
        int distance = from.DistanceTo(to);
        
        // Если расстояние 1 или меньше, нет промежуточных ячеек
        if (distance <= 1)
        {
            return lineCells; // Возвращаем пустой список - нет промежуточных ячеек
        }

        // Интерполируем между координатами
        for (int i = 1; i < distance; i++) // Начинаем с 1, чтобы исключить начальную ячейку
        {
            float t = (float)i / distance;
            
            // Интерполируем в кубических координатах
            float x = Mathf.Lerp(from.X, to.X, t);
            float y = Mathf.Lerp(from.Y, to.Y, t);
            float z = Mathf.Lerp(from.Z, to.Z, t);
            
            // Округляем до ближайшего гекса
            int rx = Mathf.RoundToInt(x);
            int ry = Mathf.RoundToInt(y);
            int rz = Mathf.RoundToInt(z);
            
            // Корректируем, если сумма не равна нулю (кубические координаты должны суммироваться в 0)
            float dx = Mathf.Abs(x - rx);
            float dy = Mathf.Abs(y - ry);
            float dz = Mathf.Abs(z - rz);
            
            if (dx > dy && dx > dz)
            {
                rx = -ry - rz;
            }
            else if (dz > dy)
            {
                rz = -rx - ry;
            }
            
            // Преобразуем обратно в offset координаты и получаем ячейку
            HexCoordinates hexCoord = new HexCoordinates(rx, rz);
            HexCell cell = GetCell(hexCoord);
            
            // Добавляем ячейку, если она валидна, не является начальной/конечной и еще не добавлена
            if (cell && cell != fromCell && cell != toCell && !lineCells.Contains(cell))
            {
                lineCells.Add(cell);
            }
        }
        
        return lineCells;
    }

    /// <summary>
    /// Проверяет наличие укрытия для цели при дальних атаках
    /// Укрытие есть если: препятствие выше И атакующего, И цели
    /// Использует ту же логику поиска препятствия, что и CalculateTrajectory
    /// </summary>
    /// <param name="fromCell">Ячейка атакующего</param>
    /// <param name="toCell">Ячейка цели</param>
    /// <returns>true если укрытие есть, false если нет</returns>
    public bool HasCover(HexCell fromCell, HexCell toCell)
    {
        int casterElevation = fromCell.Values.Elevation;
        int targetElevation = toCell.Values.Elevation;
        
        // Получаем ячейки на прямой между атакующим и целью
        List<HexCell> lineCells = GetLineOfSightCells(fromCell, toCell);
        
        if (lineCells.Count == 0)
        {
            return false;
        }

        // Ищем первое препятствие: ячейку на пути, которая выше И атакующего, И цели
        // Используем ту же логику, что и в CalculateTrajectory
        foreach (HexCell cell in lineCells)
        {
            int cellElevation = cell.Values.Elevation;
            
            // Препятствие должно быть выше И атакующего, И цели
            if (cellElevation > casterElevation && cellElevation > targetElevation)
            {
                // Нашли первое препятствие - укрытие есть
                return true;
            }
        }
        
        // Препятствий не найдено - укрытия нет
        return false;
    }

    /// <summary>
    /// Устанавливает текст метки на ячейке (публичный метод для использования из навыков)
    /// </summary>
    public void SetCellLabel(int cellIndex, string text)
    {
        SetLabel(cellIndex, text);
    }

    /// <summary>
    /// Очищает метку на ячейке
    /// </summary>
    public void ClearCellLabel(int cellIndex)
    {
        SetLabel(cellIndex, null);
    }

    /// <summary>
    /// Вычисляет траекторию для дальних атак (аналогично FindPath)
    /// </summary>
    public void FindTrajectory(HexCell fromCell, HexCell toCell)
    {
        // Проверяем, та же ли траектория (как в FindPath)
        if (currentTrajectoryFromIndex == fromCell.Index && currentTrajectoryToIndex == toCell.Index)
        {
            return; // Та же траектория, не пересчитываем
        }
        
        ClearTrajectory();
        currentTrajectoryFromIndex = fromCell.Index;
        currentTrajectoryToIndex = toCell.Index;
        
        // Вычисляем траекторию
        CalculateTrajectory(fromCell, toCell);
    }

    /// <summary>
    /// Вычисляет траекторию и находит первое препятствие
    /// Препятствие - это ячейка, которая выше И атакующего, И цели
    /// Использует ту же логику поиска, что и HasCover
    /// </summary>
    private void CalculateTrajectory(HexCell fromCell, HexCell toCell)
    {
        // Получаем ячейки на прямой
        cachedTrajectoryCells = GetLineOfSightCells(fromCell, toCell);
        
        // Ищем первое препятствие (ячейку выше И атакующего, И цели)
        int casterElevation = fromCell.Values.Elevation;
        int targetElevation = toCell.Values.Elevation;
        cachedFirstObstacle = default;
        
        // Ищем первое препятствие только среди промежуточных ячеек (fromCell и toCell уже исключены в GetLineOfSightCells)
        foreach (HexCell cell in cachedTrajectoryCells)
        {
            int cellElevation = cell.Values.Elevation;
            
            // Препятствие должно быть выше И атакующего, И цели
            if (cellElevation > casterElevation && cellElevation > targetElevation)
            {
                cachedFirstObstacle = cell;
                break; // Берем первое препятствие
            }
        }
    }

    /// <summary>
    /// Получает список ячеек на текущей траектории (аналогично GetPath)
    /// </summary>
    public List<HexCell> GetTrajectory()
    {
        if (cachedTrajectoryCells == null)
        {
            return null;
        }
        return cachedTrajectoryCells;
    }

    /// <summary>
    /// Получает первое препятствие на текущей траектории
    /// </summary>
    public HexCell GetFirstObstacle()
    {
        return cachedFirstObstacle;
    }

    /// <summary>
    /// Очищает текущую траекторию (аналогично ClearPath)
    /// </summary>
    public void ClearTrajectory()
    {
        cachedTrajectoryCells = null;
        cachedFirstObstacle = default;
        currentTrajectoryFromIndex = -1;
        currentTrajectoryToIndex = -1;
    }
}
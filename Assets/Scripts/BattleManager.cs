using UnityEngine;
using System.Collections.Generic;

public class BattleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGrid hexGrid;
    [SerializeField] private HexMapCamera hexMapCamera;

    [Header("Battle Settings")]

    [SerializeField] private GameObject playerUnitPrefab;
	[SerializeField] private GameObject enemyUnitPrefab;

    private BattleConfig battleConfig;

    void Start()
    {
        // Получаем конфиг из меню выбора
        battleConfig = BattleSettingsManager.CurrentConfig;

        // Если конфига нет (тестируем напрямую), создаем дефолтный
        if (battleConfig == null)
        {
            battleConfig = new BattleConfig();
            Debug.LogWarning("BattleConfig не найден, использую настройки по умолчанию");
        }

        InitializeBattle();
    }

    private void InitializeBattle()
    {
        // 1. Настраиваем карту
        SetupMap();

        // 2. Настраиваем камеру
        SetupCamera();

        // 3. Спавним юнитов
        SpawnUnits();

        // 4. Настраиваем туман войны
        SetupFogOfWar();

        Debug.Log("Битва инициализирована!");
    }

    private void SetupMap()
    {
        if (hexGrid == null)
        {
            hexGrid = FindFirstObjectByType<HexGrid>();
            if (hexGrid == null)
            {
                Debug.LogError("HexGrid не найден на сцене!");
                return;
            }
        }

        // Устанавливаем размер карты из конфига
        int width = battleConfig.GetGridWidth();
        int height = battleConfig.GetGridHeight();

        hexGrid.CreateMap(width, height, false);

        Debug.Log($"Создана карта размером {width}x{height}");
    }

    private void SetupCamera()
    {
        if (hexMapCamera == null)
        {
            hexMapCamera = FindFirstObjectByType<HexMapCamera>();
        }

        // HexMapCamera обычно автоматически настраивается под размер карты
        // Если нужно что-то дополнительное, можно добавить здесь
    }

    private void SpawnUnits()
    {
        // Временная реализация - спавним тестовых юнитов
        SpawnTestUnits();

        Debug.Log("Юниты размещены на карте");
    }

    private void SetupFogOfWar()
    {
        if (hexGrid != null)
        {
            if (battleConfig.enableFogOfWar)
            {
                // Включаем туман войны - используем стандартную систему видимости
                hexGrid.ResetVisibility();
                Debug.Log("Туман войны включен");
            }
            else
            {
                // Отключаем туман войны - делаем все клетки исследованными и видимыми
                DisableFogOfWar();
                Debug.Log("Туман войны отключен");
            }
        }
    }

    private void DisableFogOfWar()
    {
        // Проходим по всем клеткам и помечаем их как исследованные
        for (int i = 0; i < hexGrid.CellCountX * hexGrid.CellCountZ; i++)
        {
            // Получаем клетку
            HexCell cell = hexGrid.GetCell(i);

            // Устанавливаем флаги: Explorable + Explored
            // В туториале обычно используется cell.Flags для управления состоянием
            cell.Flags = cell.Flags.With(HexFlags.Explorable | HexFlags.Explored);

            // Увеличиваем видимость (это делает клетку видимой)
            hexGrid.IncreaseVisibility(cell, 0); // Range 0 - только сама клетка

            // Обновляем шейдерные данные для визуализации
            hexGrid.ShaderData.RefreshVisibility(i);
        }

        // Принудительно обновляем все чанки
        for (int i = 0; i < hexGrid.CellCountX * hexGrid.CellCountZ; i++)
        {
            hexGrid.RefreshCell(i);
        }
    }

    private void SpawnTestUnits()
    {
        // Получаем стартовые позиции для игрока и врага
        List<HexCell> playerStartPositions = GetPlayerStartPositions();
        List<HexCell> enemyStartPositions = GetEnemyStartPositions();

        // Спавним юнитов игрока
        for (int i = 0; i < Mathf.Min(battleConfig.playerUnitsCount, playerStartPositions.Count); i++)
        {
            SpawnUnitAt(UnitType.Tank, playerStartPositions[i], true);
        }

        // Спавним юнитов врага
        for (int i = 0; i < Mathf.Min(battleConfig.enemyUnitsCount, enemyStartPositions.Count); i++)
        {
            SpawnUnitAt(UnitType.Tank, enemyStartPositions[i], false);
        }
    }

    private void SpawnUnitAt(UnitType unitType, HexCell cell, bool isPlayerUnit)
    {
        if (cell == null)
        {
            Debug.LogError("Попытка создать юнита в null клетке!");
            return;
        }

        // Проверяем, не занята ли уже клетка
        if (cell.Unit != null)
        {
            Debug.LogWarning($"Клетка {cell.Position} уже занята юнитом {cell.Unit.name}");
            return;
        }

        // Используем префаб с BattleHexUnit
        GameObject unitPrefab = isPlayerUnit ? playerUnitPrefab : enemyUnitPrefab;
        
        if (unitPrefab == null)
        {
            Debug.LogError("Префаб юнита не назначен!");
            return;
        }

        // Создаем экземпляр юнита
        GameObject unitInstance = Instantiate(unitPrefab);
        BattleHexUnit battleUnit = unitInstance.GetComponent<BattleHexUnit>();
        
        if (battleUnit == null)
        {
            Debug.LogError("Префаб не содержит компонент BattleHexUnit!");
            Destroy(unitInstance);
            return;
        }

        // ВАЖНО: Используем метод AddUnit из HexGrid для правильного размещения
        hexGrid.AddUnit(battleUnit, cell, Random.Range(0f, 360f));
        
        // Настраиваем тег и имя
        unitInstance.name = isPlayerUnit ? "PlayerUnit" : "EnemyUnit";
        unitInstance.tag = isPlayerUnit ? "PlayerUnit" : "EnemyUnit";

        Debug.Log($"Создан юнит {unitInstance.name} в клетке {cell.Position}");
    }


    private List<HexCell> GetPlayerStartPositions()
    {
        List<HexCell> positions = new List<HexCell>();
        int width = battleConfig.GetGridWidth();
        int height = battleConfig.GetGridHeight();

        // Более безопасный способ получения позиций
        for (int z = 0; z < height; z++)
        {
            for (int x = 0; x < 2; x++) // Берем только первые 2 колонки
            {
                // Используем правильный способ получения ячейки
                HexCoordinates coordinates = new HexCoordinates(x, z);
                HexCell cell = hexGrid.GetCell(coordinates);

                if (cell != null && IsCellSuitableForSpawn(cell))
                {
                    positions.Add(cell);
                    if (positions.Count >= battleConfig.playerUnitsCount)
                        return positions;
                }
            }
        }

        return positions;
    }

    private List<HexCell> GetEnemyStartPositions()
    {
        List<HexCell> positions = new List<HexCell>();
        int width = battleConfig.GetGridWidth();
        int height = battleConfig.GetGridHeight();

        // Более безопасный способ получения позиций
        for (int z = 0; z < height; z++)
        {
            for (int x = width - 2; x < width; x++) // Берем последние 2 колонки
            {
                if (x >= 0) // Проверяем чтобы x не был отрицательным
                {
                    // Используем правильный способ получения ячейки
                    HexCoordinates coordinates = new HexCoordinates(x, z);
                    HexCell cell = hexGrid.GetCell(coordinates);

                    if (cell != null && IsCellSuitableForSpawn(cell))
                    {
                        positions.Add(cell);
                        if (positions.Count >= battleConfig.enemyUnitsCount)
                            return positions;
                    }
                }
            }
        }

        return positions;
    }

    private bool IsCellSuitableForSpawn(HexCell cell)
    {
        if (cell == null) return false;

        // Проверяем что ячейка не под водой (если в туториале есть вода)
        // if (cell.IsUnderwater) return false;

        // Проверяем что ячейка не слишком крутая (если в туториале есть склоны)
        // if (cell.HasCliff) return false;

        // Проверяем высоту (если в туториале есть возвышенности)
        // if (cell.Elevation > 3) return false; // Слишком высоко

        return true;
    }
}

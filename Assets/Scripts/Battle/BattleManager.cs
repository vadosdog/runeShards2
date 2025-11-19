using UnityEngine;
using System.Collections.Generic;
using System.IO;

public class BattleManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HexGrid hexGrid;
    [SerializeField] private HexMapCamera hexMapCamera;

    [Header("Battle Settings")]

    [SerializeField] private GameObject baseUnitPrefab;
	[SerializeField] private UnitData baseUnitData;

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

        if (battleConfig.player1Units.Count == 0)
        {
            // Временно: создаем тестовых юнитов
            battleConfig.player1Units.Add(baseUnitData);
            battleConfig.player1Units.Add(baseUnitData);
        }

        if (battleConfig.player2Units.Count == 0)
        {
            // Временно: создаем тестовых юнитов
            battleConfig.player2Units.Add(baseUnitData);
            battleConfig.player2Units.Add(baseUnitData);
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

        // Проверяем, является ли это BattleHexGrid
        BattleHexGrid battleGrid = hexGrid as BattleHexGrid;
        if (battleGrid == null)
        {
            Debug.LogWarning("HexGrid не является BattleHexGrid. Некоторые функции могут не работать.");
        }

        // Выбираем режим генерации карты
        switch (battleConfig.mapGenerationMode)
        {
            case MapGenerationMode.Prebuilt:
                LoadPrebuiltMap();
                break;

            case MapGenerationMode.Flat:
                CreateFlatMap();
                break;

            case MapGenerationMode.Generated:
            default:
                CreateGeneratedMap();
                break;
        }
    }

    /// <summary>
    /// Создает карту с автоматической генерацией рельефа.
    /// </summary>
    private void CreateGeneratedMap()
    {
        int width = battleConfig.GetGridWidth();
        int height = battleConfig.GetGridHeight();

        hexGrid.CreateMap(width, height, false);

        // Генерируем рельеф
        BattleHexGrid battleGrid = hexGrid as BattleHexGrid;
        if (battleGrid != null)
        {
            BattleMapGenerator.GenerateTerrain(battleGrid);
            Debug.Log($"Создана карта с рельефом размером {width}x{height}");
        }
        else
        {
            Debug.LogWarning("Не удалось привести HexGrid к BattleHexGrid для генерации рельефа.");
        }
    }

    /// <summary>
    /// Создает плоскую карту без рельефа.
    /// </summary>
    private void CreateFlatMap()
    {
        int width = battleConfig.GetGridWidth();
        int height = battleConfig.GetGridHeight();

        hexGrid.CreateMap(width, height, false);
        Debug.Log($"Создана плоская карта размером {width}x{height}");
    }

    /// <summary>
    /// Загружает предсозданную карту из файловой системы.
    /// </summary>
    private void LoadPrebuiltMap()
    {
        string mapName = battleConfig.prebuiltMapName;
        if (string.IsNullOrEmpty(mapName))
        {
            Debug.LogWarning("Имя предсозданной карты не указано. Создаю карту с генерацией.");
            CreateGeneratedMap();
            return;
        }

        // Пытаемся загрузить из файловой системы (persistentDataPath)
        string persistentPath = Path.Combine(Application.persistentDataPath, $"{mapName}.map");
        if (File.Exists(persistentPath))
        {
            LoadMapFromFile(persistentPath);
            return;
        }

        // Пытаемся загрузить из StreamingAssets (для предсозданных карт в сборке)
        string streamingPath = Path.Combine(Application.streamingAssetsPath, "BattleMaps", $"{mapName}.map");
        if (File.Exists(streamingPath))
        {
            LoadMapFromFile(streamingPath);
            return;
        }

        // Если карта не найдена, создаем с генерацией
        Debug.LogWarning($"Предсозданная карта '{mapName}' не найдена в:\n" +
            $"- {persistentPath}\n" +
            $"- {streamingPath}\n" +
            "Создаю карту с генерацией.");
        CreateGeneratedMap();
    }

    /// <summary>
    /// Загружает карту из файла.
    /// </summary>
    private void LoadMapFromFile(string path)
    {
        try
        {
            using (var reader = new BinaryReader(File.OpenRead(path)))
            {
                int header = reader.ReadInt32();
                hexGrid.Load(reader, header);
                Debug.Log($"Загружена предсозданная карта из файла: {path}");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Ошибка при загрузке карты из {path}: {e.Message}");
            CreateGeneratedMap();
        }
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
            BattleHexGrid battleGrid = hexGrid as BattleHexGrid;
            if (battleGrid != null)
            {
                battleGrid.FogOfWarEnabled = battleConfig.enableFogOfWar;
            }
            
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
        // Проходим по всем клеткам и помечаем их как исследованные и видимые
        for (int i = 0; i < hexGrid.CellCountX * hexGrid.CellCountZ; i++)
        {
            // Получаем клетку
            HexCell cell = hexGrid.GetCell(i);

            // Устанавливаем флаги: Explorable + Explored
            cell.Flags = cell.Flags.With(HexFlags.Explorable | HexFlags.Explored);

            // Устанавливаем видимость в шейдере напрямую
            // Все клетки видимы и исследованы, когда туман войны отключен
            hexGrid.ShaderData.SetCellVisibility(i, true, true);
        }

        // Принудительно обновляем все чанки
        for (int i = 0; i < hexGrid.CellCountX * hexGrid.CellCountZ; i++)
        {
            hexGrid.RefreshCell(i);
        }
        
        // Применяем изменения в текстуре
        hexGrid.ShaderData.enabled = true;
    }

    private void SpawnTestUnits()
    {
        // Получаем стартовые позиции
        List<HexCell> player1StartPositions = GetPlayer1StartPositions();
        List<HexCell> player2StartPositions = GetPlayer2StartPositions();

        // Спавним юнитов игрока 1 из BattleConfig
        for (int i = 0; i < Mathf.Min(battleConfig.player1Units.Count, player1StartPositions.Count); i++)
        {
            SpawnUnitAt(battleConfig.player1Units[i], player1StartPositions[i], true);
        }

        // Спавним юнитов игрока 2 из BattleConfig
        for (int i = 0; i < Mathf.Min(battleConfig.player2Units.Count, player2StartPositions.Count); i++)
        {
            SpawnUnitAt(battleConfig.player2Units[i], player2StartPositions[i], false);
        }
    }


    private void SpawnUnitAt(UnitData unitData, HexCell cell, bool isPlayerUnit)
    {
        if (cell == null || unitData == null)
        {
            Debug.LogError("Невалидные данные для создания юнита!");
            return;
        }

        if (cell.Unit != null)
        {
            Debug.LogWarning($"Клетка {cell.Position} уже занята");
            return;
        }

        // Используем БАЗОВЫЙ префаб (без специфичных настроек)
        GameObject unitPrefab = baseUnitPrefab; // Общий префаб для всех юнитов
        
        GameObject unitInstance = Instantiate(unitPrefab);
        BattleHexUnit battleUnit = unitInstance.GetComponent<BattleHexUnit>();
        
        // Инициализируем из UnitData
        battleUnit.InitializeFromUnitData(unitData);
        
        // Размещаем на карте
        hexGrid.AddUnit(battleUnit, cell, Random.Range(0f, 360f));
        
        unitInstance.name = $"{unitData.unitName} {(isPlayerUnit ? "(Player)" : "(Enemy)")}";
        unitInstance.tag = isPlayerUnit ? "PlayerUnit" : "EnemyUnit";
    }


    private List<HexCell> GetPlayer1StartPositions()
    {
        List<HexCell> positions = new List<HexCell>();
        int width = battleConfig.GetGridWidth();
        int height = battleConfig.GetGridHeight();

        // Используем offset координаты напрямую через GetCellIndex
        for (int z = 0; z < height && z < hexGrid.CellCountZ; z++)
        {
            for (int x = 0; x < 2 && x < hexGrid.CellCountX; x++) // Берем только первые 2 колонки
            {
                // Используем прямой способ получения ячейки через offset координаты
                int cellIndex = hexGrid.GetCellIndex(x, z);
                HexCell cell = hexGrid.GetCell(cellIndex);

                if (IsCellValid(cell) && IsCellSuitableForSpawn(cell))
                {
                    positions.Add(cell);
                    if (positions.Count >= battleConfig.player1UnitsCount)
                        return positions;
                }
            }
        }

        return positions;
    }

    private List<HexCell> GetPlayer2StartPositions()
    {
        List<HexCell> positions = new List<HexCell>();
        int width = battleConfig.GetGridWidth();
        int height = battleConfig.GetGridHeight();

        // Используем offset координаты напрямую через GetCellIndex
        // Идем сверху вниз (z от height-1 до 0), чтобы спавнить в правом верхнем углу
        for (int z = Mathf.Min(height - 1, hexGrid.CellCountZ - 1); z >= 0; z--)
        {
            int startX = Mathf.Max(0, Mathf.Min(width - 2, hexGrid.CellCountX - 2));
            int endX = Mathf.Min(width - 1, hexGrid.CellCountX - 1);
            
            for (int x = startX; x <= endX; x++) // Берем последние 2 колонки
            {
                // Используем прямой способ получения ячейки через offset координаты
                int cellIndex = hexGrid.GetCellIndex(x, z);
                HexCell cell = hexGrid.GetCell(cellIndex);

                if (IsCellValid(cell) && IsCellSuitableForSpawn(cell))
                {
                    positions.Add(cell);
                    if (positions.Count >= battleConfig.player2UnitsCount)
                        return positions;
                }
            }
        }

        return positions;
    }

    private bool IsCellValid(HexCell cell)
    {
        // Проверяем, что ячейка валидна (не default структура)
        // В структуре HexCell нет явного способа проверить валидность,
        // но мы можем проверить индекс
        int totalCells = hexGrid.CellCountX * hexGrid.CellCountZ;
        return cell.Index >= 0 && cell.Index < totalCells;
    }

    private bool IsCellSuitableForSpawn(HexCell cell)
    {
        if (!IsCellValid(cell)) return false;

        // Проверяем что ячейка не под водой (если в туториале есть вода)
        // if (cell.IsUnderwater) return false;

        // Проверяем что ячейка не слишком крутая (если в туториале есть склоны)
        // if (cell.HasCliff) return false;

        // Проверяем высоту (если в туториале есть возвышенности)
        // if (cell.Elevation > 3) return false; // Слишком высоко

        return true;
    }
}

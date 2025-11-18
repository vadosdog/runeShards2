using UnityEngine;

/// <summary>
/// Упрощенный генератор рельефа для боевых карт.
/// Создает базовый рельеф с разными высотами без сложных систем (реки, климат и т.д.).
/// </summary>
public static class BattleMapGenerator
{
    // Параметры по умолчанию
    private const int DefaultMinElevation = 0;
    private const int DefaultMaxElevation = 3;
    private const float DefaultElevationProbability = 0.3f;
    private const float DefaultSmoothness = 0.5f;
    private const float DefaultNoiseScale = 0.1f;

    /// <summary>
    /// Генерирует рельеф для боевой карты.
    /// </summary>
    /// <param name="grid">Сетка для генерации.</param>
    /// <param name="minElevation">Минимальная высота.</param>
    /// <param name="maxElevation">Максимальная высота.</param>
    /// <param name="elevationProbability">Вероятность создания возвышенностей (0-1).</param>
    /// <param name="smoothness">Степень сглаживания (0-1).</param>
    /// <param name="noiseScale">Масштаб шума для генерации.</param>
    /// <param name="seed">Seed для генерации. Если -1, используется случайный seed.</param>
    public static void GenerateTerrain(
        BattleHexGrid grid,
        int minElevation = DefaultMinElevation,
        int maxElevation = DefaultMaxElevation,
        float elevationProbability = DefaultElevationProbability,
        float smoothness = DefaultSmoothness,
        float noiseScale = DefaultNoiseScale,
        int seed = -1)
    {
        if (grid == null)
        {
            Debug.LogError("BattleHexGrid is null!");
            return;
        }

        // Сохраняем состояние Random
        Random.State originalRandomState = Random.state;

        // Устанавливаем seed
        if (seed == -1)
        {
            seed = Random.Range(0, int.MaxValue);
        }
        Random.InitState(seed);

        // Генерируем базовый рельеф
        GenerateBaseTerrain(grid, minElevation, maxElevation, elevationProbability, noiseScale);

        // Сглаживаем рельеф
        if (smoothness > 0f)
        {
            SmoothTerrain(grid, minElevation, maxElevation, smoothness);
        }

        // Обновляем позиции клеток
        grid.RefreshAllCells();

        // Восстанавливаем состояние Random
        Random.state = originalRandomState;

        Debug.Log($"Battle terrain generated with seed: {seed}, elevation range: {minElevation}-{maxElevation}");
    }

    /// <summary>
    /// Генерирует базовый рельеф используя простой алгоритм.
    /// </summary>
    private static void GenerateBaseTerrain(
        BattleHexGrid grid,
        int minElevation,
        int maxElevation,
        float elevationProbability,
        float noiseScale)
    {
        int cellCount = grid.CellCountX * grid.CellCountZ;

        // Первый проход: создаем базовые высоты
        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);
            
            // Используем шум для создания естественного рельефа
            Vector3 position = cell.Position;
            float noise = Mathf.PerlinNoise(
                position.x * noiseScale,
                position.z * noiseScale
            );

            // Определяем высоту на основе шума и вероятности
            int elevation = minElevation;
            if (Random.value < elevationProbability)
            {
                // Высота зависит от шума
                float normalizedNoise = (noise - 0.5f) * 2f; // -1 до 1
                float elevationFactor = (normalizedNoise + 1f) * 0.5f; // 0 до 1
                elevation = Mathf.RoundToInt(
                    minElevation + elevationFactor * (maxElevation - minElevation)
                );
                elevation = Mathf.Clamp(elevation, minElevation, maxElevation);
            }

            // Устанавливаем высоту
            HexCellData data = grid.CellData[i];
            data.values = data.values.WithElevation(elevation);
            grid.CellData[i] = data;
        }
    }

    /// <summary>
    /// Сглаживает рельеф для более естественного вида.
    /// </summary>
    private static void SmoothTerrain(
        BattleHexGrid grid,
        int minElevation,
        int maxElevation,
        float smoothness)
    {
        int cellCount = grid.CellCountX * grid.CellCountZ;
        int[] newElevations = new int[cellCount];

        // Проходим по всем клеткам
        for (int i = 0; i < cellCount; i++)
        {
            HexCell cell = grid.GetCell(i);
            int currentElevation = cell.Values.Elevation;
            int sum = currentElevation;
            int count = 1;

            // Считаем среднюю высоту соседей
            for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
            {
                if (cell.TryGetNeighbor(d, out HexCell neighbor))
                {
                    sum += neighbor.Values.Elevation;
                    count++;
                }
            }

            // Вычисляем новую высоту с учетом сглаживания
            float averageElevation = (float)sum / count;
            float smoothedElevation = Mathf.Lerp(
                currentElevation,
                averageElevation,
                smoothness
            );
            newElevations[i] = Mathf.RoundToInt(smoothedElevation);
        }

        // Применяем новые высоты
        for (int i = 0; i < cellCount; i++)
        {
            int newElevation = Mathf.Clamp(newElevations[i], minElevation, maxElevation);
            HexCellData data = grid.CellData[i];
            data.values = data.values.WithElevation(newElevation);
            grid.CellData[i] = data;
        }
    }

    /// <summary>
    /// Генерирует простой рельеф с холмами в центре.
    /// </summary>
    /// <param name="grid">Сетка для генерации.</param>
    /// <param name="minElevation">Минимальная высота.</param>
    /// <param name="maxElevation">Максимальная высота.</param>
    /// <param name="seed">Seed для генерации. Если -1, используется случайный seed.</param>
    public static void GenerateHillsTerrain(
        BattleHexGrid grid,
        int minElevation = DefaultMinElevation,
        int maxElevation = DefaultMaxElevation,
        int seed = -1)
    {
        if (grid == null)
        {
            Debug.LogError("BattleHexGrid is null!");
            return;
        }

        Random.State originalRandomState = Random.state;
        if (seed == -1)
        {
            seed = Random.Range(0, int.MaxValue);
        }
        Random.InitState(seed);

        int centerX = grid.CellCountX / 2;
        int centerZ = grid.CellCountZ / 2;
        float maxDistance = Mathf.Sqrt(
            centerX * centerX + centerZ * centerZ
        );

        for (int i = 0; i < grid.CellCountX * grid.CellCountZ; i++)
        {
            HexCell cell = grid.GetCell(i);
            
            // Расстояние от центра
            float distanceX = (cell.Coordinates.X - centerX);
            float distanceZ = (cell.Coordinates.Z - centerZ);
            float distance = Mathf.Sqrt(distanceX * distanceX + distanceZ * distanceZ);
            
            // Высота уменьшается от центра
            float normalizedDistance = Mathf.Clamp01(distance / maxDistance);
            int elevation = Mathf.RoundToInt(
                maxElevation * (1f - normalizedDistance * normalizedDistance)
            );
            elevation = Mathf.Clamp(elevation, minElevation, maxElevation);

            HexCellData data = grid.CellData[i];
            data.values = data.values.WithElevation(elevation);
            grid.CellData[i] = data;
        }

        grid.RefreshAllCells();
        Random.state = originalRandomState;
    }

    /// <summary>
    /// Генерирует плоский рельеф (для тестирования).
    /// </summary>
    /// <param name="grid">Сетка для генерации.</param>
    /// <param name="elevation">Высота для всех клеток.</param>
    public static void GenerateFlatTerrain(BattleHexGrid grid, int elevation = DefaultMinElevation)
    {
        if (grid == null)
        {
            Debug.LogError("BattleHexGrid is null!");
            return;
        }

        for (int i = 0; i < grid.CellCountX * grid.CellCountZ; i++)
        {
            HexCellData data = grid.CellData[i];
            data.values = data.values.WithElevation(elevation);
            grid.CellData[i] = data;
        }

        grid.RefreshAllCells();
    }
}


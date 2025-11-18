# Генерация карт для битвы

## Текущая ситуация

### Почему Elevation всегда 0?

1. **Создание клеток** (`HexGrid.CreateCell`, строка 405):
   ```csharp
   cell.Values = cell.Values.WithElevation(0);
   ```
   При создании карты все клетки получают Elevation = 0 по умолчанию.

2. **Генерация карты для битвы** (`BattleManager.SetupMap`):
   - Вызывается `hexGrid.CreateMap(width, height, false)`
   - Это создает плоскую карту без генерации рельефа
   - В отличие от обычных карт, где используется `HexMapGenerator.GenerateMap()`, который создает рельеф

3. **Разница между обычными и боевыми картами**:
   - **Обычные карты**: Используют `HexMapGenerator.GenerateMap()` → создает рельеф, реки, климат и т.д.
   - **Боевые карты**: Используют только `HexGrid.CreateMap()` → создает плоскую карту без рельефа

## Как работает генерация карт

### Обычные карты (HexMapGenerator)

1. **Создание базовой карты**: `grid.CreateMap(x, z, wrapping)`
2. **Генерация рельефа**: 
   - `CreateRegions()` - создает регионы
   - `CreateLand()` - создает сушу с разными высотами
   - `ErodeLand()` - эрозия рельефа
   - `CreateClimate()` - климат
   - `CreateRivers()` - реки
   - `SetTerrainType()` - типы местности

3. **Установка высот**: В процессе генерации вызывается:
   ```csharp
   grid.CellData[index].values = current.values.WithElevation(newElevation);
   ```

### Боевые карты (текущая реализация)

1. **Только создание базовой карты**: `hexGrid.CreateMap(width, height, false)`
2. **Нет генерации рельефа** → все клетки остаются с Elevation = 0

## Реализованное решение

Реализован **гибридный подход** с тремя режимами генерации карт:

### 1. BattleMapGenerator

Создан упрощенный статический генератор рельефа (`BattleMapGenerator.cs`):
- **Статический класс** - не требует компонента на сцене, вызывается напрямую
- **Генерация базового рельефа** с использованием Perlin Noise
- **Сглаживание рельефа** для более естественного вида
- **Настраиваемые параметры** (передаются в методы):
  - `minElevation` / `maxElevation` - диапазон высот (по умолчанию 0-3)
  - `elevationProbability` - вероятность создания возвышенностей (по умолчанию 0.3)
  - `smoothness` - степень сглаживания (по умолчанию 0.5)
  - `noiseScale` - масштаб шума (по умолчанию 0.1)
  - `seed` - seed для генерации (-1 для случайного)
- **Специальные режимы**:
  - `GenerateTerrain()` - стандартная генерация с параметрами
  - `GenerateHillsTerrain()` - холмы в центре карты
  - `GenerateFlatTerrain()` - плоская карта

### 2. BattleConfig - режимы генерации

Добавлен `MapGenerationMode` в `BattleConfig`:
- **Generated** - автоматическая генерация рельефа (по умолчанию)
- **Prebuilt** - загрузка предсозданной карты
- **Flat** - плоская карта без рельефа

### 3. Загрузка предсозданных карт

`BattleManager` поддерживает загрузку карт из:
- `Application.persistentDataPath` - сохраненные пользователем карты
- `Application.streamingAssetsPath/BattleMaps/` - предсозданные карты в сборке

### Как использовать

#### Генерация рельефа (по умолчанию):
1. В `BattleConfig` установить `mapGenerationMode = Generated`
2. При запуске битвы карта будет сгенерирована автоматически через `BattleMapGenerator` (статический класс)
3. Параметры генерации можно настроить, передав их в `BattleMapGenerator.GenerateTerrain()` при необходимости

#### Загрузка предсозданной карты:
1. Создать карту в редакторе (используя `HexMapEditor` или `HexMapGenerator`)
2. Сохранить через `SaveLoadMenu.Save()` в `Application.persistentDataPath`
3. Или поместить файл `.map` в `StreamingAssets/BattleMaps/`
4. В `BattleConfig` установить:
   - `mapGenerationMode = Prebuilt`
   - `prebuiltMapName = "имя_карты"` (без расширения .map)

#### Плоская карта:
1. В `BattleConfig` установить `mapGenerationMode = Flat`

### Создание предсозданных карт

1. **В редакторе Unity**:
   - Использовать сцену с `HexMapEditor`
   - Создать карту вручную или через `HexMapGenerator`
   - Сохранить через меню Save/Load

2. **Программно**:
   ```csharp
   // Создать карту
   hexGrid.CreateMap(15, 15, false);
   // Настроить рельеф...
   // Сохранить
   using (var writer = new BinaryWriter(File.Open(path, FileMode.Create)))
   {
       writer.Write(5); // версия файла
       hexGrid.Save(writer);
   }
   ```

3. **Структура файлов**:
   ```
   StreamingAssets/
     BattleMaps/
       - arena_01.map
       - hills_01.map
       - valley_01.map
   ```

### Важные замечания

- **BattleHexGrid** использует удвоенный шаг высоты (`GetElevationStep() * 2`)
- При загрузке предсозданных карт размер карты определяется из файла
- Если предсозданная карта не найдена, автоматически создается карта с генерацией


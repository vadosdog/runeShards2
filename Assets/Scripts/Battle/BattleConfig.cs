using UnityEngine;
using System.Collections.Generic;

// Перечисление для размеров карты
public enum MapSize
{
    DebugSmall,   // 5x5
    Small,   // 10x10
    Medium,  // 15x15
    Large    // 20x20
}

// Режим генерации карты
public enum MapGenerationMode
{
    Generated,   // Генерировать рельеф автоматически
    Prebuilt,   // Загрузить предсозданную карту
    Flat        // Плоская карта без рельефа
}

// Тип управления командой
public enum ControlType
{
    Human,      // Управление человеком
    Computer    // Управление компьютером (ИИ)
}

// Режим победы в битве
public enum VictoryCondition
{
    TotalAnnihilation  // Полное уничтожение - команда побеждает, если уничтожит всех противников
}

// Класс для хранения конфигурации битвы
[System.Serializable]
public class BattleConfig
{
    public MapSize mapSize = MapSize.DebugSmall;
    public List<UnitData> player1Units = new List<UnitData>();
    public List<UnitData> player2Units = new List<UnitData>();

    // Тип управления для каждой команды
    public ControlType player1ControlType = ControlType.Human;
    public ControlType player2ControlType = ControlType.Human;

    [Header("Victory Conditions")]
    public VictoryCondition victoryCondition = VictoryCondition.TotalAnnihilation;

    public bool enableFogOfWar = false;
    public bool enableVisibility = false;

    [Header("Map Generation")]
    public MapGenerationMode mapGenerationMode = MapGenerationMode.Generated;
    
    [Tooltip("Имя предсозданной карты (если используется режим Prebuilt). Карта должна быть в Resources/BattleMaps/")]
    public string prebuiltMapName = "";

    public int player1UnitsCount => player1Units.Count;
    public int player2UnitsCount => player2Units.Count;

    // Добавим методы для получения реальных размеров
    public int GetGridWidth()
    {
        return mapSize switch
        {
            MapSize.DebugSmall => 5,
            MapSize.Small => 10,
            MapSize.Medium => 15,
            MapSize.Large => 20,
            _ => 15
        };
    }

    public int GetGridHeight()
    {
        return mapSize switch
        {
            MapSize.DebugSmall => 5,
            MapSize.Small => 10,
            MapSize.Medium => 15,
            MapSize.Large => 20,
            _ => 15
        };
    }

    // Можно добавить удобные свойства
    public Vector2Int GridSize => new Vector2Int(GetGridWidth(), GetGridHeight());
}

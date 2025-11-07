using UnityEngine;

// Перечисление для размеров карты
public enum MapSize
{
    Small,   // 10x10
    Medium,  // 15x15
    Large    // 20x20
}

// Класс для хранения конфигурации битвы
[System.Serializable]
public class BattleConfig
{
    public MapSize mapSize = MapSize.Small;
    public int playerUnitsCount = 1;
    public int enemyUnitsCount = 1;

    public bool enableFogOfWar = false;
    public bool enableVisibility = false;

    // Добавим методы для получения реальных размеров
    public int GetGridWidth()
    {
        return mapSize switch
        {
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
            MapSize.Small => 10,
            MapSize.Medium => 15,
            MapSize.Large => 20,
            _ => 15
        };
    }

    // Можно добавить удобные свойства
    public Vector2Int GridSize => new Vector2Int(GetGridWidth(), GetGridHeight());
}

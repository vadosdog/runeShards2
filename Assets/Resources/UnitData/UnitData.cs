using UnityEngine;

[CreateAssetMenu(fileName = "New Unit", menuName = "Battle/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Basic Info")]
    public string unitName;
    public UnitType type;
    public GameObject battlePrefab; // Префаб "ожившей карточки"

    [Header("Stats")]
    public int maxHealth = 100;
    public int attackDamage = 10;
    public int defense = 5;
    public int speed = 10; // Для определения порядка хода
    public int movementRange = 3;
    public int attackRange = 1;

    [Header("Visual Effects")]
    public GameObject hitEffect; // Эффект при получении урона
    public GameObject deathEffect; // Эффект при смерти
}

public enum UnitType
{
    Tank,      // Воин - много здоровья, малая дальность
    Archer,    // Лучник - среднее здоровье, дальняя атака
    Healer     // Маг - мало здоровья, лечение
}

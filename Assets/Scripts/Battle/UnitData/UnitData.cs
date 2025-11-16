using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Unit", menuName = "Battle/Unit Data")]
public class UnitData : ScriptableObject
{
    [Header("Basic Info")]
    public string unitName;
    public EmotionType emotionType;
    public ElementType elementType;
    public ShapeType shapeType;
    public GameObject battlePrefab; // Префаб "ожившей карточки"

    [Header("Stats")]
    public int maxHealth = 100;
    public int maxStamina = 10;
    public int PhyAtk = 1;
    public int MagAtk = 1;
    public int PhyDef = 1;
    public int MagDef = 1;

    [Header("Skills")]
    public List<AbstractBattleSkill> Skills;

    [Header("Visual Effects")]
    public GameObject hitEffect; // Эффект при получении урона
    public GameObject deathEffect; // Эффект при смерти
}

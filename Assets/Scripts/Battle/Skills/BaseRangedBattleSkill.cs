using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Battle Skills/Base Ranged Attack")]
public class BaseRangedBattleSkill : AbstractBattleSkill
{
    // Список подсвеченных ячеек для очистки
    private HashSet<int> highlightedCells = new HashSet<int>();
    // Список ячеек с метками для очистки
    private HashSet<int> labeledCells = new HashSet<int>();
    // Ссылка на grid для очистки
    private BattleHexGrid currentGrid;
    private void Reset()
    {
        // Инициализируем значения по умолчанию при создании в редакторе
        skillName = "Base Ranged Attack";
        staminaCost = 3;
        cooldown = 0;
        targetType = SkillTargetType.EnemyUnit;
        minRange = 2;
        maxRange = 5;
        requiresLineOfSight = true;
        
        // Пользователь должен самостоятельно настроить зоны (zones) через Unity редактор
        // Добавьте нужные паттерны и эффекты через Inspector
    }

    private void OnEnable()
    {
        // Убеждаемся, что базовые значения инициализированы при загрузке
        if (string.IsNullOrEmpty(skillName))
        {
            skillName = "Base Ranged Attack";
        }
    }

    public override bool IsValidTarget(HexCell targetCell, BattleHexUnit caster)
    {
        if (targetCell == null || caster == null)
        {
            return false;
        }

        // Проверяем, нет ли противника в соседней клетке с разницей высот <= 1
        // Дальняя атака невозможна в этом случае
        if (HasAdjacentEnemyWithLowHeightDiff(caster))
        {
            return false;
        }

        // Получаем высоты для расчета модификатора дальности
        int casterElevation = caster.Location.Values.Elevation;
        int targetElevation = targetCell.Values.Elevation;
        int elevationDiff = casterElevation - targetElevation;

        // Вычисляем модифицированную дальность
        // +1 к дальности, если атакующий выше цели (согласно GDD)
        // -1 к дальности, если атакующий ниже цели
        int rangeModifier = 0;
        if (elevationDiff > 0)
        {
            rangeModifier = +1; // Атакующий выше цели - +1 к дальности
        }
        else if (elevationDiff < 0)
        {
            rangeModifier = -1; // Атакующий ниже цели - -1 к дальности
        }

        // Проверяем расстояние с учетом модификатора высоты
        int distance = caster.Location.Coordinates.DistanceTo(targetCell.Coordinates);
        int modifiedMinRange = minRange;
        int modifiedMaxRange = maxRange + rangeModifier;
        
        // Убеждаемся, что модифицированная дальность не меньше минимальной
        if (modifiedMaxRange < modifiedMinRange)
        {
            modifiedMaxRange = modifiedMinRange;
        }
        
        if (distance < modifiedMinRange || distance > modifiedMaxRange)
        {
            return false;
        }

        // Проверяем тип цели
        HexUnit targetUnit = targetCell.Unit;
        if (targetUnit == null)
        {
            return targetType == SkillTargetType.EmptyCell || targetType == SkillTargetType.AnyCell;
        }

        BattleHexUnit battleTarget = targetUnit as BattleHexUnit;
        if (battleTarget == null)
        {
            return false;
        }

        // Проверяем, является ли цель врагом
        bool isEnemy = !caster.CompareTag(targetUnit.tag);
        
        switch (targetType)
        {
            case SkillTargetType.EnemyUnit:
                if (!isEnemy) return false;
                break;
            case SkillTargetType.AllyUnit:
                if (isEnemy) return false;
                break;
            case SkillTargetType.AnyUnit:
                break;
            default:
                return false;
        }

        // Проверяем укрытие: укрытие блокирует атаку
        if (requiresLineOfSight)
        {
            BattleHexGrid battleGrid = caster.Grid as BattleHexGrid;
            if (battleGrid != null)
            {
                bool hasCover = battleGrid.HasCover(caster.Location, targetCell);
                if (hasCover)
                {
                    return false; // Укрытие блокирует атаку
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Проверяет, есть ли противник в соседней клетке с разницей высот <= 1.
    /// Дальняя атака невозможна в этом случае.
    /// </summary>
    private bool HasAdjacentEnemyWithLowHeightDiff(BattleHexUnit caster)
    {
        HexCell casterCell = caster.Location;
        int casterElevation = casterCell.Values.Elevation;

        // Проверяем все 6 соседних клеток
        for (HexDirection d = HexDirection.NE; d <= HexDirection.NW; d++)
        {
            if (casterCell.TryGetNeighbor(d, out HexCell neighborCell))
            {
                HexUnit neighborUnit = neighborCell.Unit;
                
                // Если в соседней клетке есть юнит
                if (neighborUnit != null)
                {
                    // Проверяем, является ли он противником
                    bool isEnemy = !caster.CompareTag(neighborUnit.tag);
                    
                    if (isEnemy)
                    {
                        // Проверяем разницу высот
                        int neighborElevation = neighborCell.Values.Elevation;
                        int elevationDiff = Mathf.Abs(casterElevation - neighborElevation);
                        
                        // Если разница высот <= 1, дальняя атака невозможна
                        if (elevationDiff <= 1)
                        {
                            return true;
                        }
                    }
                }
            }
        }

        return false;
    }


    public override void ShowTargetingPreview(HexCell targetCell, BattleHexUnit caster)
    {
        if (targetCell == null || caster == null || !requiresLineOfSight)
            return;

        // Очищаем предыдущую подсветку
        HideTargetingPreview();

        BattleHexGrid battleGrid = caster.Grid as BattleHexGrid;
        if (battleGrid == null)
            return;

        currentGrid = battleGrid;
        HexCell fromCell = caster.Location;

        // Траектория уже вычислена в BattleUI.DoFindTrajectory
        // Получаем ячейки на траектории из grid (fromCell и toCell уже исключены)
        List<HexCell> lineCells = battleGrid.GetTrajectory();
        if (lineCells == null || lineCells.Count == 0)
        {
            return;
        }

        // Подсвечиваем все ячейки на пути серым цветом
        foreach (HexCell cell in lineCells)
        {
            battleGrid.HighlightCell(cell.Index, Color.gray);
            highlightedCells.Add(cell.Index);
        }

        // Получаем первое препятствие из grid (проверка на null уже внутри GetFirstObstacle)
        HexCell firstObstacle = battleGrid.GetFirstObstacle();

        // Если нашли препятствие, проверяем, блокирует ли оно атаку
        if (firstObstacle != null)
        {
            // Проверяем, действительно ли препятствие блокирует атаку (создает укрытие)
            bool hasCover = battleGrid.HasCover(fromCell, targetCell);
            
            if (hasCover)
            {
                // Препятствие блокирует атаку - подсвечиваем черным с символом X
                // Убираем серую подсветку с препятствия, если она была
                if (highlightedCells.Contains(firstObstacle.Index))
                {
                    highlightedCells.Remove(firstObstacle.Index);
                    battleGrid.DisableHighlight(firstObstacle.Index);
                }

                // Подсвечиваем черным
                battleGrid.HighlightCell(firstObstacle.Index, Color.black);
                highlightedCells.Add(firstObstacle.Index);

                // Устанавливаем метку X
                battleGrid.SetCellLabel(firstObstacle.Index, "X");
                labeledCells.Add(firstObstacle.Index);
            }
            // Если препятствие не блокирует атаку, оно остается серым (уже подсвечено выше)
        }
    }

    public override void HideTargetingPreview()
    {
        if (currentGrid == null)
            return;

        // Убираем подсветку со всех ячеек
        foreach (int cellIndex in highlightedCells)
        {
            currentGrid.DisableHighlight(cellIndex);
        }

        // Очищаем метки
        foreach (int cellIndex in labeledCells)
        {
            currentGrid.ClearCellLabel(cellIndex);
        }

        // Очищаем траекторию в grid (аналогично ClearPath)
        currentGrid.ClearTrajectory();
        
        // Очищаем списки
        highlightedCells.Clear();
        labeledCells.Clear();
        currentGrid = null;
    }

    public override SkillResult Execute(HexCell targetCell, BattleHexUnit caster)
    {
        SkillResult result = new SkillResult
        {
            success = false,
            appliedEffects = new List<SkillEffectApplication>()
        };

        if (!IsValidTarget(targetCell, caster))
        {
            Debug.LogWarning($"Недопустимая цель для навыка {skillName}");
            return result;
        }

        // Проверяем стоимость стамины
        if (caster.currentStamina < staminaCost)
        {
            Debug.LogWarning($"Недостаточно стамины для использования {skillName}");
            return result;
        }

        // Проверяем наличие зон
        if (zones == null || zones.Count == 0)
        {
            Debug.LogWarning($"У навыка {skillName} нет настроенных зон");
            return result;
        }

        // Применяем эффекты из всех зон
        foreach (var zone in zones)
        {
            if (zone.pattern == null || zone.effects == null)
                continue;

            // Получаем затронутые ячейки по паттерну
            List<HexCell> affectedCells = zone.pattern.GetAffectedCells(targetCell, caster);

            foreach (var cell in affectedCells)
            {
                if (cell == null)
                    continue;

                // Находим юнита на ячейке
                HexUnit unitOnCell = cell.Unit;
                BattleHexUnit targetUnit = unitOnCell as BattleHexUnit;

                // Применяем эффекты
                foreach (var effect in zone.effects)
                {
                    if (effect == null)
                        continue;

                    int damageDealt = 0;

                    // Если эффект наносит урон, вычисляем его
                    if (effect is DealDamageEffect && targetUnit != null)
                    {
                        var damageResult = BattleCalculator.CalculateDamage(
                            effect, caster, targetUnit, new BattleContext());
                        damageDealt = damageResult.damageAmount;
                    }

                    // Применяем эффект
                    effect.Apply(caster, targetUnit, cell);

                    // Записываем результат применения
                    result.appliedEffects.Add(new SkillEffectApplication
                    {
                        effect = effect,
                        target = targetUnit,
                        cell = cell,
                        damageDealt = damageDealt
                    });
                }
            }
        }

        // Тратим стамину
        caster.ConsumeStamina(staminaCost);

        result.success = true;
        Debug.Log($"{caster.name} использовал {skillName} на {targetCell.Coordinates}");
        
        return result;
    }
}


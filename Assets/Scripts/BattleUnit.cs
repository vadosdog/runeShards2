using UnityEngine;

public class BattleUnit : MonoBehaviour
{
    [Header("References")]
    public HexCell currentCell;

    [Header("Current State")]
    public int currentHealth = 100;
    public int currentActionPoints = 2;
    public int maxActionPoints = 2;

    [Header("Unit Stats")]
    public int movementRange = 3;
    public int attackRange = 1;
    public int attackDamage = 10;

    // Событие для обновления UI
    public System.Action<BattleUnit> OnUnitStateChanged;

    public void MoveTo(HexCell targetCell)
    {
        if (currentCell != null)
        {
            // Освобождаем текущую ячейку
            // currentCell.Unit = null;
        }

        currentCell = targetCell;
        transform.position = targetCell.Position + Vector3.up * 1f;

        // Занимаем новую ячейку
        // targetCell.Unit = this;

        // Уведомляем об изменении состояния
        OnUnitStateChanged?.Invoke(this);
    }

    public void PerformAttack(BattleUnit target)
    {
        target.TakeDamage(attackDamage);
        OnUnitStateChanged?.Invoke(this);
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Max(0, currentHealth);

        // Визуальный эффект получения урона
        StartCoroutine(DamageEffect());

        OnUnitStateChanged?.Invoke(this);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private System.Collections.IEnumerator DamageEffect()
    {
        Renderer renderer = GetComponent<Renderer>();
        Color originalColor = renderer.material.color;
        renderer.material.color = Color.white;

        yield return new WaitForSeconds(0.2f);

        if (gameObject.name.Contains("Player"))
            renderer.material.color = Color.blue;
        else
            renderer.material.color = Color.red;
    }

    private void Die()
    {
        Debug.Log($"{gameObject.name} погиб!");
        // TODO: Анимация смерти

        // Освобождаем ячейку
        if (currentCell != null)
        {
            // currentCell.Unit = null;
        }

        Destroy(gameObject);
    }

    public void ResetActionPoints()
    {
        currentActionPoints = maxActionPoints;
        OnUnitStateChanged?.Invoke(this);
    }

}

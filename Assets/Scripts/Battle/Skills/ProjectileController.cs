using UnityEngine;
using System.Collections;

/// <summary>
/// Компонент для управления полетом снаряда от кастера к цели
/// </summary>
public class ProjectileController : MonoBehaviour
{
    /// <summary>
    /// Запускает полет снаряда от стартовой позиции к целевой за указанное время
    /// </summary>
    /// <param name="startPos">Стартовая позиция</param>
    /// <param name="targetPos">Целевая позиция</param>
    /// <param name="duration">Длительность полета в секундах</param>
    public IEnumerator FlyToTarget(Vector3 startPos, Vector3 targetPos, float duration)
    {
        if (duration <= 0)
        {
            transform.position = targetPos;
            yield break;
        }

        float elapsedTime = 0f;
        Vector3 direction = (targetPos - startPos).normalized;

        // Ориентируем снаряд в направлении полета
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }

        // Анимация полета
        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / duration;

            // Используем линейную интерполяцию для плавного движения
            transform.position = Vector3.Lerp(startPos, targetPos, t);

            yield return null;
        }

        // Убеждаемся, что снаряд достиг цели
        transform.position = targetPos;
    }
}


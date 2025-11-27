using UnityEngine;
using System.Collections;

/// <summary>
/// Компонент для управления визуальным эффектом навыка на цели
/// </summary>
public class SkillEffectController : MonoBehaviour
{
    private ParticleSystem[] particleSystems;
    private bool isPlaying = false;

    private void Awake()
    {
        // Находим все ParticleSystem в префабе (включая дочерние объекты)
        particleSystems = GetComponentsInChildren<ParticleSystem>();
    }

    /// <summary>
    /// Запускает эффект без ограничения по времени
    /// Эффект будет играть до тех пор, пока ParticleSystem не завершится сам (если loop=false)
    /// или пока не будет вызван StopEffect()
    /// </summary>
    public void PlayEffect()
    {
        if (isPlaying)
            return;

        isPlaying = true;

        // Проверяем, что ParticleSystem найдены
        if (particleSystems == null || particleSystems.Length == 0)
        {
            // Пытаемся найти еще раз
            particleSystems = GetComponentsInChildren<ParticleSystem>(true); // включая неактивные
        }

        // Запускаем все ParticleSystem
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                // Убеждаемся, что объект активен
                if (!ps.gameObject.activeSelf)
                {
                    ps.gameObject.SetActive(true);
                }
                
                // Останавливаем, если уже играет
                if (ps.isPlaying)
                {
                    ps.Stop();
                    ps.Clear();
                }
                
                ps.Play(true); // true = включая дочерние системы
            }
        }
    }

    /// <summary>
    /// Запускает эффект на указанную длительность (устаревший метод, оставлен для совместимости)
    /// </summary>
    /// <param name="duration">Длительность эффекта в секундах</param>
    [System.Obsolete("Используйте PlayEffect() без параметров для эффектов без ограничения по времени")]
    public void PlayEffect(float duration)
    {
        PlayEffect();
        // Автоматически останавливаем эффект через указанное время
        StartCoroutine(StopEffectAfterDelay(duration));
    }

    /// <summary>
    /// Останавливает эффект через указанное время
    /// </summary>
    private IEnumerator StopEffectAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Проверяем, что ParticleSystem найдены перед остановкой
        if (particleSystems == null)
        {
            isPlaying = false;
            yield break;
        }

        // Останавливаем все ParticleSystem
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                ps.Stop();
            }
        }

        isPlaying = false;
    }

    /// <summary>
    /// Останавливает эффект немедленно
    /// </summary>
    public void StopEffect()
    {
        foreach (var ps in particleSystems)
        {
            if (ps != null)
            {
                ps.Stop();
            }
        }

        isPlaying = false;
    }
}


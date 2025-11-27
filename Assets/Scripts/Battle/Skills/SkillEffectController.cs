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
    /// Запускает эффект на указанную длительность
    /// </summary>
    /// <param name="duration">Длительность эффекта в секундах</param>
    public void PlayEffect(float duration)
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


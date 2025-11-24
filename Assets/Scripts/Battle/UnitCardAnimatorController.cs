using UnityEngine;

/// <summary>
/// Контроллер для управления анимациями карточки юнита
/// Управляет параметрами Animator для переключения между состояниями: Idle, Move, Attack, Hurt
/// </summary>
[RequireComponent(typeof(Animator))]
public class UnitCardAnimatorController : MonoBehaviour
{
    private Animator animator;
    
    // Имена параметров в Animator Controller
    private const string PARAM_IS_MOVING = "IsMoving";
    private const string PARAM_IS_ATTACKING = "IsAttacking";
    private const string PARAM_IS_HURT = "IsHurt";
    private const string PARAM_IS_DEAD = "IsDead";
    private const string PARAM_STATE = "State";
    
    // Состояния анимации
    public enum CardAnimationState
    {
        Idle = 0,
        Move = 1,
        Attack = 2,
        Hurt = 3,
        Dead = 4
    }
    
    private CardAnimationState currentState = CardAnimationState.Idle;
    
    void Awake()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("UnitCardAnimatorController: Animator component not found!");
        }
    }
    
    /// <summary>
    /// Устанавливает состояние анимации карточки
    /// </summary>
    public void SetAnimationState(CardAnimationState state)
    {
        if (animator == null)
        {
            Debug.LogWarning("UnitCardAnimatorController: Animator is null, cannot set animation state");
            return;
        }
        
        if (!animator.enabled)
        {
            Debug.LogWarning($"UnitCardAnimatorController: Animator is disabled for state {state}, enabling it...");
            animator.enabled = true;
        }
            
        currentState = state;
        
        // Устанавливаем параметры в зависимости от состояния
        switch (state)
        {
            case CardAnimationState.Idle:
                // Сначала устанавливаем все bool параметры в false
                animator.SetBool(PARAM_IS_MOVING, false);
                animator.SetBool(PARAM_IS_ATTACKING, false);
                animator.SetBool(PARAM_IS_HURT, false);
                animator.SetBool(PARAM_IS_DEAD, false);
                // Затем устанавливаем State
                animator.SetInteger(PARAM_STATE, (int)state);
                // Принудительно обновляем аниматор для немедленного применения изменений
                animator.Update(0f);
                Debug.Log($"Установлено состояние Idle на {gameObject.name}");
                break;
                
            case CardAnimationState.Move:
                // Сначала устанавливаем все bool параметры в false
                animator.SetBool(PARAM_IS_ATTACKING, false);
                animator.SetBool(PARAM_IS_HURT, false);
                animator.SetBool(PARAM_IS_DEAD, false);
                // Затем устанавливаем Move в true
                animator.SetBool(PARAM_IS_MOVING, true);
                // И наконец устанавливаем State
                animator.SetInteger(PARAM_STATE, (int)state);
                // Принудительно обновляем аниматор для немедленного применения изменений
                animator.Update(0f);
                Debug.Log($"Установлено состояние Move на {gameObject.name}");
                break;
                
            case CardAnimationState.Attack:
                animator.SetBool(PARAM_IS_ATTACKING, true);
                animator.SetBool(PARAM_IS_MOVING, false);
                animator.SetBool(PARAM_IS_HURT, false);
                animator.SetBool(PARAM_IS_DEAD, false);
                animator.SetInteger(PARAM_STATE, (int)state);
                Debug.Log($"Установлено состояние Attack на {gameObject.name}");
                break;
                
            case CardAnimationState.Hurt:
                // Сначала устанавливаем все bool параметры в false
                animator.SetBool(PARAM_IS_MOVING, false);
                animator.SetBool(PARAM_IS_ATTACKING, false);
                animator.SetBool(PARAM_IS_DEAD, false);
                // Затем устанавливаем Hurt в true
                animator.SetBool(PARAM_IS_HURT, true);
                // И наконец устанавливаем State
                animator.SetInteger(PARAM_STATE, (int)state);
                // Принудительно обновляем аниматор для немедленного применения изменений
                animator.Update(0f);
                Debug.Log($"Установлено состояние Hurt на {gameObject.name}");
                break;
                
            case CardAnimationState.Dead:
                // Сначала устанавливаем все bool параметры в false
                animator.SetBool(PARAM_IS_MOVING, false);
                animator.SetBool(PARAM_IS_ATTACKING, false);
                animator.SetBool(PARAM_IS_HURT, false);
                // Затем устанавливаем Dead в true
                animator.SetBool(PARAM_IS_DEAD, true);
                // И наконец устанавливаем State
                animator.SetInteger(PARAM_STATE, (int)state);
                // Принудительно обновляем аниматор для немедленного применения изменений
                animator.Update(0f);
                Debug.Log($"Установлено состояние Dead на {gameObject.name}");
                break;
        }
    }
    
    /// <summary>
    /// Проигрывает анимацию атаки
    /// </summary>
    public void PlayAttackAnimation()
    {
        SetAnimationState(CardAnimationState.Attack);
    }
    
    /// <summary>
    /// Проигрывает анимацию получения урона
    /// </summary>
    public void PlayHurtAnimation()
    {
        if (animator == null)
        {
            Debug.LogError($"UnitCardAnimatorController: Animator is null on {gameObject.name} in PlayHurtAnimation!");
            return;
        }
        
        // Проверяем, назначен ли Animator Controller
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError($"UnitCardAnimatorController: Animator Controller не назначен на {gameObject.name}!");
            return;
        }
        
        if (!animator.enabled)
        {
            Debug.LogWarning($"UnitCardAnimatorController: Animator отключен на {gameObject.name}, включаю...");
            animator.enabled = true;
        }
        
        Debug.Log($"Устанавливаю состояние Hurt на аниматоре {gameObject.name}. Controller: {animator.runtimeAnimatorController.name}");
        SetAnimationState(CardAnimationState.Hurt);
        
        // Проверяем, что параметры действительно установились
        bool isHurt = animator.GetBool(PARAM_IS_HURT);
        int state = animator.GetInteger(PARAM_STATE);
        Debug.Log($"После установки Hurt - IsHurt: {isHurt}, State: {state}");
    }
    
    /// <summary>
    /// Проигрывает анимацию смерти
    /// </summary>
    public void PlayDeadAnimation()
    {
        if (animator == null)
        {
            Debug.LogError($"UnitCardAnimatorController: Animator is null on {gameObject.name} in PlayDeadAnimation!");
            return;
        }
        
        // Проверяем, назначен ли Animator Controller
        if (animator.runtimeAnimatorController == null)
        {
            Debug.LogError($"UnitCardAnimatorController: Animator Controller не назначен на {gameObject.name}!");
            return;
        }
        
        if (!animator.enabled)
        {
            Debug.LogWarning($"UnitCardAnimatorController: Animator отключен на {gameObject.name}, включаю...");
            animator.enabled = true;
        }
        
        Debug.Log($"Устанавливаю состояние Dead на аниматоре {gameObject.name}. Controller: {animator.runtimeAnimatorController.name}");
        SetAnimationState(CardAnimationState.Dead);
        
        // Проверяем, что параметры действительно установились
        bool isDead = animator.GetBool(PARAM_IS_DEAD);
        int state = animator.GetInteger(PARAM_STATE);
        Debug.Log($"После установки Dead - IsDead: {isDead}, State: {state}");
    }
    
    /// <summary>
    /// Устанавливает анимацию покоя
    /// </summary>
    public void SetIdleAnimation()
    {
        SetAnimationState(CardAnimationState.Idle);
    }
    
    /// <summary>
    /// Устанавливает анимацию движения
    /// </summary>
    public void SetMoveAnimation(bool isMoving)
    {
        if (isMoving)
        {
            SetAnimationState(CardAnimationState.Move);
        }
        else
        {
            SetIdleAnimation();
        }
    }
    
    /// <summary>
    /// Получает текущее состояние анимации
    /// </summary>
    public CardAnimationState GetCurrentState()
    {
        return currentState;
    }
    
    /// <summary>
    /// Отключает аниматор (статичное состояние, без анимаций)
    /// Используется когда юнит неактивен
    /// </summary>
    public void SetStaticState()
    {
        if (animator != null)
        {
            animator.enabled = false;
        }
    }
    
    /// <summary>
    /// Включает аниматор обратно
    /// Используется когда юнит становится активным
    /// </summary>
    public void EnableAnimator()
    {
        if (animator != null)
        {
            animator.enabled = true;
        }
    }
    
    /// <summary>
    /// Проверяет, включен ли аниматор
    /// </summary>
    public bool IsAnimatorEnabled()
    {
        return animator != null && animator.enabled;
    }
}


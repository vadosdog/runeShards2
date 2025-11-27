# Интеграция CardAttackAnimation в UnitCardBase

## Совместимость с Animator

**Важно:** `CardAttackAnimation` и `Animator` (UnitCardBase.controller) **НЕ конфликтуют** и могут работать одновременно!

### Как они работают вместе:

1. **Animator (UnitCardBase.controller)**
   - Управляет **анимациями спрайтов** (Idle, Attack, Hurt, Move, Dead)
   - Работает на уровне **Animation Clip** (ключевые кадры спрайтов)
   - Использует параметры: `IsAttacking`, `IsHurt`, `IsMoving`, `IsDead`, `State`

2. **CardAttackAnimation**
   - Деформирует **геометрию карточки** через **шейдер**
   - Работает на уровне **вершин** (деформация формы)
   - Использует шейдер `Custom/CardAttack` для визуальной деформации

### Результат:
- Animator может проигрывать анимацию спрайта (например, CardAttack.anim)
- Одновременно CardAttackAnimation деформирует карточку в параллелограмм
- Оба эффекта накладываются друг на друга, создавая более выразительную анимацию

---

## Добавление в префаб UnitCardBase

### Вариант 1: Автоматическое добавление (рекомендуется)

Компонент `CardAttackAnimation` **уже автоматически добавляется** при инициализации карточки через `UnitCardRenderer.InitializeCardComponents()`.

Он добавляется к объекту со `SpriteRenderer` (обычно это `CardSprite` внутри `CardPivot`).

### Вариант 2: Ручное добавление в префаб

Если вы хотите добавить компонент напрямую в префаб для отладки:

1. Откройте префаб `UnitCardBase` (Assets/Prefabs/UnitCardBase.prefab)
2. Найдите объект `CardSprite` (он находится внутри `CardPivot`)
3. Выберите `CardSprite` в иерархии
4. В Inspector нажмите "Add Component"
5. Найдите и добавьте `CardAttackAnimation`
6. Настройте параметры:
   - `Max Skew Amount`: 0.3 (максимальный сдвиг)
   - `Forward Duration`: 0.15 (время движения вправо)
   - `Hold Duration`: 0.1 (время застывания)
   - `Return Duration`: 0.2 (время возврата)

---

## Предпросмотр в редакторе (без запуска игры)

### Использование Editor скрипта

1. Выберите объект с компонентом `CardAttackAnimation` в сцене или префабе
2. В Inspector вы увидите секцию "Preview"
3. Нажмите кнопку **"Preview in Editor (No Play Mode)"**
4. Используйте слайдер "Preview Time" для просмотра разных фаз анимации
5. Нажмите "Stop Preview" для остановки

### Что происходит при предпросмотре:

- Создается временный материал с шейдером `Custom/CardAttack`
- Применяется деформация в реальном времени
- Можно просматривать анимацию без запуска игры
- После остановки все возвращается в исходное состояние

---

## Структура префаба UnitCardBase

```
UnitCardBase (корневой объект)
├── Animator (UnitCardBase.controller)
├── UnitCardAnimatorController (скрипт)
├── UnitCardStatusEffects (скрипт)
├── CardPivot
│   └── CardSprite (SpriteRenderer) ← Сюда добавляется CardAttackAnimation
│       └── CardAttackAnimation (скрипт) ← НОВЫЙ компонент
├── HealthBarCanvas
└── Highlight
```

---

## Проверка работы

### В редакторе:
1. Откройте префаб `UnitCardBase`
2. Найдите `CardSprite`
3. Убедитесь, что компонент `CardAttackAnimation` присутствует
4. Используйте кнопку "Preview in Editor" для тестирования

### В игре:
1. Запустите игру
2. Когда юнит атакует, вызывается `UnitCardRenderer.PlayAttackAnimation()`
3. Это автоматически запускает `CardAttackAnimation.PlayAttackAnimation()`
4. Карточка деформируется в параллелограмм

---

## Настройка параметров

### Рекомендуемые значения:

- **Max Skew Amount**: 0.2 - 0.4
  - Меньше = более тонкая деформация
  - Больше = более выраженная деформация

- **Forward Duration**: 0.1 - 0.2 секунды
  - Быстрое движение создает эффект "резкого удара"

- **Hold Duration**: 0.05 - 0.15 секунды
  - Короткая пауза подчеркивает момент удара

- **Return Duration**: 0.15 - 0.3 секунды
  - Плавный возврат выглядит естественно

### Кривые анимации:

- **Forward Curve**: EaseOut для плавного замедления при достижении максимума
- **Return Curve**: EaseInOut для естественного возврата

---

## Отладка

### Если анимация не работает:

1. **Проверьте шейдер:**
   - Убедитесь, что шейдер `Custom/CardAttack` существует
   - Проверьте, что он компилируется без ошибок

2. **Проверьте SpriteRenderer:**
   - Компонент должен быть на том же объекте, что и `SpriteRenderer`
   - Убедитесь, что у спрайта есть текстура

3. **Проверьте логи:**
   - В консоли Unity могут быть предупреждения о проблемах

4. **Проверьте Material:**
   - В режиме Play проверьте, что материал меняется на материал с шейдером атаки

---

## Пример использования в коде

```csharp
// В UnitCardRenderer.PlayAttackAnimation() уже есть интеграция:

public void PlayAttackAnimation()
{
    // Проигрываем анимацию через шейдер (деформация в параллелограмм)
    if (cardAttackAnimation != null)
    {
        cardAttackAnimation.PlayAttackAnimation();
    }
    
    // Также проигрываем стандартную анимацию аниматора (если есть)
    if (animatorController != null)
    {
        animatorController.PlayAttackAnimation();
    }
}
```

---

## Часто задаваемые вопросы

**Q: Нужно ли отключать Animator при использовании CardAttackAnimation?**  
A: Нет! Они работают независимо и дополняют друг друга.

**Q: Можно ли использовать только один из них?**  
A: Да, можно использовать только Animator или только CardAttackAnimation, или оба вместе.

**Q: Влияет ли это на производительность?**  
A: Минимально. Шейдерная деформация очень эффективна. MaterialPropertyBlock позволяет менять параметры без создания новых материалов.

**Q: Работает ли это с Sprite Atlas?**  
A: Да, шейдер поддерживает `CanUseSpriteAtlas="True"`.


# C#: стиль и современный API

## Форматирование
- Allman: `{` на новой строке для классов, методов, `if/for/foreach`.
- Однострочные `if/else` можно без скобок, если это читаемо и однозначно.
- File-scoped namespace (`namespace Content.Server.Test;`).
- Классы `sealed`, если от них не наследуются.
- Между членами класса и логическими блоками ровно **одна** пустая строка.
- Нет пустых строк сразу после `{` и перед `}`.
- Пустая строка внутри метода разделяет блоки разного смысла (инициализация / использование), а не просто «для воздуха».
- `var` когда тип очевиден справа (`new List<int>()`).

## Нейминг
- PascalCase: классы, методы, свойства, публичные поля.
- camelCase: локальные переменные и параметры. Приватные поля: `_camelCase`.
- Интерфейсы: `IName`.
- Имена говорящие: не `lst`, а `activeEffectsList`. Магические числа в `const`/`[DataField]`.

## Современный стиль SS14
| Устарело | Использовать |
|---|---|
| `OnEvent(EntityUid uid, MyComp comp, Ev args)` | `OnEvent(Entity<MyComp> ent, ref Ev args)` |
| `Method(EntityUid uid, MyComp comp)` | `Method(Entity<MyComp> ent)` (в debug проверяется принадлежность компонента) |
| `string` для id прототипа | `ProtoId<T>` (проверка в debug) |
| `[DataField, ViewVariables(VVAccess.ReadWrite)]` | `[DataField]` (VV уже включён) |
| `comp.Next = curTime + comp.Interval` | `comp.Next += comp.Interval`, время только в `TimeSpan`, не `float` |
| События-классы | `[ByRefEvent] public record struct SomeAttemptEvent(bool Cancelled = false);` |

## API систем
- Публичные методы, меняющие компоненты или отдающие данные: `TryDoSomething`, `TryGetSomething`, `ResolveSomething`.
- `Try...` возвращает `bool`, `true` строго при успехе.
- `Resolve...` может обработать `false` и залогировать: `Log.Error($"Got invalid entity {ToPrettyString(uid)}!")`, однако в большинстве случаев используется Resolve(entity.Owner, ref entity.Comp, logMissing: false) чтобы вызвать API без инородной для другого кода проверки на компонент.

## Зависимости и null-safety
- `[Dependency]` вместо `IoCManager.Resolve<T>()`. Использования readonly для таких случаев неправильно, так как оно запретить автогенерацию кода, ускоряющую рекомпиляцию.
- В UI (`*.xaml.cs`) систему допустимо получать через `IEntityManager`.
- `TryComp`/`TryGetComponent` вместо `GetComponent`, если компонент не гарантирован на 100%.

## Поддерживаемость кода
- Не используй все возможности языка c#. Код намеренно не использует все фичи из-за усложнения читаемости человеком или увеличение время компиляции без особой на то причины. Посмотри в соседние системы и используй аналогичные фичи языка.
- Пиши ассерты (DebugTools.Assert(condition, "message")) внутри своей логики, они работают быстрее тестов и позволяют другим разработчикам не сломать логику твоей системы. 
- Если файл системы получается слишком большим или имеет явно разные логики, то его стоит сделать partial и разделить на несколько файлов, например `FeatureSystem.cs` и `FeatureSystem.Special.cs`.

## Плохо → хорошо
```csharp
// ❌
namespace Content.Server.Test {
    public class TestSystem : EntitySystem {
        public override void Update(float frameTime) {
            var random = IoCManager.Resolve<IRobustRandom>();
        }
    }
}

// ✅
namespace Content.Server.Test;

public sealed partial class TestSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    public override void Update(float frameTime)
    {
    }
}
```

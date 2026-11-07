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
- Не пиши `Get...`, который подменяет отсутствие значения на `new()`/`default` или меняет объект при чтении: ошибка маскируется. Отдавай `Try...` с `out` или пусть вызывающий использует `TryGetValue`.
- `Resolve...` принимают `Entity<T?>` и подставляют недостающий компонент. Внутри вызывай `Resolve(ent.Owner, ref ent.Comp, logMissing: false)`: так вызывающему коду не нужно заранее проверять наличие компонента. Лог об ошибке (`Log.Error($"Got invalid entity {ToPrettyString(uid)}!")`) пиши, только если отсутствие компонента означает ошибку вызывающего.

## Зависимости и null-safety
- `[Dependency]` вместо `IoCManager.Resolve<T>()`. В новом коде поля с `[Dependency]` пиши **без `readonly`**, а класс делай `partial`: иначе не сработает кодогенерация, ускоряющая перекомпиляцию. Существующий код ради этого не переделывай.
- В UI (`*.xaml.cs`) систему допустимо получать через `IEntityManager`.
- `TryComp`/`TryGetComponent` вместо `GetComponent`, если компонент не гарантирован на 100%.

## Поддерживаемость кода
- Не используй все возможности C#. Код намеренно ограничивается привычным набором фич: редкие конструкции усложняют чтение и увеличивают время компиляции без веской причины. Перед написанием посмотри соседние системы и используй тот же набор фич, например PrimaryConstructor для ивентов.
  Примеры фич, которых нужно избегать:
  - **Локальные функции (функция, объявленная внутри метода).** Читателю неочевидно, где заканчивается метод и начинается вложенная логика. Выноси в отдельный приватный метод. Исключения: лямбды для подписок на события и `Action`.
    ```csharp
    // ❌
    private void Process(Entity<FooComponent> ent)
    {
        bool IsReady(EntityUid uid) => HasComp<BarComponent>(uid);

        if (IsReady(ent))
            DoWork(ent);
    }

    // ✅
    private void Process(Entity<FooComponent> ent)
    {
        if (IsReady(ent))
            DoWork(ent);
    }

    private bool IsReady(EntityUid uid)
    {
        return HasComp<BarComponent>(uid);
    }
    ```
- Пиши ассерты (`DebugTools.Assert(condition, "message")`) внутри своей логики: они срабатывают раньше тестов и не дают другим разработчикам сломать инварианты твоей системы. В Release они вырезаются, поэтому не клади в них побочные эффекты.
- Если файл системы получается слишком большим или имеет явно разные логики, сделай его `partial` и раздели по темам: `FeatureSystem.cs` (зависимости и `Initialize`), `FeatureSystem.Network.cs`, `FeatureSystem.Chats.cs` и т. д. Каждая часть регистрирует свои подписки в своём `InitializeNetwork()`/`InitializeChats()`, а `Initialize()` в главном файле только вызывает их. Ассерты-проверки инвариантов можно вынести в `FeatureSystem.Asserts.cs`.
- Набор вариантов — это `enum`, а не `const int`.
  ```csharp
  // ❌
  private const int ChatsList = 0;
  private const int ChatHistory = 1;
  private int _currentView;

  // ✅
  private enum View
  {
      ChatsList,
      ChatHistory,
  }

  private View _currentView;
  ```
- `enum`, который используют несколько систем (команды и ключи сетевых пакетов, ключи UI), лежит в `Content.Shared` в одном файле. Не вкладывай его в класс системы и не заводи в каждой системе свою копию с похожими значениями.
- Повторяющийся UI-элемент (строка списка, кнопка со сложным содержимым, панель выбора) — отдельный контрол: `.xaml` + `.xaml.cs` или класс-наследник контрола. Не собирай его кодом (`new Button { ... }`, `AddChild` в цикле) внутри окна. Простые одиночные `Label` допустимы в коде.
- Перед сдачей поищи повторы: одинаковая сборка пакетов, словарей, сообщений выносится в один приватный метод. Для «получить или создать» в словаре используй `GetOrNew` из `Robust.Shared.Utility`.

## Минимум типов
Каждый новый тип (класс, struct, интерфейс, enum, делегат) стоит читателю внимания и времени компиляции. Создавай его, только если ответишь на два вопроса:
1. Кто его использует? (минимум два места, либо это требование движка: компонент, система, событие, прототип, сетевые или сериализуемые данные.)
2. Что станет сложнее без него?

Если ответ «выглядит аккуратнее» или «пригодится потом», типа не будет.

Признаки лишнего типа:
- **Обёртка над коллекцией**, которая пробрасывает вызовы или хранит счётчик. Используй `Dictionary`/`HashSet` напрямую, счётчик держи полем.
- **Обёртка над примитивом, которая используется лишь местами** (`ContactKey` вокруг `uint`, а модели и словари вокруг всё равно хранят `uint`): получаются постоянные конвертации без реальной типобезопасности. Типизированный id оправдан, если он проходит через весь API (поля, словари, сигнатуры) и это `readonly record struct`, а не `class`.
- **Интерфейс с одной реализацией**, без планов на вторую. Особенно интерфейс, нужный лишь чтобы дженерик мог вызвать `new()` или прочитать поле: сначала попробуй обойтись параметром или другой схемой.
- **Абстрактный базовый класс** ради пары общих полей.
- **Класс-«хелпер»** с одним статическим методом. Это приватный метод системы.
- **Разные типы с одинаковой формой**, различающиеся только именем.

Оправдано без отдельного обоснования: компоненты, события (`record struct`), прототипы, данные для сети и сериализации, интерфейсы движка и контента, у которых несколько реализаций, типизированные id.

Если всё же вводишь тип вне этого списка, в ответе разработчику одной строкой напиши, кто его использует и что без него усложнится.
```csharp
// ❌ Три слоя ради счётчика и Dictionary
public interface IId { uint Id { get; init; } }
public sealed class ChatKey : Key { }
public sealed class SequenceDataStore<TKey, TValue> where TKey : IId, new() { }

// ✅ Данные в компоненте, id — типизированная обёртка над uint, счётчик — поле
[Serializable, NetSerializable]
public readonly record struct ChatUid(uint Value);

public uint NextId;
public readonly Dictionary<ChatUid, MessengerChat> Chats = new();
```

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

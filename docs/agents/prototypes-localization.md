# Прототипы (YAML) и локализация

Теория: [Прототипы](https://serbiastrong-220.github.io/ss220-docs/development/prototypes/) · [Локализация](https://serbiastrong-220.github.io/ss220-docs/development/localization/)
Где размещать и как помечать правки: [ss220-conventions.md](ss220-conventions.md).

## YAML
- Компактные списки: `-` **без** отступа относительно родительского ключа.
```yaml
# ❌
effects:
  - !type:DoAfter
  - !type:Popup

# ✅
effects:
- !type:DoAfter
- !type:Popup
```
- Новые прототипы только в `Resources/Prototypes/SS220/...`.
- В C# ссылки на прототипы типизируй как `ProtoId<T>`, не `string`.
- Сам `dotnet run --project Content.YAMLLinter` не запускай. После правок YAML предложи разработчику запустить его для проверки целостности: в CI при открытии PR эта проверка может отсутствовать.

## Локализация (Fluent, `.ftl`)
- Любой текст, видимый игроку, идёт через `Loc.GetString("string-id")`. Хардкод запрещён.
- Исключения: сообщения для админов в админ-чат и текст сущностей на игровых картах.
- Подставляй значения аргументами, не конкатенацией: `Loc.GetString("my-message", ("user", uid))`.
- Новые строки: `Resources/Locale/ru-RU/ss220/...`.
- Правки существующих строк вне `ss220`: сначала переопредели строку в папке `ru-RU-220`, подробности в [ss220-conventions.md](ss220-conventions.md). Если переопределить нельзя, оборачивай правку в `#SS220-... begin` / `end`, даже для одной строки (особенность чтения ftl).

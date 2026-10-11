# SS220 Space Station 14

Форк SS14 (Space Wizards → Corvax → SS220). C# / .NET 10, движок RobustToolbox (сабмодуль). Логика на ECS, контент в YAML-прототипах, строки в Fluent (`ru-RU`).
Отвечай разработчику на русском. Идентификаторы в коде пиши на английском.

## Структура
- `Content.Shared/` — код клиента и сервера: компоненты, общие системы, события
- `Content.Server/`, `Content.Client/` — серверная логика и клиентская логика/UI
- `Content.Tests/`, `Content.IntegrationTests/`, `Content.YAMLLinter/` — тесты и линтер прототипов
- `Resources/Prototypes/`, `Resources/Locale/ru-RU/`, `Resources/Textures/` — YAML, локализация, спрайты
- `RobustToolbox/` — движок, сабмодуль. **Не редактировать**
- Папки `SS220` (напр. `Content.Server/SS220/TTS`) — наш код. Всё вне них — апстрим

## Команды (сначала узкие, полный прогон только по просьбе)
```
python3 RUN_THIS.py                        # первичная инициализация сабмодулей
dotnet build Content.Server -c Release     # сборка одного проекта
dotnet build -c Release                    # полная сборка, ожидаем 0 ошибок
dotnet test Content.Tests --filter "FullyQualifiedName~<Name>"
dotnet run --project Content.YAMLLinter    # проверка прототипов после правок YAML
```
Подробнее: [docs/agents/build-test.md](docs/agents/build-test.md)

## Главные правила
1. **Новое — в `SS220`.** Новые классы, прототипы, спрайты и строки локализации кладём в папки `SS220`, даже если это дополнение к ванили.
2. **Правка апстрима — с меткой.** Любое изменение вне `SS220` помечаем `//SS220-feature-name` (YAML/ftl — `#`). Большие правки выносим в `Name.SS220.cs` (`partial`).
3. **ECS.** Компонент — только данные, логика — в системе. Системы без состояния. Зависимости через `[Dependency]`.
4. **Горячие пути** (`Update`, `FrameUpdate`): без LINQ и аллокаций коллекций, только `EntityQuery<T>`.
5. **Современный API:** `Entity<T>`, `ref`-события (`record struct`), `ProtoId<T>`, `TimeSpan` для времени.
6. **Тексты для игрока** — только через `Loc.GetString`, имена сущностей — через `Identity.Name`.
7. **Стиль:** Allman, file-scoped namespace, `sealed`, без пустых строк в начале/конце блоков. YAML-списки компактные (`-` без отступа).
8. В новых файлах добавляй CLA-шапку (формат в [ss220-conventions](docs/agents/ss220-conventions.md)).

## Куда смотреть по задаче
| Задача | Файл |
|---|---|
| Новый файл, правка апстрима, метки, спрайты, чейнджлог | [docs/agents/ss220-conventions.md](docs/agents/ss220-conventions.md) |
| Написание/правка C#: стиль, нейминг, API | [docs/agents/csharp-style.md](docs/agents/csharp-style.md) |
| Системы, компоненты, события, производительность | [docs/agents/ecs-performance.md](docs/agents/ecs-performance.md) |
| YAML-прототипы, локализация | [docs/agents/prototypes-localization.md](docs/agents/prototypes-localization.md) |
| Сборка, запуск, тесты, линтер | [docs/agents/build-test.md](docs/agents/build-test.md) |
| Новая система/фича из вызовов существующих систем и простой логики с компонентами (по умолчанию) | [docs/agents/new-common-system.md](docs/agents/new-common-system.md) |
| Новая базисная система, которая станет API для других систем (только если готового нет) | [docs/agents/new-fundament-architecture.md](docs/agents/new-fundament-architecture.md) |

Читай только нужный файл. Остальное не загружай.

## Разрешения
**Без запроса:** читать файлы, собирать один проект, запускать отдельные тесты и YAMLLinter.
**Только с подтверждения:** `git commit/push`, удаление файлов, любые правки в `RobustToolbox/`, добавление NuGet-пакетов (`Directory.Packages.props`), правки карт в `Resources/Maps/`, полный прогон `Content.IntegrationTests`.

## Если не уверен
- Не знаешь, апстрим файл или наш: смотри на путь (`SS220` в пути = наш) и на метки в файле.
- Есть образец рядом: ищи похожую систему/прототип в `Content.*/SS220` и повторяй её паттерн.
- Не получается после двух попыток или нужно решение по архитектуре: остановись и спроси.

## Ссылки
[Вики разработчиков](https://serbiastrong-220.github.io/ss220-docs/) · [Наставления по репозиторию](https://serbiastrong-220.github.io/ss220-docs/development/ss220-guidelines/) · [ECS](https://serbiastrong-220.github.io/ss220-docs/development/ecs/) · [Прототипы](https://serbiastrong-220.github.io/ss220-docs/development/prototypes/) · [Локализация](https://serbiastrong-220.github.io/ss220-docs/development/localization/) · [Неткод](https://serbiastrong-220.github.io/ss220-docs/development/basic-networking/) · [Производительность](https://serbiastrong-220.github.io/ss220-docs/advanced-development/performance-troubleshooting/)

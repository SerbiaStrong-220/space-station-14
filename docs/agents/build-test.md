# Сборка, запуск, тесты

Подробно: [Сборка и запуск локального сервера](https://serbiastrong-220.github.io/ss220-docs/localhost/)

## Требования
- Git
- Python 3.7+ (с python.org, не из Windows Store)
- .NET 10 SDK x64
- ~4 ГБ свободного места

## Первичная настройка

В большинстве случаев пользователь уже настроит всё правильно, иначе вот инструкция:

```
git clone https://github.com/SerbiaStrong-220/space-station-14.git
cd space-station-14
python3 RUN_THIS.py     # Windows: py -3 .\RUN_THIS.py
```
Скрипт инициализирует сабмодули и скачивает движок.

Если скрипт не сработал:
```
git submodule update --init --recursive
```

## Конфигурации сборки
| Команда | Когда |
|---|---|
| `dotnet build` | Debug. Для разработки и теста нового контента. Самая строгая: падает на ассертах |
| `dotnet build -c Release` | Проверка завершённости фичи, поведение как в обычном раунде |
| `dotnet build -c Tools` | Маппинг: без лишних станций и игровых событий |

Предупреждения (жёлтые) игнорируй, важны только ошибки (красные). Итог сборки: 0 ошибок.

Для быстрой проверки собирай один проект: `dotnet build Content.Server -c Release`, `dotnet build Content.Client -c Release`.

## Запуск
Сервер: `runserver.sh` / `runserver.bat`. Клиент: `runclient.sh` / `runclient.bat`. Для Tools-сборки есть `runserver-Tools.*` и `runclient-Tools.*`.
Запуск сервера и клиента делай только по просьбе. Конфиг сервера: `bin/Content.Server/server_config.toml`.

## Тесты и проверки
```
# Узко (по умолчанию)
dotnet test Content.Tests --filter "FullyQualifiedName~<ClassOrMethod>"
dotnet test Content.IntegrationTests --filter "FullyQualifiedName~<ClassOrMethod>"

# Линтер прототипов (только по просьбе)
dotnet run --project Content.YAMLLinter

# Полный прогон (только по просьбе, интеграционные ОЧЕНЬ долгие)
dotnet test Content.Tests
dotnet test Content.IntegrationTests
```
- Менял YAML: сам линтер не запускай, в конце работы предложи запустить `dotnet run --project Content.YAMLLinter`.
- Менял логику системы: ищи тест рядом в `Content.Tests` / `Content.IntegrationTests` и запусти его.
- Если сборка или тест падает дважды по непонятной причине, остановись и опиши проблему.

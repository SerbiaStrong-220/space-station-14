# Конвенции репозитория SS220

Источник: [Наставления по работе с репозиторием SS220](https://serbiastrong-220.github.io/ss220-docs/development/ss220-guidelines/). Без соблюдения этих правил PR не будет принят.

## Где размещать
| Что | Куда | Пример |
|---|---|---|
| Новый C#-класс | `Content.{Client,Shared,Server}/SS220/<Механика>/` | `Content.Server/SS220/TTS` |
| Новый прототип | `Resources/Prototypes/SS220/...` | `Resources/Prototypes/SS220/Entities/Clothing/Head` |
| Новый спрайт | `Resources/Textures/SS220/...` | `Resources/Textures/SS220/Clothing/Head/Hats` |
| Новые строки | `Resources/Locale/ru-RU/ss220/...` | `Resources/Locale/ru-RU/ss220/clothing/Head` |

- Под каждую механику своя папка. UI, компоненты и системы по возможности разделяем.
- Namespace соответствует пути папок (напр. `Content.Server.SS220.TTS`).
- Новые прототипы кладём в `SS220`, даже если это дополнение к ванильной сущности.

## Правки в апстриме (вне папок SS220)
Метку обязательно. Имя фичи в kebab-case, одно на весь PR.

C#, одна строка:
```csharp
NewMethod(); //SS220-new-feature
```
C#, несколько строк:
```csharp
//SS220-new-feature begin
if (number > 2)
    NewMethod();
//SS220-new-feature end
```

Общие правила
- Если правок в апстрим-файле много, вынеси их в `ExampleSystem.SS220.cs`, пометив родительский класс `partial`.
- Метка должна соответствовать коду: не оставляй метки на несвязанных правках и не оставляй правки без метки.

YAML, одна строка: `- type: SomeNewThing #SS220-new-feature`. Несколько строк:
```yaml
#SS220-new-feature begin
- type: SomeNewThing
  number: 220
#SS220-new-feature end
```
Локализация (ftl): при необходимости заменить локализацию в ru-Ru используй папку ru-Ru-220, только помни что определённые локализации в ru-Ru не будут доступы из ru-Ru-220

## CLA-шапка в новых файлах
C#: `// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt`
YAML: та же строка с `#` в начале файла, затем пустая строка.

## Спрайты (`meta.json`)
Полностью наш спрайт:
```json
"license": "EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt",
"copyright": "Made by {nickname} (Discord/GitHub) for SS220"
```
Взятый или респрайченный: сохрани оригинальную лицензию и копирайт, допиши `resprited for SS220 by {nickname} (Discord/GitHub)`. При частичном респрайте перечисли изменённую часть. Существующие спрайты можно заменять прямо в исходной папке, но лицензию и копирайт обновить обязательно. Никнейм автора не выдумывай, спроси.

## Чейнджлог
Исправления ошибок и изменения опциональных игровых механик и интеракций попадают в CL в описании PR. Формат — по шаблону описания PR в репозитории.

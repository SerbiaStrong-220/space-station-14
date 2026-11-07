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
Метку обязательно. Имя фичи в kebab-case, одно на весь PR. Юзинги не помечай

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

YAML, одна строка: `- type: SomeNewThing #SS220-new-feature`. Несколько строк:
```yaml
#SS220-new-feature begin
- type: SomeNewThing
  number: 220
#SS220-new-feature end
```
Локализация (`.ftl`): сначала **переопредели** строку, а не правь апстрим-файл. Положи новое значение того же ключа в папку `ru-RU-220` (точное имя и регистр проверь по факту: `ls Resources/Locale`). Учти: часть строк из `ru-RU` недоступна из `ru-RU-220`, поэтому не ссылайся из `ru-RU-220` на термины и ключи, определённые в `ru-RU`, и проверяй результат.
Если переопределить нельзя, правь файл апстрима и **всегда** оборачивай правку в `begin`/`end`, даже для одной строки (особенность чтения ftl):
```
#SS220-new-translation begin
ent-NewObject = новый перевод объекта
#SS220-new-translation end
```
Новые ключи, которых нет в апстриме, идут в `Resources/Locale/ru-RU/ss220/...` (см. таблицу выше).

**Общие правила**
-  Правки в апстрим-файле лучше выносить в `ExampleSystem.SS220.cs`, пометив родительский класс `partial`.
- Метка должна соответствовать коду: не оставляй метки на несвязанных правках и не оставляй правки без метки.

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

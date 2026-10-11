# ECS, производительность, стабильность

Теория: [ECS](https://serbiastrong-220.github.io/ss220-docs/development/ecs/) · [Простейшая система](https://serbiastrong-220.github.io/ss220-docs/development/creating-simple-system/) · [Неткод](https://serbiastrong-220.github.io/ss220-docs/development/basic-networking/) · [Производительность](https://serbiastrong-220.github.io/ss220-docs/advanced-development/performance-troubleshooting/)

## Разделение
- Компонент содержит **только данные**. Логики в `*Component.cs` нет.
- Вся логика в `EntitySystem`. Система не хранит изменяемое состояние, оно живёт в компонентах. Исключениями иногда могут быть клиентские системы, которые могут хранить, например, шейдеры.
- Системы общаются событиями (`SubscribeLocalEvent<T>`, `RaiseLocalEvent`), а не прямым изменением чужих компонентов. Пример: вместо `filth.Amount = 0` из чужой системы подними событие, на которое отреагирует `FilthSystem`. Также хорошим тоном является наличие возможности установить Cancel поле для ивента, чтобы остановить выполнение логики.
- Задержки делай через `DoAfter` или таймстемпы в `Update`. `Thread.Sleep` и блокирующие вызовы запрещены: сервер однопоточный и встанет целиком.
- Общую логику и данные размещай в `Content.Shared`, серверную/клиентскую часть выноси в `Content.Server`/`Content.Client`.

## Горячие пути (`Update`, `FrameUpdate`)
- Без LINQ (`Where`, `Select`, `ToList`): используй `for/foreach`.
- Без `new List<T>()` и других аллокаций коллекций в цикле. Переиспользуй или пулируй.
- Не перебирай все сущности (`EntityManager.GetEntities()`). Используй `EntityQuery<T>`, `EntityQueryEnumerator<T>` или подписку на компоненты.
- `Resolve<T>` внутри цикла не вызывай. Получай компоненты заранее или через `GetEntityQuery<T>()`.

## Стабильность
- Перед манипуляциями в отложенном коде (таймеры, DoAfter) проверяй `Deleted(entity)` и `!entity.Initialized`.
- Не используй `GetComponent` без гарантии наличия компонента, иначе будет `Exception`. Бери `TryComp`.
- Любое изменение состояния, нарушающее сетевую безопасность (доверие данным клиента без проверки на сервере), считается критичным. Однако большинство привычных путей, таких как ui имеют проверки для некорректных состояний и попыток обмана.

## Игровые требования
- Имя сущности в тексте для игрока (`_popup.PopupEntity` и т. п.) бери через `Identity.Name(uid, EntityManager)`, не `MetaData.EntityName`. Иначе сломается скрытие личности.

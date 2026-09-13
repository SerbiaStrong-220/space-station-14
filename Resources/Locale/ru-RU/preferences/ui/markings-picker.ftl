markings-used = Используемые черты
markings-unused = Неиспользуемые черты
markings-add = Добавить черту
markings-remove = Убрать черту
markings-rank-up = Вверх
markings-rank-down = Вниз
markings-search = Поиск
marking-points-remaining = Черт осталось: { $points }
marking-used = { $marking-name }
marking-used-forced = { $marking-name } (Принудительно)
marking-slot-add = Добавить
marking-slot-remove = Удалить
marking-slot = Слот { $number }
humanoid-marking-modifier-force = Принудительно
humanoid-marking-modifier-ignore-species = Игнорировать вид
humanoid-marking-modifier-base-layers = Базовый слой
humanoid-marking-modifier-enable = Включить
humanoid-marking-modifier-prototype-id = ID прототипа:

# Categories

markings-category-Special = Специальное
markings-category-Hair = Причёска
markings-category-FacialHair = Лицевая растительность
markings-category-Head = Голова
markings-category-HeadTop = Голова (верх)
markings-category-HeadSide = Голова (бок)
markings-category-Snout = Морда
markings-category-SnoutCover = Морда (Внешний)
markings-category-UndergarmentTop = Нижнее бельё (Верх)
markings-category-UndergarmentBottom = Нижнее бельё (Низ)
markings-category-Chest = Грудь
markings-category-Arms = Руки
markings-category-Legs = Ноги
markings-category-Tail = Хвост
markings-category-Overlay = Наложение

-markings-selection = { $selectable ->
    [0] У вас не осталось доступных черт.
    [one] Вы можете выбрать еще одну черту.
    *[other] Вы можете выбрать еще { $selectable } черт.
}

markings-limits = { $required ->
    [true] { $count ->
        [-1] Выберите как минимум одну черту.
        [0] Вы не можете выбрать эти черты для персонажа, но каким-то образом должны? Это баг.
        [one] Выберите одну черту.
        *[other] Выберите как минимум черту и максимум {$count} черт. { -markings-selection(selectable: $selectable) }
    }
    *[false] { $count ->
        [-1] Выберите любое количество черт.
        [0] Вы не можете выбрать никаких черт.
        [one] Выберите до одной черты.
        *[other] Выберите до {$count} черт. { -markings-selection(selectable: $selectable) }
    }
}

markings-reorder = Изменить порядок черт

humanoid-marking-modifier-respect-limits = Соблюдать лимиты

humanoid-marking-modifier-respect-group-sex = Соблюдать ограничения по группе и полу

markings-organ-Torso = Туловище

markings-organ-Head = Голова

markings-organ-ArmLeft = Левая Рука

markings-organ-ArmRight = Правая Рука

markings-organ-HandRight = Правая Кисть

markings-organ-HandLeft = Левая Кисть

markings-organ-LegLeft = Левая Нога

markings-organ-LegRight = Правая Нога

markings-organ-FootLeft = Левая Ступня

markings-organ-FootRight = Правая Ступня

markings-organ-Eyes = Глаза

markings-layer-Special = Специальное

markings-layer-Tail = Хвост

markings-layer-Tail-Moth = Крылья

markings-layer-Hair = Волосы

markings-layer-FacialHair = Волосы на лице

markings-layer-UndergarmentTop = Майка

markings-layer-UndergarmentBottom = Нижнее белье

markings-layer-Chest = Грудь

markings-layer-Head = Голова

markings-layer-Snout = Морда

markings-layer-SnoutCover = Покров морды

markings-layer-HeadSide = Голова (Бок)

markings-layer-HeadTop = Голова (Верх)

markings-layer-Eyes = Глаза

markings-layer-RArm = П. Рука

markings-layer-LArm = Л. Рука

markings-layer-RHand = П. Кисть

markings-layer-LHand = Л. Кисть

markings-layer-RLeg = П. Нога

markings-layer-LLeg = Л. Нога

markings-layer-RFoot = П. Ступня

markings-layer-LFoot = Л. Ступня

markings-layer-Overlay = Наложение

markings-layer-TailOverlay = Наложение Хвоста

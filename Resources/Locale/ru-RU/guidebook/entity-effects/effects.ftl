entity-effect-guidebook-destroy = { $chance ->
    [1] Уничтожает
    *[other] уничтожает
    } объект

entity-effect-guidebook-break = { $chance ->
    [1] Ломает
    *[other] ломает
    } объект

entity-effect-guidebook-extinguish-reaction = { $chance ->
    [1] Тушит
    *[other] тушит
    } огонь

entity-effect-guidebook-movespeed-modifier = { $chance ->
    [1] Изменяет
    *[other] изменяет
    } скорость передвижения на {NATURALFIXED($sprintspeed, 3)}x минимум на {NATURALFIXED($time, 3)} {MANY("секунду", $time)}

entity-effect-guidebook-plant-remove-kudzu = { $chance ->
    [1] Удаляет
    *[other] удаляет
    } кудзу с растения

entity-effect-guidebook-plant-mutate-chemicals = { $chance ->
    [1] Мутирует\n
    *[other] мутирует
    } растение, чтобы производить {$name}

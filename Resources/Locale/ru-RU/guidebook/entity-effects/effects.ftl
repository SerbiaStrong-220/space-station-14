entity-effect-guidebook-destroy = { $chance ->\n  [1] Уничтожает\n *[other] уничтожает\n} объект

entity-effect-guidebook-break = { $chance ->\n  [1] Ломает\n *[other] ломает\n} объект

entity-effect-guidebook-extinguish-reaction = { $chance ->\n  [1] Тушит\n *[other] тушит\n} огонь

entity-effect-guidebook-movespeed-modifier = { $chance ->\n  [1] Изменяет\n *[other] изменяет\n} скорость передвижения на {NATURALFIXED($sprintspeed, 3)}x минимум на {NATURALFIXED($time, 3)} {MANY("секунду", $time)}

entity-effect-guidebook-plant-remove-kudzu = { $chance ->\n  [1] Удаляет\n *[other] удаляет\n} кудзу с растения

entity-effect-guidebook-plant-mutate-chemicals = { $chance ->\n  [1] Мутирует\n *[other] мутирует\n} растение, чтобы производить {$name}

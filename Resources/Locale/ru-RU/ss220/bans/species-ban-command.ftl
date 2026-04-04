cmd-speciesban-desc = Запрещает пользователю играть на расе
cmd-speciesban-help = Использование: speciesban <имя или ID пользователя> <id расы (SpeciesPrototype)> <причина> [продолжительность в минутах (необязательно, 0 для навсегда)] [тяжесть (необязательно)] [discord уведомление (необязательно)]

## Completion result hints

cmd-speciesban-hint-1 = <имя или ID игрока>
cmd-speciesban-hint-2 = <id расы (SpeciesPrototype)>
cmd-speciesban-hint-3 = <причина>
cmd-speciesban-hint-4 =  [продолжительность в минутах (необязательно, 0 для навсегда)]
cmd-speciesban-hint-5 =  [тяжесть (необязательно)]
cmd-speciesban-hint-duration-1 = Навсегда
cmd-speciesban-hint-duration-2 = 1 день
cmd-speciesban-hint-duration-3 = 3 дня
cmd-speciesban-hint-duration-4 = 1 неделя
cmd-speciesban-hint-duration-5 = 2 недели
cmd-speciesban-hint-duration-6 = 1 месяц

cmd-species-ban-minutes-parse = { $time } - недопустимое количество минут.\n{ $help }
cmd-species-ban-severity-parse = ${ severity } не является валидной тяжестью\n{ $help }.
cmd-species-ban-arg-count = Недопустимое количество аргументов.
cmd-species-ban-parse = Раса с id { $speciesId } не существует.
cmd-species-ban-name-parse = Невозможно найти игрока с таким именем.

cmd-species-ban-existing = { $target } уже имеет запрет на расу { $species }.
cmd-species-ban-success = { $target } запрещено играть на расе { $species } по причине { $reason } { $length }.

cmd-species-ban-inf = навсегда
cmd-species-ban-until = до { $expires }

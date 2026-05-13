cmd-chat-ban-desc = Запрещает пользователю писать в выбранном чате
cmd-chat-ban-help = Использование: chat <имя или ID пользователя> <id чата> <причина> [продолжительность в минутах (необязательно, 0 для навсегда)] [тяжесть (необязательно)] [discord уведомление (необязательно)]

## Completion result hints

cmd-chat-ban-hint-1 = <имя или ID игрока>
cmd-chat-ban-hint-2 = <id чата>
cmd-chat-ban-hint-3 = <причина>
cmd-chat-ban-hint-4 = [продолжительность в минутах (необязательно, 0 для навсегда)]
cmd-chat-ban-hint-5 = [тяжесть (необязательно)]
cmd-chat-ban-hint-duration-1 = Навсегда
cmd-chat-ban-hint-duration-2 = 1 день
cmd-chat-ban-hint-duration-3 = 3 дня
cmd-chat-ban-hint-duration-4 = 1 неделя
cmd-chat-ban-hint-duration-5 = 2 недели
cmd-chat-ban-hint-duration-6 = 1 месяц

cmd-chat-ban-minutes-parse = { $time } - недопустимое количество минут.\n{ $help }
cmd-chat-ban-severity-parse = ${ severity } не является валидной тяжестью\n{ $help }.
cmd-chat-ban-arg-count = Недопустимое количество аргументов.
cmd-chat-ban-parse = Чат { $chat } не существует.
cmd-chat-ban-name-parse = Невозможно найти игрока с таким именем.

cmd-chat-ban-existing = { $target } уже имеет запрет на этот чат { $chat }.
cmd-chat-ban-success = { $target } запрещено писать в { $chat } чат по причине { $reason } { $length }.

cmd-chat-ban-inf = навсегда
cmd-chat-ban-until = до { $expires }

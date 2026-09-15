timer-program-name = Часы

timer-cartridge-notification-header = Уведомление таймера
timer-cartridge-notification = Таймер истёк

timer-cartridge-ui-time-wrapper = [color=white][font size=36]{ $time }[/font][/color]
timer-cartridge-ui-data-wrapper = [color=white][font size=14]{ $data }[/font][/color]
timer-cartridge-ui-shift-length-label = [color="gray"][font size=13]Смена идёт:[/font][/color]
timer-cartridge-ui-shift-length-format = [color=white]{ $hours ->
        [0] { "" }
        [1] 1 час
        [2] 2 часа
        [3] 3 часа
        [4] 4 часа
       *[5] {$hours} часов
    } { $minutes ->
        [0] 1 минуту
        [1] 1 минуту
        [2] 2 минуты
        [3] 3 минуты
        [4] 4 минуты
       *[5] {$minutes} минут
    }[/color]

timer-cartridge-ui-settings-label = [color=white]Настройки таймера[/color]
timer-cartridge-ui-enable-timer-button = Засечь
timer-cartridge-ui-disable-timer-button = Остановить
timer-cartridge-ui-notification-checkbox = Уведомления таймера

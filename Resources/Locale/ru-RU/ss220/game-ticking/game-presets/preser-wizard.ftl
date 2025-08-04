## Survivor

roles-antag-survivor-name = Выживший
roles-antag-survivor-objective = Текущая цель: Выжить

survivor-role-greeting =
    Вы — Выживший.
    Прежде всего, вам нужно вернуться живым на ЦентКом.
    Соберите столько огневой мощи, сколько потребуется, чтобы гарантировать своё выживание.
    Не доверяйте никому.

survivor-round-end-dead-count =
{
    $deadCount ->
        [one] [color=red]{$deadCount}[/color] выживший погиб.
        *[other] [color=red]{$deadCount}[/color] выживших погибли.
}

survivor-round-end-alive-count =
{
    $aliveCount ->
        [one] [color=yellow]{$aliveCount}[/color] выживший остался на станции.
        *[other] [color=yellow]{$aliveCount}[/color] выживших остались на станции.
}

survivor-round-end-alive-on-shuttle-count =
{
    $aliveCount ->
        [one] [color=green]{$aliveCount}[/color] выживший спасся.
        *[other] [color=green]{$aliveCount}[/color] выживших спаслись.
}

## Wizard

objective-issuer-swf = [color=turquoise]Федерация Космических Волшебников[/color]

wizard-title = Волшебник
wizard-description = На станции Волшебник! Никогда не знаешь, что они могут выкинуть.

roles-antag-wizard-name = Волшебник
roles-antag-wizard-objective = Преподте им урок, который они не забудут.

wizard-role-greeting =
    ТЫ — ВОЛШЕБНИК!
    Между Федирацией Космических Волшебников и НаноТрейзен давно растёт напряжение.
    Федерация выбрала тебя для визита на станцию.
    Продемонстрируй им свою силу.
    Что делать — решать тебе, главное — не забывай, что Федерация ждёд, чтобы ты выжил.

wizard-round-end-name = волшебник

## TODO: Ученик Волшебника

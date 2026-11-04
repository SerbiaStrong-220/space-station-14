latejoin-arrival-announcement-special = { $job } { $character } { $gender ->
        [male] ступил
        [female] ступила
        [epicene] ступили
       *[neuter] ступил
    } на мостик!

game-ticker-get-info-text =
    Привет и добро пожаловать в [color=white]Space Station 14![/color]
    Текущий раунд: [color=white]#{ $roundId }[/color]
    Текущее количество игроков: [color=white]{ $playerCount }[/color]
    Текущая карта: [color=white]{ $mapName }[/color]
    Текущий уровень угрозы: [color={$color}]{ $level }[/color]
    Текущий режим игры: [color=white]{ $gmTitle }[/color]
    >[color=yellow]{ $desc }[/color]

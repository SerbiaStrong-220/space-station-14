additional-info-no-category =
    Без категории

# ANTAG STATS START
additional-info-antag-items-label = [font size=15][color=OrangeRed]Предметы, купленные в аплинке: [/color][/font]

additional-info-antag-total-spent-tc =
    Всего потрачено: [bold][color=red]{ $value } тк[/color][/bold]
# ANTAG STATS END

# ECONOMY & RESOURCES STATS START
additional-info-economy-categories = [font size=15][color=gold]Экономика и ресурсы[/color][/font]

additional-info-total-food-eaten =
    Всего еды съедено: { $foodValue }
    Больше всего еды было съедено: [bold][color=yellow]{ $fattestName } ({ $fattestValue } ед.)[/color][/bold]

additional-info-cargo =
    Всего заработано: [bold][color=gold]{ $totalMoney }$[/color][/bold]
    Всего заказов одобрено: [bold][color=gold]{ $totalOrders }[/color][/bold]
    Больше всего заказов одобрил: [bold][color=gold]{ $totalOrderPlayer } ({$totalOrderPlayerCount})[/color][/bold]
    Самый покупаемый товар: [bold][color=gold]{ $maxOrderedItemName } ({ $maxOrderedItemCount } ед.)[/color][/bold]

additional-info-total-points-earned =
    Всего заработано очков в отделе РнД: [bold][color=mediumpurple]{$totalPoints}[/color][/bold]

additional-info-total-ore =
    Всего руды добыто: [bold][color=brown]{ $totalOre } ед.[/color][/bold]
# ECONOMY & RESOURCES STATS END

# GUN STATS START
additional-info-gun-categories = [font size=15][color=gold]Стрельба[/color][/font]

additional-info-total-shots =
    Всего сделано выстрелов: { $ammoValue }
    Больше всего сделано выстрелов: [bold][color=red]{ $gunnerName } ({ $gunnerValue })[/color][/bold]
# GUN STATS END

# JANITOR STATS START
additional-info-janitor-categories = [font size=15][color=gold]Уборщик[/color][/font]

additional-info-total-puddles =
    Всего убрано луж: { $puddleValue }
    Больше всего убрано луж: [bold][color=green]{ $janitorName } ({ $janitorValue } ед.)[/color][/bold]
# JANITOR STATS END

# DEATH STATS START
additional-info-death-categories = [font size=15][color=gold]Смерти[/color][/font]

additional-info-total-death =
    Всего смертей: { $deathValue }
    Больше всего умер: [bold][color=brown]{ $suicideName } ({ $suicideValue })[/color][/bold]
    Самая ранняя смерть: [bold][color=brown]{ $suicideEarlierName } ({ $suicideEarlierValue })[/color][/bold]
# DEATH STATS END

# EMERGENCY SHUTTLE START
additional-info-emergency-shuttles-categories = [font size=15][color=green]Эвакуация[/color][/font]

additional-info-emergency-shuttles =
    Первый шаттл был вызван в: [bold]{$firstShuttleTime}[/bold]
    Последний шаттл был вызван в: [bold]{$lastShuttleTime}[/bold]
    Шаттл был вызван [bold]{$countCalls}[/bold] { $countCalls ->
        [one] раз
        [few] раза
        *[other] раз
    }

# EMERGENCY SHUTTLE END

# MEDICAL START

additional-info-healing-categories = [font size=15][color=green]Медицина[/color][/font]

additional-info-healing =
    Всего излечено: [bold]{ $totalHealing }[/bold] урона
    Больше всего вылечил: [bold]{ $topHealerName }[/bold] в кол-ве [bold]{ $topHealerCount }[/bold] ед.

# MEDICAL END

# SECURITY START

additional-info-sec-categories = [font size=15][color=green]Служба безопасности[/color][/font]

additional-info-sec-baton =
    Всего ударов дубинкой: [bold]{ $totalHits }[/bold]
    Больше всего ударов дубинкой совершил: [bold]{ $topSecName }[/bold] в кол-ве: [bold]{ $topSecCount }[/bold] { $topSecCount ->
        [one] раза
        [few] раз
        *[other] раз
    }

# SECURITY END

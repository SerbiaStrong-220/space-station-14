## Kontra

# Shown at the end of a round of kontra
kontra-round-end-result =
    { $kontraCount ->
        [one] Был один Контрразведчиком.
       *[other] Было { $kontraCount } Контрразведчиков.
    }
kontra-round-end-codewords = Кодовыми словами были: [color=White]{ $codewords }[/color].
# Shown at the end of a round of kontra
kontra-user-was-a-kontra = [color=gray]{ $user }[/color] был(а) Контрразведчиком.
kontra-user-was-a-kontra-named = [color=White]{ $name }[/color] ([color=gray]{ $user }[/color]) был(а) Контрразведчиком.
kontra-was-a-kontra-named = [color=White]{ $name }[/color] был(а) Контрразведчиком.
kontra-user-was-a-kontra-with-objectives = [color=gray]{ $user }[/color] был(а) Контрразведчиком со следующими целями:
kontra-user-was-a-kontra-with-objectives-named = [color=White]{ $name }[/color] ([color=gray]{ $user }[/color]) был(а) Контрразведчиком со следующими целями:
kontra-was-a-kontra-with-objectives-named = [color=White]{ $name }[/color] был(а) Контрразведчиком со следующими целями:
preset-kontra-objective-issuer-syndicate = [color=#87cefa]Контрразведка[/color]
# Shown at the end of a round of kontra
kontra-objective-condition-success = { $condition } | [color={ $markupColor }]Успех![/color]
# Shown at the end of a round of kontra
kontra-objective-condition-fail = { $condition } | [color={ $markupColor }]Провал![/color] ({ $progress }%)
kontra-title = Контрразведчики
kontra-description = Среди нас есть Контрразведка...
kontra-not-enough-ready-players = Недостаточно игроков готовы к игре! Из { $minimumPlayers } необходимых игроков готовы { $readyPlayersCount }. Не удалось начать режим Контрразведки.
kontra-no-one-ready = Нет готовых игроков! Не удалось начать режим Контрразведки.

## kontraDeathMatch

kontra-death-match-title = Бой насмерть Контрразведчиков
kontra-death-match-description = Все — Контрразведчики. Все хотят смерти друг друга.
kontra-death-match-station-is-too-unsafe-announcement = На станции слишком опасно, чтобы продолжать. У вас есть одна минута.
kontra-death-match-end-round-description-first-line = КПК были восстановлены...
kontra-death-match-end-round-description-entry = КПК { $originalName }, с { $tcBalance } ТК

## kontraRole

# kontraRole
kontra-role-greeting =
    Вы - агент Контрразведки.
    Ваши цели и кодовые слова перечислены в меню персонажа.
    Воспользуйтесь аплинком, встроенным в ваш КПК, чтобы приобрести всё необходимое для выполнения работы.
    Слава Nanotrasen!
kontra-role-codewords =
    Кодовые слова следующие:
    { $codewords }
    Кодовые слова можно использовать в обычном разговоре, чтобы незаметно идентифицировать себя для других агентов Контрразведки.
    Прислушивайтесь к ним и храните их в тайне.
kontra-role-uplink-code =
    Установите рингтон Вашего КПК на { $code } чтобы заблокировать или разблокировать аплинк.
    Не забудьте заблокировать его и сменить код, иначе любой член экипажа станции сможет открыть аплинк!
# don't need all the flavour text for character menu
kontra-role-codewords-short =
    Кодовые слова:
    { $codewords }.
kontra-role-uplink-code-short = Ваш код аплинка: { $code }.

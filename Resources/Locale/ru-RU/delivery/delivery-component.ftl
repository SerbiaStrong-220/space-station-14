delivery-recipient-examine = Адресовано: { $recipient }, { $job }.
delivery-already-opened-examine = Уже вскрыто.
delivery-earnings-examine = Delivering this will earn the station [color=yellow]{ $spesos }[/color] spesos.
delivery-recipient-no-name = Безымянный
delivery-recipient-no-job = Неизвестно
delivery-unlocked-self = Вы разблокировали { $delivery } отпечатком пальца.
delivery-opened-self = Вы вскрываете { $delivery }.
delivery-unlocked-others =
    { CAPITALIZE($recipient) } { GENDER($recipient) ->
        [male] разблокировал
        [female] разблокировала
        [epicene] разблокировали
       *[neuter] разблокировало
    } { $delivery } используя свой отпечаток пальца.
delivery-opened-others =
    { CAPITALIZE($recipient) } { GENDER($recipient) ->
        [male] вскрыл
        [female] вскрыл
        [epicene] вскрыл
       *[neuter] вскрыл
    } { $delivery }.
delivery-unlock-verb = Разблокировать
delivery-open-verb = Вскрыть
delivery-slice-verb = Slice open
delivery-teleporter-amount-examine =
    { $amount ->
        [one] Содержит [color=yellow]{ $amount }[/color] доставку.
       *[other] Содержит  [color=yellow]{ $amount }[/color] доставок.
    }
delivery-teleporter-empty = { $entity } пустой.
delivery-teleporter-empty-verb = Взять почту.

# modifiers

delivery-priority-examine = Это [color=orange]срочная { $type }[/color]. У вас осталось [color=orange]{ $time }[/color] чтобы доставить его и получить бонус.
delivery-priority-delivered-examine = Это [color=orange] срочная { $type }[/color]. Она была доставлена вовремя.
delivery-priority-expired-examine = Это [color=orange]срочная { $type }[/color] посылка. Время на доставку истекло.
delivery-fragile-examine = Это [color=red]хрупкая { $type }[/color]. Доставьте её в целости и сохранности, чтобы получить бонус.
delivery-fragile-broken-examine = Это [color=red]хрупкая { $type }[/color]. Она выглядит сильно поврежденным.
delivery-bomb-examine = Это [color=purple]бомба { $type }[/color]. О нет...
delivery-bomb-primed-examine = Это [color=purple]бомба { $type }[/color]. Чтение этого - неразумное использование вашего времени.

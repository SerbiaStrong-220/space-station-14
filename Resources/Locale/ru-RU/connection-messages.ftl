whitelist-not-whitelisted = Вас нет в вайтлисте.
# proper handling for having a min/max or not
whitelist-playercount-invalid =
    { $min ->
        [0] Вайтлист для этого сервера применяется только для числа игроков ниже { $max }.
       *[other]
            Вайтлист для этого сервера применяется только для числа игроков выше { $min } { $max ->
                [2147483647] ->  так что, возможно, вы сможете присоединиться позже.
               *[other] ->  и ниже { $max } игроков, так что, возможно, вы сможете присоединиться позже.
            }
    }
whitelist-not-whitelisted-rp = Вас нет в вайтлисте. Чтобы попасть в вайтлист, посетите наш Discord (https://discord.gg/ss220).
cmd-whitelistadd-desc = Добавить игрока в вайтлист сервера.
cmd-whitelistadd-help = Использование: whitelistadd <username или  User ID>
cmd-whitelistadd-existing = { $username } уже находится в вайтлисте!
cmd-whitelistadd-added = { $username } добавлен в вайтлист
cmd-whitelistadd-not-found = Не удалось найти игрока '{ $username }'
cmd-whitelistadd-arg-player = [player]
cmd-whitelistremove-desc = Удалить игрока с вайтлиста сервера.
cmd-whitelistremove-help = Использование: whitelistremove <username или  User ID>
cmd-whitelistremove-existing = { $username } не находится в вайтлисте!
cmd-whitelistremove-removed = { $username } удалён с вайтлиста
cmd-whitelistremove-not-found = Не удалось найти игрока '{ $username }'
cmd-whitelistremove-arg-player = [player]
cmd-kicknonwhitelisted-desc = Кикнуть всег игроков не в белом списке с сервера.
cmd-kicknonwhitelisted-help = Использование: kicknonwhitelisted
ban-banned-permanent = Вы забанены навсегда.
ban-banned-permanent-appeal = Этот бан можно только обжаловать. Для этого посетите { $link }.
ban-expires = Вы получили бан на { $duration } минут, и он истечёт { $time } по UTC (для московского времени добавьте 3 часа).
ban-banned-1 = Вам, или другому пользователю этого компьютера или соединения, запрещено здесь играть.
ban-banned-2 = Причина бана: "{ $reason }"
ban-banned-3 = Если вы не согласны с выданным наказанием, посетите наш Discord: https://discord.gg/ss220
ban-banned-4 = Наказание выдано администратором: "{$admin}".
ban-banned-5 = Попытки обойти этот бан, например, путём создания нового аккаунта, будут фиксироваться.
ban-banned-6 = Номер раунда: { $round }
ban-banned-7 = не указано
ban-banned-8 = Номер бана: { $banId }
ban-banned-9 = Логин забаненного игрока: "{$login}".

soft-player-cap-full = Сервер заполнен!
panic-bunker-account-denied = Этот сервер находится в режиме "Бункер", часто используемом в качестве меры предосторожности против рейдов. Новые подключения от аккаунтов, не соответствующих определённым требованиям, временно не принимаются. Повторите попытку позже
panic-bunker-account-denied-reason = Этот сервер находится в режиме "Бункер", часто используемом в качестве меры предосторожности против рейдов. Новые подключения от аккаунтов, не соответствующих определённым требованиям, временно не принимаются. Повторите попытку позже Причина: "{ $reason }"
panic-bunker-account-reason-account = Ваш аккаунт Space Station 14 слишком новый. Он должен быть старше { $minutes } минут
panic-bunker-account-reason-overall =
    Необходимо минимальное отыгранное Вами время на сервере — { $minutes } { $minutes ->
        [one] минута
        [few] минуты
       *[other] минут
    }.
kick-afk = Вы были кикнуты за AFK

whitelist-playtime = У вас недостаточно наигранного времени для подключения к этому серверу. Вам нужно как минимум {$minutes} минут наигранного времени, чтобы зайти на этот сервер.

whitelist-player-count = В настоящее время сервер переполнен. Пожалуйста, попробуйте позже.

whitelist-notes = У вас слишком много заметок для подключения к этому серверу.

whitelist-manual = Вы не находитесь в вайтлисте этого сервера.

whitelist-blacklisted = Вы находитесь в черном списке этого сервера.

whitelist-always-deny = Вам запрещено подключаться к этому серверу.

whitelist-fail-prefix = Не в вайтлисте: { $msg }

cmd-blacklistadd-desc = Добавляет игрока с указанным именем в черный список сервера.

cmd-blacklistadd-help = Использование: blacklistadd <username>

cmd-blacklistadd-existing = {$username} уже находится в черном списке!

cmd-blacklistadd-added = {$username} добавлен в черный список

cmd-blacklistadd-not-found = Не удалось найти '{ $username }'

cmd-blacklistadd-arg-player = [игрок]

cmd-blacklistremove-desc = Удаляет игрока с указанным именем из черного списка сервера.

cmd-blacklistremove-help = Использование: blacklistremove <username>

cmd-blacklistremove-existing = {$username} не находится в черном списке!

cmd-blacklistremove-removed = {$username} удален из черного списка

cmd-blacklistremove-not-found = Не удалось найти '{ $username }'

cmd-blacklistremove-arg-player = [игрок]

baby-jail-account-denied = Этот сервер предназначен для новичков, а также для тех, кто хочет им помочь. Новые подключения со слишком старых аккаунтов или аккаунтов не из вайтлиста не принимаются. Посмотрите другие серверы и узнайте всё, что может предложить Space Station 14. Приятной игры!

baby-jail-account-denied-reason = Этот сервер предназначен для новичков, а также для тех, кто хочет им помочь. Новые подключения со слишком старых аккаунтов или аккаунтов не из вайтлиста не принимаются. Посмотрите другие серверы и узнайте всё, что может предложить Space Station 14. Приятной игры! Причина: "{ $reason }"

baby-jail-account-reason-account = Ваш аккаунт Space Station 14 слишком старый. С момента его создания должно пройти не более {$minutes} минут

baby-jail-account-reason-overall = Ваше общее время игры на сервере должно быть меньше {$minutes} минут

generic-misconfigured = Сервер неправильно настроен и не принимает игроков. Пожалуйста, свяжитесь с владельцем сервера и попробуйте позже.

ipintel-server-ratelimited = Этот сервер использует систему аудита с внешней проверкой, но он достиг максимального лимита проверок во внешнем сервисе. Пожалуйста, свяжитесь с администрацией сервера, чтобы сообщить им об этом и получить дополнительную помощь, или попробуйте позже.

ipintel-unknown = Этот сервер использует систему аудита с внешней проверкой, но при проверке вашего подключения произошла ошибка. Пожалуйста, свяжитесь с администрацией сервера, чтобы сообщить им об этом и получить дополнительную помощь, или попробуйте позже.

ipintel-suspicious = Похоже, вы пытаетесь подключиться, используя дата-центр, прокси, VPN или другое подозрительное соединение. По административным причинам мы не разрешаем играть с такими соединениями. Если у вас включен VPN или что-то подобное, пожалуйста, отключите его и попытайтесь переподключиться, либо свяжитесь с администрацией сервера для получения помощи, если считаете это ошибкой или если эти сервисы необходимы вам для игры.

hwid-required = Ваш клиент отказался отправлять аппаратный ID (HWID). Пожалуйста, свяжитесь с администрацией для получения помощи.

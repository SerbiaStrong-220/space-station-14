tts-sex-requirement-whitelist-not-pass = Для использования голоса необходим { $sexesCount ->
    [one] пол { $sexesList }.
    *[other] один из следующих полов: { $sexesList }.
}
tts-sex-requirement-blacklist-not-pass = Использование голоса недоступно для { $sexesCount ->
    [one] пола { $sexesList }.
    *[other] полов: { $sexesList }.
}

tts-sponsor-requirement-whitelist-not-pass = Для использования голоса необходим уровень подписки { $isExact ->
    [true] { $sponsorTier }.
    *[false] { $sponsorTier } или выше.
 }
tts-sponsor-requirement-blacklist-not-pass = Использованеи голоса недоступно для игроков с уровнем подписки { $isExact ->
    [true] { $sponsorTier }.
    *[false] { $sponsorTier } и выше.
 }

# © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

shuttle-lockdown-announce-locate = 
    { $locationType ->
        [coords] Источник располагается по координатам { $coords }.
        [location] Источник располагается { $location }.
        *[both] Источник располагается по координатам { $coords }, { $location }.
    }

# debug item
debug-shuttle-lockdown-warning = Средство отладки блокирует вызов шаттла эвакуации
debug-shuttle-lockdown-header = Отладочная блокировка вызова шаттла
debug-shuttle-lockdown-on-announce = Средство отладки блокирует вызов шаттла эвакуации. { $position }
debug-shuttle-lockdown-off-announce = Средство отладки больше не блокирует вызов шаттла эвакуации

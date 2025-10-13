reagent-effect-guidebook-mob-thresholds-modifier =
    {
        $refresh ->
            [false] На { $duration } { $duration ->
                    [one] секунду
                    [few] секунды
                    *[other] секунд
                } (складывается) вызывает изменение порогов состояний сущности: { $stateschanges }
            *[true] На { $duration } { $duration ->
                    [one] секунду
                    [few] секунды
                    *[other] секунд
                } вызывает изменение порогов состояний сущности: { $stateschanges }
    }
reagent-effect-guidebook-mob-thresholds-modifier-line = { $mobstate }: { $modifierType ->
        [multiplier] в { $multiplier } { $multiplier ->
                [one] раз
                [few] раза
                *[other] раз
            }
        [flat] на { $flat } ед
        *[both] в { $multiplier } { $multiplier ->
                [one] раз
                [few] раза
                *[other] раз
            } и на { $flat } ед
    }
reagent-effect-guidebook-mob-thresholds-modifier-flat = на { $flat } ед
reagent-effect-guidebook-mob-thresholds-modifier-and = и

reaction-effect-guidebook-hallucination = Вызывает галлюцинации длительностью в { $duration } { $duration ->
        [one] секунду
        [few] секунды
        *[other] секунд
    }

reaction-effect-guidebook-stamina-damage = { $heals ->
        [true] Восстанавливает { $value } ед. стамины
        *[false] Наносит { $value } ед. урона по стамине
    }

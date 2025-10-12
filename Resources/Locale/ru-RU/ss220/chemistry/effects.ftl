reagent-effect-guidebook-mob-thresholds-modifier =
    {
        $refresh ->
            [false] На { $duration } { $duration ->
                    [one] секунду
                    [few] секунды
                    *[other] секунд
                } (складывается) вызывает именение порогов состояний сущности: { $stateschanges }
            *[true] На { $duration } { $duration ->
                    [one] секунду
                    [few] секунды
                    *[other] секунд
                } вызывает именение порогов состояний сущности: { $stateschanges }
    }
reagent-effect-guidebook-mob-thresholds-modifier-multiplier = в { $multiplier } { $multiplier ->
        [one] раз
        [few] раза
        *[other] раз
    }
reagent-effect-guidebook-mob-thresholds-modifier-flat = на { $flat }

reaction-effect-guidebook-hallucination = Вызывает галлюцинации длительностью в { $duration } { $duration ->
        [one] секунду
        [few] секунды
        *[other] секунд
    }

reaction-effect-guidebook-stamina-damage = { $heals ->
        [true] Восстанавливает { $value }ед. стамины
        *[false] Наносит { $value }ед. урона по стамине
    }

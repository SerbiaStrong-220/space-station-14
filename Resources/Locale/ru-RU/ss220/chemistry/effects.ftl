reagent-effect-guidebook-mob-thresholds-modifier =
    {
        $refresh ->
            [false] На { $duration } секунд (складывается) вызывает именение порогов состояний сущности: { $stateschanges }
            *[true] На { $duration } секунд вызывает именение порогов состояний сущности: { $stateschanges }
    }
reagent-effect-guidebook-mob-thresholds-modifier-multiplier = в { $multiplier } { $multiplier ->
        [one] раз
        [few] раза
        *[other] раз
    }
reagent-effect-guidebook-mob-thresholds-modifier-flat = на { $flat }

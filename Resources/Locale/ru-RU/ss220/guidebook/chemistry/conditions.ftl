reagent-effect-condition-guidebook-has-components =
    цель { $inverted ->
    [true] не имеет
    *[false] имеет
    } { $requireAll ->
    [true] все компоненты:
    *[false] хотя бы 1 из компонентов:
    } {$components}

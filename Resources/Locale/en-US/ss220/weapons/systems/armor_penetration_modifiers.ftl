ammo-ap-rules-header = [color=lightblue]Armor penetration[/color]:

ammo-ap-rule-description = - Deals [color=red]{ $multiplier }%[/color] of [color=yellow]{ $type }[/color] damage,
    if resistance is { $armor ->
        [more] [color=orange]more[/color]
       *[less] [color=orange]less[/color]
       } than [color=red]{ $threshold }%[/color]

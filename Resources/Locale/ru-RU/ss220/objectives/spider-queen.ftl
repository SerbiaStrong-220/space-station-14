ent-BaseSpiderQueenObjective = { ent-BaseObjective }
    .desc = { ent-BaseObjective.desc }
ent-CreateCocoonsObjective= { ent-BaseSpiderQueenObjective }
    .desc = { ent-BaseSpiderQueenObjective.desc }
ent-SpiderQueenSurviveObjective = Выжить
    .desc = Вы должны оставаться в живых, чтобы сохранять контроль.

objective-create-cocoons-title =
    Создать { $count } { $count ->
        [one] кокон
        [few] кокона
       *[other] коконов
    }.
objective-create-cocoons-description =
    Используйте действие «Сплести кокон» чтобы создать { $count } { $count ->
        [one] кокон
        [few] кокона
       *[other] коконов
    }.

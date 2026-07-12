-create-3rd-person =
    { $chance ->
        [1] Создаёт
       *[other] создают
    }
-cause-3rd-person =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    }
-satiate-3rd-person =
    { $chance ->
        [1] Насыщает
       *[other] насыщают
    }

# SS220 fix loc ids begin
entity-effect-guidebook-spawn-entity =
    { $chance ->
        [1] Создаёт
       *[other] создают
    } { $amount ->
        [1] { $entname }
       *[other] { $amount } { $entname }
    }

entity-effect-guidebook-explosion =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } взрыв

entity-effect-guidebook-emp =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } электромагнитный импульс

entity-effect-guidebook-flash =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } ослепительную вспышку

entity-effect-guidebook-foam-area =
    { $chance ->
        [1] Создаёт
       *[other] создают
    } большое количество пены

entity-effect-guidebook-smoke-area =
    { $chance ->
        [1] Создаёт
       *[other] создают
    } большое количество дыма

entity-effect-guidebook-satiate-thirst =
    { $chance ->
        [1] Утоляет
       *[other] утоляют
    } { $relative ->
        [1] жажду средне
       *[other] жажду на { NATURALFIXED($relative, 3) }x от обычного
    }

entity-effect-guidebook-satiate-hunger =
    { $chance ->
        [1] Насыщает
       *[other] насыщают
    } { $relative ->
        [1] голод средне
       *[other] голод на { NATURALFIXED($relative, 3) }x от обычного
    }

entity-effect-guidebook-health-change =
    { $chance ->
        [1]
            { $healsordeals ->
                [heals] Излечивает
                [deals] Наносит
               *[both] Изменяет здоровье на
            }
       *[other]
            { $healsordeals ->
                [heals] излечивать
                [deals] наносить
               *[both] изменяют здоровье на
            }
    } { $changes }

entity-effect-guidebook-even-health-change =
    { $chance ->
        [1]
            { $healsordeals ->
                [heals] Равномерно лечит
                [deals] Равномерно наносит
               *[both] Равномерно изменяет здоровье на
            }
       *[other]
            { $healsordeals ->
                [heals] раномерно лечат
                [deals] равномерно наносят
               *[both] равномерно изменяют здоровье на
            }
    } { $changes }

entity-effect-guidebook-status-effect-old =
    { $type ->
        [update]
            { $chance ->
                [1] Вызывает
                *[other] вызывают
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) } { $time ->
                    [one] секунду
                    [few] секунды
                    *[other] секунд
                }, эффект не накапливается
        [add]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) } { $time ->
                    [one] секунду
                    [few] секунды
                    *[other] секунд
                }, эффект накапливается
       *[set]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) } { $time ->
                    [one] секунду
                    [few] секунды
                    *[other] секунд
                }, эффект не накапливается
        [remove]
            { $chance ->
                [1] Удаляет
               *[other] удаляют
            } { NATURALFIXED($time, 3) } от { LOC($key) }
    }

entity-effect-guidebook-status-effect =
    { $type ->
        [add]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) } { $time ->
                    [one] секунду
                    [few] секунды
                    *[other] секунд
                }, эффект накапливается
       *[set]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) } { $time ->
                    [one] секунду
                    [few] секунды
                    *[other] секунд
                }, эффект не накапливается
        [remove]
            { $chance ->
                [1] Удаляет
               *[other] удаляют
            } { NATURALFIXED($time, 3) } { $time ->
                    [one] секунду
                    [few] секунды
                    *[other] секунд
                } от { LOC($key) }
    } { $delay ->
        [0] немедленно
        *[other] после { NATURALFIXED($delay, 3) } { $time ->
                [one] секунду
                [few] секунды
                *[other] секунд
            } задержки
    }

entity-effect-guidebook-status-effect-indef =
    { $type ->
        [update]
                { $chance ->
                    [1] Вызывает
                    *[other] вызывают
                } постоянный {LOC($key)}
        [add]
                { $chance ->
                    [1] Вызывает
                    *[other] вызывают
                } постоянный {LOC($key)}
        [set]
                { $chance ->
                    [1] Вызывает
                    *[other] вызывают
                } постоянный {LOC($key)}
        *[remove]
                { $chance ->
                    [1] Удаляет
                    *[other] удаляют
                } {LOC($key)}
    } { $delay ->
        [0] немедленно
        *[other] после { NATURALFIXED($delay, 3) } { $time ->
                [one] секунду
                [few] секунды
                *[other] секунд
            } задержки
    }

entity-effect-guidebook-knockdown =
    { $type ->
        [update]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } { LOC($key) } минимум на { NATURALFIXED($time, 3) } { $time ->
                    [one] секунду
                    [few] секунды
                    *[other] секунд
                }, эффект не накапливается
        [add]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } нокдаун минимум на { NATURALFIXED($time, 3) } { $time ->
                    [one] секунду
                    [few] секунды
                    *[other] секунд
                }, эффект накапливается
       *[set]
            { $chance ->
                [1] Вызывает
               *[other] вызывают
            } нокдаун минимум на { NATURALFIXED($time, 3) } { $time ->
                    [one] секунду
                    [few] секунды
                    *[other] секунд
                }, эффект не накапливается
        [remove]
            { $chance ->
                [1] Удаляет
               *[other] удаляют
            } { NATURALFIXED($time, 3) } { $time ->
                    [one] секунду
                    [few] секунды
                    *[other] секунд
                } от нокдауна
    }

entity-effect-guidebook-set-solution-temperature-effect =
    { $chance ->
        [1] Устанавливает
       *[other] устанавливают
    } температуру раствора точно { NATURALFIXED($temperature, 2) }k

entity-effect-guidebook-adjust-solution-temperature-effect =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Добавляет
               *[-1] Удаляет
            }
       *[other]
            { $deltasign ->
                [1] добавляют
               *[-1] удаляют
            }
    } тепло из раствора, пока температура не достигнет { $deltasign ->
        [1] не более { NATURALFIXED($maxtemp, 2) }k
       *[-1] не менее { NATURALFIXED($mintemp, 2) }k
    }

entity-effect-guidebook-adjust-reagent-reagent =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Добавляют
               *[-1] Удаляет
            }
       *[other]
            { $deltasign ->
                [1] добавляют
               *[-1] удаляют
            }
    } { NATURALFIXED($amount, 2) } ед. от { $reagent } { $deltasign ->
        [1] к
       *[-1] из
    } раствора

entity-effect-guidebook-adjust-reagent-group =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Добавляет
               *[-1] Удаляет
            }
       *[other]
            { $deltasign ->
                [1] добавляют
               *[-1] удаляют
            }
    } { NATURALFIXED($amount, 2) }ед реагентов в группе { $group } { $deltasign ->
        [1] к
       *[-1] из
    } раствора

entity-effect-guidebook-adjust-temperature =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Добавляют
               *[-1] Удаляют
            }
       *[other]
            { $deltasign ->
                [1] добавляют
               *[-1] удаляют
            }
    } { POWERJOULES($amount) } тепла { $deltasign ->
        [1] к телу
       *[-1] из тела
    }, в котором он метабилизируется

entity-effect-guidebook-chem-cause-disease =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } болезнь { $disease }

entity-effect-guidebook-chem-cause-random-disease =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } болезнь { $diseases }

entity-effect-guidebook-jittering =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } тряску

entity-effect-guidebook-clean-bloodstream =
    { $chance ->
        [1] Очищает
       *[other] очищают
    } кровеносную систему от других веществ

entity-effect-guidebook-cure-disease =
    { $chance ->
        [1] Излечивает
       *[other] излечивают
    } болезнь

entity-effect-guidebook-eye-damage =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Наносит
               *[-1] Излечивает
            }
       *[other]
            { $deltasign ->
                [1] наносят
               *[-1] излечивают
            }
    } повреждения глаз

entity-effect-guidebook-vomit =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } рвоту

entity-effect-guidebook-create-gas =
    { $chance ->
        [1] Создаёт
       *[other] создают
    } { $moles } { $moles ->
        [1] моль
       *[other] моль
    } газа { $gas }

entity-effect-guidebook-drunk =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } опьянение

entity-effect-guidebook-electrocute =
    { $chance ->
        [1] Бьёт током
       *[other] бьют током
    } употребившего в течении { NATURALFIXED($time, 3) }

entity-effect-guidebook-emote =
    { $chance ->
        [1] Вызывает
       *[other] вызвать
    }  у цели [bold][color=white]{ $emote }[/color][/bold]

entity-effect-guidebook-extinguish-reactio =
    { $chance ->
        [1] Гасит
       *[other] гасят
    } огонь

entity-effect-guidebook-flammable-reaction =
    { $chance ->
        [1] Повышает
       *[other] повышают
    } воспламеняемость

entity-effect-guidebook-ignite =
    { $chance ->
        [1] Поджигает
       *[other] поджигают
    } употребившего

entity-effect-guidebook-make-sentient =
    { $chance ->
        [1] Делает
       *[other] делают
    } употребившего разумным

entity-effect-guidebook-make-polymorph =
    { $chance ->
        [1] Превращает
       *[other] превращают
    } употребившего в { $entityname }

entity-effect-guidebook-modify-bleed-amount =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Усиливает
               *[-1] Ослабляет
            }
       *[other]
            { $deltasign ->
                [1] усиливают
               *[-1] ослабляют
            }
    } кровотечение

entity-effect-guidebook-modify-blood-level =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Повышает
               *[-1] Понижает
            }
       *[other]
            { $deltasign ->
                [1] повышают
               *[-1] понижают
            }
    } уровень крови в организме

entity-effect-guidebook-paralyze =
    { $chance ->
        [1] Парализует
       *[other] парализуют
    } употребившего минимум на { NATURALFIXED($time, 3) }

entity-effect-guidebook-movespeed-modifie =
    { $chance ->
        [1] Делает
       *[other] делают
    } скорость передвижения { NATURALFIXED($walkspeed, 3) }x от стандартной минимум на { NATURALFIXED($time, 3) }

entity-effect-guidebook-reset-narcolepsy =
    { $chance ->
        [1] Предотвращает
       *[other] предотвращают
    } приступы нарколепсии

entity-effect-guidebook-wash-cream-pie-reaction =
    { $chance ->
        [1] Смывает
       *[other] смывают
    } кремовый пирог с лица

entity-effect-guidebook-cure-zombie-infection =
    { $chance ->
        [1] Лечит
       *[other] лечат
    } зомби-вирус

entity-effect-guidebook-cause-zombie-infection =
    { $chance ->
        [1] Заражает
       *[other] заражают
    } человека зомби-вирусом

entity-effect-guidebook-innoculate-zombie-infection =
    { $chance ->
        [1] Лечит
       *[other] лечат
    } зомби-вирус и обеспечивает иммунитет к нему в будущем

entity-effect-guidebook-reduce-rotting =
    { $chance ->
        [1] Регенерирует
       *[other] регенерируют
    } { NATURALFIXED($time, 3) } { $time ->
        [one] секунду
        [few] секунды
       *[other] секунд
    } гниения

entity-effect-guidebook-area-reaction =
    { $chance ->
        [1] Вызывает
       *[other] вызывают
    } дымовую или пенную реакцию на { NATURALFIXED($duration, 3) } { $duration ->
        [one] секунду
        [few] секунды
       *[other] секунд
    }

entity-effect-guidebook-add-to-solution-reaction =
    { $chance ->
        [1] Заставляет
       *[other] заставляют
    } химикаты, применённые к объекту, добавиться во внутренний контейнер для растворов этого объекта

entity-effect-guidebook-artifact-unlock =
    { $chance ->
        [1] Помогает
       *[other] помогают
    } разблокировать инопланетный артефакт.

entity-effect-guidebook-artifact-durability-restore =
    Восстанавливает { $restored } { $restored ->
        [1] прочность
       *[other] прочности
    } активного узла космического артефакта.

entity-effect-guidebook-plant-attribute =
    { $chance ->
        [1] Изменяет
       *[other] изменяют
    } { $attribute } за {$positive ->
        [true] [color=red]{$amount}[/color]
        *[false] [color=green]{$amount}[/color]
    }

entity-effect-guidebook-plant-cryoxadone =
    { $chance ->
        [1] Омолаживает
       *[other] омолаживают
    } растение, в зависимости от возраста растения и времени его роста

entity-effect-guidebook-plant-phalanximine =
    { $chance ->
        [1] Восстанавливает
       *[other] восстанавливают
    } жизнеспособность растения, ставшего нежизнеспособным в результате мутации

entity-effect-guidebook-plant-diethylamine =
    { $chance ->
        [1] Повышает
       *[other] повышают
    } продолжительность жизни растения и/или его базовое здоровье с шансом 10% на единицу

entity-effect-guidebook-plant-robust-harvest =
    { $chance ->
        [1] Повышает
       *[other] повышают
    } потенцию растения путём { $increase } до максимума в { $limit }. Приводит к тому, что растение теряет свои семена, когда потенция достигает { $seedlesstreshold }. Попытка повысить потенцию свыше { $limit } может вызвать снижение урожайности с вероятностью 10%

entity-effect-guidebook-plant-seeds-add =
    { $chance ->
        [1] Восстанавливает
       *[other] восстанавливают
    } семена растения

entity-effect-guidebook-plant-seeds-remove =
    { $chance ->
        [1] Убирает
       *[other] убирают
    } семена из растения
# SS220 fix loc ids end

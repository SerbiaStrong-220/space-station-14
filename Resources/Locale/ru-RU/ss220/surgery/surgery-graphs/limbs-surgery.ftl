# © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

surgery-leftarm-complex-name = Хирургия левой руки
surgery-rightarm-complex-name = Хирургия правой руки
surgery-lefthand-complex-name = Хирургия левой кисти
surgery-righthand-complex-name = Хирургия правой кисти
surgery-leftleg-complex-name = Хирургия левой ноги
surgery-rightleg-complex-name = Хирургия правой ноги
surgery-leftfoot-complex-name = Хирургия левой стопы
surgery-rightfoot-complex-name = Хирургия правой стопы

surgery-limb-complex-description = Позволяет вправить вывих (костоправом без разреза),
    лечить трещины, открытые переломы, разрывы артерий, некроз и извлекать осколки,
    ампутировать и пришивать конечность.
surgery-hand-complex-description = Позволяет вправить вывих (костоправом без разреза),
    лечить переломы и поверхностные раны кисти, ампутировать и пришивать кисть.
surgery-foot-complex-description = Позволяет вправить вывих (костоправом без разреза),
    лечить переломы и поверхностные раны стопы, ампутировать и пришивать стопу.

surgery-limb-complex-inventory-requirement-description = Конечность пациента не должна быть закрыта одеждой
surgery-limb-complex-inventory-requirement-failure = Вы не можете добраться до конечности { THE($target) }!
surgery-hand-complex-inventory-requirement-description = На руках не должно быть перчаток
surgery-hand-complex-inventory-requirement-failure = Снимите перчатки с { THE($target) }!
surgery-foot-complex-inventory-requirement-description = На ногах не должно быть обуви
surgery-foot-complex-inventory-requirement-failure = Снимите обувь с { THE($target) }!

surgery-limb-examine-start = Пациент готов к операции на конечности.
to-limb-skin-incision = Сделать надрез на конечности
to-limb-skin-clamp-bleeders = Зажать кровоточащие сосуды
to-retract-limb-skin = Расширить разрез
to-seal-limb-incision = Зашить разрез на конечности
to-amputate-limb = Ампутировать конечность
to-amputate-limb-shears = Отсечь конечность ножницами
to-attach-limb = Пришить конечность

limb-amputated-examine-description = На месте конечности свежая культя.
limb-amputated-popup = {THE($user)} отсекает конечность {THE($target)}, используя {$used}
limb-attached-examine-description = Конечность пришита, но разрез ещё не закрыт.
limb-attached-popup = {THE($user)} пришивает конечность к телу {THE($target)}

remove-limb-bullet-fragments = Извлечь инородное тело из конечности
surgery-pathology-requirement-description-limb-fragments = Инородное тело в этой конечности
surgery-pathology-requirement-failure-limb-fragments = Инородных тел в этой конечности не обнаружено!

start-leftarm-surgery = Подготовка операции на левой руке
leftarm-skin-incision = Надрез на левой руке
leftarm-skin-clamp-bleeders = Остановка кровотечения на левой руке
retract-leftarm-skin = Расширение разреза на левой руке
leftarm-compound-reset = Вправление отломков кости левой руки
leftarm-arterial-clamped = Пережатие артерии левой руки
leftarm-necrosis-excised = Иссечение некроза левой руки
leftarm-amputated = Ампутация левой руки
leftarm-attached = Пришивание левой руки

start-rightarm-surgery = Подготовка операции на правой руке
rightarm-skin-incision = Надрез на правой руке
rightarm-skin-clamp-bleeders = Остановка кровотечения на правой руке
retract-rightarm-skin = Расширение разреза на правой руке
rightarm-compound-reset = Вправление отломков кости правой руки
rightarm-arterial-clamped = Пережатие артерии правой руки
rightarm-necrosis-excised = Иссечение некроза правой руки
rightarm-amputated = Ампутация правой руки
rightarm-attached = Пришивание правой руки

start-lefthand-surgery = Подготовка операции на левой кисти
lefthand-skin-incision = Надрез на левой кисти
lefthand-skin-clamp-bleeders = Остановка кровотечения на левой кисти
retract-lefthand-skin = Расширение разреза на левой кисти
lefthand-compound-reset = Вправление отломков кости левой кисти
lefthand-amputated = Ампутация левой кисти
lefthand-attached = Пришивание левой кисти

start-righthand-surgery = Подготовка операции на правой кисти
righthand-skin-incision = Надрез на правой кисти
righthand-skin-clamp-bleeders = Остановка кровотечения на правой кисти
retract-righthand-skin = Расширение разреза на правой кисти
righthand-compound-reset = Вправление отломков кости правой кисти
righthand-amputated = Ампутация правой кисти
righthand-attached = Пришивание правой кисти

start-leftleg-surgery = Подготовка операции на левой ноге
leftleg-skin-incision = Надрез на левой ноге
leftleg-skin-clamp-bleeders = Остановка кровотечения на левой ноге
retract-leftleg-skin = Расширение разреза на левой ноге
leftleg-compound-reset = Вправление отломков кости левой ноги
leftleg-arterial-clamped = Пережатие артерии левой ноги
leftleg-necrosis-excised = Иссечение некроза левой ноги
leftleg-amputated = Ампутация левой ноги
leftleg-attached = Пришивание левой ноги

start-rightleg-surgery = Подготовка операции на правой ноге
rightleg-skin-incision = Надрез на правой ноге
rightleg-skin-clamp-bleeders = Остановка кровотечения на правой ноге
retract-rightleg-skin = Расширение разреза на правой ноге
rightleg-compound-reset = Вправление отломков кости правой ноги
rightleg-arterial-clamped = Пережатие артерии правой ноги
rightleg-necrosis-excised = Иссечение некроза правой ноги
rightleg-amputated = Ампутация правой ноги
rightleg-attached = Пришивание правой ноги

start-leftfoot-surgery = Подготовка операции на левой стопе
leftfoot-skin-incision = Надрез на левой стопе
leftfoot-skin-clamp-bleeders = Остановка кровотечения на левой стопе
retract-leftfoot-skin = Расширение разреза на левой стопе
leftfoot-compound-reset = Вправление отломков кости левой стопы
leftfoot-amputated = Ампутация левой стопы
leftfoot-attached = Пришивание левой стопы

start-rightfoot-surgery = Подготовка операции на правой стопе
rightfoot-skin-incision = Надрез на правой стопе
rightfoot-skin-clamp-bleeders = Остановка кровотечения на правой стопе
retract-rightfoot-skin = Расширение разреза на правой стопе
rightfoot-compound-reset = Вправление отломков кости правой стопы
rightfoot-amputated = Ампутация правой стопы
rightfoot-attached = Пришивание правой стопы

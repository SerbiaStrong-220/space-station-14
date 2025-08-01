zone-command-control-window-desc = Открывает окно управления зонами

zone-command-change-zone-desc = Изменяет параметры указанной зоны
zone-command-change-zone-uid-hint = Uid зоны для изменения

zone-commnad-create-zone-desc = Создаёт новую зону с заданными параметрами

zone-commnad-delete-zone-desc = Удаляет зону
zone-command-delete-zone-uid-hint = Uid зоны для удаления

zone-commnad-delete-zones-container-desc = Удаляет компонент контейнера и все зоны которые он содержал
zone-command-delete-zones-container-uid-hint = Uid энтити-контейнера

zone-commnad-recalc-regions-container-desc = Пересчитывает размеры прямоугольников зоны, удаляя пересечения и, по возможности, объединяя в наименьшее кол-во прямоугольников
zone-command-recalc-regions-uid-hint = Uid зоны для пересчёта регионов

zone-command-params-types-array = Параметры зоны в произвольном порядке:

    'container=uid' - uid энтити-контейнера зоны.

    'originalregion="(left, bottom, right, top); (left2, bottom2, right2, top2); ..."' - Размеры оригинального региона зоны.
    Представляет собой список из прямоугольников, которые создаются между двумя точками,
    где вместо left - координата 1й точки по X, bottom - координата 1й точки по Y, right - координата 2й точки по X, top - координата 2й точки по Y.

    'name=имя' - Наименование зоны (отображается в ui управления)

    'protoid=id' - id энтити-прототипа зоны

    'color=hex' - Цвет зоны в формате hex

    'attachtogrid=true | false' - Привязка к локальной сетке

    'cutspaceoption=None | Dinamic | Always' - Параметр вырезания космоса.
    Работает только с гридами, т.к. у мапп не может быть тайлов!
    None - Размер зоны никак не изменяется от наличия тайлов под ней.
    Dinamic - Вырезает из активного региона зоны (ActiveRegion) все участки находящиеся в космосе, при этом не изменяя оригинальный регион (OriginalRegion). Обновляется при строительстве/удалении тайла(-ов) в пределах зоны.
    Always - Вырезает из оригинального региона зоны (OriginalRegion) все участки находящиеся в космосе. Обновляется при строительстве/удалении тайла(-ов) в пределах зоны, при этом не возвращая ранее вырезанные участки (даже если на их месте уже есть тайл).

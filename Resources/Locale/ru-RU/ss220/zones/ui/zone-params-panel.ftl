zone-params-panel-name-label = Имя:
zone-params-panel-name-tooltip = Наименование зоны, которое будет отображаться в ui управления
zone-params-panel-prototype-id-label = ID прототипа:
zone-params-panel-prototype-id-tooltip = ID энтити-прототипа, который будет создан для обработки взаимодействий (энтити-ивентов) с зоной
zone-params-panel-container-net-id-label = uid контейнера:
zone-params-panel-container-net-id-tooltip = Uid энтити-контейнера, к которому закреплена зона.
    Расчет позиции регрионов зоны ведётся в локальных координатах относительно центра контейнера.
    Контейнером могут быть либо энтити-маппа (MapComponent) либо энтити-грид (MapGridComponent)

zone-params-panel-hex-color-label = Цвет:
zone-params-panel-hex-color-tooltip = Цвет зоны, который будет использоваться в оверлее
zone-params-panel-add-box-button = Добавить
zone-params-panel-cut-box-button = Вырезать
zone-params-panel-show-changes-button = Показать изменения
zone-params-panel-attach-to-grid-check-box = Привязка к сетке
zone-params-panel-attach-to-grid-tooltip = Если включено - то координаты углов регионов зоны будут привязаны к локальной сетке координат
zone-params-panel-cut-space-option-label = Вырезать космос
zone-params-panel-cut-space-option-tooltip = Параметр вырезания космоса.
    Работает только с гридами, т.к. у мапп не может быть тайлов!

    Нет - Размер зоны никак не изменяется от наличия тайлов под ней.

    Динамически - Вырезает из активного региона зоны (ActiveRegion) все участки находящиеся в космосе, при этом не изменяя оригинальный регион (OriginalRegion). Обновляется при строительстве/удалении тайла(-ов) в пределах зоны.

    Навсегда - Вырезает из оригинального региона зоны (OriginalRegion) все участки находящиеся в космосе. Обновляется при строительстве/удалении тайла(-ов) в пределах зоны, при этом не возвращая ранее вырезанные участки (даже если на их месте уже есть тайл).

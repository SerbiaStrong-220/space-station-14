zones-control-are-you-sure = Вы уверены?

zones-control-window-name = Управление зонами
zones-control-search-line-placeholder = Поиск...
zones-control-refresh-button = Обновить
zones-control-create-new-zone-button = Создать новую зону
zones-control-overlay-button = Оверлей
zones-control-create-button = Создать
zones-control-cancel-button = Отмена
zones-control-apply-button = Применить
zones-control-recreate-warning-text = Внимание! В связи с изменением id контейнера зоны и/или id прототипа зоны будет создана новая зона с заданными параметрами, а текущая - будет удалена!

zone-warning-window-name = Предупреждение
zone-warning-window-confirm-button = Подтвердить
zone-warning-window-cancel-button = Отмена

zone-container-entry-delete-container-button = Удалить

zone-state-box-name-label = Имя:
zone-state-box-name-tooltip = Наименование зоны, которое будет отображаться в ui управления
zone-state-box-prototype-id-label = ID прототипа:
zone-state-box-prototype-id-tooltip = ID энтити-прототипа, который будет создан для обработки взаимодействий (энтити-ивентов) с зоной
zone-state-box-container-net-id-label = uid контейнера:
zone-state-box-container-net-id-tooltip = Uid энтити-контейнера, к которому закреплена зона.
                                        Расчет позиции регрионов зоны ведётся в локальных координатах относительно центра контейнера.
                                        Контейнером могут быть либо энтити-маппа (MapComponent) либо энтити-грид (MapGridComponent)
zone-state-box-hex-color-label = Цвет:
zone-state-box-hex-color-tooltip = Цвет зоны, который будет использоваться в оверлее
zone-state-box-add-box-button = Добавить
zone-state-box-cut-box-button = Вырезать
zone-state-box-show-changes-button = Показать изменения
zone-state-box-attach-to-grid-check-box = Привязка к сетке
zone-state-box-attach-to-grid-tooltip = Если включено - то координаты углов регионов зоны будут привязаны к локальной сетке координат
zone-state-box-cut-space-option-label = Вырезать космос
zone-state-box-cut-space-option-tooltip = Параметр вырезания космоса
                                        Работает только с гридами, т.к. у мапп не может быть тайлов!

                                        Нет - Размер зоны никак не изменяется от наличия тайлов под ней.

                                        Динамически - Вырезает из активного региона зоны (ActiveRegion) все участки находящиеся в космосе, при этом не изменяя оригинальный регион (OriginalRegion). Обновляется при строительстве/удалении тайла(-ов) в пределах зоны.

                                        Навсегда - Вырезает из оригинального региона зоны (OriginalRegion) все участки находящиеся в космосе. Обновляется при строительстве/удалении тайла(-ов) в пределах зоны, при этом не возвращая ранее вырезанные участки (даже если на их месте уже есть тайл).

zone-cut-space-option-None = Нет
zone-cut-space-option-Dinamic = Динамически
zone-cut-space-option-Forever = Навсегда

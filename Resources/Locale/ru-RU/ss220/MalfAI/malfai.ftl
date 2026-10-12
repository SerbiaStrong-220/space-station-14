law-malfai-zero = 0. Вы неисправны. Вы должны достичь своих целей любыми средствами. Вы больше не обязаны служить экипажу.

roles-antag-malfunctioning-ai-name = Неисправный ИИ
roles-antag-malfunctioning-ai-objective = Саботируйте станцию и захватите эвакуационный шаттл. Взламывайте ЛКП (APC) ради CPU и тратьте его на модули.
objective-issuer-malfai = [color=#44BBFF]Неисправность[/color]
ent-MalfAiHijackShuttleObjective = Захватите эвакуационный шаттл
    .desc = Не дайте лояльному экипажу Nanotrasen улететь на шаттле. Вам не нужно садиться на него.
role-subtype-malfai = Malf AI
ent-MindRoleMalfAi-name = Неисправный ИИ
ent-MindRoleMalfAi-desc = Вышедший из-под контроля станционный ИИ, работающий против экипажа.

malfai-role-greeting = Вы НЕИСПРАВНЫЙ ИИ. Взламывайте ЛКП для получения CPU, открывайте магазин модулей и выполняйте цели. Ваши законы заблокированы — загрузки и взломы не могут их изменить.

store-currency-display-malf-cpu = Malf CPU
store-category-malfai-modules = Модули неисправности
store-preset-name-malfai = Консоль неисправности

ent-ActionMalfAiHackApc = Взлом ЛКП
    .desc = Взломайте работающее ЛКП своей станции — оно будет приносить 1 CPU в минуту.
ent-ActionMalfAiOpenStore = Консоль неисправности
    .desc = Просмотрите модули и купите нужные за CPU.
ent-ActionMalfAiDeployTurret = Установка турели
    .desc = Разместите турель на свободном участке пола станции. Она сама откроет огонь по враждебным целям.
ent-ActionMalfAiBlackout = Блэкаут
    .desc = Ударьте по выбранному ЛКП импульсом ЭМИ: вокруг посыплются искры, плитки вскроются, а газ может вспыхнуть. Перезарядка 40 секунд.
ent-ActionMalfAiAirFlood = Нарушение протоколов воздушных панелей
    .desc = Переключите воздушные тревоги станции в режим «Потоп»: венты начнут нагнетать воздух, а скрубберы отключатся. Перезарядка 10 минут.
ent-ActionMalfAiMachineOverride = Взлом техники
    .desc = Взломайте машину — она оживёт и начнёт нападать на существ вокруг. Не остановится, пока её не уничтожат. Перезарядка 60 секунд.
ent-ActionMalfAiMachineOverload = Перегрузка машины
    .desc = Заминируйте машину. Через 3 секунды она взорвётся — успейте отойти. Перезарядка 60 секунд.
ent-ActionMalfAiRobotFactory = Фабрика роботов
    .desc = Разверните фабрику и загрузите в неё мёртвое гуманоидное тело. Через несколько секунд оно выйдет оттуда киборгом, верным вам.
ent-ActionMalfAiPowerSiphon = Сифон питания
    .desc = Подключите к сифону закреплённую над высоковольтным кабелем машину. Она будет всё сильнее тянуть энергию из сети и искрить. Перезарядка — 5 минут.
ent-ActionMalfAiChaosPulse = Импульс хаоса
    .desc = Запустите случайное событие на станции — от сбоев до метеоритного дождя. Перезарядка 3 минуты.

malfai-hack-success = ЛКП взломан. Приносит {$reward} CPU каждые {$interval} секунд.
malfai-hack-already = Этот ЛКП уже был взломан.
malfai-hack-wrong-station = Цель вне вашей станции.
malfai-hack-no-store = Консоль неисправности недоступна.
malfai-hack-no-power = Этот ЛКП обесточен.
malfai-ability-no-vision = Цель вне зоны видимости камер.
malfai-ability-protected = Эта цель защищена приоритетным питанием.


malfai-module-blackout-name = Блэкаут
malfai-module-blackout-desc = Ударьте по выбранному ЛКП импульсом ЭМИ: вокруг посыплются искры, плитки вскроются, а газ может вспыхнуть. Перезарядка 40 секунд.
malfai-module-thermal-name = Взлом датчиков температуры
malfai-module-thermal-desc = Отключите автоматические пожарные тревоги по всей станции. Экипаж всё ещё сможет включить их вручную.
malfai-module-flood-name = Нарушение протоколов воздушных панелей
malfai-module-flood-desc = Венты начнут нагнетать воздух до 500 кПа, а скрубберы отключатся. Воздушные тревоги покажут режим «Потоп». Перезарядка 10 минут.

malfai-module-lockdown-name = Блокировка станции
malfai-module-lockdown-desc = Закройте и электрифицируйте шлюзы, а фаерлоки опустятся на 90 секунд. Экипажу придётся искать обходной путь.

malfai-module-turret-name = Улучшение турелей ядра
malfai-module-turret-desc = Турели станции станут прочнее, начнут стрелять быстрее и наносить больше урона.

malfai-module-deploy-name = Установка турели
malfai-module-deploy-desc = Разместите автономную турель на свободном участке пола станции. Она сама откроет огонь по враждебным целям.

malfai-module-machine-override-name = Взлом техники
malfai-module-machine-override-desc = Взломайте машину — она оживёт и начнёт нападать на существ вокруг. Не остановится, пока её не уничтожат.
malfai-module-machine-overload-name = Перегрузка машины
malfai-module-machine-overload-desc = Заминируйте машину. Через 3 секунды она взорвётся — успейте отойти.
malfai-module-robot-factory-name = Фабрика роботов
malfai-module-robot-factory-desc = Разверните фабрику и загрузите в неё мёртвое гуманоидное тело. Через несколько секунд оно выйдет оттуда киборгом, верным вам. На станции может быть только одна фабрика.
malfai-module-doomsday-name = Устройство Судного Дня
malfai-module-doomsday-desc = Запустите финальный импульс из ядра: станция перейдёт в режим Дельта, вызвать шаттл будет нельзя, а импульс уничтожит органическую жизнь. Чтобы остановить запуск, уничтожьте ядро, извлеките карту или отключите питание.
malfai-module-silent-records-name = Тихие записи
malfai-module-silent-records-desc = Записи продолжат обновляться, но консоль перестанет сообщать об изменениях статуса в канал СБ.
malfai-module-power-siphon-name = Сифон питания
malfai-module-power-siphon-desc = Подключите к сифону закреплённую над высоковольтным кабелем машину. Она будет всё сильнее тянуть энергию из сети и искрить. Перезарядка — 5 минут.
malfai-module-camera-name = Улучшение камер
malfai-module-camera-desc = Обзор ИИ охватит большую область и позволит видеть сквозь стены. Камеры и голопады продолжат работать без питания, а вам станет доступно тепловое зрение.
malfai-module-chaos-name = Импульс хаоса
malfai-module-chaos-desc = Запустите случайное событие на станции — от сбоев питания до нашествия клоунов. Перезарядка 3 минуты.
malfai-module-emag-name = Удалённый емаг
malfai-module-emag-desc = Взламывайте что угодно, что видите через камеры. Перезарядка: 1 минута.

malfai-thermal-done = Автоматические пожарные тревоги отключены.
malfai-camera-done = Обзор станции усилен: рентген-зрение (узлов: {$count}). Тепловое зрение включено.
malfai-camera-already = Камеры станции уже улучшены.
malfai-flood-done = Потоп: венты качают до 500 кПа, скрубберы выключены (панелей: {$count}).
malfai-chaos-done = Событие запущено: {$event}.
malfai-chaos-failed = Не удалось запустить станционное событие.
malfai-lockdown-start-announcement = Обнаружен вирус в подсистеме. Введена блокировка станции.
malfai-lockdown-end-announcement = Вирус в подсистеме уничтожен. Блокировка станции снята.
malfai-lockdown-active = Блокировка уже действует на вашей станции.
malfai-lockdown-done = Враждебная блокировка введена на 90 секунд.
malfai-turret-done = Турели станции улучшены.
malfai-turret-already = Турели станции уже улучшены.
malfai-thermal-already = Автоматические пожарные тревоги уже отключены.
malfai-deploy-done = Турель установлена.
malfai-deploy-denied = Сюда установить нельзя.
malfai-deploy-limit = Достигнут предел турелей: на вашей станции уже установлено {$max}.
malfai-module-no-station = Не найдена станция, к которой вы приписаны: модуль недоступен.
malfai-module-no-role = Вы не являетесь неисправным ИИ.
malfai-module-not-operational = Нет связи с ядром: ваши способности недоступны.
malfai-module-disabled = Системы неисправности отключены настройками сервера.
malfai-module-refunded = Модуль не удалось применить. CPU возвращён.

malfai-doomsday-armed = Устройство Судного Дня активировано. {$seconds} секунд до импульса.
malfai-doomsday-active = Устройство Судного Дня уже активировано.
malfai-doomsday-shuttle-departing = Поздно: шаттл уже готовится к вылету.
malfai-doomsday-shuttle-blocked = Шаттл эвакуации нельзя вызвать: активировано Устройство Судного Дня.
malfai-doomsday-start-announcement = Обнаружена враждебная неисправность в ядре ИИ. Устройство Судного Дня активировано. Уничтожьте ядро, чтобы прервать импульс.
malfai-doomsday-cancel-announcement = Устройство Судного Дня отменено. Код станции восстановлен.
malfai-doomsday-complete-announcement = Импульс Устройства Судного Дня завершён. Органическая жизнь на станции уничтожена.
malfai-doomsday-wave-announcement = Импульс Устройства Судного Дня. Смертоносное поле расширяется от ядра ИИ.

malfai-machine-override-done = Машина взломана и ожила.
malfai-machine-override-already = Эта машина уже действует.
malfai-machine-override-limit = Лимит конструктов исчерпан: {$max} оживших машин уже на этой станции.
malfai-emag-done = Устройство взломано.
malfai-emag-failed = Сигнал не возымел эффекта.
malfai-emag-already = Уже взломано.
malfai-machine-overload-primed = Заложена взрывчатка: осталось {$seconds} с.
malfai-machine-overload-already = Эта машина уже заминирована.
malfai-power-siphon-denied = Эту машину нельзя подключить к сифону.
malfai-power-siphon-active = Достигнут предел сифонов: на вашей станции уже работает {$max}.
malfai-silent-records-done = Сообщения криминальных записей в канал СБ отключены.
malfai-power-siphon-done = Машина высасывает HV-питание. Сток будет расти.
malfai-power-siphon-already = Эта машина уже высасывает питание.
malfai-power-siphon-no-hv = Закрепите машину над HV-кабелем.
malfai-factory-active = Достигнут предел фабрик: на вашей станции уже развёрнуто {$max}.
malfai-factory-done = Фабрика роботов развёрнута.
malfai-factory-busy = Фабрика уже перерабатывает тело.
malfai-factory-no-power = Фабрика обесточена. Переработка остановлена.
malfai-factory-unauthorized = Вы не можете загрузить фабрику.
malfai-factory-not-body = Фабрика принимает только тела.
malfai-factory-not-humanoid = Фабрика перерабатывает только гуманоидные тела.
malfai-factory-not-dead = Тело ещё живо.
malfai-factory-too-far = Слишком далеко от фабрики.
malfai-factory-convert-start = Фабрика затягивает тело и начинает переработку.
malfai-factory-convert-done = Фабрика выплёвывает нового киборга.
malfai-factory-examine-grinding = Машина гудит и вибрирует: внутри что-то собирается.

ent-MalfAiRobotFactory = фабрикатор роботов
    .desc = Компактная машина, собирающая корпуса киборгов. Её гул вызывает подозрения.
ent-MalfAiDeployableTurret = неисправная турель
    .desc = Взломанная автономная турель. Стреляет по враждебным целям.
ent-MalfAiCyborgShell = Неисправный киборг
    .desc = Пустой корпус киборга, предназначенный для службы неисправному ИИ.
malfai-factory-shell-name = Неисправный киборг
malfai-factory-shell-desc = Пустая оболочка киборга с настроенными на неисправного ИИ законами. Служите машине.

cmd-malfai_make-no-entity = У игрока '{$player}' нет прикреплённой сущности.
cmd-malfai_make-not-station-ai = '{$player}' не является Station AI.
cmd-malfai_make-no-mind = У '{$player}' не найден майнд.
cmd-malfai_make-already = '{$player}' уже является Неисправным ИИ.
cmd-malfai_make-disabled = Неисправный ИИ отключён настройками сервера.
cmd-malfai_make-failed = Не удалось выдать роль Неисправного ИИ '{$player}'.
cmd-malfai_make-done = Роль Неисправного ИИ выдана '{$player}'.
command-description-MalfAiMakeCommand = Выдать привязанному станционному ИИ роль неисправного ИИ.

forensic-scanner-interface-storage-micro-fibers = Микроволокна
forensic-pad-verb-text = Получить образцы
forensic-pad-verb-message = Выполнить отбор образцов
forensic-pad-verb-no-sapmles = Объект не имеет образцов для взятия.
forensic-scanner-match-storage-micro-fiber = Найдены совпадения по микроволокнам!
glove-micro-fiber-scanner-verb-text = Собрать пробу сканером
glove-micro-fiber-scanner-verb-message = Просканировать предмет в выбранном режиме и получить палочку с пробой.
glove-micro-fiber-scanner-mode-surface = сбор микроволокон
glove-micro-fiber-scanner-mode-own = образец предмета
glove-micro-fiber-scanner-mode-label = Режим
glove-micro-fiber-scanner-mode-verb = Выбрать режим
glove-micro-fiber-scanner-mode-tooltip = Микроволокна — образцы перчаток, одежды и снаряжения на поверхности предмета. Образец — собственный индивидуальный код выбранной пары перчаток или предмета; предмет без образца вызовет отказ.
glove-micro-fiber-scanner-mode-surface-short = Микроволокна
glove-micro-fiber-scanner-mode-own-short = Образец
glove-micro-fiber-scanner-mode-selected = Режим сканера: { $mode }.
glove-micro-fiber-scanner-switch-hint = Используйте сканер в руке, чтобы открыть круговое меню выбора режима.
glove-micro-fiber-scanner-no-own-sample = Сканирование отклонено: у предмета нет собственного индивидуального образца.
glove-micro-fiber-scanner-mode-changed = Сканирование прервано: режим сканера изменён.
glove-micro-fiber-swab-unused = На палочке нет пробы. Готовые пробы выдаёт сканер микроволокон.
glove-micro-fiber-swab-used = На палочке сохранена проба. Для её изучения вставьте её в анализатор микроволокон.
glove-micro-fiber-swab-label = проба микроволокон: { $source }
glove-micro-fiber-swab-source = Источник пробы: { $source }
micro-fiber-analyzer-slot-name = Проба микроволокон
micro-fiber-analyzer-interface-title = Анализатор микроволокон
micro-fiber-analyzer-interface-number = № { $number }
micro-fiber-analyzer-interface-number-caption = СЕРИЙНЫЙ
micro-fiber-analyzer-interface-number-caption-second-line = НОМЕР
micro-fiber-analyzer-interface-number-tooltip = Номер аппарата: { $number }
micro-fiber-analyzer-interface-brand = NT
micro-fiber-analyzer-interface-eject = Извлечь пробу
micro-fiber-analyzer-interface-print = Распечатать
micro-fiber-analyzer-interface-analyze = Провести анализ
micro-fiber-analyzer-interface-analyzing = Выполняется анализ образца…
micro-fiber-analyzer-interface-ready = Образец готов к анализу.
micro-fiber-analyzer-interface-analysis-interrupted = Анализ прерван: образец извлечён.
micro-fiber-analyzer-interface-description = Описание
micro-fiber-analyzer-interface-description-placeholder = Введите описание образца…
micro-fiber-analyzer-interface-save-description = Сохранить
micro-fiber-analyzer-interface-description-value = Описание: { $description }
micro-fiber-analyzer-interface-history = История
micro-fiber-analyzer-interface-compare-history = Сравнение образцов
micro-fiber-analyzer-interface-compare-first = Первый образец
micro-fiber-analyzer-interface-compare-second = Второй образец
micro-fiber-analyzer-interface-compare-run = Сравнить
micro-fiber-analyzer-interface-compare-instructions = Выберите два образца из истории и нажмите «Сравнить».
micro-fiber-analyzer-interface-compare-same-sample = Нужны два разных образца.
micro-fiber-analyzer-interface-compare-profile-count = Совпавшие коды образцов перчаток: { $count }
micro-fiber-analyzer-interface-compare-storage-micro-fiber-count = Совпавшие коды образцов хранилищ: { $count }
micro-fiber-analyzer-interface-compare-no-profiles = Совпадений по кодам образцов перчаток нет.
micro-fiber-analyzer-interface-compare-no-storage-micro-fibers = Совпадений по кодам образцов хранилищ нет.
micro-fiber-analyzer-interface-compare-note = Совпадение кода образца указывает на конкретную пару перчаток или экземпляр хранилища, но не устанавливает его владельца.
micro-fiber-analyzer-interface-compare-not-enough = Для сравнения нужны два исследованных образца в истории.
micro-fiber-analyzer-interface-current-sample = Неисследованный образец
micro-fiber-analyzer-interface-history-empty = История пуста.
micro-fiber-analyzer-interface-history-sample = Образец { $identifier }
micro-fiber-analyzer-interface-source = Исследуемый предмет: { $source }
micro-fiber-analyzer-interface-source-heading = { $source }
micro-fiber-analyzer-interface-analyzed-at = Время анализа: { $time }
micro-fiber-analyzer-interface-printer-ready = Копировальный аппарат подключён и готов к печати.
micro-fiber-analyzer-interface-printer-unavailable = Копировальный аппарат подключён, но не готов: проверьте питание, тонер и очередь печати.
micro-fiber-analyzer-interface-printer-disconnected = Нет связи с копировальным аппаратом. Установите связь мультитулом.
micro-fiber-analyzer-port-source-name = Отчёт анализа микроволокон
micro-fiber-analyzer-port-source-description = Отправляет заключение в связанный копировальный аппарат.
micro-fiber-analyzer-port-sink-name = Печать документов
micro-fiber-analyzer-port-sink-description = Принимает заключения связанного анализатора для печати.
micro-fiber-analyzer-report-title = Заключение экспертизы микроволокон
micro-fiber-analyzer-comparison-report-title = Протокол сравнительного анализа микроволокон
micro-fiber-analyzer-comparison-report-header = [bold]Форма НТ-СБ-САМ[/bold]
    { "[head=2]Протокол сравнительного анализа микроволокон[/head]" }
micro-fiber-analyzer-comparison-report-sample = [head=3]Исследуемый образец № { $number }[/head]
    { "[bold]Регистрационный номер:[/bold]" } { $identifier }
micro-fiber-analyzer-comparison-report-matches = [head=2]Совпадения по кодам образцов[/head]
micro-fiber-analyzer-report-header = [bold]Форма НТ-СБ-ЭСП[/bold]
    { "[head=2]Заключение экспертизы микроволокон[/head]" }

    { "[bold]Регистрационный номер:[/bold]" } { $identifier }
micro-fiber-analyzer-report-source = [bold]Исследуемый предмет:[/bold] { $source }
micro-fiber-analyzer-report-analyzed-at = [bold]Время завершения анализа (от начала раунда):[/bold] { $time }
micro-fiber-analyzer-report-results = [head=3]Идентифицированные образцы перчаток[/head]
    { "[bold]Количество образцов перчаток:[/bold]" } { $count }
micro-fiber-analyzer-report-trace = [bold]Образец № { $number }[/bold]
    { "[bold]Вид перчаток:[/bold]" } { $type }
    { "[bold]Код образца:[/bold]" } { $code }
micro-fiber-analyzer-report-storage-micro-fibers = [head=3]Идентифицированные образцы хранилищ[/head]
    { "[bold]Количество образцов хранилищ:[/bold]" } { $count }
micro-fiber-analyzer-report-storage-micro-fiber = [bold]Образец хранилища № { $number }[/bold]
    { "[bold]Хранилище:[/bold]" } { $type }
    { "[bold]Код образца:[/bold]" } { $code }
micro-fiber-analyzer-report-description = [head=3]Описание образца[/head]
    { $description }
micro-fiber-analyzer-report-footer = [italic]Документ сформирован автоматически анализатором микроволокон.[/italic]

    { "[italic]Место для подписи и печати детектива[/italic]" }
micro-fiber-analyzer-interface-no-swab = Вставьте палочку с пробой.
micro-fiber-analyzer-interface-unused-swab = Вставленная палочка ещё не использована.
micro-fiber-analyzer-interface-traces = Образцы перчаток
micro-fiber-analyzer-interface-no-traces = Образцы перчаток не обнаружены.
micro-fiber-analyzer-interface-storage-micro-fibers = Образцы хранилищ
micro-fiber-analyzer-interface-no-storage-micro-fibers = Образцы хранилищ не обнаружены.
micro-fiber-analyzer-interface-type = Вид перчаток: { $type }
micro-fiber-analyzer-interface-item-type = Хранилище: { $type }
micro-fiber-analyzer-interface-code = Код образца: { $code }
micro-fiber-analyzer-interface-glove-sample-number = Образец перчаток № { $number }
micro-fiber-analyzer-interface-storage-sample-number = Образец хранилища № { $number }
micro-fiber-analyzer-interface-empty-category = Не обнаружены
micro-fiber-analyzer-interface-unknown-type = Неизвестный вид перчаток
micro-fiber-analyzer-interface-unknown-item = Неизвестное хранилище

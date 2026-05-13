cmd-atvrange-desc = Устанавливает диапазон отладки атмосферы (в виде двух чисел с плавающей запятой, начало [красный] и конец [синий]).

cmd-atvrange-help = Использование: { $command } <начало> <конец>

cmd-atvrange-error-start = Неверное число с плавающей запятой START

cmd-atvrange-error-end = Неверное число с плавающей запятой END

cmd-atvrange-error-zero = Масштаб не может быть равен нулю, так как это вызовет деление на ноль в AtmosDebugOverlay.

cmd-atvmode-desc = Устанавливает режим отладки атмосферы. Это автоматически сбросит масштаб.

cmd-atvmode-help = Использование: {$command} <TotalMoles/GasMoles/Temperature> [<gas ID (для GasMoles)>]

cmd-atvmode-error-invalid = Неверный режим

cmd-atvmode-error-target-gas = Для этого режима необходимо указать целевой газ.

cmd-atvmode-error-out-of-range = ID газа не распознается или вне диапазона.

cmd-atvmode-error-info = Для этого режима дополнительная информация не требуется.

cmd-atvcbm-desc = Изменяет цвета с красного/зеленого/синего на оттенки серого.

cmd-atvcbm-help = Использование: {$command} <true/false>

cmd-atvcbm-error = Неверный флаг

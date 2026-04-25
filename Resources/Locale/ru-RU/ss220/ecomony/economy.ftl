guide-entry-economy = Экономика

ent-SpaceCashWithdrawalOnSpawn = снять деньги с банковского счета
    .desc = { ent-SpaceCash.desc }
ent-EconomyEFTPOS = терминал безналичной оплаты
    .desc = Проведите картой для совершения покупок без использования наличных.
ent-TransactionReceiptPaper = чек транзакции
    .desc = Распечатанный чек транзакции терминала безналичной оплаты.
ent-EconomyBankingCartridge = картридж банкинга
    .desc = Программа для управления банковским аккаунтом.
ent-EconomyATM = банкомат
    .desc = Для одних он забирает деньги, для других — отдаёт.

cmd-economy_getdetails-desc = Отображает все данные банковского счета, если он существует.
cmd-economy_changebalance-desc = Изменяет сумму денег на счете на указанную.

economy-ponder-for-data-verb-text = Вспомнить ПИН-код
economy-ponder-for-data-failed = Я не могу вспомнить детали своего банковского счёта...
economy-ponder-for-data-success = Мой банковский счёт { $accountId } и ПИН-код { $accountPin }.

economy-eftpos-reset-self = Вы сбрасываете настройки терминала.
economy-eftpos-reset-others = { $user } сбрасывает настройки терминала.

economy-eftpos-ui-title = Терминал Безналичной Оплаты
economy-eftpos-ui-payment-tab = Оплата
economy-eftpos-ui-settings-tab = Настройки
economy-eftpos-ui-amount-default = Терминал готов к работе
economy-eftpos-ui-amount-label = Сумма к оплате:
economy-eftpos-ui-amount-text = { $amount }$
economy-eftpos-ui-payment-await-card-text = Приложите карту
economy-eftpos-ui-payment-await-pin-text = Введите ПИН-код
economy-eftpos-ui-pin-code-label = ПИН-код:
economy-eftpos-ui-receipt-print-label = Распечатать чек после оплаты:
economy-eftpos-ui-receipt-print-yes = да
economy-eftpos-ui-receipt-print-no = нет
economy-eftpos-ui-card-lock-text = Заблокировать
economy-eftpos-ui-card-unlock-text = Разблокировать
economy-eftpos-ui-card-lock-desc = Проведите картой для блокировки
economy-eftpos-ui-card-unlock-desc = Проведите картой для разблокировки
economy-eftpos-ui-account-id-label = Банковский счёт:
economy-eftpos-ui-account-id-empty = Не привязан
economy-eftpos-ui-account-name-text = Владелец: { $ownerName }
economy-eftpos-ui-window-flavor-left = Получение средств не гарантируем
economy-eftpos-ui-window-flavor-right = v3.4.8

economy-eftpos-transaction-success = Транзакция успешна
economy-eftpos-transaction-error = Ошибка транзакции

economy-eftpos-receipt-begin = [bold]Платёжная операция:[/bold]
economy-eftpos-receipt-owner = [bold]Продавец:[/bold]
economy-eftpos-receipt-payer = [bold]Покупатель:[/bold]
economy-eftpos-receipt-amount = Сумма: { $amount }$
economy-eftpos-receipt-time = Дата и время: { $dateTime }
economy-eftpos-receipt-account-id = Банковский счёт: { $accountId }
economy-eftpos-receipt-account-name = Владелец счёта: { $name }

economy-banking-program-name = Банкинг
banking-program-ui-card-default = Загрузка...
banking-program-ui-card-absent = Для корректной работы приложения — вставьте карту.
banking-program-ui-card-invalid = Банковский счёт к данной карте — не привязан.
banking-program-ui-account-id-text = Банковский счёт: { $accountId }
banking-program-ui-account-name-text = Владелец счёта: { $name }
banking-program-ui-account-balance-text = Баланс счёта: { $balance }$

economy-atm-reset-self = Вы сбрасываете настройки банкомата обратно к заводским.
economy-atm-reset-others = { $user } сбрасывает настройки банкомата обратно к заводским.

economy-atm-insert-verb = Вставить карту
economy-atm-eject-verb = Достать карту
economy-atm-insert-cash-error-popup = Купюроприемник закрыт
economy-atm-wrong-pin = Неверный ПИН-код
economy-atm-ui-title = БАНКОМАТ
economy-atm-ui-select-withdraw-amount = Выберите сумму вывода
economy-atm-ui-no-account = К карте не привязан аккаунт
economy-atm-ui-no-account-unemployed = В базе данных отсутствуют ваши реквизиты
economy-atm-ui-insert-card = Вставьте карту
economy-atm-ui-account-id-text = Банковский счёт: { $accountId }
economy-atm-ui-account-name-text = Владелец счёта: { $name }
economy-atm-ui-account-balance = Баланс счёта: { $balance }$
economy-atm-ui-enter-pin = Введите ПИН-код
economy-atm-ui-withdraw-text = Вывести
economy-atm-ui-pin-code-label = ПИН-код:
economy-bank-ui-link-account = Привязать карту к банковскому счёту
economy-bank-ui-create-account = Создать новый банковский счёт
economy-atm-ui-window-flavor-left = Получение средств не гарантируем
economy-atm-ui-window-flavor-emagged-left = Перевод средств Синдикату гарантируем
economy-atm-ui-window-flavor-right = v3.4.8

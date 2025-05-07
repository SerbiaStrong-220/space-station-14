# Hidden decriptions below
hidden-desc-ThermalGoogles-syndicate = [color=#e31735]Новейшая разработка ваших товарищей по ремеслу, позволяет распозновать тепловые сигнатуры сквозь преграды.[/color]
hidden-desc-ThermalGoogles-research = [color=#D381C9]Неожиданно для себя вы замечаете знакомые электромагнитные сенсоры, когда-то НаноТрейзен занималось изучением возможности ксеноморфов видеть людей сквозь преграды, из открытых работ по этой теме вы и узнаёте сенсоры. Видимо, исследования были успешны, но должна ли была Вам в руки попасть уже засекреченная разработка?[/color]

hidden-desc-ThermalVisorImplanter-research = [color=#D381C9]Сверив номера на имплантере, вы замечаете знакомый шаблон. Когда-то НаноТрейзен исследовала возможность ксеноморфов видеть людей сквозь преграды, открытые испытания тех чипов имели такой же шаблон серийных номеров, видимо доработка была успешна, но должна ли была Вам в руки попасть уже засекреченная разработка?[/color]

hidden-desc-MindShieldImplanter-security = [color=#009ae1]Внутри имплантера находится имплант защиты разума.[/color]
#ss220 trackingimplanter-desc-fix begin
hidden-desc-TrackingImplanter-security = [color=#009ae1]Внутри имплантера находится имплант-трекер.[/color]
#ss220 trackingimplanter-desc-fix end

hidden-desc-MindSlaveImplant-medical =  [color=#52B4E9]Сверив номера импланта внутри, вы понимаете, что это несертифицированный имплант. При внимательном осмотре вы понимаете что этот имплант обладает ИИ, подавляющим разум имплантированного.[/color]
hidden-desc-MindSlaveImplant-research = [color=#D381C9]Внимательно осмотрев имплант, вы понимаете, что это внутри имплант, губительно влияющий на нервную систему.[/color]
hidden-desc-MindSlaveImplant-syndicate = [color=#e31735]Этот имплант подчиняет разум владельца тому, кто установил имлпант. При извлечении импланта контроль над разумом теряется.[/color]

hidden-desc-DnaCopyImplant-medical = [color=#52B4E9]Этот имплант содержит структуры ДНК, что позволяет менять личность.[/color]
hidden-desc-DnaCopyImplant-research = [color=#D381C9]Имплант, позволяющий копировать и изменять ДНК, что дает возможность смены личности.[/color]
hidden-desc-DnaCopyImplant-syndicate = [color=#E31735]Имплант с функцией клонирования ДНК, позволяющий носителю принять новую личность.[/color]

hidden-desc-AdrenalImplant-medical = [color=#52B4E9]Этот имплант содержит медицинские препараты, способствующие восстановлению организма.[/color]
hidden-desc-AdrenalImplant-research = [color=#D381C9]Имплант с запасом лечебных растворов, предназначенных для поддержки жизненных функций.[/color]
hidden-desc-AdrenalImplant-syndicate = [color=#E31735]Имплант с инъекционным механизмом, содержащий жидкости для ускоренного восстановления организма.[/color]

# Regular decriptions below

ent-ThermalVisorImplanter = { ent-Implanter }
    .desc = { ent-Implanter.desc }
    .suffix = Термальное зрение
ent-ThermalVisionImplant = имплант Термального Зрения
    .desc = Микрочип, который вживляют под кожу.

ent-DnaCopyImplanter = { ent-Implanter }
    .desc = { ent-Implanter.desc }
    .suffix = Копирование ДНК
ent-DnaCopyImplant = имплант копирования ДНК
    .desc = Микрочип, который вживляют под кожу.

ent-ActionActivateDnaCopy = Активировать имплант копирования ДНК
    .desc = Активирует процесс копирования ДНК, позволяя носителю изменить свою личность. Можно использовать для скрытия или маскировки.

ent-BoxDnaCopyImplant = { ent-BoxCardboard }
    .desc = { ent-BoxCardboard.desc }

ent-PaperDnaCopyImplanter = { ent-Paper }
    .desc = { ent-Paper.desc }
ent-AdrenalImplanter = { ent-Implanter }
    .desc = { ent-Implanter.desc }
    .suffix = Адреналин

ent-AdrenalImplant = имплант Адреналина
    .desc = Микрочип, который вживляют под кожу.

ent-ActionActivateAdrenalImplant = Ввести жидкость
    .desc = Вводит небольшую дозу разных лечащих веществ, эффект держится около 45 секунд. Можно ввести в предсмертном состоянии
# Uplink locale

uplink-thermal-glasses-name = Термальные очки
uplink-thermal-glasses-desc = Очки со встроенными термальными сенсорами, бонусом идёт функционал маскировки хамелеон.

uplink-thermal-visor-implanter-name = Имплант термального зрения
uplink-thermal-visor-implanter-desc = Новейшая разработка синдиката, имплант позволяет глазу носителя различать тепловые сигнатуры сквозь преграды.

uplink-dna-copy-implanter-name = Имплант копирования ДНК
uplink-dna-copy-implanter-desc = Новейшая разработка синдиката, имплант позволяет скопировать внешность другого человека.

uplink-adrenal-implanter-name = Имплант Адреналина
uplink-adrenal-implanter-desc = Внутри импланта содержится жидкость, которая ускоренно восстанавливает организм. Впрыскивается по малейшему импульсу даже в предсмертном состоянии

# silent shoes
hidden-desc-SilentShoes-syndicate = [color=#e31735]Разработанные специально для шпионских миссий Синдиката, эти ботинки совершенно бесшумны на любых поверхностях[/color]
hidden-desc-SilentShoes-research = [color=#D381C9]Вы можете различить скрытый слой поглощающего материала, полностью глушащий любые звуки шагов[/color]

hidden-desc-flaming-axe-module-syndicate = [color=#e31735]Модуль предоставляет топор, модифицированный компанией Cybersun, поджигающий жертв при ударе![/color]
hidden-desc-flaming-axe-module-research = [color=#D381C9]Данный модуль поставляет нестандартный топор. Вы можете различить воспламеняющее устройство на острие топора.[/color]

# Svarshik's toy guns!
hidden-desc-toy-weapons-syndicate = [color=#e31735]Оно сделано из бананиума.[/color]
hidden-desc-toy-weapons-clown = [color=#cc0099]Устройте самый смешной штурм станции с этим оружием, заряженным безобидными пистонами! ХОНК![/color]

hidden-desc-toy-weapons-magazine-syndicate = [color=#e31735]Очень хорошая реплика магазина. Скорее всего, он заряжен пистонами или поролоном и не может быть заряжен в настоящее оружие.[/color]
hidden-desc-toy-weapons-magazine-clown = [color=#cc0099]Одобренный Donk Co. стандартный магазин. Мультикалиберный! Может быть заряжен поролоном и пистонами одновременно![/color]

hidden-desc-toy-weapons-ammo-syndicate = [color=#e31735]Это безобидный нелетальный патрон с бумажной пулей.[/color]
hidden-desc-toy-weapons-ammo-clown = [color=#cc0099]Почти как настоящий, но на деле заряжен солью.[/color]

hidden-desc-toy-weapons-explosive-syndicate = [color=#e31735]Создает весьма реалистичный, но абсолютно безобидный взрыв для создания суматохи.[/color]
hidden-desc-toy-weapons-explosive-clown = [color=#cc0099]Взрывчатка, набитая бананиумом и конфетти, очень реалистично взрывается, но никому не вредит![/color]

hidden-desc-toy-weapons-lubebomb-syndicate = [color=#e31735]Особая граната, начинённая смазкой, изготовленная специально для подлых диверсий.[/color]
hidden-desc-toy-weapons-lubebomb-clown = [color=#cc0099]Граната, до отказа заполненная смазкой! Веселье для всей станции![/color]

hidden-desc-toy-shield-syndicate = [color=#e31735]Это пластик, совершенно не защищающий в бою.[/color]
#ss220 trackingimplanter-desc-fix begin
hidden-desc-toy-shield-clown = [color=#cc0099]Устройте баталию на игрушечных мечах с этим щитом! ХОНК![/color]
#ss220 trackingimplanter-desc-fix end

hidden-desc-toy-armour-syndicate = [color=#e31735]Это тканевая реплика настоящей брони, не предоставляющая никакой защиты.[/color]
hidden-desc-toy-armour-clown = [color=#cc0099]Выглядит 1 в 1 как настоящий! Но на самом деле - просто тряпка![/color]

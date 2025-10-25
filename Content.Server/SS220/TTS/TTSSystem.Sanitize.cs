using System.Text;
using System.Text.RegularExpressions;
using Content.Server.Chat.Systems;

namespace Content.Server.SS220.TTS;

// ReSharper disable once InconsistentNaming
public sealed partial class TTSSystem
{
    private void OnTransformSpeech(TransformSpeechEvent args)
    {
        if (!_isEnabled) return;
        args.Message = args.Message.Replace("+", "");
    }

    private string Sanitize(string text)
    {
        text = text.Trim();
        text = Regex.Replace(text, @"(?<![a-zA-Zа-яёА-ЯЁ])[a-zA-Zа-яёА-ЯЁ]+?(?![a-zA-Zа-яёА-ЯЁ])", ReplaceMatchedWord, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"[^a-zA-Zа-яА-ЯёЁ0-9,\-+?!. ]", "");
        text = Regex.Replace(text, @"[a-zA-Z]", ReplaceLat2Cyr, RegexOptions.Multiline | RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"(?<=[1-90])(\.|,)(?=[1-90])", " целых ");
        text = Regex.Replace(text, @"\d+", ReplaceWord2Num);
        text = text.Trim();
        return text;
    }

    private string ReplaceLat2Cyr(Match oneChar)
    {
        if (ReverseTranslit.TryGetValue(oneChar.Value.ToLower(), out var replace))
            return replace;
        return oneChar.Value;
    }

    private string ReplaceMatchedWord(Match word)
    {
        if (WordReplacement.TryGetValue(word.Value.ToLower(), out var replace))
            return replace;
        return word.Value;
    }

    private string ReplaceWord2Num(Match word)
    {
        if (!long.TryParse(word.Value, out var number))
            return word.Value;
        return NumberConverter.NumberToText(number);
    }

    private static readonly IReadOnlyDictionary<string, string> WordReplacement =
        new Dictionary<string, string>()
        {
            {"нт", "Эн Тэ"},
            {"смо", "Эс Мэ О"},
            {"гп", "Гэ Пэ"},
            {"рд", "Эр Дэ"},
            {"гсб", "Гэ Эс Бэ"},
            {"гв", "Гэ Вэ"},
            {"нр", "Эн Эр"},
            {"нра", "Эн Эра"},
            {"нру", "Эн Эру"},
            {"км", "Кэ Эм"},
            {"кма", "Кэ Эма"},
            {"кму", "Кэ Эму"},
            {"си", "Эс И"},
            {"срп", "Эс Эр Пэ"},
            {"пцк", "Пэ Цэ Каа"},
            {"оцк", "О Цэ Каа"},
            {"поцк", "Пэ О Цэ Каа"},
            {"цк", "Цэ Каа"},
            {"рнд", "Эр Эн Дэ"},
            {"сб", "Эс Бэ"},
            {"рцд", "Эр Цэ Дэ"},
            {"брпд", "Бэ Эр Пэ Дэ"},
            {"рпд", "Эр Пэ Дэ"},
            {"рпед", "Эр Пед"},
            {"тсф", "Тэ Эс Эф"},
            {"срт", "Эс Эр Тэ"},
            {"обр", "О Бэ Эр"},
            {"кпк", "Кэ Пэ Каа"},
            {"пда", "Пэ Дэ А"},
            {"id", "Ай Ди"},
            {"мщ", "Эм Ще"},
            {"вт", "Вэ Тэ"},
            {"ерп", "Йе Эр Пэ"},
            {"се", "Эс Йе"},
            {"апц", "А Пэ Цэ"},
            {"лкп", "Эл Ка Пэ"},
            {"см", "Эс Эм"},
            {"ека", "Йе Ка"},
            {"ка", "Кэ А"},
            {"бса", "Бэ Эс Аа"},
            {"тк", "Тэ Ка"},
            {"бфл", "Бэ Эф Эл"},
            {"бщ", "Бэ Щэ"},
            {"кк", "Кэ Ка"},
            {"ск", "Эс Ка"},
            {"зк", "Зэ Ка"},
            {"ерт", "Йе Эр Тэ"},
            {"вкд", "Вэ Ка Дэ"},
            {"нтр", "Эн Тэ Эр"},
            {"пнт", "Пэ Эн Тэ"},
            {"авд", "А Вэ Дэ"},
            {"пнв", "Пэ Эн Вэ"},
            {"ссд", "Эс Эс Дэ"},
            {"крс", "Ка Эр Эс"},
            {"кпб", "Кэ Пэ Бэ"},
            {"сссп", "Эс Эс Эс Пэ"},
            {"крб", "Ка Эр Бэ"},
            {"бд", "Бэ Дэ"},
            {"сст", "Эс Эс Тэ"},
            {"скс", "Эс Ка Эс"},
            {"икн", "И Ка Эн"},
            {"нсс", "Эн Эс Эс"},
            {"емп", "Йе Эм Пэ"},
            {"бс", "Бэ Эс"},
            {"цкс", "Цэ Ка Эс"},
            {"срд", "Эс Эр Дэ"},
            {"жпс", "Джи Пи Эс"},
            {"gps", "Джи Пи Эс"},
            {"ннксс", "Эн Эн Ка Эс Эс"},
            {"ss", "Эс Эс"},
            {"тесла", "тэсла"},
            {"трейзен", "трэйзэн"},
            {"нанотрейзен", "нанотрэйзэн"},
            {"рпзд", "Эр Пэ Зэ Дэ"},
            {"кз", "Кэ Зэ"},
            {"рхбз", "Эр Хэ Бэ Зэ"},
            {"рхбзз", "Эр Хэ Бэ Зэ Зэ"},
            {"днк", "Дэ Эн Ка"},
            {"с4", "Си 4"}, // cyrillic
            {"c4", "Си 4"}, // latinic
            {"бсс", "Бэ Эс Эс"},
            // ss220 fix deutch begin
            {"ja", "Я"},
            {"mein", "Майн"},
            {"gott", "Готх"},
            {"nein", "Найнъ"},
            {"scheisse", "Шайз"},
            {"wurst", "Вуст"},
            {"wurste", "Вёстэ"},
            {"manner", "Мяннэ"},
            {"frauen", "Фраун"},
            {"herr", "Герр"},
            {"herren", "Геррен"},
            {"meine", "Майнэ"},
            {"hier", "Хие"},
            {"dumnkopf", "Думкопф"},
            {"dummkopfe", "Думкёпфэ"},
            {"schmetterling", "Шмэттерлин"},
            {"maschine", "Машинэ"},
            {"maschinen", "Машинэн"},
            {"achtung", "Ахтунг"},
            {"musik", "Музык"},
            {"kapitan", "Капитэин"},
            {"doner", "Дёнэр"},
            {"dankeschon", "Данке Шён"},
            {"gesundheit", "Гесундхайт"},
            {"flammenwerfer", "Фламэнверфер"},
            {"poltergeist", "Полтэргайст"},
            {"branntwein", "Брантвайн"},
            {"rucksack", "Рюксак"},
            {"medizin", "Медицин"},
            {"akzent", "Акцэнт"},
            {"anomalie", "Аномалий"},
            {"doof", "Доф"},
            {"warnung" , "Варнун"},
            {"wunderbar", "Вундэрбар"},
            {"warnungen", "Варнунгэ"},
            {"karpfen", "Карпфн"},
            {"bier", "Биэ"},
            {"guten", "Гутн"},
            {"krankenwagen", "Кранкн Вагн"},
            {"auf", "Ау"},
            {"wiedersehen", "Фидерзеин"},
            {"tschuss", "Чус"},
            {"tschau", "Чау"},
            {"fantastisch", "Фантастиш"},
            {"doppelganger", "Доппэльгнгэа"},
            {"verboten", "Вэрботн"},
            {"schnell", "Шнэль"},
            {"krankenhaus", "Кранкнхауз"},
            {"kugelblitz", "Кьюгельблиц"},
            {"ist", "Ыст"},
            {"pulkzerstorer", "Пулькцерштёрер"},
            {"pulkzerstorers", "Пулькцерштёрер"},
            {"pulkzerstorere", "Пулькцерштёрер"},
            {"offizier", "Оффизые"},
            {"offiziere", "Оффизыер"},
            {"offiziers", "Оффизыерс"},
            // ss220 fix deutch end
            // ss220 fix spanish begin
            {"sí", "Си"},
            {"no", "Но"},
            {"mi", "Ми"},
            {"carajo", "Карахо"},
            {"salchicha", "Сальчича"},
            {"hombre", "Омбре"},
            {"mujer", "Мухе"},
            {"dios mío", "Диос Мио"},
            {"idiota", "Идиота"},
            {"atención", "Атенсьон"},
            {"capitán", "Капитано"},
            {"qué", "Ке"},
            {"gracias", "Грасьяс"},
            {"hola", "Ола"},
            {"adiós", "Адьос"},
            {"rápido", "Рапидо"},
            {"amigo", "Амиго"},
            {"trabajo", "Трабахо"},
            {"comida", "Комида"},
            {"agua", "Агуа"},
            {"por favor", "Пор Фавор"},
            {"perdón", "Пердон"},
            {"peligro", "Пелигро"},
            {"ayuda", "Аюда"},
            {"nave", "Навэ"},
            {"problema", "Проблема"},
            {"arma", "Арма"},
            {"médico", "Медико"},
            {"ingeniero", "Инхеньеро"},
            {"perfecto", "Перфекто"},
            {"terrible", "Террибле"},
            {"fuego", "Фуэго"},
            {"atmósfera", "Атмосфера"},
            {"gravedad", "Граведад"},
            {"escudo", "Эскудо"},
            {"reactor", "Реактор"},
            {"motor", "Мотор"},
            {"compartimiento", "Компартьимьенто"},
            {"esclusa", "Эсклуса"},
            {"traje espacial", "Трахе Эспасьяль"},
            {"herramientas", "Эррамьентес"},
            {"sistema", "Система"},
            {"daño", "Даньо"},
            {"reparación", "Репарасьон"},
            {"alarma", "Аларма"},
            {"ventilación", "Вентиласьон"},
            {"oxígeno", "Оксихено"},
            {"presión", "Пресьон"},
            {"temperatura", "Температура"},
            {"estabilizador", "Эстабилисадор"},
            {"panel", "Панель"},
            {"cables", "Каблес"},
            {"energía", "Энерхия"},
            {"batería", "Батерия"},
            {"generador", "Хенерадор"},
            {"enfriamiento", "Энфриамиенто"},
            {"sobrecalentamiento", "Собрекалентамиенто"},
            {"fuga", "Фуга"},
            {"radiación", "Радиасон"},
            {"protección", "Протексьон"},
            {"escáner", "Эсканер"},
            {"sensor", "Сенсор"},
            {"médico", "Медико"},
            {"herida", "Эрида"},
            {"hemorragia", "Эморахия"},
            {"medicamentos", "Медикаментос"},
            {"farmacia", "Фармасия"},
            {"quirófano", "Кирофано"},
            {"paciente", "Пасьенте"},
            {"caramba", "Карамба"},
            {"loco", "Локо"},
            {"locura", "Локура"},
            {"increíble", "Инкреибеле"},
            {"excelente", "Экселенте"},
            {"bien", "Бьен"},
            {"mal", "Маль"},
            {"extraño", "Эстраньо"},
            {"sospechoso", "Соспечосо"},
            {"anomalía", "Аномалия"},
            {"artefacto", "Артефакто"},
            {"alienígena", "Алиенихена"},
            {"singularidad", "Сингуларидад"},
            {"teletransporte", "Телетранспорте"},
            {"invisible", "Инвизибле"},
            {"misterioso", "Мистериосо"},
            {"barrera", "Баррера"},
            {"laboratorio", "Лабораторио"},
            {"experimento", "Эксперименто"},
            {"investigación", "Инвестигасьон"},
            {"muestra", "Муэстра"},
            {"mutación", "Мутасьон"},
            {"clonación", "Клонасьон"},
            {"genial", "Хеньяль"},
            {"apesta", "Апеста"},
            {"calor", "Калор"},
            {"frío", "Фрио"},
            {"hambriento", "Амбриенто"},
            {"sed", "Сед"},
            {"cansado", "Кансадо"},
            {"herido", "Эридо"},
            {"muerto", "Муэрто"},
            {"vivo", "Виво"},
            {"peligroso", "Пелигросо"},
            {"seguro", "Сегуро"},
            {"silencio", "Силенсьо"},
            {"ruidoso", "Руидосо"},
            {"oscuro", "Оскуро"},
            {"luminoso", "Луминозо"},
            {"limpio", "Лимпио"},
            {"sucio", "Сусьо"},
            {"yo", "Йо"},
            {"tú", "Ту"},
            {"él", "Эль"},
            {"ella", "Элья"},
            {"ello", "Эльо"},
            {"nosotros", "Носотрос"},
            {"ustedes", "Устедес"},
            {"ellos", "Эльос"},
            {"me", "Мэ"},
            {"te", "Тэ"},
            {"le", "Ле"},
            {"nos", "Нос"},
            {"les", "Лес"},
            {"les", "Лес"},
            {"querido", "Керидо"},
            {"querida", "Керида"},
            {"amor mío", "Амор Мио"},
            {"cariño", "Кариньо"},
            {"alegría mía", "Алехрия Мия"},
            {"hermosa", "Эрмоса"},
            {"hermoso", "Эрмосо"},
            {"por supuesto", "Пор Супуэсто"},
            {"entiendo", "Энтьендо"},
            {"quizás", "Кисас"},
            {"sueño mío", "Суэньо Мио"},
            {"princesa", "Принсеса"},
            {"príncipe", "Принсипе"},
            {"realmente", "Реальменте"},
            {"pasión mía", "Пасьон Мия"},
            {"beso", "Бесо"},
            {"abrazar", "Абрасар"},
            {"ternura", "Тернура"},
            {"romántico", "Романтико"},
            {"cita", "Сита"},
            {"flores", "Флорес"},
            {"corazón", "Корасон"},
            {"alma mía", "Альма Мия"},
            {"feliz", "Фелис"},
            {"apasionado", "Апасьонадо"},
            {"tierno", "Тьерно"},
            {"rojo", "Рохо"},
            {"naranja", "Наранха"},
            {"amarillo", "Амарильо"},
            {"verde", "Верде"},
            {"azul claro", "Азуль Кларо"},
            {"azul", "Азуль"},
            {"morado", "Морадо"},
            {"rosa", "Роса"},
            {"blanco", "Бланко"},
            {"entendido", "Энтендидо"},
            {"gris", "Грис"},
            {"marrón", "Маррон"},
            {"color", "Колор"},
            {"claro", "Кларо"},
            {"oscuro", "Оскуро"},
            {"brillante", "Брийянте"},
            {"transparente", "Транспаренте"},
            {"arcoíris", "Аркоирис"},
            {"metálico", "Металико"},
            {"multicolor", "Мультаколор"},
            {"papi", "Папи"},
            {"mami", "Мами"},
            {"hijito", "Ихито"},
            {"hijita", "Ихита"},
            {"chamba", "Чамба"},
            {"dinero", "Динеро"},
            {"genial", "Хеньяль"},
            {"cuate", "Куате"},
            {"chica", "Чика"},
            {"chico", "Чико"},
            {"güey", "Гуэй"},
            {"bronca", "Бронка"},
            {"güeva", "Гуэва"},
            {"huevón", "Уэвон"},
            {"peda", "Педа"},
            {"pedo", "Педо"},
            {"chido", "Чидо"},
            {"feo", "Фео"},
            {"chingo", "Чинго"},
            {"poquito", "Покито"},
            {"despacio", "Деспасьо"},
            {"rico", "Рико"},
            {"asco", "Аско"},
            {"cansado", "Кансадо"},
            {"hambriento", "Амбриенто"},
            {"por cierto", "Пор Сьерто"},
            {"en general", "Эн Хенераль"},
            {"como", "Комо"},
            {"o sea", "О Сеа"},
            {"en fin", "Эн Фин"},
            {"directamente", "Директаменте"},
            {"como si", "Комо Си"},
            {"aquí", "Аки"},
            {"allí", "Айи"},
            {"dónde", "Донде"},
            {"cuándo", "Куандо"},
            {"por qué", "Пор Ке"},
            {"para qué", "Пара Ке"},
            {"qué", "Ке"},
            {"cuánto", "Куанто"},
            {"demasiado", "Демасьядо"},
            {"casi", "Каси"},
            {"inmediatamente", "Инмедиатаменте"},
            {"de repente", "Де Репенте"},
            {"después", "Деспуэс"},
            {"siempre", "Сьемпре"},
            {"nunca", "Нунка"},
            {"a veces", "А Весес"},
            {"normalmente", "Нормальменте"},
            {"exactamente", "Эзактаменте"},
            {"probablemente", "Пробаблементе"},
            {"especialmente", "Эспесьяльменте"},
            {"parece", "Паресе"},
            {"precisamente", "Пресис аменте"},
            {"por ejemplo", "Пор Эхемпло"},
            {"claro", "Кларо"},
            {"de verdad", "Де Бердад"},
            {"posiblemente", "Посиблементе"},
            {"bastante", "Бастанте"},
            {"suficiente", "Суфисьенте"}
            // ss220 fix spanish end

        };

    private static readonly IReadOnlyDictionary<string, string> ReverseTranslit =
        new Dictionary<string, string>()
        {
            {"a", "а"},
            {"b", "б"},
            {"v", "в"},
            {"g", "г"},
            {"d", "д"},
            {"e", "е"},
            {"je", "ё"},
            {"zh", "ж"},
            {"z", "з"},
            {"i", "и"},
            {"y", "й"},
            {"k", "к"},
            {"l", "л"},
            {"m", "м"},
            {"n", "н"},
            {"o", "о"},
            {"p", "п"},
            {"r", "р"},
            {"s", "с"},
            {"t", "т"},
            {"u", "у"},
            {"f", "ф"},
            {"h", "х"},
            {"c", "ц"},
            {"x", "кс"},
            {"ch", "ч"},
            {"sh", "ш"},
            {"jsh", "щ"},
            {"hh", "ъ"},
            {"ih", "ы"},
            {"jh", "ь"},
            {"eh", "э"},
            {"ju", "ю"},
            {"ja", "я"},
            {"ü", "у"},
            {"ö", "о"},
            {"ä", "а"},
        };
}

// Source: https://codelab.ru/s/csharp/digits2phrase
public static class NumberConverter
{
    private static readonly string[] Frac20Male =
    {
        "", "один", "два", "три", "четыре", "пять", "шесть",
        "семь", "восемь", "девять", "десять", "одиннадцать",
        "двенадцать", "тринадцать", "четырнадцать", "пятнадцать",
        "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать"
    };

    private static readonly string[] Frac20Female =
    {
        "", "одна", "две", "три", "четыре", "пять", "шесть",
        "семь", "восемь", "девять", "десять", "одиннадцать",
        "двенадцать", "тринадцать", "четырнадцать", "пятнадцать",
        "шестнадцать", "семнадцать", "восемнадцать", "девятнадцать"
    };

	private static readonly string[] Hunds =
	{
		"", "сто", "двести", "триста", "четыреста",
		"пятьсот", "шестьсот", "семьсот", "восемьсот", "девятьсот"
	};

	private static readonly string[] Tens =
	{
		"", "десять", "двадцать", "тридцать", "сорок", "пятьдесят",
		"шестьдесят", "семьдесят", "восемьдесят", "девяносто"
	};

	public static string NumberToText(long value, bool male = true)
    {
        if (value >= (long)Math.Pow(10, 15))
            return String.Empty;

        if (value == 0)
            return "ноль";

		var str = new StringBuilder();

		if (value < 0)
		{
			str.Append("минус");
			value = -value;
		}

        value = AppendPeriod(value, 1000000000000, str, "триллион", "триллиона", "триллионов", true);
        value = AppendPeriod(value, 1000000000, str, "миллиард", "миллиарда", "миллиардов", true);
        value = AppendPeriod(value, 1000000, str, "миллион", "миллиона", "миллионов", true);
        value = AppendPeriod(value, 1000, str, "тысяча", "тысячи", "тысяч", false);

		var hundreds = (int)(value / 100);
		if (hundreds != 0)
			AppendWithSpace(str, Hunds[hundreds]);

		var less100 = (int)(value % 100);
        var frac20 = male ? Frac20Male : Frac20Female;
		if (less100 < 20)
			AppendWithSpace(str, frac20[less100]);
		else
		{
			var tens = less100 / 10;
			AppendWithSpace(str, Tens[tens]);
			var less10 = less100 % 10;
			if (less10 != 0)
				str.Append(" " + frac20[less100%10]);
		}

		return str.ToString();
	}

	private static void AppendWithSpace(StringBuilder stringBuilder, string str)
	{
		if (stringBuilder.Length > 0)
			stringBuilder.Append(" ");
		stringBuilder.Append(str);
	}

	private static long AppendPeriod(
        long value,
        long power,
		StringBuilder str,
		string declension1,
		string declension2,
		string declension5,
		bool male)
	{
		var thousands = (int)(value / power);
		if (thousands > 0)
		{
			AppendWithSpace(str, NumberToText(thousands, male, declension1, declension2, declension5));
			return value % power;
		}
		return value;
	}

	private static string NumberToText(
        long value,
        bool male,
		string valueDeclensionFor1,
		string valueDeclensionFor2,
		string valueDeclensionFor5)
	{
		return
            NumberToText(value, male)
			+ " "
			+ GetDeclension((int)(value % 10), valueDeclensionFor1, valueDeclensionFor2, valueDeclensionFor5);
	}

	private static string GetDeclension(int val, string one, string two, string five)
	{
		var t = (val % 100 > 20) ? val % 10 : val % 20;

		switch (t)
		{
			case 1:
				return one;
			case 2:
			case 3:
			case 4:
				return two;
			default:
				return five;
		}
	}
}

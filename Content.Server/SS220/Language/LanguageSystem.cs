// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.GameTicking.Events;
using Content.Shared.SS220.Language.Components;
using Content.Shared.SS220.Language;
using Robust.Shared.Random;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Text.RegularExpressions;
using Content.Shared.SS220.Language.Systems;
using Robust.Shared.Configuration;
using Content.Shared.SS220.CCVars;
using Content.Shared.Chat;
using Content.Server.Chat.Systems;

namespace Content.Server.SS220.Language;

public sealed partial class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly LanguageManager _language = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly ChatSystem _chatSystem = default!;

    // Cached values for one tick
    private static readonly Dictionary<string, string> ScrambleCache = new();

    private static int Seed = 0;

    private Regex? _textWithKeyRegex;
    private TimeSpan _regexTimeout = TimeSpan.FromSeconds(1);

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<LanguageComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<LanguageComponent, SendLanguageMessageAttemptEvent>(OnSendLanguageMessageAttemptEvent);

        // UI
        SubscribeNetworkEvent<ClientSelectLanguageEvent>(OnClientSelectLanguage);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        ScrambleCache.Clear();
    }

    private void OnRoundStart(RoundStartingEvent args)
    {
        Seed = _random.Next();
    }

    /// <summary>
    ///     Initializes an entity with a language component,
    ///     either the first language in the LearnedLanguages list into the CurrentLanguage variable
    /// </summary>
    private void OnMapInit(Entity<LanguageComponent> ent, ref MapInitEvent args)
    {
        TrySetLanguage(ent, 0);
    }

    private void OnSendLanguageMessageAttemptEvent(Entity<LanguageComponent> ent, ref SendLanguageMessageAttemptEvent args)
    {
        args.Listener = ent;
    }

    #region Client
    private void OnClientSelectLanguage(ClientSelectLanguageEvent msg, EntitySessionEventArgs args)
    {
        var entity = args.SenderSession.AttachedEntity;
        if (entity == null || !TryComp<LanguageComponent>(entity, out var comp))
            return;

        TrySetLanguage((entity.Value, comp), msg.LanguageId);
    }
    #endregion

    /// <summary>
    ///     A method of encrypting the original message into a message created
    ///     from the syllables of prototypes languages
    /// </summary>
    public string ScrambleMessage(string message, LanguagePrototype proto)
    {
        var cacheKey = $"{proto.ID}:{message}";

        // If the original message is already there earlier encrypted,
        // it is taken from the cache, it is necessary for the correct display when sending in the radio,
        // when the character whispers and transmits a message to the radio
        if (ScrambleCache.TryGetValue(cacheKey, out var cachedValue))
            return cachedValue;

        var scrambled = proto.ScrambleMethod.ScrambleMessage(message, Seed);

        ScrambleCache[cacheKey] = scrambled;

        return scrambled;
    }

    public string SanitizeMessage(EntityUid source,
        EntityUid listener,
        string message,
        out string colorlessMessage,
        out string obfuscatedMessage,
        bool setColor = true)
    {
        var sanitizedMessage = SanitizeMessage(source, listener, message, out colorlessMessage, setColor);
        obfuscatedMessage = _chatSystem.ObfuscateMessageReadability(colorlessMessage, 0.2f);
        return sanitizedMessage;
    }

    /// <summary>
    ///     Sanitize the <paramref name="message"/> by removing the language tags and scramble it (if necessary) for <paramref name="listener"/>
    /// </summary>
    public string SanitizeMessage(EntityUid source,
        EntityUid listener,
        string message,
        out string colorlessMessage,
        bool setColor = true)
    {
        colorlessMessage = message;
        var languageProto = GetSelectedLanguage(source);
        if (languageProto == null)
            return message;

        var languageStrings = SplitMessageByLanguages(source, message, languageProto);
        var sanitizedMessage = new StringBuilder();
        var sanitizedColorlessMessage = new StringBuilder();
        for (var i = 0; i < languageStrings.Count; i++)
        {
            var curString = languageStrings[i];
            if (CanUnderstand(listener, curString.Item2.ID))
            {
                sanitizedColorlessMessage.Append(curString.Item1);
                var newMessage = curString.Item1;
                if (i + 1 == languageStrings.Count)
                    newMessage = newMessage.Trim();

                if (setColor)
                    newMessage = SetColor(newMessage, curString.Item2);

                sanitizedMessage.Append(newMessage);
            }
            else
            {
                var scrambledString = ScrambleMessage(curString.Item1, curString.Item2);
                sanitizedColorlessMessage.Append(scrambledString);
                if (i + 1 == languageStrings.Count)
                    scrambledString = scrambledString.Trim();

                if (setColor)
                    scrambledString = SetColor(scrambledString, curString.Item2);

                sanitizedMessage.Append(scrambledString);
            }
        }

        colorlessMessage = sanitizedColorlessMessage.ToString().Trim();
        return sanitizedMessage.ToString().Trim();
    }

    /// <summary>
    ///     Split the message into parts by language tags.
    ///     <paramref name="defaultLanguage"/> will be used for the part of the message without the language tag.
    /// </summary>
    private List<(string, LanguagePrototype)> SplitMessageByLanguages(EntityUid source, string message, LanguagePrototype defaultLanguage)
    {
        var list = new List<(string, LanguagePrototype)>();
        var p = LanguageManager.KeyPrefix;
        _textWithKeyRegex ??= new Regex(
            $@"^{p}(.*?)\s(?={p}\w+\s)|(?<=\s){p}(.*?)\s(?={p}\w+\s)|(?<=\s){p}(.*)|^{p}(.*)",
            RegexOptions.Compiled,
            _regexTimeout);

        var matches = _textWithKeyRegex.Matches(message);
        if (matches.Count <= 0)
        {
            list.Add((message, defaultLanguage));
            return list;
        }

        var textBeforeFirstTag = message.Substring(0, matches[0].Index);
        (string, LanguagePrototype?) buffer = (string.Empty, null);
        if (textBeforeFirstTag != string.Empty)
            buffer = (textBeforeFirstTag, defaultLanguage);

        foreach (Match m in matches)
        {
            if (!TryGetLanguageFromString(m.Value, out var messageWithoutTags, out var language) ||
                !CanSpeak(source, language.ID))
            {
                if (buffer.Item2 == null)
                {
                    buffer = (m.Value, defaultLanguage);
                }
                else
                {
                    buffer.Item1 += m.Value;
                }

                continue;
            }

            if (buffer.Item2 == language)
            {
                buffer.Item1 += messageWithoutTags;
                continue;
            }
            else if (buffer.Item2 != null)
            {
                list.Add((buffer.Item1, buffer.Item2));
            }

            buffer = (messageWithoutTags, language);
        }

        if (buffer.Item2 != null)
        {
            list.Add((buffer.Item1, buffer.Item2));
        }

        return list;
    }

    /// <summary>
    ///     Get a list of message parts with a language tag
    ///     <paramref name="skipDefaultLanguageKeyAtTheBeginning"/> will the first key be skipped if it is equal to the default language key.
    /// </summary>
    public List<(string, string)> GetMessagesWithLanguageKey(EntityUid source, string message, bool skipDefaultLanguageKeyAtTheBeginning = false)
    {
        var list = new List<(string, string)>();
        var defaultLanguage = GetSelectedLanguage(source);
        if (defaultLanguage == null)
        {
            list.Add((string.Empty, message));
            return list;
        }

        var splited = SplitMessageByLanguages(source, message, defaultLanguage);
        for (var i = 0; i < splited.Count; i++)
        {
            var languageMessage = splited[i].Item1;
            var language = splited[i].Item2;
            if (skipDefaultLanguageKeyAtTheBeginning && i == 0 && language == defaultLanguage)
            {
                // Если в первой части сообщения нет языкового тега, отличного от тега языка по умолчанию, то он пропускается.
                list.Add((string.Empty, languageMessage));
                continue;
            }

            var key = LanguageManager.KeyPrefix + language.Key;
            list.Add((key, languageMessage));
        }

        return list;
    }

    public string ChangeLanguageMessages(EntityUid source, string message, Func<string, string> func, bool skipDefaultLanguageKeyAtTheBeginning = false)
    {
        var newMessage = string.Empty;
        foreach (var (key, languageMessage) in GetMessagesWithLanguageKey(source, message, skipDefaultLanguageKeyAtTheBeginning))
        {
            var newLangMessage = func.Invoke(languageMessage);
            newMessage += $"{key} {newLangMessage}";
        }

        return newMessage.Trim();
    }

    /// <summary>
    ///     Tries to find the first language tag in the message and extracts it from the message
    /// </summary>
    public bool TryGetLanguageFromString(string message,
        [NotNullWhen(true)] out string? messageWithoutTags,
        [NotNullWhen(true)] out LanguagePrototype? language)
    {
        messageWithoutTags = null;
        language = null;

        var keyPatern = $@"{LanguageManager.KeyPrefix}\w+\s+";

        var m = Regex.Match(message, keyPatern);
        if (m == null || !_language.TryGetLanguageByKey(m.Value.Trim(), out language))
            return false;

        messageWithoutTags = Regex.Replace(message, keyPatern, string.Empty);
        return messageWithoutTags != null && language != null;
    }

    /// <summary>
    ///     A method to get a prototype language from an entity.
    ///     If the entity does not have a language component, a universal language is assigned.
    /// </summary>
    public LanguagePrototype? GetSelectedLanguage(EntityUid uid)
    {
        if (!TryComp<LanguageComponent>(uid, out var comp))
        {
            if (_language.TryGetLanguageById(UniversalLanguage, out var universalProto))
                return universalProto;

            return null;
        }

        var languageID = comp.SelectedLanguage;
        if (languageID == null)
            return null;

        _language.TryGetLanguageById(languageID, out var proto);
        return proto;
    }

    /// <summary>
    ///     Raises event to receive a response is it possible to send a message in the language
    ///     This is done for the possibility of forwarding
    /// </summary>
    public bool SendLanguageMessageAttempt(EntityUid uid, out EntityUid listener)
    {
        var ev = new SendLanguageMessageAttemptEvent();
        RaiseLocalEvent(uid, ref ev);
        listener = ev.Listener ?? uid;
        return !ev.Cancelled;
    }

    /// <summary>
    ///     Sets the color of the prototype language to the message 
    /// </summary>
    public string SetColor(string message, LanguagePrototype proto)
    {
        if (proto.Color == null)
            return message;

        var color = proto.Color.Value.ToHex();
        message = $"[color={color}]{message}[/color]";
        return message;
    }

    /// <summary>
    ///     Adds languages for <paramref name="target"/> from <paramref name="ent"/>
    /// </summary>
    public void AddLanguagesFromSource(Entity<LanguageComponent> ent, EntityUid target)
    {
        var targetComp = EnsureComp<LanguageComponent>(target);
        foreach (var language in ent.Comp.AvailableLanguages)
        {
            AddLanguage((target, targetComp), language);
        }
    }

    public bool MessageLanguagesLimit(EntityUid source, string message, [NotNullWhen(true)] out string? reason)
    {
        reason = null;
        if (!HasComp<LanguageComponent>(source))
            return false;

        var defaultLanguage = GetSelectedLanguage(source);
        if (defaultLanguage == null)
            return false;

        var languagesLimit = _config.GetCVar(CCVars220.MaxLanguagesInOneMessage);
        var languagesStrings = SplitMessageByLanguages(source, message, defaultLanguage);
        if (languagesStrings.Count > languagesLimit)
        {
            reason = Loc.GetString("language-message-languages-limit", ("limit", languagesLimit));
            return true;
        }

        return false;
    }
}

[ByRefEvent]
public sealed class SendLanguageMessageAttemptEvent() : CancellableEntityEventArgs
{
    public EntityUid? Listener = null;
}


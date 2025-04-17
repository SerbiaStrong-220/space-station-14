// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Shared.SS220.Language.Systems;

public abstract partial class SharedLanguageSystem
{
    private Regex? _textWithKeyRegex;
    private TimeSpan _regexTimeout = TimeSpan.FromSeconds(1);

    // Cache for 1 tick
    private Dictionary<string, LanguageMessage> _cachedMessages = new();

    /// <summary>
    /// Sanitize the message by forming <see cref="LanguageMessage"/> by dividing the message into <see cref="LanguageNode"/>
    /// </summary>
    public LanguageMessage SanitizeMessage(EntityUid source, string message)
    {
        var cacheKey = GetCahceKey(source, message);
        if (_cachedMessages.TryGetValue(cacheKey, out var cahcedLanguageMessage))
            return cahcedLanguageMessage;

        List<LanguageNode> nodes = new();
        var defaultLanguage = GetSelectedLanguage(source);
        if (defaultLanguage == null && !_language.TryGetLanguageById(UniversalLanguage, out defaultLanguage))
            return new LanguageMessage(nodes, message, this);

        var languageStrings = SplitMessageByLanguages(source, message, defaultLanguage);
        foreach (var (inStringMessage, language) in languageStrings)
        {
            var node = new LanguageNode(language, inStringMessage, this);
            nodes.Add(node);
        }

        var languageMessage = new LanguageMessage(nodes, message, this);
        _cachedMessages.Add(cacheKey, languageMessage);
        return languageMessage;
    }

    private string GetCahceKey(EntityUid source, string message)
    {
        var avalibleLanguageKeys = string.Empty;
        if (!TryComp<LanguageComponent>(source, out var languageComponent))
        {
            if (_language.TryGetLanguageById(UniversalLanguage, out var UniLanguage))
                avalibleLanguageKeys = UniLanguage.KeyWithPrefix;
        }
        else if (languageComponent.KnowAllLLanguages)
            avalibleLanguageKeys = "knowall";
        else
        {
            foreach (var definition in languageComponent.AvailableLanguages)
            {
                if (!definition.CanSpeak)
                    continue;

                if (_language.TryGetLanguageById(definition.Id, out var language))
                {
                    if (avalibleLanguageKeys.Length > 0)
                        avalibleLanguageKeys += "_";

                    avalibleLanguageKeys += language.KeyWithPrefix;
                }
            }
        }

        if (avalibleLanguageKeys == string.Empty)
            avalibleLanguageKeys = "knownothing";

        return $"{avalibleLanguageKeys}/\"{message}\"";
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
    ///     Split the message into parts by language tags.
    ///     <paramref name="defaultLanguage"/> will be used for the part of the message without the language tag.
    /// </summary>
    protected List<(string, LanguagePrototype)> SplitMessageByLanguages(EntityUid source, string message, LanguagePrototype defaultLanguage)
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

        list = [.. list.Select(x => (x.Item1.Trim(), x.Item2))]; // trim strings
        return list;
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

        messageWithoutTags = Regex.Replace(message, keyPatern, string.Empty).Trim();
        return messageWithoutTags != null && language != null;
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

            list.Add((language.KeyWithPrefix, languageMessage));
        }

        return list;
    }
}

[DataDefinition]
[Serializable, NetSerializable]
public sealed partial class LanguageMessage
{
    [DataField]
    public List<LanguageNode> Nodes;

    [DataField]
    public string OriginalMessage;

    private readonly SharedLanguageSystem _languageSystem;

    public LanguageMessage(List<LanguageNode> nodes, string originalMessage, SharedLanguageSystem? languageSystem = null)
    {
        Nodes = nodes;
        OriginalMessage = originalMessage;
        _languageSystem = languageSystem ?? IoCManager.Resolve<EntityManager>().System<SharedLanguageSystem>();
    }

    /// <summary>
    /// Gets a united message from <see cref="Nodes"/>
    /// </summary>
    public string GetMessage(EntityUid? listener, bool sanitize, bool colored = true)
    {
        var message = "";
        if (Nodes.Count <= 0)
            return OriginalMessage;

        for (var i = 0; i < Nodes.Count; i++)
        {
            var node = Nodes[i];
            var scrambled = sanitize && listener != null && !_languageSystem.CanUnderstand(listener.Value, node.Language.ID);
            if (i == 0)
                message += node.GetMessage(scrambled, colored);
            else
                message += " " + node.GetMessage(scrambled, colored);
        }

        return message;
    }

    /// <summary>
    /// Gets a united message from <see cref="Nodes"/> with language keys
    /// </summary>
    public string GetMessageWithLanguageKeys(bool withDefault = true)
    {
        string messageWithLanguageTags = "";
        for (var i = 0; i < Nodes.Count; i++)
        {
            if (i == 0)
            {
                if (withDefault)
                    messageWithLanguageTags += Nodes[i].GetMessageWithKey();
                else
                    messageWithLanguageTags += Nodes[i].GetMessage(false, false);
            }
            else
                messageWithLanguageTags += " " + Nodes[i].GetMessageWithKey();
        }
        return messageWithLanguageTags;
    }

    /// <summary>
    /// Gets a obfuscated united message from <see cref="Nodes"/>
    /// </summary>
    public string GetObfuscatedMessage(EntityUid listener, bool sanitize)
    {
        return _languageSystem.ObfuscateMessageReadability(GetMessage(listener, sanitize, false), 0.2f);
    }

    /// <summary>
    /// Changes the message value of each <see cref="Nodes"/> by function
    /// </summary>
    public void ChangeInNodeMessage(Func<string, string> func)
    {
        foreach (var node in Nodes)
            node.SetMessage(func.Invoke(node.Message));
    }
}

/// <summary>
/// It contains information about the message, the language in which it was spoken and its scrambled version.
/// </summary>
[DataDefinition]
[Serializable, NetSerializable, Access(Other = AccessPermissions.ReadExecute)]
public sealed partial class LanguageNode
{
    [DataField]
    public ProtoId<LanguagePrototype> LanguageId
    {
        get => _languageId;
        set
        {
            _languageId = value;
            Language = IoCManager.Resolve<PrototypeManager>().Index(value);
        }
    }
    private ProtoId<LanguagePrototype> _languageId;

    public LanguagePrototype Language;

    [DataField]
    public string Message
    {
        get => _message;
        set
        {
            _message = value;
            UpdateScrambledMessage();
        }
    }
    private string _message = string.Empty;

    public string ScrambledMessage = string.Empty;

    private readonly SharedLanguageSystem _languageSystem;

    public LanguageNode(LanguagePrototype language, string message, SharedLanguageSystem? languageSystem = null)
    {
        _languageSystem = languageSystem ?? IoCManager.Resolve<EntityManager>().System<SharedLanguageSystem>();

        Language = language;
        _languageId = language.ID;

        Message = message;
    }

    public LanguageNode(ProtoId<LanguagePrototype> languageId, string message, SharedLanguageSystem? languageSystem = null)
    {
        _languageSystem = languageSystem ?? IoCManager.Resolve<EntityManager>().System<SharedLanguageSystem>();

        _languageId = languageId;
        Language = IoCManager.Resolve<PrototypeManager>().Index(languageId);

        Message = message;
    }

    public string GetMessage(bool scrambled, bool colored)
    {
        var message = Message;
        if (scrambled)
            message = ScrambledMessage;

        if (colored)
            message = _languageSystem.SetColor(message, Language);

        return message;
    }

    public string GetMessageWithKey()
    {
        return $"{Language.KeyWithPrefix} {Message}";
    }

    public void SetMessage(string value)
    {
        Message = value;
        UpdateScrambledMessage();
    }

    public void UpdateScrambledMessage()
    {
        ScrambledMessage = Language.ScrambleMethod.ScrambleMessage(Message, _languageSystem.Seed);
    }
}

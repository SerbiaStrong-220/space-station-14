// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language.Components;
using Robust.Shared.Serialization;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Content.Shared.SS220.Language.Systems;

public abstract partial class SharedLanguageSystem
{
    private Regex? _textWithKeyRegex;
    private TimeSpan _regexTimeout = TimeSpan.FromSeconds(1);

    public LanguageMessage SanitizeMessage(EntityUid source, string message)
    {
        List<LanguageNode> nodes = new();
        var defaultLanguage = GetSelectedLanguage(source);
        if (defaultLanguage == null && !_language.TryGetLanguageById(UniversalLanguage, out defaultLanguage))
            return new LanguageMessage(nodes, message, this);

        var languageStrings = SplitMessageByLanguages(source, message, defaultLanguage);
        foreach (var (languageMessage, language) in languageStrings)
        {
            var node = new LanguageNode(language, languageMessage, this);
            nodes.Add(node);
        }

        return new LanguageMessage(nodes, message, this);
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

        messageWithoutTags = Regex.Replace(message, keyPatern, string.Empty);
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

            list.Add((language.Key, languageMessage));
        }

        return list;
    }
}

[Serializable, NetSerializable]
public sealed class LanguageMessage
{
    public List<LanguageNode> Nodes;
    public string OriginalMessage;

    private readonly SharedLanguageSystem _languageSystem;

    public LanguageMessage(List<LanguageNode> nodes, string originalMessage, SharedLanguageSystem? languageSystem = null)
    {
        Nodes = nodes;
        OriginalMessage = originalMessage;
        _languageSystem = languageSystem ?? IoCManager.Resolve<EntityManager>().System<SharedLanguageSystem>();
    }

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

    public string GetMessageWithLanguageKeys()
    {
        string messageWithLanguageTags = "";
        for (var i = 0; i < Nodes.Count; i++)
        {
            if (i == 0)
                messageWithLanguageTags += Nodes[i].GetMessageWithKey();
            else
                messageWithLanguageTags += " " + Nodes[i].GetMessageWithKey();
        }
        return messageWithLanguageTags;
    }

    public string GetObfuscatedMessage(EntityUid listener, bool sanitize)
    {
        return _languageSystem.ObfuscateMessageReadability(GetMessage(listener, sanitize, false), 0.2f);
    }

    public void ChangeInNodeMessage(Func<string, string> func)
    {
        foreach (var node in Nodes)
            node.SetMessage(func.Invoke(node.Message));
    }
}

[Serializable, NetSerializable, Access(Other = AccessPermissions.ReadExecute)]
public sealed class LanguageNode
{
    public LanguagePrototype Language;
    public string Message;
    public string ScrambledMessage = string.Empty;

    private readonly SharedLanguageSystem _languageSystem;

    public LanguageNode(LanguagePrototype language, string message, SharedLanguageSystem? languageSystem = null)
    {
        Language = language;
        Message = message;
        _languageSystem = languageSystem ?? IoCManager.Resolve<EntityManager>().System<SharedLanguageSystem>();

        UpdateScrambledMessage();
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
        return $"{Language.Key} {Message}";
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

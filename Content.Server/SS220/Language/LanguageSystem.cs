// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Server.GameTicking.Events;
using Content.Shared.SS220.Language;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace Content.Server.SS220.Language;
public sealed partial class LanguageSystem : SharedLanguageSystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStart);
        SubscribeLocalEvent<LanguageComponent, MapInitEvent>(OnMapInit);
    }

    private static readonly Dictionary<string, string> ScrambleCache = new Dictionary<string, string>();
    private const int SCRAMBLE_CACHE_LEN = 20;

    private void OnRoundStart(RoundStartingEvent args)
    {
        ScrambleCache.Clear();
    }

    /// <summary>
    ///     Initializes an entity with a language component,
    ///     either the first language in the LearnedLanguages list into the CurrentLanguage variable
    /// </summary>
    private void OnMapInit(Entity<LanguageComponent> ent, ref MapInitEvent args)
    {
        if (ent.Comp.CurrentLanguage == null)
            ent.Comp.CurrentLanguage = ent.Comp.LearnedLanguages.FirstOrDefault(UniversalLanguage);

        Dirty(ent.Owner, ent.Comp);
    }

    /// <summary>
    ///     A method of encrypting the original message into a message created
    ///     from the syllables of prototypes languages
    /// </summary>
    public string ScrambleText(EntityUid? ent, string input, LanguagesPrototype proto)
    {
        input = RemoveColorTags(input);
        var cacheKey = $"{proto.ID}:{input}";

        // If the original message is already there earlier encrypted,
        // it is taken from the cache, it is necessary for the correct display when sending in the radio,
        // when the character whispers and transmits a message to the radio
        if (ScrambleCache.TryGetValue(cacheKey, out var cachedValue))
            return cachedValue;

        var scrambledText = proto.ScrambleMethod.ScrambleMessage(input);

        ScrambleCache[cacheKey] = scrambledText;
        // Removes the first message from the cache if it fills up
        if (ScrambleCache.Count > SCRAMBLE_CACHE_LEN)
        {
            var keysToRemove = ScrambleCache.Keys.Take(ScrambleCache.Count - SCRAMBLE_CACHE_LEN).ToList();
            foreach (var key in keysToRemove)
            {
                ScrambleCache.Remove(key);
            }
        }

        return scrambledText;
    }

    /// <summary>
    ///     Workaround for some message transmissions.
    ///     Removes BBCodes colors leaving only the original message.
    ///     (I couldn't think of anything cleverer)
    /// </summary>
    public string RemoveColorTags(string input)
    {
        if (string.IsNullOrEmpty(input))
            return input;

        string pattern = @"\[color=#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{8})\](.*?)\[/color\]";
        string cleanedText = Regex.Replace(input, pattern, "$2");

        return cleanedText;
    }

    public string SanitizeMessage(EntityUid source, EntityUid listener, string message)
    {
        var languageProto = GetProto(source);
        if (languageProto == null || CheckLanguage(listener, languageProto))
            return message;

        var newMessage = ScrambleText(source, message, languageProto);
        newMessage = SetColor(newMessage, languageProto);
        return newMessage;
    }
}


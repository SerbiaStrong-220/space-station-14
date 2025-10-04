// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Ghost;
using Content.Shared.SS220.Language.Components;
using Robust.Shared.Random;
using Content.Shared.Paper;
using Content.Shared.SS220.Paper;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Content.Shared.SS220.Language.Systems;

public abstract partial class SharedLanguageSystem : EntitySystem
{
    [Dependency] private readonly LanguageManager _language = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public readonly string UniversalLanguage = "Universal";
    public readonly string GalacticLanguage = "Galactic";

    public int Seed = 0;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PaperSetContentAttemptEvent>(OnPaperSetContentAttempt, after: [typeof(SharedDocumentHelperSystem)]);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        _cachedMessages.Clear();
    }

    #region Component
    /// <summary>
    /// Adds languages to <see cref="LanguageComponent.AvailableLanguages"/> from <paramref name="languageIds"/>.
    /// </summary>
    /// <param name="canSpeak">Will entity be able to speak this language</param>
    public void AddLanguages(Entity<LanguageComponent> ent, IEnumerable<string> languageIds, bool canSpeak = false)
    {
        foreach (var language in languageIds)
            AddLanguage(ent, language, canSpeak);
    }

    /// <summary>
    /// Adds a <see cref="LanguageDefinition"/> from list to <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    public void AddLanguages(Entity<LanguageComponent> ent, List<LanguageDefinition> definitions)
    {
        foreach (var def in definitions)
            AddLanguage(ent, def);
    }

    /// <summary>
    /// Adds language to the <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    /// <param name="canSpeak">Will entity be able to speak this language</param>
    public LanguageDefinition? AddLanguage(Entity<LanguageComponent> ent, string languageId, bool canSpeak = false)
    {
        if (!_language.TryGetLanguageById(languageId, out _))
            return null;

        var newDef = new LanguageDefinition(languageId, canSpeak);
        return AddLanguage(ent, newDef) ? newDef : null;
    }

    /// <summary>
    /// Adds a <see cref="LanguageDefinition"/> to the <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    public bool AddLanguage(Entity<LanguageComponent> ent, LanguageDefinition definition)
    {
        if (!ent.Comp.AvailableLanguages.Add(definition))
            return false;

        ent.Comp.SelectedLanguage ??= definition.Id;
        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Ensures that the entity has a <see cref="LanguageDefinition"/> in <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    public LanguageDefinition EnsureLanguage(Entity<LanguageComponent> entity, string languageId)
    {
        var definition = GetLanguageDef(entity, languageId);
        definition ??= AddLanguage(entity, languageId);
        if (definition is null)
            throw new Exception($"Failed to ensure language \"{languageId}\" for entity {ToPrettyString(entity)}");

        return definition;
    }

    /// <summary>
    /// Clears <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    public void ClearLanguages(Entity<LanguageComponent> ent)
    {
        ent.Comp.AvailableLanguages.Clear();
        ent.Comp.SelectedLanguage = null;
        Dirty(ent);
    }

    /// <summary>
    /// Removes language from <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    /// <returns><see langword="true"/> if <paramref name="languageId"/> successful removed</returns>
    public bool RemoveLanguage(Entity<LanguageComponent> ent, string languageId)
    {
        var def = GetLanguageDef(ent, languageId);
        if (def == null)
            return false;

        if (ent.Comp.AvailableLanguages.Remove(def))
        {
            if (ent.Comp.SelectedLanguage == languageId && !TrySelectRandomLanguage(ent))
                ent.Comp.SelectedLanguage = null;

            Dirty(ent);
            return true;
        }

        return false;
    }

    /// <summary>
    /// Does the <see cref="LanguageComponent.AvailableLanguages"/> contain this language.
    /// </summary>
    public static bool HasLanguageDef(Entity<LanguageComponent> ent, string languageId)
    {
        return TryGetLanguageDef(ent, languageId, out _);
    }

    /// <summary>
    /// Gets <see cref="LanguageDefinition"/> from <see cref="LanguageComponent.AvailableLanguages"/> by <paramref name="languageId"/>
    /// </summary>
    public static LanguageDefinition? GetLanguageDef(Entity<LanguageComponent> ent, string languageId)
    {
        return ent.Comp.AvailableLanguages.FirstOrDefault(l => l.Id == languageId);
    }

    /// <summary>
    /// Tries to get <see cref="LanguageDefinition"/> from <see cref="LanguageComponent.AvailableLanguages"/> by <paramref name="languageId"/>
    /// </summary>
    public static bool TryGetLanguageDef(Entity<LanguageComponent> entity, string languageId, [NotNullWhen(true)] out LanguageDefinition? definition)
    {
        definition = GetLanguageDef(entity, languageId);
        return definition != null;
    }

    /// <summary>
    /// Tries set <see cref="LanguageComponent.SelectedLanguage"/> by random language from <see cref="LanguageComponent.AvailableLanguages"/>
    /// </summary>
    public bool TrySelectRandomLanguage(Entity<LanguageComponent> ent)
    {
        if (ent.Comp.SpokenLanguages.Count <= 0)
            return false;

        ent.Comp.SelectedLanguage = _random.Pick(ent.Comp.SpokenLanguages).Id;
        Dirty(ent);
        return true;
    }

    /// <summary>
    /// Tries set <see cref="LanguageComponent.SelectedLanguage"/> by language id.
    /// Doesn't set language if <see cref="LanguageComponent.AvailableLanguages"/> doesn't contain this <paramref name="languageId"/>
    /// </summary>
    public bool TrySelectLanguage(Entity<LanguageComponent> ent, string languageId)
    {
        if (!CanSpeak(ent, languageId))
            return false;

        ent.Comp.SelectedLanguage = languageId;
        Dirty(ent);
        return true;
    }
    #endregion

    /// <summary>
    ///     Checks whether the entity can speak this language.
    /// </summary>
    public bool CanSpeak(EntityUid uid, string languageId)
    {
        if (!TryComp<LanguageComponent>(uid, out var comp))
        {
            // Энтити без компонента языка всегда говорят на универсальном
            return languageId == UniversalLanguage;
        }

        if (comp.KnowAllLanguages)
            return true;

        return TryGetLanguageDef((uid, comp), languageId, out var def) && def.CanSpeak;
    }

    /// <summary>
    ///     Checks whether the entity understands this language.
    /// </summary>
    public bool CanUnderstand(EntityUid uid, string languageId)
    {
        if (KnowsAllLanguages(uid) ||
            languageId == UniversalLanguage)
            return true;

        if (!TryComp<LanguageComponent>(uid, out var comp))
            return false;
        else if (comp.KnowAllLanguages)
            return true;

        return HasLanguageDef((uid, comp), languageId);
    }

    /// <summary>
    ///     Checks whether the entity knows all languages.
    /// </summary>
    public bool KnowsAllLanguages(EntityUid uid)
    {
        return HasComp<GhostComponent>(uid);
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
    ///     Copy of Content.Server.Chat.Systems.ObfuscateMessageReadability
    /// </summary>
    public string ObfuscateMessageReadability(string message, float chance)
    {
        for (var i = 0; i < message.Length; i++)
        {
            if (char.IsWhiteSpace(message[i]))
            {
                continue;
            }

            if (_random.Prob(1 - chance))
            {
                message = message.Remove(i);
                message = message.Insert(i, "~");
            }
        }

        return message;
    }

    /// <summary>
    ///     Returns the int value from a string.
    /// </summary>
    public static int GetSeedFromString(string input)
    {
        const int p = 31;
        const int m = 1000000009;
        int result = 0;
        int p_pow = 1;
        foreach (var c in input)
        {
            result = (result + (c + 1) * p_pow) % m;
            p_pow = p_pow * p % m;
        }
        return result;
    }
}

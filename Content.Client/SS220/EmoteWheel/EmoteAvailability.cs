using System.Linq;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Speech;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.EmoteWheel;

/// <summary>
/// Works out which emotes a species can actually use, by reading its entity prototype rather than
/// spawning anything. Used to grey out emotes in the editor that would never appear on that species'
/// wheel.
/// </summary>
/// <remarks>
/// This is a static approximation of the runtime checks in the emote wheel. It sees species-level gating
/// - components, tags and <see cref="SpeechComponent.AllowedEmotes"/> - which is what actually varies
/// between characters. It does not see transient state such as being a zombie or cluwne, so an emote
/// shown as available here can still be blocked in the moment. Erring towards "available" is deliberate:
/// greying out something the player can in fact use would be worse than the reverse.
/// </remarks>
public static class EmoteAvailability
{
    /// <summary>
    /// Ids of every emote the given species can use. Returns null when the species cannot be resolved,
    /// meaning "no opinion" - callers should treat everything as available.
    /// </summary>
    public static HashSet<string>? ForSpecies(IPrototypeManager prototypes, string? speciesId)
    {
        if (string.IsNullOrEmpty(speciesId)
            || !prototypes.TryIndex<SpeciesPrototype>(speciesId, out var species)
            || !prototypes.TryIndex(species.Prototype, out var entity))
        {
            return null;
        }

        var components = entity.Components.Keys.ToHashSet();
        var tags = GetTags(entity);
        var allowedEmotes = GetAllowedEmotes(entity);

        var result = new HashSet<string>();
        foreach (var emote in prototypes.EnumeratePrototypes<EmotePrototype>())
        {
            if (emote.Category == EmoteCategory.Invalid || emote.ChatTriggers.Count == 0)
                continue;

            if (emote.Whitelist != null && !Matches(emote.Whitelist, components, tags))
                continue;

            if (emote.Blacklist != null && Matches(emote.Blacklist, components, tags))
                continue;

            // Emotes that are not available by default have to be granted explicitly by the species.
            if (!emote.Available && !allowedEmotes.Contains(emote.ID))
                continue;

            result.Add(emote.ID);
        }

        return result;
    }

    private static bool Matches(EntityWhitelist list, HashSet<string> components, HashSet<string> tags)
    {
        var required = (list.Components?.Length ?? 0) + (list.Tags?.Count ?? 0);
        if (required == 0)
            return false;

        var matched = 0;

        if (list.Components != null)
        {
            foreach (var component in list.Components)
            {
                if (components.Contains(component))
                    matched++;
            }
        }

        if (list.Tags != null)
        {
            foreach (var tag in list.Tags)
            {
                if (tags.Contains(tag.Id))
                    matched++;
            }
        }

        return list.RequireAll ? matched == required : matched > 0;
    }

    private static HashSet<string> GetAllowedEmotes(EntityPrototype entity)
    {
        return entity.TryGetComponent<SpeechComponent>("Speech", out var speech)
            ? speech.AllowedEmotes.Select(static x => x.Id).ToHashSet()
            : new HashSet<string>();
    }

    private static HashSet<string> GetTags(EntityPrototype entity)
    {
        return entity.TryGetComponent<TagComponent>("Tag", out var tag)
            ? tag.Tags.Select(static x => x.Id).ToHashSet()
            : new HashSet<string>();
    }
}

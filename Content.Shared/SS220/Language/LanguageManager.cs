// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using System.Diagnostics.CodeAnalysis;

namespace Content.Shared.SS220.Language;

public sealed class LanguageManager
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly INetManager _net = default!;

    public List<LanguagePrototype> Languages { get; private set; } = new();

    public readonly string KeyPrefix = "%";

    public void Initialize()
    {
        Languages = [.. _prototype.EnumeratePrototypes<LanguagePrototype>()];
    }

    /// <summary>
    /// Tries get language prototipe by id
    /// </summary>
    public bool TryGetLanguageById(string id, [NotNullWhen(true)] out LanguagePrototype? language)
    {
        language = Languages.Find(l => l.ID == id);
        return language != null;
    }

    /// <summary>
    /// Tries get language prototipe by language key
    /// </summary>
    public bool TryGetLanguageByKey(string key, [NotNullWhen(true)] out LanguagePrototype? language)
    {
        language = Languages.Find(l => KeyPrefix + l.Key == key);
        return language != null;
    }
}

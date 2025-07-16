// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.CluwneComms;

[Prototype("memelertLevel")]
public sealed partial class MemelertLevelPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    [DataField] public MemelertLevelDetail LevelDetails = new();
}

/// <summary>
/// Alert level detail. Does not contain an ID, that is handled by
/// the Levels field in AlertLevelPrototype.
/// </summary>
[DataDefinition]
public sealed partial class MemelertLevelDetail
{
    /// <summary>
    /// What is announced upon this alert level change. Can be a localized string.
    /// </summary>
    [DataField("announcement")] public string Announcement { get; private set; } = string.Empty;

    /// <summary>
    /// The sound that this alert level will play in-game once selected.
    /// </summary>
    [DataField("sound")] public SoundSpecifier? Sound { get; private set; }

    /// <summary>
    /// The color that this alert level will show in-game in chat.
    /// </summary>
    [DataField("color")] public Color Color { get; private set; } = Color.White;
}


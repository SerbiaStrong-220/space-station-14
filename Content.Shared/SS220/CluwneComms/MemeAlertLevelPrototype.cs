using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.CluwneComms;

[Prototype("memelertLevels")]
public sealed partial class MemelertLevelPrototype : IPrototype
{
    [IdDataField] public string ID { get; } = default!;

    /// <summary>
    /// Dictionary of alert levels. Keyed by string - the string key is the most important
    /// part here. Visualizers will use this in order to dictate what alert level to show on
    /// client side sprites, and localization uses each key to dictate the alert level name.
    /// </summary>
    [DataField("levels")] public Dictionary<string, MemelertLevelDetail> Levels = new();

    /// <summary>
    /// Default level that the station is on upon initialization.
    /// If this isn't in the dictionary, this will default to whatever .First() gives.
    /// </summary>
    [DataField("defaultLevel")] public string DefaultLevel { get; private set; } = default!;
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


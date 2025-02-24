// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Prototypes;


namespace Content.Shared.SS220.Language;

[Prototype("languages")]
public sealed partial class LanguagesPrototype : IPrototype
{
    public const string Universal = "Universal";
    public const string Galactic = "Galactic";

    [ViewVariables, IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string? Name { get; private set; }

    [DataField]
    public string? Description { get; private set; }
    /// <summary>
    ///  List of syllables from which the original message will be encrypted
    ///  A null value does not scramlbe the message in any way
    /// </summary>
    [DataField]
    public List<string>? Syllables = new();
    /// <summary>
    ///  Chance of space between scrambled syllables
    /// </summary>
    [DataField]
    public float SpaceChance { get; private set; } = default!;
    /// <summary>
    ///  Chance for a dot after a scrambled syllable, 
    ///  here you can assign any character instead of a dot
    /// </summary>
    [DataField]
    public string JoinOverride { get; private set; } = ". ";
    /// <summary>
    ///  The color of the language in which messages will be recolored, 
    ///  an empty value will not be recolored
    /// </summary>
    [DataField]
    public Color? Color;
}

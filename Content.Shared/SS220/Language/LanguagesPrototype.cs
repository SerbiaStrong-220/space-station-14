// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language.EncryptionMethods;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Language;

[Prototype("languages")]
public sealed partial class LanguagesPrototype : IPrototype
{
    [ViewVariables, IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string? Name { get; private set; }

    [DataField]
    public string? Description { get; private set; }

    /// <summary>
    ///  The color of the language in which messages will be recolored, 
    ///  an empty value will not be recolored
    /// </summary>
    [DataField]
    public Color? Color;

    [DataField]
    public ScrambleMethod ScrambleMethod = new RandomSyllablesScrambleMethod();
}

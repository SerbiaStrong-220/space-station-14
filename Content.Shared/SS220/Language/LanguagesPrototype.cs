// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.Language.EncryptionMethods;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Language;

[Prototype("language")]
[Serializable, NetSerializable]
public sealed partial class LanguagePrototype : IPrototype
{
    [ViewVariables, IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public string Name = string.Empty;

    [DataField]
    public string Description = string.Empty;

    /// <summary>
    /// Language key
    /// </summary>
    [DataField(required: true)]
    public string Key = string.Empty;
    public string KeyWithPrefix { get => LanguageManager.KeyPrefix + Key; }

    /// <summary>
    ///  The color of the language in which messages will be recolored, 
    ///  an empty value will not be recolored
    /// </summary>
    [DataField]
    public Color? Color;

    /// <summary>
    /// The method used to scramble the message
    /// </summary>
    [DataField]
    public ScrambleMethod ScrambleMethod = new SyllablesScrambleMethod();
}

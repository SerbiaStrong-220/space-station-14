// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Language.EncryptionMethods;

[ImplicitDataDefinitionForInheritors]
public abstract partial class ScrambleMethod
{
    /// <summary>
    /// Scramble <paramref name="message"/> according to a specific algorithm.
    /// It is acceptable to use a specific seed to get the same result if the message is the same
    /// </summary>
    public abstract string ScrambleMessage(string message, int? seed = null);
}

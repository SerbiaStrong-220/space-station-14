// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Language.EncryptionMethods;

/// <summary>
/// Returns the original message without scramble.
/// Used in the universal language
/// </summary>
public sealed partial class EmptyScrambleMethod : ScrambleMethod
{
    public override string ScrambleMessage(string message, int? seed = null)
    {
        return message;
    }
}

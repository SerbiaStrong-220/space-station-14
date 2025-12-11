// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Signature;

[Serializable]
public abstract class AbstractSignatureLogData(SignatureData data)
{
    public const string SignatureLogTag = "[Signature]";

    public override string ToString()
    {
        return $"{SignatureLogTag}({data.Width}x{data.Height})";
    }
}

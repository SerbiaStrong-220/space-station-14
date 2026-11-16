// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.Signature;
using JetBrains.Annotations;

namespace Content.Server.SS220.Signature;

public sealed class SignatureLogData(SignatureData data) : AbstractSignatureLogData(data)
{
    [UsedImplicitly]
    public string Serialized { get; } = SignatureSerializer.Serialize(data);
}

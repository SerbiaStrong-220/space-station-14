// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared.SS220.Pathology;

namespace Content.Server.SS220.Pathology;

[Serializable, NetSerializable]
public sealed partial class EntityProvidedPathologyContext : IPathologyContext
{
    public EntProtoId ProtoId;

    public HashSet<string> DNAs = new();

    public HashSet<string> Fingerprints = new();
}

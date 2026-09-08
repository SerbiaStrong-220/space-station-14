// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Teleport.Components;

/// <summary>
///     Polymorph teleported entity after its being teleported
/// </summary>
[RegisterComponent]
public sealed partial class PolymorphTeleportTargetComponent : Component
{
    /// <summary>
    ///     The entity we polymorph into
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> PolymorphEnt;
}

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Polymorph;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Teleport.Components;

/// <summary>
///     Polymorphs the target after teleportation.
/// </summary>
[RegisterComponent]
public sealed partial class PolymorphAfterTeleportComponent : Component
{
    /// <summary>
    ///     Polymorph prototype applied to the target.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<PolymorphPrototype> PolymorphPrototype;
}

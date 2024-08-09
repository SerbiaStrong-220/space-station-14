// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.CultYogg.Nyarlathotep.Components;

/// <summary>
/// Component for entities to be attacked by Nyarlathotep.
/// </summary>
[RegisterComponent, Access(typeof(NyarlathotepSystem))]
public sealed partial class NyarlathotepTargetComponent : Component
{

}

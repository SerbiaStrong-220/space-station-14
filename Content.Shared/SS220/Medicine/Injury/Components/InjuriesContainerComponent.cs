// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.SS220.Medicine.Injury.Components;

/// <summary>
/// Simple container for injuries 
/// </summary>

[RegisterComponent]
public sealed partial class InjuriesContainerComponent : Component
{
    [DataField("injuries")]
    public List<EntityUid> Injuries = new();
    [DataField("nextTick")]
    public TimeSpan NextTick;
}
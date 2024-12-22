using System.Numerics;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared.SS220.Contractor;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class ContractorTargetComponent : Component
{
    [DataField]
    public EntityUid Performer;

    [DataField]
    public FixedPoint2 AmountTc;

    [DataField]
    public EntityCoordinates PortalPosition; //position where target must be placed

    [DataField]
    [AutoNetworkedField]
    public Vector2 PositionOnStation; // position where target was placed on station

    [DataField]
    public TimeSpan EnteredPortalTime;

    [DataField]
    public EntityUid? PortalEntity;

    public TimeSpan TimeInJail = TimeSpan.FromSeconds(5);
}

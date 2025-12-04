using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Contractor;

/// <summary>
/// This is used for marking a portal.
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class ContractorPortalOnTriggerComponent : Component
{
    [DataField]
    public TimeSpan OpenPortalTime;

    [DataField]
    public NetEntity? TargetEntity;

    [DataField]
    public TimeSpan TimeForTeleportBack = TimeSpan.FromSeconds(5); // just for debug
}

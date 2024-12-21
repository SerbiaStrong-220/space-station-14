using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Contractor;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
[NetworkedComponent]
public sealed partial class ContractorPortalOnTriggerComponent : Component
{
    [DataField]
    public TimeSpan OpenPortalTime;

    [DataField]
    public NetEntity? TargetEntity;

    public TimeSpan MaxPortalTime = TimeSpan.FromSeconds(15);

}

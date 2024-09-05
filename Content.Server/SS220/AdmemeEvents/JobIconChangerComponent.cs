// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.AdmemeEvents;

using Content.Shared.SS220.AdmemeEvents;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

[RegisterComponent]
[Access(typeof(JobIconChangerSystem))]
public sealed partial class JobIconChangerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<FactionIconPrototype>? JobIcon;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool CheckReach = false;

    /// <summary>
    /// Filter mode: None | IOT | NT | USSP
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public EventRoleIconFilterGroup IconFilterGroup = EventRoleIconFilterGroup.None;
}

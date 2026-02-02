// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.AdmemeEvents;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.AdmemeEvents;

[RegisterComponent]
[Access(typeof(JobIconChangerSystem))]
public sealed partial class JobIconChangerComponent : Component
{
    [DataField]
    public ProtoId<FactionIconPrototype>? JobIcon;

    [DataField]
    public bool CheckReach;

    /// <summary>
    /// Filter mode: None | IOT | NT | USSP
    /// </summary>
    [DataField]
    public EventRoleIconFilterGroup IconFilterGroup = EventRoleIconFilterGroup.None;
}

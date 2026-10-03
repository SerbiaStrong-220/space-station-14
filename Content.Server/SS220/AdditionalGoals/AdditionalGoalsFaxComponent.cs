// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.SS220.AdditionalGoals;

/// <summary>
/// Marks the fax in a head's office as a recipient of optional department goals.
/// Routing does not depend on the fax's editable display name.
/// </summary>
[RegisterComponent]
public sealed partial class AdditionalGoalsFaxComponent : Component
{
    /// <summary>
    /// The head of the department who should receive additional goals.
    /// Can be changed at runtime from the ViewVariables menu.
    /// A valid default is needed when adding the component manually: a default
    /// ProtoId contains a null ID, which the VV editor cannot display.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public ProtoId<JobPrototype> Job = "HeadOfPersonnel";
}

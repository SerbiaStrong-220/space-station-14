// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Roles.Components;

namespace Content.Shared.SS220.MalfAI;

[RegisterComponent]
public sealed partial class MalfAiRoleComponent : BaseMindRoleComponent
{
    [ViewVariables]
    public EntityUid? OwnerMind;

    [ViewVariables]
    public EntityUid? Store;

    [ViewVariables]
    public bool SilentCriminalRecords;
}

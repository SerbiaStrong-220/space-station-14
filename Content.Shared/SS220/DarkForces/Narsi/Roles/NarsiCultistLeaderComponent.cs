using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.DarkForces.Narsi.Roles;

[RegisterComponent, NetworkedComponent]
public sealed partial class NarsiCultistLeaderComponent : Component
{
    [DataField("leaderAbility")]
    public EntProtoId LeaderAbility = "NarsiCultistLeaderAction";

    [DataField("leaderActionEntity")]
    public EntityUid? LeaderAbilityEntityUid;
}

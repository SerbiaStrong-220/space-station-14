// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;

namespace Content.Shared.SS220.NuclearReinforcementRequest;

[RegisterComponent]
[NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class NuclearReinforcementRequestComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public int UsesRemain = 1;

    [DataField]
    public EntProtoId UplinkProto = "BaseUplinkRadio40TC";

    [DataField]
    public EntProtoId ReinforcementProto = "ReinforcementRadioSyndicateNukeops";

    [DataField]
    public ProtoId<AntagPrototype> AntagProtoToSearchFor = "Nukeops";
}

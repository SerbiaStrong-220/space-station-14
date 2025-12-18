// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Content.Shared.Materials;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.ResourceMiner;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ResourceMinerComponent : Component
{
    [DataField]
    [AutoNetworkedField]
    public Dictionary<ProtoId<MaterialPrototype>, int> GenerationAmount = new();

    [DataField, AutoNetworkedField]
    public EntityUid? Silo;

    [DataField, AutoNetworkedField]
    public TimeSpan NextUpdate;

    [DataField, AutoNetworkedField]
    public TimeSpan TimeBetweenUpdate = TimeSpan.FromSeconds(1.5f);
}

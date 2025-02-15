using Content.Shared.Whitelist;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.MinorFauna.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class CocoonerComponent : Component
{
    /// <summary>
    /// list of cocoon lists and their conditions
    /// </summary>
    [DataField("cocoonTypes", required: true)]
    public List<CocoonsList> CocoonsList = new();
}

[DataDefinition]
public sealed partial class CocoonsList
{
    [DataField("entityWhiteList")]
    public EntityWhitelist? Whitelist;

    [DataField("entityBlackList")]
    public EntityWhitelist? Blacklist;

    [DataField("entityProtoList", required: true)]
    public List<EntProtoId> Protos = new();
}

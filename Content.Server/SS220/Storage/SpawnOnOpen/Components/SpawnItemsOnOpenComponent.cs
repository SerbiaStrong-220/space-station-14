using Robust.Shared.Prototypes;

namespace Content.Server.SS220.Storage.SpawnOnOpen.Components;

[RegisterComponent]
public sealed partial class SpawnItemsOnOpenComponent : Component
{
    /// <summary>
    ///     The list of entities to spawn,ALL OF WHICH WILL BE SPAWNED
    /// </summary>
    [DataField("items")]
    public List <EntProtoId> Item = new();

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("uses")]
    public int Uses = 1;
}

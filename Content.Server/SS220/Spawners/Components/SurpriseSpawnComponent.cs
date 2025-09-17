using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;
using Robust.Shared.ViewVariables;

namespace Content.Server.Spawners.Components;

[RegisterComponent]
public sealed partial class SurpriseSpawnerComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public int SpawnCount = 1;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float SpawnChance = 1.0f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public float Offset = 0.2f;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool DeleteAfterSpawn = true;
}

// В© SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.Spawners.Components;

[RegisterComponent]
public sealed partial class SpawnOnStorageOpenComponent : Component
{
    [DataField(required: true)]
    public EntityTableSelector Table = default!;

    [DataField]
    public float Offset = 0.2f;

    [DataField]
    public bool Triggered = false;

    [DataField]
    public bool RemoveComponentAfterSpawn = false;
}

using Robust.Shared.Prototypes;
using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server.SS220.Storage.SpawnOnStorageOpen.Components;

[RegisterComponent]
public sealed partial class SpawnOnStorageOpenComponent : Component
{
    /// <summary>
    ///     Basically any class inheriting from <see cref="EntityTableSelector"/> will fit
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Selector;

    [DataField]
    public int Uses = 1;
}

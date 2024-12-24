using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.CultYogg.Buildings;

[RegisterComponent, NetworkedComponent]
public sealed partial class CultYoggBuildingComponent : Component
{
    /// <summary>
    /// List of prototypes to spawn when the building is erased
    /// </summary>
    [DataField]
    public List<CultYoggEntitySpawn>? SpawnOnErase;

    /// <summary>
    /// Time to erase the building
    /// </summary>
    [DataField]
    public TimeSpan? EraseTime = TimeSpan.Zero;
}

[DataDefinition]
public sealed partial class CultYoggEntitySpawn
{
    [DataField(required: true)]
    public EntProtoId Id;

    [DataField]
    public int Amount = 1;

    /// <summary>
    /// If entity has <see cref "Stacks.StackComponent"/> sets the stack size for each entity
    /// </summary>
    [DataField]
    public int? StackAmount;
}

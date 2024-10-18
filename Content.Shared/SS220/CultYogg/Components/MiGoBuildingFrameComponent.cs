// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.CultYogg.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CultYoggBuildingFrameComponent : Component
{
    public const string ContainerId = "cult-yogg-building-frame-storage";

    [ViewVariables]
    public Container Container = default!;

    [ViewVariables, AutoNetworkedField]
    public ProtoId<CultYoggBuildingPrototype> BuildingPrototypeId = default!;

    /// <summary>
    /// Defines added amount of each material as in <see cref="CultYoggBuildingPrototype.Materials"/> list.
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public List<int> AddedMaterialsAmount = [];
}

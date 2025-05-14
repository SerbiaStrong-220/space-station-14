
namespace Content.Shared.SS220.GridZones.Components;

public sealed partial class ZoneComponent : Component
{
    [ViewVariables(VVAccess.ReadOnly)]
    public EntityUid? AttachedGrid;
}

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.ModuleFurniture.Components;

[RegisterComponent]
public sealed partial class ModuleFurnitureFillComponent : Component
{
    /// <summary>
    /// List of EntityProtoId, which will be spawned and inserted into furniture
    /// </summary>
    [DataField("filled", serverOnly: true)]
    public List<EntProtoId> FillingEntity = new();
}

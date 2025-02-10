using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.MinorFauna.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class CocoonerComponent : Component
{
    /// <summary>
    /// Minimal distance for cocooning
    /// </summary>
    [DataField]
    public float CocoonsMinDistance = 0.5f;

    /// <summary>
    ///  Ids of the cocoon prototype
    /// </summary>
    [DataField]
    public List<EntProtoId> CocoonPrototypes = new();

    /// <summary>
    /// List of cocoons created by component owner
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> CocoonsList = new();

}

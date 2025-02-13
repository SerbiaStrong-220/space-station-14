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
    ///  Dict for cocoons
    /// </summary>
    [DataField]
    public Dictionary<CocoonTypes, List<EntProtoId>> CocoonsProto = new();

    /// <summary>
    /// List of cocoons created by component owner
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> CocoonsList = new();

}

public enum CocoonTypes
{
    Humanoids,
    Animals,
    SmallAnimals
}

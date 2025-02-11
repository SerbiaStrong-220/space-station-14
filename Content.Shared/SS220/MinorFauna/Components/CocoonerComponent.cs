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
    ///  Ids of the cocoon prototype for humanoids
    /// </summary>
    [DataField("humanoidCocoons")]
    public List<EntProtoId> CocoonHumanoidPrototypes = new();

    /// <summary>
    ///  Ids of the cocoon prototype for animals
    /// </summary>
    [DataField("animalCocoons")]
    public List<EntProtoId> CocoonAnimalPrototypes = new();

    /// <summary>
    ///  Ids of the cocoon prototype for small animals aka mouses
    /// </summary>
    [DataField("smallAnimalCocoons")]
    public List<EntProtoId> CocoonSmallAnimalPrototypes = new();

    /// <summary>
    /// List of cocoons created by component owner
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> CocoonsList = new();

}

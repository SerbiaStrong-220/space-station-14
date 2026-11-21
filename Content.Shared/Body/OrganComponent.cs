using Content.Shared.Chemistry.Reagent;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Body;

/// <summary>
/// Marks an entity as being able to be inserted into an entity with <seealso cref="BodyComponent" />.
/// </summary>
/// <seealso cref="BodySystem" />
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BodySystem))]
public sealed partial class OrganComponent : Component
{
    /// <summary>
    /// The body entity containing this organ, if any
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Body;

    /// <summary>
    /// What kind of organ is this, if any
    /// </summary>
    [DataField]
    public ProtoId<OrganCategoryPrototype>? Category;

    //SS220 Recycler expansion start
    /// <summary>
    /// The reagent to be used for blood spills if this organ is gibbed.
    /// If null, will attempt to use the body's blood, or a default blood reagent.
    /// </summary>
    [DataField]
    public ProtoId<ReagentPrototype>? BloodReagent;
    //SS220 Recycler expansion end
}

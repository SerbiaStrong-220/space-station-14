// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Content.Shared.Stacks;
using Content.Shared.Tools;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.FCB.ComplexRepairable;

/// <summary>
/// Use this component to mark a device as repairable.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ComplexRepairableComponent : Component
{
    /// <summary>
    ///     All the damage to change information is stored in this <see cref="DamageSpecifier"/>.
    /// </summary>
    /// <remarks>
    ///     If this data-field is specified, it will change damage by this amount instead of setting all damage to 0.
    ///     in order to heal/repair the damage values have to be negative.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public DamageSpecifier? Damage;

    /// <summary>
    /// Cost of fuel used to repair this device.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int FuelCost = 5;

    /// <summary>
    /// Material used to fix the owner of the component 
    /// </summary>
    [DataField]
    public ProtoId<StackPrototype> Material;

    /// <summary>
    /// How much of given material the user has to insert in order to repair mech
    /// </summary>
    [AutoNetworkedField]
    public int LeftToInsert;

    /// <summary>
    /// When total damage reaches this value the user will have to use one piece of specified material. This multiplies with damage
    /// </summary>
    [DataField("materialRepairTreshold"), AutoNetworkedField]
    public FixedPoint2 MaterialRepairTreshold;

    /// <summary>
    /// Tool quality necessary to repair this device.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ToolQualityPrototype> QualityNeeded = "Welding";

    /// <summary>
    /// This value multiplies the time needed to repair the entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public int DoAfterModifier = 1;

    /// <summary>
    /// A multiplier that will be applied to the above if an entity is repairing themselves.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float SelfRepairPenalty = 3f;

    /// <summary>
    /// Whether an entity is allowed to repair itself.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool AllowSelfRepair = true;
}

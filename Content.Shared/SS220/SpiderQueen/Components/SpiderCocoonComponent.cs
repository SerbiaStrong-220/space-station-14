// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SpiderQueen.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpiderCocoonComponent : Component
{
    /// <summary>
    /// The time at which the next every-second action will occur (for example <see cref="DamagePerSecond"/>).
    /// </summary>
    [ViewVariables]
    public TimeSpan NextSecond = TimeSpan.Zero;

    /// <summary>
    /// The entity that created this cocoon
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public EntityUid? CocoonOwner;

    /// <summary>
    /// ID of the container in which the entities placed in the cocoon are stored
    /// </summary>
    [DataField("container", required: true)]
    public string CocoonContainerId = "cocoon";

    /// <summary>
    /// Bonus to max blood points from this cocoon
    /// </summary>
    [DataField]
    public FixedPoint2 BloodPointsBonus = FixedPoint2.Zero;

    /// <summary>
    /// The amount of blood points that can be extracted from the cocoon
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 BloodPointsAmount = FixedPoint2.Zero;

    /// <summary>
    /// How much blood points is given for each unit of blood
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BloodConversionCoefficient = 1f;

    /// <summary>
    /// How much blood is converted into blood points per second
    /// </summary>
    [DataField("bloodConversion"), AutoNetworkedField]
    public FixedPoint2 BloodConversionPerSecond = FixedPoint2.New(0.5);

    /// <summary>
    /// How much damage does the entity receive inside the cocoon
    /// </summary>
    [DataField("damage")]
    public DamageSpecifier? DamagePerSecond;

    /// <summary>
    /// The cap of the damage of the entity, above which the cocoon cannot cause damage.
    /// </summary>
    [DataField]
    public Dictionary<string, FixedPoint2> DamageCap = new();
}

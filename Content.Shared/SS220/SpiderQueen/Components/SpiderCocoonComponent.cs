// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Damage;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SpiderQueen.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpiderCocoonComponent : Component
{
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
    /// Bonus to max mana from this cocoon
    /// </summary>
    [DataField]
    public FixedPoint2 MaxManaBonus = FixedPoint2.Zero;

    /// <summary>
    /// The amount of mana that can be extracted from the cocoon
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 ManaAmount = FixedPoint2.Zero;

    /// <summary>
    /// How much mana is given by entity per second
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 ManaByEntity = FixedPoint2.New(1);

    /// <summary>
    /// How much damage does the entity receive inside the cocoon
    /// </summary>
    [DataField]
    public DamageSpecifier? DamagePerSecond;

    /// <summary>
    /// The cap of the damage of the entity, above which the cocoon cannot cause damage.
    /// Also, when this damage cap is reached, the cocoon stops accumulating mana
    /// </summary>
    [DataField]
    public Dictionary<string, FixedPoint2> DamageCap = new();
}

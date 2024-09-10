// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SpiderQueen.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpiderCocoonComponent : Component
{
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
    /// Bonus to passive mana generation from this cocoon.
    /// This value may vary depending on the number of cocoons.
    /// </summary>
    [DataField]
    public FixedPoint2 ManaGenerationBonus = FixedPoint2.Zero;
}

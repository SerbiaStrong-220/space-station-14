// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SpiderQueen.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpiderQueenComponent : Component
{
    /// <summary>
    /// Current amount of mana
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 CurrentMana = FixedPoint2.Zero;

    /// <summary>
    /// Maximum amount of mana
    /// </summary>
    [DataField]
    public FixedPoint2 MaxMana = FixedPoint2.Zero;

    /// <summary>
    /// How much mana will be generated in a second
    /// </summary>
    [DataField]
    public FixedPoint2 PassiveGeneration = 0.5f;

    [ViewVariables]
    public TimeSpan NextSecond = TimeSpan.Zero;

    /// <summary>
    /// Id of the cocoon prototype
    /// </summary>
    [DataField]
    public EntProtoId CocoonProto = "SpiderCocoon";

    /// <summary>
    /// List of cocoons created by this entity
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public List<EntityUid> CocoonsList = new();

    /// <summary>
    /// The bonus to passive mana generation that give by cocoons 
    /// </summary>
    [ViewVariables, AutoNetworkedField]
    public FixedPoint2 CocoonsManaBonus = FixedPoint2.Zero;

    /// <summary>
    /// Coefficient that indicating how much the bonus from each subsequent cocoon will decrease
    /// </summary>
    [DataField]
    public FixedPoint2 CocoonsBonusCoefficient = 1f;
}

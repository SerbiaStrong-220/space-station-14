// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SpiderQueen.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpiderQueenComponent : Component
{
    [ViewVariables]
    public bool IsAnnounced = false;

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
    public FixedPoint2 PassiveGeneration = FixedPoint2.New(0.5);

    [ViewVariables]
    public TimeSpan NextSecond = TimeSpan.Zero;

    /// <summary>
    /// Id of the cocoon prototype
    /// </summary>
    [DataField]
    public List<EntProtoId> CocoonPrototypes = new();

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
    public FixedPoint2 CocoonsBonusCoefficient = FixedPoint2.New(1);

    /// <summary>
    /// The minimum distance between cocoons for their spawn
    /// </summary>
    [DataField]
    public float CocoonsMinDistance = 0.5f;

    /// <summary>
    /// How many cocoons need to station announcement
    /// </summary>
    [DataField]
    public int? CocoonsCountToAnnouncement;
}

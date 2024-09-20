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
    /// Current amount of blood points
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 CurrentBloodPoints = FixedPoint2.Zero;

    /// <summary>
    /// Maximum amount of blood points
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxBloodPoints = FixedPoint2.Zero;

    /// <summary>
    /// How much hunger converts into blood points per second
    /// </summary>
    [DataField("hungerConversion")]
    public float HungerConversionPerSecond = 0.25f;

    /// <summary>
    /// How much blood points is given for each unit of hunger
    /// </summary>
    [DataField("convertCoefficient")]
    public float HungerConvertCoefficient = 2f;

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
    /// The time it takes to extract blood points from the cocoon
    /// </summary>
    [DataField]
    public TimeSpan CocoonExtractTime = TimeSpan.FromSeconds(3);

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

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Alert;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.SpiderQueen.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SpiderQueenComponent : Component
{
    /// <summary>
    /// The time at which the next every-second action will occur (for example <see cref="HungerConversionPerSecond"/>
    /// </summary>
    [ViewVariables]
    public TimeSpan NextSecond = TimeSpan.Zero;

    /// <summary>
    /// Current amount of blood points
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 CurrentBloodPoints = FixedPoint2.Zero;

    /// <summary>
    /// Maximum amount of blood points
    /// </summary>
    [DataField, AutoNetworkedField]
    public FixedPoint2 MaxBloodPoints = FixedPoint2.New(200);

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

    /// <summary>
    /// How much hunger is given for each unit of extracted blood points
    /// </summary>
    [DataField]
    public float HungerExtractCoefficient = 0.2f;

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
    public TimeSpan CocoonExtractTime = TimeSpan.FromSeconds(5);

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

    /// <summary>
    /// Has there been a special announcement about this entity yet
    /// </summary>
    [ViewVariables]
    public bool IsAnnouncedOnce = false;

    /// <summary>
    /// The prototype of alert that displays the current amount of blood points
    /// </summary>
    [DataField]
    public ProtoId<AlertPrototype> BloodPointsAlert = "BloodPoints";
}

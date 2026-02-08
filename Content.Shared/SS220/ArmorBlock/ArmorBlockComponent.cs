// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.ArmorBlock;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]

public sealed partial class ArmorBlockComponent : Component
{
    /// <summary>
    /// The entity this armor protects
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? Owner = null;
    /// <summary>
    /// The damage tresholds(a.k.a. resists)
    /// </summary>
    [DataField("tresholddict"), AutoNetworkedField]
    public Dictionary<string, FixedPoint2> TresholdDict = new Dictionary<string, FixedPoint2>();

    /// <summary>
    /// A list of armor damage tresholds(a.k.a. resist of the armor itself)
    /// </summary>
    [DataField("durabilitytresholddict"), AutoNetworkedField]
    public Dictionary<string, FixedPoint2> DurabilityTresholdDict = new Dictionary<string, FixedPoint2>();

    /// <summary>
    /// Specifies what types of damage should be converted to others
    /// </summary>
    [DataField("conversiondict"), AutoNetworkedField]
    public Dictionary<string, string> TransformSpecifierDict = new Dictionary<string, string>();
}

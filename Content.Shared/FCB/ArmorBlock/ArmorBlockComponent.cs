// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;

namespace Content.Shared.FCB.ArmorBlock;

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

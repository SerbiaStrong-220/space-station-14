// © FCB, MIT, full text: https://github.com/Free-code-base-14/space-station-14/blob/master/LICENSE.TXT
using Content.Shared.Damage.Prototypes;
using Content.Shared.FixedPoint;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.AltArmor.Components;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]

public sealed partial class AltArmorComponent : Component
{
    /// <summary>
    /// The damage tresholds(a.k.a. resists)
    /// </summary>
    [DataField("tresholddict"), AutoNetworkedField]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> TresholdDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>();

    /// <summary>
    /// A list of armor damage tresholds(a.k.a. resist of the armor itself)
    /// </summary>
    [DataField("durabilitytresholddict"), AutoNetworkedField]
    public Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2> DurabilityTresholdDict = new Dictionary<ProtoId<DamageTypePrototype>, FixedPoint2>();

    /// <summary>
    /// Specifies what types of damage should be converted to others
    /// </summary>
    [DataField("conversiondict"), AutoNetworkedField]
    public Dictionary<ProtoId<DamageTypePrototype>, ProtoId<DamageTypePrototype>> TransformSpecifierDict = new Dictionary<ProtoId<DamageTypePrototype>, ProtoId<DamageTypePrototype>>();

    /// <summary>
    /// Does damage on this entity affect it's protection
    /// </summary>
    [DataField("damageaffects"), AutoNetworkedField]
    public bool DamageAffectsProtection = false;//for now

    /// <summary>
    /// At which amount of damage taken does this entity looses all it's protection
    /// </summary>
    [DataField("zeroprotection"), AutoNetworkedField]
    public int ZeroProtectionThreshold = 100;
}

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.SS220.Medicine.Injury.Components;

[RegisterComponent]
public sealed partial class InjuryComponent : Component
{
    /// <summary>
    /// List of damage specifiers. Deals passive damage.
    /// </summary>

    [DataField("damageSpecifiers", customTypeSerializer: typeof(PrototypeIdListSerializer<DamageTypePrototype>))]
    public List<string> DamageSpecifiers = new();

    /// <summary>
    /// For aghhhhhhh????????? Break bone = slowly moves???
    /// </summary>

    [DataField("consequences")]
    public bool Consequences { get; set; }

    /// <summary>
    /// Can we perform surgery on limb without incision
    /// </summary>
    [DataField("severity")]
    public InjuryStages Severity { get; set; }
    public bool IsBleeding { get; set; }

    [DataField("isGaping")]
    public bool IsGaping { get; set; }

    [DataField("isInfected")]
    public bool IsInfected { get; set; }

    /// <summary>
    /// Where the wound is: inner or outer
    /// </summary>

    [DataField("localization")]
    public InjuryLocalization Localization { get; set; }
}
public enum InjuryStages
{
    LIGHT, // Needed make incision, retraction, clamping and etc for entry into a limb
    MEDIUM, // Need make retraction for entry into a limb
    SEVERE, // You don't need anything, just safe him!
}

public enum InjuryLocalization
{
    Inner,
    Outer,
}
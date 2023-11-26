// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.SS220.Medicine.Injury.Components;

[RegisterComponent]
public sealed partial class InjuryComponent : Component
{
    /// <summary>
    /// List of damage specifiers. Deals passive damage.
    /// </summary>

    [DataField("damage")]
    public DamageSpecifier Damage = new();

    /// <summary>
    /// For aghhhhhhh????????? Broken bones = slowly moves???
    /// </summary>

    [DataField("consequences")]
    public bool Consequences { get; set; }

    /// <summary>
    /// Can we perform surgery on limb without incision
    /// </summary>
    [DataField("severity")]
    public InjurySeverityStages Severity { get; set; }

    [DataField("isGapes")]
    public bool IsGapes { get; set; }

    [DataField("isInfected")]
    public bool IsInfected { get; set; }

    [DataField("isBleeeding")]
    public bool IsBleeding { get; set; }
    
    /// <summary>
    /// Where the wound is: inner or outer
    /// </summary>

    [DataField("location")]
    public InjuryLocation Location { get; set; }
}
public enum InjurySeverityStages
{
    LIGHT, // Needed make incision, retraction, clamping and etc for entry into a limb
    MEDIUM, // Need make retraction for entry into a limb
    SEVERE, // You don't need anything, just safe him!
}

public enum InjuryLocation
{
    Inner,
    Outer,
}
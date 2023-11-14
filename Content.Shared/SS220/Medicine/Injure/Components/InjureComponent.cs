// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Shared.SS220.Medicine.Injure.Components;

[RegisterComponent]
public sealed partial class InjureComponent : Component
{
    /// <summary>
    /// List of damage specifiers. Deals passive damage.
    /// </summary>

    [DataField("damageSpecifiers", customTypeSerializer: typeof(PrototypeIdListSerializer<DamageTypePrototype>))]
    public List<string> DamageSpecifiers = new();

    /// <summary>
    /// For aghhhhhhh????????? Bone break = slowly moves???
    /// </summary>

    [DataField("symptomes")]
    public bool Symptomes { get; set; }

    /// <summary>
    /// Can we perform surgery on limb without incision
    /// </summary>
    [DataField("intervenableStage")]
    public InjureStages IntervenableStage { get; set; }
    public bool IsBleeding { get; set; }

    [DataField("isInnerWound")]
    public bool IsInnerWound { get; set; }
}
public enum InjureStages
{
    LIGHT, // Needed make incision, retraction, clamping and etc for entry into a limb
    MEDIUM, // Need make retraction for entry into a limb
    SEVERE, // You don't need anything, just safe him!
    SURGICAL // Needs?
}
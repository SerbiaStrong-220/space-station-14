using Content.Shared.Damage;
using Robust.Shared.GameStates;

namespace Content.Shared.SS220.OrigamiBook;

[RegisterComponent]
[NetworkedComponent]
public sealed partial class OrigamiWeaponComponent : Component
{
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField(required: true)]
    public DamageSpecifier DamageWithoutGlasses = new();

    [DataField]
    public float TimeParalyze = 4f;

    [DataField]
    public float ThrowSpeedIncrease;

    [DataField]
    public List<string> BlockerSlots = new()
    {
        "eyes",
        "mask",
        "head",
    };
}

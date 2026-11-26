// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;

namespace Content.Shared.SS220.PathologyStatusEffects;

/// <summary>
/// Damages the entity every second by <see cref="Damage"/> for each pathology using this status effect, but not more than <see cref="MaxStacks"/> of them,
/// e.g. several necroses poison the body as two at most.
/// </summary>
[RegisterComponent]
public sealed partial class PathologyDamageStatusEffectComponent : Component
{
    /// <summary>
    /// Damage per second of one stack.
    /// </summary>
    [DataField(required: true)]
    public DamageSpecifier Damage = new();

    [DataField(required: true)]
    public int MaxStacks;

    [ViewVariables]
    public TimeSpan NextUpdate;
}

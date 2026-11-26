// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.PathologyStatusEffects;

/// <summary>
/// Slows the sprint down the more stacks the status effect has, the modifier is reached at <see cref="MaxStacks"/>,
/// e.g. a damaged heart slows more with each stack of damage.
/// </summary>
[RegisterComponent]
public sealed partial class PathologyMovementModStatusEffectComponent : Component
{
    [DataField(required: true)]
    public float SprintSpeedModifier;

    [DataField(required: true)]
    public int MaxStacks;
}

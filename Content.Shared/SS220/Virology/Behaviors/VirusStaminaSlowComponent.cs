// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent]
public sealed partial class VirusStaminaSlowComponent : Component
{
    /// <summary>Fraction host's stamina recovery is reduced by. 0.2 = 20% slower.</summary>
    [DataField]
    public float SlowFraction = 0.2f;

    /// <summary>Stamina decay before we slowed it, will be restored.</summary>
    [ViewVariables]
    public float OriginalDecay;

    [ViewVariables]
    public bool Applied;
}

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent]
public sealed partial class VirusHyperabsorptionComponent : Component
{
    /// <summary>Extra metabolic rate for the current stage (0.1 = +10% faster).</summary>
    [DataField]
    public float SpeedBonus = 0.1f;

    [ViewVariables]
    public bool Reverting;
}

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent]
public sealed partial class VirusMetabolismSlowComponent : Component
{
    /// <summary>Fraction to slow metabolism by (0.1 = 10% slower). Set per stage.</summary>
    [DataField]
    public float Reduction = 0.1f;

    public bool Reverting;
}

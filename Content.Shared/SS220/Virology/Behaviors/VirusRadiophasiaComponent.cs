// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent]
public sealed partial class VirusRadiophasiaComponent : Component
{
    [DataField]
    public float RadiationIntensity = 0.5f;

    /// <summary>Damage healed per rad the carrier receives.</summary>
    [DataField]
    public DamageSpecifier HealPerRad = new();

    /// <summary>We added host's radiation source, so cure only ours.</summary>
    [ViewVariables]
    public bool AddedRadiation;

    /// <summary>Carrier's own radiation to restore to.</summary>
    [ViewVariables]
    public float? PreviousIntensity;
}

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Damage;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

/// <summary>
/// Drives the radiophasia symptom: the host glows green, emits radiation, and is mended by radiation instead
/// of harmed. The glow and radiation grow with the symptom's stage. Lives from stage 1 until cured.
/// </summary>
[RegisterComponent]
public sealed partial class RadiophasiaComponent : Component
{
    /// <summary>The symptom this tracks.</summary>
    [DataField]
    public ProtoId<PathologyPrototype> Pathology = "Radiophasia";

    [DataField]
    public Color LightColor = Color.LimeGreen;

    // base values at stage 1, multiplied by stage number (stage 3 = x3)

    [DataField]
    public float LightRadius = 1.5f;

    [DataField]
    public float LightEnergy = 1f;

    [DataField]
    public float RadiationIntensity = 0.5f;

    /// <summary>
    /// Damage healed per rad the host receives.
    /// </summary>
    [DataField]
    public DamageSpecifier HealPerRad = new();

    [ViewVariables]
    public int Stage = 1;

    /// <summary>Whether we added the host's <see cref="Content.Shared.Radiation.Components.RadiationSourceComponent"/> ourselves, so cure only strips ours.</summary>
    [ViewVariables]
    public bool AddedRadiation;

    /// <summary>Whether we added the host's point light ourselves, so cure only strips ours.</summary>
    [ViewVariables]
    public bool AddedLight;
}

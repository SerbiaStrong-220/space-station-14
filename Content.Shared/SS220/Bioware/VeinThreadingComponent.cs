// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Bioware;

/// <summary>
/// Bioware of the circulation group: reinforced veins lose less blood while bleeding.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BiowareSystem))]
public sealed partial class VeinThreadingComponent : Component
{
    /// <summary>
    /// Multiplier of the blood lost on each bleed tick.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float BleedCoefficient = 0.5f;
}

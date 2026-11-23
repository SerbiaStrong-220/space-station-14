// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Bioware;

/// <summary>
/// Bioware of the nerves group: spliced nerves take less stamina damage, so stuns come slower.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(BiowareSystem))]
public sealed partial class NerveSplicingComponent : Component
{
    /// <summary>
    /// Multiplier of the incoming stamina damage.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float StaminaCoefficient = 0.5f;
}

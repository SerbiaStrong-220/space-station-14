// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.CombustingMindShield;

/// <summary>
/// Allows you to limit the lifespan of the mindshield
/// </summary>

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState]
public sealed partial class CombustingMindShieldComponent : Component
{
    /// <summary>
    /// When Mindshield will cease to exist
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public TimeSpan? CombustionTime;

    /// <summary>
    /// How long the mindshield will exist
    /// </summary>
    [DataField]
    public TimeSpan BeforeCombustionTime = TimeSpan.FromSeconds(180);

    [AutoNetworkedField]
    public EntityUid? Implant;
}

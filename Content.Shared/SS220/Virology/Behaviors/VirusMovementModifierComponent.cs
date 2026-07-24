// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Virology.Behaviors;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class VirusMovementModifierComponent : Component
{
    /// <summary>Walk speed modifier.</summary>
    [DataField, AutoNetworkedField]
    public float Walk = 1f;

    /// <summary>Run speed modifier.</summary>
    [DataField, AutoNetworkedField]
    public float Sprint = 1f;

    public bool Reverting;
}

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Pathology;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(raiseAfterAutoHandleState: true)]
public sealed partial class PathologyMovementModifierComponent : Component
{
    /// <summary>Walk speed coefficient.</summary>
    [DataField, AutoNetworkedField]
    public float Walk = 1f;

    /// <summary>Run speed coefficient.</summary>
    [DataField, AutoNetworkedField]
    public float Sprint = 1f;

    /// <summary>Set while component is being removed so speed refresh drops modifier.</summary>
    public bool Reverting;
}

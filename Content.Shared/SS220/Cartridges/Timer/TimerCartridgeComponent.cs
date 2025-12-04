using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Cartridges.Timer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class TimerCartridgeComponent : Component
{
    [DataField, AutoNetworkedField]
    public TimeSpan Timer;

    [DataField, AutoNetworkedField]
    public bool TimerActive;

    [DataField, AutoNetworkedField]
    public bool TimerNotify = true;
};

// © SS220, MIT full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/MIT_LICENSE.TXT

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.SiliconComponents;

[RegisterComponent]
[NetworkedComponent, AutoGenerateComponentState]

public sealed partial class SiliconPartComponent : Component //Yeah-yeah the naming is pretty messed up
{
    [DataField]
    [AutoNetworkedField]
    public bool Active = true;

    [DataField]
    [AutoNetworkedField]
    public EntityUid? PartOwner = null;

    [DataField]
    public PartType PartType;

    [DataField]
    public int OccupiedSpace = 1;

    [DataField]
    public TimeSpan TimeToInstall = new TimeSpan(0, 0, 5);
}

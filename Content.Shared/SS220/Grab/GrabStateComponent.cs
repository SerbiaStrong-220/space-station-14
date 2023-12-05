// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Shared.SS220.Grab;

[RegisterComponent]
public sealed partial class GrabStateComponent : Component
{
    public NetEntity Inflictor;
    public NetEntity Target;
    public GrabState State;
}
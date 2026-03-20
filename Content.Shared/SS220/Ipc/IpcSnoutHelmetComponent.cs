// Taken from: Corvax https://github.com/space-syndicate/space-station-14

using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Ipc;

[RegisterComponent, NetworkedComponent]
public sealed partial class SnoutHelmetComponent : Component
{
    [DataField]
    public bool EnableAlternateHelmet;

    [DataField(readOnly: true)]
    public string? ReplacementRace;
}

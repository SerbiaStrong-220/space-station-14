using Robust.Shared.GameStates;

namespace Content.Shared.SS220.Kpb;

[RegisterComponent, NetworkedComponent]
public sealed partial class SnoutHelmetComponent : Component
{
    [DataField]
    public bool EnableAlternateHelmet;

    [DataField(readOnly: true)]
    public string? ReplacementRace;
}

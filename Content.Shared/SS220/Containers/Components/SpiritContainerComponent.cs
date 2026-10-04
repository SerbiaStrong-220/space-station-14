// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.DoAfter; // SS220 Spirits can contain
using Robust.Shared.GameStates; // SS220 Spirits can contain

namespace Content.Shared.SS220.Containers.Components;

[RegisterComponent, NetworkedComponent] // SS220 Spirits can contain
public sealed partial class SpiritContainerComponent : Component
{
    // SS220 Spirits can contain begin
    [DataField]
    public float EscapeTime = 5f;

    // Server-side attempts, tracked per occupant so several spirits can escape independently.
    public readonly Dictionary<EntityUid, DoAfterId> Escaping = new();
    // SS220 Spirits can contain end
}


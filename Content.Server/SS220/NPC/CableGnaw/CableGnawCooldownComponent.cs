// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.NPC.CableGnaw;

/// <summary>
/// Runtime-only component that tracks when an NPC may next try to gnaw a cable.
/// Added automatically by <see cref="CableGnawSystem"/>.
/// </summary>
[RegisterComponent, Access(typeof(CableGnawSystem))]
public sealed partial class CableGnawCooldownComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextAttempt = TimeSpan.Zero;
}

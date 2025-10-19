// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.EmergencyShuttleControl.Lockdown;

/// <summary>
///     This is invoked when the <see cref="EmergencyShuttleLockdownComponent"/> has been deactivated by EntitySystem.
/// </summary>
[Serializable, NetSerializable]
public sealed class EmergencyShuttleLockdownDeactiveEvent : EntityEventArgs
{

}

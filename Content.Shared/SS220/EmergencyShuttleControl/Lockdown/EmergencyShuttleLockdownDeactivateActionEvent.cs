// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.EmergencyShuttleControl.Lockdown;

/// <summary>
///     Tells to EntitySystem that must deactivate component.
/// </summary>
[Serializable, NetSerializable]
public sealed class EmergencyShuttleLockdownDeactivateActionEvent : EntityEventArgs
{

}

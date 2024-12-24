// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Serialization;

namespace Content.Shared.SS220.Surgery.Ui;

[Serializable, NetSerializable]
public sealed class SurgeryStarted : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class SurgeryDrapeUpdate(EntityUid user, EntityUid target) : BoundUserInterfaceState
{
    public EntityUid User { get; } = user;
    public EntityUid Target { get; } = target;
}

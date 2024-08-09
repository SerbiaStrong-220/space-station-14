// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

namespace Content.Server.SS220.SuperMatterCrystal;

public sealed class SuperMatterActivationEvent(EntityUid performer, EntityUid target) : HandledEntityEventArgs
{
    public EntityUid Performer = performer;
    public EntityUid Target = target;
}
public sealed class SuperMatterSetAdminDisableEvent(EntityUid performer, EntityUid target, bool adminDisable) : HandledEntityEventArgs
{
    public EntityUid Performer = performer;
    public EntityUid Target = target;
    public bool AdminDisableValue = adminDisable;
}

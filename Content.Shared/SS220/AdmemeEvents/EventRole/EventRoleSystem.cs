// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Cloning;
using Content.Shared.Cloning.Events;
using Robust.Shared.Serialization.Manager;

namespace Content.Shared.SS220.AdmemeEvents.EventRole;

public sealed partial class EventRoleSystem : EntitySystem
{
    [Dependency] private readonly ISerializationManager _serialization = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EventRoleComponent, CloningEvent>(OnCloning);
    }

    private void OnCloning(Entity<EventRoleComponent> entity, ref CloningEvent args)
    {
        var targetComp = EnsureComp<EventRoleComponent>(args.CloneUid);
        _serialization.CopyTo(entity.Comp, ref targetComp, notNullableOverride: true);
    }
}

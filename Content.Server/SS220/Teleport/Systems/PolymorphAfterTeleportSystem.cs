// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Polymorph.Systems;
using Content.Server.SS220.Teleport.Components;
using Content.Shared.SS220.Teleport;

namespace Content.Server.SS220.Teleport.Systems;

public sealed partial class PolymorphAfterTeleportSystem : EntitySystem
{
    [Dependency] private PolymorphSystem _polymorphSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PolymorphAfterTeleportComponent, TargetTeleportedEvent>(OnTargetTeleported);
    }

    private void OnTargetTeleported(Entity<PolymorphAfterTeleportComponent> ent, ref TargetTeleportedEvent args)
    {
        _polymorphSystem.PolymorphEntity(args.Target, ent.Comp.PolymorphPrototype);
    }
}

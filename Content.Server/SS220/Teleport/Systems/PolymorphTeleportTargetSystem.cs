// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Polymorph.Systems;
using Content.Server.SS220.Teleport.Components;
using Content.Shared.SS220.Teleport;

namespace Content.Server.SS220.Teleport.Systems;

public sealed partial class PolymorphTeleportTargetSystem : EntitySystem
{
    [Dependency] private PolymorphSystem _polymorphSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PolymorphTeleportTargetComponent, TargetTeleportedEvent>(OnTargetTeleported);
    }

    private void OnTargetTeleported(Entity<PolymorphTeleportTargetComponent> ent, ref TargetTeleportedEvent args)
    {
        _polymorphSystem.PolymorphEntity(args.Target, ent.Comp.PolymorphEnt);
    }
}

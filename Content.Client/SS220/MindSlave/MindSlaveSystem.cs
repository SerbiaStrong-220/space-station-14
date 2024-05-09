// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.MindSlave;
using Content.Shared.StatusIcon.Components;

namespace Content.Client.SS220.MindSlave;

public sealed class MindSlaveSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindSlaveComponent, CanDisplayStatusIconsEvent>(OnSlaveGetIcons);
        SubscribeLocalEvent<MindSlaveMasterComponent, CanDisplayStatusIconsEvent>(OnMasterGetIcons);
    }

    private void OnSlaveGetIcons(Entity<MindSlaveComponent> entity, ref CanDisplayStatusIconsEvent args)
    {
        if (TryComp<MindSlaveMasterComponent>(args.User, out var masterComp) && masterComp.EnslavedEntities.Contains(entity))
            return;

        args.Cancelled = true;
    }

    private void OnMasterGetIcons(Entity<MindSlaveMasterComponent> entity, ref CanDisplayStatusIconsEvent args)
    {
        if (HasComp<MindSlaveComponent>(args.User) && entity.Comp.EnslavedEntities.Contains(args.User.Value))
            return;

        args.Cancelled = true;
    }
}


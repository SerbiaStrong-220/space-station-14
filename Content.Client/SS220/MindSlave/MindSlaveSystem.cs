// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.MindSlave;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.MindSlave;

public sealed class MindSlaveSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MindSlaveComponent, GetStatusIconsEvent>(OnSlaveGetIcons);
        SubscribeLocalEvent<MindSlaveMasterComponent, GetStatusIconsEvent>(OnMasterGetIcons);
    }

    private void OnSlaveGetIcons(Entity<MindSlaveComponent> entity, ref GetStatusIconsEvent args)
    {
        var viewer = _player.LocalSession?.AttachedEntity;

        if (viewer != entity &&
            (!TryComp<MindSlaveMasterComponent>(viewer, out var masterComp) ||
            !masterComp.EnslavedEntities.Contains(entity)))
            return;

        var iconPrototype = _prototype.Index(entity.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }

    private void OnMasterGetIcons(Entity<MindSlaveMasterComponent> entity, ref GetStatusIconsEvent args)
    {
        var viewer = _player.LocalSession?.AttachedEntity;

        if (viewer != entity &&
            (!HasComp<MindSlaveComponent>(viewer) ||
            !entity.Comp.EnslavedEntities.Contains(viewer.Value)))
            return;

        var iconPrototype = _prototype.Index(entity.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }
}


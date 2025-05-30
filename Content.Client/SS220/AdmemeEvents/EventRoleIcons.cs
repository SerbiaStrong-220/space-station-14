// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Antag;
using Content.Shared.Ghost;
using Content.Shared.SS220.AdmemeEvents;
using Content.Shared.SS220.AdmemeEvents.EventRole;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.AdmemeEvents;

/// <summary>
/// Used for the client to get status icons from other event roles.
/// </summary>
public sealed class EventRoleIconsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _player = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EventRoleComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnGetStatusIcons(Entity<EventRoleComponent> entity, ref GetStatusIconsEvent args)
    {
        var viewer = _player.LocalSession?.AttachedEntity;

        if (viewer != entity &&
            !HasComp<ShowEventRoleIconsComponent>(viewer) &&
            (!TryComp<EventRoleComponent>(viewer, out var viewerComp) ||
            viewerComp.RoleGroupKey != entity.Comp.RoleGroupKey))
            return;

        var iconPrototype = _prototype.Index(entity.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }
}

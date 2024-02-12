// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Client.Antag;
using Content.Shared.Antag;
using Content.Shared.Ghost;
using Content.Shared.SS220.AdmemeEvents;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.AdmemeEvents;

/// <summary>
/// Used for the client to get status icons from other event roles.
/// </summary>
public sealed class EventRoleIconsSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EventRoleComponent, CanDisplayStatusIconsEvent>(OnCanShowRevIcon);
    }

    private void OnCanShowRevIcon(EntityUid uid, IAntagStatusIconComponent component, ref CanDisplayStatusIconsEvent args)
    {
        args.Cancelled = !CanDisplayIcon(args.User, component.IconVisibleToGhost);
    }

    private bool CanDisplayIcon(EntityUid? ent, bool visibleToGhost)
    {
        if (HasComp<GhostComponent>(ent) && visibleToGhost)
            return true;

        if (!HasComp<EventRoleComponent>(ent))
            return false;

        return true;
    }
}

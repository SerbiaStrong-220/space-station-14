using Content.Shared.Ghost;
using Content.Shared.SS220.Overlays;
using Content.Shared.Revolutionary.Components;
using Content.Shared.Revolutionary;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Revolutionary;

/// <summary>
/// Used for the client to get status icons from other revs.
/// </summary>
public sealed class RevolutionarySystem : SharedRevolutionarySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private IPlayerManager _player = default!; // SS220 ghost-hud-settings

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RevolutionaryComponent, GetStatusIconsEvent>(GetRevIcon);
        SubscribeLocalEvent<HeadRevolutionaryComponent, GetStatusIconsEvent>(GetHeadRevIcon);
    }

    private void GetRevIcon(Entity<RevolutionaryComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<HeadRevolutionaryComponent>(ent))
            return;

        // SS220 ghost-hud-settings begin
        var viewer = _player.LocalSession?.AttachedEntity;
        if (HasComp<GhostComponent>(viewer) && !HasComp<ShowRevolutionaryIconsComponent>(viewer))
            return;
        // SS220 ghost-hud-settings end
        if (_prototype.Resolve(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }

    private void GetHeadRevIcon(Entity<HeadRevolutionaryComponent> ent, ref GetStatusIconsEvent args)
    {
        // SS220 ghost-hud-settings begin
        var viewer = _player.LocalSession?.AttachedEntity;
        if (HasComp<GhostComponent>(viewer) && !HasComp<ShowRevolutionaryIconsComponent>(viewer))
            return;
        // SS220 ghost-hud-settings end
        if (_prototype.Resolve(ent.Comp.StatusIcon, out var iconPrototype))
            args.StatusIcons.Add(iconPrototype);
    }
}

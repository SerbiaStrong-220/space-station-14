using System.Linq;
using Content.Shared.Body;
using Content.Shared.Ghost;
using Content.Shared.SS220.Overlays;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Zombies;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.Zombies;

public sealed class ZombieSystem : SharedZombieSystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;
    [Dependency] private IPlayerManager _playerManager = default!; // SS220 ghost-hud-settings

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ZombieComponent, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<ZombieComponent, GetStatusIconsEvent>(GetZombieIcon);
        SubscribeLocalEvent<InitialInfectedComponent, GetStatusIconsEvent>(GetInitialInfectedIcon);
    }

    private void GetZombieIcon(Entity<ZombieComponent> ent, ref GetStatusIconsEvent args)
    {
        // SS220 ghost-hud-settings begin
        var viewer = _playerManager.LocalSession?.AttachedEntity;
        if (HasComp<GhostComponent>(viewer) && !HasComp<ShowZombieIconsComponent>(viewer))
            return;
        // SS220 ghost-hud-settings end
        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }

    private void GetInitialInfectedIcon(Entity<InitialInfectedComponent> ent, ref GetStatusIconsEvent args)
    {
        if (HasComp<ZombieComponent>(ent))
            return;

        // SS220 ghost-hud-settings begin
        var viewer = _playerManager.LocalSession?.AttachedEntity;
        if (HasComp<GhostComponent>(viewer) && !HasComp<ShowZombieIconsComponent>(viewer))
            return;
        // SS220 ghost-hud-settings end
        var iconPrototype = _prototype.Index(ent.Comp.StatusIcon);
        args.StatusIcons.Add(iconPrototype);
    }

    private void OnStartup(EntityUid uid, ZombieComponent component, ComponentStartup args)
    {
        if (HasComp<VisualBodyComponent>(uid))
            return;

        if (!TryComp<SpriteComponent>(uid, out var sprite))
            return;

        for (var i = 0; i < sprite.AllLayers.Count(); i++)
        {
            _sprite.LayerSetColor((uid, sprite), i, component.SkinColor);
        }
    }
}

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CultYogg.CultYoggIcons;
using Content.Shared.SS220.CultYogg.Sacrificials;
using Content.Shared.SS220.CultYogg.Unenslavable;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.CultYogg.CultYoggIcon;

public sealed class ShowCultYoggIconsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ShowCultYoggIconsComponent, GetStatusIconsEvent>(OnGetCultistsIconsEvent);
        SubscribeLocalEvent<CultYoggSacrificialComponent, GetStatusIconsEvent>(OnGetSacrificialIconsEvent);
        SubscribeLocalEvent<UnenslavableComponent, GetStatusIconsEvent>(OnGetUnenslavableIconsEvent);
    }

    private void OnGetCultistsIconsEvent(Entity<ShowCultYoggIconsComponent> uid, ref GetStatusIconsEvent ev)
    {
        if (!TryComp<ShowCultYoggIconsComponent>(uid, out var cultComp))
            return;

        var iconId = cultComp.StatusIcon;

        if (_prototype.TryIndex(iconId, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
        else
            Log.Error($"Invalid faction icon prototype: {iconId}");
    }

    private void OnGetSacrificialIconsEvent(Entity<CultYoggSacrificialComponent> ent, ref GetStatusIconsEvent ev)
    {
        var viewer = _playerManager.LocalSession?.AttachedEntity;
        if (viewer == ent)
            return;

        var iconId = ent.Comp.StatusIcon;

        if (_prototype.TryIndex<FactionIconPrototype>(iconId, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
        else
            Log.Error($"Invalid faction icon prototype: {iconId}");
    }

    private void OnGetUnenslavableIconsEvent(Entity<UnenslavableComponent> ent, ref GetStatusIconsEvent ev)
    {
        var viewer = _playerManager.LocalSession?.AttachedEntity;
        if (viewer == ent)
            return;

        var iconId = ent.Comp.StatusIcon;

        if (_prototype.TryIndex<FactionIconPrototype>(iconId, out var iconPrototype))
            ev.StatusIcons.Add(iconPrototype);
        else
            Log.Error($"Invalid faction icon prototype: {iconId}");
    }
}

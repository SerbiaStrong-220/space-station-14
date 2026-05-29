// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CultYogg.Unenslavable;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.CultYogg.CultYoggIcon;

public sealed class UnenslavableSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<UnenslavableComponent, GetStatusIconsEvent>(OnGetUnenslavableIconsEvent);
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
            Log.Error($"Invalid faction icon id: {iconId}");
    }
}

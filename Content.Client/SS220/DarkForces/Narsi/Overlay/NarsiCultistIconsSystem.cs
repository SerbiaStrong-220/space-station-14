using Content.Shared.GameTicking;
using Content.Shared.SS220.DarkForces.Narsi.Buildings.Altar;
using Content.Shared.SS220.DarkForces.Narsi.Roles;
using Content.Shared.StatusIcon;
using Content.Shared.StatusIcon.Components;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.DarkForces.Narsi.Overlay;

public sealed class NarsiCultistIconsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    [ValidatePrototypeId<FactionIconPrototype>]
    private const string NarsiCultistLeaderIcon = "NarsiCultistLeaderIcon";

    [ValidatePrototypeId<FactionIconPrototype>]
    private const string NarsiCultistIcon = "NarsiCultistIcon";


    private bool _isIconsRitualFinished;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NarsiCultistComponent, GetStatusIconsEvent>(OnGetStatusIcon);
        SubscribeLocalEvent<NarsiCultistLeaderComponent, GetStatusIconsEvent>(OnLeaderGetStatusIcon);
        SubscribeLocalEvent<NarsiCultCreatureComponent, GetStatusIconsEvent>(OnGetCreatureStatusIcon);

        SubscribeNetworkEvent<RoundRestartCleanupEvent>(RoundRestartCleanup);
        SubscribeNetworkEvent<NarsiIconsRitualFinishedEvent>(OnIconsRitualFinished);
    }

    private void RoundRestartCleanup(RoundRestartCleanupEvent ev)
    {
        _isIconsRitualFinished = false;
    }

    private void OnIconsRitualFinished(NarsiIconsRitualFinishedEvent ev)
    {
        _isIconsRitualFinished = true;
    }

    private void OnLeaderGetStatusIcon(Entity<NarsiCultistLeaderComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!_isIconsRitualFinished)
            return;

        args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(NarsiCultistLeaderIcon));
    }
    private void OnGetCreatureStatusIcon(Entity<NarsiCultCreatureComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!_isIconsRitualFinished)
            return;

        args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(NarsiCultistIcon));
    }

    private void OnGetStatusIcon(Entity<NarsiCultistComponent> ent, ref GetStatusIconsEvent args)
    {
        if (!_isIconsRitualFinished)
            return;

        if (HasComp<NarsiCultistLeaderComponent>(ent))
            return;

        args.StatusIcons.Add(_prototype.Index<StatusIconPrototype>(NarsiCultistIcon));
    }
}

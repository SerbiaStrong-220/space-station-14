// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Body.Systems;
using Content.Server.Destructible;
using Content.Server.SS220.GameTicking.Rules.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.SS220.CultYogg.Altar;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.MiGo;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;

namespace Content.Server.SS220.CultYogg.Altar;

public sealed partial class CultYoggAltarSystem : SharedCultYoggAltarSystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly BodySystem _body = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CultYoggAltarComponent, MiGoSacrificeDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<CultYoggAltarComponent> ent, ref MiGoSacrificeDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        if (args.Target == null)
            return;

        if (!TryComp<AppearanceComponent>(ent, out var appearanceComp))
            return;

        _body.GibBody(args.Target.Value, true);
        ent.Comp.Used = true;

        RemComp<StrapComponent>(ent);
        RemComp<DestructibleComponent>(ent);

        var query = EntityQueryEnumerator<GameRuleComponent, CultYoggRuleComponent>();
        while (query.MoveNext(out var uid, out _, out var cultRule))
        {
            var ev = new CultYoggSacrificedTargetEvent(ent);
            RaiseLocalEvent(uid, ref ev, true);
        }

        //send cooldown to a MiGo sacrifice action
        var queryMiGo = EntityQueryEnumerator<MiGoComponent>(); //ToDo ask if this code is ok
        while (queryMiGo.MoveNext(out var uid, out var comp))
        {
            var sacrAction = comp.MiGoSacrificeActionEntity;

            if (comp.MiGoErectActionEntity == null)
                continue;

            if (!TryComp<ActionComponent>(sacrAction, out var actionComponent))
                continue;

            if (actionComponent.UseDelay == null)
                continue;

            _actionsSystem.SetCooldown(sacrAction, actionComponent.UseDelay.Value);
        }

        UpdateAppearance(ent, ent.Comp, appearanceComp);
    }
}

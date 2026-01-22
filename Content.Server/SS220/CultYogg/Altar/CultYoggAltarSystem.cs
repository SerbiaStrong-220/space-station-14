// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Administration.Logs;
using Content.Server.Body.Systems;
using Content.Server.Chat.Systems;
using Content.Server.Destructible;
using Content.Server.Pinpointer;
using Content.Server.SS220.GameTicking.Rules.Components;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.Database;
using Content.Shared.GameTicking.Components;
using Content.Shared.SS220.CultYogg.Altar;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.MiGo;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.CultYogg.Altar;

public sealed partial class CultYoggAltarSystem : SharedCultYoggAltarSystem
{
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IAdminLogManager _adminLog = default!;
    [Dependency] private readonly BodySystem _body = default!;
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly NavMapSystem _navMap = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CultYoggAltarComponent, MiGoSacrificeDoAfterEvent>(OnDoAfter);
    }

    private void OnDoAfter(Entity<CultYoggAltarComponent> ent, ref MiGoSacrificeDoAfterEvent args)
    {
        if (args.Cancelled)
        {
            ent.Comp.AnnounceTime = null;
            return;
        }

        if (!TryComp<StrapComponent>(ent, out var strapComp))
            return;

        var sacrificial = strapComp.BuckledEntities.FirstOrNull();

        if (sacrificial == null)
            return;

        if (!TryComp<AppearanceComponent>(ent, out var appearanceComp))
            return;

        _adminLog.Add(LogType.RoundFlow, LogImpact.Medium, $"Cult Yogg sacrificed {ToPrettyString(sacrificial.Value):target}");
        _body.GibBody(sacrificial.Value, true);
        ent.Comp.Used = true;

        RemComp<StrapComponent>(ent);
        RemComp<DestructibleComponent>(ent);

        var query = EntityQueryEnumerator<GameRuleComponent, CultYoggRuleComponent>();
        while (query.MoveNext(out var uid, out _, out _))
        {
            var ev = new CultYoggSacrificedTargetEvent(ent);
            RaiseLocalEvent(uid, ref ev, true);
        }

        //send cooldown to a MiGo sacrifice action
        var queryMiGo = EntityQueryEnumerator<MiGoComponent>();
        while (queryMiGo.MoveNext(out _, out var comp))
        {
            if (comp.MiGoErectActionEntity == null)
                continue;

            var sacrAction = comp.MiGoSacrificeActionEntity;

            if (!TryComp<ActionComponent>(sacrAction, out var actionComponent))
                continue;

            if (actionComponent.UseDelay == null)
                continue;

            _actionsSystem.SetCooldown(sacrAction, actionComponent.UseDelay.Value);
        }

        UpdateAppearance(ent, ent.Comp, appearanceComp);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CultYoggAltarComponent, TransformComponent>();
        while (query.MoveNext(out var ent, out var altarComp, out var xform))
        {
            if (altarComp.AnnounceTime == null)
                continue;

            if (_time.CurTime <= altarComp.AnnounceTime)
                continue;

            var msg = Loc.GetString("cult-yogg-sacrifice-warning",
    ("location", FormattedMessage.RemoveMarkupOrThrow(_navMap.GetNearestBeaconString((ent, xform)))));
            _chat.DispatchGlobalAnnouncement(msg, announcementSound: altarComp.Sound, colorOverride: Color.Red);

            altarComp.AnnounceTime = null;
        }
    }
}

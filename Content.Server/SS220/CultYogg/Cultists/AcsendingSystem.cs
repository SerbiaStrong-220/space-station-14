// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.CultYoggIcons;
using Robust.Shared.Timing;
using Content.Shared.Examine;

namespace Content.Server.SS220.CultYogg.Cultists;

public sealed class AcsendingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AcsendingComponent, ComponentInit>(SetupAcsending);
        SubscribeLocalEvent<AcsendingComponent, ExaminedEvent>(OnExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AcsendingComponent>();
        while (query.MoveNext(out var uid, out var acsend))
        {
            if (_timing.CurTime < acsend.AcsendingTime)
                continue;

            var ev = new CultYoggForceAscendingEvent();
            RaiseLocalEvent(uid, ref ev);
        }
    }

    private void SetupAcsending(Entity<AcsendingComponent> uid, ref ComponentInit args)
    {
        uid.Comp.AcsendingTime = _timing.CurTime + uid.Comp.AcsendingInterval;
    }
    private void OnExamined(Entity<AcsendingComponent> uid, ref ExaminedEvent args)
    {
        if (!HasComp<ShowCultYoggIconsComponent>(args.Examiner))
            return;

        args.PushMarkup($"[color=green]{Loc.GetString("cult-yogg-cultist-acsending", ("ent", uid))}[/color]");
    }
}

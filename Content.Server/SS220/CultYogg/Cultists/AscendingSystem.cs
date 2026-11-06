// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Examine;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.CultYoggIcons;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg.Cultists;

public sealed partial class AscendingSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private CultYoggSystem _cultYogg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AscendingComponent, ComponentInit>(SetupAscending);
        SubscribeLocalEvent<AscendingComponent, ExaminedEvent>(OnExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<AscendingComponent>();
        while (query.MoveNext(out var ent, out var ascending))
        {
            if (_timing.CurTime < ascending.AscendingTime)
                continue;

            if (TerminatingOrDeleted(ent))//idk what the bug that was, mb this will help
                continue;

            if (TryComp<CultYoggComponent>(ent, out var cult))
                _cultYogg.AscendCultist((ent, cult));

            RemComp<AscendingComponent>(ent);
        }
    }

    private void SetupAscending(Entity<AscendingComponent> uid, ref ComponentInit args)
    {
        uid.Comp.AscendingTime = _timing.CurTime + uid.Comp.AscendingInterval;
    }

    private void OnExamined(Entity<AscendingComponent> uid, ref ExaminedEvent args)
    {
        if (!HasComp<ShowCultYoggIconsComponent>(args.Examiner))
            return;

        args.PushMarkup($"[color=green]{Loc.GetString("cult-yogg-cultist-ascending", ("ent", uid))}[/color]");
    }
}

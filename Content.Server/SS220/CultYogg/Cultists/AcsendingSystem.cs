// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.Examine;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.SS220.CultYogg.CultYoggIcons;
using Content.Shared.Station;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server.SS220.CultYogg.Cultists;

public sealed class AcsendingSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly CultYoggSystem _cultYogg = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AcsendingComponent, ComponentInit>(SetupAcsending);
        SubscribeLocalEvent<AcsendingComponent, ExaminedEvent>(OnExamined);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_station.GetStations().FirstOrNull() is not { } station) // only "proper" way to find THE station
            return;

        var query = EntityQueryEnumerator<AcsendingComponent>();
        while (query.MoveNext(out var ent, out var acsend))
        {
            if (_timing.CurTime < acsend.AcsendingTime)
                continue;

            var owningStation = _station.GetOwningStation(ent);//rude, but working

            if (owningStation != station)//do not allow spawn MiGo not on station, cause idk how to restrict one specific grid (void)
            {
                _popup.PopupClient(Loc.GetString("cult-yogg-acsending-should-be-station"), ent, ent);
                acsend.AcsendingTime += acsend.AcsendingInterval;
                continue;
            }

            if (TerminatingOrDeleted(ent))//idk what the bug that was, mb this will help
                continue;

            if (TryComp<CultYoggComponent>(ent, out var cult))
                _cultYogg.AcsendCultist((ent, cult));

            RemComp<AcsendingComponent>(ent);
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

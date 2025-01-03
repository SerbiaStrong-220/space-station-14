// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.Popups;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg.Cultists;

public sealed class CultYoggPurifiedSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<CultYoggPurifiedComponent, ComponentInit>(OnInit);
    }

    private void OnInit(Entity<CultYoggPurifiedComponent> ent, ref ComponentInit args)
    {
        _popup.PopupEntity(Loc.GetString("cult-yogg-cleansing-start"), ent, ent);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<CultYoggPurifiedComponent>();
        while (query.MoveNext(out var uid, out var cleansedComp))
        {
            if (_timing.CurTime < cleansedComp.PurifyingDecayEventTime)
                continue;

            RemComp<CultYoggPurifiedComponent>(uid);
        }
    }
}

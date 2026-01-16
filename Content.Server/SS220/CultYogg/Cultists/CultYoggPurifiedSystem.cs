// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.GameTicking.Rules;
using Content.Shared.Popups;
using Content.Shared.SS220.CultYogg.Cultists;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Timing;

namespace Content.Server.SS220.CultYogg.Cultists;

public sealed class CultYoggPurifiedSystem : EntitySystem
{
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly CultYoggRuleSystem _cultRuleSystem = default!;

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
        while (query.MoveNext(out var ent, out var purifyedComp))
        {
            if (_timing.CurTime >= purifyedComp.DecayTime)
                RemCompDeferred<CultYoggPurifiedComponent>(ent);

            if (_timing.CurTime >= purifyedComp.PurifyTime)
            {
                //After purifying effect
                _audio.PlayPvs(purifyedComp.PurifiedSound, ent);

                var ev = new CultYoggDeCultingEvent(ent);
                RaiseLocalEvent(ent, ref ev, true);
            }
        }
    }
}

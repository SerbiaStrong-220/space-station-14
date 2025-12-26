// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.SS220.GameTicking.Rules;
using Content.Shared.SS220.Chameleon;
using Content.Shared.SS220.CultYogg.Cultists;

namespace Content.Server.SS220.CultYogg.CultYoggChamelenBlocker;

public sealed class CultYoggChamelenBlockerSystem : EntitySystem
{
    [Dependency] private readonly CultYoggRuleSystem _cultRuleSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CultYoggChamelenBlockerComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<ChangeCultYoggStageEvent>(OnStageChanged);
        SubscribeLocalEvent<CultYoggChamelenBlockerComponent, ChameleonAttemptEvent>(OnChameleonAttempt);
    }

    private void OnInit(Entity<CultYoggChamelenBlockerComponent> ent, ref ComponentInit args)
    {
        if (!_cultRuleSystem.TryGetCultGameRule(out var rule))
            return;

        if (rule.Value.Comp.Stage < CultYoggStage.Alarm)
            return;

        var ev = new ChameleonRevealEvent();
        RaiseLocalEvent(ent, ref ev);
    }

    private void OnStageChanged(ref ChangeCultYoggStageEvent args)
    {
        if (args.Stage != CultYoggStage.Alarm)
            return;

        var query = AllEntityQuery<CultYoggChamelenBlockerComponent>();
        while (query.MoveNext(out var ent, out _))
        {
            var ev = new ChameleonRevealEvent();
            RaiseLocalEvent(ent, ref ev);
        }
    }

    private void OnChameleonAttempt(Entity<CultYoggChamelenBlockerComponent> ent, ref ChameleonAttemptEvent args)
    {
        if (!_cultRuleSystem.TryGetCultGameRule(out var rule))
            return;

        if (rule.Value.Comp.Stage < CultYoggStage.Alarm)
            return;

        var proto = MetaData(ent).EntityPrototype;

        if (proto == null)
            return;

        if (args.Proto == proto)
            return;

        args.Cancelled = true;
    }
}

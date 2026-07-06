// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.Virology;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Server.SS220.Virology;

public sealed partial class VirusImmunityRoundStartSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IConfigurationManager _cfg = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<RulePlayerJobsAssignedEvent>(OnJobsAssigned);
    }

    private void OnJobsAssigned(RulePlayerJobsAssignedEvent ev)
    {
        var min = _cfg.GetCVar(CCVars220.VirologyImmuneCountMin);
        var max = _cfg.GetCVar(CCVars220.VirologyImmuneCountMax);
        if (max <= 0)
            return;

        var eligible = new List<EntityUid>();
        foreach (var player in ev.Players)
        {
            if (player.AttachedEntity is { } mob
                && HasComp<HumanoidProfileComponent>(mob)
                && !HasComp<VirusImmunityComponent>(mob))
                eligible.Add(mob);
        }

        var target = min >= max ? max : _random.Next(min, max + 1);
        var count = Math.Min(target, eligible.Count);
        for (var i = 0; i < count; i++)
            AddComp<VirusImmunityComponent>(_random.PickAndTake(eligible));
    }
}

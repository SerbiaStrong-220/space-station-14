// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.SS220.CCVars;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Server.SS220.Pathology;

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
        var fraction = _cfg.GetCVar(CCVars220.PathologyImmunityFraction);
        if (fraction <= 0f)
            return;

        var eligible = new List<EntityUid>();
        foreach (var player in ev.Players)
        {
            if (player.AttachedEntity is { } mob
                && HasComp<HumanoidProfileComponent>(mob)
                && !HasComp<VirusImmunityComponent>(mob))
                eligible.Add(mob);
        }

        var count = Math.Min((int)MathF.Round(eligible.Count * fraction, MidpointRounding.AwayFromZero), eligible.Count);
        if (count <= 0)
            return;

        for (var i = 0; i < count; i++)
            AddComp<VirusImmunityComponent>(_random.PickAndTake(eligible));
    }
}

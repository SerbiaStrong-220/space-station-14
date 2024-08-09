// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.GameTicking.Rules;
using Content.Server.Objectives.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Objectives.Systems;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.SS220.Objectives;

/// <summary>
/// Handles help progress condition logic and picking random help targets.
/// </summary>
public sealed class CultYoggSummonProgressConditionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedObjectivesSystem _objectives = default!;
    //[Dependency] private readonly TargetObjectiveSystem _target = default!;
    [Dependency] private readonly TraitorRuleSystem _traitorRule = default!;

    public override void Initialize()
    {
        base.Initialize();
    }
}

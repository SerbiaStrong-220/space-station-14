// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Server.Fax;
using Content.Server.Station.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Fax.Components;
using Content.Shared.GameTicking;
using Content.Shared.Mobs.Systems;
using Content.Shared.Paper;
using Content.Shared.Roles;
using Content.Shared.SS220.Photocopier;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.AdditionalGoals;

/// <summary>
/// Sends one random optional assignment per department and station at round start.
/// An empty pool intentionally produces no fax.
/// </summary>
public sealed class AdditionalGoalsSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly FaxSystem _fax = default!;
    [Dependency] private readonly IPlayerManager _players = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;

    // In particular, HeadOfSecurity must never receive these assignments,
    // even if a mapper or a goal author accidentally configures it.
    private static readonly HashSet<ProtoId<JobPrototype>> EligibleJobs =
    [
        "Captain",
        "HeadOfPersonnel",
        "ChiefEngineer",
        "ResearchDirector",
        "ChiefMedicalOfficer",
        "Quartermaster",
    ];

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RoundStartedEvent>(OnRoundStarted);
    }

    private void OnRoundStarted(RoundStartedEvent args)
    {
        var pools = _prototype.EnumeratePrototypes<AdditionalGoalPrototype>()
            .Where(goal => EligibleJobs.Contains(goal.Job))
            .GroupBy(goal => goal.Job)
            .ToDictionary(group => group.Key, group => group.ToList());

        // All faxes for the same department on the same station receive the same goal.
        // Keep this round-local so a restart cannot reuse the previous assignments.
        var selected = new Dictionary<(EntityUid Station, ProtoId<JobPrototype> Job), AdditionalGoalPrototype>();
        var query = EntityQueryEnumerator<AdditionalGoalsFaxComponent, FaxMachineComponent>();
        while (query.MoveNext(out var uid, out var recipient, out var fax))
        {
            if (!pools.TryGetValue(recipient.Job, out var goals) ||
                _station.GetOwningStation(uid) is not { } station)
                continue;

            var key = (station, recipient.Job);
            if (!selected.TryGetValue(key, out var goal))
            {
                goal = _random.Pick(goals);
                selected.Add(key, goal);
            }

            if (!_random.Prob(goal.Chance))
                continue;

            var stationName = MetaData(station).EntityName;
            var headName = _prototype.Index(recipient.Job).LocalizedName;
            var targetName = GetTargetName(goal.Target, station);
            if (goal.Target != AdditionalGoalTarget.None && targetName is null)
                continue;

            var text = Loc.GetString(goal.Text,
                ("station", stationName),
                ("head", headName),
                ("target", targetName ?? string.Empty));
            var paper = new PaperPhotocopiedData
            {
                Content = Loc.GetString("additional-goals-fax-content",
                    ("station", stationName), ("head", headName), ("goal", text)),
                StampState = "paper_stamp-centcom",
                StampedBy =
                [
                    new()
                    {
                        StampedName = Loc.GetString("stamp-component-stamped-name-centcom"),
                        StampedColor = Color.FromHex("#dca019"),
                    },
                ],
            };

            var printout = new PhotocopyableFaxPrintout(
                new Dictionary<Type, IPhotocopiedComponentData> { { typeof(PaperComponent), paper } },
                new PhotocopyableMetaData
                {
                    EntityName = Loc.GetString("additional-goals-fax-paper-name", ("head", headName)),
                    PrototypeId = "PaperNtFormCc",
                });

            // Use the regular fax queue: a powered-off fax will print after power returns.
            _fax.Receive(uid, printout, component: fax);
        }
    }

    private string? GetTargetName(AdditionalGoalTarget target, EntityUid station)
    {
        if (target == AdditionalGoalTarget.None)
            return null;

        var employees = new List<EntityUid>();
        foreach (var session in _players.Sessions)
        {
            if (session.Status != SessionStatus.InGame ||
                session.AttachedEntity is not { } entity ||
                TerminatingOrDeleted(entity) ||
                !_mobState.IsAlive(entity) ||
                _station.GetOwningStation(entity) != station)
                continue;

            employees.Add(entity);
        }

        if (employees.Count == 0)
            return null;

        return Identity.Name(_random.Pick(employees), EntityManager);
    }
}

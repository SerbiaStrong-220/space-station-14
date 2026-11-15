using Content.Shared.Paper;
using Content.Server.Paper;
using Robust.Shared.Random;
using Content.Server.StationRecords.Systems;
using Content.Shared.StationRecords;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;
using Content.Shared.Station;
using System.Linq;
using Robust.Shared.Prototypes;
using Content.Shared.Roles;
using Content.Shared.Mind;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Content.Shared.Ghost;
using Content.Shared.Bed.Cryostorage;

namespace Content.Server.SS220.RedWings;

public sealed partial class RedWingsClientPaperSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private StationRecordsSystem _stationRecords = default!;
    [Dependency] private SharedStationSystem _station = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private IPlayerManager _playerManager = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<RedWingsClientPaperComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(EntityUid uid, RedWingsClientPaperComponent component, MapInitEvent args)
    {
        SetupPaper(uid, component);
    }

    private void SetupPaper(EntityUid uid, RedWingsClientPaperComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        if (TryComp(uid, out PaperComponent? paperComp))
        {
            if (TryGetClientList(component, out var paperContent))
            {
                _paper.SetContent((uid, paperComp), paperContent);
            }
        }
    }

private string GetRandomInsuranceLevel()
{
    var roll = _random.NextDouble();
    
    if (roll < 0.6)
        return Loc.GetString("book-text-redwings-insurance-silver");
    else if (roll < 0.9)
        return Loc.GetString("book-text-redwings-insurance-gold");
    else
        return Loc.GetString("book-text-redwings-insurance-platinum");
}

    private bool TryGetClientList(RedWingsClientPaperComponent component, [NotNullWhen(true)] out string? redWingsClientList)
    {
        redWingsClientList = null;
        var clientAmount = component.ClientAmount;
        var forbiddenDepartment = component.ForbiddenDepartment;
        var forbiddenSpecies = component.ForbiddenSpecies;
        var clientMessage = new FormattedMessage();

        if (_station.GetStations().FirstOrNull() is not { } station)
            return false;

        var allRecords = _stationRecords.GetRecordsOfType<GeneralStationRecord>(station).ToList();

        var forbiddenJobIds = new HashSet<ProtoId<JobPrototype>>();
        foreach (var deptId in forbiddenDepartment)
        {
            if (_prototypeManager.TryIndex<DepartmentPrototype>(deptId, out var dept))
            {
                foreach (var jobId in dept.Roles)
                {
                    forbiddenJobIds.Add(new ProtoId<JobPrototype>(jobId));
                }
            }
        }

        var validRecords = new List<(uint, GeneralStationRecord)>();

        foreach (var record in allRecords)
        {
            var generalRecord = record.Item2;

            if (forbiddenJobIds.Contains(generalRecord.JobPrototype))
                continue;

            if (forbiddenSpecies.Contains(generalRecord.Species))
                continue;

            var foundPlayer = false;
            foreach (var session in _playerManager.Sessions)
            {
                if (session.Status != SessionStatus.InGame)
                    continue;

                if (session.AttachedEntity is not { } entityUid)
                    continue;

                var entityName = MetaData(entityUid).EntityName;
                if (entityName != generalRecord.Name)
                    continue;

                if (HasComp<GhostComponent>(entityUid))
                    continue;

                if (HasComp<CryostorageContainedComponent>(entityUid))
                    continue;

                if (!_mind.TryGetMind(entityUid, out _, out var mindComp))
                    continue;

                if (mindComp.UserId != session.UserId)
                    continue;

                foundPlayer = true;
                break;
            }

            if (!foundPlayer)
                continue;

            validRecords.Add(record);
        }

        if (validRecords.Count == 0)
            return false;

        _random.Shuffle(validRecords);
        var selectedRecords = validRecords.Take(clientAmount).ToList();

        clientMessage.PushNewline();
        foreach (var record in selectedRecords)
        {
            var name = record.Item2.Name;
            var dna = record.Item2.DNA;

            clientMessage.PushNewline();
            clientMessage.AddMarkupPermissive(Loc.GetString("book-text-redwings-client-middle", 
                ("dna", dna ?? ""), 
                ("name", name),
                ("insurance", GetRandomInsuranceLevel())));
            clientMessage.PushNewline();
        }
        clientMessage.PushNewline();

        redWingsClientList = Loc.GetString("book-text-redwings-client-start") + clientMessage + Loc.GetString("book-text-redwings-client-end");

        return true;
    }
}
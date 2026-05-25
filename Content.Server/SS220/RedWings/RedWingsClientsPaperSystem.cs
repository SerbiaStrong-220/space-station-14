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

namespace Content.Server.SS220.RedWings;

public sealed class RedWingsClientPaperSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly PaperSystem _paper = default!;
    [Dependency] private readonly StationRecordsSystem _stationRecords = default!;
    [Dependency] private readonly SharedStationSystem _station = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

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

    private bool TryGetClientList(RedWingsClientPaperComponent component, [NotNullWhen(true)] out string? redWingsClientList)
    {
        redWingsClientList = null;
        var clientAmount = component.ClientAmount;
        var forbiddenDepartment = component.ForbiddenDepartment;
        var clientMessage = new FormattedMessage();

        if (_station.GetStations().FirstOrNull() is not { } station)
            return false;

        var allRecords = _stationRecords.GetRecordsOfType<GeneralStationRecord>(station).ToList();

        var forbiddenJobIds = new HashSet<string>();
        foreach (var deptId in forbiddenDepartment)
        {
            if (_prototypeManager.TryIndex<DepartmentPrototype>(deptId, out var dept))
            {
                foreach (var jobId in dept.Roles)
                {
                    forbiddenJobIds.Add(jobId);
                }
            }
        }

        var filteredRecords = allRecords
            .Where(record => !forbiddenJobIds.Contains(record.Item2.JobPrototype))
            .ToList();

        if (filteredRecords.Count == 0)
            return false;

        _random.Shuffle(filteredRecords);
        var selectedRecords = filteredRecords.Take(clientAmount).ToList();

        clientMessage.PushNewline();
        foreach (var record in selectedRecords)
        {
            var name = record.Item2.Name;
            var dna = record.Item2.DNA;
            
            clientMessage.PushNewline();
            clientMessage.AddMarkupPermissive(Loc.GetString("book-text-redwings-client-middle", ("dna", dna ?? ""), ("name", name)));
            clientMessage.PushNewline();
        }
        clientMessage.PushNewline();
            
        redWingsClientList = Loc.GetString("book-text-redwings-client-start") + clientMessage + Loc.GetString("book-text-redwings-client-end");

        return true;
    }
}

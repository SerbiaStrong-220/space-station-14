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
            if (TryGetClientList(out var paperContent, component))
            {
                    _paper.SetContent((uid, paperComp), paperContent);
            }
        }
    }

    private bool TryGetClientList([NotNullWhen(true)] out string? redWingsClientList, RedWingsClientPaperComponent component)
    {
        redWingsClientList = null;
        var clientAmount = component.ClientAmount;
        var clientMessage = new FormattedMessage();

        if (_station.GetStations().FirstOrNull() is not { } station)
            return false;

        if (!TryComp<StationRecordsComponent>(station, out var stationRecords))
            return false;

        var recordCount = stationRecords.Records.Keys.Count;

        if (recordCount == 0)
            return false;

        var allRecords = _stationRecords.GetRecordsOfType<GeneralStationRecord>(station).ToList();

        if (allRecords.Count == 0)
        {
            return false;
        }
            
        var forbiddenJobIds = new HashSet<string>();
        foreach (var deptId in new[] { "Command", "Security" })
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
        {
            return false;
        }
            
        _random.Shuffle(filteredRecords);
        var selectedRecords = filteredRecords.Take(clientAmount).ToList();   
            
        clientMessage.PushNewline();
        foreach (var record in selectedRecords)
        {
            var name = record.Item2.Name;
            var dna = record.Item2.DNA;
            clientMessage.PushNewline();
            clientMessage.AddMarkupOrThrow(Loc.GetString("book-text-redwings-client-name", ("name", name)));
            clientMessage.PushNewline();
            clientMessage.AddMarkupOrThrow(Loc.GetString("book-text-redwings-client-dna", ("dna", dna ?? "")));
            clientMessage.PushNewline();
            clientMessage.AddMarkupOrThrow(Loc.GetString("book-text-redwings-client-middle"));
            clientMessage.PushNewline();
        }
        clientMessage.PushNewline();
            
        if (!clientMessage.IsEmpty)
        {
            redWingsClientList = Loc.GetString("book-text-redwings-client-start") + clientMessage + Loc.GetString("book-text-redwings-client-end");
            return true;
        }

        return false;
    }
}

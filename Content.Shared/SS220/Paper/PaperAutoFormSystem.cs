// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Access.Components;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Paper;
using Content.Shared.PDA;
using Content.Shared.SS220.CCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using System.Text.RegularExpressions;

namespace Content.Shared.SS220.Paper;

public sealed partial class PaperAutoFormSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    private const string AutoFormDatasetId = "PaperAutoFormDataset";

    private readonly Dictionary<string, ReplacedData> _keyWordsReplace = [];

    private int _gameYearDelta;

    public override void Initialize()
    {
        base.Initialize();
        _gameYearDelta = _configurationManager.GetCVar(CCVars220.GameYearDelta);

        var dataset = _prototypeManager.Index<PaperAutoFormDatasetPrototype>(AutoFormDatasetId);
        foreach (var (key, value) in dataset.KeyWordsReplace)
        {
            var locKey = Loc.GetString(key);
            _keyWordsReplace.Add(locKey, value);
        }
    }

    public string ReplaceKeyWords(Entity<PaperComponent> ent, string content)
    {
        // GenerateRegexAttribute cause errors on the client side and doesn't work
        return Regex.Replace(content, "\\u0025\\b(\\w+)\\b", match =>
        {
            var word = match.Value.ToLower();
            if (!_keyWordsReplace.TryGetValue(word, out var replacedData))
                return word;

            var writer = ent.Comp.Writer;
            return replacedData switch
            {
                ReplacedData.Date => GetCurrentDate(),
                ReplacedData.Time => GetStationTime(),
                ReplacedData.Name => GetWriterName(writer) ?? word,
                ReplacedData.Job => GetWriterJobByID(writer) ?? word,
                _ => word
            };
        });
    }

    private string GetCurrentDate()
    {
        var day = DateTime.UtcNow.AddHours(3).Day;
        var month = DateTime.UtcNow.AddHours(3).Month;
        var year = DateTime.UtcNow.AddHours(3).Year + _gameYearDelta;
        return $"{day:00}.{month:00}.{year}";
    }

    private string GetStationTime()
    {
        var stationTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
        return stationTime.ToString("hh\\:mm\\:ss");
    }

    private string? GetWriterName(EntityUid? writer)
    {
        if (writer is null ||
            !TryComp<MetaDataComponent>(writer.Value, out var metaData))
            return null;

        return metaData.EntityName;
    }

    private string? GetWriterJobByID(EntityUid? writer)
    {
        if (writer is null ||
            !_inventorySystem.TryGetSlotEntity(writer.Value, "id", out var idUid))
            return null;

        string? job = null;
        // PDA
        if (EntityManager.TryGetComponent(idUid, out PdaComponent? pda) &&
            TryComp<IdCardComponent>(pda.ContainedId, out var id) &&
            id.LocalizedJobTitle != null)
        {
            job = id.LocalizedJobTitle;
        }
        // ID Card
        else if (EntityManager.TryGetComponent(idUid, out id) &&
            id.LocalizedJobTitle != null)
        {
            job = id.LocalizedJobTitle;
        }

        return job;
    }
}

public enum ReplacedData : byte
{
    Date,
    Time,
    Name,
    Job,
}

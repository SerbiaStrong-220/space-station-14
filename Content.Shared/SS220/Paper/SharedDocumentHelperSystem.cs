// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Access.Components;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Paper;
using Content.Shared.PDA;
using Content.Shared.SS220.CCVars;
using Robust.Shared.Configuration;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System.Linq;
using System.Text.RegularExpressions;

namespace Content.Shared.SS220.Paper;

public abstract partial class SharedDocumentHelperSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _configurationManager = default!;
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    private int _gameYearDelta;
    private readonly Dictionary<string, DocumentHelperOptions> _allowedTags = new()
    {
        {"%date", DocumentHelperOptions.Date},
        {"%time", DocumentHelperOptions.Time},
    };

    public override void Initialize()
    {
        base.Initialize();
        _gameYearDelta = _configurationManager.GetCVar(CCVars220.GameYearDelta);

        SubscribeLocalEvent<PaperSetContentAttemptEvent>(OnPaperSetContentAttempt);
    }

    private void OnPaperSetContentAttempt(ref PaperSetContentAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        args.TransformedContent = ParseTags(args.Paper, args.TransformedContent);
    }

    #region Paper
    public virtual string ParseTags(Entity<PaperComponent> ent, string content)
    {
        // GenerateRegexAttribute cause errors on the client side and doesn't work
        return Regex.Replace(content, "\\u0025\\b(\\w+)\\b", match =>
        {
            var word = match.Value.ToLower();
            if (!_allowedTags.TryGetValue(word, out var replacedData))
                return word;

            return replacedData switch
            {
                DocumentHelperOptions.Date => GetGameDate(),
                DocumentHelperOptions.Time => GetStationTime(),
                _ => word
            };
        });
    }
    #endregion

    #region Date
    public string GetGameDate()
    {
        var day = DateTime.UtcNow.AddHours(3).Day;
        var month = DateTime.UtcNow.AddHours(3).Month;
        var year = DateTime.UtcNow.AddHours(3).Year + _gameYearDelta;
        return $"{day:00}.{month:00}.{year}";
    }
    #endregion

    #region Time
    public string GetStationTime()
    {
        var stationTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
        return stationTime.ToString("hh\\:mm\\:ss");
    }
    #endregion

    #region Entity name
    public List<string> GetEntNames(EntityUid? uid)
    {
        List<string> namesList = new()
        {
            GetEntName(uid),
            GetEntNameById(uid)
        };

        return namesList.Where(n => n != string.Empty).ToList();
    }

    public string GetEntName(EntityUid? uid)
    {
        if (uid is null)
            return string.Empty;

        return MetaData(uid.Value).EntityName;
    }

    public string GetEntNameById(EntityUid? uid)
    {
        if (uid is null ||
            !_inventorySystem.TryGetSlotEntity(uid.Value, "id", out var idUid))
            return string.Empty;

        var name = string.Empty;
        // Id
        if (TryComp(idUid, out IdCardComponent? id) &&
            id.FullName != null)
        {
            name = id.FullName ?? string.Empty;
        }
        // Pda
        else if (TryComp<PdaComponent>(idUid, out var pda) &&
            TryComp(pda.ContainedId, out id) &&
            id.FullName != null)
        {
            name = id.FullName ?? string.Empty;
        }

        return name;
    }
    #endregion

    #region Entity job
    public string GetEntJobByID(EntityUid? uid)
    {
        if (uid is null ||
            !_inventorySystem.TryGetSlotEntity(uid.Value, "id", out var idUid))
            return string.Empty;

        var job = string.Empty;
        // PDA
        if (EntityManager.TryGetComponent(idUid, out PdaComponent? pda) &&
            TryComp<IdCardComponent>(pda.ContainedId, out var id) &&
            id.LocalizedJobTitle != null)
        {
            job = id.LocalizedJobTitle ?? string.Empty;
        }
        // ID Card
        else if (EntityManager.TryGetComponent(idUid, out id) &&
            id.LocalizedJobTitle != null)
        {
            job = id.LocalizedJobTitle ?? string.Empty;
        }

        return job;
    }
    #endregion

    public Dictionary<DocumentHelperOptions, List<string>> GetOptionValuesPair(DocumentHelperOptions options, EntityUid? uid = null)
    {
        Dictionary<DocumentHelperOptions, List<string>> optionValuesPair = [];
        var i = 0;
        while ((options & DocumentHelperOptions.All) != 0)
        {
            var currentOption = (DocumentHelperOptions)(1 << i);
            if ((currentOption & options) == 0)
            {
                i++;
                continue;
            }

            var values = GetValuesByOption(currentOption, uid);
            if (values.Count > 0)
                optionValuesPair.Add(currentOption, values);

            options ^= currentOption;
            i++;
        }

        return optionValuesPair;
    }

    public virtual List<string> GetValuesByOption(DocumentHelperOptions option, EntityUid? uid = null)
    {
        List<string> values = [];
        switch (option)
        {
            case DocumentHelperOptions.Date:
                values.Add(GetGameDate());
                break;
            case DocumentHelperOptions.Time:
                values.Add(GetStationTime());
                break;
            case DocumentHelperOptions.Name:
                values = values.Union(GetEntNames(uid)).ToList();
                break;
            case DocumentHelperOptions.Job:
                var job = GetEntJobByID(uid);
                if (job != string.Empty)
                    values.Add(job);
                break;
            default:
                break;
        }

        return values;
    }

    public virtual void UpdateUserInterface(Entity<PaperComponent> entity, EntityUid actor)
    {
    }
}

[Flags]
[Serializable, NetSerializable]
public enum DocumentHelperOptions
{
    None = 0,
    Date = 1 << 0,
    Time = 1 << 1,
    Station = 1 << 2,
    Name = 1 << 3,
    Job = 1 << 4,

    // In comment to avoid the same value with Station
    // ServerInfo = Station,

    All = -1,
}

[Serializable, NetSerializable]
public sealed class DocumentHelperOptionsMessage : BoundUserInterfaceMessage
{
    public Dictionary<DocumentHelperOptions, List<string>> OptionValuesPair;

    public DocumentHelperOptionsMessage(Dictionary<DocumentHelperOptions, List<string>> optionValuesPair)
    {
        OptionValuesPair = optionValuesPair;
    }
}

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Paper;
using Content.Shared.PDA;
using Robust.Shared.Timing;
using System.Text.RegularExpressions;

namespace Content.Shared.SS220.Paper;

public sealed partial class PaperAutoFormSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly SharedGameTicker _gameTicker = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    private readonly Dictionary<string, ReplacedData> _keyWordsReplace = new()
    {
        { "%date", ReplacedData.Date },
        { "%дата", ReplacedData.Date },
        { "%time", ReplacedData.Time },
        { "%время", ReplacedData.Time },
        { "%name", ReplacedData.Name },
        { "%имя", ReplacedData.Name },
        { "%job", ReplacedData.Job },
        { "%должность", ReplacedData.Job }
    };

    public string ReplaceKeyWords(Entity<PaperComponent> ent, string content)
    {
        return Regex.Replace(content, "\\u0025\\b(\\w+)\\b", match =>
        {
            var word = match.Value.ToLower();
            if (!_keyWordsReplace.TryGetValue(word, out var replacedData))
                return word;

            var writer = ent.Comp.Writer;
            switch (replacedData)
            {
                case ReplacedData.Date:
                    {
                        var day = DateTime.UtcNow.AddHours(3).Day;
                        var month = DateTime.UtcNow.AddHours(3).Month;
                        var year = 2568;
                        return $"{day:00}.{month:00}.{year}";
                    }

                case ReplacedData.Time:
                    {
                        var stationTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
                        return stationTime.ToString("hh\\:mm\\:ss");
                    }

                case ReplacedData.Name when writer != null:
                    {
                        if (TryComp<MetaDataComponent>(writer.Value, out var metaData))
                            return metaData.EntityName;
                        break;
                    }

                case ReplacedData.Job when writer != null:
                    {
                        if (_inventorySystem.TryGetSlotEntity(writer.Value, "id", out var idUid))
                        {
                            // PDA
                            if (EntityManager.TryGetComponent(idUid, out PdaComponent? pda) &&
                                TryComp<IdCardComponent>(pda.ContainedId, out var id) &&
                                id.JobTitle != null)
                            {
                                return id.JobTitle;
                            }

                            // ID Card
                            if (EntityManager.TryGetComponent(idUid, out id) &&
                                id.JobTitle != null)
                            {
                                return id.JobTitle;
                            }
                        }

                        break;
                    }

                default:
                    break;
            }

            return word;
        });
    }

    private enum ReplacedData : byte
    {
        Date,
        Time,
        Name,
        Job,
    }
}

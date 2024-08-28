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
    [Dependency] private readonly IdExaminableSystem _idExaminableSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;

    public string ReplaceKeyWords(Entity<PaperComponent> ent, string content)
    {
        return Regex.Replace(content, "\\u0025\\b(\\w+)\\b", match =>
        {
            var word = match.Value.ToLower();

            if (word == "%date")
            {
                var day = DateTime.UtcNow.AddHours(3).Day;
                var month = DateTime.UtcNow.AddHours(3).Month;
                var year = 2568;
                return $"{day:00}.{month:00}.{year}";
            }

            if (word == "%time")
            {
                var stationTime = _gameTiming.CurTime.Subtract(_gameTicker.RoundStartTimeSpan);
                return stationTime.ToString("hh\\:mm\\:ss");
            }

            if (ent.Comp.Writer != null)
            {
                var writerUid = (EntityUid)ent.Comp.Writer;
                if (word == "%name")
                {
                    if (TryComp<MetaDataComponent>(writerUid, out var metaData))
                        return metaData.EntityName;
                }

                if (word == "%job")
                {
                    if (_inventorySystem.TryGetSlotEntity(writerUid, "id", out var idUid))
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
                }
            }

            return word;
        });
    }
}

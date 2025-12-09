// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Linq;
using Content.Shared.SS220.Experience;
using Content.Shared.SS220.Experience.Systems;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client.SS220.Experience;

public sealed class ExperienceRedactorSystem : EntitySystem
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly ExperienceSystem _experience = default!;
    [Dependency] private readonly ExperienceInfoSystem _experienceInfo = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    public Dictionary<int, KnowledgePrototype> CachedIndexedKnowledge { private set; get; } = new();

    /// <summary>
    /// This field contains data for changes done in experience component before sending it to server <br/>
    /// Validates changes in debug version
    /// </summary>
    public ExperienceData ExperienceData = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PrototypesReloadedEventArgs>(OnPrototypeReload);

        ReloadCachedIndexedKnowledge();
    }

    private void OnPrototypeReload(PrototypesReloadedEventArgs args)
    {
        if (!args.WasModified<KnowledgePrototype>())
            return;

        ReloadCachedIndexedKnowledge();
    }

    public ExperienceData UpdateExperienceData(EntityUid uid)
    {
        ExperienceData = _experienceInfo.GetEntityExperienceData(uid);
        return ExperienceData;
    }

    public void RemoveKnowledge(int index)
    {
        if (!CachedIndexedKnowledge.TryGetValue(index, out var knowledgePrototype))
        {
            Log.Error($"Tried to remove unknown indexed knowledge with index {index}!");
            return;
        }

        ExperienceData.Knowledges.Remove(knowledgePrototype.ID);
    }

    public void AddKnowledge(int index)
    {
        if (!CachedIndexedKnowledge.TryGetValue(index, out var knowledgePrototype))
        {
            Log.Error($"Tried to add unknown indexed knowledge with index {index}!");
            return;
        }

        ExperienceData.Knowledges.Add(knowledgePrototype.ID);
    }

    private void ReloadCachedIndexedKnowledge()
    {
        CachedIndexedKnowledge = _prototype.EnumeratePrototypes<KnowledgePrototype>()
            .OrderBy(x => Loc.GetString(x.KnowledgeName))
            .Select((x, index) => new { Index = index, Proto = x })
            .ToDictionary(x => x.Index, x => x.Proto);
    }
}

using Content.Server.SS220.DarkForces.Narsi.Progress.Components;
using Content.Server.SS220.DarkForces.Narsi.Runes.Events;

namespace Content.Server.SS220.DarkForces.Narsi.Progress.Objectives.Summon;

public sealed class NarsiCultSummonObjectiveSystem : EntitySystem
{
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<NarsiSummoningEndEvent>(OnNarsiSummoned);
    }

    private void OnNarsiSummoned(NarsiSummoningEndEvent ev)
    {
        var query = EntityQueryEnumerator<NarsiObjectiveComponent>();
        while (query.MoveNext(out _, out var component))
        {
            component.Completed = true;
        }
    }
}

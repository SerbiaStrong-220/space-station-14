// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ChemicalAdaptation;
using Robust.Shared.Timing;

namespace Content.Server.SS220.ChemicalAdaptation;

public sealed class ChemicalAdaptation : SharedChemicalAdaptationSystem
{
    [Dependency] private readonly IGameTiming _time = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChemicalAdaptationComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            List<string> onRemove = [];
            foreach (var (name, info) in comp.ChemicalAdaptations)
            {
                if (info.Duration > _time.CurTime)
                    continue;

                onRemove.Add(name);
            }

            foreach (var name in onRemove)
            {
                RemoveAdaptation(ent, comp, name);
            }
        }
    }

    public void EnsureChemAdaptation(ChemicalAdaptationComponent comp, string chemId, TimeSpan duration, float modifier)
    {
        if (!comp.ChemicalAdaptations.TryGetValue(chemId, out var adapt))
        {
            comp.ChemicalAdaptations.Add(chemId, new AdaptationInfo(duration, modifier));
            return;
        }

        adapt.Modifier *= modifier;
        adapt.Duration = _time.CurTime + duration;

    }

    public void RemoveAdaptation(EntityUid ent, ChemicalAdaptationComponent comp, string chemId)
    {
        comp.ChemicalAdaptations.Remove(chemId);

        if (comp.ChemicalAdaptations.Count == 0)
            RemCompDeferred<ChemicalAdaptationComponent>(ent);
    }
}

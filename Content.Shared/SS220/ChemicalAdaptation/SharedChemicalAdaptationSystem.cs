// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Timing;

namespace Content.Shared.SS220.ChemicalAdaptation;

public sealed class ChemicalAdaptationSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _time = default!;

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChemicalAdaptationComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            List<string> toRemove = [];
            foreach (var (name, info) in comp.ChemicalAdaptations)
            {
                if (info.Duration > _time.CurTime)
                    continue;

                toRemove.Add(name);
            }

            foreach (var name in toRemove)
            {
                RemoveAdaptation((ent, comp), name);
            }
        }
    }

    public void EnsureChemAdaptation(Entity<ChemicalAdaptationComponent> ent, string chemId, TimeSpan duration, float modifier, bool refresh)
    {
        if (!ent.Comp.ChemicalAdaptations.TryGetValue(chemId, out var adapt))
        {
            ent.Comp.ChemicalAdaptations.Add(chemId, new AdaptationInfo(duration, modifier, refresh));
            Dirty(ent, ent.Comp);
            return;
        }

        adapt.Modifier *= modifier;

        if (refresh)
            adapt.Duration = _time.CurTime + duration;
        else
            adapt.Duration += duration;

        Dirty(ent, ent.Comp);
    }

    public void RemoveAdaptation(Entity<ChemicalAdaptationComponent> ent, string chemId)
    {
        ent.Comp.ChemicalAdaptations.Remove(chemId);

        if (ent.Comp.ChemicalAdaptations.Count == 0)
            RemCompDeferred<ChemicalAdaptationComponent>(ent);

        Dirty(ent, ent.Comp);
    }

    public bool TryGetModifier(EntityUid ent, string reagent, out float value)
    {
        if (!TryComp<ChemicalAdaptationComponent>(ent, out var adaptComp)
            || !adaptComp.ChemicalAdaptations.TryGetValue(reagent, out var adaptationInfo))
        {
            value = 0;
            return false;
        }

        value = adaptationInfo.Modifier;
        return true;
    }
}

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Robust.Shared.Timing;

namespace Content.Shared.SS220.ChemicalAdaptation;

public abstract class SharedChemicalAdaptationSystem : EntitySystem
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

    public bool TryModifyValue(EntityUid ent, string reagent, ref int value)
    {
        if (!TryComp<ChemicalAdaptationComponent>(ent, out var adaptComp))
            return false;

        if (!adaptComp.ChemicalAdaptations.TryGetValue(reagent, out var adaptationInfo))
            return false;

        value = (int)(value * adaptationInfo.Modifier);

        return true;
    }

    public bool TryModifyValue(EntityUid ent, string reagent, ref float value)
    {
        if (!TryComp<ChemicalAdaptationComponent>(ent, out var adaptComp))
            return false;

        if (!adaptComp.ChemicalAdaptations.TryGetValue(reagent, out var adaptationInfo))
            return false;

        value = value * adaptationInfo.Modifier;

        return true;
    }

    public bool TryModifyValue(EntityUid ent, string reagent, ref TimeSpan value)
    {
        if (!TryComp<ChemicalAdaptationComponent>(ent, out var adaptComp))
            return false;

        if (!adaptComp.ChemicalAdaptations.TryGetValue(reagent, out var adaptationInfo))
            return false;

        value = value * adaptationInfo.Modifier;

        return true;
    }
}

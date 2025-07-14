// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ChemicalAdaptation;
using Content.Shared.StatusEffect;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;

namespace Content.Server.SS220.ChemicalAdaptation;

public sealed class ChemicalAdaptation : SharedChemicalAdaptationSystem
{
    [Dependency] private readonly IGameTiming _time = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
    }
    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ChemicalAdaptationComponent>();
        while (query.MoveNext(out var ent, out var comp))
        {
            foreach (var (name, info) in comp.ChemicalAdaptations)
            {
                if (info.Duration > _time.CurTime)
                    continue;

                RemoveChem(ent, comp, name);
                return; //idk how to properly del element from list in cycle, so "return"
            }
        }
    }

    public void EnsureChemAdaptation(ChemicalAdaptationComponent comp, string chemId, TimeSpan duration, float modifier)
    {
        if (comp.ChemicalAdaptations.TryGetValue(chemId, out var adapt))
        {
            adapt.Modifier *= modifier;
            adapt.Duration = _time.CurTime + duration;
            return;
        }

        comp.ChemicalAdaptations.Add(chemId, new AdaptationInfo(duration, modifier));
    }

    public void RemoveChem(EntityUid ent, ChemicalAdaptationComponent comp, string chemId)
    {
        comp.ChemicalAdaptations.Remove(chemId);

        if (comp.ChemicalAdaptations.Count == 0)
            RemCompDeferred<ChemicalAdaptationComponent>(ent);
    }
}

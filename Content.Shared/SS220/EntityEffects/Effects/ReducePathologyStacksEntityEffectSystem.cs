// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Network;

namespace Content.Shared.SS220.EntityEffects.Effects;

public sealed partial class ReducePathologyStacksEntityEffectSystem : EntityEffectSystem<PathologyHolderComponent, ReducePathologyStacks>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPathologySystem _pathology = default!;

    protected override void Effect(Entity<PathologyHolderComponent> entity, ref EntityEffectEvent<ReducePathologyStacks> args)
    {
        // pathologies are server authoritative, predicted metabolism must not change them
        if (_net.IsClient)
            return;

        if (!entity.Comp.ActivePathologies.TryGetValue(args.Effect.Pathology, out var data) || data.Level > 0)
            return;

        _pathology.TryChangePathologyStack(entity.AsNullable(), args.Effect.Pathology, -1);
    }
}

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityEffects;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Network;

namespace Content.Shared.SS220.EntityEffects.Effects;

public sealed partial class AddPathologyEntityEffectSystem : EntityEffectSystem<PathologyHolderComponent, AddPathology>
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private SharedPathologySystem _pathology = default!;

    protected override void Effect(Entity<PathologyHolderComponent> entity, ref EntityEffectEvent<AddPathology> args)
    {
        // pathologies are server authoritative, predicted metabolism must not change them
        if (_net.IsClient)
            return;

        _pathology.TryAddOrStackPathology(entity.AsNullable(), args.Effect.Pathology, args.Effect.Stacks);
    }
}

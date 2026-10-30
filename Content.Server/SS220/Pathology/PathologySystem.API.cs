// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Content.Server.Forensics;
using Content.Shared.Random;
using Content.Shared.Random.Helpers;
using Content.Shared.SS220.Pathology;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.SS220.Pathology;

public sealed partial class PathologySystem : SharedPathologySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private IPrototypeManager _prototype = default!;

    public bool TryMakeEntityContext(Entity<ForensicsComponent?> provider, ProtoId<WeightedRandomEntityPrototype>? providerEntityRandom, [NotNullWhen(true)] out EntityProvidedPathologyContext? context)
    {
        context = null;
        if (providerEntityRandom is null)
            return false;

        if (!_prototype.Resolve(providerEntityRandom, out var randomEntityPrototype))
            return false;

        if (randomEntityPrototype.Weights.Count == 0)
        {
            Log.Error($"Got empty weight dictionary for {nameof(WeightedRandomEntityPrototype)} {providerEntityRandom}!");
            return false;
        }

        var entity = _random.Pick(randomEntityPrototype.Weights);
        context = new EntityProvidedPathologyContext { ProtoId = entity };

        if (Resolve(provider.Owner, ref provider.Comp, logMissing: false))
        {
            context.DNAs = provider.Comp.DNAs;
            context.Fingerprints = provider.Comp.Fingerprints;
        }

        return true;
    }
}

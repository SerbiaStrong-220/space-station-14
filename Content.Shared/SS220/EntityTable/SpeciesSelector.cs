// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityTable;

/// <summary>
/// Selects contents using the species prototype ID supplied in the context.
/// </summary>
public sealed partial class SpeciesSelector : EntityTableSelector
{
    /// <summary>
    /// Context key for the recipient's typed species prototype ID.
    /// </summary>
    public const string SpeciesContextKey = "RecipientSpecies";

    /// <summary>
    /// Selectors for supported species.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<SpeciesPrototype>, EntityTableSelector> Species = [];

    /// <summary>
    /// Selector used when there is no dedicated selector for the species.
    /// </summary>
    [DataField(required: true)]
    public EntityTableSelector Fallback = default!;

    /// <summary>
    /// Optional selector used when the context provides no recipient species.
    /// </summary>
    [DataField]
    public EntityTableSelector? WithoutSpecies;

    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        return GetSelector(ctx).GetSpawns(rand, entMan, proto, ctx);
    }

    protected override IEnumerable<(EntProtoId spawn, double)> ListSpawnsImplementation(
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        return GetSelector(ctx).ListSpawns(entMan, proto, ctx);
    }

    protected override IEnumerable<(EntProtoId spawn, double)> AverageSpawnsImplementation(
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        return GetSelector(ctx).AverageSpawns(entMan, proto, ctx);
    }

    private EntityTableSelector GetSelector(EntityTableContext ctx)
    {
        if (!ctx.TryGetData<ProtoId<SpeciesPrototype>>(SpeciesContextKey, out var species))
            return WithoutSpecies ?? Fallback;

        if (!Species.TryGetValue(species, out var selector))
            return Fallback;

        return selector;
    }
}

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.EntityTable;

/// <summary>
/// Selects an entity table using the job prototype ID supplied in the context.
/// </summary>
public sealed partial class JobSelector : EntityTableSelector
{
    /// <summary>
    /// Context key for the recipient's typed job prototype ID.
    /// </summary>
    public const string JobContextKey = "RecipientJob";

    /// <summary>
    /// Entity tables for supported jobs.
    /// </summary>
    [DataField(required: true)]
    public Dictionary<ProtoId<JobPrototype>, ProtoId<EntityTablePrototype>> Jobs = [];

    /// <summary>
    /// Table used when the context has no job or the job has no dedicated table.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<EntityTablePrototype> Fallback;

    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        return GetTable(proto, ctx).GetSpawns(rand, entMan, proto, ctx);
    }

    protected override IEnumerable<(EntProtoId spawn, double)> ListSpawnsImplementation(
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        return GetTable(proto, ctx).ListSpawns(entMan, proto, ctx);
    }

    protected override IEnumerable<(EntProtoId spawn, double)> AverageSpawnsImplementation(
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        return GetTable(proto, ctx).AverageSpawns(entMan, proto, ctx);
    }

    private EntityTableSelector GetTable(IPrototypeManager proto, EntityTableContext ctx)
    {
        if (!ctx.TryGetData<ProtoId<JobPrototype>>(JobContextKey, out var job))
            return proto.Index(Fallback).Table;

        if (!Jobs.TryGetValue(job, out var table))
            return proto.Index(Fallback).Table;

        return proto.Index(table).Table;
    }
}

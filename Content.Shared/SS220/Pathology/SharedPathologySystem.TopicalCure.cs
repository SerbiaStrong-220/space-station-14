// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Prototypes;

namespace Content.Shared.SS220.Pathology;

public abstract partial class SharedPathologySystem
{
    private void InitializeTopicalCureEvents()
    {
        SubscribeLocalEvent<TopicalPathologyCureComponent, HealingCanTreatEvent>(OnTopicalCanTreat);
        SubscribeLocalEvent<TopicalPathologyCureComponent, HealingTreatedEvent>(OnTopicalTreated);
    }

    private void OnTopicalCanTreat(Entity<TopicalPathologyCureComponent> ent, ref HealingCanTreatEvent args)
    {
        if (TryGetTopicalCurable(args.Target, out _))
            args.CanTreat = true;
    }

    private void OnTopicalTreated(Entity<TopicalPathologyCureComponent> ent, ref HealingTreatedEvent args)
    {
        // which one is cured is up to the server, its state comes to the client
        if (_net.IsClient || !TryGetTopicalCurable(args.Target, out var pathologyId))
            return;

        TryRemovePathology(args.Target, pathologyId.Value, checkStacks: false);
    }

    private bool TryGetTopicalCurable(EntityUid target, [NotNullWhen(true)] out ProtoId<PathologyPrototype>? pathologyId)
    {
        pathologyId = null;
        if (!TryComp<PathologyHolderComponent>(target, out var holder))
            return false;

        foreach (var (id, data) in holder.ActivePathologies)
        {
            if (!TryResolveDefinition(id, data.Level, out _, out var definition) || !definition.CuredByTopicals)
                continue;

            pathologyId = id;
            return true;
        }

        return false;
    }
}

// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Server.Body.Systems;
using Content.Shared.SS220.Pathology;

namespace Content.Server.SS220.Pathology;

public sealed partial class PathologySystem
{
    private void InitializeBreathing()
    {
        // must run before the body relays the inhaled gas to the lungs
        SubscribeLocalEvent<PathologyHolderComponent, InhaledGasEvent>(OnInhaledGas, before: [typeof(RespiratorSystem)]);
    }

    private void OnInhaledGas(Entity<PathologyHolderComponent> entity, ref InhaledGasEvent args)
    {
        if (args.Handled || !IsBreathingStopped(entity.Comp))
            return;

        // handled but not succeeded: the respirator gives the gas back and the body suffocates like without lungs
        args = args with { Handled = true, Succeeded = false };
    }

    private bool IsBreathingStopped(PathologyHolderComponent holder)
    {
        foreach (var (pathologyId, data) in holder.ActivePathologies)
        {
            if (TryResolveDefinition(pathologyId, data.Level, out _, out var definition) && definition.StopsBreathing)
                return true;
        }

        return false;
    }
}

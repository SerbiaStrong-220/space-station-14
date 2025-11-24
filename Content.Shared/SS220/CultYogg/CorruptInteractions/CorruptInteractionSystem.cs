// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.CultYogg.Cultists;
using Content.Shared.Tools.Components;
using Content.Shared.Tools.Systems;

namespace Content.Shared.SS220.CultYogg.CorruptInteractions;

public sealed class CorruptInteractionsSystem : EntitySystem
{
    [Dependency] private readonly WeldableSystem _weldable = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<WeldCorruptInteractionComponent, CorruptInteractionEvent>(OnCorruptInteraction);
    }

    private void OnCorruptInteraction(Entity<WeldCorruptInteractionComponent> ent, ref CorruptInteractionEvent args)
    {
        if (args.Handled)
            return;

        if (!TryComp<WeldableComponent>(ent, out var weldable))
            return;

        _weldable.SetWeldedState(ent, !weldable.IsWelded);

        args.Handled = true;
    }
}

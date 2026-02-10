// © SS220, An EULA/CLA with a hosting restriction, full text: https://raw.githubusercontent.com/SerbiaStrong-220/space-station-14/master/CLA.txt

using Content.Shared.SS220.ClinkGlasses;
using Robust.Shared.Timing;

namespace Content.Client.SS220.ClinkGlasses;

public sealed class ClinkGlassesSystem : SharedClinkGlassesSystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;

    protected override void DoRaiseGlass(EntityUid initiator, EntityUid item)
    {
        if (!TryComp<ClinkGlassesInitiatorComponent>(initiator, out var initiatorComp))
            return;

        initiatorComp.NextClinkTime = _gameTiming.CurTime + initiatorComp.Cooldown;
    }

    protected override void DoClinkGlassesOffer(EntityUid initiator, EntityUid receiver, EntityUid item)
    {
        if (!TryComp<ClinkGlassesInitiatorComponent>(initiator, out var initiatorComp))
            return;

        initiatorComp.NextClinkTime = _gameTiming.CurTime + initiatorComp.Cooldown;
    }

}
